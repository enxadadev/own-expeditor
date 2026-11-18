using System.Collections.Concurrent;
using System.Text.Json;
using OwnExpeditor.Domain.Games;
using OwnExpeditor.Domain.Saves;
using OwnExpeditor.Sii;

namespace OwnExpeditor.Data.Distances;

/// <summary>
/// Cache em <c>distances-ets2.json</c>/<c>distances-ats.json</c> na pasta de ajustes; gravação via <c>.tmp</c> + mover.
/// Leitura (<see cref="Load"/>) devolve um instantâneo imutável sem travar; a gravação é adiada e feita fora do lock de dados.
/// </summary>
internal sealed class JsonDistanceCache(string directory, TimeSpan? debounce = null) : IDistanceCache, IDisposable
{
    private sealed class Stored
    {
        public int Version { get; set; } = 1;
        public Dictionary<string, int> Pairs { get; set; } = [];
        public Dictionary<string, string> Scanned { get; set; } = [];

        /// <summary>Estimativas próprias (ausente em arquivos antigos).</summary>
        public Dictionary<string, int> Estimates { get; set; } = [];
    }

    private sealed record Snapshot(IReadOnlyDictionary<CityPair, int> Pairs, IReadOnlyDictionary<CityPair, int> Estimates);

    private static readonly Snapshot EmptySnapshot = new(new Dictionary<CityPair, int>(), new Dictionary<CityPair, int>());

    private readonly TimeSpan _debounce = debounce ?? TimeSpan.FromMilliseconds(400);
    private readonly object _gate = new();      // só memória: nunca segura durante I/O de gravação
    private readonly object _ioGate = new();    // serializa a escrita em disco
    private readonly Dictionary<GameKind, Stored> _loaded = [];
    private readonly ConcurrentDictionary<GameKind, Snapshot> _snapshots = new();
    private readonly HashSet<GameKind> _dirty = [];
    private bool _timerRunning;

    public Task PreloadAsync(GameKind game, CancellationToken ct) => Task.Run(() => EnsureLoaded(game), ct);

    public IReadOnlyDictionary<CityPair, int> Load(GameKind game) => SnapshotOf(game).Pairs;

    public IReadOnlyDictionary<CityPair, int> LoadEstimates(GameKind game) => SnapshotOf(game).Estimates;

    public void Merge(GameKind game, IEnumerable<KeyValuePair<CityPair, int>> pairs)
    {
        ArgumentNullException.ThrowIfNull(pairs);
        var changed = false;
        lock (_gate)
        {
            var stored = Get(game);
            if (AddAll(stored, pairs) > 0)
            {
                Publish(game, stored);
                changed = true;
            }
        }
        if (changed) SchedulePersist(game);
    }

    public void RecordEstimate(GameKind game, CityPair pair, int km)
    {
        if (km <= 0) return;
        var key = KeyOf(pair);
        lock (_gate)
        {
            var stored = Get(game);
            if (stored.Pairs.ContainsKey(key) || stored.Estimates.TryGetValue(key, out var old) && old == km) return;
            stored.Estimates[key] = km;
            Publish(game, stored);
        }
        SchedulePersist(game);
    }

    public Task FlushAsync(CancellationToken ct = default) => Task.Run(FlushDirty, ct);

    public void Dispose() => FlushDirty();

    public async Task<int> ScanAsync(GameKind game, IEnumerable<SaveSlotInfo> saves, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(saves);
        var added = 0;
        var changed = false;
        try
        {
            foreach (var save in saves)
            {
                ct.ThrowIfCancellationRequested();
                var fingerprint = FingerprintOf(save.GameSiiPath);
                if (fingerprint is null) continue;
                lock (_gate)
                {
                    if (Get(game).Scanned.TryGetValue(save.GameSiiPath, out var seen) && seen == fingerprint) continue;
                }

                Dictionary<CityPair, int> pairs;
                try
                {
                    var bytes = await File.ReadAllBytesAsync(save.GameSiiPath, ct);
                    pairs = await Task.Run(() => DistancePairs.Extract(SiiDocument.Parse(SiiDecoder.DecodeToText(bytes))), ct);
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    continue; // ilegível ou formato inesperado agora: um save ruim não derruba a varredura; tenta de novo na próxima
                }

                lock (_gate)
                {
                    var stored = Get(game);
                    added += AddAll(stored, pairs);
                    stored.Scanned[save.GameSiiPath] = fingerprint;
                    Publish(game, stored);
                    _dirty.Add(game);
                    changed = true;
                }
            }
        }
        finally
        {
            if (changed) await FlushAsync(CancellationToken.None);
        }
        return added;
    }

