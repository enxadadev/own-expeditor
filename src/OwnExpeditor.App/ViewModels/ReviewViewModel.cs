using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using OwnExpeditor.App.Controls;
using OwnExpeditor.App.Services;
using OwnExpeditor.App.Shell;
using OwnExpeditor.Core;
using OwnExpeditor.Core.Games;
using OwnExpeditor.Core.Session;
using OwnExpeditor.Core.Text;

namespace OwnExpeditor.App.ViewModels;

/// <summary>Um <c>ChangeSet</c> na revisão: título, linhas antes → depois e Descartar (alimenta o <c>DiffList</c>).</summary>
public sealed class ReviewChangeViewModel(string moduleName, string title, IReadOnlyList<IDiffLine> lines, System.Windows.Input.ICommand discard) : IDiffGroup
{
    public string ModuleName { get; } = moduleName;

    public string Title { get; } = title;

    public IReadOnlyList<IDiffLine> Lines { get; } = lines;

    public System.Windows.Input.ICommand? DiscardCommand { get; } = discard;
}

public sealed record ReviewModuleViewModel(string ModuleId, string Name, string CountText, string DiscardAutomation,
    IReadOnlyList<ReviewChangeViewModel> Changes, System.Windows.Input.ICommand DiscardModuleCommand);

/// <summary>
/// Página Revisar (shell): tudo o que mudou, por módulo, antes → depois; descartar item/módulo (com Desfazer) ou tudo
/// (com confirmação); Aplicar abre a confirmação. Nada é gravado aqui.
/// </summary>
public sealed partial class ReviewViewModel : LocalizedViewModel
{
    private readonly IDialogService _dialogs;
    private readonly IToastService _toast;
    private readonly IGameStatusMonitor _monitor;
    private readonly IShellLauncher _launcher;
    private readonly IUiDispatcher _ui;
    private readonly Dictionary<string, int> _order;
    private IEditSession? _session;
    private IReadOnlyList<ReviewModuleViewModel> _modules = [];
    private int _total;

    public ReviewViewModel(ILocalizer loc, IDialogService dialogs, IToastService toast, IGameStatusMonitor monitor,
        IShellLauncher launcher, IUiDispatcher ui, IEnumerable<ModuleDescriptor> descriptors) : base(loc)
    {
        _dialogs = dialogs;
        _toast = toast;
        _monitor = monitor;
        _launcher = launcher;
        _ui = ui;
        _order = descriptors.ToDictionary(d => d.Id, d => d.Order);
        ApplyCommand = new AsyncRelayCommand(RequestApplyAsync, () => HasChanges);
        DiscardAllCommand = new AsyncRelayCommand(DiscardAllAsync, () => HasChanges);
        BackCommand = new RelayCommand(() => BackRequested?.Invoke(this, EventArgs.Empty));
        GoToModuleCommand = new RelayCommand(() => GoToModuleRequested?.Invoke(this, EventArgs.Empty));
    }

    public AsyncRelayCommand ApplyCommand { get; }

    public AsyncRelayCommand DiscardAllCommand { get; }

    public RelayCommand BackCommand { get; }

    public RelayCommand GoToModuleCommand { get; }

    /// <summary>Terminou a confirmação (gravou, cancelou ou pediu para reabrir/escolher outro save).</summary>
    public event EventHandler<ApplyOutcome>? ApplyFinished;

    public event EventHandler? BackRequested;

    public event EventHandler? GoToModuleRequested;

    /// <summary>Título do primeiro módulo habilitado, para "Ir para {0}" (definido pelo shell).</summary>
    public string? FirstModuleTitle { get; set; }

    public IReadOnlyList<ReviewModuleViewModel> Modules => _modules;

    public int Total => _total;

    public bool HasChanges => _total > 0;

    public bool IsEmpty => _total == 0;

    public string Title => Loc["Review_Title"];

    public string SummaryLabel => Loc["Review_Summary"].ToUpper(Loc.Culture);

    public string TotalText => _total.ToString("N0", Loc.Culture);

    public string TotalValue => _total.ToString("N0", Loc.Culture);

    public string ModuleValue => _modules.Count.ToString("N0", Loc.Culture);

    public string ApplyHint => Loc[HasChanges ? "Review_Apply_Hint" : "Review_Apply_Disabled_Hint"];

    public string Subtitle => _total == 0 ? Loc["Review_Subtitle_None"]
        : Loc.Get("Review_Subtitle", Loc.GetPlural("Count_Changes", _total), Loc.GetPlural("Count_Modules", _modules.Count));

