using OwnExpeditor.Domain.Saves;

namespace OwnExpeditor.Core.Session;

public sealed class SessionChangedEventArgs(IEditSession? previous, IEditSession? current) : EventArgs
{
    public IEditSession? Previous { get; } = previous;
    public IEditSession? Current { get; } = current;
}

public interface IEditSessionHost
{
    IEditSession? Current { get; }
    event EventHandler<SessionChangedEventArgs>? SessionChanged;
    Task<IEditSession> OpenAsync(SaveSlotInfo save, IProgress<LoadProgress>? progress, CancellationToken ct);
    void Close();
}
