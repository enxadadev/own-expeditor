using OwnExpeditor.Data.Maps;

namespace OwnExpeditor.Data.Tests.Maps;

public class MapIndexCacheTests
{
    private static string[] Files(string dir) => Directory.GetFiles(dir, "*", SearchOption.AllDirectories);

    [Fact]
    public async Task TryReadAsync_NothingWritten_ReturnsNull()
    {
        using var tmp = new TempDir();
        var cache = new MapIndexCache(tmp.Combine("cache"));

        var read = await cache.TryReadAsync("k1", CancellationToken.None);

        Assert.Null(read);
    }

    [Fact]
    public async Task TryReadAsync_AfterWriteWithSameKey_ReturnsEquivalentIndex()
    {
        using var tmp = new TempDir();
        var cache = new MapIndexCache(tmp.Combine("cache"));
        var index = MapTestData.SampleIndex();
        await cache.WriteAsync("k1", index, CancellationToken.None);

        var read = await cache.TryReadAsync("k1", CancellationToken.None);

        Assert.NotNull(read);
        MapTestData.AssertEquivalent(index, read);
    }

    [Fact]
    public async Task TryReadAsync_AfterWriteWithOtherKey_ReturnsNull()
    {
        using var tmp = new TempDir();
        var cache = new MapIndexCache(tmp.Combine("cache"));
        await cache.WriteAsync("k1", MapTestData.SampleIndex(), CancellationToken.None);

        var read = await cache.TryReadAsync("k2", CancellationToken.None);

        Assert.Null(read);
    }

    [Fact]
    public async Task TryReadAsync_ReaderOnNewInstance_ReadsWhatWasPersisted()
    {
        using var tmp = new TempDir();
        var dir = tmp.Combine("cache");
        await new MapIndexCache(dir).WriteAsync("k1", MapTestData.SampleIndex(), CancellationToken.None);

        var read = await new MapIndexCache(dir).TryReadAsync("k1", CancellationToken.None);

        Assert.NotNull(read);
    }

    [Fact]
    public async Task WriteAsync_Twice_LastWriteWins()
    {
        using var tmp = new TempDir();
        var cache = new MapIndexCache(tmp.Combine("cache"));
        var first = MapTestData.SampleIndex();
        var second = first with { Cities = [first.Cities[0]] };
        await cache.WriteAsync("k1", first, CancellationToken.None);
        await cache.WriteAsync("k1", second, CancellationToken.None);

        var read = await cache.TryReadAsync("k1", CancellationToken.None);

        Assert.Single(read!.Cities);
    }

    [Fact]
    public async Task WriteAsync_Completed_LeavesNoTmpFileAndCreatesDirectory()
    {
        using var tmp = new TempDir();
        var dir = tmp.Combine("cache", "map");
        var cache = new MapIndexCache(dir);

        await cache.WriteAsync("k1", MapTestData.SampleIndex(), CancellationToken.None);

        var files = Files(dir);
        Assert.NotEmpty(files);
        Assert.DoesNotContain(files, f => f.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task TryReadAsync_LeftoverTmpFromInterruptedWrite_IsIgnored()
    {
        using var tmp = new TempDir();
        var dir = tmp.Combine("cache");
        var cache = new MapIndexCache(dir);
        await cache.WriteAsync("k1", MapTestData.SampleIndex(), CancellationToken.None);
        File.WriteAllBytes(Path.Combine(dir, "interrupted.tmp"), [1, 2, 3]);

        var read = await cache.TryReadAsync("k1", CancellationToken.None);

        Assert.NotNull(read);
    }

    [Fact]
    public async Task TryReadAsync_OnlyTmpFileExists_ReturnsNull()
    {
        using var tmp = new TempDir();
        var dir = tmp.Dir("cache");
        File.WriteAllBytes(Path.Combine(dir, "map.tmp"), [1, 2, 3]);
        var cache = new MapIndexCache(dir);

        var read = await cache.TryReadAsync("k1", CancellationToken.None);

        Assert.Null(read);
    }

    [Fact]
    public async Task TryReadAsync_FileOverwrittenWithGarbage_ReturnsNullWithoutThrowing()
    {
        using var tmp = new TempDir();
        var dir = tmp.Combine("cache");
        var cache = new MapIndexCache(dir);
        await cache.WriteAsync("k1", MapTestData.SampleIndex(), CancellationToken.None);
        foreach (var f in Files(dir)) File.WriteAllBytes(f, [0xDE, 0xAD, 0xBE, 0xEF, 0x00, 0x01]);

        var read = await cache.TryReadAsync("k1", CancellationToken.None);

        Assert.Null(read);
    }

    [Fact]
    public async Task TryReadAsync_FileTruncatedInTheMiddle_ReturnsNullWithoutThrowing()
    {
        using var tmp = new TempDir();
        var dir = tmp.Combine("cache");
        var cache = new MapIndexCache(dir);
        await cache.WriteAsync("k1", MapTestData.SampleIndex(), CancellationToken.None);
        foreach (var f in Files(dir))
        {
            var bytes = File.ReadAllBytes(f);
            File.WriteAllBytes(f, bytes[..(bytes.Length / 2)]);
        }

        var read = await cache.TryReadAsync("k1", CancellationToken.None);

        Assert.Null(read);
    }

    [Fact]
    public async Task WriteAsync_AfterCorruption_RebuildsReadableCache()
    {
        using var tmp = new TempDir();
        var dir = tmp.Combine("cache");
        var cache = new MapIndexCache(dir);
        await cache.WriteAsync("k1", MapTestData.SampleIndex(), CancellationToken.None);
        foreach (var f in Files(dir)) File.WriteAllBytes(f, [0, 0, 0]);
        await cache.WriteAsync("k1", MapTestData.SampleIndex(), CancellationToken.None);

        var read = await cache.TryReadAsync("k1", CancellationToken.None);

        Assert.NotNull(read);
    }

    [Fact]
    public async Task WriteAsync_CancelledToken_ThrowsOperationCanceled()
    {
        using var tmp = new TempDir();
        var cache = new MapIndexCache(tmp.Combine("cache"));
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => cache.WriteAsync("k1", MapTestData.SampleIndex(), cts.Token));
    }

    [Fact]
    public async Task TryReadAsync_CancelledToken_ThrowsOperationCanceled()
    {
        using var tmp = new TempDir();
        var cache = new MapIndexCache(tmp.Combine("cache"));
        await cache.WriteAsync("k1", MapTestData.SampleIndex(), CancellationToken.None);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cache.TryReadAsync("k1", cts.Token));
    }
}
