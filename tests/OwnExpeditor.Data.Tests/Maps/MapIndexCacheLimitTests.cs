using OwnExpeditor.Data.Maps;

namespace OwnExpeditor.Data.Tests.Maps;

public class MapIndexCacheLimitTests
{
    [Fact]
    public async Task WriteAsync_MoreThanThreeKeysOfSameGame_KeepsOnlyTheThreeMostRecent()
    {
        using var tmp = new TempDir();
        var cache = new MapIndexCache(tmp.Combine("cache"));
        for (var i = 1; i <= 5; i++)
        {
            await cache.WriteAsync($"ets2|v{i}", MapTestData.SampleIndex(), CancellationToken.None);
            await Task.Delay(30);
        }

        Assert.Equal(3, Directory.GetFiles(cache.Directory, "*.idx").Length);
        Assert.Null(await cache.TryReadAsync("ets2|v1", CancellationToken.None));
        Assert.Null(await cache.TryReadAsync("ets2|v2", CancellationToken.None));
        Assert.NotNull(await cache.TryReadAsync("ets2|v3", CancellationToken.None));
        Assert.NotNull(await cache.TryReadAsync("ets2|v5", CancellationToken.None));
    }

    [Fact]
    public async Task WriteAsync_OtherGame_DoesNotEvictFirstGameIndexes()
    {
        using var tmp = new TempDir();
        var cache = new MapIndexCache(tmp.Combine("cache"));
        for (var i = 1; i <= 3; i++)
        {
            await cache.WriteAsync($"ets2|v{i}", MapTestData.SampleIndex(), CancellationToken.None);
            await Task.Delay(30);
        }

        await cache.WriteAsync("ats|v1", MapTestData.SampleIndex(), CancellationToken.None);

        Assert.NotNull(await cache.TryReadAsync("ets2|v1", CancellationToken.None));
        Assert.NotNull(await cache.TryReadAsync("ats|v1", CancellationToken.None));
    }
}
