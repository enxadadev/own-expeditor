using OwnExpeditor.App.Tests.Support;
using OwnExpeditor.App.ViewModels;
using OwnExpeditor.Core;
using OwnExpeditor.Core.Session;
using OwnExpeditor.Domain.Settings;

namespace OwnExpeditor.App.Tests;

public class OpenErrorTests
{
    private static OpenErrorViewModel Make(ShellFixture fx, CoreError code, int? line = null, string? stage = null) =>
        new(fx.Loc, fx.Clipboard, fx.Launcher, new CoreException(code, "detalhe") { Line = line, StageKey = stage },
            Sample.Slot(), "1.2.3", fx.Time, new Services.ImmediateUiDispatcher(), () => { }, () => { });

    [Theory]
    [InlineData(CoreError.SaveParseFailed, OpenErrorKind.Parse)]
    [InlineData(CoreError.SaveCorrupt, OpenErrorKind.Corrupt)]
    [InlineData(CoreError.SaveNotFound, OpenErrorKind.NotFound)]
    [InlineData(CoreError.AccessDenied, OpenErrorKind.AccessDenied)]
    [InlineData(CoreError.Conflict, OpenErrorKind.Unknown)]
    [InlineData(CoreError.WriteFailed, OpenErrorKind.Unknown)]
    public void KindFor_MapsCoreErrorToScreen(CoreError code, OpenErrorKind kind) =>
        Assert.Equal(kind, OpenErrorViewModel.KindFor(code));

    [Fact]
    public void Parse_Screen_OffersCauses_CopyDetails_AndOpenFolder()
    {
        var fx = new ShellFixture();

        var vm = Make(fx, CoreError.SaveParseFailed, stage: LoadStages.Parse);

        Assert.Equal("Não foi possível entender o save", vm.Title);
        Assert.Equal(3, vm.Causes.Count);
        Assert.True(vm.ShowCopyDetails);
        Assert.True(vm.ShowOpenFolder);
        Assert.Equal(2, vm.Tips.Count);
    }

    [Fact]
    public void Corrupt_Screen_HasNoCopyOrOpenFolder_ButShowsLineInDetails()
    {
        var fx = new ShellFixture();

        var vm = Make(fx, CoreError.SaveCorrupt, line: 18204);

        Assert.Equal("Não foi possível ler este save", vm.Title);
        Assert.False(vm.ShowCopyDetails);
        Assert.False(vm.ShowOpenFolder);
        Assert.Equal("Código: SaveCorrupt · perto da linha 18.204", vm.DetailsText);
    }

    [Theory]
    [InlineData(CoreError.SaveNotFound, "Save não encontrado")]
    [InlineData(CoreError.AccessDenied, "Sem permissão para ler o save")]
    [InlineData(CoreError.SessionBusy, "Não foi possível abrir o save")]
    public void Other_Screens_UseTheirOwnTextsAndNoTips(CoreError code, string title)
    {
        var fx = new ShellFixture();

        var vm = Make(fx, code);

        Assert.Equal(title, vm.Title);
        Assert.False(vm.ShowTips);
        Assert.False(vm.ShowCauses);
        Assert.Equal($"Código: {code}", vm.DetailsText);
    }

    [Fact]
    public async Task CopyDetails_CopiesTechnicalText_WithoutSaveContent_AndRevertsLabelAfter2Seconds()
    {
        var fx = new ShellFixture();
        var vm = Make(fx, CoreError.SaveParseFailed, stage: LoadStages.Parse);
        Assert.Equal("Copiar detalhes", vm.CopyText);

        await vm.CopyDetailsCommand.ExecuteAsync(null);

        var text = Assert.Single(fx.Clipboard.Texts);
        Assert.Contains("SaveParseFailed", text);
        Assert.Contains("Demo A", text);
        Assert.Contains("Save 4", text);
        Assert.Contains("1.2.3", text);
        Assert.Contains("Interpretando o save", text);
        Assert.DoesNotContain("SiiNunit", text);
        Assert.Equal("Copiado", vm.CopyText);

        fx.Time.Advance(TimeSpan.FromSeconds(3));
        Assert.Equal("Copiar detalhes", vm.CopyText);
    }

    [Fact]
    public async Task OpenSaveFolder_OpensTheFolderOfTheSaveFile()
    {
        var fx = new ShellFixture();
        var vm = Make(fx, CoreError.SaveParseFailed);

        await vm.OpenFolderCommand.ExecuteAsync(null);

        Assert.Single(fx.Launcher.Folders);
        Assert.EndsWith("4", fx.Launcher.Folders[0].Replace('\\', '/'));
        Assert.DoesNotContain("game.sii", fx.Launcher.Folders[0]);
    }

    [Fact]
    public void Details_AreCollapsedByDefault_AndToggle()
    {
        var fx = new ShellFixture();
        var vm = Make(fx, CoreError.SaveCorrupt);

        Assert.False(vm.IsDetailsOpen);
        vm.ToggleDetailsCommand.Execute(null);
        Assert.True(vm.IsDetailsOpen);
    }

    [Fact]
    public void Texts_FollowLanguageChange()
    {
        var fx = new ShellFixture();
        var vm = Make(fx, CoreError.SaveCorrupt);

        fx.Loc.SetLanguage(Language.En);

        Assert.Equal("Couldn't read this save", vm.Title);
    }
}

public class LoadingTests
{
    [Fact]
    public void BeforeAnyProgress_IsIndeterminate_WithoutStages()
    {
        var vm = new LoadingViewModel(new Services.Localizer(Language.PtBr));
        vm.Begin(Sample.Slot(), () => { });

        Assert.True(vm.IsIndeterminate);
        Assert.Empty(vm.StageItems);
    }

    [Fact]
    public void Progress_ShowsPercent_Stage_AndSize()
    {
        var vm = new LoadingViewModel(new Services.Localizer(Language.PtBr));
        vm.Begin(Sample.Slot(), () => { });

        vm.Report(new LoadProgress(62, LoadStages.Parse, 90_600_000));

        Assert.Equal(0.62, vm.Progress);
        Assert.Equal("62%", vm.PercentShort);
        Assert.Equal("Interpretando o save", vm.StageText);
        Assert.Equal("86,4 MB", vm.SizeText);
        Assert.Equal([StageState.Done, StageState.Current, StageState.Pending], vm.StageItems.Select(i => i.State));
    }

    [Fact]
    public void NegativePercent_MeansIndeterminate()
    {
        var vm = new LoadingViewModel(new Services.Localizer(Language.PtBr));
        vm.Begin(Sample.Slot(), () => { });

        vm.Report(new LoadProgress(-1, LoadStages.Read));

        Assert.True(vm.IsIndeterminate);
    }

    [Fact]
    public void Cancel_CallsTheCallbackOnce_AndShowsCancelling()
    {
        var calls = 0;
        var vm = new LoadingViewModel(new Services.Localizer(Language.PtBr));
        vm.Begin(Sample.Slot(), () => calls++);

        vm.CancelCommand.Execute(null);
        vm.Cancel();

        Assert.Equal(1, calls);
        Assert.Equal("Cancelando…", vm.CancelText);
    }

    [Fact]
    public void Begin_ResetsPreviousProgress()
    {
        var vm = new LoadingViewModel(new Services.Localizer(Language.PtBr));
        vm.Begin(Sample.Slot(), () => { });
        vm.Report(new LoadProgress(90, LoadStages.Index));

        vm.Begin(Sample.Slot("2", "Save 2"), () => { });

        Assert.True(vm.IsIndeterminate);
        Assert.False(vm.IsCancelling);
    }
}
