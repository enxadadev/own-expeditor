using System.Globalization;
using System.Text;
using OwnExpeditor.Domain.Maps;
using OwnExpeditor.Sii;

namespace OwnExpeditor.Data.Maps;

/// <summary>Dados de uma cidade em <c>def/city/*.sui</c> (a posição vem do setor, não daqui).</summary>
internal sealed record CityMeta(string? Country, int? Population, IReadOnlyList<MapPos> LabelOffsets);

/// <summary>
/// Lê <c>def/country/*.sui</c> (nome e <c>pos</c>) e <c>def/city/*.sui</c> (país, população, deslocamentos de rótulo)
/// pelo sistema de arquivos em camadas. Nada é fixo: tudo vem da instalação (compatível com DLC e mods).
/// </summary>
internal static class MapDefReader
{
    public static (IReadOnlyList<MapCountry> Countries, IReadOnlyDictionary<string, CityMeta> Cities) Read(
        IScsFileSystem fs, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(fs);
        return (ReadCountries(fs, ct), ReadCities(fs, ct));
    }

    internal static IReadOnlyList<MapCountry> ReadCountries(IScsFileSystem fs, CancellationToken ct)
    {
        var countries = new Dictionary<string, MapCountry>(StringComparer.Ordinal);
        foreach (var path in SuiFiles(fs, "def/country"))
        {
            ct.ThrowIfCancellationRequested();
            var block = ReadBlock(fs, path);
            if (block is null) continue;
            var token = NormalizeCountry(block.Id) ?? Stem(path);
            if (!block.Scalars.TryGetValue("pos", out var pos) || !TryParsePos(pos, out var position)) continue;
            block.Scalars.TryGetValue("name", out var name);
            countries[token] = new MapCountry(token, Unquote(name), position);
        }
        return [.. countries.Values.OrderBy(c => c.Token, StringComparer.Ordinal)];
    }

    internal static IReadOnlyDictionary<string, CityMeta> ReadCities(IScsFileSystem fs, CancellationToken ct)
    {
        var cities = new Dictionary<string, CityMeta>(StringComparer.Ordinal);
        foreach (var path in SuiFiles(fs, "def/city"))
        {
            ct.ThrowIfCancellationRequested();
            var block = ReadBlock(fs, path);
            if (block is null) continue;
            var token = block.Id is { Length: > 0 } id && id.StartsWith("city.", StringComparison.Ordinal) ? id[5..] : Stem(path);

            block.Scalars.TryGetValue("country", out var country);
            int? population = block.Scalars.TryGetValue("population", out var pop)
                && int.TryParse(pop, NumberStyles.Integer, CultureInfo.InvariantCulture, out var popValue) ? popValue : null;

            block.Arrays.TryGetValue("map_x_offsets", out var xs);
            block.Arrays.TryGetValue("map_y_offsets", out var ys);
            var count = Math.Max(xs?.Count ?? 0, ys?.Count ?? 0);
            var offsets = new MapPos[count];
            for (var i = 0; i < count; i++)
                offsets[i] = new MapPos(ParseFloat(xs, i), ParseFloat(ys, i));

            cities[token] = new CityMeta(NormalizeCountry(country), population, offsets);
        }
        return cities;
    }

    private static IEnumerable<string> SuiFiles(IScsFileSystem fs, string dir) =>
        fs.List(dir).Where(p => p.EndsWith(".sui", StringComparison.Ordinal)).Order(StringComparer.Ordinal);

    private static string Stem(string path)
    {
        var name = path[(path.LastIndexOf('/') + 1)..];
        var dot = name.IndexOf('.');
        return dot < 0 ? name : name[..dot];
    }

    /// <summary>Id de país em minúsculas, sem aspas nem prefixo <c>country.data.</c> (mesma regra do <c>GameCatalog</c>).</summary>
    private static string? NormalizeCountry(string? value)
    {
        var id = value?.Trim().Trim('"').Replace("country.data.", "", StringComparison.Ordinal).Trim().ToLowerInvariant();
        return string.IsNullOrEmpty(id) ? null : id;
    }

