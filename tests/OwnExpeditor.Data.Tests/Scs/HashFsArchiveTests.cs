using System.Buffers;
using System.Buffers.Binary;
using System.Text;
using OwnExpeditor.Data.Scs;

namespace OwnExpeditor.Data.Tests.Scs;

public class HashFsArchiveTests
{
    private static readonly byte[] Payload = Encoding.UTF8.GetBytes(string.Concat(Enumerable.Repeat("conteudo do arquivo ", 8)));

    private static HashFsArchive Open(TempDir tmp, byte[] archive)
    {
        var path = tmp.Combine("test.scs");
        File.WriteAllBytes(path, archive);
        return HashFsArchive.Open(path);
    }

    private static byte[] Valid() => new ScsArchiveBuilder()
        .File("def/plain.txt", Payload)
        .ZFile("def/zipped.txt", Payload)
        .Directory("def", [], ["plain.txt", "zipped.txt"])
        .Build();

    // ---- caminho feliz ----

    [Fact]
    public void Open_ValidArchive_ReadsFilesAndListsDirectory()
    {
        using var tmp = new TempDir();
        using var archive = Open(tmp, Valid());

        Assert.Equal(Payload, archive.Read("def/plain.txt"));
        Assert.Equal(Payload, archive.Read("def/zipped.txt"));
        var (dirs, files) = archive.List("def");
        Assert.Empty(dirs);
        Assert.Equal(["plain.txt", "zipped.txt"], files);
        Assert.Null(archive.Read("def/missing.txt"));
    }

    // ---- OpenPooled ----

    [Theory]
    [InlineData("def/plain.txt")]
    [InlineData("def/zipped.txt")]
    public void OpenPooled_StoredAndZlib_ReturnsContent(string path)
    {
        using var tmp = new TempDir();
        using var archive = Open(tmp, Valid());

        using var stream = archive.OpenPooled(path);

        Assert.NotNull(stream);
        Assert.True(stream.TryGetBuffer(out var segment));
        Assert.Equal(Payload.Length, stream.Length);
        Assert.Equal(Payload, segment.AsSpan(0, Payload.Length).ToArray());
    }

    [Fact]
    public void OpenPooled_Missing_ReturnsNull()
    {
        using var tmp = new TempDir();
        using var archive = Open(tmp, Valid());

        Assert.Null(archive.OpenPooled("def/missing.txt"));
        Assert.Null(archive.OpenPooled("def"));   // diretório
    }

    [Theory]
    [InlineData("def/plain.txt")]
    [InlineData("def/zipped.txt")]
    public void OpenPooled_Dispose_ReturnsBufferToPoolExactlyOnceEvenIfDisposedTwice(string path)
    {
        using var tmp = new TempDir();
        using var archive = Open(tmp, Valid());
        var stream = archive.OpenPooled(path)!;
        Assert.True(stream.TryGetBuffer(out var segment));
        var buffer = segment.Array!;

        stream.Dispose();
        var rented = ArrayPool<byte>.Shared.Rent(buffer.Length);   // o pool compartilhado guarda o último devolvido na própria thread
        stream.Dispose();                                          // segundo Dispose não pode devolver de novo
        var second = ArrayPool<byte>.Shared.Rent(buffer.Length);

        Assert.Same(buffer, rented);
        Assert.NotSame(buffer, second);
        ArrayPool<byte>.Shared.Return(rented);
        ArrayPool<byte>.Shared.Return(second);
    }

    [Fact]
    public void OpenPooled_UnsupportedCompression_ThrowsNotSupported()
    {
        using var tmp = new TempDir();
        var bytes = new ScsArchiveBuilder().RawFile("def/tex.bin", [1, 2, 3, 4], compression: 0x20, declaredSize: 4).Build();
        using var archive = Open(tmp, bytes);

        Assert.Throws<NotSupportedException>(() => archive.OpenPooled("def/tex.bin"));
        Assert.Throws<NotSupportedException>(() => archive.Read("def/tex.bin"));
    }

