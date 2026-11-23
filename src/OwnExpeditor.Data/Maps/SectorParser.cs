using System.Buffers.Binary;
using OwnExpeditor.Domain.Maps;

namespace OwnExpeditor.Data.Maps;

/// <summary>Cidade lida de um item City (uma área da cidade; várias por token).</summary>
internal readonly record struct RawCity(string Token, ulong NodeUid, float Width, float Height, bool Hidden, MapPos Center = default);

/// <summary>Empresa lida de um item Company; <c>HeaderX/Z</c> = canto do item (contingência se o nó não existir).</summary>
internal readonly record struct RawCompany(string CompanyToken, string CityToken, ulong NodeUid, float HeaderX, float HeaderZ);

/// <summary>Nó da tabela do setor, em metros.</summary>
internal readonly record struct RawNode(ulong Uid, float X, float Z);

/// <summary>Resultado de <see cref="SectorParser.Parse"/>; reutilizável (<see cref="Clear"/>).</summary>
internal sealed class SectorScratch
{
    public List<RawCity> Cities { get; } = [];
    public List<RawCompany> Companies { get; } = [];
    public List<RawNode> Nodes { get; } = [];

    public void Clear()
    {
        Cities.Clear();
        Companies.Clear();
        Nodes.Clear();
    }
}

/// <summary>
/// Leitor de um <c>.base</c> do mapa na versão 907 (docs/scs-map-format.md §4 a §6). Os itens não têm tamanho próprio:
/// cada tipo é medido pela regra do documento, e o fim do arquivo tem de fechar exatamente (itens + nós + cauda).
/// Qualquer divergência torna o setor ilegível (nada é devolvido parcialmente).
/// </summary>
internal static class SectorParser
{
    public const uint SupportedVersion = 907;

    private const int HeaderSize = 0x14;
    private const int CommonHeader = 0x34;
    private const int FlagsSize = 5;
    private const int NodeSize = 0x38;

    private const uint TypeRoad = 3;
    private const uint TypePrefab = 4;
    private const uint TypeCompany = 6;
    private const uint TypeCity = 12;

    /// <summary>Lê o setor para <paramref name="result"/>. Devolve null se ok ou o motivo (com offset) se o setor é ilegível.</summary>
    public static string? Parse(ReadOnlySpan<byte> d, SectorScratch result)
    {
        result.Clear();
        if (d.Length < HeaderSize) return $"arquivo com {d.Length} bytes (cabeçalho incompleto)";
        var version = BinaryPrimitives.ReadUInt32LittleEndian(d);
        if (version != SupportedVersion) return $"versão do setor {version} (suportada: {SupportedVersion})";

        var itemCount = BinaryPrimitives.ReadInt32LittleEndian(d[0x10..]);
        if (itemCount < 0 || itemCount > (d.Length - HeaderSize) / CommonHeader)
            return $"contagem de itens inválida ({itemCount}) em 0x10";

        long pos = HeaderSize;
        for (var i = 0; i < itemCount; i++)
        {
            if (!I32(d, pos, out var rawType)) return $"item {i} além do fim do arquivo (offset 0x{pos:X})";
            var type = (uint)rawType;
            var end = Measure(type, d, pos);
            if (end < 0 || end > d.Length) return $"item {i} (tipo {type}) com tamanho inválido ou além do fim (offset 0x{pos:X})";

            if (type == TypeCity)
            {
                var err = ReadCity(d, pos, result);
                if (err is not null) return err;
            }
            else if (type == TypeCompany)
            {
                var err = ReadCompany(d, pos, result);
                if (err is not null) return err;
            }
            pos = end;
        }

        if (!I32(d, pos, out var nodeCount) || nodeCount < 0) return $"tabela de nós ausente ou inválida (offset 0x{pos:X})";
        var nodesStart = pos + 4;
        pos = nodesStart + (long)nodeCount * NodeSize;
        if (pos > d.Length) return $"tabela de nós de {nodeCount} nós passa do fim do arquivo (offset 0x{nodesStart:X})";
        if (!I32(d, pos, out var tail) || tail < 0) return $"cauda ausente ou inválida (offset 0x{pos:X})";
        pos += 4 + 8L * tail;
        if (pos != d.Length) return $"o setor não fecha no fim do arquivo (esperado 0x{pos:X}, tamanho 0x{d.Length:X})";

        // Uma passada pela tabela de nós: guarda todos em result.Nodes e, só se o setor tem City, a posição (primeira ocorrência)
        // dos nós que elas usam. Custo O(nós + cidades), sem busca linear por cidade.
        Dictionary<ulong, MapPos?>? cityNodes = null;
        if (result.Cities.Count > 0)
        {
            cityNodes = new Dictionary<ulong, MapPos?>(result.Cities.Count);
            foreach (var city in result.Cities) cityNodes[city.NodeUid] = null;
        }

        for (var i = 0; i < nodeCount; i++)
        {
            var n = d.Slice((int)nodesStart + i * NodeSize, NodeSize);
            var uid = BinaryPrimitives.ReadUInt64LittleEndian(n);
            var position = Position(n);
            result.Nodes.Add(new RawNode(uid, position.X, position.Z));
            if (cityNodes is not null && cityNodes.TryGetValue(uid, out var known) && known is null) cityNodes[uid] = position;
        }

        if (cityNodes is not null)
        {
            for (var i = 0; i < result.Cities.Count; i++)
            {
                var city = result.Cities[i];
                if (cityNodes[city.NodeUid] is not { } center)
                {
                    result.Clear();
                    return $"cidade '{city.Token}' aponta para nó {city.NodeUid} fora da tabela do próprio setor";
                }
                result.Cities[i] = city with { Center = center };
            }
        }
        return null;
    }

