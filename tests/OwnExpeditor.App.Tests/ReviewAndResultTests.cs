using OwnExpeditor.App.Tests.Support;
using OwnExpeditor.App.ViewModels;
using OwnExpeditor.Core;
using OwnExpeditor.Core.Session;
using OwnExpeditor.Domain.Settings;

namespace OwnExpeditor.App.Tests;

public class ReviewTests
{
    [Fact]
    public async Task Review_GroupsChangesByModule_InDescriptorOrder()
    {
        var fx = new ShellFixture();
        await fx.OpenAsync(changes: 3);

        var review = fx.Shell.Review;

        Assert.Equal(3, review.Total);
        Assert.Equal(["jobs", "profile"], review.Modules.Select(m => m.ModuleId));
        Assert.Equal(2, review.Modules[0].Changes.Count);
        Assert.Equal("3 mudanças em 2 módulos", review.Subtitle);
    }

    [Fact]
    public async Task Review_Lines_FormatValuesByKind_AndEmptyBecomesNull()
    {
        var fx = new ShellFixture();
        var session = await fx.OpenAsync(changes: 0);
        session.Stage(Sample.Money(500));

        var line = fx.Shell.Review.Modules.Single().Changes.Single().Lines.Single();

        Assert.Equal("100 €", line.Before);
        Assert.Equal("500 €", line.After);
        Assert.True(line.IsNumeric);
    }

    [Fact]
    public async Task Review_FollowsSession_WhenChangesAreStagedOrDiscarded()
    {
        var fx = new ShellFixture();
        var session = await fx.OpenAsync(changes: 1);

        session.Stage(Sample.Money(9));
        Assert.Equal(2, fx.Shell.Review.Total);

        session.DiscardAll();
        Assert.True(fx.Shell.Review.IsEmpty);
        Assert.False(fx.Shell.Review.ApplyCommand.CanExecute(null));
        Assert.Equal("Nenhuma mudança pendente", fx.Shell.Review.Subtitle);
    }

    [Fact]
    public async Task DiscardItem_RemovesIt_AndOffersUndoInAToast()
    {
        var fx = new ShellFixture();
        var session = await fx.OpenAsync(changes: 2);
        var item = fx.Shell.Review.Modules[0].Changes[0];

        item.DiscardCommand!.Execute(null);

        Assert.Single(session.Pending);
        Assert.True(fx.Toast.IsVisible);
        Assert.Equal("Mudança descartada.", fx.Toast.Text);
        Assert.True(fx.Toast.HasAction);
        Assert.Equal("Desfazer", fx.Toast.ActionText);
    }

    [Fact]
    public async Task Undo_RestagesTheDiscardedChange()
    {
        var fx = new ShellFixture();
        var session = await fx.OpenAsync(changes: 2);
        fx.Shell.Review.Modules[0].Changes[0].DiscardCommand!.Execute(null);

        fx.Toast.RunActionCommand.Execute(null);

        Assert.Equal(2, session.Pending.Count);
        Assert.Equal(2, fx.Shell.Review.Total);
        Assert.False(fx.Toast.IsVisible);
    }

    [Fact]
    public async Task Undo_WhenItNowConflicts_ShowsFailureToast_AndKeepsTheOtherChange()
    {
        var fx = new ShellFixture();
        var session = await fx.OpenAsync(changes: 1);
        fx.Shell.Review.Modules[0].Changes[0].DiscardCommand!.Execute(null);
        // outra mudança passa a usar o mesmo atributo (mesma unidade), com Key diferente
        session.Stage(Sample.Job("jobs:other", "_nameless.a", "demo_co.city_b", "demo_co.city_z"));

        fx.Toast.RunActionCommand.Execute(null);

        Assert.Single(session.Pending);
        Assert.Equal("jobs:other", session.Pending[0].Key);
        Assert.Equal(fx.Loc.FormatError(CoreError.Conflict), fx.Toast.Text); // toast de falha, não o de desfazer
        Assert.False(fx.Toast.HasAction);
    }

    [Fact]
    public async Task DiscardModule_RemovesAllItsChanges_AndUndoRestoresThem()
    {
        var fx = new ShellFixture();
        var session = await fx.OpenAsync(changes: 3);

        fx.Shell.Review.Modules[0].DiscardModuleCommand.Execute(null);
        Assert.Single(session.Pending);
        Assert.Equal("Mudanças descartadas.", fx.Toast.Text);

        fx.Toast.RunActionCommand.Execute(null);
        Assert.Equal(3, session.Pending.Count);
    }

    [Fact]
    public async Task DiscardAll_AsksFirst_AndKeepsEverythingWhenRefused()
    {
        var fx = new ShellFixture();
        var session = await fx.OpenAsync(changes: 2);

        var discarding = fx.Shell.Review.DiscardAllCommand.ExecuteAsync(null);
        var dialog = Assert.IsType<ConfirmDialogViewModel>(fx.Dialogs.Current);
        Assert.True(dialog.IsDestructive);
        dialog.CancelCommand.Execute(null);
        await discarding;

        Assert.Equal(2, session.Pending.Count);
    }

