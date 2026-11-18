using OwnExpeditor.Core.Text;

namespace OwnExpeditor.Core.Session;

public sealed record DiffLine(TextRef Label, DiffValue Before, DiffValue After);

/// <summary>Igualdade por valor: inclui <c>Id</c>, e compara <c>Lines</c> e <c>Patches</c> em sequência.</summary>
public sealed record ChangeSet(string ModuleId, string Key, TextRef Title,
                               IReadOnlyList<DiffLine> Lines, IReadOnlyList<SavePatch> Patches)
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public bool Equals(ChangeSet? other) =>
        other is not null && Id == other.Id && ModuleId == other.ModuleId && Key == other.Key
        && Title.Equals(other.Title) && Lines.SequenceEqual(other.Lines) && Patches.SequenceEqual(other.Patches);

    public override int GetHashCode()
    {
        var h = new HashCode();
        h.Add(Id);
        h.Add(Key);
        foreach (var l in Lines) h.Add(l);
        foreach (var p in Patches) h.Add(p);
        return h.ToHashCode();
    }
}

public sealed record ModuleDiff(string ModuleId, IReadOnlyList<ChangeSet> Changes)
{
    public bool Equals(ModuleDiff? other) =>
        other is not null && ModuleId == other.ModuleId && Changes.SequenceEqual(other.Changes);

    public override int GetHashCode() => HashCode.Combine(ModuleId, Changes.Count);
}

public sealed record SessionDiff(IReadOnlyList<ModuleDiff> Modules, int PatchCount)
{
    public bool Equals(SessionDiff? other) =>
        other is not null && PatchCount == other.PatchCount && Modules.SequenceEqual(other.Modules);

    public override int GetHashCode() => HashCode.Combine(PatchCount, Modules.Count);
}