    /// <summary>x e z do nó em metros (gravados em 1/256 m; dividir como double evita perda no float).</summary>
    private static MapPos Position(ReadOnlySpan<byte> node) => new(
        (float)(BinaryPrimitives.ReadInt32LittleEndian(node[0x08..]) / 256.0),
        (float)(BinaryPrimitives.ReadInt32LittleEndian(node[0x10..]) / 256.0));

    private static string? ReadCity(ReadOnlySpan<byte> d, long start, SectorScratch result)
    {
        var f = (int)start + CommonHeader;
        var hidden = (d[f] & 1) != 0;
        if (!ScsToken.TryDecode(BinaryPrimitives.ReadUInt64LittleEndian(d[(f + 0x05)..]), out var token))
            return $"City com token inválido (offset 0x{start:X})";
        result.Cities.Add(new RawCity(
            token,
            BinaryPrimitives.ReadUInt64LittleEndian(d[(f + 0x15)..]),
            BinaryPrimitives.ReadSingleLittleEndian(d[(f + 0x0D)..]),
            BinaryPrimitives.ReadSingleLittleEndian(d[(f + 0x11)..]),
            hidden));
        return null;
    }

    private static string? ReadCompany(ReadOnlySpan<byte> d, long start, SectorScratch result)
    {
        var f = (int)start + CommonHeader;
        if (!ScsToken.TryDecode(BinaryPrimitives.ReadUInt64LittleEndian(d[(f + 0x05)..]), out var city)
            || !ScsToken.TryDecode(BinaryPrimitives.ReadUInt64LittleEndian(d[(f + 0x15)..]), out var company))
            return $"Company com token inválido (offset 0x{start:X})";
        result.Companies.Add(new RawCompany(
            company,
            city,
            BinaryPrimitives.ReadUInt64LittleEndian(d[(f + 0x1D)..]),
            BinaryPrimitives.ReadSingleLittleEndian(d[((int)start + 0x0C)..]),
            BinaryPrimitives.ReadSingleLittleEndian(d[((int)start + 0x14)..])));
        return null;
    }

    // ---- medição dos itens (versão 907); devolve o offset do fim do item, ou -1 ----

