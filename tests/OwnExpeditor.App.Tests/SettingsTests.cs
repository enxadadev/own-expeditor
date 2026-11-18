using OwnExpeditor.Core.Games;
using OwnExpeditor.App.Tests.Support;
using OwnExpeditor.App.ViewModels;
using OwnExpeditor.Domain.Games;
using OwnExpeditor.Domain.Settings;

namespace OwnExpeditor.App.Tests;

public class SettingsTests
{
    [Fact]
    public void Refresh_ReflectsSavedSettings()
    {
        var fx = new ShellFixture();
        fx.Settings.Current = new AppSettings(ThemePreference.Dark, Language.En, null, new GameFolders());

        fx.Shell.SettingsPage.Refresh();

        Assert.Equal(ThemePreference.Dark, fx.Shell.SettingsPage.SelectedTheme!.Value);
        Assert.Equal(Language.En, fx.Shell.SettingsPage.SelectedLanguage!.Value);
        Assert.Empty(fx.Settings.Updates); // sincronizar a tela não grava nada
        Assert.Empty(fx.Theme.Applied);
    }

    [Fact]
    public void SelectTheme_AppliesNow_AndPersists()
    {
        var fx = new ShellFixture();
        var page = fx.Shell.SettingsPage;

        page.SelectedTheme = page.ThemeOptions.First(o => (ThemePreference)o.Value == ThemePreference.Light);

        Assert.Equal([ThemePreference.Light], fx.Theme.Applied);
        Assert.Equal(ThemePreference.Light, fx.Settings.Current.Theme);
        Assert.True(page.ShowSaved);
        Assert.False(page.ShowFailed);
    }

    [Fact]
    public void SelectLanguage_SwitchesTheUiNow_AndPersists()
    {
        var fx = new ShellFixture(Language.PtBr);
        var page = fx.Shell.SettingsPage;

        page.SelectedLanguage = page.LanguageOptions.First(o => (Language)o.Value == Language.En);

        Assert.Equal(Language.En, fx.Loc.Language);
        Assert.Equal(Language.En, fx.Settings.Current.Language);
        Assert.Equal("Theme", page.ThemeOptions[0].Label == "Light" ? "Theme" : "?");
    }

    [Fact]
    public void LanguageLabels_AreEndonyms_InBothLanguages()
    {
        var fx = new ShellFixture(Language.PtBr);
        var page = fx.Shell.SettingsPage;

        Assert.Equal(["Português (Brasil)", "English"], page.LanguageOptions.Select(o => o.Label));
        fx.Loc.SetLanguage(Language.En);
        Assert.Equal(["Português (Brasil)", "English"], page.LanguageOptions.Select(o => o.Label));
    }

    [Fact]
    public void PersistFailure_ShowsFailure_ButKeepsTheChangeApplied()
    {
        var fx = new ShellFixture();
        fx.Settings.FailWith = new IOException("disco cheio");
        var page = fx.Shell.SettingsPage;

        page.SelectedTheme = page.ThemeOptions.First(o => (ThemePreference)o.Value == ThemePreference.Dark);

        Assert.Equal([ThemePreference.Dark], fx.Theme.Applied);
        Assert.True(page.ShowFailed);
        Assert.False(page.ShowSaved);
    }

    [Fact]
    public void SavedStatus_Disappears_After3Seconds()
    {
        var fx = new ShellFixture();
        var page = fx.Shell.SettingsPage;
        page.SelectedTheme = page.ThemeOptions.First(o => (ThemePreference)o.Value == ThemePreference.Dark);
        Assert.True(page.ShowSaved);

        fx.Time.Advance(TimeSpan.FromSeconds(4));

        Assert.False(page.ShowSaved);
    }

    [Fact]
    public void Persisting_KeepsTheOtherSettings()
    {
        var fx = new ShellFixture();
        fx.Settings.Current = new AppSettings(ThemePreference.System, Language.En, "/x/game.sii", new GameFolders("/g"));
        var page = fx.Shell.SettingsPage;
        page.Refresh();

        page.SelectedTheme = page.ThemeOptions.First(o => (ThemePreference)o.Value == ThemePreference.Dark);

        Assert.Equal("/x/game.sii", fx.Settings.Current.LastSavePath);
        Assert.Equal("/g", fx.Settings.Current.GameFolders.Ets2);
    }

    // ---- pasta do jogo

