using Avalonia.Input;
using OwnExpeditor.App.Shell;
using OwnExpeditor.App.Tests.Support;
using OwnExpeditor.App.ViewModels;
using OwnExpeditor.Core;
using OwnExpeditor.Core.Session;
using OwnExpeditor.Domain.Settings;

namespace OwnExpeditor.App.Tests;

public class ShellNavigationTests
{
    [Fact]
    public void New_Shell_StartsOnWelcome_WithModulesDisabled()
    {
        var fx = new ShellFixture();

        Assert.Equal(ShellPage.Welcome, fx.Shell.Page);
        Assert.False(fx.Shell.HasSession);
        Assert.Equal(5, fx.Shell.Modules.Count);
        Assert.All(fx.Shell.Modules, m => Assert.False(m.IsEnabled));
        Assert.True(fx.Shell.SettingsNav.IsEnabled);
    }

    [Fact]
    public void New_Shell_DoesNotOpenAnySaveOnItsOwn()
    {
        var fx = new ShellFixture();
        fx.Settings.Current = fx.Settings.Current with { LastSavePath = Sample.Slot().GameSiiPath };

        fx.Shell.InitializeAsync().GetAwaiter().GetResult();

        Assert.Empty(fx.Host.Opened);
        Assert.Equal(ShellPage.Welcome, fx.Shell.Page);
    }

    [Fact]
    public async Task Modules_AreOrderedByDescriptorOrder()
    {
        var fx = new ShellFixture();
        await fx.OpenAsync();

        Assert.Equal(["jobs", "fleet", "profile", "company", "map"], fx.Shell.Modules.Select(m => m.Id));
    }

    [Fact]
    public async Task OpenSave_Success_ShowsFirstModule_AndEnablesNavigation()
    {
        var fx = new ShellFixture();

        await fx.OpenAsync();

        Assert.Equal(ShellPage.Module, fx.Shell.Page);
        Assert.Equal("jobs", fx.Shell.ActiveModuleId);
        Assert.IsType<ModulePlaceholderViewModel>(fx.Shell.CurrentPage);
        Assert.True(fx.Shell.HasSession);
        Assert.All(fx.Shell.Modules, m => Assert.True(m.IsEnabled));
        Assert.True(fx.Shell.SessionBar.IsSessionOpen);
        Assert.Equal("Save 4", fx.Shell.SessionBar.SaveName);
    }

    [Fact]
    public async Task OpenSave_Success_RemembersLastSavePath()
    {
        var fx = new ShellFixture();

        await fx.OpenAsync();

        Assert.Equal(Sample.Slot().GameSiiPath, fx.Settings.Current.LastSavePath);
    }

    [Fact]
    public async Task OpenSave_WhileLoading_ShowsLoadingPage_WithProgress_AndModulesDisabled()
    {
        var fx = new ShellFixture();
        var release = new TaskCompletionSource();
        fx.Host.Open = async (s, progress, _) =>
        {
            progress!.Report(new LoadProgress(62, LoadStages.Parse, 90_000_000));
            await release.Task;
            return Sample.Session(s);
        };

        var opening = fx.Shell.OpenSaveAsync(Sample.Slot());

        Assert.Equal(ShellPage.Loading, fx.Shell.Page);
        Assert.Equal(0.62, fx.Shell.Loading.Progress);
        Assert.False(fx.Shell.HasSession);
        Assert.All(fx.Shell.Modules, m => Assert.False(m.IsEnabled));
        release.SetResult();
        await opening;
        Assert.Equal(ShellPage.Module, fx.Shell.Page);
    }

    [Fact]
    public async Task OpenSave_Cancelled_ReturnsToWelcome_WithoutSession()
    {
        var fx = new ShellFixture();
        fx.Host.Open = async (_, _, ct) =>
        {
            await Task.Delay(Timeout.Infinite, ct);
            return null!;
        };

        var opening = fx.Shell.OpenSaveAsync(Sample.Slot());
        fx.Shell.Loading.CancelCommand.Execute(null);
        await opening;

        Assert.Equal(ShellPage.Welcome, fx.Shell.Page);
        Assert.False(fx.Shell.HasSession);
        Assert.Null(fx.Shell.OpenError);
    }

    [Fact]
    public async Task OpenSave_WhileAlreadyOpening_IgnoresSecondRequest()
    {
        var fx = new ShellFixture();
        var release = new TaskCompletionSource();
        fx.Host.Open = async (s, _, _) =>
        {
            await release.Task;
            return Sample.Session(s);
        };

        var first = fx.Shell.OpenSaveAsync(Sample.Slot());
        await fx.Shell.OpenSaveAsync(Sample.Slot("2", "Save 2"));
        release.SetResult();
        await first;

        Assert.Single(fx.Host.Opened);
    }

