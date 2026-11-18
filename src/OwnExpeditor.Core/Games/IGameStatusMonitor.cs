namespace OwnExpeditor.Core.Games;

public interface IGameStatusMonitor
{
    bool IsRunning { get; }
    event EventHandler<bool>? RunningChanged;
}
