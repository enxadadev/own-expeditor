using OwnExpeditor.Data.Maps;
using OwnExpeditor.Domain.Games;
using OwnExpeditor.Domain.Saves;

namespace OwnExpeditor.Core.Maps;

/// <summary>Adaptador de <see cref="IMapService"/> (Data) para a porta <see cref="IGameMapProvider"/>.</summary>
public sealed class GameMapProvider(IMapService maps) : IGameMapProvider
{
    private readonly IMapService _maps = maps;

    public async Task<GameMapResult> LoadCitiesAsync(GameKind game, ProfileInfo profile, IProgress<double>? progress, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(profile);
        var result = await _maps.GetAsync(game, profile, MapDetail.Cities, ct).ConfigureAwait(false);
        return new GameMapResult(result.Status, result.Index);
    }
}
