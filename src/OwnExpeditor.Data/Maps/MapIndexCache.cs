using System.Security.Cryptography;
using System.Text;
using OwnExpeditor.Domain.Maps;

namespace OwnExpeditor.Data.Maps;

/// <summary>
/// Cache binário do <see cref="MapIndex"/> em disco. Um arquivo por chave: <c>&lt;grupo&gt;-&lt;hash&gt;.idx</c>, gravado em
/// <c>.tmp</c> e movido. Arquivo corrompido, truncado, de outra versão de esquema ou de outra chave é tratado como ausente.
/// </summary>
/// <remarks>
/// O grupo é o trecho da chave antes do primeiro <c>|</c> (o <c>MapSource</c> usa o jogo): depois de cada gravação ficam só
/// os <see cref="KeepPerGroup"/> arquivos mais recentes do grupo.
/// </remarks>
internal sealed class MapIndexCache(string directory)
{
    /// <summary>Versão do layout binário; qualquer mudança incompatível sobe este número (e invalida os arquivos antigos).</summary>
    public const int SchemaVersion = 1;

    /// <summary>Índices mantidos por grupo (jogo).</summary>
    public const int KeepPerGroup = 3;

    private const string Extension = ".idx";
    private const string TempExtension = ".tmp";
    private static readonly TimeSpan StaleTempAge = TimeSpan.FromDays(1);

    // Menor tamanho possível de cada item no arquivo (string = 1 byte de tamanho; bool = 1 byte), para validar contagens.
    private const int MinCityBytes = 15;      // token(1) + centro(8) + país opcional(1) + população opcional(1) + nº offsets(4)
    private const int MinCountryBytes = 10;   // token(1) + nome opcional(1) + posição(8)
    private const int MinCompanyBytes = 11;   // empresa(1) + cidade(1) + posição(8) + nó opcional(1)
    private const int MinRoadBytes = 12;      // classe(4) + lod(4) + nº pontos(4)
    private const int PosBytes = 8;
    private const int EdgeBytes = 24;         // from, to, comprimento, escala, classe, polilinha: 6 x 4
    private static readonly byte[] Magic = "OEMI"u8.ToArray();
    private static readonly byte[] EndMarker = "END!"u8.ToArray();

    /// <summary>Diretório do cache, normalizado (<c>..</c> e separadores resolvidos) para comparar caminhos na limpeza.</summary>
    public string Directory { get; } = Path.GetFullPath(directory);

    public async Task<MapIndex?> TryReadAsync(string key, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        ct.ThrowIfCancellationRequested();
        var path = PathFor(key);
        byte[] bytes;
        try
        {
            if (!File.Exists(path)) return null;
            bytes = await File.ReadAllBytesAsync(path, ct).ConfigureAwait(false);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return null;
        }

        try
        {
            return Deserialize(bytes, key);
        }
        catch (Exception e) when (e is InvalidDataException or IOException or ArgumentException or FormatException or OverflowException)
        {
            return null;   // corrompido: o chamador reconstrói e regrava
        }
    }

    public async Task WriteAsync(string key, MapIndex index, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNull(index);
        ct.ThrowIfCancellationRequested();

        var bytes = Serialize(key, index);
        System.IO.Directory.CreateDirectory(Directory);
        var path = PathFor(key);
        var tmp = Path.Combine(Directory, $"{Path.GetFileName(path)}.{Guid.NewGuid():N}{TempExtension}");
        try
        {
            await File.WriteAllBytesAsync(tmp, bytes, ct).ConfigureAwait(false);
            File.Move(tmp, path, overwrite: true);
        }
        catch
        {
            TryDelete(tmp);
            throw;
        }
        Prune(GroupOf(key), keep: path);
    }

    // ---- arquivos ----

