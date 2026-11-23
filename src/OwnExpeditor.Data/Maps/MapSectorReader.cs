using System.Buffers;
using OwnExpeditor.Domain.Maps;

namespace OwnExpeditor.Data.Maps;

/// <summary>
/// Lê os setores <c>map/&lt;mapa&gt;/sec*.base</c> (versão 907, ver docs/scs-map-format.md) e <c>def/country</c> + <c>def/city</c>,
/// e monta o <see cref="MapIndex"/>. Na fase 1a só City e Company (estradas e grafo ficam vazios/nulos, qualquer que seja o detalhe).
/// </summary>
/// <remarks>
/// Um setor ilegível (versão diferente, tipo de item desconhecido, tamanho inválido ou fim que não fecha) é ignorado e contado:
/// acima de 5% dos setores o status é <see cref="MapStatus.UnsupportedVersion"/>; de 1 setor até 5%, <see cref="MapStatus.PartiallyRead"/>.
/// Os setores são lidos em sequência, reaproveitando um único buffer; só os nós são guardados em uma tabela global.
/// </remarks>
internal sealed class MapSectorReader
{
    private const string MapRoot = "map";
    private const string SectorExtension = ".base";

    public Task<MapLoadResult> ReadAsync(IScsFileSystem fs, MapDetail detail, IProgress<double>? progress, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(fs);
        return Task.Run(() => Read(fs, progress, ct), ct);
    }

    private static MapLoadResult Read(IScsFileSystem fs, IProgress<double>? progress, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        List<string> sectors;
        try
        {
            sectors = ListSectors(fs);
        }
        catch (Exception e) when (e is InvalidDataException or EndOfStreamException or ArgumentException)
        {
            return new MapLoadResult(MapStatus.Failed, null, []);   // diretório do pacote corrompido: sem mapa
        }
        if (sectors.Count == 0) return new MapLoadResult(MapStatus.Failed, null, []);

        var cities = new Dictionary<string, RawCity>(StringComparer.Ordinal);
        var companies = new List<RawCompany>();
        var nodes = new List<RawNode>();
        var scratch = new SectorScratch();
        var unreadable = 0;

        for (var i = 0; i < sectors.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            if (!TryReadSector(fs, sectors[i], scratch))
            {
                unreadable++;
            }
            else
            {
                foreach (var city in scratch.Cities) Offer(cities, city);
                companies.AddRange(scratch.Companies);
                nodes.AddRange(scratch.Nodes);
            }
            progress?.Report(0.95 * (i + 1) / sectors.Count);
        }

        // Mais de 5% dos setores ilegíveis: o formato mudou, o índice não é confiável.
        if (unreadable * 20 > sectors.Count) return new MapLoadResult(MapStatus.UnsupportedVersion, null, []);

        ct.ThrowIfCancellationRequested();
        var (countries, meta) = MapDefReader.Read(fs, ct);
        var index = Build(cities, companies, nodes, countries, meta);
        progress?.Report(1.0);
        return new MapLoadResult(unreadable == 0 ? MapStatus.Ready : MapStatus.PartiallyRead, index, []);
    }

    /// <summary>Caminhos dos <c>.base</c> de todos os mapas em <c>map/*/</c> (sem exigir o <c>.mbd</c>, que só existe no base.scs).</summary>
    private static List<string> ListSectors(IScsFileSystem fs)
    {
        var sectors = new List<string>();
        foreach (var map in fs.List(MapRoot))
            foreach (var file in fs.List(map))
                if (file.EndsWith(SectorExtension, StringComparison.Ordinal)) sectors.Add(file);
        sectors.Sort(StringComparer.Ordinal);
        return sectors;
    }

    private static bool TryReadSector(IScsFileSystem fs, string path, SectorScratch scratch)
    {
        Stream? stream;
        try
        {
            stream = fs.Open(path);
        }
        catch (Exception e) when (e is InvalidDataException or NotSupportedException or EndOfStreamException or ArgumentException)
        {
            return false;   // zlib corrompido, compressão desconhecida ou arquivo truncado: setor ilegível
        }
        if (stream is null) return false;

        using (stream)
        {
            if (stream is MemoryStream ms && ms.TryGetBuffer(out var segment))
                return SectorParser.Parse(segment.AsSpan((int)ms.Position), scratch) is null;

            var length = checked((int)(stream.Length - stream.Position));
            var buffer = ArrayPool<byte>.Shared.Rent(Math.Max(length, 1));
            try
            {
                stream.ReadExactly(buffer.AsSpan(0, length));
                return SectorParser.Parse(buffer.AsSpan(0, length), scratch) is null;
            }
            catch (EndOfStreamException)
            {
                return false;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }

    /// <summary>
    /// Escolhe a área que representa a cidade: a visível (bit "oculto" desligado); entre iguais, a de maior largura × altura.
    /// O centro é o nó da área, já resolvido na tabela do próprio setor.
    /// </summary>
    private static void Offer(Dictionary<string, RawCity> best, RawCity city)
    {
        if (!best.TryGetValue(city.Token, out var current) || IsBetter(city, current)) best[city.Token] = city;
    }

    private static bool IsBetter(RawCity candidate, RawCity current)
    {
        if (candidate.Hidden != current.Hidden) return !candidate.Hidden;
        return candidate.Width * candidate.Height > current.Width * current.Height;
    }

    private static MapIndex Build(
        Dictionary<string, RawCity> cities, List<RawCompany> companies, List<RawNode> nodes,
        IReadOnlyList<MapCountry> countries, IReadOnlyDictionary<string, CityMeta> meta)
    {
        var mapCities = new List<MapCity>(cities.Count);
        foreach (var city in cities.Values.OrderBy(c => c.Token, StringComparer.Ordinal))
        {
            meta.TryGetValue(city.Token, out var m);
            mapCities.Add(new MapCity(city.Token, city.Center, m?.Country, m?.Population, m?.LabelOffsets ?? []));
        }

        // Só os nós usados por empresas viram dicionário; o resto da tabela global é descartado.
        var needed = new HashSet<ulong>(companies.Count);
        foreach (var c in companies) needed.Add(c.NodeUid);
        var positions = new Dictionary<ulong, MapPos>(needed.Count);
        foreach (var n in nodes)
            if (needed.Contains(n.Uid)) positions[n.Uid] = new MapPos(n.X, n.Z);

        var seen = new HashSet<(string, string)>();
        var mapCompanies = new List<MapCompany>(companies.Count);
        foreach (var c in companies)
        {
            if (!seen.Add((c.CompanyToken, c.CityToken))) continue;
            // Sem o nó (setor ilegível ou ausente): canto do item, erro de até algumas centenas de metros.
            var position = positions.TryGetValue(c.NodeUid, out var p) ? p : new MapPos(c.HeaderX, c.HeaderZ);
            mapCompanies.Add(new MapCompany(c.CompanyToken, c.CityToken, position, null));
        }
        mapCompanies.Sort((a, b) =>
        {
            var byCity = string.CompareOrdinal(a.CityToken, b.CityToken);
            return byCity != 0 ? byCity : string.CompareOrdinal(a.CompanyToken, b.CompanyToken);
        });

        return new MapIndex(mapCities, countries, mapCompanies, [], null);
    }
}
