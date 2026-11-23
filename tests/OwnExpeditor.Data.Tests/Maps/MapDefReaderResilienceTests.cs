using System.Buffers;
using System.Text;
using OwnExpeditor.Data.Maps;
using OwnExpeditor.Data.Scs;

namespace OwnExpeditor.Data.Tests.Maps;

public class MapDefReaderResilienceTests
{
    private sealed class BytesFs : IScsFileSystem
    {
        private readonly Dictionary<string, Func<Stream>> _files = new(StringComparer.Ordinal);

        public BytesFs Add(string path, string text) => Add(path, Encoding.UTF8.GetBytes(text));

        public BytesFs Add(string path, byte[] bytes)
        {
            _files[path] = () => new MemoryStream(bytes, writable: false);
            return this;
        }

        /// <summary>Arquivo servido como o <c>OpenPooled</c> real: buffer do pool, maior que o conteúdo, com lixo depois do fim.</summary>
        public BytesFs AddPooled(string path, byte[] content, string trailingGarbage)
        {
            var garbage = Encoding.UTF8.GetBytes(trailingGarbage);
            _files[path] = () =>
            {
                var buffer = ArrayPool<byte>.Shared.Rent(content.Length + garbage.Length);
                Array.Fill(buffer, (byte)'9');
                content.CopyTo(buffer, 0);
                garbage.CopyTo(buffer, content.Length);
                return new PooledMemoryStream(buffer, content.Length);
            };
            return this;
        }

        public Stream? Open(string path) => _files.TryGetValue(path, out var open) ? open() : null;

        public IEnumerable<string> List(string dir)
        {
            var prefix = dir.TrimEnd('/') + "/";
            return _files.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal));
        }
    }

    private static readonly byte[] TruncatedEncrypted = [.. "ScsC"u8, 1, 2, 3];
    private static readonly byte[] TruncatedThreeNK = [.. "3nK"u8, 1];

    private const string Roma = "city_data : city.roma\n{\n\tcountry: italy\n\tpopulation: 100\n}\n";
    private const string Italy = "country_data : country.data.italy\n{\n\tname: \"Italia\"\n\tpos: (1, 0, 2)\n}\n";

    [Theory]
    [InlineData("encrypted")]
    [InlineData("threenk")]
    public void ReadCities_UndecodableSuiFile_IsSkippedAndOthersStillRead(string kind)
    {
        var bad = kind == "encrypted" ? TruncatedEncrypted : TruncatedThreeNK;
        var fs = new BytesFs()
            .Add("def/city/a_bad.sui", bad)
            .Add("def/city/roma.sui", Roma);

        var cities = MapDefReader.ReadCities(fs, CancellationToken.None);

        Assert.Equal(["roma"], cities.Keys);
        Assert.Equal(100, cities["roma"].Population);
    }

    [Fact]
    public void ReadCountries_UndecodableSuiFile_IsSkippedAndOthersStillRead()
    {
        var fs = new BytesFs()
            .Add("def/country/a_bad.sui", TruncatedEncrypted)
            .Add("def/country/italy.sui", Italy);

        var countries = MapDefReader.ReadCountries(fs, CancellationToken.None);

        Assert.Equal(["italy"], countries.Select(c => c.Token));
    }

    [Fact]
    public void Read_AllSuiFilesUndecodable_ReturnsEmptyWithoutThrowing()
    {
        var fs = new BytesFs()
            .Add("def/city/a.sui", TruncatedEncrypted)
            .Add("def/country/b.sui", TruncatedThreeNK);

        var (countries, cities) = MapDefReader.Read(fs, CancellationToken.None);

        Assert.Empty(countries);
        Assert.Empty(cities);
    }

    [Fact]
    public void ReadCities_PooledStreamWithBufferLargerThanContent_ReadsOnlyTheContent()
    {
        // sem '}' final: se o leitor olhasse o buffer inteiro, a população viraria 1234
        var fs = new BytesFs().AddPooled("def/city/x.sui", Encoding.UTF8.GetBytes("city_data : city.x\n{\n\tpopulation: 12"), trailingGarbage: "34");

        var meta = Assert.Single(MapDefReader.ReadCities(fs, CancellationToken.None)).Value;

        Assert.Equal(12, meta.Population);
    }

    [Fact]
    public void ReadCities_PooledEncryptedStreamWithLargerBuffer_IsSkippedNotCrashed()
    {
        var fs = new BytesFs()
            .AddPooled("def/city/bad.sui", TruncatedEncrypted, trailingGarbage: "zzzz")
            .Add("def/city/roma.sui", Roma);

        var cities = MapDefReader.ReadCities(fs, CancellationToken.None);

        Assert.Equal(["roma"], cities.Keys);
    }
}
