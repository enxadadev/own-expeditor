using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using OwnExpeditor.Data.Scs;

namespace OwnExpeditor.Data.Tests.Scs;

/// <summary>Monta pacotes HashFS v2 sintéticos (nada vem do jogo); os testes de corrupção alteram os bytes depois.</summary>
internal sealed class ScsArchiveBuilder
{
    public const int HeaderSize = 0x34;

    // offsets dos campos do cabeçalho
    public const int EntryCountAt = 0x0C;
    public const int EntryTableSizeAt = 0x10;
    public const int MetaWordCountAt = 0x14;
    public const int MetaTableSizeAt = 0x18;
    public const int EntryTableOffsetAt = 0x1C;
    public const int MetaTableOffsetAt = 0x24;

    private sealed record Item(string Path, bool IsDirectory, byte[] Stored, int Compression, int DeclaredSize);

    private readonly List<Item> _items = [];

    /// <summary>Arquivo guardado sem compressão (0x00).</summary>
    public ScsArchiveBuilder File(string path, byte[] data)
    {
        _items.Add(new Item(path, false, data, 0x00, data.Length));
        return this;
    }

    /// <summary>Arquivo comprimido com zlib (0x10); <paramref name="declaredSize"/> permite declarar um tamanho descompactado errado.</summary>
    public ScsArchiveBuilder ZFile(string path, byte[] data, int? declaredSize = null)
    {
        using var ms = new MemoryStream();
        using (var z = new ZLibStream(ms, CompressionLevel.Optimal, leaveOpen: true)) z.Write(data);
        _items.Add(new Item(path, false, ms.ToArray(), 0x10, declaredSize ?? data.Length));
        return this;
    }

    /// <summary>Arquivo com código de compressão arbitrário (ex.: 0x20 = não suportado); os bytes são guardados como estão.</summary>
    public ScsArchiveBuilder RawFile(string path, byte[] stored, int compression, int declaredSize)
    {
        _items.Add(new Item(path, false, stored, compression, declaredSize));
        return this;
    }

    /// <summary>Diretório com a lista de filhos no formato do pacote (subdiretórios começam com '/').</summary>
    public ScsArchiveBuilder Directory(string path, string[] subDirectories, string[] files)
    {
        var names = subDirectories.Select(d => "/" + d).Concat(files).Select(Encoding.UTF8.GetBytes).ToList();
        using var ms = new MemoryStream();
        ms.Write(BitConverter.GetBytes(names.Count));
        foreach (var n in names) ms.WriteByte((byte)n.Length);
        foreach (var n in names) ms.Write(n);
        return DirectoryRaw(path, ms.ToArray());
    }

    /// <summary>Diretório com conteúdo arbitrário (para testar listas corrompidas).</summary>
    public ScsArchiveBuilder DirectoryRaw(string path, byte[] content)
    {
        _items.Add(new Item(path, true, content, 0x00, content.Length));
        return this;
    }

    /// <summary>
    /// Monta o pacote. <paramref name="tamper"/> altera as tabelas já preenchidas, antes de comprimi-las
    /// (<c>entries</c>: 16 bytes por entrada; <c>meta</c>: N palavras de 4 bytes e depois N descritores de 16 bytes).
    /// </summary>
    public byte[] Build(Action<byte[], byte[]>? tamper = null)
    {
        using var ms = new MemoryStream();
        ms.Write(new byte[HeaderSize]);

        var blocks = new long[_items.Count];
        for (var i = 0; i < _items.Count; i++)
        {
            while (ms.Position % 16 != 0) ms.WriteByte(0);
            blocks[i] = ms.Position;
            ms.Write(_items[i].Stored);
        }

        // meta: uma palavra por entrada apontando para um descritor de 16 bytes; descritores logo depois das palavras
        var n = _items.Count;
        var meta = new byte[n * 4 + n * 16];
        var entries = new byte[n * 16];
        for (var i = 0; i < n; i++)
        {
            var item = _items[i];
            var descriptorWord = n + 4 * i;
            BinaryPrimitives.WriteUInt16LittleEndian(meta.AsSpan(i * 4), (ushort)(descriptorWord & 0xFFFF));
            meta[i * 4 + 2] = (byte)(descriptorWord >> 16);
            meta[i * 4 + 3] = 0x80;

            var d = meta.AsSpan(n * 4 + i * 16, 16);
            BinaryPrimitives.WriteUInt16LittleEndian(d, (ushort)(item.Stored.Length & 0xFFFF));
            d[2] = (byte)(item.Stored.Length >> 16);
            d[3] = (byte)item.Compression;
            BinaryPrimitives.WriteUInt16LittleEndian(d[4..], (ushort)(item.DeclaredSize & 0xFFFF));
            d[6] = (byte)(item.DeclaredSize >> 16);
            BinaryPrimitives.WriteUInt32LittleEndian(d[12..], (uint)(blocks[i] / 16));

            var e = entries.AsSpan(i * 16, 16);
            BinaryPrimitives.WriteUInt64LittleEndian(e, CityHash.Hash64(item.Path.Trim('/')));
            BinaryPrimitives.WriteInt32LittleEndian(e[8..], i);
            BinaryPrimitives.WriteUInt16LittleEndian(e[12..], 1);
            BinaryPrimitives.WriteUInt16LittleEndian(e[14..], (ushort)(item.IsDirectory ? 1 : 0));
        }

        tamper?.Invoke(entries, meta);
        var entryTable = ZCompress(entries);
        var metaTable = ZCompress(meta);
        while (ms.Position % 16 != 0) ms.WriteByte(0);
        var entryOffset = ms.Position;
        ms.Write(entryTable);
        var metaOffset = ms.Position;
        ms.Write(metaTable);

        var bytes = ms.ToArray();
        var h = bytes.AsSpan(0, HeaderSize);
        "SCS#"u8.CopyTo(h);
        BinaryPrimitives.WriteUInt16LittleEndian(h[4..], 2);
        "CITY"u8.CopyTo(h[8..]);
        BinaryPrimitives.WriteInt32LittleEndian(h[EntryCountAt..], n);
        BinaryPrimitives.WriteInt32LittleEndian(h[EntryTableSizeAt..], entryTable.Length);
        BinaryPrimitives.WriteInt32LittleEndian(h[MetaWordCountAt..], meta.Length / 4);
        BinaryPrimitives.WriteInt32LittleEndian(h[MetaTableSizeAt..], metaTable.Length);
        BinaryPrimitives.WriteInt64LittleEndian(h[EntryTableOffsetAt..], entryOffset);
        BinaryPrimitives.WriteInt64LittleEndian(h[MetaTableOffsetAt..], metaOffset);
        return bytes;
    }

    /// <summary>Atalho para alterar um campo do cabeçalho de um pacote já montado.</summary>
    public static byte[] PatchInt32(byte[] archive, int at, int value)
    {
        var copy = (byte[])archive.Clone();
        BinaryPrimitives.WriteInt32LittleEndian(copy.AsSpan(at), value);
        return copy;
    }

    public static byte[] PatchInt64(byte[] archive, int at, long value)
    {
        var copy = (byte[])archive.Clone();
        BinaryPrimitives.WriteInt64LittleEndian(copy.AsSpan(at), value);
        return copy;
    }

    private static byte[] ZCompress(byte[] data)
    {
        using var ms = new MemoryStream();
        using (var z = new ZLibStream(ms, CompressionLevel.Optimal, leaveOpen: true)) z.Write(data);
        return ms.ToArray();
    }
}
