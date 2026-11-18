using OwnExpeditor.Core.Games;
using OwnExpeditor.Domain.Games;
using OwnExpeditor.Domain.Saves;

namespace OwnExpeditor.Core.Session;

public interface IEditSession
{
    SaveSlotInfo Save { get; }
    GameKind Game { get; }
    bool IsReadOnly { get; }
    ISaveView Original { get; }
    ISaveView View { get; }
    IGameData GameData { get; }
    IGameNames Names => GameData;
    IReadOnlyList<ChangeSet> Pending { get; }
    bool HasChanges { get; }
    event EventHandler? Changed;
    void Stage(ChangeSet change);
    bool Discard(Guid changeSetId);
    void DiscardModule(string moduleId);
    void DiscardAll();
    SessionDiff GetDiff();
    Task<ApplyResult> ApplyAsync(ApplyTarget target, CancellationToken ct);
}
