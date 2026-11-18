using OwnExpeditor.App.Tests.Support;
using OwnExpeditor.App.ViewModels;
using OwnExpeditor.Core;
using OwnExpeditor.Core.Session;
using OwnExpeditor.Core.Text;

namespace OwnExpeditor.App.Tests;

public class ConfirmApplyTests
{
    private static (ShellFixture Fx, TestSession Session, ConfirmApplyViewModel Dialog, Task Showing) Open(
        int changes = 2, bool cloud = false)
    {
        var fx = new ShellFixture();
        var session = fx.OpenAsync(changes, Sample.Slot(cloud: cloud)).GetAwaiter().GetResult();
        fx.Shell.ReviewCommand.Execute(null);
        var showing = fx.Shell.Review.RequestApplyAsync();
        return (fx, session, (ConfirmApplyViewModel)fx.Dialogs.Current!, showing);
    }

    [Fact]
    public void Defaults_OverwriteIsChosen_AndConfirmIsEnabled()
    {
        var (_, _, dialog, _) = Open();

        Assert.True(dialog.IsOverwrite);
        Assert.False(dialog.IsNewSlot);
        Assert.True(dialog.CanConfirm);
        Assert.Equal("Gravar no save", dialog.ConfirmLabel);
        Assert.Equal("2 mudanças serão gravadas em Demo A · Save 4.", dialog.IntroText);
    }

    [Fact]
    public void Intro_WithOneChange_UsesSingularForm()
    {
        var (_, _, dialog, _) = Open(changes: 1);

        Assert.Equal("1 mudança será gravada em Demo A · Save 4.", dialog.IntroText);
    }

    [Fact]
    public void NewSlot_PrefillsEditedName_AndChangesLabels()
    {
        var (_, _, dialog, _) = Open();

        dialog.IsNewSlot = true;

        Assert.Equal("Save 4 (editado)", dialog.NewName);
        Assert.Equal("Criar novo save", dialog.ConfirmLabel);
        Assert.Contains("novo save", dialog.IntroText);
        Assert.True(dialog.CanConfirm);
    }

