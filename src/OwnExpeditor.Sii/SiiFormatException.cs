namespace OwnExpeditor.Sii;

/// <summary>Conteúdo .sii inválido. <see cref="IsUnreadable"/> separa arquivo danificado de formato não entendido.</summary>
public sealed class SiiFormatException(SiiErrorKind kind, string message, int? line = null, long? offset = null,
    Exception? inner = null) : FormatException(message, inner)
{
    public SiiErrorKind Kind { get; } = kind;

    /// <summary>Linha (1-based) no texto, quando se aplica.</summary>
    public int? Line { get; } = line;

    /// <summary>Posição em bytes no binário, quando se aplica.</summary>
    public long? Offset { get; } = offset;

    /// <summary>Verdadeiro quando o arquivo está danificado (vazio, truncado, decifragem/descompressão/tamanho).</summary>
    public bool IsUnreadable { get; } = kind is SiiErrorKind.Empty or SiiErrorKind.Truncated
        or SiiErrorKind.DecryptFailed or SiiErrorKind.DecompressFailed or SiiErrorKind.SizeMismatch;
}