    internal static long Measure(uint type, ReadOnlySpan<byte> d, long s)
    {
        var f = s + CommonHeader;      // F: início dos flags
        var b = f + FlagsSize;         // início dos campos específicos
        switch (type)
        {
            case 1: // Terrain
            {
                var p = Arr(d, b + 0xEE, 0x14);
                p = Quad(d, p);
                p = Quad(d, p);
                return p < 0 ? -1 : p + 0x20;
            }
            case 2: return Arr(d, b + 0x2C, 4);                                // Building
            case TypeRoad: return s + CommonHeader + 0xD9;
            case TypePrefab: return Prefab(d, f);
            case 5: { var p = Arr(d, b + 0x18, 8); return p < 0 ? -1 : p + 0x24; }  // Model
            case TypeCompany:
            {
                if (!I32(d, f + 0x25, out var n) || n < 0) return -1;
                return f + 0x29 + 12L * n;
            }
            case 7: return Arr(d, b + 0x10, 8);                                // Service
            case 8: return Arr(d, b, 8);                                       // CutPlane
            case TypeCity: return s + 0x51;
            case 18: return s + CommonHeader + 0x15;                           // MapOverlay
            case 19: return s + CommonHeader + 0x29;                           // Ferry
            case 22: return Arr(d, b + 0x1C, 8);                               // Garage
            case 34: return Trigger(d, b);
            case 35: return Arr(d, b + 0x10, 8);                               // FuelPump
            case 36: return RoadSide(d, b);
            case 37: return s + CommonHeader + 0x1D;                           // BusStop
            case 38: { var p = Arr(d, b, 8); p = Arr(d, p, 8); return p < 0 ? -1 : p + 0x0C; }  // TrafficRule
            case 39: { var p = Arr(d, b + 0xF1, 0x14); return Quad(d, p); }    // BezierPatch
            case 41: // Trajectory
            {
                var p = Arr(d, b, 8);
                p = p < 0 ? -1 : p + 8;
                p = Arr(d, p, 0x1C);
                p = Arr(d, p, 0x10);
                return Arr(d, p, 8);
            }
            case 42: { var p = Arr(d, b, 8); return p < 0 ? -1 : p + 4; }      // MapArea
            case 44: return Curve(d, b);
            case 46: return Cutscene(d, b);
            case 48: return Arr(d, b + 0x10, 8);                               // VisibilityArea
            default: return -1;
        }
    }

    private static bool I32(ReadOnlySpan<byte> d, long pos, out int value)
    {
        if (pos < 0 || pos + 4 > d.Length)
        {
            value = 0;
            return false;
        }
        value = BinaryPrimitives.ReadInt32LittleEndian(d[(int)pos..]);
        return true;
    }

    private static bool U16(ReadOnlySpan<byte> d, long pos, out int value)
    {
        if (pos < 0 || pos + 2 > d.Length)
        {
            value = 0;
            return false;
        }
        value = BinaryPrimitives.ReadUInt16LittleEndian(d[(int)pos..]);
        return true;
    }

    /// <summary>Lê <c>i32 n</c> em <paramref name="pos"/> e devolve o fim de <c>n · elemSize</c> bytes; -1 se inválido ou além do arquivo.</summary>
    private static long Arr(ReadOnlySpan<byte> d, long pos, int elemSize)
    {
        if (pos < 0 || !I32(d, pos, out var n) || n < 0) return -1;
        var end = pos + 4 + (long)n * elemSize;
        return end > d.Length ? -1 : end;
    }

    private static long Quad(ReadOnlySpan<byte> d, long p)
    {
        if (p < 0 || !U16(d, p, out var nMat)) return -1;
        p += 2 + 0x0A * (long)nMat;
        if (!U16(d, p, out var nCor)) return -1;
        p += 2 + 4L * nCor + 4;
        p = Arr(d, p, 4);
        p = Arr(d, p, 0x10);
        return Arr(d, p, 0x10);
    }

    private static long Prefab(ReadOnlySpan<byte> d, long f)
    {
        var p = Arr(d, f + 0x15, 8);                       // peças adicionais
        if (p < 0 || !I32(d, p, out var nodes)) return -1; // nós ligados
        p = Arr(d, p, 8);
        p = Arr(d, p, 8);                                  // itens conectados
        if (p < 0) return -1;
        return p + 8 + 1 + 1 + 12L * nodes + 8;
    }