    [Fact]
    public async Task OpenSave_WithPendingChanges_AsksBeforeDiscarding_AndKeepsSessionWhenRefused()
    {
        var fx = new ShellFixture();
        var session = await fx.OpenAsync(changes: 2);

        var opening = fx.Shell.OpenSaveAsync(Sample.Slot("2", "Save 2"));
        var dialog = Assert.IsType<ConfirmDialogViewModel>(fx.Dialogs.Current);
        dialog.CancelCommand.Execute(null);
        await opening;

        Assert.Single(fx.Host.Opened);
        Assert.Same(session, fx.Shell.Session);
        Assert.Equal(2, fx.Shell.PendingCount);
    }

    [Fact]
    public async Task OpenSave_WithPendingChanges_ConfirmDialogMentionsCount_AndOpensWhenConfirmed()
    {
        var fx = new ShellFixture();
        await fx.OpenAsync(changes: 2);

        var opening = fx.Shell.OpenSaveAsync(Sample.Slot("2", "Save 2"));
        var dialog = Assert.IsType<ConfirmDialogViewModel>(fx.Dialogs.Current);
        Assert.Contains("2 mudanças", dialog.Message);
        Assert.True(dialog.IsDestructive);
        dialog.ConfirmCommand.Execute(null);
        await opening;

        Assert.Equal(2, fx.Host.Opened.Count);
        Assert.Equal("Save 2", fx.Shell.SessionBar.SaveName);
    }

    [Fact]
    public async Task OpenSave_Failure_LeavesNoSession_AndClosesPreviousOne()
    {
        var fx = new ShellFixture();
        await fx.OpenAsync();
        fx.Host.Open = (_, _, _) => throw new CoreException(CoreError.SaveCorrupt, "x");

        await fx.Shell.OpenSaveAsync(Sample.Slot("2", "Save 2"));

        Assert.False(fx.Shell.HasSession);
        Assert.Null(fx.Host.Current);
        Assert.Equal(ShellPage.OpenError, fx.Shell.Page);
        Assert.All(fx.Shell.Modules, m => Assert.False(m.IsEnabled));
        Assert.True(fx.Shell.SessionBar.HasError);
        Assert.False(fx.Shell.SessionBar.ShowReview);
    }

    [Theory]
    [InlineData(CoreError.SaveParseFailed, OpenErrorKind.Parse)]
    [InlineData(CoreError.SaveCorrupt, OpenErrorKind.Corrupt)]
    [InlineData(CoreError.SaveNotFound, OpenErrorKind.NotFound)]
    [InlineData(CoreError.AccessDenied, OpenErrorKind.AccessDenied)]
    [InlineData(CoreError.SessionBusy, OpenErrorKind.Unknown)]
    [InlineData(CoreError.NoSession, OpenErrorKind.Unknown)]
    public async Task OpenSave_CoreError_MapsToTheRightErrorScreen(CoreError code, OpenErrorKind expected)
    {
        var fx = new ShellFixture();
        fx.Host.Open = (_, _, _) => throw new CoreException(code, "x");

        await fx.Shell.OpenSaveAsync(Sample.Slot());

        Assert.Equal(expected, fx.Shell.OpenError!.Kind);
        Assert.Same(fx.Shell.OpenError, fx.Shell.CurrentPage);
    }

    [Fact]
    public async Task OpenSave_UnexpectedException_ShowsGenericErrorScreen()
    {
        var fx = new ShellFixture();
        fx.Host.Open = (_, _, _) => throw new InvalidOperationException("boom");

        await fx.Shell.OpenSaveAsync(Sample.Slot());

        Assert.Equal(OpenErrorKind.Unknown, fx.Shell.OpenError!.Kind);
    }

    [Fact]
    public async Task OpenError_Retry_ReopensTheSameSave()
    {
        var fx = new ShellFixture();
        var calls = 0;
        fx.Host.Open = (s, _, _) => ++calls == 1 ? throw new CoreException(CoreError.SaveCorrupt, "x") : Task.FromResult<IEditSession>(Sample.Session(s));
        await fx.Shell.OpenSaveAsync(Sample.Slot());

        fx.Shell.OpenError!.RetryCommand.Execute(null);

        Assert.Equal(ShellPage.Module, fx.Shell.Page);
        Assert.Equal(2, fx.Host.Opened.Count);
        Assert.Equal(fx.Host.Opened[0], fx.Host.Opened[1]);
    }

