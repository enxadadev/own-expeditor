using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OwnExpeditor.Data.Distances;
using OwnExpeditor.Data.Games;
using OwnExpeditor.Data.Maps;
using OwnExpeditor.Data.Saves;
using OwnExpeditor.Data.Settings;

namespace OwnExpeditor.Data;

public static class DataServiceCollectionExtensions
{
    /// <summary>Registra as implementações da Data (singletons). Chamar de novo não duplica registros.</summary>
    public static IServiceCollection AddOwnExpeditorData(this IServiceCollection services, DataOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton(options ?? new DataOptions());
        services.TryAddSingleton<ISaveFileStore>(_ => new SaveFileStore());
        services.TryAddSingleton<IProfileLocator>(sp => new ProfileLocator(sp.GetRequiredService<DataOptions>()));
        services.TryAddSingleton<IGameProcessMonitor>(_ => new GameProcessMonitor());
        services.TryAddSingleton<IGameInstallLocator>(sp => new GameInstallLocator(sp.GetRequiredService<DataOptions>()));
        services.TryAddSingleton<IGameCatalogSource>(_ => new GameCatalogSource());
        services.TryAddSingleton<ISettingsStore>(sp => new JsonSettingsStore(
            sp.GetRequiredService<DataOptions>().SettingsDirectory
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OwnExpeditor")));
        services.TryAddSingleton<IDistanceCache>(sp => new JsonDistanceCache(
            sp.GetRequiredService<DataOptions>().SettingsDirectory
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OwnExpeditor")));
        services.TryAddSingleton<IModLocator>(_ => new NullModLocator());
        services.TryAddSingleton<IMapSource>(sp => new MapSource(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OwnExpeditor", "cache", "map")));
        return services;
    }
}