    [Fact]
    public void OpenPooled_DeclaredSizeLargerThanContent_ThrowsInvalidData()
    {
        using var tmp = new TempDir();
        var bytes = new ScsArchiveBuilder().ZFile("def/a.txt", Payload, declaredSize: Payload.Length + 500).Build();
        using var archive = Open(tmp, bytes);

        Assert.Throws<InvalidDataException>(() => archive.OpenPooled("def/a.txt"));
        Assert.Throws<InvalidDataException>(() => archive.Read("def/a.txt"));
    }

    [Fact]
    public void OpenPooled_CorruptZlibData_ThrowsInvalidData()
    {
        using var tmp = new TempDir();
        var bytes = new ScsArchiveBuilder().RawFile("def/a.txt", Encoding.ASCII.GetBytes("not zlib at all!"), 0x10, 64).Build();
        using var archive = Open(tmp, bytes);

        Assert.Throws<InvalidDataException>(() => archive.OpenPooled("def/a.txt"));
    }

    [Fact]
    public void OpenPooled_DataOffsetBeyondEndOfFile_ThrowsInvalidData()
    {
        using var tmp = new TempDir();
        // arquivo único: descritor começa em meta[4]; "block" (u32) fica em +12
        var bytes = new ScsArchiveBuilder().File("def/a.txt", Payload)
            .Build((_, meta) => BinaryPrimitives.WriteUInt32LittleEndian(meta.AsSpan(4 + 12), 0x00FFFFFF));
        using var archive = Open(tmp, bytes);

        Assert.Throws<InvalidDataException>(() => archive.OpenPooled("def/a.txt"));
        Assert.Throws<InvalidDataException>(() => archive.Read("def/a.txt"));
    }

    // ---- Open com cabeçalho corrompido ----

    [Fact]
    public void Open_EntryCountThatOverflowsWhenMultipliedBy16_ThrowsInvalidData()
    {
        using var tmp = new TempDir();
        var bytes = ScsArchiveBuilder.PatchInt32(Valid(), ScsArchiveBuilder.EntryCountAt, 0x7FFFFFFF);
        File.WriteAllBytes(tmp.Combine("a.scs"), bytes);

        Assert.Throws<InvalidDataException>(() => HashFsArchive.Open(tmp.Combine("a.scs")));
    }

    [Theory]
    [InlineData(ScsArchiveBuilder.EntryCountAt, -1)]
    [InlineData(ScsArchiveBuilder.MetaWordCountAt, -1)]
    [InlineData(ScsArchiveBuilder.MetaWordCountAt, 0x7FFFFFFF)]
    [InlineData(ScsArchiveBuilder.EntryTableSizeAt, -5)]
    [InlineData(ScsArchiveBuilder.EntryTableSizeAt, 0x7FFFFFF0)]
    [InlineData(ScsArchiveBuilder.MetaTableSizeAt, 0x7FFFFFF0)]
    public void Open_ImpossibleHeaderCounts_ThrowsInvalidData(int field, int value)
    {
        using var tmp = new TempDir();
        File.WriteAllBytes(tmp.Combine("a.scs"), ScsArchiveBuilder.PatchInt32(Valid(), field, value));

        Assert.Throws<InvalidDataException>(() => HashFsArchive.Open(tmp.Combine("a.scs")));
    }

    [Theory]
    [InlineData(ScsArchiveBuilder.EntryTableOffsetAt, long.MaxValue)]
    [InlineData(ScsArchiveBuilder.EntryTableOffsetAt, -1L)]
    [InlineData(ScsArchiveBuilder.MetaTableOffsetAt, long.MaxValue - 2)]
    [InlineData(ScsArchiveBuilder.MetaTableOffsetAt, 1_000_000L)]
    public void Open_TableOffsetOutsideFile_ThrowsInvalidData(int field, long value)
    {
        using var tmp = new TempDir();
        File.WriteAllBytes(tmp.Combine("a.scs"), ScsArchiveBuilder.PatchInt64(Valid(), field, value));

        Assert.Throws<InvalidDataException>(() => HashFsArchive.Open(tmp.Combine("a.scs")));
    }

    [Fact]
    public void Open_EntryCountLargerThanTableHolds_ThrowsInvalidData()
    {
        using var tmp = new TempDir();
        // a tabela inflada tem 3 entradas; o cabeçalho diz 500
        File.WriteAllBytes(tmp.Combine("a.scs"), ScsArchiveBuilder.PatchInt32(Valid(), ScsArchiveBuilder.EntryCountAt, 500));

        Assert.Throws<InvalidDataException>(() => HashFsArchive.Open(tmp.Combine("a.scs")));
    }

