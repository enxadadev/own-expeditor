using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OwnExpeditor.App.Controls;
using OwnExpeditor.App.Services;
using OwnExpeditor.App.ViewModels;
using OwnExpeditor.Core;
using OwnExpeditor.Core.Modules.Jobs;
using OwnExpeditor.Core.Session;
using OwnExpeditor.Core.Text;
using OwnExpeditor.Domain.Games;

namespace OwnExpeditor.App.Modules.Jobs;

/// <summary>
/// Módulo Fretes (design/fretes-pais-cidade): Saída (país → cidade → empresa [+ vaga]) → Chegada (país → cidade → empresa) →
/// carga. A cadeia de escolhas é do Core (<see cref="JobSelectionRules"/>): aqui cada escolha do usuário vira uma
/// <see cref="JobSelection"/> revalidada e os oito seletores são repopulados a partir dela. O frete vira <see cref="ChangeSet"/>
/// por <see cref="JobPlanner"/> a partir de <c>session.Original</c> e só entra na sessão com <c>Stage</c>. Nada é gravado aqui.
/// </summary>
public sealed partial class JobsPageViewModel : ModulePageViewModel
{
    public const string Id = JobPlanner.ModuleId;

    private sealed record Staged(JobRequest Request, ExistingOffer? Offer);

    private readonly IJobBoardFactory _factory;
    private readonly IToastService _toast;
    private readonly IUiDispatcher _ui;
    private readonly Dictionary<string, Staged> _staged = [];
    private readonly HashSet<GameKind> _refreshed = [];
    private CancellationTokenSource? _lifetime;
    private JobBoard? _board;
    private IEditSession? _session;
    private bool _refreshing;
    private bool _nextJobPending; // Add pediu: o próximo Rebuild (já com o board novo) aplica NextJob em vez de revalidar

    private JobSelection _sel = new();
    private int _hours = 72;
    private Staged? _editing;
    private string? _editingKey;
    private IReadOnlyList<JobField> _noticeCleared = [];
    private IReadOnlyList<JobField> _noticeAuto = [];
    private string? _error;
    private bool _cloudDismissed;

    public JobsPageViewModel(ILocalizer loc, IJobBoardFactory factory, IToastService toast, IUiDispatcher ui) : base(loc)
    {
        _factory = factory;
        _toast = toast;
        _ui = ui;

        Listen(FromCountry, JobField.FromCountry);
        Listen(FromCity, JobField.FromCity);
        Listen(FromCompany, JobField.FromCompany);
        Listen(Slot, JobField.Slot);
        Listen(ToCountry, JobField.ToCountry);
        Listen(ToCity, JobField.ToCity);
        Listen(ToCompany, JobField.ToCompany);
        Listen(Cargo, JobField.Cargo);

        SwapCommand = new RelayCommand(Swap, () => CanSwap);
        AddCommand = new RelayCommand(Add, () => CanAdd);
        CancelEditCommand = new RelayCommand(CancelEdit);
        DismissNoticeCommand = new RelayCommand(() => { _noticeCleared = _noticeAuto = []; Notify(); });
        DismissErrorCommand = new RelayCommand(() => { _error = null; Notify(); });
        DismissCloudCommand = new RelayCommand(() => { _cloudDismissed = true; Notify(); });
        OpenSettingsCommand = new RelayCommand(() => Navigation?.OpenSettings());
        ChooseSaveCommand = new RelayCommand(() => Navigation?.ChooseSave());
        RetryCommand = new RelayCommand(() =>
        {
            if (_session is { } session && _lifetime is { } cts) _ = LoadAsync(session, cts.Token);
        });
    }

    public override string ModuleId => Id;

    // ---- partes reutilizáveis pela View (a chave de cada seletor está em JobsPageViewModel.Pickers.cs)

    public JobPickerViewModel FromCountry { get; } = new();

    public JobPickerViewModel FromCity { get; } = new();

    public JobPickerViewModel FromCompany { get; } = new();

    public JobPickerViewModel Slot { get; } = new();

    public JobPickerViewModel ToCountry { get; } = new();

