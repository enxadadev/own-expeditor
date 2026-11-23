using OwnExpeditor.Data.Maps;
using OwnExpeditor.Domain.Maps;

namespace OwnExpeditor.Data.Tests.Maps;

public class MapSectorReaderTests
{
    private static Task<MapLoadResult> ReadAsync(FakeScsFileSystem fs, CancellationToken ct = default)
        => new MapSectorReader().ReadAsync(fs, MapDetail.Cities, null, ct);

    private static SectorBuilder GoodSector(int i)
        => new SectorBuilder()
            .Overlay().Road().Service(3)
            .City($"c{i}", 1, 100, 100)
            .Node(1, i * 4000 + 1000f, 1000f);

    /// <summary>20 setores bons (índices 0..19); os de <paramref name="bad"/> são trocados por dados ruins.</summary>
    private static FakeScsFileSystem Map(int total, params (int Index, byte[] Data)[] bad)
    {
        var fs = new FakeScsFileSystem();
        for (var i = 0; i < total; i++)
            fs.AddSector(i, 0, bad.Any(b => b.Index == i) ? bad.First(b => b.Index == i).Data : GoodSector(i).Build());
        return fs;
    }

    // ---- City ----

    [Fact]
    public async Task ReadAsync_CitySector_ReturnsTokenAndPositionFromNode()
    {
        var sector = new SectorBuilder()
            .City("city_a", 11)
            .Node(11, 100.5f, 2000.25f)
            .Build();
        var fs = new FakeScsFileSystem().AddSector(0, 0, sector);

        var result = await ReadAsync(fs);

        Assert.Equal(MapStatus.Ready, result.Status);
        var city = Assert.Single(result.Index!.Cities);
        Assert.Equal("city_a", city.Token);
        Assert.Equal(new MapPos(100.5f, 2000.25f), city.Center);
    }

    [Fact]
    public async Task ReadAsync_CityInNegativeSector_ReadsSignedSectorName()
    {
        var sector = new SectorBuilder().City("city_n", 5).Node(5, -3900.5f, -100.75f).Build();
        var fs = new FakeScsFileSystem().AddSector(-1, -1, sector);

        var result = await ReadAsync(fs);

        var city = Assert.Single(result.Index!.Cities);
        Assert.Equal(new MapPos(-3900.5f, -100.75f), city.Center);
    }

    [Fact]
    public async Task ReadAsync_CityAfterVariableSizeItems_MeasuresItemsAndFindsCity()
    {
        var sector = new SectorBuilder()
            .Overlay().Service(0).Road().Service(5).Overlay()
            .City("city_a", 11)
            .Node(11, 500f, 600f)
            .Build();
        var fs = new FakeScsFileSystem().AddSector(0, 0, sector);

        var result = await ReadAsync(fs);

        Assert.Equal(MapStatus.Ready, result.Status);
        Assert.Equal("city_a", Assert.Single(result.Index!.Cities).Token);
    }

    [Fact]
    public async Task ReadAsync_SameTokenVisibleAndHidden_PicksVisibleItemEvenIfSmaller()
    {
        var sector = new SectorBuilder()
            .City("city_a", 11, width: 5000, height: 5000, hidden: true)
            .City("city_a", 12, width: 100, height: 100, hidden: false)
            .Node(11, 100f, 100f)
            .Node(12, 2000f, 3000f)
            .Build();
        var fs = new FakeScsFileSystem().AddSector(0, 0, sector);

        var result = await ReadAsync(fs);

        var city = Assert.Single(result.Index!.Cities);
        Assert.Equal(new MapPos(2000f, 3000f), city.Center);
    }

    [Fact]
    public async Task ReadAsync_SameTokenAllHidden_PicksLargestArea()
    {
        var sector = new SectorBuilder()
            .City("city_a", 11, width: 100, height: 100, hidden: true)
            .City("city_a", 12, width: 400, height: 300, hidden: true)
            .City("city_a", 13, width: 200, height: 200, hidden: true)
            .Node(11, 100f, 100f)
            .Node(12, 2000f, 3000f)
            .Node(13, 300f, 300f)
            .Build();
        var fs = new FakeScsFileSystem().AddSector(0, 0, sector);

        var result = await ReadAsync(fs);

        var city = Assert.Single(result.Index!.Cities);
        Assert.Equal(new MapPos(2000f, 3000f), city.Center);
    }

    [Fact]
    public async Task ReadAsync_DifferentTokens_ReturnsOneCityEach()
    {
        var sector = new SectorBuilder()
            .City("city_a", 11).City("city_b", 12)
            .Node(11, 100f, 100f).Node(12, 200f, 200f)
            .Build();
        var fs = new FakeScsFileSystem().AddSector(0, 0, sector);

        var result = await ReadAsync(fs);

        Assert.Equal(["city_a", "city_b"], result.Index!.Cities.Select(c => c.Token).Order());
    }

    // ---- Company ----

    [Fact]
    public async Task ReadAsync_CompanyWithNodeInSameSector_ReturnsTokensAndNodePosition()
    {
        var sector = new SectorBuilder()
            .City("city_a", 11)
            .Company("co_one", "city_a", 21)
            .Node(11, 100f, 100f)
            .Node(21, 321.5f, 1234.75f)
            .Build();
        var fs = new FakeScsFileSystem().AddSector(0, 0, sector);

        var result = await ReadAsync(fs);

        var company = Assert.Single(result.Index!.Companies);
        Assert.Equal("co_one", company.CompanyToken);
        Assert.Equal("city_a", company.CityToken);
        Assert.Equal(new MapPos(321.5f, 1234.75f), company.Position);
    }

