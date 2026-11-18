using OwnExpeditor.Data.Settings;
using OwnExpeditor.Domain.Settings;

namespace OwnExpeditor.Data.Tests;

public class JsonSettingsStoreTests : IDisposable
{
    private readonly TempDir _t = new();

    public void Dispose() => _t.Dispose();

    private JsonSettingsStore Store(string? sub = null) => new(sub is null ? _t.Path : _t.Combine(sub));

    private string SettingsFile => _t.Combine("settings.json");

    [Fact]
    public async Task LoadAsync_NoFile_ReturnsNullForFirstRun()
    {
        Assert.Null(await Store().LoadAsync(default));
    }

    [Fact]
    public async Task SaveAsync_ThenLoad_RoundTripsAllFieldsIncludingGameFolders()
    {
        var settings = new AppSettings(ThemePreference.Dark, Language.En, "/saves/1/game.sii",
            new GameFolders("/games/ets2", "/games/ats"));
        var store = Store();

        await store.SaveAsync(settings, default);
        var loaded = await Store().LoadAsync(default);

        Assert.Equal(settings, loaded);
    }

    [Fact]
    public async Task SaveAsync_NullGameFoldersAndLastSave_RoundTripAsNull()
    {
        var settings = new AppSettings(ThemePreference.Light, Language.PtBr, null, new GameFolders());

        await Store().SaveAsync(settings, default);

        Assert.Equal(settings, await Store().LoadAsync(default));
    }

    [Fact]
    public async Task SaveAsync_MissingDirectory_CreatesIt()
    {
        var store = Store("novo/sub");

        await store.SaveAsync(new AppSettings(ThemePreference.System, Language.En, null, new GameFolders()), default);

        Assert.True(File.Exists(_t.Combine("novo", "sub", "settings.json")));
    }

    [Fact]
    public async Task SaveAsync_Twice_OverwritesAndLeavesNoTmp()
    {
        var store = Store();
        await store.SaveAsync(new AppSettings(ThemePreference.Dark, Language.En, null, new GameFolders()), default);

        await store.SaveAsync(new AppSettings(ThemePreference.Light, Language.PtBr, null, new GameFolders()), default);

        Assert.Equal(ThemePreference.Light, (await store.LoadAsync(default))!.Theme);
        Assert.Empty(Directory.GetFiles(_t.Path, "*.tmp"));
    }

    [Theory]
    [InlineData("{ isto nao e json")]
    [InlineData("")]
    [InlineData("[]")]
    [InlineData("null")]
    [InlineData("{\"Theme\": ")]
    public async Task LoadAsync_CorruptJson_ReturnsDefaultsWithoutThrowing(string content)
    {
        File.WriteAllText(SettingsFile, content);

        var loaded = await Store().LoadAsync(default);

        Assert.NotNull(loaded);
        Assert.Equal(ThemePreference.System, loaded.Theme);
        Assert.Null(loaded.LastSavePath);
        Assert.Equal(new GameFolders(), loaded.GameFolders);
    }

    [Fact]
    public async Task LoadAsync_UnknownEnumValue_FallsBackOnlyForThatField()
    {
        File.WriteAllText(SettingsFile,
            "{\"theme\":\"Neon\",\"language\":\"En\",\"lastSavePath\":\"/x\",\"gameFolders\":{\"ets2\":\"/e\"}}");

        var loaded = await Store().LoadAsync(default);

        Assert.Equal(ThemePreference.System, loaded!.Theme);
        Assert.Equal(Language.En, loaded.Language);
        Assert.Equal("/x", loaded.LastSavePath);
        Assert.Equal(new GameFolders("/e", null), loaded.GameFolders);
    }

    [Fact]
    public async Task LoadAsync_FileWithoutGameFolders_ReturnsEmptyGameFolders()
    {
        File.WriteAllText(SettingsFile, "{\"theme\":\"Dark\",\"language\":\"PtBr\"}");

        var loaded = await Store().LoadAsync(default);

        Assert.Equal(ThemePreference.Dark, loaded!.Theme);
        Assert.Equal(new GameFolders(), loaded.GameFolders);
    }

    [Fact]
    public async Task SaveAsync_AfterCorruptFile_ReplacesItWithValidJson()
    {
        File.WriteAllText(SettingsFile, "{ lixo");
        var settings = new AppSettings(ThemePreference.Dark, Language.En, null, new GameFolders("/e"));

        await Store().SaveAsync(settings, default);

        Assert.Equal(settings, await Store().LoadAsync(default));
    }
}
