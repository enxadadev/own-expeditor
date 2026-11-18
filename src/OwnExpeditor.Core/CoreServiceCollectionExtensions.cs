using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OwnExpeditor.Core.Games;
using OwnExpeditor.Core.Profiles;
using OwnExpeditor.Core.Session;
using OwnExpeditor.Core.Settings;
using OwnExpeditor.Data;
using OwnExpeditor.Data.Games;
using OwnExpeditor.Data.Saves;
using OwnExpeditor.Data.Settings;

namespace OwnExpeditor.Core;

public static class CoreServiceCollectionExtensions
{
    /// <summary>
    /// Registra a Data (<c>AddOwnExpeditorData</c>) e os serviços do Core como singletons; serviços já registrados
    /// antes (inclusive <c>DataOptions</c>) vencem, e chamar de novo não duplica. O <see cref="IGameStatusMonitor"/>
    /// só passa a monitorar quando é resolvido (o App deve resolvê-lo na partida). Módulos entram em suas próprias extensões.
    /// </summary>
    public static IServiceCollection AddOwnExpeditorCore(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddOwnExpeditorData();
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IAppSettingsService>(sp => new AppSettingsService(
            sp.GetRequiredService<ISettingsStore>(), sp.GetRequiredService<IGameInstallLocator>()));
        services.TryAddSingleton<IGameDataService>(sp => new GameDataService(
            sp.GetRequiredService<IGameInstallLocator>(), sp.GetRequiredService<IGameCatalogSource>(),
            sp.GetRequiredService<IAppSettingsService>()));
        services.TryAddSingleton<IProfileService>(sp => new ProfileService(sp.GetRequiredService<IProfileLocator>()));
        services.TryAddSingleton<IEditSessionHost>(sp => new EditSessionHost(
            sp.GetRequiredService<ISaveFileStore>(), sp.GetRequiredService<IGameDataService>(),
            sp.GetRequiredService<IGameProcessMonitor>(), sp.GetRequiredService<TimeProvider>()));
        services.TryAddSingleton<IGameStatusMonitor>(sp => new GameStatusMonitor(
            sp.GetRequiredService<IEditSessionHost>(), sp.GetRequiredService<IGameProcessMonitor>(),
            sp.GetRequiredService<TimeProvider>()));
        return services;
    }
}
