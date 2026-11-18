using OwnExpeditor.Domain.Games;

namespace OwnExpeditor.Core.Games;

public interface IGameNames
{
    /// <summary>Nome legível do id; nunca null (cai em <see cref="GameIds.Humanize"/>).</summary>
    string Get(GameNameKind kind, string id);
}
