using OwnExpeditor.Domain.Games;

namespace OwnExpeditor.Domain.Settings;

/// <summary>Pastas de instalação escolhidas manualmente (null = detectar).</summary>
public sealed record GameFolders(string? Ets2 = null, string? Ats = null)
{
    public string? For(GameKind game) => game switch
    {
        GameKind.Ets2 => Ets2,
        GameKind.Ats => Ats,
        _ => throw new ArgumentOutOfRangeException(nameof(game), game, null),
    };

    public GameFolders With(GameKind game, string? path) => game switch
    {
        GameKind.Ets2 => this with { Ets2 = path },
        GameKind.Ats => this with { Ats = path },
        _ => throw new ArgumentOutOfRangeException(nameof(game), game, null),
    };
}