    public JobPickerViewModel ToCity { get; } = new();

    public JobPickerViewModel ToCompany { get; } = new();

    public JobPickerViewModel Cargo { get; } = new();

    public RelayCommand SwapCommand { get; }

    public RelayCommand AddCommand { get; }

    public RelayCommand CancelEditCommand { get; }

    public RelayCommand DismissNoticeCommand { get; }

    public RelayCommand DismissErrorCommand { get; }

    public RelayCommand DismissCloudCommand { get; }

    public RelayCommand OpenSettingsCommand { get; }

    public RelayCommand ChooseSaveCommand { get; }

    public RelayCommand RetryCommand { get; }

    /// <summary>Pede à View que mova o foco (campo incompleto seguinte, sem abrir o popup).</summary>
    public event EventHandler<JobsFocusTarget>? FocusRequested;

    // ---- sessão

    protected override async Task OnSessionOpenedAsync(IEditSession session, CancellationToken ct)
    {
        ResetState();
        _session = session;
        session.Changed += OnSessionChanged;
        _lifetime = new CancellationTokenSource();
        ct.ThrowIfCancellationRequested();
        await LoadAsync(session, ct);
        if (State == PageState.Ready) FocusRequested?.Invoke(this, NextFocus()); // o primeiro campo vazio, sem abrir popup
    }

    private async Task LoadAsync(IEditSession session, CancellationToken ct)
    {
        State = PageState.Loading;
        Notify();
        try
        {
            await _factory.WarmUpAsync(session.Game, ct); // lê o cache de distâncias fora da UI antes do primeiro board
            if (!ReferenceEquals(_session, session)) return; // fechada enquanto esperava
            _board = _factory.Create(session); // índice em memória sobre o save já carregado; as distâncias do disco vêm depois, em segundo plano
            Refresh();
            StartDistanceRefresh(session.Game);
        }
        catch (Exception e) when (e is not (OperationCanceledException or OutOfMemoryException))
        {
            if (!ReferenceEquals(_session, session)) return;
            State = PageState.Error;
            Notify();
        }
    }

    protected override void OnSessionClosed()
    {
        if (_session is not null) _session.Changed -= OnSessionChanged;
        _lifetime?.Cancel();
        _lifetime?.Dispose();
        _lifetime = null;
        _session = null;
        _board = null;
        ResetState();
        State = PageState.Idle;
        Notify();
    }

    private void ResetState()
    {
        _staged.Clear();
        _sel = new JobSelection();
        _noticeCleared = _noticeAuto = [];
        _error = null;
        _hours = 72;
        _editing = null;
        _editingKey = null;
        _cloudDismissed = false;
        _pendingRows = [];
        _missing = [];
        _distance = null;
        _hasDepartureCompanies = true;
        foreach (var picker in Pickers) picker.Set([], null);
    }

    /// <summary>O Core avisa em qualquer thread (Stage, Discard, Apply, dados do jogo trocados): refaz o board na thread da UI.</summary>
    private void OnSessionChanged(object? sender, EventArgs e) => _ui.Post(Rebuild);

    private void Rebuild()
    {
        if (_session is not { } session) return;
        var afterAdd = _nextJobPending;
        _nextJobPending = false; // consumido aqui, antes de qualquer coisa que possa lançar
        _board = _factory.Create(session);
        if (afterAdd) _sel = NextAfterAdd(_board, _sel);
        Refresh(revalidate: !afterAdd);
    }

    /// <summary>Soma ao cache as distâncias dos saves do usuário, uma vez por jogo e fora da UI; depois refaz o board.</summary>
    private void StartDistanceRefresh(GameKind game)
    {
        if (_lifetime is not { } cts || !_refreshed.Add(game)) return;
        _ = RunAsync();

        async Task RunAsync()
        {
            try
            {
                var added = await _factory.RefreshDistancesAsync(game, cts.Token);
                if (added > 0) _ui.Post(Rebuild);
            }
            catch (OperationCanceledException)
            {
                _ui.Post(() => _refreshed.Remove(game)); // _refreshed só muda na thread da UI
            }
            catch (Exception e) when (e is not OutOfMemoryException)
            {
                // a tabela de distâncias é conforto: sem ela as distâncias seguem estimadas
            }
        }
    }

