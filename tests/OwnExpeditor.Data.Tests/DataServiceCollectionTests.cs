using Microsoft.Extensions.DependencyInjection;
using OwnExpeditor.Data.Games;
using OwnExpeditor.Data.Saves;
using OwnExpeditor.Data.Settings;
using OwnExpeditor.Domain.Settings;

namespace OwnExpeditor.Data.Tests;

public class DataServiceCollectionTests : IDisposable
{
    private readonly TempDir _t = new();

    public void Dispose() => _t.Dispose();

    /// <summary>Provider mínimo (o pacote completo de DI não é dependência dos testes).</summary>
    private sealed class Provider(IServiceCollection services) : IServiceProvider
    {
        private readonly Dictionary<Type, object> _cache = [];

        public object? GetService(Type type)
        {
            if (_cache.TryGetValue(type, out var hit)) return hit;
            var d = services.LastOrDefault(s => s.ServiceType == type);
            if (d is null) return null;
            var value = d.ImplementationInstance
                ?? d.ImplementationFactory?.Invoke(this)
                ?? Activator.CreateInstance(d.ImplementationType!);
            return _cache[type] = value!;
        }
    }

    private (ServiceCollection Services, Provider Provider) Build(DataOptions? options = null)
    {
        var services = new ServiceCollection();
        services.AddOwnExpeditorData(options ?? new DataOptions
        {
            DocumentsRoot = _t.Combine("docs"),
            SettingsDirectory = _t.Combine("cfg"),
            HomeDirectory = _t.Combine("home"),
            SteamRoots = [],
            UseRegistry = false,
        });
        return (services, new Provider(services));
    }

    [Theory]
    [InlineData(typeof(ISaveFileStore))]
    [InlineData(typeof(IProfileLocator))]
    [InlineData(typeof(IGameProcessMonitor))]
    [InlineData(typeof(ISettingsStore))]
    [InlineData(typeof(IGameInstallLocator))]
    [InlineData(typeof(IGameCatalogSource))]
    public void AddOwnExpeditorData_RegistersEachServiceAsSingleton(Type service)
    {
        var (services, provider) = Build();

        var descriptor = Assert.Single(services, s => s.ServiceType == service);

        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
        Assert.NotNull(provider.GetService(service));
    }

    [Fact]
    public void AddOwnExpeditorData_CalledTwice_DoesNotDuplicateRegistrations()
    {
        var (services, _) = Build();
        services.AddOwnExpeditorData();

        Assert.Single(services, s => s.ServiceType == typeof(ISaveFileStore));
        Assert.Single(services, s => s.ServiceType == typeof(ISettingsStore));
    }

    [Fact]
    public async Task AddOwnExpeditorData_SettingsStore_UsesConfiguredDirectory()
    {
        var (_, provider) = Build();
        var store = (ISettingsStore)provider.GetService(typeof(ISettingsStore))!;

        await store.SaveAsync(new AppSettings(ThemePreference.Dark, Language.En, null, new GameFolders()), default);

        Assert.True(File.Exists(_t.Combine("cfg", "settings.json")));
    }

    [Fact]
    public void AddOwnExpeditorData_ProfileLocator_UsesConfiguredDocumentsRoot()
    {
        Layout.Profile(_t, "docs/Euro Truck Simulator 2", "profiles", Sii.Hex("DI"),
            OwnExpeditor.Domain.Games.GameKind.Ets2, ("1", "a"));
        var (_, provider) = Build();
        var locator = (IProfileLocator)provider.GetService(typeof(IProfileLocator))!;

        Assert.Equal("DI", Assert.Single(locator.FindProfiles()).DisplayName);
    }

    [Fact]
    public void AddOwnExpeditorData_NullServices_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => DataServiceCollectionExtensions.AddOwnExpeditorData(null!));
    }
}
