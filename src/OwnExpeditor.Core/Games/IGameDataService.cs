using OwnExpeditor.Domain.Games;

namespace OwnExpeditor.Core.Games;

public interface IGameDataService
{
    /// <summary>Nunca lança por falta de pasta: devolve dados com <see cref="IGameData.IsAvailable"/> falso.</summary>
    Task<IGameData> GetAsync(GameKind game, CancellationToken ct);
    GameFolderStatus GetFolder(GameKind game);
    Task<GameFolderCheck> SetManualFolderAsync(GameKind game, string? path, CancellationToken ct);
    event EventHandler<GameKind>? Changed;
}