    protected override void OnCultureChanged()
    {
        if (_board is not null) Refresh();
        else base.OnCultureChanged();
    }

    // ---- ações do usuário

    private void Listen(JobPickerViewModel picker, JobField field) =>
        picker.Chosen += (_, option) => Choose(field, option.Key);

    /// <summary>Escolha do usuário num seletor: a seleção nova é revalidada pelo Core e tudo é repopulado a partir dela.</summary>
    private void Choose(JobField field, object key)
    {
        if (_refreshing || _board is null) return;
        Apply(field switch
        {
            JobField.FromCountry => _sel with { FromCountry = (string)key },
            JobField.FromCity => _sel with { FromCity = (string)key },
            JobField.FromCompany => _sel with { FromCompany = (CompanyLocation)key, Slot = null },
            JobField.Slot => _sel with { Slot = (JobSlotChoice)key },
            JobField.ToCountry => _sel with { ToCountry = (string)key },
            JobField.ToCity => _sel with { ToCity = (string)key },
            JobField.ToCompany => _sel with { ToCompany = (CompanyLocation)key },
            _ => _sel with { Cargo = (string)key },
        });
    }

    /// <summary>Revalida (o aviso só existe por ação do usuário), refaz a tela e leva o foco ao próximo campo incompleto.</summary>
    private void Apply(JobSelectionResult result, IEnumerable<JobField>? alsoCleared = null)
    {
        _error = null;
        _sel = result.Selection;
        _noticeAuto = result.AutoPicked;
        _noticeCleared = [.. (alsoCleared ?? []).Concat(result.Cleared).Distinct()];
        Refresh();
        FocusRequested?.Invoke(this, NextFocus());
    }

    private void Apply(JobSelection next) => Apply(JobSelectionRules.Revalidate(_board!, next, IsEditing));

    public void SelectExpiration(int hours)
    {
        if (_refreshing || hours == _hours || !JobPlanner.AllowedExpirationHours.Contains(hours)) return;
        _hours = hours;
        Notify();
    }

    private bool CanSwap => !IsEditing && _board is not null && _sel.FromCity is not null && _sel.ToCity is not null;

    /// <summary>Troca saída e chegada (país e cidade); as regras são direcionais, então empresas, vaga e carga são limpas.</summary>
    private void Swap()
    {
        if (!CanSwap) return;
        var swapped = JobSelectionRules.Swap(_board!, _sel);
        var result = JobSelectionRules.Revalidate(_board!, swapped.Selection);
        Apply(result, swapped.Cleared);
    }

    // ---- editar / descartar pendentes

    private void EditPending(string key)
    {
        if (_session is null || _board is null || !_staged.TryGetValue(key, out var staged)) return;
        if (_session.Pending.All(c => c.Key != key)) return;
        var r = staged.Request;
        _editing = staged;
        _editingKey = key;
        string? CountryFor(string city) => _board.HasCountries ? _board.CountryOf(city) ?? JobBoard.OtherCountry : null;
        _sel = new JobSelection(CountryFor(r.Origin.City), r.Origin.City, r.Origin,
            r.Mode == JobSlotMode.ReplaceOffer ? JobSlotChoice.Replace(r.ReplaceOfferId!) : JobSlotChoice.Free,
            CountryFor(r.Target.City), r.Target.City, r.Target, r.Cargo);
        _hours = r.ExpirationHours;
        _noticeCleared = _noticeAuto = [];
        _error = null;
        Refresh();
    }

    /// <summary>Cancelar a edição mantém saída e cidade de chegada; limpa vaga, empresa de chegada e carga.</summary>
    private void CancelEdit()
    {
        if (_editing is null || _board is null) return;
        _editing = null;
        _editingKey = null;
        _noticeCleared = _noticeAuto = [];
        _error = null;
        _sel = JobSelectionRules.NextJob(_board, _sel).Selection;
        Refresh();
    }