    private static long Trigger(ReadOnlySpan<byte> d, long b)
    {
        var p = Arr(d, b, 8);                              // tags
        if (p < 0 || !I32(d, p, out var nodes)) return -1;
        p = Arr(d, p, 8);                                  // nós
        if (p < 0 || !I32(d, p, out var actions) || actions < 0) return -1;
        p += 4;
        for (var i = 0; i < actions; i++)
        {
            if (!I32(d, p + 8, out var overrides)) return -1;
            p += 12;
            if (overrides < 0) continue;
            p += 4L * overrides;
            if (!I32(d, p, out var parameters) || parameters < 0) return -1;
            p += 4;
            for (var k = 0; k < parameters; k++)
            {
                if (!I32(d, p, out var len) || len < 0) return -1;
                p += 4 + 4 + (long)len;
            }
            p = Arr(d, p, 8);                              // tags alvo da ação
            if (p < 0) return -1;
            p += 8;
        }
        return nodes == 1 ? p + 4 : p;
    }

    private static long RoadSide(ReadOnlySpan<byte> d, long b)
    {
        var at = b + 0x20;
        if (at >= d.Length) return -1;
        var signs = (sbyte)d[(int)at];
        if (signs < 0) return -1;
        var p = at + 1 + 0x18L * signs;
        if (!I32(d, p, out var len) || len < 0) return -1;
        p += 4;
        if (len != 0)
        {
            p += 4 + (long)len;
            if (!I32(d, p, out var overrides) || overrides < 0) return -1;
            p += 4;
            for (var i = 0; i < overrides; i++)
            {
                if (p + 9 > d.Length) return -1;
                var flags = d[(int)p + 8];
                p += 9 + ((flags & 1) != 0 ? 2 : 0) + ((flags & 2) != 0 ? 8 : 0);
            }
        }
        if (!I32(d, p, out var nSigns) || nSigns < 0) return -1;
        p += 4;
        for (var i = 0; i < nSigns; i++)
        {
            if (!I32(d, p + 0x0C, out var attrs) || attrs < 0) return -1;
            p += 0x0C + 4;
            for (var k = 0; k < attrs; k++)
            {
                if (!U16(d, p, out var attrType)) return -1;
                p += 2 + 4;
                switch (attrType)
                {
                    case 5:
                        if (!I32(d, p, out var len2) || len2 < 0) return -1;
                        p += 4 + 4 + (long)len2;
                        break;
                    case 6: p += 8; break;
                    case 1: p += 1; break;
                    default: p += 4; break;
                }
            }
            if (p > d.Length) return -1;
        }
        return p;
    }

    private static long Curve(ReadOnlySpan<byte> d, long b)
    {
        if (!I32(d, b + 0x20 + 4, out var mask)) return -1;
        var p = b + 0x20 + 4 + 4;
        for (var bit = 0; bit < 32; bit++)
        {
            if ((mask & (1 << bit)) == 0) continue;
            p += 0x4C;
            p = Arr(d, p, 4);
            if (p < 0) return -1;
            p += 0x14;
        }
        return p;
    }

    private static long Cutscene(ReadOnlySpan<byte> d, long b)
    {
        var p = Arr(d, b, 8);
        if (p < 0) return -1;
        p += 8;
        if (!I32(d, p, out var actions) || actions < 0) return -1;
        p += 4;
        for (var i = 0; i < actions; i++)
        {
            p = Arr(d, p, 4);                              // números
            if (p < 0 || !I32(d, p, out var texts) || texts < 0) return -1;
            p += 4;
            for (var k = 0; k < texts; k++)
            {
                if (!I32(d, p, out var len) || len < 0) return -1;
                p += 4 + 4 + (long)len;
            }
            p = Arr(d, p, 8);                              // alvos
            if (p < 0) return -1;
            p += 8;
        }
        return p;
    }
}