    [Fact]
    public async Task OpenError_ChooseAnother_GoesToWelcome_WithoutSession()
    {
        var fx = new ShellFixture();
        fx.Host.Open = (_, _, _) => throw new CoreException(CoreError.SaveCorrupt, "x");
        await fx.Shell.OpenSaveAsync(Sample.Slot());

        fx.Shell.OpenError!.ChooseAnotherCommand.Execute(null);

        Assert.Equal(ShellPage.Welcome, fx.Shell.Page);
        Assert.False(fx.Shell.SessionBar.HasSave);
    }

    [Fact]
    public async Task Settings_Open_WorksWithoutSession_AndEscReturnsToWelcome()
    {
        var fx = new ShellFixture();

        fx.Shell.OpenSettingsCommand.Execute(null);
        Assert.Equal(ShellPage.Settings, fx.Shell.Page);
        Assert.True(fx.Shell.SettingsNav.IsActive);
        Assert.True(fx.Shell.TryBack());

        Assert.Equal(ShellPage.Welcome, fx.Shell.Page);
        await Task.CompletedTask;
    }

    [Fact]
    public async Task Settings_Open_KeepsSessionAndPending_AndEscReturnsToModule()
    {
        var fx = new ShellFixture();
        var session = await fx.OpenAsync(changes: 2);
        fx.Shell.NavigateToModule("fleet");

        fx.Shell.OpenSettingsCommand.Execute(null);
        Assert.Equal(ShellPage.Settings, fx.Shell.Page);
        Assert.True(fx.Shell.SettingsPage.ShowPendingNote);
        fx.Shell.TryBack();

        Assert.Equal(ShellPage.Module, fx.Shell.Page);
        Assert.Equal("fleet", fx.Shell.ActiveModuleId);
        Assert.Same(session, fx.Shell.Session);
        Assert.Equal(2, session.Pending.Count);
    }

    [Fact]
    public async Task NavigateToModule_WithoutSession_DoesNothing()
    {
        var fx = new ShellFixture();

        fx.Shell.NavigateToModule("jobs");

        Assert.Equal(ShellPage.Welcome, fx.Shell.Page);
        await Task.CompletedTask;
    }

    [Fact]
    public async Task Review_WithNoPending_CannotBeOpened()
    {
        var fx = new ShellFixture();
        await fx.OpenAsync(changes: 0);

        Assert.False(fx.Shell.ReviewCommand.CanExecute(null));
        Assert.False(fx.Shell.SessionBar.ReviewEnabled);
        Assert.Equal("Sem mudanças", fx.Shell.SessionBar.ReviewText);
    }

    [Fact]
    public async Task Review_WithPending_OpensReviewPage_AndMarksBadgeActive()
    {
        var fx = new ShellFixture();
        await fx.OpenAsync(changes: 3);

        fx.Shell.ReviewCommand.Execute(null);

        Assert.Equal(ShellPage.Review, fx.Shell.Page);
        Assert.True(fx.Shell.SessionBar.IsReviewActive);
        Assert.Equal("Revisar (3)", fx.Shell.SessionBar.ReviewText);
        Assert.All(fx.Shell.Modules, m => Assert.False(m.IsActive));
        Assert.False(fx.Shell.ReviewCommand.CanExecute(null));
    }

    [Fact]
    public async Task Review_Esc_ReturnsToLastModule()
    {
        var fx = new ShellFixture();
        await fx.OpenAsync(changes: 1);
        fx.Shell.NavigateToModule("company");
        fx.Shell.ReviewCommand.Execute(null);

        fx.Shell.TryBack();

        Assert.Equal(ShellPage.Module, fx.Shell.Page);
        Assert.Equal("company", fx.Shell.ActiveModuleId);
    }

    [Fact]
    public async Task SessionChanged_UpdatesPendingBadge()
    {
        var fx = new ShellFixture();
        var session = await fx.OpenAsync(changes: 0);

        session.Stage(Sample.Job("jobs:slot:a", "_nameless.a", "demo_co.city_b", "demo_co.city_x"));

        Assert.Equal(1, fx.Shell.SessionBar.PendingCount);
        Assert.True(fx.Shell.ReviewCommand.CanExecute(null));
    }

    [Fact]
    public async Task GameStatus_Change_UpdatesSessionBar()
    {
        var fx = new ShellFixture();
        await fx.OpenAsync();

        fx.Monitor.Set(true);

        Assert.True(fx.Shell.SessionBar.IsGameRunning);
        Assert.Equal("Jogo aberto", fx.Shell.SessionBar.GameStatusText);
    }