    [Fact]
    public async Task DiscardAll_WhenConfirmed_ClearsPending()
    {
        var fx = new ShellFixture();
        var session = await fx.OpenAsync(changes: 2);

        var discarding = fx.Shell.Review.DiscardAllCommand.ExecuteAsync(null);
        ((ConfirmDialogViewModel)fx.Dialogs.Current!).ConfirmCommand.Execute(null);
        await discarding;

        Assert.Empty(session.Pending);
        Assert.True(fx.Shell.Review.IsEmpty);
    }

    [Fact]
    public async Task Toast_HidesItself_After6Seconds()
    {
        var fx = new ShellFixture();
        await fx.OpenAsync(changes: 1);
        fx.Shell.Review.Modules[0].Changes[0].DiscardCommand!.Execute(null);
        Assert.True(fx.Toast.IsVisible);

        fx.Time.Advance(TimeSpan.FromSeconds(5));
        Assert.True(fx.Toast.IsVisible);
        fx.Time.Advance(TimeSpan.FromSeconds(2));

        Assert.False(fx.Toast.IsVisible);
    }

    [Fact]
    public async Task Review_FollowsLanguageChange()
    {
        var fx = new ShellFixture();
        await fx.OpenAsync(changes: 2);

        fx.Loc.SetLanguage(Language.En);

        Assert.Equal("2 changes in 1 module", fx.Shell.Review.Subtitle);
        Assert.Equal("Jobs", fx.Shell.Review.Modules[0].Name);
    }

    // ---- fluxo completo: revisar → confirmar → resultado

    [Fact]
    public async Task Apply_Overwrite_ShowsResultPage_WithBackup_AndClearsPending()
    {
        var fx = new ShellFixture();
        await fx.OpenAsync(changes: 2);
        fx.Shell.ReviewCommand.Execute(null);

        var applying = fx.Shell.RequestApplyCommand.ExecuteAsync(null);
        ((ConfirmApplyViewModel)fx.Dialogs.Current!).ConfirmCommand.Execute(null);
        await applying;

        Assert.Equal(ShellPage.Result, fx.Shell.Page);
        var result = Assert.IsType<ResultViewModel>(fx.Shell.CurrentPage);
        Assert.True(result.HasBackup);
        Assert.False(result.IsNewSave);
        Assert.Equal("2 mudanças gravadas no save.", result.Summary);
        Assert.Equal(0, fx.Shell.PendingCount);
        Assert.Equal("Sem mudanças", fx.Shell.SessionBar.ReviewText);
    }

    [Fact]
    public async Task Apply_NewSlot_SessionBarFollowsTheNewSave_AndResultHasNoBackup()
    {
        var fx = new ShellFixture();
        var session = await fx.OpenAsync(changes: 2);
        session.OnApply = t => Task.FromResult(new ApplyResult(Sample.Slot("7", ((ApplyTarget.NewSlot)t).DisplayName), null, 2, 2));
        fx.Shell.ReviewCommand.Execute(null);

        var applying = fx.Shell.Review.RequestApplyAsync();
        var dialog = (ConfirmApplyViewModel)fx.Dialogs.Current!;
        dialog.IsNewSlot = true;
        dialog.ConfirmCommand.Execute(null);
        await applying;

        var result = Assert.IsType<ResultViewModel>(fx.Shell.CurrentPage);
        Assert.True(result.IsNewSave);
        Assert.False(result.HasBackup);
        Assert.Equal("Save 4 (editado) · slot 7", result.SlotName);
        Assert.Equal("Save 4 (editado)", fx.Shell.SessionBar.SaveName);
    }

    [Fact]
    public async Task Apply_Error_KeepsTheReviewPage_WithPendingChanges()
    {
        var fx = new ShellFixture();
        var session = await fx.OpenAsync(changes: 2);
        session.OnApply = _ => throw new CoreException(CoreError.GameRunning, "x");
        fx.Shell.ReviewCommand.Execute(null);

        var applying = fx.Shell.Review.RequestApplyAsync();
        var dialog = (ConfirmApplyViewModel)fx.Dialogs.Current!;
        dialog.ConfirmCommand.Execute(null);
        dialog.ErrorActions.Last().Command.Execute(null); // Voltar à revisão
        await applying;

        Assert.Equal(ShellPage.Review, fx.Shell.Page);
        Assert.Equal(2, fx.Shell.PendingCount);
    }

    [Fact]
    public async Task Apply_SaveChangedOnDisk_ReopenDiscardsPendingAndReloadsTheSave()
    {
        var fx = new ShellFixture();
        var session = await fx.OpenAsync(changes: 2);
        session.OnApply = _ => throw new CoreException(CoreError.SaveChangedOnDisk, "x");
        fx.Host.Open = (s, _, _) => Task.FromResult<IEditSession>(Sample.Session(s)); // o arquivo novo não traz as pendências
        fx.Shell.ReviewCommand.Execute(null);

        var applying = fx.Shell.Review.RequestApplyAsync();
        var dialog = (ConfirmApplyViewModel)fx.Dialogs.Current!;
        dialog.ConfirmCommand.Execute(null);
        dialog.ErrorActions.Single().Command.Execute(null);
        await applying;

        Assert.Equal(2, fx.Host.Opened.Count);
        Assert.Equal(ShellPage.Module, fx.Shell.Page);
        Assert.Equal(0, fx.Shell.PendingCount);
        Assert.Null(fx.Dialogs.Current); // reabrir não pergunta de novo: o aviso de perda já foi dado
    }