    private static string? Unquote(string? value) => value?.Trim().Trim('"');

    private static float ParseFloat(List<string>? values, int index) =>
        values is not null && index < values.Count
        && float.TryParse(values[index], NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : 0f;

    /// <summary><c>(x, y, z)</c> em metros do mundo; devolve (x, z).</summary>
    private static bool TryParsePos(string value, out MapPos pos)
    {
        pos = default;
        var text = value.AsSpan().Trim();
        if (text.Length < 2 || text[0] != '(' || text[^1] != ')') return false;
        var parts = text[1..^1].ToString().Split(',', StringSplitOptions.TrimEntries);
        if (parts.Length != 3) return false;
        if (!float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x)
            || !float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var z)) return false;
        pos = new MapPos(x, z);
        return true;
    }

    private sealed class Block
    {
        public string? Id { get; set; }
        public Dictionary<string, string> Scalars { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, List<string>> Arrays { get; } = new(StringComparer.Ordinal);
    }

    /// <summary>
    /// Primeiro bloco <c>tipo : id { ... }</c> do arquivo (campos <c>chave: valor</c> e <c>chave[]: valor</c>).
    /// Null se o arquivo não existe ou não decodifica (cifrado/binário corrompido): um .sui ruim não derruba o índice.
    /// </summary>
    private static Block? ReadBlock(IScsFileSystem fs, string path)
    {
        string text;
        using (var stream = fs.Open(path))
        {
            if (stream is null) return null;
            try
            {
                text = ReadText(stream);
            }
            catch (SiiFormatException)
            {
                return null;
            }
        }

        var block = new Block();
        var inBody = false;
        foreach (var raw in text.AsSpan().EnumerateLines())
        {
            var line = StripComment(raw).Trim();
            if (line.IsEmpty) continue;
            if (!inBody)
            {
                if (line.EndsWith("{", StringComparison.Ordinal))
                {
                    inBody = true;
                    line = line[..^1].TrimEnd();
                }
                var colon = line.IndexOf(':');
                if (colon > 0 && block.Id is null) block.Id = line[(colon + 1)..].Trim().ToString();
                continue;
            }
            if (line[0] == '}') break;

            var sep = line.IndexOf(':');
            if (sep <= 0) continue;
            var key = line[..sep].Trim();
            var value = line[(sep + 1)..].Trim().ToString();
            if (key.EndsWith("[]", StringComparison.Ordinal))
            {
                var name = key[..^2].ToString();
                if (!block.Arrays.TryGetValue(name, out var list)) block.Arrays[name] = list = [];
                list.Add(value);
            }
            else
            {
                block.Scalars.TryAdd(key.ToString(), value);
            }
        }
        return block;
    }

    /// <summary>
    /// Texto do .sui. Texto puro é decodificado direto do buffer do stream (sem cópia) quando ele o expõe; formatos cifrados
    /// ou binários precisam de um <c>byte[]</c> exato e copiam só o conteúdo.
    /// </summary>
    private static string ReadText(Stream stream)
    {
        if (stream is MemoryStream ms && ms.TryGetBuffer(out var segment))
        {
            var content = segment.AsSpan((int)ms.Position);
            return SiiDecoder.DetectFormat(content) is SiiFormat.Text or SiiFormat.Unknown
                ? Encoding.UTF8.GetString(content).TrimStart('\uFEFF')
                : SiiDecoder.DecodeLenient(content.ToArray());
        }

        using var copy = new MemoryStream();
        stream.CopyTo(copy);
        return SiiDecoder.DecodeLenient(copy.ToArray());
    }

    private static ReadOnlySpan<char> StripComment(ReadOnlySpan<char> line)
    {
        var inQuote = false;
        for (var i = 0; i < line.Length; i++)
        {
            if (line[i] == '"') inQuote = !inQuote;
            else if (line[i] == '#' && !inQuote) return line[..i];
            else if (line[i] == '/' && !inQuote && i + 1 < line.Length && line[i + 1] == '/') return line[..i];
        }
        return line;
    }
}
