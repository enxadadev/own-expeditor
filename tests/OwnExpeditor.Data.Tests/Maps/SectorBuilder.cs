using System.Text;

namespace OwnExpeditor.Data.Tests.Maps;

/// <summary>Monta setores .base sintéticos (versão 907) byte a byte; nada vem do jogo.</summary>
internal sealed class SectorBuilder
{
    public const int HeaderSize = 0x14;
    public const int CommonHeader = 0x34;
    public const int CitySize = 0x51;

    private readonly List<byte[]> _items = [];
    private readonly List<byte[]> _nodes = [];

    public uint Version { get; set; } = 907;

    public SectorBuilder City(string token, ulong nodeUid, float width = 100, float height = 100, bool hidden = false)
    {
        var w = NewItem(12, uid: 1000 + (ulong)_items.Count);
        w.Write((byte)(hidden ? 1 : 0)); w.Write(new byte[4]);   // flags (5 bytes)
        w.Write(TokenCodec.Encode(token));
        w.Write(width);
        w.Write(height);
        w.Write(nodeUid);
        return Add(w);
    }

    public SectorBuilder Company(string companyToken, string cityToken, ulong nodeUid, ulong prefabUid = 1)
    {
        var w = NewItem(6, uid: 2000 + (ulong)_items.Count);
        w.Write(new byte[5]);
        w.Write(TokenCodec.Encode(cityToken));
        w.Write(prefabUid);
        w.Write(TokenCodec.Encode(companyToken));
        w.Write(nodeUid);
        w.Write(0);                                              // nós extras
        return Add(w);
    }

    /// <summary>Road: tipo 3, tamanho fixo 269.</summary>
    public SectorBuilder Road()
    {
        var w = NewItem(3, uid: 3000 + (ulong)_items.Count);
        w.Write(new byte[0xD9]);
        return Add(w);
    }

    /// <summary>MapOverlay: tipo 18, tamanho fixo 73.</summary>
    public SectorBuilder Overlay()
    {
        var w = NewItem(18, uid: 4000 + (ulong)_items.Count);
        w.Write(new byte[0x15]);
        return Add(w);
    }

    /// <summary>Service: tipo 7, tamanho variável (<c>i32 n</c> em F+5+0x10, depois 8·n).</summary>
    public SectorBuilder Service(int n)
    {
        var w = NewItem(7, uid: 5000 + (ulong)_items.Count);
        w.Write(new byte[5 + 0x10]);
        w.Write(n);
        w.Write(new byte[8 * n]);
        return Add(w);
    }

    /// <summary>Item de tipo e corpo arbitrários (cabeçalho comum de 0x34 bytes + <paramref name="body"/>), para casos hostis.</summary>
    public SectorBuilder Raw(uint type, byte[] body)
    {
        var w = NewItem(type, uid: 7000 + (ulong)_items.Count);
        w.Write(body);
        return Add(w);
    }

    /// <summary>Corpo com 5 bytes de flags seguidos de inteiros de 32 bits (campos específicos do item).</summary>
    public static byte[] Body(params int[] ints)
    {
        var ms = new MemoryStream();
        var w = new BinaryWriter(ms);
        w.Write(new byte[5]);
        foreach (var i in ints) w.Write(i);
        return ms.ToArray();
    }

    public SectorBuilder Unknown(uint type = 99)
    {
        var w = NewItem(type, uid: 6000 + (ulong)_items.Count);
        w.Write(new byte[16]);
        return Add(w);
    }

    /// <summary>Nó da tabela: x/z em metros (gravados em 1/256 m).</summary>
    public SectorBuilder Node(ulong uid, float xMeters, float zMeters)
    {
        var ms = new MemoryStream();
        var w = new BinaryWriter(ms);
        w.Write(uid);
        w.Write((int)(xMeters * 256));
        w.Write(0);
        w.Write((int)(zMeters * 256));
        w.Write(1f); w.Write(0f); w.Write(0f); w.Write(0f);     // rotação neutra
        w.Write(0UL); w.Write(0UL);                              // ligações A/B
        w.Write(0x3000001u);
        _nodes.Add(ms.ToArray());
        return this;
    }

    public byte[] Build()
    {
        var ms = new MemoryStream();
        var w = new BinaryWriter(ms);
        w.Write(Version);
        w.Write(0x00750461u);
        w.Write(0u);
        w.Write(3u);
        w.Write(_items.Count);
        foreach (var i in _items) w.Write(i);
        w.Write(_nodes.Count);
        foreach (var n in _nodes) w.Write(n);
        w.Write(0);                                              // cauda: 0 filhos
        return ms.ToArray();
    }

    public static byte[] Truncate(byte[] data, int removedBytes) => data[..^removedBytes];

    public static byte[] Extend(byte[] data, int extraBytes) => [.. data, .. new byte[extraBytes]];

    /// <summary>Corta o arquivo no meio do primeiro item City (contagem diz 1+ itens).</summary>
    public static byte[] CutInsideFirstItem(byte[] data, int bytesIntoItem = 30) => data[..(HeaderSize + bytesIntoItem)];

    private BinaryWriter NewItem(uint type, ulong uid)
    {
        var w = new BinaryWriter(new MemoryStream());
        w.Write(type);
        w.Write(uid);
        w.Write(9999f); w.Write(0f); w.Write(9999f);             // x/z do cabeçalho: propositalmente errados
        w.Write(new byte[28]);
        return w;
    }

    private SectorBuilder Add(BinaryWriter w)
    {
        _items.Add(((MemoryStream)w.BaseStream).ToArray());
        return this;
    }
}

/// <summary>Token SCS em base 38 (primeiro caractere no dígito menos significativo).</summary>
internal static class TokenCodec
{
    private const string Alphabet = "0123456789abcdefghijklmnopqrstuvwxyz_";

    public static ulong Encode(string token)
    {
        ulong v = 0;
        for (var i = token.Length - 1; i >= 0; i--)
            v = v * 38 + (ulong)(Alphabet.IndexOf(token[i]) + 1);
        return v;
    }
}
