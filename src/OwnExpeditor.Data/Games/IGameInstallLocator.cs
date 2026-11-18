using OwnExpeditor.Domain.Games;

namespace OwnExpeditor.Data.Games;

public interface IGameInstallLocator
{
    /// <summary>Pastas de instalação válidas encontradas (Steam, Flatpak, padrões), sem repetição.</summary>
    IReadOnlyList<string> Detect(GameKind game);

    /// <summary>Confere <c>base.scs</c>, <c>def.scs</c> e o executável do jogo certo em <c>bin/*</c>.</summary>
    GameFolderCheck Validate(GameKind game, string path);
}