    [Theory]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("a", true)]
    [InlineData("  Meu save  ", true)]
    public void NewSlot_NameLength_ValidatesLowerBound(string name, bool valid)
    {
        var (_, _, dialog, _) = Open();
        dialog.IsNewSlot = true;

        dialog.NewName = name;

        Assert.Equal(valid, dialog.NameValid);
        Assert.Equal(valid, dialog.CanConfirm);
        Assert.Equal(!valid, dialog.NameInvalid);
    }

    [Fact]
    public void NewSlot_Name_Accepts40Characters_AndRefuses41()
    {
        var (_, _, dialog, _) = Open();
        dialog.IsNewSlot = true;

        dialog.NewName = new string('x', 40);
        Assert.True(dialog.CanConfirm);

        dialog.NewName = new string('x', 41);
        Assert.False(dialog.CanConfirm);
        Assert.True(dialog.NameInvalid);
    }

    [Fact]
    public void Overwrite_IgnoresAnInvalidName()
    {
        var (_, _, dialog, _) = Open();
        dialog.IsNewSlot = true;
        dialog.NewName = "";
        Assert.False(dialog.CanConfirm);

        dialog.IsOverwrite = true;

        Assert.True(dialog.CanConfirm);
        Assert.False(dialog.NameInvalid);
    }

    [Fact]
    public async Task Confirm_Overwrite_AppliesWithOverwriteTarget_AndClosesWithApplied()
    {
        var (_, session, dialog, showing) = Open();

        dialog.ConfirmCommand.Execute(null);
        await showing;

        Assert.IsType<ApplyTarget.Overwrite>(Assert.Single(session.Applied));
        Assert.Equal(ApplyOutcomeKind.Applied, dialog.Outcome.Kind);
        Assert.NotNull(dialog.Outcome.Result);
    }

    [Fact]
    public async Task Confirm_NewSlot_AppliesWithTrimmedName()
    {
        var (_, session, dialog, showing) = Open();
        dialog.IsNewSlot = true;
        dialog.NewName = "  Rota nova  ";

        dialog.ConfirmCommand.Execute(null);
        await showing;

        var target = Assert.IsType<ApplyTarget.NewSlot>(Assert.Single(session.Applied));
        Assert.Equal("Rota nova", target.DisplayName);
    }

    [Fact]
    public void Confirm_WithInvalidName_DoesNotWrite()
    {
        var (_, session, dialog, _) = Open();
        dialog.IsNewSlot = true;
        dialog.NewName = "";

        Assert.False(dialog.ConfirmCommand.CanExecute(null));
        dialog.ConfirmCommand.Execute(null);

        Assert.Empty(session.Applied);
    }

    [Fact]
    public async Task Confirm_WhileApplying_ShowsApplyingState_AndRefusesCancel()
    {
        var (fx, session, dialog, showing) = Open();
        var release = new TaskCompletionSource<ApplyResult>();
        session.OnApply = _ => release.Task;

        dialog.ConfirmCommand.Execute(null);

        Assert.True(dialog.IsApplying);
        Assert.False(dialog.ShowQuestion);
        Assert.False(dialog.CanCancelWithEscape);
        dialog.Cancel();
        Assert.Same(dialog, fx.Dialogs.Current);

        release.SetResult(new ApplyResult(Sample.Slot(), null, 2, 2));
        await showing;
        Assert.Equal(ApplyOutcomeKind.Applied, dialog.Outcome.Kind);
    }

    [Fact]
    public async Task Cancel_ClosesWithCancelledOutcome_WithoutWriting()
    {
        var (_, session, dialog, showing) = Open();

        dialog.Cancel();
        await showing;

        Assert.Equal(ApplyOutcomeKind.Cancelled, dialog.Outcome.Kind);
        Assert.Empty(session.Applied);
    }

    [Fact]
    public void Apply_GameRunning_ShowsErrorInTheSameDialog_WithRetryAndBack()
    {
        var (fx, session, dialog, _) = Open();
        session.OnApply = _ => throw new CoreException(CoreError.GameRunning, "aberto");

        dialog.ConfirmCommand.Execute(null);

        Assert.True(dialog.HasError);
        Assert.False(dialog.IsApplying);
        Assert.Same(dialog, fx.Dialogs.Current);
        Assert.Equal(["Já fechei, tentar de novo", "Voltar à revisão"], dialog.ErrorActions.Select(a => a.Label));
        Assert.True(dialog.ErrorActions[0].IsPrimary);
        Assert.Equal("Suas mudanças pendentes continuam aqui.", dialog.PendingText);
        Assert.Contains("GameRunning", dialog.ErrorDetails);
    }

    [Fact]
    public async Task Apply_RetryAfterError_WritesAgainWithSameTarget_AndCloses()
    {
        var (_, session, dialog, showing) = Open();
        dialog.IsNewSlot = true;
        var attempts = 0;
        session.OnApply = t => ++attempts == 1
            ? throw new CoreException(CoreError.WriteFailed, "x")
            : Task.FromResult(new ApplyResult(Sample.Slot("7", "Save 4 (editado)"), null, 2, 2));
        dialog.ConfirmCommand.Execute(null);
        Assert.True(dialog.HasError);

        dialog.ErrorActions.First(a => a.IsPrimary).Command.Execute(null);
        await showing;

        Assert.Equal(2, session.Applied.Count);
        Assert.All(session.Applied, t => Assert.IsType<ApplyTarget.NewSlot>(t));
        Assert.Equal(ApplyOutcomeKind.Applied, dialog.Outcome.Kind);
    }

    [Fact]
    public async Task Apply_SaveChangedOnDisk_OffersReopen_AndWarnsPendingWillBeLost()
    {
        var (_, session, dialog, showing) = Open();
        session.OnApply = _ => throw new CoreException(CoreError.SaveChangedOnDisk, "x");
        dialog.ConfirmCommand.Execute(null);

        Assert.Equal("As mudanças pendentes serão descartadas.", dialog.PendingText);
        var action = Assert.Single(dialog.ErrorActions);
        Assert.Equal("Abrir o save de novo", action.Label);
        action.Command.Execute(null);
        await showing;

        Assert.Equal(ApplyOutcomeKind.ReopenSave, dialog.Outcome.Kind);
    }

    [Fact]
    public void Apply_InvalidEditWithReason_ShowsTheModuleReasonInsteadOfTheGenericText()
    {
        var (_, session, dialog, _) = Open();
        session.OnApply = _ => throw new CoreException(CoreError.InvalidEdit, "x") { Reason = TextRef.Of("Jobs_Error_NoFreeSlot") };

        dialog.ConfirmCommand.Execute(null);

        Assert.Contains("vaga livre", dialog.ErrorMessage);
    }

    [Fact]
    public void SteamCloud_Warning_FollowsProfile_AndDestination()
    {
        var (_, _, cloud, _) = Open(cloud: true);
        var (_, _, plain, _) = Open(cloud: false);

        Assert.True(cloud.IsSteamCloud);
        Assert.False(plain.IsSteamCloud);
        var overwriteText = cloud.CloudText;
        cloud.IsNewSlot = true;
        Assert.NotEqual(overwriteText, cloud.CloudText);
        Assert.True(cloud.CanConfirm); // o aviso informa, não bloqueia
    }

    [Fact]
    public void GameRunning_WarningTitle_FollowsTheMonitor()
    {
        var (fx, _, dialog, _) = Open();
        Assert.Equal("Feche o jogo antes de continuar", dialog.WarnTitle);

        fx.Monitor.Set(true);

        Assert.Equal("O jogo está aberto", dialog.WarnTitle);
        Assert.True(dialog.CanConfirm); // a checagem que vale é a do ApplyAsync
    }

    [Fact]
    public void Summary_GroupsTitlesByModule()
    {
        var (_, _, dialog, _) = Open(changes: 3);

        Assert.Equal(["Fretes", "Perfil"], dialog.Modules.Select(m => m.Name));
        Assert.Equal("2 mudanças", dialog.Modules[0].CountText);
    }

    [Theory]
    [InlineData(CoreError.GameRunning, ErrorAction.RetryGameClosed, ErrorAction.BackToReview, PendingNote.Kept)]
    [InlineData(CoreError.SaveChangedOnDisk, ErrorAction.ReopenSave, null, PendingNote.Lost)]
    [InlineData(CoreError.SaveNotFound, ErrorAction.ChooseSave, null, PendingNote.None)]
    [InlineData(CoreError.ReadOnlySession, ErrorAction.Close, null, PendingNote.Kept)]
    [InlineData(CoreError.NothingToApply, ErrorAction.Close, null, PendingNote.None)]
    [InlineData(CoreError.StalePatch, ErrorAction.BackToReview, ErrorAction.ReopenSave, PendingNote.Kept)]
    [InlineData(CoreError.UnknownUnit, ErrorAction.BackToReview, ErrorAction.ReopenSave, PendingNote.Kept)]
    [InlineData(CoreError.UnknownAttribute, ErrorAction.BackToReview, ErrorAction.ReopenSave, PendingNote.Kept)]
    [InlineData(CoreError.BackupExists, ErrorAction.Retry, null, PendingNote.Kept)]
    [InlineData(CoreError.BackupFailed, ErrorAction.Retry, ErrorAction.BackToReview, PendingNote.Kept)]
    [InlineData(CoreError.WriteFailed, ErrorAction.Retry, ErrorAction.BackToReview, PendingNote.Kept)]
    [InlineData(CoreError.VerifyFailed, ErrorAction.Retry, ErrorAction.BackToReview, PendingNote.Kept)]
    [InlineData(CoreError.AccessDenied, ErrorAction.Retry, ErrorAction.OpenFolder, PendingNote.Kept)]
    [InlineData(CoreError.SlotUnavailable, ErrorAction.Retry, ErrorAction.BackToReview, PendingNote.Kept)]
    [InlineData(CoreError.SessionBusy, ErrorAction.Retry, ErrorAction.BackToReview, PendingNote.Kept)]
    public void ErrorMap_FollowsTheDesignTable(CoreError code, ErrorAction primary, ErrorAction? secondary, PendingNote pending)
    {
        var plan = ApplyErrorMap.For(code);

        Assert.Equal(primary, plan.Primary);
        Assert.Equal(secondary, plan.Secondary);
        Assert.Equal(pending, plan.Pending);
    }

    [Fact]
    public void ErrorMap_CoversEveryCoreError_WithAnAction()
    {
        foreach (var code in Enum.GetValues<CoreError>())
            Assert.True(Enum.IsDefined(ApplyErrorMap.For(code).Primary), code.ToString());
    }
}
