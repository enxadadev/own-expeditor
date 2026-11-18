using OwnExpeditor.Core.Session;
using OwnExpeditor.Data.Games;
using OwnExpeditor.Domain.Games;

namespace OwnExpeditor.Core.Games;

/// <summary>
/// Diz se o jogo da sessão aberta está rodando: confere ao abrir a sessão e a cada 2 s; sem sessão não consulta nada.
/// <c>RunningChanged</c> dispara na thread do timer (a UI deve despachar). O Apply confere de novo por conta própria.
/// </summary>
internal sealed class GameStatusMonitor : IGameStatusMonitor, IDisposable
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(2);

    private readonly IEditSessionHost _host;
    private readonly IGameProcessMonitor _process;
    private readonly TimeProvider _time;
    private readonly object _gate = new();
    private ITimer? _timer;
    private GameKind? _game;
    private bool _running;
    private bool _disposed;

    public GameStatusMonitor(IEditSessionHost host, IGameProcessMonitor process, TimeProvider time)
    {
        _host = host;
        _process = process;
        _time = time;
        host.SessionChanged += OnSessionChanged;
        if (host.Current is { } current) Start(current.Game);
    }

    public bool IsRunning { get { lock (_gate) return _running; } }

    public event EventHandler<bool>? RunningChanged;

    public void Dispose()
    {
        _host.SessionChanged -= OnSessionChanged;
        lock (_gate)
        {
            _disposed = true;
            _timer?.Dispose();
            _timer = null;
            _game = null;
        }
    }

    private void OnSessionChanged(object? sender, SessionChangedEventArgs e)
    {
        if (e.Current is null) Stop();
        else Start(e.Current.Game);
    }

    private void Start(GameKind game)
    {
        lock (_gate)
        {
            if (_disposed) return;
            _timer?.Dispose();
            _timer = null;
            _game = game;
        }
        Poll(game);
        lock (_gate)
        {
            if (_disposed || _game != game) return;
            _timer = _time.CreateTimer(_ => Poll(game), null, Interval, Interval);
        }
    }

    private void Stop()
    {
        bool changed;
        lock (_gate)
        {
            _timer?.Dispose();
            _timer = null;
            _game = null;
            changed = _running;
            _running = false;
        }
        if (changed) RunningChanged?.Invoke(this, false);
    }

    private void Poll(GameKind game)
    {
        bool running;
        try
        {
            running = _process.IsRunning(game);
        }
        catch (Exception)
        {
            running = false; // a lista de processos pode falhar (permissão); sem resposta, não bloqueia o usuário
        }

        lock (_gate)
        {
            if (_disposed || _game != game || _running == running) return;
            _running = running;
        }
        RunningChanged?.Invoke(this, running);
    }
}
