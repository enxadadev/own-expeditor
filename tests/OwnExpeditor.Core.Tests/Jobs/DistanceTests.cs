using Microsoft.Extensions.DependencyInjection;
using OwnExpeditor.Core.Tests;
using OwnExpeditor.Data;
using OwnExpeditor.Data.Distances;
using OwnExpeditor.Domain.Games;
using OwnExpeditor.Domain.Saves;

namespace OwnExpeditor.Core.Modules.Jobs.Tests;

public sealed class DistanceTests : IDisposable
{
    private readonly TempDir _t = new();

    public void Dispose() => _t.Dispose();

    private static KeyValuePair<CityPair, int> P(string a, string b, int km) => new(CityPair.Of(a, b), km);

    private static DistanceTable Table(params KeyValuePair<CityPair, int>[] pairs) => new(pairs);

    private IDistanceCache Cache()
    {
        var services = new ServiceCollection();
        services.AddOwnExpeditorData(new DataOptions { SettingsDirectory = _t.Combine("settings") });
        return services.BuildServiceProvider().GetRequiredService<IDistanceCache>();
    }

    // ---- tabela e estimativa

    [Fact]
    public void Known_pair_works_both_ways()
    {
        var t = Table(P("a", "b", 100));
        Assert.Equal(new RouteDistance(100, DistanceSource.Known), t.Get("a", "b"));
        Assert.Equal(new RouteDistance(100, DistanceSource.Known), t.Get("b", "a"));
    }

    [Fact]
    public void Same_city_is_zero_known()
    {
        Assert.Equal(new RouteDistance(0, DistanceSource.Known), Table().Get("a", "a"));
    }

    [Fact]
    public void Empty_table_never_returns_null_it_estimates_a_default()
    {
        var d = Table().Get("a", "b");
        Assert.Equal(DistanceSource.Estimated, d.Source);
        Assert.Equal(DistanceTable.DefaultKm, d.Km);
    }

    [Fact]
    public void Invalid_pairs_are_dropped()
    {
        var t = Table(P("a", "b", 0), P("a", "c", -5), P("d", "d", 10));
        Assert.Empty(t.Known);
    }

    [Fact]
    public void Unknown_pair_is_estimated_by_path_through_known_cities()
    {
        // a --100-- b --200-- c: a→c não é conhecida; caminho = 300 (um par isolado não calibra, fator 1)
        var d = Table(P("a", "b", 100), P("b", "c", 200)).Get("a", "c");
        Assert.Equal(DistanceSource.Estimated, d.Source);
        Assert.InRange(d.Km, 90, 360);
    }

    [Fact]
    public void City_without_any_known_pair_falls_back_to_the_median()
    {
        var d = Table(P("a", "b", 100), P("b", "c", 300), P("a", "c", 200)).Get("a", "island");
        Assert.Equal(new RouteDistance(200, DistanceSource.Estimated), d);
    }

    [Fact]
    public void Estimate_is_symmetric_and_stable()
    {
        var t = Table(P("a", "b", 100), P("b", "c", 150), P("c", "d", 120), P("a", "d", 400));
        Assert.Equal(t.Get("a", "c").Km, t.Get("c", "a").Km);
        Assert.Equal(t.Get("a", "c"), t.Get("a", "c"));
    }

    [Fact]
    public void Calibration_keeps_the_estimate_within_bounds_of_the_graph_path()
    {
        // a→d só existe via a-b-c-d (caminho 210); o fator calibrado fica em [0,3; 1,2]
        var d = Table(P("a", "b", 100), P("b", "c", 100), P("a", "c", 50), P("c", "d", 10)).Get("a", "d");
        Assert.InRange(d.Km, 1, (int)Math.Ceiling(60 * 1.2));
    }

    [Fact]
    public void Known_is_read_only_and_reflects_the_valid_pairs()
    {
        var t = Table(P("a", "b", 100), P("c", "d", 0));
        Assert.Equal(100, Assert.Single(t.Known).Value);
        Assert.IsNotType<Dictionary<CityPair, int>>(t.Known);
    }

    [Fact]
    public void Estimate_on_a_synthetic_road_network_is_close_to_the_truth()
    {
        // 30 cidades num plano (semente fixa); estrada = reta × 1,3; só os 5 vizinhos mais próximos de cada uma são conhecidos
        var rng = new Random(1);
        var pos = Enumerable.Range(0, 30).Select(_ => (X: rng.NextDouble() * 800, Y: rng.NextDouble() * 600)).ToArray();
        double Line(int i, int j) => Math.Sqrt(Math.Pow(pos[i].X - pos[j].X, 2) + Math.Pow(pos[i].Y - pos[j].Y, 2));
        int Road(int i, int j) => (int)Math.Round(Line(i, j) * 1.3);
        var knownPairs = new HashSet<(int, int)>();
        for (var i = 0; i < pos.Length; i++)
            foreach (var j in Enumerable.Range(0, pos.Length).Where(x => x != i).OrderBy(x => Line(i, x)).Take(5))
                knownPairs.Add((Math.Min(i, j), Math.Max(i, j)));
        var table = new DistanceTable(knownPairs.Select(p => P($"c{p.Item1}", $"c{p.Item2}", Road(p.Item1, p.Item2))));
        var hidden = (from i in Enumerable.Range(0, pos.Length) from j in Enumerable.Range(i + 1, pos.Length - i - 1)
                      where !knownPairs.Contains((i, j)) select (i, j)).ToList();

        var errors = hidden.Select(h => Math.Abs((double)table.Get($"c{h.i}", $"c{h.j}").Km / Road(h.i, h.j) - 1)).ToList();

        Assert.True(errors.Average() < 0.2, $"erro médio {errors.Average():P0}");
        Assert.All(hidden, h => Assert.Equal(DistanceSource.Estimated, table.Get($"c{h.i}", $"c{h.j}").Source));
    }

