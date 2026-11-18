using OwnExpeditor.Core.Games;
using OwnExpeditor.Data;
using OwnExpeditor.Domain.Games;
using OwnExpeditor.Domain.Settings;

namespace OwnExpeditor.Core.Tests;

public class GameDataServiceTests
{
    private const string Dir = "/games/ets2";

    private sealed class Env
    {
        public FakeInstallLocator Locator { get; } = new();
        public FakeCatalogSource Source { get; } = new();
        public FakeAppSettings Settings { get; } = new();
        public GameDataService Service { get; }

        public Env(bool detected = true)
        {
            if (detected) Locator.Detected[GameKind.Ets2] = [Dir];
            Source.Catalog = Catalog();
            Source.Texts["en_us"] = Localization.FromText("key[0]: \"city_budapest\"\r\nval[0]: \"Budapest EN\"\r\nkey[1]: \"cargo_goods\"\r\nval[1]: \"Goods EN\"\r\n", "en_us");
            Source.Texts["pt_br"] = Localization.FromText("key[0]: \"city_budapest\"\r\nval[0]: \"Budapeste\"\r\nkey[1]: \"cargo_goods\"\r\nval[1]: \"Mercadorias\"\r\n", "pt_br");
            Service = new GameDataService(Locator, Source, Settings);
        }
    }

    private static GameCatalog Catalog()
    {
        var cat = new GameCatalog();
        var shipper = new CompanyDef("shipper_co") { DisplayName = "Shipper Co." };
        shipper.CargoOut.Add("goods");
        var receiver = new CompanyDef("receiver_co");
        receiver.CargoIn.Add("goods");
        cat.Companies["shipper_co"] = shipper;
        cat.Companies["receiver_co"] = receiver;
        cat.Cities["budapest"] = new CityDef("budapest") { NativeName = "Budapest Native", LocalizationKey = "@@city_budapest@@" };
        cat.Cities["native_only"] = new CityDef("native_only") { NativeName = "Nativeville", LocalizationKey = "@@missing_key@@" };
        cat.Cargos["goods"] = new CargoDef("goods") { LocalizationKey = "@@cargo_goods@@" };
        cat.Cargos["untranslated"] = new CargoDef("untranslated") { LocalizationKey = "@@missing_key@@" };
        return cat;
    }

    private static Task SetLanguage(Env env, Language language) =>
        env.Settings.UpdateAsync(env.Settings.Current with { Language = language }, default);

    [Fact]
    public async Task GetAsync_NoFolder_ReturnsUnavailableDataWithoutThrowing()
    {
        var env = new Env(detected: false);

        var data = await env.Service.GetAsync(GameKind.Ets2, default);

        Assert.False(data.IsAvailable);
        Assert.Equal(GameKind.Ets2, data.Game);
        Assert.Empty(env.Source.DefinitionCalls);
    }

    [Fact]
    public async Task GetAsync_NoFolder_NamesFallBackToReadableId()
    {
        var env = new Env(detected: false);

        var data = await env.Service.GetAsync(GameKind.Ets2, default);

        Assert.Equal("Plumb suppl", data.Get(GameNameKind.Cargo, "plumb_suppl"));
        Assert.Equal("Demo co", data.Get(GameNameKind.Company, "demo_co"));
        Assert.Empty(data.CargoOut("demo_co"));
        Assert.False(data.Ships("demo_co", "x"));
        Assert.False(data.Accepts("demo_co", "x"));
    }

    [Fact]
    public async Task GetAsync_DetectedFolder_LoadsDefinitionsAndAnswersRules()
    {
        var env = new Env();

        var data = await env.Service.GetAsync(GameKind.Ets2, default);

        Assert.True(data.IsAvailable);
        Assert.Equal([Dir], env.Source.DefinitionCalls);
        Assert.True(data.Ships("shipper_co", "goods"));
        Assert.True(data.Accepts("receiver_co", "goods"));
        Assert.False(data.Ships("receiver_co", "goods"));
        Assert.Equal(["goods"], data.CargoOut("shipper_co"));
        Assert.Empty(data.CargoOut("unknown"));
    }

    [Fact]
    public async Task GetAsync_CalledTwice_LoadsDefinitionsOnce()
    {
        var env = new Env();

        var first = await env.Service.GetAsync(GameKind.Ets2, default);
        var second = await env.Service.GetAsync(GameKind.Ets2, default);

        Assert.Same(first, second);
        Assert.Single(env.Source.DefinitionCalls);
    }

    [Fact]
    public async Task GetAsync_OtherGameWithoutFolder_IsUnavailableIndependently()
    {
        var env = new Env();

        var ats = await env.Service.GetAsync(GameKind.Ats, default);

        Assert.False(ats.IsAvailable);
        Assert.Equal(GameKind.Ats, ats.Game);
    }