    private string PathFor(string key)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)).AsSpan(0, 12)).ToLowerInvariant();
        return Path.Combine(Directory, $"{GroupOf(key)}-{hash}{Extension}");
    }

    private static string GroupOf(string key)
    {
        var bar = key.IndexOf('|');
        if (bar <= 0) return "default";
        var chars = key[..Math.Min(bar, 16)].ToLowerInvariant().Select(c => char.IsAsciiLetterOrDigit(c) ? c : '_');
        return new string(chars.ToArray());
    }

    private void Prune(string group, string keep)
    {
        try
        {
            var dir = new DirectoryInfo(Directory);
            var stale = dir
                .EnumerateFiles($"{group}-*{Extension}")
                .Where(f => !string.Equals(f.FullName, keep, StringComparison.Ordinal))
                .OrderByDescending(f => f.LastWriteTimeUtc)
                .Skip(KeepPerGroup - 1);
            foreach (var file in stale.ToList()) TryDelete(file.FullName);

            // .tmp de gravações interrompidas (processo morto entre escrever e mover); os recentes podem ser de outra gravação em curso
            var limit = DateTime.UtcNow - StaleTempAge;
            foreach (var tmp in dir.EnumerateFiles($"*{TempExtension}").Where(f => f.LastWriteTimeUtc < limit).ToList())
                TryDelete(tmp.FullName);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // limpeza é só otimização de espaço
        }
    }

    private static void TryDelete(string path)
    {
        try { File.Delete(path); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { /* melhor esforço */ }
    }

    // ---- formato binário ----

    private static byte[] Serialize(string key, MapIndex index)
    {
        var ms = new MemoryStream(64 * 1024);
        using (var w = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true))
        {
            w.Write(Magic);
            w.Write(SchemaVersion);
            w.Write(key);

            w.Write(index.Cities.Count);
            foreach (var c in index.Cities)
            {
                w.Write(c.Token);
                Write(w, c.Center);
                WriteOptional(w, c.Country);
                w.Write(c.Population.HasValue);
                if (c.Population is { } population) w.Write(population);
                w.Write(c.LabelOffsets.Count);
                foreach (var o in c.LabelOffsets) Write(w, o);
            }

            w.Write(index.Countries.Count);
            foreach (var c in index.Countries)
            {
                w.Write(c.Token);
                WriteOptional(w, c.Name);
                Write(w, c.Pos);
            }

            w.Write(index.Companies.Count);
            foreach (var c in index.Companies)
            {
                w.Write(c.CompanyToken);
                w.Write(c.CityToken);
                Write(w, c.Position);
                w.Write(c.NodeId.HasValue);
                if (c.NodeId is { } node) w.Write(node);
            }

            w.Write(index.Roads.Count);
            foreach (var r in index.Roads)
            {
                w.Write((int)r.Class);
                w.Write(r.Lod);
                w.Write(r.Points.Count);
                foreach (var p in r.Points) Write(w, p);
            }

            w.Write(index.Graph is not null);
            if (index.Graph is { } graph)
            {
                w.Write(graph.Nodes.Count);
                foreach (var n in graph.Nodes) Write(w, n);
                w.Write(graph.Edges.Count);
                foreach (var e in graph.Edges)
                {
                    w.Write(e.From);
                    w.Write(e.To);
                    w.Write(e.LengthM);
                    w.Write(e.Scale);
                    w.Write((int)e.Class);
                    w.Write(e.Polyline);
                }
            }
            w.Write(EndMarker);
        }
        return ms.ToArray();
    }

    private static MapIndex? Deserialize(byte[] bytes, string expectedKey)
    {
        using var ms = new MemoryStream(bytes, writable: false);
        using var r = new BinaryReader(ms, Encoding.UTF8);

        if (!r.ReadBytes(Magic.Length).AsSpan().SequenceEqual(Magic)) return null;
        if (r.ReadInt32() != SchemaVersion) return null;
        if (r.ReadString() != expectedKey) return null;

        var cityCount = Count(r, MinCityBytes);
        var cities = new List<MapCity>(cityCount);
        for (var n = 0; n < cityCount; n++)
        {
            var token = r.ReadString();
            var center = ReadPos(r);
            var country = ReadOptional(r);
            int? population = r.ReadBoolean() ? r.ReadInt32() : null;
            var offsets = new MapPos[Count(r, PosBytes)];
            for (var i = 0; i < offsets.Length; i++) offsets[i] = ReadPos(r);
            cities.Add(new MapCity(token, center, country, population, offsets));
        }

        var countryCount = Count(r, MinCountryBytes);
        var countries = new List<MapCountry>(countryCount);
        for (var n = 0; n < countryCount; n++)
            countries.Add(new MapCountry(r.ReadString(), ReadOptional(r), ReadPos(r)));

        var companyCount = Count(r, MinCompanyBytes);
        var companies = new List<MapCompany>(companyCount);
        for (var n = 0; n < companyCount; n++)
        {
            var company = r.ReadString();
            var city = r.ReadString();
            var position = ReadPos(r);
            int? node = r.ReadBoolean() ? r.ReadInt32() : null;
            companies.Add(new MapCompany(company, city, position, node));
        }

        var roadCount = Count(r, MinRoadBytes);
        var roads = new List<MapPolyline>(roadCount);
        for (var n = 0; n < roadCount; n++)
        {
            var roadClass = ReadEnum<MapRoadClass>(r);
            var lod = r.ReadInt32();
            var points = new MapPos[Count(r, PosBytes)];
            for (var i = 0; i < points.Length; i++) points[i] = ReadPos(r);
            roads.Add(new MapPolyline(roadClass, lod, points));
        }

        MapGraph? graph = null;
        if (r.ReadBoolean())
        {
            var nodes = new MapPos[Count(r, PosBytes)];
            for (var i = 0; i < nodes.Length; i++) nodes[i] = ReadPos(r);
            var edges = new MapEdge[Count(r, EdgeBytes)];
            for (var i = 0; i < edges.Length; i++)
                edges[i] = new MapEdge(r.ReadInt32(), r.ReadInt32(), r.ReadSingle(), r.ReadSingle(), ReadEnum<MapRoadClass>(r), r.ReadInt32());
            graph = new MapGraph(nodes, edges);
        }

        if (!r.ReadBytes(EndMarker.Length).AsSpan().SequenceEqual(EndMarker) || ms.Position != ms.Length)
            throw new InvalidDataException("Fim do índice de cache ausente ou com bytes sobrando.");
        return new MapIndex(cities, countries, companies, roads, graph);
    }

    /// <summary>Lê uma contagem e recusa valores impossíveis para o que resta do arquivo (evita alocar GB por lixo).</summary>
    private static int Count(BinaryReader r, int minBytesPerItem)
    {
        var n = r.ReadInt32();
        var remaining = r.BaseStream.Length - r.BaseStream.Position;
        if (n < 0 || (long)n * minBytesPerItem > remaining)
            throw new InvalidDataException($"Contagem inválida ({n}) no índice de cache.");
        return n;
    }

    private static T ReadEnum<T>(BinaryReader r) where T : struct, Enum
    {
        var value = (T)Enum.ToObject(typeof(T), r.ReadInt32());
        if (!Enum.IsDefined(value)) throw new InvalidDataException($"Valor de {typeof(T).Name} inválido no índice de cache.");
        return value;
    }

    private static void Write(BinaryWriter w, MapPos p)
    {
        w.Write(p.X);
        w.Write(p.Z);
    }

    private static MapPos ReadPos(BinaryReader r) => new(r.ReadSingle(), r.ReadSingle());

    private static void WriteOptional(BinaryWriter w, string? value)
    {
        w.Write(value is not null);
        if (value is not null) w.Write(value);
    }

    private static string? ReadOptional(BinaryReader r) => r.ReadBoolean() ? r.ReadString() : null;
}