    // ---- JobBoard + tabela

    [Fact]
    public void Board_merges_cache_pairs_and_save_wins()
    {
        var s = JobsSave.Open();
        var cached = new Dictionary<CityPair, int>
        {
            [CityPair.Of("budapest", "berlin")] = 690,
            [CityPair.Of("budapest", "kosice")] = 999, // o save diz 226
        };

        var board = JobBoard.Build(s.View, s.GameData, cached);

        Assert.Equal(new RouteDistance(690, DistanceSource.Known), board.Distance("berlin", "budapest"));
        Assert.Equal(new RouteDistance(226, DistanceSource.Known), board.Distance("budapest", "kosice"));
    }

    [Fact]
    public void Board_distance_is_never_null_for_any_pair_of_cities()
    {
        var board = JobsSave.Board(JobsSave.Open());
        string[] cities = ["berlin", "budapest", "kosice", "wien"];
        foreach (var a in cities)
        foreach (var b in cities)
            Assert.True(board.Distance(a, b).Km >= 0);
    }

    [Fact]
    public void Distance_from_the_save_is_Known()
    {
        Assert.Equal(new RouteDistance(226, DistanceSource.Known), JobsSave.Board(JobsSave.Open()).Distance("budapest", "kosice"));
    }

    // ---- cache (Data)

    [Fact]
    public async Task Cache_persists_pairs_between_instances_and_keeps_existing_values()
    {
        var first = Cache();
        first.Merge(GameKind.Ets2, [P("a", "b", 100)]);
        first.Merge(GameKind.Ets2, [P("b", "a", 999), P("c", "d", 50)]);
        await first.FlushAsync();

        var loaded = Cache().Load(GameKind.Ets2);

        Assert.Equal(100, loaded[CityPair.Of("a", "b")]);
        Assert.Equal(50, loaded[CityPair.Of("c", "d")]);
        Assert.Empty(Cache().Load(GameKind.Ats));
    }

    [Fact]
    public void Cache_unreadable_file_starts_empty()
    {
        _t.File("{ not json", "settings", "distances-ets2.json");
        Assert.Empty(Cache().Load(GameKind.Ets2));
    }

    [Fact]
    public async Task Scan_aggregates_all_saves_and_skips_unchanged_ones()
    {
        var one = _t.File(JobsSave.Text(), "p", "save", "1", "game.sii");
        var two = _t.File(JobsSave.Text().Replace("lkwlog.kosice\"", "dfh.berlin\"", StringComparison.Ordinal), "p", "save", "2", "game.sii");
        var cache = Cache();
        var slots = new[] { Slot(one, "1"), Slot(two, "2") };

        var added = await cache.ScanAsync(GameKind.Ets2, slots, default);
        var again = await cache.ScanAsync(GameKind.Ets2, slots, default);

        Assert.Equal(2, added); // budapest–kosice (save 1) e budapest–berlin (save 2)
        Assert.Equal(0, again);
        var loaded = Cache().Load(GameKind.Ets2);
        Assert.Equal(226, loaded[CityPair.Of("budapest", "kosice")]);
    }

    [Fact]
    public async Task Scan_ignores_unreadable_saves()
    {
        var bad = _t.File("not a save", "p", "save", "1", "game.sii");
        var added = await Cache().ScanAsync(GameKind.Ets2, [Slot(bad, "1"), Slot(_t.Combine("nope", "game.sii"), "2")], default);
        Assert.Equal(0, added);
    }

    [Fact]
    public async Task Factory_feeds_the_cache_from_the_open_save_and_reuses_it()
    {
        var services = new ServiceCollection();
        services.AddOwnExpeditorData(new DataOptions { SettingsDirectory = _t.Combine("settings"), DocumentsRoot = _t.Combine("docs"), SteamRoots = [], UseRegistry = false });
        services.AddJobsModule();
        await using var sp = services.BuildServiceProvider();
        var cache = sp.GetRequiredService<IDistanceCache>();
        cache.Merge(GameKind.Ets2, [P("budapest", "berlin", 690)]);

        var board = sp.GetRequiredService<IJobBoardFactory>().Create(JobsSave.Open());

        Assert.Equal(new RouteDistance(690, DistanceSource.Known), board.Distance("budapest", "berlin"));
        Assert.Equal(226, cache.Load(GameKind.Ets2)[CityPair.Of("budapest", "kosice")]); // aprendido do save aberto
    }

