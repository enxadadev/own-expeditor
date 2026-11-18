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
/// Módulo Fretes (design/fretes): cidade de saída → cidade de destino → empresa de saída (+ onde entra) → carga → empresa de
/// chegada. As opções vêm do <see cref="JobBoard"/> (sobre <c>session.View</c>, que já desconta as pendências); o frete vira
/// <see cref="ChangeSet"/> por <see cref="JobPlanner"/> a partir de <c>session.Original</c> e só entra na sessão com
/// <c>Stage</c>. Nada é gravado aqui. Regra de negócio fica no Core; aqui só o fluxo de telas.
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

    // escolhas
    private string? _fromCity;
    private string? _toCity;
    private CompanyLocation? _fromCompany;
    private JobSlotMode _mode = JobSlotMode.FillEmpty;
    private string? _offerId;
    private string? _cargo;
    private CompanyLocation? _toCompany;
    private int _hours = 72;
    private Staged? _editing;
    private string? _editingKey;

    private string? _notice;
    private string? _error;
    private bool _cloudDismissed;

    public JobsPageViewModel(ILocalizer loc, IJobBoardFactory factory, IToastService toast, IUiDispatcher ui) : base(loc)
    {
        _factory = factory;
        _toast = toast;
        _ui = ui;

        ToCity = new JobPickerViewModel();
        FromCompany = new JobPickerViewModel();
        Cargo = new JobPickerViewModel();
        ToCompany = new JobPickerViewModel();
        Offer = new JobPickerViewModel();
        ToCity.Chosen += (_, o) => Act(() => _toCity = (string)o.Key, JobsFocusTarget.FromCompany);
        FromCompany.Chosen += (_, o) => Act(() => ChooseCompany((CompanyLocation)o.Key), JobsFocusTarget.Cargo);
        Cargo.Chosen += (_, o) => Act(() => _cargo = (string)o.Key, JobsFocusTarget.ToCompany);
        ToCompany.Chosen += (_, o) => Act(() => _toCompany = (CompanyLocation)o.Key, JobsFocusTarget.Add);
        Offer.Chosen += (_, o) => Act(() => _offerId = (string)o.Key, JobsFocusTarget.Cargo);

        SwapCommand = new RelayCommand(Swap, () => CanSwap);
        AddCommand = new RelayCommand(Add, () => CanAdd);
        CancelEditCommand = new RelayCommand(CancelEdit);
        DismissNoticeCommand = new RelayCommand(() => { _notice = null; Notify(); });
        DismissErrorCommand = new RelayCommand(() => { _error = null; Notify(); });
        DismissCloudCommand = new RelayCommand(() => { _cloudDismissed = true; Notify(); });
        OpenSettingsCommand = new RelayCommand(() => Navigation?.OpenSettings());
        ChooseSaveCommand = new RelayCommand(() => Navigation?.ChooseSave());
        ClearCitySearchCommand = new RelayCommand(() => CityQuery = "");
        ChangeToCityCommand = new RelayCommand(() =>
        {
            ToCity.ToggleChecked = true;
            ToCity.IsOpen = true;
        });
        ChangeFromCityCommand = new RelayCommand(() => FocusRequested?.Invoke(this, JobsFocusTarget.CityList));
    }

    public override string ModuleId => Id;

    // ---- partes reutilizáveis pela View

    public JobPickerViewModel ToCity { get; }

    public JobPickerViewModel FromCompany { get; }

    public JobPickerViewModel Cargo { get; }

    public JobPickerViewModel ToCompany { get; }

    public JobPickerViewModel Offer { get; }

    public RelayCommand SwapCommand { get; }

    public RelayCommand AddCommand { get; }

    public RelayCommand CancelEditCommand { get; }

    public RelayCommand DismissNoticeCommand { get; }

    public RelayCommand DismissErrorCommand { get; }

    public RelayCommand DismissCloudCommand { get; }

    public RelayCommand OpenSettingsCommand { get; }

    public RelayCommand ChooseSaveCommand { get; }

    public RelayCommand ClearCitySearchCommand { get; }

    public RelayCommand ChangeToCityCommand { get; }

    public RelayCommand ChangeFromCityCommand { get; }

    /// <summary>Pede à View que mova o foco (próximo passo incompleto, lista de cidades).</summary>
    public event EventHandler<JobsFocusTarget>? FocusRequested;

    // ---- sessão

    protected override async Task OnSessionOpenedAsync(IEditSession session, CancellationToken ct)
    {
        ResetState();
        _session = session;
        session.Changed += OnSessionChanged;
        State = PageState.Loading;
        Notify();
        _lifetime = new CancellationTokenSource();
        ct.ThrowIfCancellationRequested();
        await _factory.WarmUpAsync(session.Game, ct); // lê o cache de distâncias fora da UI antes do primeiro board
        if (!ReferenceEquals(_session, session)) return; // fechada enquanto esperava
        _board = _factory.Create(session); // índice em memória sobre o save já carregado; as distâncias do disco vêm depois, em segundo plano
        Refresh();
        StartDistanceRefresh(session.Game);
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
        _fromCity = _toCity = _cargo = _offerId = _notice = _error = null;
        _fromCompany = _toCompany = null;
        _mode = JobSlotMode.FillEmpty;
        _hours = 72;
        _editing = null;
        _editingKey = null;
        _cloudDismissed = false;
        _citySelection = null;
        _cityQuery = "";
        _cityItems = _allCityItems = [];
        _originCityIds = _allCityIds = _usableCityIds = [];
        _pendingRows = [];
        _companies = _toCompanies = [];
        _cargos = [];
        _offers = [];
        _distance = null;
        _hasMatch = _hasUsableCompanies = true;
        foreach (var picker in new[] { ToCity, FromCompany, Cargo, ToCompany, Offer }) picker.Set([], null);
    }

    /// <summary>O Core avisa em qualquer thread (Stage, Discard, Apply, dados do jogo trocados): refaz o board na thread da UI.</summary>
    private void OnSessionChanged(object? sender, EventArgs e) => _ui.Post(Rebuild);

    private void Rebuild()
    {
        if (_session is not { } session) return;
        _board = _factory.Create(session);
        Refresh();
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

    private void Act(Action change, JobsFocusTarget next, bool moveFocus = true)
    {
        if (_refreshing || _board is null) return;
        _notice = null;
        _error = null;
        change();
        Refresh();
        if (moveFocus) FocusRequested?.Invoke(this, NextIncomplete() ?? next);
    }

    private void ChooseCompany(CompanyLocation company)
    {
        _fromCompany = company;
        _mode = JobSlotMode.FillEmpty;
        _offerId = null;
        NormalizeMode(forceDefault: true, [], []);
    }

    /// <summary>
    /// Escolha na lista de cidades de saída (passo 1). O foco fica na lista: as setas já escolhem, e mover o foco dali no meio
    /// da navegação por teclado a quebraria (o Tab segue para o campo do passo 2).
    /// </summary>
    public void SelectFromCity(string? city)
    {
        if (_refreshing || IsEditing || city is null || city == _fromCity) return;
        Act(() => _fromCity = city, JobsFocusTarget.ToCity, moveFocus: false);
    }

    public void SelectMode(JobSlotMode mode)
    {
        if (_refreshing || IsEditing || _fromCompany is null || mode == _mode) return;
        Act(() =>
        {
            _mode = mode;
            _offerId = null;
        }, mode == JobSlotMode.ReplaceOffer ? JobsFocusTarget.Offer : JobsFocusTarget.Cargo);
    }

    public void SelectExpiration(int hours)
    {
        if (_refreshing || hours == _hours || !JobPlanner.AllowedExpirationHours.Contains(hours)) return;
        _hours = hours;
        Notify();
    }

    private bool CanSwap => !IsEditing && _fromCity is not null && _toCity is not null && IsOriginCity(_toCity);

    /// <summary>Troca saída e destino; as regras são direcionais, então empresa, modo, carga e chegada são limpos.</summary>
    private void Swap()
    {
        if (!CanSwap) return;
        Act(() =>
        {
            (_fromCity, _toCity) = (_toCity, _fromCity);
            _fromCompany = _toCompany = null;
            _cargo = _offerId = null;
            _mode = JobSlotMode.FillEmpty;
        }, JobsFocusTarget.FromCompany);
    }

    // ---- editar / descartar pendentes

    private void EditPending(string key)
    {
        if (_session is null || _board is null || !_staged.TryGetValue(key, out var staged)) return;
        if (_session.Pending.All(c => c.Key != key)) return;
        var r = staged.Request;
        _editing = staged;
        _editingKey = key;
        _fromCity = r.Origin.City;
        _toCity = r.Target.City;
        _fromCompany = r.Origin;
        _mode = r.Mode;
        _offerId = r.ReplaceOfferId;
        _cargo = r.Cargo;
        _toCompany = r.Target;
        _hours = r.ExpirationHours;
        _notice = _error = null;
        Refresh();
        FocusRequested?.Invoke(this, JobsFocusTarget.ToCity);
    }

    private void CancelEdit()
    {
        if (_editing is null) return;
        _editing = null;
        _editingKey = null;
        ClearFormKeepingCities(keepTo: false);
        _notice = _error = null;
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
        var request = BuildRequest();
        ExistingOffer? offer = _mode == JobSlotMode.ReplaceOffer
            ? _board.ReplaceableOffers(request.Origin).FirstOrDefault(o => o.OfferId == _offerId) ?? _editing?.Offer
            : null;
        string? key = null;
        _staged.TryGetValue(_editingKey ?? "", out var previous);
        try
        {
            // Plan lê o save BASE: reenfileirar a mesma Key (edição) não dá StalePatch.
            var change = _factory.Plan(session, _board, request);
            key = change.Key;
            var slot = change.Patches.OfType<SetAttribute>().Select(p => p.UnitId).FirstOrDefault();
            // guardado antes do Stage: o Changed do Core refaz a tela já com a pendência conhecida
            _staged[key] = new Staged(request.Mode == JobSlotMode.FillEmpty ? request with { FillSlotId = slot } : request, offer);
            session.Stage(change);
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

        var wasEditing = _editing is not null;
        _editing = null;
        _editingKey = null;
        ClearFormKeepingCities(keepTo: true);
        _notice = _error = null;
        _toast.Show(TextRef.Of(wasEditing ? "Jobs_Updated_Toast" : "Jobs_Added_Toast"),
            wasEditing ? null : TextRef.Of("Jobs_Added_View"), wasEditing ? null : () => Navigation?.OpenReview());
        Refresh();
    }

    private void ClearFormKeepingCities(bool keepTo)
    {
        if (!keepTo) _toCity = null;
        _fromCompany = _toCompany = null;
        _cargo = _offerId = null;
        _mode = JobSlotMode.FillEmpty;
    }

    private JobRequest BuildRequest() => new(_fromCompany!, _toCompany!, _cargo!, _mode,
        _mode == JobSlotMode.ReplaceOffer ? _offerId : null, 0, _hours,
        _mode == JobSlotMode.FillEmpty ? _editing?.Request.FillSlotId : null);

    /// <summary>Conflito do Stage vira a mensagem do passo certo; o resto usa o motivo do Core ou o texto do código.</summary>
    private string ErrorText(CoreException e, JobRequest request) => e.Code switch
    {
        CoreError.Conflict => Loc[request.Mode == JobSlotMode.ReplaceOffer ? JobsTextKeys.ErrorOfferMissing : JobsTextKeys.ErrorNoFreeSlot],
        CoreError.InvalidEdit when e.Reason is { } reason => Loc.Format(reason),
        _ => Loc.FormatError(e.Code),
    };
}
