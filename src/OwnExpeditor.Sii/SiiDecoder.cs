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
        ArgumentNullException.ThrowIfNull(data);
        if (data.Length == 0) throw new SiiFormatException(SiiErrorKind.Empty, "Arquivo .sii vazio.", offset: 0);
        switch (DetectFormat(data))
        {
            case SiiFormat.Text:
                return Encoding.UTF8.GetString(data);
            case SiiFormat.Encrypted:
                return DecodeToText(Decrypt(data));
            case SiiFormat.Binary:
                return BsiiDecoder.ToText(data);
            case SiiFormat.ThreeNK:
                return DecodeToText(Decode3nK(data));
            default:
                throw new SiiFormatException(SiiErrorKind.UnknownFormat, "Arquivo não reconhecido como .sii.", offset: 0);
        }
    }

    /// <summary>
    /// Como DecodeToText, mas aceita qualquer texto (ex.: .sui sem "SiiNunit").
    /// Usado para arquivos de definição do jogo.
    /// </summary>
    public static string DecodeLenient(byte[] data) => DetectFormat(data) switch
    {
        SiiFormat.Encrypted => DecodeLenient(Decrypt(data)),
        SiiFormat.ThreeNK => DecodeLenient(Decode3nK(data)),
        SiiFormat.Binary => BsiiDecoder.ToText(data),
        _ => Encoding.UTF8.GetString(data).TrimStart('﻿'),
    };

    /// <summary>3nK: XOR com tabela fixa a partir de uma semente (cabeçalho de 6 bytes).</summary>
    public static byte[] Decode3nK(byte[] data)
    {
        if (DetectFormat(data) != SiiFormat.ThreeNK)
            throw new SiiFormatException(SiiErrorKind.UnknownFormat, "Cabeçalho 3nK inválido.", offset: 0);
        if (data.Length < 6)
            throw new SiiFormatException(SiiErrorKind.Truncated, "Cabeçalho 3nK incompleto.", offset: data.Length);
        var seed = data[5];
        var output = new byte[data.Length - 6];
        for (var j = 0; j < output.Length; j++)
        {
            var b = (byte)((seed + j) & 0xFF);
            var key = (byte)((((byte)(b << 2) ^ (byte)~b) << 3) ^ b);
            output[j] = (byte)(data[6 + j] ^ key);
        }
        return output;
    }

    /// <summary>Remove a camada ScsC e devolve o conteúdo interno (texto ou BSII).</summary>
    public static byte[] Decrypt(byte[] data)
    {
        if (DetectFormat(data) != SiiFormat.Encrypted)
            throw new SiiFormatException(SiiErrorKind.UnknownFormat, "Cabeçalho ScsC inválido.", offset: 0);
        if (data.Length < EncryptedHeaderSize)
            throw new SiiFormatException(SiiErrorKind.Truncated,
                $"Cabeçalho ScsC incompleto ({data.Length} de {EncryptedHeaderSize} bytes).", offset: data.Length);

        var iv = data.AsSpan(36, 16).ToArray();
        var expectedSize = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(52, 4));

        byte[] compressed;
        try
        {
            using var aes = Aes.Create();
            aes.Key = Key;
            compressed = aes.DecryptCbc(data.AsSpan(EncryptedHeaderSize), iv, PaddingMode.None); // o zlib ignora o padding no fim
        }
        catch (CryptographicException e)
        {
            throw new SiiFormatException(SiiErrorKind.DecryptFailed,
                "Falha ao decifrar o conteúdo ScsC.", offset: EncryptedHeaderSize, inner: e);
        }

        if (expectedSize > int.MaxValue)
            throw new SiiFormatException(SiiErrorKind.SizeMismatch,
                $"Tamanho declarado no cabeçalho ScsC ({expectedSize}) é grande demais.", offset: 52);

        using var output = new MemoryStream((int)Math.Min(expectedSize, 1 << 28));
        try
        {
            using var input = new MemoryStream(compressed);
            using var zlib = new ZLibStream(input, CompressionMode.Decompress);
            zlib.CopyTo(output);
        }
        catch (InvalidDataException e)
        {
            throw new SiiFormatException(SiiErrorKind.DecompressFailed,
                "Falha ao descomprimir o conteúdo ScsC.", offset: EncryptedHeaderSize, inner: e);
        }

        if (output.Length != expectedSize)
            throw new SiiFormatException(SiiErrorKind.SizeMismatch,
                $"Tamanho decodificado ({output.Length}) difere do cabeçalho ({expectedSize}).", offset: 52);

        return output.ToArray();
    }
}
