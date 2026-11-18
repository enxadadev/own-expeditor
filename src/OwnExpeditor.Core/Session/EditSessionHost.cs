using OwnExpeditor.Core.Games;
using OwnExpeditor.Core.Internal;
using OwnExpeditor.Data.Games;
using OwnExpeditor.Data.Saves;
using OwnExpeditor.Domain.Games;
using OwnExpeditor.Domain.Saves;
using OwnExpeditor.Sii;

namespace OwnExpeditor.Core.Session;

/// <summary>
/// Dono da sessão atual. Abrir = Read (0–30%) → Parse (30–85%) → Index (85–100%); só uma abertura por vez.
/// Uma abertura que falha ou é cancelada não mexe na sessão atual.
/// </summary>
internal sealed class EditSessionHost : IEditSessionHost
{
    private const int ReadEnd = 30;
    private const int ParseEnd = 85;
    private static readonly TimeSpan ReportInterval = TimeSpan.FromMilliseconds(100);

    private readonly ISaveFileStore _store;
    private readonly IGameDataService _gameData;
    private readonly IGameProcessMonitor _process;
    private readonly TimeProvider _time;
    private readonly object _gate = new();
    private EditSession? _current;
    private int _opening;

    public EditSessionHost(ISaveFileStore store, IGameDataService gameData, IGameProcessMonitor process,
        TimeProvider? time = null)
    {
        _store = store;
        _gameData = gameData;
        _process = process;
        _time = time ?? TimeProvider.System;
        _gameData.Changed += OnGameDataChanged;
    }

    public IEditSession? Current { get { lock (_gate) return _current; } }

    public event EventHandler<SessionChangedEventArgs>? SessionChanged;

    // Sem ConfigureAwait(false) de propósito: SessionChanged volta ao contexto de quem chamou (UI).
    public async Task<IEditSession> OpenAsync(SaveSlotInfo save, IProgress<LoadProgress>? progress, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(save);
        ct.ThrowIfCancellationRequested();
        if (Interlocked.CompareExchange(ref _opening, 1, 0) != 0)
            throw new CoreException(CoreError.SessionBusy, "Já há um save sendo aberto.");
        try
        {
            var session = await LoadAsync(save, new ProgressGate(progress, _time), ct);
            EditSession? previous;
            lock (_gate)
            {
                previous = _current;
                _current = session;
            }
            previous?.Close();
            SessionChanged?.Invoke(this, new SessionChangedEventArgs(previous, session));
            return session;
        }
        finally
        {
            Volatile.Write(ref _opening, 0);
        }
    }

    public void Close()
    {
        EditSession? previous;
        lock (_gate)
        {
            previous = _current;
            _current = null;
        }
        if (previous is null) return;
        previous.Close();
        SessionChanged?.Invoke(this, new SessionChangedEventArgs(previous, null));
    }

    private async Task<EditSession> LoadAsync(SaveSlotInfo save, ProgressGate gate, CancellationToken ct)
    {
        gate.Report(new LoadProgress(0, LoadStages.Read, save.SizeBytes > 0 ? save.SizeBytes : null), force: true);
        SaveFile file;
        try
        {
            file = await _store.ReadAsync(save.GameSiiPath, ct);
        }
        catch (Exception e) when (CoreErrors.IsReadFailure(e))
        {
            throw CoreErrors.FromRead(e, LoadStages.Read);
        }
        gate.Report(new LoadProgress(ReadEnd, LoadStages.Read, file.Bytes.LongLength), force: true);

        ct.ThrowIfCancellationRequested();
        gate.Report(new LoadProgress(ReadEnd, LoadStages.Parse), force: true);
        SiiDocument document;
        try
        {
            document = await Task.Run(() =>
                SiiDocument.Parse(SiiDecoder.DecodeToText(file.Bytes), new ParseProgress(gate, ct)), ct);
        }
        catch (SiiFormatException e)
        {
            throw CoreErrors.FromSii(e, LoadStages.Parse);
        }

        ct.ThrowIfCancellationRequested();
        gate.Report(new LoadProgress(ParseEnd, LoadStages.Index), force: true);
        var data = await _gameData.GetAsync(save.Profile.Game, ct);
        var session = new EditSession(save, document, data, new WriteContext(_store, _process, file.Fingerprint));
        await Task.Run(session.EnsureIndexed, ct);
        gate.Report(new LoadProgress(100, LoadStages.Index), force: true);
        return session;
    }

    /// <summary>Dados do jogo mudaram (pasta/idioma): a sessão aberta desse jogo recebe os novos, sem perder pendências.</summary>
    private void OnGameDataChanged(object? sender, GameKind game)
    {
        if (Current is EditSession session && session.Game == game) _ = RefreshAsync(session, game);
    }

    private async Task RefreshAsync(EditSession session, GameKind game)
    {
        try
        {
            session.UpdateGameData(await _gameData.GetAsync(game, CancellationToken.None));
        }
        catch (Exception)
        {
            // Rodando em segundo plano, sem quem receba o erro: o catálogo nunca derruba a sessão (risco 5);
            // em qualquer falha ficam os dados anteriores.
        }
    }

    /// <summary>No máximo um relatório por 100 ms (sem contar os de borda de etapa) e só quando o percentual muda.</summary>
    private sealed class ProgressGate(IProgress<LoadProgress>? sink, TimeProvider time)
    {
        private long _lastTimestamp;
        private int _lastPercent = -1;
        private bool _any;

        public void Report(LoadProgress value, bool force)
        {
            if (sink is null) return;
            var now = time.GetTimestamp();
            if (!force)
            {
                if (value.Percent == _lastPercent) return;
                if (_any && time.GetElapsedTime(_lastTimestamp, now) < ReportInterval) return;
            }
            _any = true;
            _lastTimestamp = now;
            _lastPercent = value.Percent;
            sink.Report(value);
        }
    }

    private sealed class ParseProgress(ProgressGate gate, CancellationToken ct) : IProgress<double>
    {
        public void Report(double fraction)
        {
            ct.ThrowIfCancellationRequested();
            var percent = ReadEnd + (int)Math.Round(fraction * (ParseEnd - ReadEnd));
            gate.Report(new LoadProgress(Math.Clamp(percent, ReadEnd, ParseEnd), LoadStages.Parse), force: false);
        }
    }
}
