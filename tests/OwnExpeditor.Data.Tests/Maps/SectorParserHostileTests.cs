using System.Buffers.Binary;
using OwnExpeditor.Data.Maps;
using OwnExpeditor.Domain.Maps;

namespace OwnExpeditor.Data.Tests.Maps;

/// <summary>Setores adulterados: o parser devolve o motivo (nunca lança nem devolve dados parciais).</summary>
public class SectorParserHostileTests
{
    private const int Big = int.MaxValue;

    private static string? Parse(byte[] sector) => SectorParser.Parse(sector, new SectorScratch());

    private static byte[] WithItemCount(int count)
    {
        var bytes = new SectorBuilder().City("c", 1).Node(1, 1, 1).Build();
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(0x10), count);
        return bytes;
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    [InlineData(Big)]
    [InlineData(1000)]
    public void Parse_ImpossibleItemCount_ReturnsReason(int count)
    {
        Assert.NotNull(Parse(WithItemCount(count)));
    }

    public static IEnumerable<object[]> HostileItems()
    {
        // Service (tipo 7): i32 n em F+5+0x10
        yield return new object[] { 7u, Pad(0x10, -1) };
        yield return new object[] { 7u, Pad(0x10, Big) };
        yield return new object[] { 7u, Pad(0x10, int.MinValue) };
        // Trigger (tipo 34): tags, nós, ações (cada contagem lida do arquivo)
        yield return new object[] { 34u, SectorBuilder.Body(-1) };
        yield return new object[] { 34u, SectorBuilder.Body(Big) };
        yield return new object[] { 34u, SectorBuilder.Body(0, -1) };
        yield return new object[] { 34u, SectorBuilder.Body(0, Big) };
        yield return new object[] { 34u, SectorBuilder.Body(0, 0, -1) };
        yield return new object[] { 34u, SectorBuilder.Body(0, 0, Big) };
        yield return new object[] { 34u, SectorBuilder.Body(0, 0, Big, 0, 0, 0, 0, 0, 0) };
        yield return new object[] { 34u, SectorBuilder.Body(0, 0, 1, 0, 0, Big) };              // overrides enorme
        yield return new object[] { 34u, SectorBuilder.Body(0, 0, 1, 0, 0, -5, 0, 0) };        // overrides negativo
        yield return new object[] { 34u, SectorBuilder.Body(0, 0, 1, 0, 0, 0, Big) };           // parâmetros enorme
        yield return new object[] { 34u, SectorBuilder.Body(0, 0, 1, 0, 0, 0, -1) };            // parâmetros negativo
        yield return new object[] { 34u, SectorBuilder.Body(0, 0, 1, 0, 0, 0, 1, -1, 0) };     // tamanho de texto negativo
        yield return new object[] { 34u, SectorBuilder.Body(0, 0, 1, 0, 0, 0, 1, Big, 0) };    // tamanho de texto gigante
        // Cutscene (tipo 46) e TrafficRule (38)
        yield return new object[] { 46u, SectorBuilder.Body(-1) };
        yield return new object[] { 46u, SectorBuilder.Body(0, 0, 0, Big) };
        yield return new object[] { 38u, SectorBuilder.Body(Big) };
        yield return new object[] { 38u, SectorBuilder.Body(0, -1) };
        // Curve (44): máscara com todos os bits ligados e nada depois
        yield return new object[] { 44u, Pad(0x20, 0, -1) };
        // RoadSide (36): nº de placas (sbyte) negativo e tamanhos negativos
        yield return new object[] { 36u, Pad(0x1B, 0x80) };
        yield return new object[] { 36u, new byte[5 + 0x20].Concat(new byte[] { 0 }).Concat(BitConverter.GetBytes(-1)).ToArray() };
        // Terrain (1), Building (2), Prefab (4), Company (6)
        yield return new object[] { 1u, Pad(0xEE, -1) };
        yield return new object[] { 2u, Pad(0x2C, Big) };
        yield return new object[] { 4u, SectorBuilder.Body(-1) };
        yield return new object[] { 6u, Pad(0x20, Big) };
        yield return new object[] { 6u, Pad(0x20, -1) };
    }

    /// <summary>5 bytes de flags + <paramref name="zeros"/> bytes zerados + inteiros.</summary>
    private static byte[] Pad(int zeros, params int[] ints)
    {
        var ms = new MemoryStream();
        var w = new BinaryWriter(ms);
        w.Write(new byte[5 + zeros]);
        foreach (var i in ints) w.Write(i);
        return ms.ToArray();
    }

