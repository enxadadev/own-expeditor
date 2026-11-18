using OwnExpeditor.Data.Saves;
using OwnExpeditor.Sii;

namespace OwnExpeditor.Core.Internal;

/// <summary>Traduz erros das camadas de baixo (Sii, Data, E/S) para <see cref="CoreException"/>.</summary>
internal static class CoreErrors
{
    /// <summary>Arquivo danificado → <c>SaveCorrupt</c>; formato não entendido → <c>SaveParseFailed</c>.</summary>
    public static CoreException FromSii(SiiFormatException e, string? stageKey = null) =>
        new(e.IsUnreadable ? CoreError.SaveCorrupt : CoreError.SaveParseFailed, e.Message, e)
        {
            Line = e.Line,
            StageKey = stageKey,
        };

    public static CoreException FromSaveWrite(SaveWriteException e) => new(e.Failure switch
    {
        SaveWriteFailure.NotFound => CoreError.SaveNotFound,
        SaveWriteFailure.ChangedOnDisk => CoreError.SaveChangedOnDisk,
        SaveWriteFailure.BackupExists => CoreError.BackupExists,
        SaveWriteFailure.BackupFailed => CoreError.BackupFailed,
        SaveWriteFailure.WriteFailed => CoreError.WriteFailed,
        SaveWriteFailure.VerifyFailed => CoreError.VerifyFailed,
        SaveWriteFailure.AccessDenied => CoreError.AccessDenied,
        SaveWriteFailure.SlotUnavailable => CoreError.SlotUnavailable,
        _ => CoreError.WriteFailed,
    }, e.Message, e);

    /// <summary>Leitura: arquivo/pasta ausente → <c>SaveNotFound</c>; acesso negado ou travado → <c>AccessDenied</c>.</summary>
    public static CoreException FromRead(Exception e, string? stageKey = null) => new(
        e is FileNotFoundException or DirectoryNotFoundException ? CoreError.SaveNotFound : CoreError.AccessDenied,
        e.Message, e)
    { StageKey = stageKey };

    public static bool IsReadFailure(Exception e) => e is IOException or UnauthorizedAccessException;
}
