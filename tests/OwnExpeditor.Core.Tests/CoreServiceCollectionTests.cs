using Microsoft.Extensions.DependencyInjection;
using OwnExpeditor.Core.Games;
using OwnExpeditor.Core.Profiles;
using OwnExpeditor.Core.Session;
using OwnExpeditor.Core.Settings;
using OwnExpeditor.Data;
using OwnExpeditor.Data.Saves;
using OwnExpeditor.Domain.Games;

namespace OwnExpeditor.Core.Tests;

public sealed class CoreServiceCollectionTests : IDisposable
{
    private readonly TempDir _t = new();

    public void Dispose() => _t.Dispose();

    private ServiceProvider Provider()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new DataOptions
        {
            DocumentsRoot = _t.Combine("docs"),
            SettingsDirectory = _t.Combine("settings"),
            HomeDirectory = _t.Combine("home"),
            SteamRoots = [],
            UseRegistry = false,
        });
        services.AddOwnExpeditorCore();
        return services.BuildServiceProvider();
    }

    private string CreateSave(string text = SampleSave.Text, string slot = "1")
    {
        var hex = Convert.ToHexString("p1"u8.ToArray());
        _t.File("SiiNunit\r\n{\r\nsave_container : _nameless.0 {\r\n name: \"Slot\"\r\n}\r\n\r\n}\r\n",
            "docs", "Euro Truck Simulator 2", "profiles", hex, "save", slot, "info.sii");
        return _t.File(text, "docs", "Euro Truck Simulator 2", "profiles", hex, "save", slot, "game.sii");
    }

    [Fact]
    public void AddOwnExpeditorCore_RegistersEveryCoreService()
    {
        using var sp = Provider();

        Assert.NotNull(sp.GetService<IEditSessionHost>());
        Assert.NotNull(sp.GetService<IGameDataService>());
        Assert.NotNull(sp.GetService<IGameStatusMonitor>());
        Assert.NotNull(sp.GetService<IProfileService>());
        Assert.NotNull(sp.GetService<IAppSettingsService>());
        Assert.NotNull(sp.GetService<ISaveFileStore>());
    }

    [Fact]
    public void AddOwnExpeditorCore_ServicesAreSingletons()
    {
        using var sp = Provider();

        Assert.Same(sp.GetRequiredService<IEditSessionHost>(), sp.GetRequiredService<IEditSessionHost>());
        Assert.Same(sp.GetRequiredService<IGameDataService>(), sp.GetRequiredService<IGameDataService>());
        Assert.Same(sp.GetRequiredService<IAppSettingsService>(), sp.GetRequiredService<IAppSettingsService>());
    }

    [Fact]
    public void AddOwnExpeditorCore_CalledTwice_DoesNotDuplicateRegistrations()
    {
        var services = new ServiceCollection();
        services.AddOwnExpeditorCore();
        var count = services.Count;

        services.AddOwnExpeditorCore();

        Assert.Equal(count, services.Count);
    }

    [Fact]
    public void AddOwnExpeditorCore_RegistersDataLayerToo()
    {
        var services = new ServiceCollection();
        services.AddOwnExpeditorCore();

        Assert.Contains(services, d => d.ServiceType == typeof(ISaveFileStore));
    }

    [Fact]
    public void AddOwnExpeditorCore_CallerRegisteredServiceWins()
    {
        var services = new ServiceCollection();
        var fake = new FakeProcessMonitor();
        services.AddSingleton<OwnExpeditor.Data.Games.IGameProcessMonitor>(fake);
        services.AddOwnExpeditorCore();
        using var sp = services.BuildServiceProvider();

        Assert.Same(fake, sp.GetRequiredService<OwnExpeditor.Data.Games.IGameProcessMonitor>());
    }

    [Fact]
    public async Task Profiles_FoundThroughCoreService()
    {
        CreateSave();
        using var sp = Provider();

        var profiles = await sp.GetRequiredService<IProfileService>().GetProfilesAsync(default);
        var saves = await sp.GetRequiredService<IProfileService>().GetSavesAsync(profiles.Single(), default);

        Assert.Equal("p1", profiles.Single().DisplayName);
        Assert.Equal("1", saves.Single().Id);
    }

    [Fact]
    public async Task EndToEnd_Overwrite_WritesSaveWithBackupAndNoTempFile()
    {
        var path = CreateSave();
        using var sp = Provider();
        var slot = await FirstSlot(sp);
        var session = await sp.GetRequiredService<IEditSessionHost>().OpenAsync(slot, null, default);
        session.Stage(SampleSave.FillTarget());

        var result = await session.ApplyAsync(new ApplyTarget.Overwrite(), default);

        Assert.Contains("target: \"demo_co.city_c\"", File.ReadAllText(path));
        Assert.Equal(SampleSave.Text, File.ReadAllText(result.BackupPath!));
        Assert.StartsWith(path + ".bak-", result.BackupPath);
        Assert.False(File.Exists(path + ".tmp"));
        Assert.False(session.HasChanges);
    }

    [Fact]
    public async Task EndToEnd_NewSlot_CreatesNextSlotAndKeepsOriginalIntact()
    {
        var path = CreateSave();
        using var sp = Provider();
        var slot = await FirstSlot(sp);
        var session = await sp.GetRequiredService<IEditSessionHost>().OpenAsync(slot, null, default);
        session.Stage(SampleSave.FillTarget());

        var result = await session.ApplyAsync(new ApplyTarget.NewSlot("Novo save"), default);

        Assert.Equal(SampleSave.Text, File.ReadAllText(path));
        Assert.Equal("2", result.WrittenSlot.Id);
        Assert.Equal("Novo save", result.WrittenSlot.DisplayName);
        Assert.Contains("target: \"demo_co.city_c\"", File.ReadAllText(result.WrittenSlot.GameSiiPath));
        Assert.Contains("Novo save", File.ReadAllText(Path.Combine(Path.GetDirectoryName(result.WrittenSlot.GameSiiPath)!, "info.sii")));
        Assert.Equal(result.WrittenSlot, session.Save);
    }

    [Fact]
    public async Task EndToEnd_SaveChangedByTheGameAfterOpen_IsRefusedAndFileIsKept()
    {
        var path = CreateSave();
        using var sp = Provider();
        var slot = await FirstSlot(sp);
        var session = await sp.GetRequiredService<IEditSessionHost>().OpenAsync(slot, null, default);
        session.Stage(SampleSave.FillTarget());
        var changedByGame = SampleSave.Text.Replace("1000", "5000");
        File.WriteAllText(path, changedByGame);

        var e = await Assert.ThrowsAsync<CoreException>(() => session.ApplyAsync(new ApplyTarget.Overwrite(), default));

        Assert.Equal(CoreError.SaveChangedOnDisk, e.Code);
        Assert.Equal(changedByGame, File.ReadAllText(path));
        Assert.Empty(Directory.GetFiles(Path.GetDirectoryName(path)!, "game.sii.bak-*"));
    }

    [Fact]
    public async Task EndToEnd_Settings_RoundTripThroughRealStore()
    {
        using var sp = Provider();
        var settings = sp.GetRequiredService<IAppSettingsService>();

        await settings.UpdateAsync(settings.Current with { Theme = Domain.Settings.ThemePreference.Dark }, default);

        using var sp2 = Provider();
        var reloaded = sp2.GetRequiredService<IAppSettingsService>();
        await reloaded.LoadAsync(default);
        Assert.Equal(Domain.Settings.ThemePreference.Dark, reloaded.Current.Theme);
    }

    [Fact]
    public async Task GameData_WithoutInstalledGame_IsUnavailableNotAnError()
    {
        using var sp = Provider();

        var data = await sp.GetRequiredService<IGameDataService>().GetAsync(GameKind.Ets2, default);

        Assert.False(data.IsAvailable);
        Assert.Equal(GameFolderSource.NotFound, sp.GetRequiredService<IGameDataService>().GetFolder(GameKind.Ets2).Source);
    }

    private static async Task<Domain.Saves.SaveSlotInfo> FirstSlot(ServiceProvider sp)
    {
        var profiles = sp.GetRequiredService<IProfileService>();
        var profile = (await profiles.GetProfilesAsync(default)).Single();
        return (await profiles.GetSavesAsync(profile, default)).Single();
    }
}