    [Fact]
    public async Task ConfirmExit_WithoutPending_ExitsWithoutAsking()
    {
        var fx = new ShellFixture();
        await fx.OpenAsync();

        Assert.True(await fx.Shell.ConfirmExitAsync());
        Assert.Null(fx.Dialogs.Current);
    }

    [Fact]
    public async Task ConfirmExit_WithPending_AsksAndHonorsAnswer()
    {
        var fx = new ShellFixture();
        await fx.OpenAsync(changes: 1);

        var asking = fx.Shell.ConfirmExitAsync();
        var dialog = Assert.IsType<ConfirmDialogViewModel>(fx.Dialogs.Current);
        dialog.CancelCommand.Execute(null);

        Assert.False(await asking);
    }

    // ---- inicialização

    [Fact]
    public async Task Initialize_AppliesSavedThemeAndLanguage()
    {
        var fx = new ShellFixture(Language.PtBr);
        fx.Settings.Current = new AppSettings(ThemePreference.Dark, Language.En, null, new GameFolders());

        await fx.Shell.InitializeAsync();

        Assert.Equal([ThemePreference.Dark], fx.Theme.Applied);
        Assert.Equal(Language.En, fx.Loc.Language);
        Assert.Equal(1, fx.Settings.Loads);
    }

    [Fact]
    public async Task Initialize_FirstRun_UsesLanguageDetectedByTheSettingsService()
    {
        var fx = new ShellFixture(Language.PtBr);
        // padrão do Core na primeira execução: tema Sistema e idioma do SO (aqui, en)
        fx.Settings.Current = new AppSettings(ThemePreference.System, Language.En, null, new GameFolders());

        await fx.Shell.InitializeAsync();

        Assert.Equal([ThemePreference.System], fx.Theme.Applied);
        Assert.Equal(Language.En, fx.Loc.Language);
    }

    [Fact]
    public async Task Initialize_ListsProfiles_AndOffersLastSave_WithoutOpeningIt()
    {
        var fx = new ShellFixture();
        fx.Settings.Current = fx.Settings.Current with { LastSavePath = Sample.Slot().GameSiiPath };

        await fx.Shell.InitializeAsync();

        Assert.Single(fx.Shell.Welcome.Picker.Profiles);
        Assert.Equal("Save 4", fx.Shell.Welcome.LastSave!.DisplayName);
        Assert.True(fx.Shell.Welcome.HasLastSave);
        Assert.Empty(fx.Host.Opened);
    }

    [Fact]
    public async Task Initialize_WithUnreadableSettings_StillStartsWithDefaults()
    {
        var fx = new ShellFixture();
        fx.Settings.LoadFailWith = new IOException("settings.json ilegível");

        await fx.Shell.InitializeAsync();

        Assert.Equal(ShellPage.Welcome, fx.Shell.Page);
        Assert.Equal([ThemePreference.System], fx.Theme.Applied);
    }

    // ---- atalhos

    [Fact]
    public async Task CtrlR_OpensReview_OnlyWithPending()
    {
        var fx = new ShellFixture();
        await fx.OpenAsync(changes: 0);
        Assert.False(ShellShortcuts.TryExecute(fx.Shell, Key.R, KeyModifiers.Control));
        Assert.Equal(ShellPage.Module, fx.Shell.Page);

        var fx2 = new ShellFixture();
        await fx2.OpenAsync(changes: 1);
        Assert.True(ShellShortcuts.TryExecute(fx2.Shell, Key.R, KeyModifiers.Control));
        Assert.Equal(ShellPage.Review, fx2.Shell.Page);
    }

    [Fact]
    public async Task CtrlEnter_OnReview_OpensConfirmation_AndNeverWrites()
    {
        var fx = new ShellFixture();
        var session = await fx.OpenAsync(changes: 2);
        fx.Shell.ReviewCommand.Execute(null);

        var handled = ShellShortcuts.TryExecute(fx.Shell, Key.Enter, KeyModifiers.Control);

        Assert.True(handled);
        Assert.IsType<ConfirmApplyViewModel>(fx.Dialogs.Current);
        Assert.Empty(session.Applied);
    }

    [Fact]
    public async Task CtrlEnter_OutsideReview_DoesNothing()
    {
        var fx = new ShellFixture();
        var session = await fx.OpenAsync(changes: 2);

        var handled = ShellShortcuts.TryExecute(fx.Shell, Key.Enter, KeyModifiers.Control);

        Assert.False(handled);
        Assert.Null(fx.Dialogs.Current);
        Assert.Empty(session.Applied);
    }