    private static string KeyOf(CityPair pair) => $"{pair.A}|{pair.B}";

    private static string? FingerprintOf(string path)
    {
        try
        {
            var info = new FileInfo(path);
            return info.Exists ? $"{info.Length}:{info.LastWriteTimeUtc.Ticks}" : null;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>Junta pares reais; uma estimativa própria igual é ignorada, uma real diferente a substitui e apaga a marca.</summary>
    private static int AddAll(Stored stored, IEnumerable<KeyValuePair<CityPair, int>> pairs)
    {
        var added = 0;
        foreach (var (pair, km) in pairs)
        {
            if (km <= 0) continue;
            var key = KeyOf(pair);
            if (stored.Estimates.TryGetValue(key, out var estimate))
            {
                if (estimate == km) continue;
                stored.Estimates.Remove(key);
                stored.Pairs[key] = km;
                added++;
            }
            else if (stored.Pairs.TryAdd(key, km)) added++;
        }
        return added;
    }

    private string PathOf(GameKind game) =>
        Path.Combine(directory, game == GameKind.Ats ? "distances-ats.json" : "distances-ets2.json");

    private Snapshot SnapshotOf(GameKind game)
    {
        if (_snapshots.TryGetValue(game, out var hit)) return hit;
        EnsureLoaded(game);
        return _snapshots.GetValueOrDefault(game) ?? EmptySnapshot;
    }

    private void EnsureLoaded(GameKind game)
    {
        lock (_gate) Get(game);
    }

    private void Publish(GameKind game, Stored stored) =>
        _snapshots[game] = new Snapshot(Parse(stored.Pairs), Parse(stored.Estimates));

    private static Dictionary<CityPair, int> Parse(Dictionary<string, int> source)
    {
        var result = new Dictionary<CityPair, int>(source.Count);
        foreach (var (key, km) in source)
        {
            var sep = key.IndexOf('|', StringComparison.Ordinal);
            if (sep > 0 && km > 0) result[CityPair.Of(key[..sep], key[(sep + 1)..])] = km;
        }
        return result;
    }

    /// <summary>Chamar com <c>_gate</c> tomado.</summary>
    private Stored Get(GameKind game)
    {
        if (_loaded.TryGetValue(game, out var hit)) return hit;
        Stored stored;
        try
        {
            var path = PathOf(game);
            stored = File.Exists(path)
                ? JsonSerializer.Deserialize<Stored>(File.ReadAllText(path)) ?? new Stored()
                : new Stored();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException)
        {
            stored = new Stored(); // cache é descartável: ilegível = recomeça
        }
        stored.Pairs ??= [];
        stored.Scanned ??= [];
        stored.Estimates ??= [];
        _loaded[game] = stored;
        Publish(game, stored);
        return stored;
    }

    private void SchedulePersist(GameKind game)
    {
        lock (_gate)
        {
            _dirty.Add(game);
            if (_timerRunning) return;
            _timerRunning = true;
        }
        _ = Task.Run(async () =>
        {
            while (true)
            {
                await Task.Delay(_debounce);
                lock (_gate)
                {
                    if (_dirty.Count == 0)
                    {
                        _timerRunning = false;
                        return;
                    }
                }
                FlushDirty();
            }
        });
    }

    /// <summary>
    /// Serializa em memória sob o lock de dados (curto) e grava fora dele: leituras e varreduras não esperam o disco.
    /// O lock de I/O envolve os dois passos para que uma gravação antiga nunca sobrescreva uma mais nova.
    /// </summary>
    private void FlushDirty()
    {
        lock (_ioGate)
        {
            List<(GameKind Game, string Json)> work = [];
            lock (_gate)
            {
                foreach (var game in _dirty) work.Add((game, JsonSerializer.Serialize(Get(game))));
                _dirty.Clear();
            }
            foreach (var (game, json) in work) TryWrite(game, json);
        }
    }

    private void TryWrite(GameKind game, string json)
    {
        try
        {
            Directory.CreateDirectory(directory);
            var path = PathOf(game);
            var tmp = path + ".tmp";
            File.WriteAllText(tmp, json);
            File.Move(tmp, path, overwrite: true);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // sem disco: o cache continua em memória nesta execução
        }
    }
}
