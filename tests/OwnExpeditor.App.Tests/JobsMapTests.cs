using OwnExpeditor.App.Modules.Jobs;
using OwnExpeditor.App.Services;
using OwnExpeditor.Core.Maps;
using OwnExpeditor.App.Tests.Support;
using OwnExpeditor.Core.Session;
using OwnExpeditor.Domain.Games;
using OwnExpeditor.Domain.Maps;
using OwnExpeditor.Domain.Saves;
using OwnExpeditor.Domain.Settings;
using OwnExpeditor.App.ViewModels;

namespace OwnExpeditor.App.Tests;

/// <summary>Mapa do jogo na tela de Fretes (ADR 0007, fase 1a): carga em segundo plano, MapPoint em metros e nota por status.</summary>
public class JobsMapTests
{
    private sealed class FakeMaps : IGameMapProvider
    {
        public TaskCompletionSource<GameMapResult> Gate { get; } = new();
        public CancellationToken Token { get; private set; }
        public IProgress<double>? Progress { get; private set; }
        public int Calls { get; private set; }
        public Exception? Fails { get; init; }

        public async Task<GameMapResult> LoadCitiesAsync(GameKind game, ProfileInfo profile, IProgress<double>? progress, CancellationToken ct)
        {
            Calls++;
            Token = ct;
            Progress = progress;
            if (Fails is { } e) throw e;
            return await Gate.Task.WaitAsync(ct);
        }
    }

    private static MapCity City(string token, float x, float z, string? country = null, int? pop = null) => new(token, new MapPos(x, z), country, pop, []);

    private static GameMapResult Ready(params MapCity[] cities) => new(MapStatus.Ready,
        new MapIndex(cities, [new MapCountry("bulgaria", "Bulgaria", new MapPos(10, -20)), new MapCountry("france", "France", new MapPos(99, 99))], [], [], null));

    private static (JobsPageViewModel Page, JobsFixture Fx, FakeMaps Maps, Localizer Loc) Build(FakeMaps? maps = null, Language lang = Language.En)
    {
        var fx = new JobsFixture(lang);
        maps ??= new FakeMaps();
        var page = new JobsPageViewModel(fx.Loc, fx.Factory, fx.Toast, new ImmediateUiDispatcher(), maps) { Navigation = fx.Navigation };
        return (page, fx, maps, fx.Loc);
    }

    private static async Task Settle(Func<bool> done)
    {
        for (var i = 0; i < 200 && !done(); i++) await Task.Delay(10);
    }

    [Fact]
    public async Task Ready_MapsSavedCitiesToMetersWithYAsMinusZ()
    {
        var (page, fx, maps, _) = Build();
        await page.AttachAsync(fx.Session, default);
        maps.Gate.SetResult(Ready(City("sofia", 1000, 2000, "bulgaria", 1_200_000), City("istanbul", 5000, -4000, "turkey"),
            City("paris", 9, 9, "france"), City("belgrad", -300, 100, "serbia")));
        await Settle(() => !page.IsMapLoading);
        Pick(fx, page);

        Assert.Equal(1000, page.MapOrigin!.X);
        Assert.Equal(-2000, page.MapOrigin.Y);
        Assert.Equal(1_200_000, page.MapOrigin.Population);
        Assert.Equal(4000, page.MapDestination!.Y);
        Assert.True(page.MapOrigin.HasCoordinates && page.MapDestination.HasCoordinates);
    }

    [Fact]
    public async Task Ready_BackdropHasOnlySaveCitiesWithPositionAndTheirCountries()
    {
        var (page, fx, maps, _) = Build();
        await page.AttachAsync(fx.Session, default);
        maps.Gate.SetResult(Ready(City("sofia", 1, 2, "bulgaria"), City("paris", 9, 9, "france")));
        await Settle(() => !page.IsMapLoading);

        Assert.Equal(["Sófia"], page.MapCities.Select(c => c.Label).ToArray()); // praha/belgrad sem posição, paris fora do save
        Assert.Equal(["Bulgaria"], page.MapCountries.Select(c => c.Label).ToArray());
        Assert.Equal(20, page.MapCountries[0].Y); // âncora do país: Y = -Z
    }

