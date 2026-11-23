using System.Buffers;
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
    private const int HeaderSize = 0x34;
    private const int EntrySize = 16;
    private const int MetaDescriptorSize = 16;

    /// <summary>Maior razão teórica de um fluxo deflate (1032:1); limita o tamanho declarado de uma tabela ao que o disco poderia conter.</summary>
    private const long MaxInflateRatio = 1032;

    private readonly FileStream _file;
    private readonly long _length;
    private readonly Dictionary<ulong, Entry> _entries;

    public string Path { get; }

    private readonly record struct Entry(bool IsDirectory, long Offset, int CompressedSize, int Size, int Compression);

    private HashFsArchive(string path, FileStream file, long length, Dictionary<ulong, Entry> entries)
    {
        Path = path;
        _file = file;
        _length = length;
        _entries = entries;
    }

    /// <exception cref="InvalidDataException">Pacote corrompido ou truncado (cabeçalho, tabelas ou offsets fora do arquivo).</exception>
    /// <exception cref="NotSupportedException">Versão do HashFS ou hash diferentes de v2/CITY.</exception>
    public static HashFsArchive Open(string path)
    {
        var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        try
        {
            var length = file.Length;
            if (length < HeaderSize)
                throw new InvalidDataException($"{path}: arquivo com {length} bytes, menor que o cabeçalho HashFS ({HeaderSize} bytes).");

            Span<byte> h = stackalloc byte[HeaderSize];
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

            if (entryCount < 0) throw new InvalidDataException($"{path}: número de entradas inválido ({entryCount}) em 0x0C.");
            if (metaWordCount < 0) throw new InvalidDataException($"{path}: tamanho da tabela de metadados inválido ({metaWordCount} palavras) em 0x14.");

            var index = ReadTable(file, path, "de entradas", entryTableOffset, entryTableSize, (long)entryCount * EntrySize);
            var meta = ReadTable(file, path, "de metadados", metaTableOffset, metaTableSize, (long)metaWordCount * 4);

            var entries = new Dictionary<ulong, Entry>(entryCount);
            for (var i = 0; i < entryCount; i++)
            {
                var e = index.AsSpan(i * EntrySize, EntrySize);
                var hash = BinaryPrimitives.ReadUInt64LittleEndian(e);
                var metaIndex = BinaryPrimitives.ReadInt32LittleEndian(e[8..]);
                var partCount = BinaryPrimitives.ReadUInt16LittleEndian(e[12..]);
                var isDir = (BinaryPrimitives.ReadUInt16LittleEndian(e[14..]) & 1) != 0;

                for (var p = 0; p < partCount; p++)
                {
                    var word = (long)metaIndex + p;
                    if (metaIndex < 0 || (word + 1) * 4 > meta.Length)
                        throw new InvalidDataException($"{path}: a entrada {i} aponta para fora da tabela de metadados (palavra {word} de {meta.Length / 4}).");
                    var d = meta.AsSpan((int)word * 4, 4);
                    if ((d[3] & 0x80) == 0) continue; // só a parte de dados interessa
                    var off = (BinaryPrimitives.ReadUInt16LittleEndian(d) | (d[2] << 16)) * 4;
                    if (off + MetaDescriptorSize > meta.Length)
                        throw new InvalidDataException($"{path}: o descritor de dados da entrada {i} fica fora da tabela de metadados (byte {off} de {meta.Length}).");
                    var dp = meta.AsSpan(off, MetaDescriptorSize);
                    var zsize = BinaryPrimitives.ReadUInt16LittleEndian(dp) | (dp[2] << 16);
                    var compression = dp[3] & 0xF0;
                    var usize = BinaryPrimitives.ReadUInt16LittleEndian(dp[4..]) | (dp[6] << 16);
                    var block = BinaryPrimitives.ReadUInt32LittleEndian(dp[12..]);
                    entries[hash] = new Entry(isDir, block * 16L, zsize, usize, compression);
                    break;
                }
            }
            return new HashFsArchive(path, file, length, entries);
        }
        catch (EndOfStreamException e)
        {
            file.Dispose();
            throw new InvalidDataException($"{path}: arquivo truncado ({e.Message})", e);
        }
        catch
        {
            file.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Lê e descomprime uma tabela do índice. Antes de alocar, confere que a tabela cabe no arquivo e que o tamanho
    /// descompactado declarado é possível para o tamanho comprimido (evita alocar GB por um cabeçalho adulterado).
    /// </summary>
    private static byte[] ReadTable(FileStream file, string path, string what, long offset, int size, long expectedSize)
    {
        var length = file.Length;
        if (size < 0 || offset < 0 || offset > length || size > length - offset)
            throw new InvalidDataException(
                $"{path}: a tabela {what} (offset 0x{offset:X}, {size} bytes) fica fora do arquivo ({length} bytes).");
        if (expectedSize > Array.MaxLength || expectedSize > size * MaxInflateRatio)
            throw new InvalidDataException(
                $"{path}: a tabela {what} declara {expectedSize} bytes descompactados, impossível para {size} bytes comprimidos.");

        var raw = new byte[size];
        file.Position = offset;
        file.ReadExactly(raw);
        return Inflate(path, $"tabela {what}", raw, (int)expectedSize);
    }

    public bool Exists(string path) => _entries.ContainsKey(Hash(path));

    public bool DirectoryExists(string path) => _entries.TryGetValue(Hash(path), out var e) && e.IsDirectory;

    /// <summary>Conteúdo de um diretório: subdiretórios e arquivos (só nomes).</summary>
    public (IReadOnlyList<string> Directories, IReadOnlyList<string> Files) List(string path)
    {
        if (!_entries.TryGetValue(Hash(path), out var e) || !e.IsDirectory)
            return ([], []);

        var data = ReadEntry(e);
        if (data.Length < 4)
            throw new InvalidDataException($"{Path}: diretório '{path}' com {data.Length} bytes (cabeçalho incompleto).");
        var count = BinaryPrimitives.ReadInt32LittleEndian(data);
        if (count < 0 || count > data.Length - 4)
            throw new InvalidDataException($"{Path}: diretório '{path}' declara {count} nomes em {data.Length} bytes.");

        var pos = 4 + count;
        var dirs = new List<string>();
        var files = new List<string>();
        for (var i = 0; i < count; i++)
        {
            var len = data[4 + i];
            if (pos + len > data.Length)
                throw new InvalidDataException($"{Path}: diretório '{path}': o nome {i} ({len} bytes) passa do fim do conteúdo ({data.Length} bytes).");
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

    /// <summary>
    /// Abre um arquivo do pacote como <see cref="MemoryStream"/> somente leitura sobre um buffer do <see cref="ArrayPool{T}"/>
    /// (sem alocar o conteúdo; o buffer volta ao pool no <c>Dispose</c>). <c>TryGetBuffer</c> expõe os bytes sem cópia.
    /// Null se não existir.
    /// </summary>
    public MemoryStream? OpenPooled(string path)
    {
        if (!_entries.TryGetValue(Hash(path), out var e) || e.IsDirectory) return null;
        if (e.Compression is not (0x00 or 0x10)) throw new NotSupportedException($"{Path}: compressão 0x{e.Compression:X2} não suportada.");

        CheckRange(e, path);
        var pool = ArrayPool<byte>.Shared;
        byte[]? raw = pool.Rent(Math.Max(e.CompressedSize, 1));
        try
        {
            lock (_file)
            {
                _file.Position = e.Offset;
                try { _file.ReadExactly(raw.AsSpan(0, e.CompressedSize)); }
                catch (EndOfStreamException) { throw Truncated(path, e); }
            }
            if (e.Compression == 0x00)
            {
                var stream = new PooledMemoryStream(raw, e.CompressedSize);
                raw = null;
                return stream;
            }

            var output = pool.Rent(Math.Max(e.Size, 1));
            try
            {
                using var zlib = new ZLibStream(new MemoryStream(raw, 0, e.CompressedSize, writable: false), CompressionMode.Decompress);
                zlib.ReadExactly(output.AsSpan(0, e.Size));
                return new PooledMemoryStream(output, e.Size);
            }
            catch (Exception ex) when (ex is EndOfStreamException or InvalidDataException)
            {
                pool.Return(output);
                throw new InvalidDataException($"{Path}: '{path}' não descompacta em {e.Size} bytes (zlib corrompido ou tamanho declarado errado).", ex);
            }
            catch
            {
                pool.Return(output);
                throw;
            }
        }
        finally
        {
            if (raw is not null) pool.Return(raw);
        }
    }

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
        if (e.Compression is not (0x00 or 0x10))
            throw new NotSupportedException($"{Path}: compressão 0x{e.Compression:X2} não suportada.");
        CheckRange(e, null);
        var raw = new byte[e.CompressedSize];
        lock (_file)
        {
            _file.Position = e.Offset;
            try { _file.ReadExactly(raw); }
            catch (EndOfStreamException) { throw Truncated(null, e); }
        }
        return e.Compression switch
        {
            0x00 => raw,
            _ => Inflate(Path, "conteúdo de arquivo", raw, e.Size),
        };
    }

    /// <summary>O bloco de dados tem de caber no arquivo (offset e tamanho vêm do índice, que pode estar corrompido).</summary>
    private void CheckRange(Entry e, string? entryPath)
    {
        if (e.Offset < 0 || e.CompressedSize < 0 || e.Size < 0 || e.Offset > _length || e.CompressedSize > _length - e.Offset)
            throw Truncated(entryPath, e);
    }

    private InvalidDataException Truncated(string? entryPath, Entry e) => new(
        $"{Path}: {(entryPath is null ? "entrada" : $"'{entryPath}'")} com dados fora do arquivo (offset 0x{e.Offset:X}, {e.CompressedSize} bytes; arquivo com {_length} bytes).");

    private static string Join(string root, string name) => root.Length == 0 ? name : root + "/" + name;

    private static ulong Hash(string path) => CityHash.Hash64(path.Trim('/'));

    private static byte[] Inflate(string archivePath, string what, byte[] data, int expectedSize)
    {
        try
        {
            using var zlib = new ZLibStream(new MemoryStream(data), CompressionMode.Decompress);
            var output = new byte[expectedSize];
            zlib.ReadExactly(output);
            return output;
        }
        catch (Exception e) when (e is EndOfStreamException or InvalidDataException)
        {
            throw new InvalidDataException($"{archivePath}: {what} não descompacta em {expectedSize} bytes (zlib corrompido ou tamanho declarado errado).", e);
        }
    }

    public void Dispose() => _file.Dispose();
}
