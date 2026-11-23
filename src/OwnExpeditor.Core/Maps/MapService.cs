using OwnExpeditor.Data.Games;
using OwnExpeditor.Data.Maps;
using OwnExpeditor.Domain.Games;
using OwnExpeditor.Domain.Maps;
using OwnExpeditor.Domain.Saves;

namespace OwnExpeditor.Core.Maps;

public sealed class MapService(IMapSource source, IModLocator mods, IGameInstallLocator installs) : IMapService
{
    private readonly IMapSource _source = source;
    private readonly IModLocator _mods = mods;
    private readonly IGameInstallLocator _installs = installs;

    public event EventHandler<GameKind>? Changed;

    public async Task<MapLoadResult> GetAsync(GameKind game, ProfileInfo profile, MapDetail detail, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ct.ThrowIfCancellationRequested();

        var installDir = _installs.Detect(game).FirstOrDefault();
        if (installDir is null)
            return new MapLoadResult(MapStatus.NoGameFolder, null, []);

        var request = new MapRequest(game, installDir, _mods.Active(profile), detail);
        return await _source.LoadAsync(request, null, ct).ConfigureAwait(false);
    }

    private void OnChanged(GameKind game) => Changed?.Invoke(this, game);
}