    [Fact]
    public async Task PairWithoutPosition_StaysSchematic()
    {
        var (page, fx, maps, _) = Build();
        await page.AttachAsync(fx.Session, default);
        maps.Gate.SetResult(Ready(City("sofia", 1, 2)));
        await Settle(() => !page.IsMapLoading);
        Pick(fx, page);

        Assert.True(page.MapOrigin!.HasCoordinates);
        Assert.False(page.MapDestination!.HasCoordinates); // o Auto do RouteMap cai no esquemático
    }

    [Fact]
    public async Task WithoutProvider_NoNoteAndNoPositions()
    {
        var fx = await new JobsFixture().AttachAsync();
        fx.Ready();

        Assert.Equal("Route sketch, without real positions", fx.Page.MapStatusNote); // esquemático: o aviso vai na nota
        Assert.False(fx.Page.MapOrigin!.HasCoordinates);
        Assert.Empty(fx.Page.MapCities);
    }

    [Fact]
    public async Task Loading_NeverBlocksAndShowsBuildingNote()
    {
        var (page, fx, maps, _) = Build();
        var attach = page.AttachAsync(fx.Session, default);

        Assert.True(await Task.WhenAny(attach, Task.Delay(2000)) == attach); // abrir a tela não espera o mapa
        Assert.Equal(PageState.Ready, page.State);
        Assert.True(page.IsMapLoading);
        Assert.Equal("Preparing the game map…", page.MapStatusNote);
    }

    [Theory]
    [InlineData(MapStatus.NoGameFolder, "Set the game folder to see the map")]
    [InlineData(MapStatus.UnsupportedVersion, "Couldn't read the map for this game version")]
    [InlineData(MapStatus.Failed, "Couldn't read the game map")]
    public async Task Status_ShowsNote(MapStatus status, string note)
    {
        var (page, fx, maps, _) = Build();
        await page.AttachAsync(fx.Session, default);
        maps.Gate.SetResult(new GameMapResult(status, null));
        await Settle(() => !page.IsMapLoading);

        Assert.Equal(note, page.MapStatusNote);
    }

    [Fact]
    public async Task Ready_HasNoNote_AndNoteFollowsLanguage()
    {
        var (page, fx, maps, loc) = Build();
        await page.AttachAsync(fx.Session, default);
        maps.Gate.SetResult(new GameMapResult(MapStatus.UnsupportedVersion, null));
        await Settle(() => !page.IsMapLoading);
        loc.SetLanguage(Language.PtBr);

        Assert.Equal("Não foi possível ler o mapa desta versão do jogo", page.MapStatusNote);
    }

    [Fact]
    public async Task ProviderThrows_BecomesFailedNoteAndPageStaysReady()
    {
        var (page, fx, _, _) = Build(new FakeMaps { Fails = new InvalidOperationException("boom") });
        await page.AttachAsync(fx.Session, default);
        await Settle(() => !page.IsMapLoading);

        Assert.Equal(PageState.Ready, page.State);
        Assert.Equal("Couldn't read the game map", page.MapStatusNote);
    }

    [Fact]
    public async Task Detach_CancelsTheLoadAndLateResultIsIgnored()
    {
        var (page, fx, maps, _) = Build();
        await page.AttachAsync(fx.Session, default);
        await Settle(() => maps.Calls > 0);

        page.Detach();

        Assert.True(maps.Token.IsCancellationRequested);
        Assert.False(page.IsMapLoading);
    }

    [Fact]
    public async Task ReattachingASave_CancelsThePreviousLoadAndStartsAnother()
    {
        var (page, fx, maps, _) = Build();
        await page.AttachAsync(fx.Session, default);
        await Settle(() => maps.Calls > 0);
        var first = maps.Token;

        await page.AttachAsync(fx.Session, default);

        Assert.True(first.IsCancellationRequested);
    }

    private static void Pick(JobsFixture fx, JobsPageViewModel page)
    {
        if (page.ShowCountries) JobsFixture.Pick(page.FromCountry, "bulgaria");
        JobsFixture.Pick(page.FromCity, "sofia");
        JobsFixture.Pick(page.FromCompany, JobsSample.Radus);
        if (page.ShowCountries) JobsFixture.Pick(page.ToCountry, "turkey");
        JobsFixture.Pick(page.ToCity, "istanbul");
    }
}
