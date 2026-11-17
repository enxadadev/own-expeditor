using System.Buffers.Binary;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace OwnExpeditor.Sii;

public enum SiiFormat
{
    Unknown,
    Text,
    Encrypted,
    Binary,
    ThreeNK,
}

/// <summary>
/// Converte o conteúdo bruto de um arquivo .sii em texto SiiNunit.
/// Suporta texto puro e ScsC (AES-256-CBC + zlib). Ver docs/sii-format.md.
/// </summary>
public static class SiiDecoder
{
    private const int EncryptedHeaderSize = 56;

    // Chave pública do formato ScsC (mesma do SII_Decrypt).
    private static readonly byte[] Key =
    [
        0x2a, 0x5f, 0xcb, 0x17, 0x91, 0xd2, 0x2f, 0xb6, 0x02, 0x45, 0xb3, 0xd8, 0x36, 0x9e, 0xd0, 0xb2,
        0xc2, 0x73, 0x71, 0x56, 0x3f, 0xbf, 0x1f, 0x3c, 0x9e, 0xdf, 0x6b, 0x11, 0x82, 0x5a, 0x5d, 0x0a,
    ];

    public static SiiFormat DetectFormat(ReadOnlySpan<byte> data)
    {
        if (data.StartsWith("SiiNunit"u8)) return SiiFormat.Text;
        if (data.Length > 3 &&data[0] == 0xEF && data[1] == 0xBB && data[2] == 0xBF
            && data[3..].StartsWith("SiiNunit"u8)) return SiiFormat.Text;
        if (data.StartsWith("ScsC"u8)) return SiiFormat.Encrypted;
        if (data.StartsWith("BSII"u8)) return SiiFormat.Binary;
        if (data.StartsWith("3nK"u8)) return SiiFormat.ThreeNK;
        return SiiFormat.Unknown;
    }

    public static string DecodeToText(byte[] data)
    {
        switch (DetectFormat(data))
        {
            case SiiFormat.Text:
                return Encoding.UTF8.GetString(data);
            case SiiFormat.Encrypted:
                return DecodeToText(Decrypt(data));
            case SiiFormat.Binary:
                return BsiiDecoder.ToText(data);
            case SiiFormat.ThreeNK:
                throw new NotSupportedException("Formato 3nK ainda não é suportado.");
            default:
                throw new InvalidDataException("Arquivo não reconhecido como .sii.");
        }
    }

    /// <summary>Remove a camada ScsC e devolve o conteúdo interno (texto ou BSII).</summary>
    public static byte[] Decrypt(byte[] data)
    {
        if (data.Length < EncryptedHeaderSize || DetectFormat(data) != SiiFormat.Encrypted)
            throw new InvalidDataException("Cabeçalho ScsC inválido.");

        var iv = data.AsSpan(36, 16).ToArray();
        var expectedSize = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(52, 4));

        using var aes = Aes.Create();
        aes.Key = Key;
        var compressed = aes.DecryptCbc(data.AsSpan(EncryptedHeaderSize), iv, PaddingMode.None); // o zlib ignora o padding no fim

        using var input = new MemoryStream(compressed);
        using var zlib = new ZLibStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream((int)expectedSize);
        zlib.CopyTo(output);

        if (output.Length != expectedSize)
            throw new InvalidDataException(
                $"Tamanho decodificado ({output.Length}) difere do cabeçalho ({expectedSize}).");

        return output.ToArray();
    }
}