    [Fact]
    public async Task ReadAsync_CompanyNodeInAnotherSector_UsesGlobalNodeTable()
    {
        var sectorWithCompany = new SectorBuilder()
            .Company("co_one", "city_a", 77)
            .Build();
        var sectorWithNode = new SectorBuilder()
            .City("city_a", 11)
            .Node(11, 4100f, 100f)
            .Node(77, 4500.5f, 700.25f)
            .Build();
        var fs = new FakeScsFileSystem()
            .AddSector(0, 0, sectorWithCompany)     // lido antes do setor que tem o nó
            .AddSector(1, 0, sectorWithNode);

        var result = await ReadAsync(fs);

        var company = Assert.Single(result.Index!.Companies);
        Assert.Equal(new MapPos(4500.5f, 700.25f), company.Position);
    }

    // ---- setores ilegíveis ----

    [Fact]
    public async Task ReadAsync_AllSectorsVersion907_ReturnsReady()
    {
        var result = await ReadAsync(Map(20));

        Assert.Equal(MapStatus.Ready, result.Status);
        Assert.Equal(20, result.Index!.Cities.Count);
    }

    [Fact]
    public async Task ReadAsync_OneSectorOfTwentyWithOtherVersion_IsSkippedAndPartiallyRead()
    {
        var other = GoodSector(3);
        other.Version = 906;

        var result = await ReadAsync(Map(20, (3, other.Build())));

        Assert.Equal(MapStatus.PartiallyRead, result.Status);
        Assert.Equal(19, result.Index!.Cities.Count);
        Assert.DoesNotContain(result.Index.Cities, c => c.Token == "c3");
    }

    [Fact]
    public async Task ReadAsync_OneSectorWithUnknownItemType_IsSkippedAndPartiallyRead()
    {
        var bad = new SectorBuilder().Overlay().Unknown(99).City("c3", 1).Node(1, 3 * 4000 + 1000f, 1000f).Build();

        var result = await ReadAsync(Map(20, (3, bad)));

        Assert.Equal(MapStatus.PartiallyRead, result.Status);
        Assert.Equal(19, result.Index!.Cities.Count);
    }

    [Fact]
    public async Task ReadAsync_OneSectorWithItemPastEndOfFile_IsSkippedAndPartiallyRead()
    {
        var bad = SectorBuilder.CutInsideFirstItem(GoodSector(3).Build(), bytesIntoItem: 30);

        var result = await ReadAsync(Map(20, (3, bad)));

        Assert.Equal(MapStatus.PartiallyRead, result.Status);
        Assert.Equal(19, result.Index!.Cities.Count);
    }

    [Fact]
    public async Task ReadAsync_OneSectorWithExtraBytesAfterTail_IsSkippedAndPartiallyRead()
    {
        var bad = SectorBuilder.Extend(GoodSector(3).Build(), 8);

        var result = await ReadAsync(Map(20, (3, bad)));

        Assert.Equal(MapStatus.PartiallyRead, result.Status);
        Assert.Equal(19, result.Index!.Cities.Count);
    }

    [Fact]
    public async Task ReadAsync_OneSectorWithTruncatedNodeTable_IsSkippedAndPartiallyRead()
    {
        var bad = SectorBuilder.Truncate(GoodSector(3).Build(), 10);

        var result = await ReadAsync(Map(20, (3, bad)));

        Assert.Equal(MapStatus.PartiallyRead, result.Status);
        Assert.Equal(19, result.Index!.Cities.Count);
    }

    [Fact]
    public async Task ReadAsync_OneSectorWithCityNodeMissingFromItsTable_IsSkippedAndPartiallyRead()
    {
        var bad = new SectorBuilder().City("c3", 1).Node(999, 3 * 4000 + 1000f, 1000f).Build();

        var result = await ReadAsync(Map(20, (3, bad)));

        Assert.Equal(MapStatus.PartiallyRead, result.Status);
        Assert.Equal(19, result.Index!.Cities.Count);
    }

    [Fact]
    public async Task ReadAsync_TwoSectorsOfTwentyUnreadable_ReturnsUnsupportedVersion()
    {
        var other = GoodSector(3);
        other.Version = 908;

        var result = await ReadAsync(Map(20, (3, other.Build()), (4, other.Build())));

        Assert.Equal(MapStatus.UnsupportedVersion, result.Status);
    }

    [Fact]
    public async Task ReadAsync_AllSectorsOtherVersion_ReturnsUnsupportedVersion()
    {
        var other = GoodSector(0);
        other.Version = 906;
        var fs = new FakeScsFileSystem().AddSector(0, 0, other.Build());

        var result = await ReadAsync(fs);

        Assert.Equal(MapStatus.UnsupportedVersion, result.Status);
    }

    // ---- cancelamento ----

    [Fact]
    public async Task ReadAsync_CancelledToken_ThrowsOperationCanceled()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => ReadAsync(Map(20), cts.Token));
    }

    [Fact]
    public async Task ReadAsync_CancelledWhileReading_StopsBeforeReadingAllSectors()
    {
        using var cts = new CancellationTokenSource();
        var fs = Map(20);
        fs.OnOpen = _ => { if (fs.OpenCount == 3) cts.Cancel(); };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => ReadAsync(fs, cts.Token));

        Assert.True(fs.OpenCount < 20);
    }
}