    [Theory]
    [MemberData(nameof(HostileItems))]
    public void Parse_KnownTypeWithHostileCounts_ReturnsReasonWithoutThrowing(uint type, byte[] body)
    {
        var sector = new SectorBuilder().Raw(type, body).Node(1, 1, 1).Build();

        var reason = Parse(sector);

        Assert.NotNull(reason);
    }

    [Theory]
    [MemberData(nameof(HostileItems))]
    public void Parse_HostileItemBeforeValidCity_NeverReturnsPartialData(uint type, byte[] body)
    {
        var sector = new SectorBuilder().City("c", 1).Raw(type, body).Node(1, 1, 1).Build();
        var scratch = new SectorScratch();

        var reason = SectorParser.Parse(sector, scratch);

        Assert.NotNull(reason);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(40)]
    public void Parse_KnownTypeTruncatedAtEnd_ReturnsReasonWithoutThrowing(int removed)
    {
        var sector = SectorBuilder.Truncate(new SectorBuilder().Service(3).City("c", 1).Node(1, 1, 1).Build(), removed);

        Assert.NotNull(Parse(sector));
    }

    [Fact]
    public void Parse_FileCutInsideServiceItem_ReturnsReason()
    {
        var sector = new SectorBuilder().Service(5).Node(1, 1, 1).Build();

        Assert.NotNull(Parse(sector[..(SectorBuilder.HeaderSize + SectorBuilder.CommonHeader + 10)]));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(Big)]
    public void Parse_NodeCountImpossible_ReturnsReason(int nodes)
    {
        var bytes = new SectorBuilder().City("c", 1).Node(1, 1, 1).Build();
        var at = SectorBuilder.HeaderSize + SectorBuilder.CitySize;
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(at), nodes);

        Assert.NotNull(Parse(bytes));
    }

    [Fact]
    public async Task ReadAsync_OneHostileSectorAmongTwenty_IsCountedAndRestIsKept()
    {
        var fs = new FakeScsFileSystem();
        for (var i = 0; i < 20; i++)
        {
            var data = i == 7
                ? new SectorBuilder().Raw(34, SectorBuilder.Body(0, 0, Big)).Node(1, 1, 1).Build()
                : new SectorBuilder().City($"c{i}", 1).Node(1, i * 4000 + 100f, 100f).Build();
            fs.AddSector(i, 0, data);
        }

        var result = await new MapSectorReader().ReadAsync(fs, MapDetail.Cities, null, CancellationToken.None);

        Assert.Equal(MapStatus.PartiallyRead, result.Status);
        Assert.Equal(19, result.Index!.Cities.Count);
    }

    // ---- S2: resolução dos nós das cidades ----

    [Fact]
    public void Parse_ManyCitiesAndNodes_ResolvesEachCityToItsOwnNode()
    {
        var b = new SectorBuilder();
        const int cities = 300;
        for (var i = 0; i < cities; i++) b.City($"c{i}", (ulong)(100 + i));
        for (var i = cities - 1; i >= 0; i--) b.Node((ulong)(100 + i), i + 0.5f, -i - 0.25f);   // ordem inversa: nada de busca ordenada
        for (var i = 0; i < 2000; i++) b.Node((ulong)(10_000 + i), i, i);
        var scratch = new SectorScratch();

        var reason = SectorParser.Parse(b.Build(), scratch);

        Assert.Null(reason);
        Assert.Equal(cities, scratch.Cities.Count);
        Assert.Equal(cities + 2000, scratch.Nodes.Count);
        for (var i = 0; i < cities; i++)
            Assert.Equal(new MapPos(i + 0.5f, -i - 0.25f), scratch.Cities[i].Center);
    }

    [Fact]
    public void Parse_TwoCitiesSharingTheSameNode_BothGetThePosition()
    {
        var scratch = new SectorScratch();

        var reason = SectorParser.Parse(new SectorBuilder().City("a", 5).City("b", 5).Node(5, 10f, 20f).Build(), scratch);

        Assert.Null(reason);
        Assert.All(scratch.Cities, c => Assert.Equal(new MapPos(10f, 20f), c.Center));
    }

    [Fact]
    public void Parse_SectorWithoutCities_StillReturnsNodeTable()
    {
        var scratch = new SectorScratch();

        var reason = SectorParser.Parse(new SectorBuilder().Road().Node(1, 1, 2).Node(2, 3, 4).Build(), scratch);

        Assert.Null(reason);
        Assert.Equal([1UL, 2UL], scratch.Nodes.Select(n => n.Uid));
    }

    [Fact]
    public void Parse_CityPointingToNodeOutsideTable_ReturnsReason()
    {
        Assert.NotNull(Parse(new SectorBuilder().City("a", 99).Node(5, 1, 1).Build()));
    }
}