    [Fact]
    public async Task Result_KeepEditing_ReturnsToTheModule()
    {
        var fx = new ShellFixture();
        await fx.OpenAsync(changes: 1);
        fx.Shell.NavigateToModule("fleet");
        fx.Shell.ReviewCommand.Execute(null);
        var applying = fx.Shell.Review.RequestApplyAsync();
        ((ConfirmApplyViewModel)fx.Dialogs.Current!).ConfirmCommand.Execute(null);
        await applying;

        ((ResultViewModel)fx.Shell.CurrentPage!).KeepEditingCommand.Execute(null);

        Assert.Equal(ShellPage.Module, fx.Shell.Page);
        Assert.Equal("fleet", fx.Shell.ActiveModuleId);
    }
}

public class ResultTests
{
    private static ResultViewModel Make(ShellFixture fx, ApplyResult result, ApplyTarget target, bool cloud = false) =>
        new(fx.Loc, fx.Launcher, fx.Clipboard, fx.Toast, result, target, cloud, () => { });

    [Fact]
    public void Overwrite_ShowsBackupPath_AndOpensItsFolderRevealingTheFile()
    {
        var fx = new ShellFixture();
        var slot = Sample.Slot();
        var result = Make(fx, new ApplyResult(slot, "/profiles/p/save/4/game.sii.bak-1", 3, 5), new ApplyTarget.Overwrite());

        result.OpenFolderCommand.Execute(null);

        Assert.True(result.HasBackup);
        Assert.Equal(["/profiles/p/save/4/game.sii.bak-1"], fx.Launcher.Revealed);
        Assert.Equal("Save 4 · slot 4", result.SlotName);
        Assert.Equal("3 mudanças gravadas no save.", result.Summary);
    }

    [Fact]
    public void NewSave_HasNoBackup_AndOpensTheNewSlotFolder()
    {
        var fx = new ShellFixture();
        var slot = Sample.Slot("7", "Meu save");
        var result = Make(fx, new ApplyResult(slot, null, 1, 1), new ApplyTarget.NewSlot("Meu save"));

        result.OpenFolderCommand.Execute(null);

        Assert.False(result.HasBackup);
        Assert.Equal(["/profiles/44656D6F41/save/7"], fx.Launcher.Folders.Select(f => f.Replace('\\', '/')));
        Assert.Equal("1 mudança gravada no novo save “Meu save”.", result.Summary);
        Assert.Equal("Novo save criado", result.Title);
    }

    [Fact]
    public void OpenFolder_Failure_ShowsToastWithThePath()
    {
        var fx = new ShellFixture();
        fx.Launcher.Succeeds = false;
        var result = Make(fx, new ApplyResult(Sample.Slot(), "/x/game.sii.bak-1", 1, 1), new ApplyTarget.Overwrite());

        result.OpenFolderCommand.Execute(null);

        Assert.Contains("/x/game.sii.bak-1", fx.Toast.Text);
    }

    [Fact]
    public void CopyPath_CopiesTheBackupPath_AndConfirmsInAToast()
    {
        var fx = new ShellFixture();
        var result = Make(fx, new ApplyResult(Sample.Slot(), "/x/game.sii.bak-1", 1, 1), new ApplyTarget.Overwrite());

        result.CopyPathCommand.Execute(null);

        Assert.Equal(["/x/game.sii.bak-1"], fx.Clipboard.Texts);
        Assert.Equal("Caminho copiado", fx.Toast.Text);
    }

    [Fact]
    public void SteamCloud_ShowsOneLineByDestination()
    {
        var fx = new ShellFixture();
        var overwrite = Make(fx, new ApplyResult(Sample.Slot(), "/b", 1, 1), new ApplyTarget.Overwrite(), cloud: true);
        var created = Make(fx, new ApplyResult(Sample.Slot("7", "n"), null, 1, 1), new ApplyTarget.NewSlot("n"), cloud: true);

        Assert.True(overwrite.ShowCloud);
        Assert.NotEqual(overwrite.CloudText, created.CloudText);
    }

    [Fact]
    public void Steps_AreNumbered_PerDestination()
    {
        var fx = new ShellFixture();
        var overwrite = Make(fx, new ApplyResult(Sample.Slot(), "/b", 1, 1), new ApplyTarget.Overwrite());
        var created = Make(fx, new ApplyResult(Sample.Slot("7", "n"), null, 1, 1), new ApplyTarget.NewSlot("n"));

        Assert.Equal(3, overwrite.HelpSteps.Length);
        Assert.StartsWith("1. ", overwrite.HelpSteps[0]);
        Assert.Equal(2, created.HelpSteps.Length);
    }
}