    [Fact]
    public async Task CtrlEnter_WhileConfirmationIsOpen_IsIgnored_SoItNeverConfirms()
    {
        var fx = new ShellFixture();
        var session = await fx.OpenAsync(changes: 2);
        fx.Shell.ReviewCommand.Execute(null);
        ShellShortcuts.TryExecute(fx.Shell, Key.Enter, KeyModifiers.Control);

        var handled = ShellShortcuts.TryExecute(fx.Shell, Key.Enter, KeyModifiers.Control);

        Assert.False(handled);
        Assert.Empty(session.Applied);
    }

    [Theory]
    [InlineData(Key.D1, "jobs")]
    [InlineData(Key.D3, "profile")]
    [InlineData(Key.D5, "map")]
    [InlineData(Key.NumPad2, "fleet")]
    public async Task CtrlNumber_NavigatesToModule_WithSession(Key key, string expected)
    {
        var fx = new ShellFixture();
        await fx.OpenAsync();

        Assert.True(ShellShortcuts.TryExecute(fx.Shell, key, KeyModifiers.Control));

        Assert.Equal(expected, fx.Shell.ActiveModuleId);
    }

    [Fact]
    public void CtrlNumber_WithoutSession_DoesNothing()
    {
        var fx = new ShellFixture();

        Assert.False(ShellShortcuts.TryExecute(fx.Shell, Key.D1, KeyModifiers.Control));
        Assert.Equal(ShellPage.Welcome, fx.Shell.Page);
    }

    [Fact]
    public void CtrlComma_OpensSettings_EvenWithoutSession()
    {
        var fx = new ShellFixture();

        Assert.True(ShellShortcuts.TryExecute(fx.Shell, Key.OemComma, KeyModifiers.Control));

        Assert.Equal(ShellPage.Settings, fx.Shell.Page);
    }

    [Fact]
    public void CtrlO_WithoutSession_RequestsFocusOnTheProfileList()
    {
        var fx = new ShellFixture();
        var focused = 0;
        fx.Shell.FocusPickerRequested += (_, _) => focused++;

        Assert.True(ShellShortcuts.TryExecute(fx.Shell, Key.O, KeyModifiers.Control));

        Assert.Equal(1, focused);
        Assert.False(fx.Shell.SessionBar.IsPickerOpen);
    }

    [Fact]
    public async Task CtrlO_WithSession_OpensTheSavePicker_AndEscClosesIt()
    {
        var fx = new ShellFixture();
        await fx.OpenAsync();

        ShellShortcuts.TryExecute(fx.Shell, Key.O, KeyModifiers.Control);
        Assert.True(fx.Shell.SessionBar.IsPickerOpen);

        Assert.True(ShellShortcuts.TryExecute(fx.Shell, Key.Escape, KeyModifiers.None));
        Assert.False(fx.Shell.SessionBar.IsPickerOpen);
    }

    [Fact]
    public async Task Esc_WithDialogOpen_CancelsTheDialogFirst()
    {
        var fx = new ShellFixture();
        await fx.OpenAsync(changes: 1);
        fx.Shell.ReviewCommand.Execute(null);
        ShellShortcuts.TryExecute(fx.Shell, Key.Enter, KeyModifiers.Control);
        Assert.NotNull(fx.Dialogs.Current);

        ShellShortcuts.TryExecute(fx.Shell, Key.Escape, KeyModifiers.None);

        Assert.Null(fx.Dialogs.Current);
        Assert.Equal(ShellPage.Review, fx.Shell.Page);
    }

    [Fact]
    public async Task Esc_WhileLoading_CancelsTheLoad()
    {
        var fx = new ShellFixture();
        fx.Host.Open = async (_, _, ct) =>
        {
            await Task.Delay(Timeout.Infinite, ct);
            return null!;
        };
        var opening = fx.Shell.OpenSaveAsync(Sample.Slot());

        Assert.True(ShellShortcuts.TryExecute(fx.Shell, Key.Escape, KeyModifiers.None));
        await opening;

        Assert.Equal(ShellPage.Welcome, fx.Shell.Page);
    }

    [Fact]
    public void Shortcuts_WithOtherModifiers_AreNotHandled()
    {
        var fx = new ShellFixture();

        Assert.False(ShellShortcuts.TryExecute(fx.Shell, Key.R, KeyModifiers.None));
        Assert.False(ShellShortcuts.TryExecute(fx.Shell, Key.S, KeyModifiers.Control)); // Ctrl+S continua livre
    }
}