    public string GoToText => Loc.Get("Empty_Go", FirstModuleTitle ?? "");

    public bool CanGoToModule => FirstModuleTitle is not null;

    /// <summary>Liga a página à sessão atual (ou a nenhuma).</summary>
    public void Attach(IEditSession? session)
    {
        if (_session is not null) _session.Changed -= OnSessionChanged;
        _session = session;
        if (_session is not null) _session.Changed += OnSessionChanged;
        Rebuild();
    }

    private void OnSessionChanged(object? sender, EventArgs e) => _ui.Post(Rebuild);

    protected override void OnCultureChanged() => Rebuild();

    private void Rebuild()
    {
        var modules = new List<ReviewModuleViewModel>();
        if (_session is { } session)
            foreach (var module in session.GetDiff().Modules.OrderBy(m => _order.GetValueOrDefault(m.ModuleId, int.MaxValue)))
            {
                var name = Loc["Module_" + module.ModuleId];
                var changes = module.Changes.Select(c => new ReviewChangeViewModel(name, Loc.Format(c.Title),
                    c.Lines.Select(l => (IDiffLine)ToLine(l, session.Names)).ToList(),
                    new RelayCommand(() => DiscardItem(c)))).ToList();
                var moduleChanges = module.Changes;
                modules.Add(new ReviewModuleViewModel(module.ModuleId, name, Loc.GetPlural("Count_Changes", changes.Count),
                    Loc.Get("Review_DiscardModule_Aria", name), changes, new RelayCommand(() => DiscardModule(module.ModuleId, moduleChanges))));
            }
        _modules = modules;
        _total = modules.Sum(m => m.Changes.Count);
        ApplyCommand.NotifyCanExecuteChanged();
        DiscardAllCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(string.Empty);
    }

    private Controls.DiffLine ToLine(Core.Session.DiffLine line, IGameNames names) => new(Loc.Format(line.Label),
        FormatValue(line.Before, names), FormatValue(line.After, names), line.After is DiffValue.Number or DiffValue.Money,
        IconOf(line.Before, names), IconOf(line.After, names));

    /// <summary>Ícone (decoração) só para valores de reboque; os demais e o vazio não têm.</summary>
    private static string? IconOf(DiffValue value, IGameNames names) =>
        value is DiffValue.Trailer t ? TrailerIcons.Resolve(names.Trailer(t.Definition, t.Variant))?.Key : null;

    /// <summary>Valor vazio vira <c>null</c>: o controle mostra "—" e não risca.</summary>
    private string? FormatValue(DiffValue value, IGameNames names) => value is DiffValue.Empty ? null : Loc.Format(value, names);

    // ---- descartar, com Desfazer

    private void DiscardItem(ChangeSet change)
    {
        if (_session is not { } session || !session.Discard(change.Id)) return;
        _toast.Show(TextRef.Of("Toast_Discarded"), TextRef.Of("Toast_Undo"), () => Restage(session, [change]));
    }

    private void DiscardModule(string moduleId, IReadOnlyList<ChangeSet> changes)
    {
        if (_session is not { } session) return;
        session.DiscardModule(moduleId);
        _toast.Show(TextRef.Of(changes.Count == 1 ? "Toast_Discarded" : "Toast_DiscardedMany"), TextRef.Of("Toast_Undo"),
            () => Restage(session, changes));
    }

    /// <summary>Desfazer = enfileirar de novo (mesma <c>Key</c>). Conflito ou patch velho → toast de falha.</summary>
    private void Restage(IEditSession session, IReadOnlyList<ChangeSet> changes)
    {
        try
        {
            foreach (var change in changes) session.Stage(change);
        }
        catch (CoreException e)
        {
            _toast.Show(TextRef.Of($"Error_{e.Code}"));
        }
    }

    private async Task DiscardAllAsync()
    {
        if (_session is not { } session) return;
        var ok = await _dialogs.ConfirmAsync(TextRef.Of("Discard_All_Title"), TextRef.Of("Discard_All_Message"),
            TextRef.Of("Discard_All_Confirm"), destructive: true);
        if (ok) session.DiscardAll();
    }

    // ---- aplicar

    /// <summary>Abre a confirmação (Ctrl+Enter ou botão). Nunca grava sozinho.</summary>
    public async Task RequestApplyAsync()
    {
        if (_session is not { } session || !HasChanges) return;
        var dialog = new ConfirmApplyViewModel(Loc, session, _monitor, _launcher, _ui);
        await _dialogs.ShowAsync(dialog);
        ApplyFinished?.Invoke(this, dialog.Outcome);
    }
}
