using OwnExpeditor.Domain.Games;

namespace OwnExpeditor.Core.Games;

/// <summary>Dados do jogo indisponíveis (pasta não achada ou ilegível): nomes legíveis a partir do id, sem regras.</summary>
internal sealed class UnavailableGameData(GameKind game) : IGameData
{
    public GameKind Game { get; } = game;
    public bool IsAvailable => false;

    public string Get(GameNameKind kind, string id)
    {
        ArgumentNullException.ThrowIfNull(id);
        return GameIds.Humanize(id);
    }

    public bool Ships(string company, string cargo) => false;
    public bool Accepts(string company, string cargo) => false;
    public IReadOnlyCollection<string> CargoOut(string company) => [];
}
