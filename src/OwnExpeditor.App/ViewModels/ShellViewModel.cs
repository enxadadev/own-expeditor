using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using OwnExpeditor.App.Services;
using OwnExpeditor.App.Shell;
using OwnExpeditor.Core;
using OwnExpeditor.Core.Games;
using OwnExpeditor.Core.Session;
using OwnExpeditor.Core.Settings;
using OwnExpeditor.Core.Text;
using OwnExpeditor.Domain.Saves;

namespace OwnExpeditor.App.ViewModels;

public enum ShellPage { Welcome, Loading, OpenError, Module, Review, Result, Settings }

/// <summary>
/// O shell: navegação (módulos, Revisar, Resultado, Configurações), abertura do save (Carregando → módulo ou erro),
/// barra de sessão, atalhos e o fluxo Revisar → Confirmar → Resultado. Só orquestra: regra de negócio fica no Core.
/// </summary>
public sealed partial class ShellViewModel : LocalizedViewModel
{
    private readonly IEditSessionHost _host;
    private readonly IAppSettingsService _settings;
    private readonly IThemeService _theme;
    private readonly IGameStatusMonitor _monitor;
    private readonly IUiDispatcher _ui;
    private readonly IShellLauncher _launcher;
    private readonly IClipboardService _clipboard;
    private readonly TimeProvider _time;
    private readonly Dictionary<string, ModulePageViewModel> _pages = [];
    private readonly List<ModuleDescriptor> _descriptors;
    private CancellationTokenSource? _openCts;
    private IEditSession? _session;
    private string? _lastModuleId;
    private (ShellPage Page, object? Vm, string? ModuleId)? _before;
    private bool _opening;

    public ShellViewModel(ILocalizer loc, IEditSessionHost host, IAppSettingsService settings, IThemeService theme,
        IGameStatusMonitor monitor, IUiDispatcher ui, IDialogService dialogs, ToastViewModel toast, IShellLauncher launcher,
        IClipboardService clipboard, WelcomeViewModel welcome, LoadingViewModel loading, SettingsViewModel settingsPage,
        ReviewViewModel review, SessionBarViewModel sessionBar, IEnumerable<ModuleDescriptor> modules,
        IServiceProvider services, TimeProvider time) : base(loc)
    {
        _host = host;
        _settings = settings;
        _theme = theme;
        _monitor = monitor;
        _ui = ui;
        _launcher = launcher;
        _clipboard = clipboard;
        _time = time;
        Dialogs = dialogs;
        Toast = toast;
        Welcome = welcome;
        Loading = loading;
        SettingsPage = settingsPage;
        Review = review;
        SessionBar = sessionBar;

        _descriptors = modules.OrderBy(m => m.Order).ToList();
        Modules = [.. _descriptors.Select(d => NavItemViewModel.For(loc, d))];
        foreach (var (d, item) in _descriptors.Zip(Modules))
        {
            _pages[d.Id] = d.CreatePage(services);
            var id = d.Id;
            item.Command = new RelayCommand(() => NavigateToModule(id), () => HasSession);
        }
        SettingsNav = new NavItemViewModel(loc, "settings", "Nav_Settings", "IconNavSettings", requiresSession: false, "Nav_Settings_Short")
        {
            Command = new RelayCommand(OpenSettings),
        };

        ReviewCommand = new RelayCommand(OpenReview, CanOpenReview);
        RequestApplyCommand = new AsyncRelayCommand(RequestApplyAsync, () => Dialog is null && Page == ShellPage.Review && Review.HasChanges);
        OpenSaveCommand = new RelayCommand(OpenSaveRequested);
        OpenSettingsCommand = new RelayCommand(OpenSettings);
        NavigateCommand = new RelayCommand<int>(NavigateByIndex);
        BackCommand = new RelayCommand(() => TryBack());

        _page = ShellPage.Welcome;
        _currentPage = welcome;
        SetActive(null);

        welcome.OpenRequested += (_, s) => _ = OpenSaveAsync(s);
        welcome.Picker.OpenRequested += (_, s) => _ = OpenSaveAsync(s);
        sessionBar.Picker.OpenRequested += (_, s) => { sessionBar.IsPickerOpen = false; _ = OpenSaveAsync(s); };
        sessionBar.ReviewRequested += (_, _) => OpenReview();
        review.BackRequested += (_, _) => BackToModule();
        review.GoToModuleRequested += (_, _) => BackToModule();
        review.ApplyFinished += (_, o) => _ = OnApplyFinishedAsync(o);
        dialogs.PropertyChanged += OnDialogsChanged;
        monitor.RunningChanged += (_, running) => _ui.Post(() => SessionBar.IsGameRunning = running);
        SessionBar.IsGameRunning = monitor.IsRunning;
        RefreshState();
    }

