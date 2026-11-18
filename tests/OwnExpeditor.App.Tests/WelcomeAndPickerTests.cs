using OwnExpeditor.App.Services;
using OwnExpeditor.App.Tests.Support;
using OwnExpeditor.App.ViewModels;
using OwnExpeditor.Domain.Saves;
using OwnExpeditor.Domain.Settings;

namespace OwnExpeditor.App.Tests;

public class WelcomeAndPickerTests
{
    private static async Task<(ShellFixture Fx, WelcomeViewModel Vm)> LoadedAsync(string? lastPath = null)
    {
        var fx = new ShellFixture();
        fx.Settings.Current = fx.Settings.Current with { LastSavePath = lastPath };
        await fx.Shell.Welcome.ActivateAsync(CancellationToken.None);
        return (fx, fx.Shell.Welcome);
    }

    [Fact]
    public async Task Activate_ListsProfiles_SelectsFirstProfile_ButNoSave()
    {
        var (_, vm) = await LoadedAsync();

        Assert.Single(vm.Picker.Profiles);
        Assert.NotNull(vm.Picker.SelectedProfile);
        Assert.Equal(2, vm.Picker.Saves.Count);
        Assert.Null(vm.Picker.SelectedSave);
        Assert.False(vm.Picker.OpenCommand.CanExecute(null));
        Assert.Equal("Escolha um save para continuar", vm.Picker.Hint);
    }

    [Fact]
    public async Task Picker_SavesAreListedNewestFirst_WithKindBadge()
    {
        var fx = new ShellFixture();
        fx.Profiles.Saves["/profiles/44656D6F41"] =
        [
            Sample.Slot("old", "Antigo") with { LastWriteUtc = new DateTime(2026, 1, 1) },
            Sample.Slot("quicksave", "Quicksave") with { LastWriteUtc = new DateTime(2026, 6, 1) },
            Sample.Slot("autosave", "Autosave") with { LastWriteUtc = new DateTime(2026, 5, 1) },
        ];
        await fx.Shell.Welcome.ActivateAsync(CancellationToken.None);

        var saves = fx.Shell.Welcome.Picker.Saves;

        Assert.Equal(["Quicksave", "Autosave", "Antigo"], saves.Select(s => s.Name));
        Assert.Equal(["Rápido", "Automático", "Manual"], saves.Select(s => s.KindText));
    }

    [Fact]
    public async Task Picker_ChoosingASave_EnablesOpen_AndUpdatesHint()
    {
        var (_, vm) = await LoadedAsync();

        vm.Picker.SelectedSave = vm.Picker.Saves[0];

        Assert.True(vm.Picker.OpenCommand.CanExecute(null));
        Assert.Contains("Demo A", vm.Picker.Hint);
    }

    [Fact]
    public async Task Picker_Open_RaisesRequestWithTheSelectedSave_AndOpensNothingByItself()
    {
        var (fx, vm) = await LoadedAsync();
        SaveSlotInfo? requested = null;
        vm.Picker.OpenRequested += (_, s) => requested = s;
        vm.Picker.SelectedSave = vm.Picker.Saves[0];

        vm.Picker.OpenCommand.Execute(null);

        Assert.Equal(vm.Picker.Saves[0].Info, requested);
        Assert.Empty(fx.Host.Opened.Where(_ => false));
    }

    [Fact]
    public async Task Picker_Activate_SelectsAndOpens()
    {
        var (fx, vm) = await LoadedAsync();

        vm.Picker.Activate(vm.Picker.Saves[0]);
        await Task.Delay(1);

        Assert.Single(fx.Host.Opened);
    }

    [Fact]
    public async Task LastSave_IsOffered_WhenItStillExists_AndReopenOpensIt()
    {
        var (fx, vm) = await LoadedAsync(Sample.Slot().GameSiiPath);

        Assert.True(vm.HasLastSave);
        Assert.Contains("Save 4", vm.LastSaveText);
        Assert.Empty(fx.Host.Opened);

        vm.ReopenLastCommand.Execute(null);
        await Task.Delay(1);

        Assert.Single(fx.Host.Opened);
    }

    [Fact]
    public async Task LastSave_IsHidden_WhenFileNoLongerListed()
    {
        var (_, vm) = await LoadedAsync("/sumiu/game.sii");

        Assert.False(vm.HasLastSave);
        Assert.False(vm.ReopenLastCommand.CanExecute(null));
    }

    [Fact]
    public async Task NoProfiles_ShowsEmptyState()
    {
        var fx = new ShellFixture();
        fx.Profiles.Profiles.Clear();

        await fx.Shell.Welcome.ActivateAsync(CancellationToken.None);

        Assert.True(fx.Shell.Welcome.Picker.ShowNoProfiles);
        Assert.False(fx.Shell.Welcome.Picker.ShowLists);
    }

    [Fact]
    public async Task ProfileWithoutSaves_ShowsNoSavesState()
    {
        var fx = new ShellFixture();
        fx.Profiles.Saves["/profiles/44656D6F41"] = [];

        await fx.Shell.Welcome.ActivateAsync(CancellationToken.None);

        Assert.True(fx.Shell.Welcome.Picker.ShowNoSaves);
    }

    [Fact]
    public async Task Picker_FollowsLanguageChange()
    {
        var (fx, vm) = await LoadedAsync();

        fx.Loc.SetLanguage(Language.En);

        Assert.Equal("Choose a save to continue", vm.Picker.Hint);
        Assert.Equal("Autosave", vm.Picker.Saves[1].KindText.Replace("Automático", "Autosave"));
    }
}

public class DialogAndToastTests
{
    [Fact]
    public async Task Dialogs_AreShownOneAtATime_InOrder()
    {
        var loc = new Localizer(Language.PtBr);
        var service = new DialogService(loc);
        var first = service.ConfirmAsync(Core.Text.TextRef.Of("Confirm_Title"), Core.Text.TextRef.Of("Confirm_Title"), Core.Text.TextRef.Of("Common_Close"), false);
        var second = service.ConfirmAsync(Core.Text.TextRef.Of("Review_Title"), Core.Text.TextRef.Of("Review_Title"), Core.Text.TextRef.Of("Common_Close"), true);

        var firstDialog = Assert.IsType<ConfirmDialogViewModel>(service.Current);
        Assert.Equal("Gravar mudanças no save?", firstDialog.Title);
        firstDialog.ConfirmCommand.Execute(null);
        Assert.True(await first);

        Assert.True(SpinWait.SpinUntil(() => service.Current is not null, TimeSpan.FromSeconds(5))); // o semáforo libera em segundo plano
        var secondDialog = Assert.IsType<ConfirmDialogViewModel>(service.Current);
        Assert.Equal("Revisar mudanças", secondDialog.Title);
        secondDialog.CancelCommand.Execute(null);
        Assert.False(await second);
        Assert.Null(service.Current);
    }

    [Fact]
    public async Task Dialog_RaisesCurrentChanged()
    {
        var service = new DialogService(new Localizer(Language.PtBr));
        var changes = 0;
        service.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(IDialogService.Current)) changes++; };

        var showing = service.ConfirmAsync(Core.Text.TextRef.Of("Confirm_Title"), Core.Text.TextRef.Of("Confirm_Title"), Core.Text.TextRef.Of("Common_Close"), false);
        ((ConfirmDialogViewModel)service.Current!).Cancel();
        await showing;

        Assert.Equal(2, changes);
    }
}
