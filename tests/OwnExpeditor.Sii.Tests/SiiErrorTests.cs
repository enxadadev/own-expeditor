using System.Buffers.Binary;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using OwnExpeditor.Sii;

namespace OwnExpeditor.Sii.Tests;

/// <summary>Um caso por SiiErrorKind (docs/sii-format.md, seção 7), com bytes/strings mínimos.</summary>
public class SiiErrorTests
{
    // Chave pública do formato ScsC (a mesma do decoder), só para montar entradas de teste.
    private static readonly byte[] Key =
    [
        0x2a, 0x5f, 0xcb, 0x17, 0x91, 0xd2, 0x2f, 0xb6, 0x02, 0x45, 0xb3, 0xd8, 0x36, 0x9e, 0xd0, 0xb2,
        0xc2, 0x73, 0x71, 0x56, 0x3f, 0xbf, 0x1f, 0x3c, 0x9e, 0xdf, 0x6b, 0x11, 0x82, 0x5a, 0x5d, 0x0a,
    ];

    private static byte[] ScsC(byte[] cipher, uint declaredSize)
    {
        var data = new byte[56 + cipher.Length];
        "ScsC"u8.CopyTo(data);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(52), declaredSize);
        cipher.CopyTo(data.AsSpan(56));
        return data;
    }

    private static byte[] Encrypt(byte[] plain)
    {
        using var aes = Aes.Create();
        aes.Key = Key;
        return aes.EncryptCbc(plain, new byte[16], PaddingMode.Zeros);
    }

    private static byte[] Zlib(byte[] plain)
    {
        using var ms = new MemoryStream();
        using (var z = new ZLibStream(ms, CompressionLevel.Optimal, leaveOpen: true)) z.Write(plain);
        return ms.ToArray();
    }

    private static SiiFormatException Fail(Action act) => Assert.Throws<SiiFormatException>(act);

    private static byte[] Bsii(uint version, params byte[] rest)
    {
        var data = new byte[8 + rest.Length];
        "BSII"u8.CopyTo(data);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(4), version);
        rest.CopyTo(data.AsSpan(8));
        return data;
    }

    [Fact]
    public void Empty_bytes()
    {
        var e = Fail(() => SiiDecoder.DecodeToText([]));
        Assert.Equal(SiiErrorKind.Empty, e.Kind);
        Assert.True(e.IsUnreadable);
    }

    [Fact]
    public void Empty_text_document()
    {
        var e = Fail(() => SiiDocument.Parse("\r\n  \r\n"));
        Assert.Equal(SiiErrorKind.Empty, e.Kind);
        Assert.True(e.IsUnreadable);
    }

    [Fact]
    public void UnknownFormat()
    {
        var e = Fail(() => SiiDecoder.DecodeToText("hello"u8.ToArray()));
        Assert.Equal(SiiErrorKind.UnknownFormat, e.Kind);
        Assert.Equal(0, e.Offset);
        Assert.False(e.IsUnreadable);
    }

    [Fact]
    public void Truncated_scsc_header()
    {
        var e = Fail(() => SiiDecoder.DecodeToText("ScsC\0\0\0\0"u8.ToArray()));
        Assert.Equal(SiiErrorKind.Truncated, e.Kind);
        Assert.True(e.IsUnreadable);
    }

    [Fact]
    public void Truncated_bsii_has_offset()
    {
        var e = Fail(() => BsiiDecoder.ToText(Bsii(2, 0, 0, 0, 0))); // início de estrutura sem o resto
        Assert.Equal(SiiErrorKind.Truncated, e.Kind);
        Assert.NotNull(e.Offset);
        Assert.True(e.IsUnreadable);
    }

    [Fact]
    public void Truncated_text_block_not_closed_has_line()
    {
        var e = Fail(() => SiiDocument.Parse("SiiNunit\r\n{\r\nx : a {\r\n k: v\r\n"));
        Assert.Equal(SiiErrorKind.Truncated, e.Kind);
        Assert.Equal(3, e.Line);
    }

    [Fact]
    public void DecryptFailed_cipher_not_block_aligned()
    {
        var e = Fail(() => SiiDecoder.DecodeToText(ScsC(new byte[5], 10)));
        Assert.Equal(SiiErrorKind.DecryptFailed, e.Kind);
        Assert.True(e.IsUnreadable);
    }

    [Fact]
    public void DecompressFailed_garbage_after_decrypt()
    {
        var e = Fail(() => SiiDecoder.DecodeToText(ScsC(Encrypt([0xDE, 0xAD, 0xBE, 0xEF, 1, 2, 3, 4]), 8)));
        Assert.Equal(SiiErrorKind.DecompressFailed, e.Kind);
        Assert.True(e.IsUnreadable);
    }

    [Fact]
    public void SizeMismatch_header_differs()
    {
        var plain = "SiiNunit\r\n{\r\n}\r\n"u8.ToArray();
        var e = Fail(() => SiiDecoder.DecodeToText(ScsC(Encrypt(Zlib(plain)), (uint)plain.Length + 1)));
        Assert.Equal(SiiErrorKind.SizeMismatch, e.Kind);
        Assert.True(e.IsUnreadable);
    }

    [Fact]
    public void ScsC_valid_still_decodes()
    {
        var plain = "SiiNunit\r\n{\r\n}\r\n"u8.ToArray();
        Assert.Equal("SiiNunit\r\n{\r\n}\r\n", SiiDecoder.DecodeToText(ScsC(Encrypt(Zlib(plain)), (uint)plain.Length)));
    }

    [Fact]
    public void UnsupportedVersion()
    {
        var e = Fail(() => SiiDecoder.DecodeToText(Bsii(9)));
        Assert.Equal(SiiErrorKind.UnsupportedVersion, e.Kind);
        Assert.Equal(4, e.Offset);
        Assert.False(e.IsUnreadable);
    }

    [Fact]
    public void UnknownValueType()
    {
        // estrutura id 1 "a" com um campo de tipo 0x7F "f"; depois um bloco dessa estrutura.
        byte[] rest =
        [
            0, 0, 0, 0,  1,  1, 0, 0, 0,  1, 0, 0, 0, (byte)'a',
            0x7F, 0, 0, 0,  1, 0, 0, 0, (byte)'f',
            0, 0, 0, 0,
            1, 0, 0, 0,  0,
        ];
        var e = Fail(() => BsiiDecoder.ToText(Bsii(2, rest)));
        Assert.Equal(SiiErrorKind.UnknownValueType, e.Kind);
        Assert.NotNull(e.Offset);
        Assert.False(e.IsUnreadable);
    }

    [Fact]
    public void Syntax_has_line()
    {
        var e = Fail(() => SiiDocument.Parse("SiiNunit\r\n{\r\nx : a {\r\n sem separador\r\n}\r\n}\r\n"));
        Assert.Equal(SiiErrorKind.Syntax, e.Kind);
        Assert.Equal(4, e.Line);
        Assert.False(e.IsUnreadable);
    }

    [Fact]
    public void OfType_returns_indexed_list_in_file_order()
    {
        var doc = SiiDocument.Parse("SiiNunit\r\n{\r\nx : a {\r\n}\r\n\r\ny : b {\r\n}\r\n\r\nx : c {\r\n}\r\n\r\n}\r\n");
        Assert.Equal(["a", "c"], doc.OfType("x").Select(u => u.Id));
        Assert.Empty(doc.OfType("nada"));
    }

    [Fact]
    public void Parse_reports_progress_up_to_one()
    {
        var values = new List<double>();
        SiiDocument.Parse("SiiNunit\r\n{\r\nx : a {\r\n}\r\n\r\n}\r\n", new SyncProgress(values));
        Assert.Contains(1.0, values);
    }

    private sealed class SyncProgress(List<double> values) : IProgress<double>
    {
        public void Report(double value) => values.Add(value);
    }
}