    [Fact]
    public void Folders_ShowDetectedAndNotFound_PerGame()
    {
        var fx = new ShellFixture();
        fx.Shell.SettingsPage.Refresh();

        var ets2 = fx.Shell.SettingsPage.Folders.Single(f => f.Game == GameKind.Ets2);
        var ats = fx.Shell.SettingsPage.Folders.Single(f => f.Game == GameKind.Ats);

        Assert.Equal("Detectada automaticamente", ets2.StatusText);
        Assert.True(ets2.IsFound);
        Assert.True(ets2.CanOpen);
        Assert.False(ets2.IsManual);
        Assert.Equal("Não encontrada", ats.StatusText);
        Assert.False(ats.IsFound);
        Assert.False(ats.CanOpen);
        Assert.Contains("American Truck Simulator", ats.NotFoundHelp);
    }

    [Fact]
    public async Task ChooseFolder_Valid_IsAccepted_AndBecomesManual()
    {
        var fx = new ShellFixture();
        fx.Shell.SettingsPage.Refresh();
        var ats = fx.Shell.SettingsPage.Folders.Single(f => f.Game == GameKind.Ats);
        fx.Picker.Result = "/games/ats";

        await ats.ChooseCommand.ExecuteAsync(null);

        Assert.Equal([(GameKind.Ats, "/games/ats")], fx.GameData.Sets);
        Assert.True(ats.IsManual);
        Assert.Equal("Escolhida por você", ats.StatusText);
        Assert.False(ats.ShowInvalid);
    }

    [Theory]
    [InlineData(GameFolderCheck.NotAGameFolder)]
    [InlineData(GameFolderCheck.WrongGame)]
    [InlineData(GameFolderCheck.NotFound)]
    public async Task ChooseFolder_Invalid_IsRefused_KeepsThePreviousOne_AndShowsError(GameFolderCheck check)
    {
        var fx = new ShellFixture();
        fx.Shell.SettingsPage.Refresh();
        var ets2 = fx.Shell.SettingsPage.Folders.Single(f => f.Game == GameKind.Ets2);
        fx.Picker.Result = "/qualquer/coisa";
        fx.GameData.NextCheck = check;

        await ets2.ChooseCommand.ExecuteAsync(null);

        Assert.True(ets2.ShowInvalid);
        Assert.Equal("/games/ets2", ets2.Path);
        Assert.Equal("Detectada automaticamente", ets2.StatusText);
        Assert.Contains("Euro Truck Simulator 2", ets2.InvalidText);
    }

    [Fact]
    public async Task ChooseFolder_Cancelled_ChangesNothing()
    {
        var fx = new ShellFixture();
        fx.Shell.SettingsPage.Refresh();
        var ets2 = fx.Shell.SettingsPage.Folders.Single(f => f.Game == GameKind.Ets2);
        fx.Picker.Result = null;

        await ets2.ChooseCommand.ExecuteAsync(null);

        Assert.Empty(fx.GameData.Sets);
        Assert.False(ets2.ShowInvalid);
    }

    [Fact]
    public async Task ChooseFolder_AfterAnInvalidTry_ClearsTheError_WhenValid()
    {
        var fx = new ShellFixture();
        fx.Shell.SettingsPage.Refresh();
        var ets2 = fx.Shell.SettingsPage.Folders.Single(f => f.Game == GameKind.Ets2);
        fx.Picker.Result = "/ruim";
        fx.GameData.NextCheck = GameFolderCheck.NotAGameFolder;
        await ets2.ChooseCommand.ExecuteAsync(null);
        fx.GameData.NextCheck = GameFolderCheck.Ok;
        fx.Picker.Result = "/bom";

        await ets2.ChooseCommand.ExecuteAsync(null);

        Assert.False(ets2.ShowInvalid);
        Assert.Equal("/bom", ets2.Path);
    }

    [Fact]
    public async Task ResetToAutomaticDetection_ClearsTheManualPath()
    {
        var fx = new ShellFixture();
        fx.GameData.Folders[GameKind.Ets2] = new(GameKind.Ets2, "/manual", GameFolderSource.Manual);
        fx.Shell.SettingsPage.Refresh();
        var ets2 = fx.Shell.SettingsPage.Folders.Single(f => f.Game == GameKind.Ets2);
        Assert.True(ets2.IsManual);

        await ets2.ResetCommand.ExecuteAsync(null);

        Assert.Equal([(GameKind.Ets2, (string?)null)], fx.GameData.Sets);
        Assert.False(ets2.IsManual);
    }

    [Fact]
    public async Task OpenFolder_OpensTheInstallFolder()
    {
        var fx = new ShellFixture();
        fx.Shell.SettingsPage.Refresh();
        var ets2 = fx.Shell.SettingsPage.Folders.Single(f => f.Game == GameKind.Ets2);

        await ets2.OpenFolderCommand.ExecuteAsync(null);

        Assert.Equal(["/games/ets2"], fx.Launcher.Folders);
    }
}