    [Fact]
    public async Task GetAsync_CatalogLoadThrows_ReturnsUnavailableInsteadOfFailing()
    {
        var env = new Env();
        env.Source.DefinitionsThrow = new InvalidDataException("scs ruim");

        var data = await env.Service.GetAsync(GameKind.Ets2, default);

        Assert.False(data.IsAvailable);
    }

    [Fact]
    public async Task GetAsync_CatalogWithoutCompanies_IsUnavailable()
    {
        var env = new Env();
        env.Source.Catalog = new GameCatalog();

        Assert.False((await env.Service.GetAsync(GameKind.Ets2, default)).IsAvailable);
    }

    [Fact]
    public async Task GetAsync_AlreadyCancelled_ThrowsOperationCanceled()
    {
        var env = new Env();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => env.Service.GetAsync(GameKind.Ets2, cts.Token));
    }

    [Fact]
    public async Task Names_PortugueseApp_RequestsPtBrTextsAndTranslates()
    {
        var env = new Env();
        await SetLanguage(env, Language.PtBr);

        var data = await env.Service.GetAsync(GameKind.Ets2, default);

        Assert.Equal(["pt_br"], env.Source.TextCalls.Last().Languages);
        Assert.Equal("Budapeste", data.Get(GameNameKind.City, "budapest"));
        Assert.Equal("Mercadorias", data.Get(GameNameKind.Cargo, "goods"));
    }

    [Fact]
    public async Task Names_EnglishApp_RequestsEnUsThenEnGbTexts()
    {
        var env = new Env();

        var data = await env.Service.GetAsync(GameKind.Ets2, default);

        Assert.Equal(["en_us", "en_gb"], env.Source.TextCalls.Last().Languages);
        Assert.Equal("Budapest EN", data.Get(GameNameKind.City, "budapest"));
    }

    [Fact]
    public async Task Names_MissingTranslation_FallsBackToNativeNameThenReadableId()
    {
        var env = new Env();

        var data = await env.Service.GetAsync(GameKind.Ets2, default);

        Assert.Equal("Nativeville", data.Get(GameNameKind.City, "native_only"));
        Assert.Equal("Unknown city", data.Get(GameNameKind.City, "unknown_city"));
        Assert.Equal("Untranslated", data.Get(GameNameKind.Cargo, "untranslated"));
        Assert.Equal("Brand new cargo", data.Get(GameNameKind.Cargo, "brand_new_cargo"));
    }

    [Fact]
    public async Task Names_Company_UsesDefinitionNameThenReadableId()
    {
        var env = new Env();

        var data = await env.Service.GetAsync(GameKind.Ets2, default);

        Assert.Equal("Shipper Co.", data.Get(GameNameKind.Company, "shipper_co"));
        Assert.Equal("Receiver co", data.Get(GameNameKind.Company, "receiver_co"));
    }

    [Fact]
    public async Task Names_TruckAndTrailer_UseReadableId()
    {
        var env = new Env();

        var data = await env.Service.GetAsync(GameKind.Ets2, default);

        Assert.Equal("Mercedes actros", data.Get(GameNameKind.Truck, "mercedes_actros"));
        Assert.Equal("Scs box", data.Get(GameNameKind.Trailer, "scs_box"));
    }

    [Fact]
    public async Task LanguageChange_ReloadsOnlyTextsAndKeepsDefinitions()
    {
        var env = new Env();
        var before = await env.Service.GetAsync(GameKind.Ets2, default);
        var changed = new TaskCompletionSource<GameKind>();
        env.Service.Changed += (_, g) => changed.TrySetResult(g);

        await SetLanguage(env, Language.PtBr);
        Assert.Equal(GameKind.Ets2, await changed.Task.WaitAsync(TimeSpan.FromSeconds(5)));
        var after = await env.Service.GetAsync(GameKind.Ets2, default);

        Assert.Single(env.Source.DefinitionCalls);
        Assert.Equal(2, env.Source.TextCalls.Count);
        Assert.Equal("Budapest EN", before.Get(GameNameKind.City, "budapest"));
        Assert.Equal("Budapeste", after.Get(GameNameKind.City, "budapest"));
        Assert.True(after.Ships("shipper_co", "goods"));
    }

    [Fact]
    public async Task LanguageChange_GameNotLoadedYet_DoesNotLoadAnything()
    {
        var env = new Env();

        await SetLanguage(env, Language.PtBr);

        Assert.Empty(env.Source.DefinitionCalls);
        Assert.Empty(env.Source.TextCalls);
    }

    [Fact]
    public async Task SettingsChange_SameLanguage_DoesNotReload()
    {
        var env = new Env();
        await env.Service.GetAsync(GameKind.Ets2, default);

        await env.Settings.UpdateAsync(env.Settings.Current with { Theme = ThemePreference.Dark }, default);
        await env.Service.GetAsync(GameKind.Ets2, default);

        Assert.Single(env.Source.DefinitionCalls);
        Assert.Single(env.Source.TextCalls);
    }

    [Fact]
    public void GetFolder_NothingDetected_IsNotFound()
    {
        var env = new Env(detected: false);

        Assert.Equal(new GameFolderStatus(GameKind.Ets2, null, GameFolderSource.NotFound), env.Service.GetFolder(GameKind.Ets2));
    }

    [Fact]
    public void GetFolder_Detected_ReportsFirstDetectedFolder()
    {
        var env = new Env();
        env.Locator.Detected[GameKind.Ets2] = ["/a", "/b"];

        Assert.Equal(new GameFolderStatus(GameKind.Ets2, "/a", GameFolderSource.Detected), env.Service.GetFolder(GameKind.Ets2));
    }

    [Fact]
    public async Task SetManualFolder_Valid_SavesSettingsAndReportsManualSource()
    {
        var env = new Env();
        env.Locator.Checks[(GameKind.Ets2, "/mine")] = GameFolderCheck.Ok;

        var check = await env.Service.SetManualFolderAsync(GameKind.Ets2, "/mine", default);

        Assert.Equal(GameFolderCheck.Ok, check);
        Assert.Equal("/mine", env.Settings.Current.GameFolders.Ets2);
        Assert.Equal(new GameFolderStatus(GameKind.Ets2, "/mine", GameFolderSource.Manual), env.Service.GetFolder(GameKind.Ets2));
    }

    [Theory]
    [InlineData(GameFolderCheck.NotFound)]
    [InlineData(GameFolderCheck.NotAGameFolder)]
    [InlineData(GameFolderCheck.WrongGame)]
    public async Task SetManualFolder_Invalid_ReturnsCheckAndNeverWritesSettings(GameFolderCheck result)
    {
        var env = new Env();
        env.Locator.Checks[(GameKind.Ets2, "/bad")] = result;

        var check = await env.Service.SetManualFolderAsync(GameKind.Ets2, "/bad", default);

        Assert.Equal(result, check);
        Assert.Empty(env.Settings.Updates);
        Assert.Equal(GameFolderSource.Detected, env.Service.GetFolder(GameKind.Ets2).Source);
    }

    [Fact]
    public async Task SetManualFolder_Null_ClearsManualFolderAndReturnsToDetection()
    {
        var env = new Env();
        env.Locator.Checks[(GameKind.Ets2, "/mine")] = GameFolderCheck.Ok;
        await env.Service.SetManualFolderAsync(GameKind.Ets2, "/mine", default);

        var check = await env.Service.SetManualFolderAsync(GameKind.Ets2, null, default);

        Assert.Equal(GameFolderCheck.Ok, check);
        Assert.Null(env.Settings.Current.GameFolders.Ets2);
        Assert.Equal(GameFolderSource.Detected, env.Service.GetFolder(GameKind.Ets2).Source);
    }

    [Fact]
    public async Task SetManualFolder_KeepsTheOtherGameFolder()
    {
        var env = new Env();
        env.Locator.Checks[(GameKind.Ets2, "/mine")] = GameFolderCheck.Ok;
        env.Locator.Checks[(GameKind.Ats, "/ats")] = GameFolderCheck.Ok;
        await env.Service.SetManualFolderAsync(GameKind.Ats, "/ats", default);

        await env.Service.SetManualFolderAsync(GameKind.Ets2, "/mine", default);

        Assert.Equal(new GameFolders("/mine", "/ats"), env.Settings.Current.GameFolders);
    }

    [Fact]
    public void GetFolder_ManualFolderNoLongerValid_FallsBackToDetected()
    {
        var env = new Env();
        env.Settings.UpdateAsync(env.Settings.Current with { GameFolders = new GameFolders("/gone") }, default).Wait();

        var status = env.Service.GetFolder(GameKind.Ets2);

        Assert.Equal(new GameFolderStatus(GameKind.Ets2, Dir, GameFolderSource.Detected), status);
    }

    [Fact]
    public async Task FolderChange_RaisesChangedAndNextGetLoadsTheNewFolder()
    {
        var env = new Env();
        await env.Service.GetAsync(GameKind.Ets2, default);
        env.Locator.Checks[(GameKind.Ets2, "/mine")] = GameFolderCheck.Ok;
        var raised = new List<GameKind>();
        env.Service.Changed += (_, g) => raised.Add(g);

        await env.Service.SetManualFolderAsync(GameKind.Ets2, "/mine", default);
        await env.Service.GetAsync(GameKind.Ets2, default);

        Assert.Equal([GameKind.Ets2], raised);
        Assert.Equal([Dir, "/mine"], env.Source.DefinitionCalls);
    }

    [Fact]
    public async Task GetFolder_Manual_IsUsedForLoading()
    {
        var env = new Env(detected: false);
        env.Locator.Checks[(GameKind.Ets2, "/mine")] = GameFolderCheck.Ok;
        await env.Service.SetManualFolderAsync(GameKind.Ets2, "/mine", default);

        var data = await env.Service.GetAsync(GameKind.Ets2, default);

        Assert.True(data.IsAvailable);
        Assert.Equal(["/mine"], env.Source.DefinitionCalls);
    }
}
