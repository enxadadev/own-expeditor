using OwnExpeditor.Domain.Games;

namespace OwnExpeditor.Data.Games;

public interface IGameProcessMonitor
{
    /// <summary>O processo do jogo (<c>eurotrucks2</c> / <c>amtrucks</c>) está rodando.</summary>
    bool IsRunning(GameKind game);
}
