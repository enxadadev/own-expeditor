using OwnExpeditor.Data.Maps;
using OwnExpeditor.Data.Tests.Scs;
using OwnExpeditor.Domain.Games;
using OwnExpeditor.Domain.Maps;

namespace OwnExpeditor.Data.Tests.Maps;

/// <summary>Pacotes .scs corrompidos nunca derrubam o carregamento: viram <see cref="MapStatus.Failed"/>.</summary>
public class MapSourceCorruptPackageTests
{
    private sealed class ThrowingFs(Exception error, bool onList) : IScsFileSystem
    {
        public Stream? Open(string path) => onList ? null : throw error;
        public IEnumerable<string> List(string dir) => onList ? throw error : [];
    }

    private static string Install(TempDir tmp, byte[] baseScs)
    {
        var dir = tmp.Dir("game");
        File.WriteAllBytes(Path.Combine(dir, "base.scs"), baseScs);
        return dir;
    }

    private static MapRequest Req(string dir) => new(GameKind.Ets2, dir, [], MapDetail.Cities);

    private static async Task AssertFailedAndNoCache(TempDir tmp, MapSource source, MapRequest request)
    {
        var result = await source.LoadAsync(request, null, CancellationToken.None);

        Assert.Equal(MapStatus.Failed, result.Status);
        Assert.Null(result.Index);
        Assert.Empty(Directory.Exists(tmp.Combine("cache")) ? Directory.GetFiles(tmp.Combine("cache"), "*.idx") : []);
    }

    [Fact]
    public async Task LoadAsync_PackageWithCorruptMapDirectory_ReturnsFailed()
    {
        using var tmp = new TempDir();
        var scs = new ScsArchiveBuilder().DirectoryRaw("map", BitConverter.GetBytes(1_000_000)).Build();
        var dir = Install(tmp, scs);

        await AssertFailedAndNoCache(tmp, new MapSource(tmp.Combine("cache")), Req(dir));
    }

    [Fact]
    public async Task LoadAsync_PackageWithNameLengthsBeyondContent_ReturnsFailed()
    {
        using var tmp = new TempDir();
        byte[] content = [.. BitConverter.GetBytes(1), 250, (byte)'x'];
        var dir = Install(tmp, new ScsArchiveBuilder().DirectoryRaw("map", content).Build());

        await AssertFailedAndNoCache(tmp, new MapSource(tmp.Combine("cache")), Req(dir));
    }

    [Theory]
    [InlineData(ScsArchiveBuilder.EntryCountAt, 0x7FFFFFFF)]
    [InlineData(ScsArchiveBuilder.EntryTableSizeAt, 0x7FFFFFF0)]
    [InlineData(ScsArchiveBuilder.MetaWordCountAt, -1)]
    public async Task LoadAsync_PackageWithCorruptHeader_ReturnsFailed(int field, int value)
    {
        using var tmp = new TempDir();
        var scs = ScsArchiveBuilder.PatchInt32(new ScsArchiveBuilder().Directory("map", [], []).Build(), field, value);
        var dir = Install(tmp, scs);

        await AssertFailedAndNoCache(tmp, new MapSource(tmp.Combine("cache")), Req(dir));
    }

    [Fact]
    public async Task LoadAsync_PackageTruncatedAfterHeader_ReturnsFailed()
    {
        using var tmp = new TempDir();
        var scs = new ScsArchiveBuilder().Directory("map", [], []).Build();
        var dir = Install(tmp, scs[..(ScsArchiveBuilder.HeaderSize + 4)]);

        await AssertFailedAndNoCache(tmp, new MapSource(tmp.Combine("cache")), Req(dir));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task LoadAsync_FileSystemThrowsArgumentOutOfRange_ReturnsFailed(bool onList)
    {
        using var tmp = new TempDir();
        var dir = tmp.Dir("game");
        var source = new MapSource(tmp.Combine("cache"), _ => new ThrowingFs(new ArgumentOutOfRangeException("bytes"), onList));

        await AssertFailedAndNoCache(tmp, source, Req(dir));
    }

    [Fact]
    public async Task LoadAsync_FileSystemListThrowsInvalidData_ReturnsFailed()
    {
        using var tmp = new TempDir();
        var dir = tmp.Dir("game");
        var source = new MapSource(tmp.Combine("cache"), _ => new ThrowingFs(new InvalidDataException("lista corrompida"), onList: true));

        await AssertFailedAndNoCache(tmp, source, Req(dir));
    }

    [Fact]
    public async Task ReadAsync_ListingSectorsThrowsInvalidData_ReturnsFailedWithoutThrowing()
    {
        var fs = new ThrowingFs(new InvalidDataException("lista corrompida"), onList: true);

        var result = await new MapSectorReader().ReadAsync(fs, MapDetail.Cities, null, CancellationToken.None);

        Assert.Equal(MapStatus.Failed, result.Status);
        Assert.Null(result.Index);
    }
}
