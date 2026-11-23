using System.Buffers.Binary;
using OwnExpeditor.Data.Maps;
using OwnExpeditor.Domain.Maps;

namespace OwnExpeditor.Data.Tests.Maps;

public class MapIndexCacheRobustnessTests
{
    private static MapIndex LargeIndex(int cities, int companies, int nodes, int edges)
    {
        var graphNodes = Enumerable.Range(0, nodes).Select(i => new MapPos(i, -i)).ToArray();
        var graphEdges = Enumerable.Range(0, edges)
            .Select(i => new MapEdge(i % nodes, (i + 1) % nodes, 10f + i, 1f, MapRoadClass.Main, i))
            .ToArray();
        return new MapIndex(
            Cities: [.. Enumerable.Range(0, cities).Select(i => new MapCity($"c{i}", new MapPos(i, i), null, null, []))],
            Countries: [],
            Companies: [.. Enumerable.Range(0, companies).Select(i => new MapCompany($"co{i}", "c0", new MapPos(i, 0), null))],
            Roads: [],
            Graph: new MapGraph(graphNodes, graphEdges));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(50)]
    public async Task TryReadAsync_GraphWithSeveralEdges_RoundTrips(int edges)
    {
        using var tmp = new TempDir();
        var cache = new MapIndexCache(tmp.Combine("cache"));
        var index = LargeIndex(cities: 1, companies: 1, nodes: 4, edges: edges);
        await cache.WriteAsync("k1", index, CancellationToken.None);

        var read = await cache.TryReadAsync("k1", CancellationToken.None);

        Assert.NotNull(read);
        Assert.Equal(index.Graph!.Edges, read.Graph!.Edges);
        Assert.Equal(index.Graph.Nodes, read.Graph.Nodes);
    }

    [Fact]
    public async Task TryReadAsync_ManyMinimalCitiesAndCompanies_RoundTrips()
    {
        using var tmp = new TempDir();
        var cache = new MapIndexCache(tmp.Combine("cache"));
        var index = LargeIndex(cities: 40, companies: 40, nodes: 3, edges: 5);
        await cache.WriteAsync("k1", index, CancellationToken.None);

        var read = await cache.TryReadAsync("k1", CancellationToken.None);

        Assert.NotNull(read);
        Assert.Equal(40, read.Cities.Count);
        Assert.Equal(40, read.Companies.Count);
    }

    [Fact]
    public async Task TryReadAsync_OtherSchemaVersion_IsDiscarded()
    {
        using var tmp = new TempDir();
        var dir = tmp.Combine("cache");
        var cache = new MapIndexCache(dir);
        await cache.WriteAsync("k1", MapTestData.SampleIndex(), CancellationToken.None);
        var file = Assert.Single(Directory.GetFiles(dir, "*.idx"));
        var bytes = File.ReadAllBytes(file);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(4), MapIndexCache.SchemaVersion + 1);   // logo após o magic
        File.WriteAllBytes(file, bytes);

        var read = await cache.TryReadAsync("k1", CancellationToken.None);

        Assert.Null(read);
    }

    [Fact]
    public async Task WriteAsync_TwoConcurrentWritesOnSameKey_BothCompleteAndCacheIsReadable()
    {
        using var tmp = new TempDir();
        var dir = tmp.Combine("cache");
        var cache = new MapIndexCache(dir);

        await Task.WhenAll(
            Enumerable.Range(0, 2).Select(_ => Task.Run(() => cache.WriteAsync("k1", MapTestData.SampleIndex(), CancellationToken.None))));

        var read = await cache.TryReadAsync("k1", CancellationToken.None);
        Assert.NotNull(read);
        Assert.Empty(Directory.GetFiles(dir, "*.tmp"));
        Assert.Single(Directory.GetFiles(dir, "*.idx"));
    }

    [Fact]
    public async Task WriteAsync_DirectoryWithDotDotSegments_StillKeepsThreeIndexes()
    {
        using var tmp = new TempDir();
        tmp.Dir("cache");
        var cache = new MapIndexCache(Path.Combine(tmp.Path, "cache", "..", "cache"));
        for (var i = 1; i <= 5; i++)
        {
            await cache.WriteAsync($"ets2|v{i}", MapTestData.SampleIndex(), CancellationToken.None);
            await Task.Delay(30);
        }

        Assert.Equal(MapIndexCache.KeepPerGroup, Directory.GetFiles(tmp.Combine("cache"), "*.idx").Length);
        Assert.NotNull(await cache.TryReadAsync("ets2|v5", CancellationToken.None));
    }

    [Fact]
    public async Task WriteAsync_StaleTmpFiles_AreRemovedButRecentOnesKept()
    {
        using var tmp = new TempDir();
        var dir = tmp.Dir("cache");
        var old = Path.Combine(dir, "old.tmp");
        var recent = Path.Combine(dir, "recent.tmp");
        File.WriteAllBytes(old, [1]);
        File.WriteAllBytes(recent, [1]);
        File.SetLastWriteTimeUtc(old, DateTime.UtcNow.AddDays(-2));
        var cache = new MapIndexCache(dir);

        await cache.WriteAsync("k1", MapTestData.SampleIndex(), CancellationToken.None);

        Assert.False(File.Exists(old));
        Assert.True(File.Exists(recent));
    }
}
