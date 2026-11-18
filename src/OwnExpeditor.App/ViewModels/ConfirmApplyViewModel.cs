using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OwnExpeditor.App.Services;
using OwnExpeditor.Core;
using OwnExpeditor.Core.Games;
using OwnExpeditor.Core.Session;

namespace OwnExpeditor.App.ViewModels;

public enum ApplyDestination { Overwrite, NewSlot }

public sealed record ConfirmModuleSummary(string Name, string CountText, string Titles);

public sealed record ErrorActionItem(string Label, RelayCommand Command, bool IsPrimary);

/// <summary>
/// Diálogo "Gravar mudanças no save?": o usuário escolhe o destino (sobrescrever, padrão, ou novo save com nome de
/// 1 a 40 caracteres), vê os avisos (jogo aberto, Steam Cloud) e grava. Gravando e erros trocam o conteúdo do mesmo
/// diálogo. Só o botão Gravar escreve: nenhum atalho confirma.
/// </summary>
public sealed partial class ConfirmApplyViewModel : DialogViewModel
{
    public const int MaxNameLength = 40;

    private readonly IEditSession _session;
    private readonly IGameStatusMonitor _monitor;
    private readonly IShellLauncher _launcher;
    private readonly IUiDispatcher _ui;
    private readonly IReadOnlyList<ModuleDiff> _modules;
    private readonly int _changeCount;
    private CoreException? _error;
    private CancellationTokenSource? _cts;

    public ConfirmApplyViewModel(ILocalizer loc, IEditSession session, IGameStatusMonitor monitor, IShellLauncher launcher,
        IUiDispatcher ui) : base(loc)
    {
        _session = session;
        _monitor = monitor;
        _launcher = launcher;
        _ui = ui;
        var diff = session.GetDiff();
        _modules = diff.Modules;
        _changeCount = diff.Modules.Sum(m => m.Changes.Count);
        _newName = Loc.Get("Confirm_NewName_Default", session.Save.DisplayName);
        _isGameRunning = monitor.IsRunning;
        monitor.RunningChanged += OnRunningChanged;
        ConfirmCommand = new AsyncRelayCommand(ApplyAsync, () => CanConfirm);
    }

    public AsyncRelayCommand ConfirmCommand { get; }

    public RelayCommand CancelCommand => _cancelCommand ??= new RelayCommand(Cancel, () => !IsApplying);

    private RelayCommand? _cancelCommand;

