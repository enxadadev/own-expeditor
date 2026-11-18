using OwnExpeditor.Domain.Games;

namespace OwnExpeditor.Core.Modules.Jobs;

/// <summary>
/// Distâncias entre cidades: conhecidas (pares de <c>shortest_distance_km</c>) ou estimadas pelo menor caminho no grafo
/// dos pares conhecidos × fator calibrado em tempo de execução (leave-one-out numa amostra dos próprios pares).
/// Nunca devolve nulo. Seguro entre threads. Método e medições: docs/sii-format.md §10.
/// </summary>
public sealed class DistanceTable
{
    /// <summary>Sem nenhum par conhecido.</summary>
    public const int DefaultKm = 500;

    private const int CalibrationSample = 300;
    private const double MinFactor = 0.3;
    private const double MaxFactor = 1.2;

    private readonly Dictionary<CityPair, int> _known = [];
    private readonly Dictionary<string, List<(string City, int Km)>> _adjacency = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Dictionary<string, int>> _paths = new(StringComparer.Ordinal);
    private readonly object _gate = new();
    private double? _factor;
    private int? _fallback;

    public DistanceTable(IEnumerable<KeyValuePair<CityPair, int>> known)
    {
        ArgumentNullException.ThrowIfNull(known);
        Known = _known.AsReadOnly();
        foreach (var (pair, km) in known)
        {
            if (km <= 0 || pair.A == pair.B) continue;
            _known[pair] = km;
            Link(pair.A, pair.B, km);
            Link(pair.B, pair.A, km);
        }
    }

    /// <summary>Pares conhecidos (somente leitura).</summary>
    public IReadOnlyDictionary<CityPair, int> Known { get; }

    public RouteDistance Get(string fromCity, string toCity)
    {
        ArgumentNullException.ThrowIfNull(fromCity);
        ArgumentNullException.ThrowIfNull(toCity);
        if (fromCity == toCity) return new RouteDistance(0, DistanceSource.Known);
        if (_known.TryGetValue(CityPair.Of(fromCity, toCity), out var km)) return new RouteDistance(km, DistanceSource.Known);
        lock (_gate) return new RouteDistance(Estimate(fromCity, toCity), DistanceSource.Estimated);
    }

    private void Link(string a, string b, int km)
    {
        if (!_adjacency.TryGetValue(a, out var list)) _adjacency[a] = list = [];
        list.Add((b, km));
    }

    private int Estimate(string from, string to)
    {
        if (_adjacency.ContainsKey(from) && _adjacency.ContainsKey(to))
        {
            var paths = PathsFrom(from);
            if (paths.TryGetValue(to, out var path))
                return Math.Max(1, (int)Math.Round(path * (_factor ??= Calibrate())));
        }
        return Fallback();
    }

    private int Fallback()
    {
        if (_fallback is { } cached) return cached;
        var value = DefaultKm;
        if (_known.Count > 0)
        {
            var sorted = _known.Values.Order().ToArray();
            value = sorted[sorted.Length / 2];
        }
        _fallback = value;
        return value;
    }

    private Dictionary<string, int> PathsFrom(string source)
    {
        if (_paths.TryGetValue(source, out var hit)) return hit;
        return _paths[source] = Dijkstra(source, target: null, skip: null);
    }

    private double Calibrate()
    {
        var ordered = _known.OrderBy(p => p.Key.A, StringComparer.Ordinal).ThenBy(p => p.Key.B, StringComparer.Ordinal).ToArray();
        if (ordered.Length == 0) return 1.0;
        var step = Math.Max(1, ordered.Length / CalibrationSample);
        var ratios = new List<double>();
        for (var i = 0; i < ordered.Length && ratios.Count < CalibrationSample; i += step)
        {
            var (pair, km) = (ordered[i].Key, ordered[i].Value);
            var path = Dijkstra(pair.A, pair.B, pair);
            if (path.TryGetValue(pair.B, out var viaGraph) && viaGraph > 0) ratios.Add((double)km / viaGraph);
        }
        if (ratios.Count == 0) return 1.0;
        ratios.Sort();
        return Math.Clamp(ratios[ratios.Count / 2], MinFactor, MaxFactor);
    }

    /// <summary>Menores caminhos a partir de <paramref name="source"/>; para cedo ao achar <paramref name="target"/>; <paramref name="skip"/> remove uma aresta.</summary>
    private Dictionary<string, int> Dijkstra(string source, string? target, CityPair? skip)
    {
        var dist = new Dictionary<string, int>(StringComparer.Ordinal) { [source] = 0 };
        var queue = new PriorityQueue<string, int>();
        queue.Enqueue(source, 0);
        while (queue.TryDequeue(out var city, out var d))
        {
            if (d > dist[city]) continue;
            if (city == target) break;
            foreach (var (next, km) in _adjacency[city])
            {
                if (skip is { } s && s == CityPair.Of(city, next)) continue;
                var nd = d + km;
                if (dist.TryGetValue(next, out var old) && old <= nd) continue;
                dist[next] = nd;
                queue.Enqueue(next, nd);
            }
        }
        return dist;
    }
}
