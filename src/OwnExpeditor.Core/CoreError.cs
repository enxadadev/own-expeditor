namespace OwnExpeditor.Core;

/// <summary>Códigos de erro de negócio do Core. A UI mapeia para <c>Error_&lt;Code&gt;</c>.</summary>
public enum CoreError
{
    SaveNotFound, SaveParseFailed, SaveCorrupt, NoSession, SessionBusy, ReadOnlySession,
    Conflict, StalePatch, UnknownUnit, UnknownAttribute, InvalidEdit, NothingToApply, GameRunning,
    SaveChangedOnDisk, BackupExists, BackupFailed, WriteFailed, VerifyFailed, AccessDenied, SlotUnavailable,
}
