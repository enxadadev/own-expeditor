using System.Globalization;
using OwnExpeditor.Core.Settings;
using OwnExpeditor.Domain.Games;
using OwnExpeditor.Domain.Settings;

namespace OwnExpeditor.Core.Tests;

public class AppSettingsServiceTests
{
    private sealed class Env(string culture = "en-US")
    {
        public FakeSettingsStore Store { get; } = new();
        public FakeInstallLocator Locator { get; } = new();
        public List<AppSettings> Events { get; } = [];

        public AppSettingsService Create()
        {
            var service = new AppSettingsService(Store, Locator, () => CultureInfo.GetCultureInfo(culture));
            service.Changed += (_, s) => Events.Add(s);
            return service;
        }
    }

    private static AppSettings Settings(ThemePreference theme = ThemePreference.Dark, Language language = Language.PtBr,
        string? last = "/saves/x", GameFolders? folders = null) =>
        new(theme, language, last, folders ?? new GameFolders());

    [Theory]
    [InlineData("pt-BR", Language.PtBr)]
    [InlineData("pt-PT", Language.PtBr)]
    [InlineData("pt", Language.PtBr)]
    [InlineData("en-US", Language.En)]
    [InlineData("de-DE", Language.En)]
    public void Current_BeforeLoad_UsesDefaultsWithLanguageFromSystemCulture(string culture, Language expected)
    {
        var current = new Env(culture).Create().Current;

        Assert.Equal(ThemePreference.System, current.Theme);
        Assert.Equal(expected, current.Language);
        Assert.Null(current.LastSavePath);
        Assert.Equal(new GameFolders(), current.GameFolders);
    }

    [Fact]
    public async Task LoadAsync_FirstRun_KeepsDefaultsWithoutWritingOrRaising()
    {
        var env = new Env();
        var service = env.Create();
        var defaults = service.Current;

        await service.LoadAsync(default);

        Assert.Equal(defaults, service.Current);
        Assert.Empty(env.Store.Saved);
        Assert.Empty(env.Events);
    }

    [Fact]
    public async Task LoadAsync_StoredSettings_BecomeCurrentAndRaiseChanged()
    {
        var env = new Env { Store = { Stored = Settings() } };
        var service = env.Create();

        await service.LoadAsync(default);

        Assert.Equal(Settings(), service.Current);
        Assert.Equal([Settings()], env.Events);
    }

    [Fact]
    public async Task UpdateAsync_Valid_PersistsSetsCurrentAndRaisesChanged()
    {
        var env = new Env();
        var service = env.Create();

        await service.UpdateAsync(Settings(), default);

        Assert.Equal([Settings()], env.Store.Saved);
        Assert.Equal(Settings(), service.Current);
        Assert.Equal([Settings()], env.Events);
    }

    [Fact]
    public async Task UpdateAsync_SameAsCurrent_DoesNotWriteOrRaise()
    {
        var env = new Env();
        var service = env.Create();
        await service.UpdateAsync(Settings(), default);

        await service.UpdateAsync(Settings(), default);

        Assert.Single(env.Store.Saved);
        Assert.Single(env.Events);
    }

    [Fact]
    public async Task UpdateAsync_NewManualFolderValid_IsSaved()
    {
        var env = new Env();
        env.Locator.Checks[(GameKind.Ets2, "/game")] = GameFolderCheck.Ok;
        var service = env.Create();

        await service.UpdateAsync(Settings(folders: new GameFolders("/game")), default);

        Assert.Equal("/game", env.Store.Saved.Single().GameFolders.Ets2);
    }

    [Theory]
    [InlineData(GameFolderCheck.NotFound)]
    [InlineData(GameFolderCheck.NotAGameFolder)]
    [InlineData(GameFolderCheck.WrongGame)]
    public async Task UpdateAsync_NewManualFolderInvalid_ThrowsInvalidEditAndNeverWrites(GameFolderCheck check)
    {
        var env = new Env();
        env.Locator.Checks[(GameKind.Ets2, "/bad")] = check;
        var service = env.Create();
        var before = service.Current;

        var e = await Assert.ThrowsAsync<CoreException>(
            () => service.UpdateAsync(Settings(folders: new GameFolders("/bad")), default));

        Assert.Equal(CoreError.InvalidEdit, e.Code);
        Assert.Empty(env.Store.Saved);
        Assert.Equal(before, service.Current);
        Assert.Empty(env.Events);
    }

    [Fact]
    public async Task UpdateAsync_InvalidAtsFolder_IsAlsoRefused()
    {
        var env = new Env();
        var service = env.Create();

        var e = await Assert.ThrowsAsync<CoreException>(
            () => service.UpdateAsync(Settings(folders: new GameFolders(Ats: "/bad")), default));

        Assert.Equal(CoreError.InvalidEdit, e.Code);
        Assert.Empty(env.Store.Saved);
    }

    [Fact]
    public async Task UpdateAsync_UnchangedFolderThatBecameInvalid_IsNotRevalidated()
    {
        var env = new Env { Store = { Stored = Settings(folders: new GameFolders("/was-valid")) } };
        var service = env.Create();
        await service.LoadAsync(default);

        await service.UpdateAsync(service.Current with { Theme = ThemePreference.Light }, default);

        Assert.Equal(ThemePreference.Light, env.Store.Saved.Single().Theme);
        Assert.Equal("/was-valid", env.Store.Saved.Single().GameFolders.Ets2);
    }

    [Fact]
    public async Task UpdateAsync_ClearingFolder_IsNotValidated()
    {
        var env = new Env { Store = { Stored = Settings(folders: new GameFolders("/was-valid")) } };
        var service = env.Create();
        await service.LoadAsync(default);

        await service.UpdateAsync(service.Current with { GameFolders = new GameFolders() }, default);

        Assert.Null(env.Store.Saved.Single().GameFolders.Ets2);
    }

    [Fact]
    public async Task UpdateAsync_StoreFailsWithIo_ThrowsWriteFailedAndKeepsCurrent()
    {
        var env = new Env { Store = { SaveThrows = new IOException("disco") } };
        var service = env.Create();
        var before = service.Current;

        var e = await Assert.ThrowsAsync<CoreException>(() => service.UpdateAsync(Settings(), default));

        Assert.Equal(CoreError.WriteFailed, e.Code);
        Assert.Equal(before, service.Current);
        Assert.Empty(env.Events);
    }

    [Fact]
    public async Task UpdateAsync_StoreDenied_ThrowsAccessDenied()
    {
        var env = new Env { Store = { SaveThrows = new UnauthorizedAccessException() } };
        var service = env.Create();

        var e = await Assert.ThrowsAsync<CoreException>(() => service.UpdateAsync(Settings(), default));

        Assert.Equal(CoreError.AccessDenied, e.Code);
    }

    [Fact]
    public async Task UpdateAsync_Null_ThrowsArgumentNull() =>
        await Assert.ThrowsAsync<ArgumentNullException>(() => new Env().Create().UpdateAsync(null!, default));
}