    public IDialogService Dialogs { get; }

    public ToastViewModel Toast { get; }

    public WelcomeViewModel Welcome { get; }

    public LoadingViewModel Loading { get; }

    public SettingsViewModel SettingsPage { get; }

    public ReviewViewModel Review { get; }

    public SessionBarViewModel SessionBar { get; }

    public IReadOnlyList<NavItemViewModel> Modules { get; }

    public NavItemViewModel SettingsNav { get; }

    public DialogViewModel? Dialog => Dialogs.Current;

    public RelayCommand ReviewCommand { get; }

    public AsyncRelayCommand RequestApplyCommand { get; }

    public RelayCommand OpenSaveCommand { get; }

    public RelayCommand OpenSettingsCommand { get; }

    /// <summary>Ctrl+1…5: módulo pelo número (1 = primeiro).</summary>
    public RelayCommand<int> NavigateCommand { get; }

    public RelayCommand BackCommand { get; }

    [ObservableProperty] private ShellPage _page;
    [ObservableProperty] private object? _currentPage;
    [ObservableProperty] private ResultViewModel? _result;
    [ObservableProperty] private OpenErrorViewModel? _openError;

    /// <summary>A tela inicial deve focar a lista de perfis (Ctrl+O sem sessão).</summary>
    public event EventHandler? FocusPickerRequested;

    public bool HasSession => _session is not null;

    public IEditSession? Session => _session;

    public int PendingCount => _session?.Pending.Count ?? 0;

    public string? ActiveModuleId { get; private set; }

    public bool ShowReviewHint => HasSession && Page != ShellPage.Loading;

    public bool ShowApplyHint => Page == ShellPage.Review && Review.HasChanges;

    public string SafetyNote => Loc["Shell_SafetyNote"];

    public string ModulesLabel => Loc["Nav_Label"];

    // ---- partida

    /// <summary>Lê as preferências, aplica tema e idioma salvos (primeira execução: idioma do sistema) e lista os perfis.</summary>
    public async Task InitializeAsync(CancellationToken ct = default)
    {
        try
        {
            await _settings.LoadAsync(ct);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            // settings.json ilegível: o app abre com os padrões
        }
        var current = _settings.Current;
        _theme.Apply(current.Theme);
        Loc.SetLanguage(current.Language);
        SettingsPage.Refresh();
        SessionBar.IsGameRunning = _monitor.IsRunning;
        await Welcome.ActivateAsync(ct);
    }

    // ---- navegação

    private void SetPage(ShellPage page, object? vm)
    {
        Page = page;
        CurrentPage = vm;
        RefreshState();
    }

    private void SetActive(string? moduleId)
    {
        ActiveModuleId = moduleId;
        foreach (var m in Modules) m.IsActive = Page == ShellPage.Module && m.Id == moduleId;
        SettingsNav.IsActive = Page == ShellPage.Settings;
        SessionBar.IsReviewActive = Page == ShellPage.Review;
    }

