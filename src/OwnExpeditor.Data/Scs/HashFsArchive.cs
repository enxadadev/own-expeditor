using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;

namespace OwnExpeditor.Data.Scs;

/// <summary>
/// Leitor de arquivos .scs no formato HashFS v2 (jogo 1.50+).
/// Spec: github.com/Wilps93/scs_tools FORMAT.md (MIT). Lê só o índice ao abrir;
/// o conteúdo de cada arquivo é lido sob demanda. Texturas (GDeflate) não são suportadas.
/// </summary>
public sealed class HashFsArchive : IDisposable
{
    private readonly FileStream _file;
    private readonly Dictionary<ulong, Entry> _entries;

    public string Path { get; }

    private readonly record struct Entry(bool IsDirectory, long Offset, int CompressedSize, int Size, int Compression);

    private HashFsArchive(string path, FileStream file, Dictionary<ulong, Entry> entries)
    {
        Path = path;
        _file = file;
        _entries = entries;
    }

    public static HashFsArchive Open(string path)
    {
        var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        try
        {
            Span<byte> h = stackalloc byte[0x34];
            file.ReadExactly(h);
            if (!h[..4].SequenceEqual("SCS#"u8)) throw new InvalidDataException($"{path}: não é um arquivo HashFS.");
            var version = BinaryPrimitives.ReadUInt16LittleEndian(h[4..]);
            if (version != 2) throw new NotSupportedException($"{path}: HashFS versão {version} não suportada.");
            if (!h[8..12].SequenceEqual("CITY"u8)) throw new NotSupportedException($"{path}: hash desconhecido.");

            var entryCount = BinaryPrimitives.ReadInt32LittleEndian(h[0x0C..]);
            var entryTableSize = BinaryPrimitives.ReadInt32LittleEndian(h[0x10..]);
            var metaWordCount = BinaryPrimitives.ReadInt32LittleEndian(h[0x14..]);
            var metaTableSize = BinaryPrimitives.ReadInt32LittleEndian(h[0x18..]);
            var entryTableOffset = BinaryPrimitives.ReadInt64LittleEndian(h[0x1C..]);
            var metaTableOffset = BinaryPrimitives.ReadInt64LittleEndian(h[0x24..]);

            var index = Inflate(ReadAt(file, entryTableOffset, entryTableSize), entryCount * 16);
            var meta = Inflate(ReadAt(file, metaTableOffset, metaTableSize), metaWordCount * 4);

            var entries = new Dictionary<ulong, Entry>(entryCount);
            for (var i = 0; i < entryCount; i++)
            {
                var e = index.AsSpan(i * 16, 16);
                var hash = BinaryPrimitives.ReadUInt64LittleEndian(e);
                var metaIndex = BinaryPrimitives.ReadInt32LittleEndian(e[8..]);
                var partCount = BinaryPrimitives.ReadUInt16LittleEndian(e[12..]);
                var isDir = (BinaryPrimitives.ReadUInt16LittleEndian(e[14..]) & 1) != 0;

                for (var p = 0; p < partCount; p++)
                {
                    var d = meta.AsSpan((metaIndex + p) * 4, 4);
                    if ((d[3] & 0x80) == 0) continue; // só a parte de dados interessa
                    var off = (BinaryPrimitives.ReadUInt16LittleEndian(d) | (d[2] << 16)) * 4;
                    var dp = meta.AsSpan(off, 16);
                    var zsize = BinaryPrimitives.ReadUInt16LittleEndian(dp) | (dp[2] << 16);
                    var compression = dp[3] & 0xF0;
                    var usize = BinaryPrimitives.ReadUInt16LittleEndian(dp[4..]) | (dp[6] << 16);
                    var block = BinaryPrimitives.ReadUInt32LittleEndian(dp[12..]);
                    entries[hash] = new Entry(isDir, block * 16L, zsize, usize, compression);
                    break;
                }
            }
            return new HashFsArchive(path, file, entries);
        }
        catch
        {
            file.Dispose();
            throw;
        }
    }

    public bool Exists(string path) => _entries.ContainsKey(Hash(path));

    public bool DirectoryExists(string path) => _entries.TryGetValue(Hash(path), out var e) && e.IsDirectory;

    /// <summary>Conteúdo de um diretório: subdiretórios e arquivos (só nomes).</summary>
    public (IReadOnlyList<string> Directories, IReadOnlyList<string> Files) List(string path)
    {
        if (!_entries.TryGetValue(Hash(path), out var e) || !e.IsDirectory)
            return ([], []);

        var data = ReadEntry(e);
        var count = BinaryPrimitives.ReadInt32LittleEndian(data);
        var pos = 4 + count;
        var dirs = new List<string>();
        var files = new List<string>();
        for (var i = 0; i < count; i++)
        {
            var len = data[4 + i];
            var name = Encoding.UTF8.GetString(data, pos, len);
            pos += len;
            if (name.StartsWith('/')) dirs.Add(name[1..]);
            else files.Add(name);
        }
        return (dirs, files);
    }

    /// <summary>Lê um arquivo do pacote, ou null se não existir.</summary>
    public byte[]? Read(string path) =>
        _entries.TryGetValue(Hash(path), out var e) && !e.IsDirectory ? ReadEntry(e) : null;

    /// <summary>Todos os arquivos abaixo de um diretório, com caminho completo.</summary>
    public IEnumerable<string> Walk(string root)
    {
        var (dirs, files) = List(root);
        foreach (var f in files) yield return Join(root, f);
        foreach (var d in dirs)
        foreach (var f in Walk(Join(root, d)))
            yield return f;
    }

    private byte[] ReadEntry(Entry e)
    {
        byte[] raw;
        lock (_file) raw = ReadAt(_file, e.Offset, e.CompressedSize);
        return e.Compression switch
        {
            0x00 => raw,
            0x10 => Inflate(raw, e.Size),
            _ => throw new NotSupportedException($"Compressão 0x{e.Compression:X2} não suportada."),
        };
    }

    private static string Join(string root, string name) => root.Length == 0 ? name : root + "/" + name;

    private static ulong Hash(string path) => CityHash.Hash64(path.Trim('/'));

    private static byte[] ReadAt(FileStream f, long offset, int size)
    {
        var buf = new byte[size];
        f.Position = offset;
        f.ReadExactly(buf);
        return buf;
    }

    private static byte[] Inflate(byte[] data, int expectedSize)
    {
        using var zlib = new ZLibStream(new MemoryStream(data), CompressionMode.Decompress);
        var output = new byte[expectedSize];
        zlib.ReadExactly(output);
        return output;
    }

    public void Dispose() => _file.Dispose();
}