    [Fact]
    public void Load_returns_the_same_immutable_snapshot_until_something_changes()
    {
        var cache = Cache();
        cache.Merge(GameKind.Ets2, [P("a", "b", 100)]);
        var first = cache.Load(GameKind.Ets2);

        Assert.Same(first, cache.Load(GameKind.Ets2));
        cache.Merge(GameKind.Ets2, [P("a", "b", 100)]); // nada novo: o mesmo instantâneo
        Assert.Same(first, cache.Load(GameKind.Ets2));
        cache.Merge(GameKind.Ets2, [P("c", "d", 5)]);
        Assert.NotSame(first, cache.Load(GameKind.Ets2));
        Assert.Single(first); // quem já tinha o instantâneo antigo não o vê mudar
    }

    [Fact]
    public async Task Merge_is_persisted_after_a_debounce_without_an_explicit_flush()
    {
        var cache = Cache();
        cache.Merge(GameKind.Ets2, [P("a", "b", 100)]);
        var path = _t.Combine("settings", "distances-ets2.json");
        for (var i = 0; i < 100 && !File.Exists(path); i++) await Task.Delay(50);
        Assert.True(File.Exists(path));
        Assert.Equal(100, Cache().Load(GameKind.Ets2)[CityPair.Of("a", "b")]);
    }

    [Fact]
    public async Task Own_estimate_is_ignored_when_read_back_and_overridden_by_a_different_real_value()
    {
        var cache = Cache();
        cache.RecordEstimate(GameKind.Ets2, CityPair.Of("a", "b"), 300);

        cache.Merge(GameKind.Ets2, [P("b", "a", 300)]);
        Assert.Empty(cache.Load(GameKind.Ets2));
        Assert.Equal(300, cache.LoadEstimates(GameKind.Ets2)[CityPair.Of("a", "b")]);

        await cache.FlushAsync(); // as marcas persistem no mesmo JSON
        var reopened = Cache();
        reopened.Merge(GameKind.Ets2, [P("a", "b", 300)]);
        Assert.Empty(reopened.Load(GameKind.Ets2));

        reopened.Merge(GameKind.Ets2, [P("a", "b", 410)]); // valor real diferente vence e apaga a marca
        Assert.Equal(410, reopened.Load(GameKind.Ets2)[CityPair.Of("a", "b")]);
        Assert.Empty(reopened.LoadEstimates(GameKind.Ets2));
    }

    [Fact]
    public void Estimate_for_an_already_known_pair_is_not_recorded()
    {
        var cache = Cache();
        cache.Merge(GameKind.Ets2, [P("a", "b", 100)]);
        cache.RecordEstimate(GameKind.Ets2, CityPair.Of("a", "b"), 120);
        Assert.Empty(cache.LoadEstimates(GameKind.Ets2));
    }

    [Fact]
    public void Old_cache_file_without_estimates_still_loads()
    {
        _t.File("{\"Version\":1,\"Pairs\":{\"a|b\":100},\"Scanned\":{}}", "settings", "distances-ets2.json");
        var cache = Cache();
        Assert.Equal(100, cache.Load(GameKind.Ets2)[CityPair.Of("a", "b")]);
        Assert.Empty(cache.LoadEstimates(GameKind.Ets2));
    }

    [Fact]
    public async Task Scan_skips_a_bad_save_and_still_reads_the_following_ones()
    {
        var bad = _t.File("not a save", "p", "save", "1", "game.sii");
        var good = _t.File(JobsSave.Text(), "p", "save", "2", "game.sii");
        var added = await Cache().ScanAsync(GameKind.Ets2, [Slot(bad, "1"), Slot(good, "2")], default);
        Assert.Equal(1, added);
    }

    [Fact]
    public async Task Scan_persists_only_when_something_changed()
    {
        var path = _t.Combine("settings", "distances-ets2.json");
        var cache = Cache();
        var bad = _t.File("not a save", "p", "save", "1", "game.sii");
        await cache.ScanAsync(GameKind.Ets2, [Slot(bad, "1")], default);
        Assert.False(File.Exists(path)); // nada lido, nada gravado

        var good = _t.File(JobsSave.Text(), "p", "save", "2", "game.sii");
        await cache.ScanAsync(GameKind.Ets2, [Slot(good, "2")], default);
        Assert.True(File.Exists(path));

        File.Delete(path);
        await cache.ScanAsync(GameKind.Ets2, [Slot(good, "2")], default); // já lido e sem mudança
        Assert.False(File.Exists(path));
    }

    private static SaveSlotInfo Slot(string path, string id) =>
        new(new ProfileInfo(GameKind.Ets2, "p", "p", "/p", false), id, id, path, DateTime.UtcNow, 0);
}
