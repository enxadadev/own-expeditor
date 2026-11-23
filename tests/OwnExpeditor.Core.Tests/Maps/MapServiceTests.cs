using OwnExpeditor.Core.Maps;
using OwnExpeditor.Data.Games;
using OwnExpeditor.Data.Maps;
using OwnExpeditor.Domain.Games;
using OwnExpeditor.Domain.Maps;
using OwnExpeditor.Domain.Saves;

namespace OwnExpeditor.Core.Tests.Maps;

public class MapServiceTests
{
    private static readonly ProfileInfo Profile = new(GameKind.Ets2, "p1", "Perfil", "/tmp/perfil-inexistente", false);

    private sealed class FakeSource(MapLoadResult result) : IMapSource
    {
        public List<MapRequest> Requests { get; } = [];
        public CancellationToken LastToken { get; private set; }

        public Task<MapLoadResult> LoadAsync(MapRequest request, IProgress<double>? progress, CancellationToken ct)
        {
            Requests.Add(request);
            LastToken = ct;
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(result);
        }
    }

    private sealed class FakeMods(params ModPackage[] mods) : IModLocator
    {
        public IReadOnlyList<ModPackage> Active(ProfileInfo profile) => mods;
    }

    private sealed class FakeInstalls(params string[] dirs) : IGameInstallLocator
    {
        public IReadOnlyList<string> Detect(GameKind game) => dirs;
        public GameFolderCheck Validate(GameKind game, string path) => GameFolderCheck.Ok;
    }

    private static MapLoadResult Ready() => new(MapStatus.Ready,
        new MapIndex([], [], [], [], null), ["mod_ignorado"]);

    [Fact]
    public async Task GetAsync_NoGameFolderDetected_ReturnsNoGameFolderWithoutCallingSource()
    {
        var source = new FakeSource(Ready());
        var service = new MapService(source, new FakeMods(), new FakeInstalls());

        var result = await service.GetAsync(GameKind.Ets2, Profile, MapDetail.Cities, CancellationToken.None);

        Assert.Equal(MapStatus.NoGameFolder, result.Status);
        Assert.Null(result.Index);
        Assert.Empty(source.Requests);
    }

    [Fact]
    public async Task GetAsync_GameFolderDetected_PassesGameFolderDetailAndModsToSource()
    {
        var mod = new ModPackage("mod_a", "/mods/a.scs", 1);
        var source = new FakeSource(Ready());
        var service = new MapService(source, new FakeMods(mod), new FakeInstalls("/jogo/ets2"));

        await service.GetAsync(GameKind.Ets2, Profile, MapDetail.Roads, CancellationToken.None);

        var request = Assert.Single(source.Requests);
        Assert.Equal(GameKind.Ets2, request.Game);
        Assert.Equal("/jogo/ets2", request.InstallDir);
        Assert.Equal(MapDetail.Roads, request.Detail);
        Assert.Equal([mod], request.Mods);
    }

    [Fact]
    public async Task GetAsync_SourceReturnsResult_ReturnsItUnchanged()
    {
        var expected = Ready();
        var service = new MapService(new FakeSource(expected), new FakeMods(), new FakeInstalls("/jogo/ets2"));

        var result = await service.GetAsync(GameKind.Ets2, Profile, MapDetail.Cities, CancellationToken.None);

        Assert.Same(expected, result);
    }

    [Theory]
    [InlineData(MapStatus.PartiallyRead)]
    [InlineData(MapStatus.UnsupportedVersion)]
    [InlineData(MapStatus.Failed)]
    public async Task GetAsync_SourceReturnsNonReadyStatus_RepassesStatus(MapStatus status)
    {
        var service = new MapService(new FakeSource(new MapLoadResult(status, null, [])),
            new FakeMods(), new FakeInstalls("/jogo/ets2"));

        var result = await service.GetAsync(GameKind.Ets2, Profile, MapDetail.Cities, CancellationToken.None);

        Assert.Equal(status, result.Status);
    }

    [Fact]
    public async Task GetAsync_CancelledToken_ThrowsOperationCanceled()
    {
        var service = new MapService(new FakeSource(Ready()), new FakeMods(), new FakeInstalls("/jogo/ets2"));
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.GetAsync(GameKind.Ets2, Profile, MapDetail.Cities, cts.Token));
    }

    [Fact]
    public async Task GetAsync_Called_PassesCancellationTokenToSource()
    {
        var source = new FakeSource(Ready());
        var service = new MapService(source, new FakeMods(), new FakeInstalls("/jogo/ets2"));
        using var cts = new CancellationTokenSource();

        await service.GetAsync(GameKind.Ets2, Profile, MapDetail.Cities, cts.Token);

        Assert.Equal(cts.Token, source.LastToken);
    }
}