    private void RefreshState()
    {
        SetActive(Page == ShellPage.Module ? ActiveModuleId : null);
        foreach (var m in Modules) { m.IsEnabled = HasSession; (m.Command as RelayCommand)?.NotifyCanExecuteChanged(); }
        ReviewCommand.NotifyCanExecuteChanged();
        RequestApplyCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(HasSession));
        OnPropertyChanged(nameof(PendingCount));
        OnPropertyChanged(nameof(ShowReviewHint));
        OnPropertyChanged(nameof(ShowApplyHint));
    }

    public void ShowWelcome()
    {
        SetPage(ShellPage.Welcome, Welcome);
        _ = Welcome.ActivateAsync(CancellationToken.None);
    }

    public void NavigateToModule(string moduleId)
    {
        if (!HasSession || !_pages.TryGetValue(moduleId, out var page)) return;
        _lastModuleId = moduleId;
        ActiveModuleId = moduleId;
        SetPage(ShellPage.Module, page);
    }

    private void NavigateByIndex(int number)
    {
        if (number >= 1 && number <= Modules.Count) NavigateToModule(Modules[number - 1].Id);
    }

    private void BackToModule()
    {
        var id = _lastModuleId ?? Modules.FirstOrDefault()?.Id;
        if (id is not null) NavigateToModule(id);
    }

    private bool CanOpenReview() => Dialog is null && HasSession && Page != ShellPage.Review && PendingCount > 0;

    private void OpenReview()
    {
        if (!CanOpenReview()) return;
        SetPage(ShellPage.Review, Review);
    }

    private void OpenSettings()
    {
        if (Page == ShellPage.Settings) return;
        _before = (Page, CurrentPage, ActiveModuleId);
        SettingsPage.PendingCount = PendingCount;
        SettingsPage.Refresh();
        SetPage(ShellPage.Settings, SettingsPage);
    }

    private void OpenSaveRequested()
    {
        if (HasSession)
        {
            SessionBar.IsPickerOpen = true;
            return;
        }
        if (_opening) return;
        if (Page != ShellPage.Welcome) ShowWelcome();
        FocusPickerRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Esc: fecha o diálogo, o seletor, cancela a carga ou volta (Revisar, Resultado, Configurações).</summary>
    public bool TryBack()
    {
        if (Dialog is { } dialog)
        {
            if (dialog.CanCancelWithEscape) dialog.Cancel();
            return true;
        }
        if (SessionBar.IsPickerOpen)
        {
            SessionBar.IsPickerOpen = false;
            return true;
        }
        switch (Page)
        {
            case ShellPage.Loading:
                Loading.Cancel();
                return true;
            case ShellPage.Review or ShellPage.Result:
                BackToModule();
                return true;
            case ShellPage.Settings:
                RestoreBeforeSettings();
                return true;
            default:
                return false;
        }
    }

    private void RestoreBeforeSettings()
    {
        var (page, vm, moduleId) = _before ?? (HasSession ? ShellPage.Module : ShellPage.Welcome, null, null);
        _before = null;
        if (page is ShellPage.Module or ShellPage.Review or ShellPage.Result && !HasSession) page = ShellPage.Welcome;
        if (page == ShellPage.Welcome || vm is null)
        {
            if (HasSession) BackToModule(); else ShowWelcome();
            return;
        }
        ActiveModuleId = moduleId;
        SetPage(page, vm);
    }

    // ---- abrir save

    /// <summary>Abre o save: pede confirmação se há pendências, mostra Carregando e termina no módulo ou numa tela de erro.</summary>
    public async Task OpenSaveAsync(SaveSlotInfo save, bool confirmDiscard = true)
    {
        ArgumentNullException.ThrowIfNull(save);
        if (_opening) return;
        if (confirmDiscard && _session is { HasChanges: true } open)
        {
            var count = open.Pending.Count;
            var ok = await Dialogs.ConfirmAsync(TextRef.Of("Shell_SwitchSave_Title"),
                TextRef.Of("Shell_SwitchSave_Message", Loc.GetPlural("Count_Changes", count)),
                TextRef.Of("Shell_SwitchSave_Confirm"), destructive: true);
            if (!ok) return;
        }

        _opening = true;
        var cts = _openCts = new CancellationTokenSource();
        try
        {
            CloseSession();
            OpenError?.Dispose();
            OpenError = null;
            SessionBar.Save = save;
            SessionBar.HasError = false;
            SessionBar.IsPickerOpen = false;
            Loading.Begin(save, () => cts.Cancel());
            SetPage(ShellPage.Loading, Loading);

            var session = await _host.OpenAsync(save, new DispatchedProgress<LoadProgress>(_ui, Loading.Report), cts.Token);
            await AttachSessionAsync(session, cts.Token);
            BackToModule();
            _ = RememberLastSaveAsync(save);
        }
        catch (OperationCanceledException)
        {
            SessionBar.Save = null;
            ShowWelcome();
        }
        catch (CoreException e)
        {
            ShowOpenError(e, save);
        }
        catch (Exception e) when (e is not OutOfMemoryException)
        {
            ShowOpenError(new CoreException(CoreError.NoSession, e.Message, e), save);
        }
        finally
        {
            _opening = false;
            cts.Dispose();
            if (ReferenceEquals(_openCts, cts)) _openCts = null;
        }
    }

    private void ShowOpenError(CoreException error, SaveSlotInfo save)
    {
        SessionBar.Save = save;
        SessionBar.HasError = true;
        SessionBar.IsSessionOpen = false;
        OpenError = new OpenErrorViewModel(Loc, _clipboard, _launcher, error, save, AppInfo.Version, _time, _ui,
            retry: () => _ = OpenSaveAsync(save, confirmDiscard: false),
            chooseAnother: () =>
            {
                SessionBar.Save = null;
                SessionBar.HasError = false;
                ShowWelcome();
            });
        SetPage(ShellPage.OpenError, OpenError);
    }

    private async Task AttachSessionAsync(IEditSession session, CancellationToken ct)
    {
        _session = session;
        session.Changed += OnSessionChanged;
        Review.Attach(session);
        Review.FirstModuleTitle = Modules.FirstOrDefault()?.Title;
        SessionBar.Save = session.Save;
        SessionBar.HasError = false;
        SessionBar.IsSessionOpen = true;
        SessionBar.PendingCount = session.Pending.Count;
        foreach (var page in _pages.Values) await page.AttachAsync(session, ct);
        RefreshState();
    }

    private void CloseSession()
    {
        if (_session is { } session) session.Changed -= OnSessionChanged;
        foreach (var page in _pages.Values) page.Detach();
        _host.Close();
        _session = null;
        Review.Attach(null);
        SessionBar.IsSessionOpen = false;
        SessionBar.PendingCount = 0;
        RefreshState();
    }

    /// <summary>Fecha a sessão e volta a "Abra um save".</summary>
    public void CloseAndShowWelcome()
    {
        CloseSession();
        SessionBar.Save = null;
        SessionBar.HasError = false;
        ShowWelcome();
    }

    private void OnSessionChanged(object? sender, EventArgs e) => _ui.Post(() =>
    {
        if (_session is not { } session) return;
        SessionBar.Save = session.Save;
        SessionBar.PendingCount = session.Pending.Count;
        SettingsPage.PendingCount = session.Pending.Count;
        RefreshState();
    });

    private async Task RememberLastSaveAsync(SaveSlotInfo save)
    {
        try
        {
            await _settings.UpdateAsync(_settings.Current with { LastSavePath = save.GameSiiPath }, CancellationToken.None);
        }
        catch (Exception e) when (e is not OutOfMemoryException)
        {
            // lembrar o último save é um conforto: falhar não pode atrapalhar a abertura
        }
    }

    // ---- aplicar

    private Task RequestApplyAsync() => Review.RequestApplyAsync();

    private async Task OnApplyFinishedAsync(ApplyOutcome outcome)
    {
        switch (outcome.Kind)
        {
            case ApplyOutcomeKind.Applied when outcome is { Result: { } result, Target: { } target } && _session is { } session:
                Result?.Dispose();
                Result = new ResultViewModel(Loc, _launcher, _clipboard, Toast, result, target,
                    session.Save.Profile.IsSteamCloud, BackToModule);
                SetPage(ShellPage.Result, Result);
                break;
            case ApplyOutcomeKind.ReopenSave when _session is { } reopen:
                await OpenSaveAsync(reopen.Save, confirmDiscard: false);
                break;
            case ApplyOutcomeKind.ChooseSave:
                CloseAndShowWelcome();
                break;
        }
    }

    /// <summary>Fechar a janela: com pendências pede confirmação ("Sair sem aplicar?").</summary>
    public async Task<bool> ConfirmExitAsync()
    {
        if (_session is not { HasChanges: true } session) return true;
        return await Dialogs.ConfirmAsync(TextRef.Of("Shell_Exit_Title"),
            TextRef.Of("Shell_Exit_Message", Loc.GetPlural("Count_Changes", session.Pending.Count)),
            TextRef.Of("Shell_Exit_Confirm"), destructive: true);
    }

    private void OnDialogsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(IDialogService.Current)) return;
        OnPropertyChanged(nameof(Dialog));
        ReviewCommand.NotifyCanExecuteChanged();
        RequestApplyCommand.NotifyCanExecuteChanged();
    }
}
