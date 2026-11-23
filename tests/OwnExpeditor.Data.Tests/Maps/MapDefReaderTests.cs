using System.Text;
using OwnExpeditor.Data.Maps;
using OwnExpeditor.Domain.Maps;

namespace OwnExpeditor.Data.Tests.Maps;

public class MapDefReaderTests
{
    /// <summary>FS em memória com arquivos de texto (def/*.sui) e setores; <c>List</c> devolve caminhos completos dos filhos diretos.</summary>
    private sealed class TextFs : IScsFileSystem
    {
        private readonly Dictionary<string, byte[]> _files = new(StringComparer.Ordinal);

        public TextFs Add(string path, string text)
        {
            _files[path] = Encoding.UTF8.GetBytes(text);
            return this;
        }

        public TextFs AddSector(int sx, byte[] data)
        {
            _files[$"map/europe/sec{sx.ToString("+0000;-0000")}+0000.base"] = data;
            return this;
        }

        public Stream? Open(string path) => _files.TryGetValue(path, out var d) ? new MemoryStream(d, writable: false) : null;

        public IEnumerable<string> List(string dir)
        {
            var prefix = dir.TrimEnd('/') + "/";
            return _files.Keys
                .Where(k => k.StartsWith(prefix, StringComparison.Ordinal))
                .Select(k => prefix + k[prefix.Length..].Split('/')[0])
                .Distinct();
        }
    }

    private const string Italy = """
        country_data : country.data.italy
        {
        	country_id: 3
        	name: "Italia"
        	name_localized: "@@italy@@"
        	pos: (-4210, 0, 31600.5)   # ancora do rotulo
        	fuel_price: 2.1
        }
        """;

    private const string Roma = """
        city_data : city.roma
        {
        	city_name: "Roma"
        	country: italy
        	population: 2800000

        	map_x_offsets[]: 0
        	map_x_offsets[]: -3
        	map_x_offsets[]: -44

        	map_y_offsets[]: 5
        	map_y_offsets[]: -8
        	map_y_offsets[]: 12
        }

        license_plate_data : .roma.lp.car
        {
        	country: other
        	population: 1
        }
        """;

    [Fact]
    public void ReadCountries_FileWithPos_ReturnsTokenNameAndAnchor()
    {
        var fs = new TextFs().Add("def/country/italia.sui", Italy);

        var countries = MapDefReader.ReadCountries(fs, CancellationToken.None);

        var country = Assert.Single(countries);
        Assert.Equal("italy", country.Token);
        Assert.Equal("Italia", country.Name);
        Assert.Equal(new MapPos(-4210f, 31600.5f), country.Pos);
    }

    [Fact]
    public void ReadCountries_FileWithoutPos_IsSkipped()
    {
        var fs = new TextFs()
            .Add("def/country/italia.sui", Italy)
            .Add("def/country/nopos.sui", "country_data : country.data.nopos\n{\n\tname: \"X\"\n}\n");

        var countries = MapDefReader.ReadCountries(fs, CancellationToken.None);

        Assert.Equal(["italy"], countries.Select(c => c.Token));
    }

    [Fact]
    public void ReadCities_FullFile_ReturnsCountryPopulationAndOffsetsPerZoom()
    {
        var fs = new TextFs().Add("def/city/roma.sui", Roma);

        var cities = MapDefReader.ReadCities(fs, CancellationToken.None);

        var meta = Assert.Single(cities).Value;
        Assert.Equal("italy", meta.Country);
        Assert.Equal(2800000, meta.Population);
        Assert.Equal([new MapPos(0, 5), new MapPos(-3, -8), new MapPos(-44, 12)], meta.LabelOffsets);
    }

    [Fact]
    public void ReadCities_IdDiffersFromFileName_UsesIdAsKey()
    {
        var fs = new TextFs().Add("def/city/veliko_tarnovo.sui", "city_data : city.veli_tarnovo\n{\n\tcountry: bulgaria\n}\n");

        var cities = MapDefReader.ReadCities(fs, CancellationToken.None);

        Assert.Equal(["veli_tarnovo"], cities.Keys);
    }

    [Fact]
    public void ReadCities_WithoutPopulationOrOffsets_LeavesThemEmpty()
    {
        var fs = new TextFs().Add("def/city/x.sui", "city_data : city.x\n{\n\tcity_name: \"X\"\n}\n");

        var meta = Assert.Single(MapDefReader.ReadCities(fs, CancellationToken.None)).Value;

        Assert.Null(meta.Country);
        Assert.Null(meta.Population);
        Assert.Empty(meta.LabelOffsets);
    }

    [Fact]
    public void ReadCities_CancelledToken_ThrowsOperationCanceled()
    {
        var fs = new TextFs().Add("def/city/roma.sui", Roma);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.Throws<OperationCanceledException>(() => MapDefReader.ReadCities(fs, cts.Token));
    }

    [Fact]
    public async Task ReadAsync_WithDefFiles_FillsCityMetaAndCountries()
    {
        var sector = new SectorBuilder().City("roma", 1).City("semdef", 2).Node(1, 100f, 200f).Node(2, 300f, 400f).Build();
        var fs = new TextFs()
            .AddSector(0, sector)
            .Add("def/city/roma.sui", Roma)
            .Add("def/country/italia.sui", Italy);

        var result = await new MapSectorReader().ReadAsync(fs, MapDetail.Cities, null, CancellationToken.None);

        Assert.Equal(MapStatus.Ready, result.Status);
        var roma = Assert.Single(result.Index!.Cities, c => c.Token == "roma");
        Assert.Equal("italy", roma.Country);
        Assert.Equal(2800000, roma.Population);
        Assert.Equal(3, roma.LabelOffsets.Count);
        var semdef = Assert.Single(result.Index.Cities, c => c.Token == "semdef");
        Assert.Null(semdef.Country);
        Assert.Empty(semdef.LabelOffsets);
        Assert.Equal("italy", Assert.Single(result.Index.Countries).Token);
    }
}
