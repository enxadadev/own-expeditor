using OwnExpeditor.App.Tests.Support;
using OwnExpeditor.App.ViewModels;
using OwnExpeditor.Core;
using OwnExpeditor.Core.Text;

namespace OwnExpeditor.App.Tests;

/// <summary>Regressão: depois de sair da seleção de save não havia como voltar a ela (sem botão Início).</summary>
public class ShellHomeTests
{
    [Fact]
    public void Home_FromSettingsWithoutSession_ReturnsToSaveSelection()
    {
        var fx = new ShellFixture();
        fx.Shell.OpenSettingsCommand.Execute(null);

        Assert.True(fx.Shell.HomeCommand.CanExecute(null));
        fx.Shell.HomeCommand.Execute(null);

        Assert.Equal(ShellPage.Welcome, fx.Shell.Page);
        Assert.Same(fx.Shell.Welcome, fx.Shell.CurrentPage);
        Assert.False(fx.Shell.SettingsNav.IsActive);
    }

    [Fact]
    public async Task Home_FromModuleWithSession_ReturnsToSaveSelection_KeepingSessionAndPending()
    {
        var fx = new ShellFixture();
        var session = await fx.OpenAsync(changes: 2);
        fx.Shell.NavigateToModule("fleet");

        fx.Shell.HomeCommand.Execute(null);

        Assert.Equal(ShellPage.Welcome, fx.Shell.Page);
        Assert.Same(fx.Shell.Welcome, fx.Shell.CurrentPage);
        Assert.All(fx.Shell.Modules, m => Assert.False(m.IsActive));
        Assert.Same(session, fx.Shell.Session);
        Assert.Equal(2, session.Pending.Count);
    }

    [Fact]
    public async Task Home_ThenModule_GoesBackToModule()
    {
        var fx = new ShellFixture();
        await fx.OpenAsync();
        fx.Shell.HomeCommand.Execute(null);

        fx.Shell.NavigateToModule("jobs");

        Assert.Equal(ShellPage.Module, fx.Shell.Page);
    }

    [Fact]
    public async Task Home_IsDisabledWhileLoading_AndEnabledAfterOpening()
    {
        var fx = new ShellFixture();
        var release = new TaskCompletionSource();
        fx.Host.Open = async (s, _, _) => { await release.Task; return Sample.Session(s); };

        var opening = fx.Shell.OpenSaveAsync(Sample.Slot());
        Assert.False(fx.Shell.HomeCommand.CanExecute(null));
        release.SetResult();
        await opening;

        Assert.Equal(ShellPage.Module, fx.Shell.Page);
        Assert.True(fx.Shell.HomeCommand.CanExecute(null));
    }

    [Fact]
    public async Task Home_FromOpenError_ClearsTheFailedSave()
    {
        var fx = new ShellFixture();
        fx.Host.Open = (_, _, _) => throw new CoreException(CoreError.SaveCorrupt, "x");
        await fx.Shell.OpenSaveAsync(Sample.Slot());
        Assert.True(fx.Shell.HomeCommand.CanExecute(null));

        fx.Shell.HomeCommand.Execute(null);

        Assert.Equal(ShellPage.Welcome, fx.Shell.Page);
        Assert.False(fx.Shell.SessionBar.HasSave);
        Assert.Null(fx.Shell.OpenError);
    }

    [Fact]
    public async Task Home_WithDialogOpen_IsDisabled_AndKeepsThePage()
    {
        var fx = new ShellFixture();
        await fx.OpenAsync();
        _ = fx.Shell.Dialogs.ConfirmAsync(TextRef.Of("Shell_Exit_Title"), TextRef.Of("Shell_Exit_Title"), TextRef.Of("Shell_Exit_Confirm"), destructive: false);

        Assert.False(fx.Shell.HomeCommand.CanExecute(null));
        fx.Shell.HomeCommand.Execute(null);

        Assert.Equal(ShellPage.Module, fx.Shell.Page);
    }

    [Fact]
    public void Home_OnSaveSelection_StaysThere()
    {
        var fx = new ShellFixture();

        fx.Shell.HomeCommand.Execute(null);

        Assert.Equal(ShellPage.Welcome, fx.Shell.Page);
        Assert.True(fx.Shell.IsHomeActive);
    }
}
