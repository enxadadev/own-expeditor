using OwnExpeditor.Domain.Saves;

namespace OwnExpeditor.Core.Session;

public abstract record ApplyTarget
{
    public sealed record Overwrite : ApplyTarget;
    public sealed record NewSlot(string DisplayName) : ApplyTarget;
}

public sealed record ApplyResult(SaveSlotInfo WrittenSlot, string? BackupPath, int ChangeSetCount, int PatchCount);
