using OwnExpeditor.Data.Maps;
using OwnExpeditor.Domain.Games;
using OwnExpeditor.Domain.Saves;

namespace OwnExpeditor.Core.Maps;

public interface IMapService
{
    Task<MapLoadResult> GetAsync(GameKind game, ProfileInfo profile, MapDetail detail, CancellationToken ct);

    /// <summary>Pasta do jogo ou mods mudaram.</summary>
    event EventHandler<GameKind>? Changed;
}
