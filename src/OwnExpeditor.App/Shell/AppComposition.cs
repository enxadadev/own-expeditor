using System;
using Microsoft.Extensions.DependencyInjection;
using OwnExpeditor.App.Modules.Jobs;
using OwnExpeditor.App.Services;
using OwnExpeditor.App.ViewModels;
using OwnExpeditor.App.Views;
using OwnExpeditor.Core;
using OwnExpeditor.Core.Games;
using OwnExpeditor.Core.Modules.Jobs;

namespace OwnExpeditor.App.Shell;

/// <summary>
/// Raiz de composição do shell: registra os serviços do Core (que trazem a Data) e os do App, os ViewModels e os
/// módulos. O App nunca referencia Sii/Data: tudo entra por <c>AddOwnExpeditorCore</c> (ADR 0006).
/// </summary>
public static class AppComposition
{
    /// <param name="overrides">Troca serviços (testes, amostras): o último registro vence.</param>
    public static IServiceProvider Build(Action<IServiceCollection>? overrides = null)
    {
        var services = new ServiceCollection();
        services.AddOwnExpeditorCore();

        services.AddSingleton<ILocalizer>(_ => Localizer.Instance);
        services.AddSingleton<IThemeHost, ApplicationThemeHost>();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<IUiDispatcher, AvaloniaUiDispatcher>();
        services.AddSingleton<IShellLauncher, ShellLauncher>();
        services.AddSingleton<IFolderPicker, AvaloniaFolderPicker>();
        services.AddSingleton<IClipboardService, AvaloniaClipboardService>();
        services.AddSingleton<DialogService>();
        services.AddSingleton<IDialogService>(sp => sp.GetRequiredService<DialogService>());
        services.AddSingleton<ToastViewModel>();
        services.AddSingleton<IToastService>(sp => sp.GetRequiredService<ToastViewModel>());

        // Duas listas de perfis independentes: a da tela inicial e a do seletor da barra de sessão.
        services.AddSingleton(sp => new WelcomeViewModel(sp.GetRequiredService<ILocalizer>(),
            new SavePickerViewModel(sp.GetRequiredService<ILocalizer>(), sp.GetRequiredService<Core.Profiles.IProfileService>()),
            sp.GetRequiredService<Core.Settings.IAppSettingsService>()));
        services.AddSingleton(sp => new SessionBarViewModel(sp.GetRequiredService<ILocalizer>(),
            new SavePickerViewModel(sp.GetRequiredService<ILocalizer>(), sp.GetRequiredService<Core.Profiles.IProfileService>())));
        services.AddSingleton<LoadingViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<ReviewViewModel>();
        services.AddSingleton<ShellViewModel>();

        services.AddJobsModule();
        AddModules(services);
        overrides?.Invoke(services);
        return services.BuildServiceProvider();
    }

    /// <summary>Módulos da navegação: Fretes é real; os demais seguem como espaço reservado até existirem.</summary>
    public static IServiceCollection AddModules(IServiceCollection services)
    {
        services.AddModule(JobsPageViewModel.Id, $"Module_{JobsPageViewModel.Id}", "IconNavJobs", 1,
            sp => new JobsPageViewModel(sp.GetRequiredService<ILocalizer>(), sp.GetRequiredService<IJobBoardFactory>(),
                sp.GetRequiredService<IToastService>(), sp.GetRequiredService<IUiDispatcher>()));
        return AddModulePlaceholders(services);
    }

    /// <summary>Módulos da navegação até existirem: só um espaço reservado (Fretes já é real; ver <see cref="AddModules"/>).</summary>
    public static IServiceCollection AddModulePlaceholders(IServiceCollection services)
    {
        (string Id, int Order, string Icon)[] modules =
            [("fleet", 2, "IconNavFleet"), ("profile", 3, "IconNavProfile"),
             ("company", 4, "IconNavCompany"), ("map", 5, "IconNavMap")];
        foreach (var (id, order, icon) in modules)
            services.AddModule(id, $"Module_{id}", icon, order,
                sp => new ModulePlaceholderViewModel(sp.GetRequiredService<ILocalizer>(), id, $"Module_{id}"));
        return services;
    }

    /// <summary>Liga cada ViewModel à sua View (idempotente).</summary>
    public static void RegisterViews()
    {
        ViewRegistry.Register<ShellViewModel, ShellView>();
        ViewRegistry.Register<SessionBarViewModel, SessionBarView>();
        ViewRegistry.Register<WelcomeViewModel, WelcomeView>();
        ViewRegistry.Register<SavePickerViewModel, SavePickerView>();
        ViewRegistry.Register<LoadingViewModel, LoadingView>();
        ViewRegistry.Register<OpenErrorViewModel, OpenErrorView>();
        ViewRegistry.Register<ModulePlaceholderViewModel, ModulePlaceholderView>();
        ViewRegistry.Register<JobsPageViewModel, JobsView>();
        ViewRegistry.Register<ReviewViewModel, ReviewView>();
        ViewRegistry.Register<ResultViewModel, ResultView>();
        ViewRegistry.Register<SettingsViewModel, SettingsView>();
        ViewRegistry.Register<ConfirmApplyViewModel, ConfirmApplyView>();
        ViewRegistry.Register<ConfirmDialogViewModel, ConfirmDialogView>();
    }

    /// <summary>Resolve o monitor do jogo: ele só passa a consultar o processo depois de criado (na partida).</summary>
    public static void StartBackgroundServices(IServiceProvider services) =>
        _ = services.GetRequiredService<IGameStatusMonitor>();
}
