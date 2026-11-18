using Microsoft.Extensions.DependencyInjection;
using OwnExpeditor.App.Services;
using OwnExpeditor.App.Shell;
using OwnExpeditor.App.ViewModels;
using OwnExpeditor.App.Views;
using OwnExpeditor.Core.Games;
using OwnExpeditor.Core.Session;
using OwnExpeditor.Domain.Settings;

namespace OwnExpeditor.App.Tests;

public class CompositionTests
{
    [Fact]
    public void Build_ResolvesShell_WithTheRealCoreServices()
    {
        using var provider = (ServiceProvider)AppComposition.Build(s => s.AddSingleton<ILocalizer>(new Localizer(Language.PtBr)));

        var shell = provider.GetRequiredService<ShellViewModel>();

        Assert.Equal(5, shell.Modules.Count);
        Assert.NotNull(provider.GetRequiredService<IEditSessionHost>());
        Assert.NotNull(provider.GetRequiredService<IGameStatusMonitor>());
    }

    [Fact]
    public void Build_Overrides_ReplaceServices()
    {
        var fake = new Support.FakeHost();
        using var provider = (ServiceProvider)AppComposition.Build(s =>
        {
            s.AddSingleton<ILocalizer>(new Localizer(Language.PtBr));
            s.AddSingleton<IEditSessionHost>(fake);
        });

        Assert.Same(fake, provider.GetRequiredService<IEditSessionHost>());
    }

    [Fact]
    public void Services_AreSingletons_SoThereIsOneShellAndOneToast()
    {
        using var provider = (ServiceProvider)AppComposition.Build(s => s.AddSingleton<ILocalizer>(new Localizer(Language.PtBr)));

        Assert.Same(provider.GetRequiredService<ShellViewModel>(), provider.GetRequiredService<ShellViewModel>());
        Assert.Same(provider.GetRequiredService<ToastViewModel>(), provider.GetRequiredService<IToastService>());
        Assert.Same(provider.GetRequiredService<DialogService>(), provider.GetRequiredService<IDialogService>());
    }

    [Fact]
    public void Welcome_And_SessionBar_HaveIndependentPickers()
    {
        using var provider = (ServiceProvider)AppComposition.Build(s => s.AddSingleton<ILocalizer>(new Localizer(Language.PtBr)));
        var shell = provider.GetRequiredService<ShellViewModel>();

        Assert.NotSame(shell.Welcome.Picker, shell.SessionBar.Picker);
    }

    [Fact]
    public void RegisterViews_MapsEveryViewModelOfThePages()
    {
        AppComposition.RegisterViews();

        foreach (var vm in new[] { typeof(ShellViewModel), typeof(WelcomeViewModel), typeof(LoadingViewModel), typeof(OpenErrorViewModel),
                     typeof(ModulePlaceholderViewModel), typeof(ReviewViewModel), typeof(ResultViewModel), typeof(SettingsViewModel),
                     typeof(ConfirmApplyViewModel), typeof(ConfirmDialogViewModel), typeof(SessionBarViewModel), typeof(SavePickerViewModel) })
            Assert.True(ViewRegistry.IsRegistered(vm), vm.Name);
        Assert.True(ViewRegistry.IsViewRegistered(typeof(ShellView)));
    }

    [Fact]
    public void ViewRegistry_Build_UnregisteredViewModel_ReturnsNull() =>
        Assert.Null(ViewRegistry.Build(new object()));
}