    /// <summary>Como o diálogo terminou (só vale depois de <c>WhenClosed</c>).</summary>
    public ApplyOutcome Outcome { get; private set; } = new(ApplyOutcomeKind.Cancelled);

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(IsOverwrite), nameof(IsNewSlot), nameof(ConfirmLabel),
        nameof(IntroText), nameof(CloudText), nameof(UndoNote), nameof(ApplyingHint), nameof(NameInvalid), nameof(CanConfirm))]
    private ApplyDestination _destination = ApplyDestination.Overwrite;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(NameValid), nameof(NameInvalid), nameof(CanConfirm))]
    private string _newName;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(WarnTitle), nameof(WarnBody))] private bool _isGameRunning;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(ShowQuestion), nameof(CanConfirm), nameof(CanCancelWithEscape))]
    private bool _isApplying;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(ShowQuestion), nameof(ErrorTitle), nameof(ErrorMessage),
        nameof(ErrorDetails), nameof(ErrorActions), nameof(PendingText))]
    private bool _hasError;

    public bool ShowQuestion => !IsApplying && !HasError;

    public override bool CanCancelWithEscape => !IsApplying;

    // ---- pergunta

    public string Title => Loc["Confirm_Title"];

    public string TitleUpper => Title.ToUpper(Loc.Culture);

    public string ErrorTitleUpper => ErrorTitle.ToUpper(Loc.Culture);

    public string ProfileSaveText => $"{_session.Save.Profile.DisplayName} · {_session.Save.DisplayName}";

    public string IntroText => Destination == ApplyDestination.NewSlot
        ? _changeCount == 1 ? Loc.Get("Confirm_IntroNew_One", ProfileSaveText) : Loc.Get("Confirm_IntroNew_Other", _changeCount, ProfileSaveText)
        : _changeCount == 1 ? Loc.Get("Confirm_Intro_One", ProfileSaveText) : Loc.Get("Confirm_Intro_Other", _changeCount, ProfileSaveText);

    public string WarnTitle => Loc[IsGameRunning ? "Confirm_Warn_Title_Running" : "Confirm_Warn_Title"];

    public string WarnBody => Loc[IsGameRunning ? "Confirm_Warn_Body_Running" : "Confirm_Warn_Body"];

    public bool IsSteamCloud => _session.Save.Profile.IsSteamCloud;

    public string CloudTitle => Loc["Confirm_Cloud_Title"];

    public string CloudText => Loc[Destination == ApplyDestination.NewSlot ? "Confirm_Cloud_New" : "Confirm_Cloud_Overwrite"];

    public IReadOnlyList<ConfirmModuleSummary> Modules => _modules.Select(m => new ConfirmModuleSummary(
        Loc["Module_" + m.ModuleId], Loc.GetPlural("Count_Changes", m.Changes.Count),
        string.Join(" · ", m.Changes.Select(c => Loc.Format(c.Title))))).ToList();

    public bool IsOverwrite
    {
        get => Destination == ApplyDestination.Overwrite;
        set { if (value) Destination = ApplyDestination.Overwrite; }
    }

    public bool IsNewSlot
    {
        get => Destination == ApplyDestination.NewSlot;
        set { if (value) Destination = ApplyDestination.NewSlot; }
    }

    public string OverwriteHelp => Loc.Get("Confirm_Dest_Overwrite_Help", _session.Save.DisplayName);

    public string FolderPath => Path.GetDirectoryName(_session.Save.GameSiiPath) ?? "";

    public string BackupPattern => Loc["Confirm_Backup_Pattern"];

    public string NewSlotNote => Loc.Get("Confirm_NewSlot_Note", _session.Save.Profile.DisplayName);

    public string ProfileFolderPath => Path.GetDirectoryName(Path.GetDirectoryName(_session.Save.GameSiiPath) ?? "") ?? "";

    public bool NameValid => IsNameValid(NewName);

    public bool NameInvalid => IsNewSlot && !NameValid;

    public static bool IsNameValid(string? name) => name is not null && name.Trim().Length is >= 1 and <= MaxNameLength;

    public string UndoNote => Loc[IsNewSlot ? "Confirm_Undo_Note_New" : "Confirm_Undo_Note"];

    public string ConfirmLabel => Loc[IsNewSlot ? "Confirm_Confirm_New" : "Confirm_Confirm"];

    public bool CanConfirm => !IsApplying && (IsOverwrite || NameValid);

    public string ApplyingHint => Loc[IsNewSlot ? "Confirm_Applying_Hint_New" : "Confirm_Applying_Hint"];

    public ApplyTarget Target => IsNewSlot ? new ApplyTarget.NewSlot(NewName.Trim()) : new ApplyTarget.Overwrite();

    // ---- erro

    public string ErrorTitle => Loc["Error_Title_Apply"];

    public string ErrorMessage => _error is null ? "" :
        _error is { Code: CoreError.InvalidEdit, Reason: { } reason } ? Loc.Format(reason) : Loc.FormatError(_error.Code);

    public string ErrorDetails => _error is null ? "" : $"{_error.Code}: {_error.Message}";

    public string PendingText => _error is null ? "" : ApplyErrorMap.For(_error.Code).Pending switch
    {
        PendingNote.Kept => Loc["Error_PendingKept"],
        PendingNote.Lost => Loc["Error_PendingLost"],
        _ => "",
    };

    public IReadOnlyList<ErrorActionItem> ErrorActions
    {
        get
        {
            if (_error is null) return [];
            var plan = ApplyErrorMap.For(_error.Code);
            var items = new List<ErrorActionItem> { MakeAction(plan.Primary, true) };
            if (plan.Secondary is { } secondary) items.Add(MakeAction(secondary, false));
            return items;
        }
    }

    private ErrorActionItem MakeAction(ErrorAction action, bool primary) => new(Loc[action switch
    {
        ErrorAction.Retry => "Action_Retry",
        ErrorAction.RetryGameClosed => "Action_RetryGameClosed",
        ErrorAction.BackToReview => "Action_BackToReview",
        ErrorAction.OpenReview => "Action_OpenReview",
        ErrorAction.ReopenSave => "Action_ReopenSave",
        ErrorAction.ChooseSave => "Action_ChooseSave",
        ErrorAction.OpenFolder => "Action_OpenFolder",
        _ => "Common_Close",
    }], new RelayCommand(() => Run(action)), primary);

    private void Run(ErrorAction action)
    {
        switch (action)
        {
            case ErrorAction.Retry or ErrorAction.RetryGameClosed:
                _ = ApplyAsync();
                break;
            case ErrorAction.ReopenSave:
                Finish(new ApplyOutcome(ApplyOutcomeKind.ReopenSave));
                break;
            case ErrorAction.ChooseSave:
                Finish(new ApplyOutcome(ApplyOutcomeKind.ChooseSave));
                break;
            case ErrorAction.OpenFolder:
                _ = _launcher.OpenFolderAsync(FolderPath);
                break;
            default:
                Finish(new ApplyOutcome(ApplyOutcomeKind.Cancelled));
                break;
        }
    }

    // ---- ações

    partial void OnDestinationChanged(ApplyDestination value) => ConfirmCommand.NotifyCanExecuteChanged();

    partial void OnNewNameChanged(string value) => ConfirmCommand.NotifyCanExecuteChanged();

    partial void OnIsApplyingChanged(bool value)
    {
        ConfirmCommand.NotifyCanExecuteChanged();
        CancelCommand.NotifyCanExecuteChanged();
    }

    public override void Cancel()
    {
        if (IsApplying) return;
        Finish(new ApplyOutcome(ApplyOutcomeKind.Cancelled));
    }

    private async Task ApplyAsync()
    {
        if (!CanConfirm && !HasError) return;
        var target = Target;
        _error = null;
        HasError = false;
        IsApplying = true;
        _cts = new CancellationTokenSource();
        try
        {
            var result = await _session.ApplyAsync(target, _cts.Token);
            Finish(new ApplyOutcome(ApplyOutcomeKind.Applied, result, target));
        }
        catch (CoreException e)
        {
            Fail(e);
        }
        catch (OperationCanceledException)
        {
            Fail(new CoreException(CoreError.WriteFailed, "Gravação cancelada."));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Fail(new CoreException(CoreError.WriteFailed, e.Message, e));
        }
    }

    private void Fail(CoreException e)
    {
        _error = e;
        IsApplying = false;
        HasError = true;
        OnPropertyChanged(nameof(ErrorTitle));
        OnPropertyChanged(nameof(ErrorMessage));
        OnPropertyChanged(nameof(ErrorDetails));
        OnPropertyChanged(nameof(ErrorActions));
        OnPropertyChanged(nameof(PendingText));
    }

    private void Finish(ApplyOutcome outcome)
    {
        Outcome = outcome;
        Close();
    }

    private void OnRunningChanged(object? sender, bool running) => _ui.Post(() => IsGameRunning = running);

    protected override void OnDisposed()
    {
        _monitor.RunningChanged -= OnRunningChanged;
        _cts?.Dispose();
    }
}
