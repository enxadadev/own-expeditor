using OwnExpeditor.Core.Maps;
using OwnExpeditor.Data.Maps;
using OwnExpeditor.Domain.Games;
using OwnExpeditor.Domain.Maps;
using OwnExpeditor.Domain.Saves;

namespace OwnExpeditor.Core.Tests.Maps;

public class GameMapProviderTests
{
    private static readonly ProfileInfo Profile = new(GameKind.Ets2, "p1", "Perfil", "/tmp/perfil-inexistente", false);

    private sealed class FakeMapService(MapLoadResult result) : IMapService
    {
        public List<(GameKind Game, ProfileInfo Profile, MapDetail Detail, CancellationToken Ct)> Calls { get; } = [];

        public event EventHandler<GameKind>? Changed { add { } remove { } }

        public Task<MapLoadResult> GetAsync(GameKind game, ProfileInfo profile, MapDetail detail, CancellationToken ct)
        {
            Calls.Add((game, profile, detail, ct));
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(result);
        }
    }

    [Fact]
    public async Task LoadCitiesAsync_Ready_ReturnsStatusAndIndexRequestingCitiesDetail()
    {
        var index = new MapIndex([], [], [], [], null);
        var fake = new FakeMapService(new MapLoadResult(MapStatus.Ready, index, ["mod_x"]));
        using var cts = new CancellationTokenSource();

        var result = await new GameMapProvider(fake).LoadCitiesAsync(GameKind.Ets2, Profile, null, cts.Token);

        Assert.Equal(MapStatus.Ready, result.Status);
        Assert.Same(index, result.Index);
        var call = Assert.Single(fake.Calls);
        Assert.Equal(MapDetail.Cities, call.Detail);
        Assert.Equal(GameKind.Ets2, call.Game);
        Assert.Same(Profile, call.Profile);
        Assert.Equal(cts.Token, call.Ct);
    }

    [Fact]
    public async Task LoadCitiesAsync_NoIndex_PropagatesStatusWithNullIndex()
    {
        var fake = new FakeMapService(new MapLoadResult(MapStatus.NoGameFolder, null, []));

        var result = await new GameMapProvider(fake).LoadCitiesAsync(GameKind.Ets2, Profile, null, CancellationToken.None);

        Assert.Equal(MapStatus.NoGameFolder, result.Status);
        Assert.Null(result.Index);
    }

    [Fact]
    public async Task LoadCitiesAsync_Cancelled_Throws()
    {
        var fake = new FakeMapService(new MapLoadResult(MapStatus.Ready, null, []));
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => new GameMapProvider(fake).LoadCitiesAsync(GameKind.Ets2, Profile, null, cts.Token));
    }
}
