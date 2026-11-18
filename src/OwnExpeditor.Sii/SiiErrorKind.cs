namespace OwnExpeditor.Sii;

/// <summary>Classificação dos erros ao ler um .sii. Ver docs/sii-format.md, seção 7.</summary>
public enum SiiErrorKind
{
    Empty,
    UnknownFormat,
    Truncated,
    DecryptFailed,
    DecompressFailed,
    SizeMismatch,
    UnsupportedVersion,
    UnknownValueType,
    Syntax,
}