    private void DiscardPending(string key)
    {
        if (_session is not { } session) return;
        var change = session.Pending.FirstOrDefault(c => c.Key == key);
        if (change is null || !session.Discard(change.Id)) return;
        _toast.Show(TextRef.Of("Toast_Discarded"), TextRef.Of("Toast_Undo"), () => Undo(session, change));
    }

    /// <summary>Desfazer = enfileirar de novo o mesmo <c>ChangeSet</c>; se a vaga já foi tomada, avisa em vez de falhar calado.</summary>
    private void Undo(IEditSession session, ChangeSet change)
    {
        try
        {
            session.Stage(change);
        }
        catch (CoreException e)
        {
            _toast.Show(TextRef.Of($"Error_{e.Code}"));
        }
    }

    // ---- adicionar

    private void Add()
    {
        if (!CanAdd || _session is not { } session || _board is null) return;
        var request = JobSelectionRules.ToRequest(_sel, _hours, _editing?.Request.FillSlotId);
        if (request is null) return;
        ExistingOffer? offer = request.Mode == JobSlotMode.ReplaceOffer
            ? _board.ReplaceableOffers(request.Origin).FirstOrDefault(o => o.OfferId == request.ReplaceOfferId) ?? _editing?.Offer
            : null;
        string? key = null;
        var staged = false;
        _staged.TryGetValue(_editingKey ?? "", out var previous);
        try
        {
            // Plan lê o save BASE: reenfileirar a mesma Key (edição) não dá StalePatch.
            var change = _factory.Plan(session, _board, request);
            key = change.Key;
            var slot = change.Patches.OfType<SetAttribute>().Select(p => p.UnitId).FirstOrDefault();
            // guardado antes do Stage: o Changed do Core refaz a tela já com a pendência conhecida
            _staged[key] = new Staged(request.Mode == JobSlotMode.FillEmpty ? request with { FillSlotId = slot } : request, offer);
            _nextJobPending = true; // o refazer do board (Changed do Core, possivelmente adiado) aplica NextJob já com o board novo
            session.Stage(change);
            staged = true;
        }
        catch (CoreException e)
        {
            if (key is not null)
            {
                if (previous is not null && _editingKey == key) _staged[key] = previous;
                else _staged.Remove(key);
            }
            _error = ErrorText(e, request);
            Refresh();
            return;
        }
        finally
        {
            if (!staged) _nextJobPending = false; // nunca fica preso se o Stage falhar (CoreException ou não)
        }

        var wasEditing = _editing is not null;
        _editing = null;
        _editingKey = null;
        _noticeCleared = _noticeAuto = [];
        _error = null;
        _toast.Show(TextRef.Of(wasEditing ? "Jobs_Updated_Toast" : "Jobs_Added_Toast"),
            wasEditing ? null : TextRef.Of("Jobs_Added_View"), wasEditing ? null : () => Navigation?.OpenReview());
        Refresh(revalidate: false);
    }

    /// <summary>
    /// Fica a saída e a cidade de chegada; o resto recomeça. Se a empresa de saída esgotou (no board novo), o usuário escolhe
    /// a próxima. Um Rebuild seguinte (ex.: distâncias) ainda pode escolher sozinha a única empresa restante (opção única).
    /// </summary>
    private static JobSelection NextAfterAdd(JobBoard board, JobSelection current)
    {
        var next = JobSelectionRules.NextJob(board, current).Selection;
        return next.FromCompany == current.FromCompany ? next : next with { FromCompany = null, Slot = null };
    }

    /// <summary>Conflito do Stage vira a mensagem do passo certo; o resto usa o motivo do Core ou o texto do código.</summary>
    private string ErrorText(CoreException e, JobRequest request) => e.Code switch
    {
        CoreError.Conflict => Loc[request.Mode == JobSlotMode.ReplaceOffer ? JobsTextKeys.ErrorOfferMissing : JobsTextKeys.ErrorNoFreeSlot],
        CoreError.InvalidEdit when e.Reason is { } reason => Loc.Format(reason),
        _ => Loc.FormatError(e.Code),
    };
}