    [Fact]
    public void Open_FileTruncatedInsideHeader_ThrowsInvalidData()
    {
        using var tmp = new TempDir();
        File.WriteAllBytes(tmp.Combine("a.scs"), Valid()[..20]);

        Assert.Throws<InvalidDataException>(() => HashFsArchive.Open(tmp.Combine("a.scs")));
    }

    [Fact]
    public void Open_FileTruncatedInsideTables_ThrowsInvalidData()
    {
        using var tmp = new TempDir();
        var bytes = Valid();
        File.WriteAllBytes(tmp.Combine("a.scs"), bytes[..(bytes.Length - 10)]);

        Assert.Throws<InvalidDataException>(() => HashFsArchive.Open(tmp.Combine("a.scs")));
    }

    [Fact]
    public void Open_MetaIndexPointingOutsideMetaTable_ThrowsInvalidData()
    {
        using var tmp = new TempDir();
        var bytes = new ScsArchiveBuilder().File("def/a.txt", Payload)
            .Build((entries, _) => BinaryPrimitives.WriteInt32LittleEndian(entries.AsSpan(8), 1_000_000));
        File.WriteAllBytes(tmp.Combine("a.scs"), bytes);

        Assert.Throws<InvalidDataException>(() => HashFsArchive.Open(tmp.Combine("a.scs")));
    }

    [Fact]
    public void Open_DescriptorOffsetOutsideMetaTable_ThrowsInvalidData()
    {
        using var tmp = new TempDir();
        var bytes = new ScsArchiveBuilder().File("def/a.txt", Payload)
            .Build((_, meta) => BinaryPrimitives.WriteUInt16LittleEndian(meta.AsSpan(0), 0xFFFF));
        File.WriteAllBytes(tmp.Combine("a.scs"), bytes);

        Assert.Throws<InvalidDataException>(() => HashFsArchive.Open(tmp.Combine("a.scs")));
    }

    // ---- List com diretório corrompido ----

    [Fact]
    public void List_DirectoryEntryCountLargerThanContent_ThrowsInvalidData()
    {
        using var tmp = new TempDir();
        using var archive = Open(tmp, new ScsArchiveBuilder().DirectoryRaw("map", BitConverter.GetBytes(1_000_000)).Build());

        Assert.Throws<InvalidDataException>(() => archive.List("map"));
    }

    [Fact]
    public void List_NegativeCount_ThrowsInvalidData()
    {
        using var tmp = new TempDir();
        using var archive = Open(tmp, new ScsArchiveBuilder().DirectoryRaw("map", BitConverter.GetBytes(-3)).Build());

        Assert.Throws<InvalidDataException>(() => archive.List("map"));
    }

    [Fact]
    public void List_NameLengthsBeyondContent_ThrowsInvalidData()
    {
        using var tmp = new TempDir();
        byte[] content = [.. BitConverter.GetBytes(2), 200, 200, (byte)'a', (byte)'b'];
        using var archive = Open(tmp, new ScsArchiveBuilder().DirectoryRaw("map", content).Build());

        Assert.Throws<InvalidDataException>(() => archive.List("map"));
    }

    [Fact]
    public void List_ContentShorterThanCountField_ThrowsInvalidData()
    {
        using var tmp = new TempDir();
        using var archive = Open(tmp, new ScsArchiveBuilder().DirectoryRaw("map", [1, 0]).Build());

        Assert.Throws<InvalidDataException>(() => archive.List("map"));
    }

    [Fact]
    public void Walk_NestedDirectories_ReturnsFullPaths()
    {
        using var tmp = new TempDir();
        var bytes = new ScsArchiveBuilder()
            .Directory("map", ["europe"], ["top.txt"])
            .Directory("map/europe", [], ["sec.base"])
            .File("map/top.txt", Payload)
            .File("map/europe/sec.base", Payload)
            .Build();
        using var archive = Open(tmp, bytes);

        Assert.Equal(["map/top.txt", "map/europe/sec.base"], archive.Walk("map").ToArray());
    }
}
