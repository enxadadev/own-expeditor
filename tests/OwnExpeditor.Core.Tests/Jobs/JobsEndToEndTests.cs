using Microsoft.Extensions.DependencyInjection;
using OwnExpeditor.Core.Games;
using OwnExpeditor.Core.Session;
using OwnExpeditor.Core.Tests;
using OwnExpeditor.Data;
using OwnExpeditor.Data.Games;
using OwnExpeditor.Data.Saves;
using OwnExpeditor.Domain.Games;
using OwnExpeditor.Domain.Saves;
using OwnExpeditor.Sii;
using static OwnExpeditor.Core.Modules.Jobs.Tests.JobsSave;

namespace OwnExpeditor.Core.Modules.Jobs.Tests;

/// <summary>
/// Fluxo completo com arquivos temporários e a Data real: abrir → JobBoard → JobPlanner → Stage → ApplyAsync
/// (Sobrescrever e Criar novo save). Catálogo de jogo sintético (sem pasta do jogo).
/// </summary>
public sealed class JobsEndToEndTests : IDisposable
{
    private readonly TempDir _t = new();
    private readonly string _gameSii;
    private readonly ServiceProvider _sp;

    public JobsEndToEndTests()
    {
        var hex = Convert.ToHexString("p1"u8.ToArray());
        _t.File("SiiNunit\r\n{\r\nsave_container : _nameless.0 {\r\n name: \"Slot\"\r\n}\r\n\r\n}\r\n",
            "docs", "Euro Truck Simulator 2", "profiles", hex, "save", "1", "info.sii");
        _gameSii = _t.File(JobsSave.Text(), "docs", "Euro Truck Simulator 2", "profiles", hex, "save", "1", "game.sii");

        var services = new ServiceCollection();
        services.AddSingleton<IGameProcessMonitor>(new FakeProcessMonitor());
        var gameData = new FakeGameDataService();
        gameData.Data[GameKind.Ets2] = RulesGameData.Demo(); // catálogo sintético no lugar da pasta do jogo
        services.AddSingleton<IGameDataService>(gameData);
        services.AddSingleton(new DataOptions
        {
            DocumentsRoot = _t.Combine("docs"),
            SettingsDirectory = _t.Combine("settings"),
            HomeDirectory = _t.Combine("home"),
            SteamRoots = [],
            UseRegistry = false,
        });
        services.AddOwnExpeditorCore();
        services.AddJobsModule();
        _sp = services.BuildServiceProvider();
    }

    public void Dispose()
    {
        _sp.Dispose();
        _t.Dispose();
    }

    private async Task<IEditSession> OpenAsync()
    {
        var locator = _sp.GetRequiredService<IProfileLocator>();
        var slot = Assert.Single(locator.FindSaves(Assert.Single(locator.FindProfiles())));
        return await _sp.GetRequiredService<IEditSessionHost>().OpenAsync(slot, null, default);
    }

    private JobBoard BoardOf(IEditSession s) => _sp.GetRequiredService<IJobBoardFactory>().Create(s);

    private static SiiDocument Load(string path) => SiiDocument.Load(path);

    [Fact]
    public async Task Overwrite_fill_empty_writes_the_job_with_backup_and_no_tmp()
    {
        var s = await OpenAsync();
        var board = BoardOf(s);
        s.Stage(JobPlanner.Plan(s.Original, board, new JobRequest(Kaarfor, Lkwlog, "used_plast", JobSlotMode.FillEmpty)));

        var result = await s.ApplyAsync(new ApplyTarget.Overwrite(), default);

        var job = Load(_gameSii).Find("_nameless.b")!;
        Assert.Equal("\"lkwlog.kosice\"", job.Get("target"));
        Assert.Equal("cargo.used_plast", job.Get("cargo"));
        Assert.Equal("226", job.Get("shortest_distance_km"));
        Assert.Equal("trailer.curtain", job.Get("trailer_variant"));
        Assert.Equal("\"lkwlog.kosice\"", Load(_gameSii).Find("_nameless.a")!.Get("target")); // a oferta existente intacta
        Assert.NotNull(result.BackupPath);
        Assert.Equal(JobsSave.Text(), File.ReadAllText(result.BackupPath!));
        Assert.Empty(Directory.GetFiles(Path.GetDirectoryName(_gameSii)!, "*.tmp"));
        Assert.False(s.HasChanges);
        Assert.Equal(1, result.ChangeSetCount);
    }

    [Fact]
    public async Task Overwrite_replace_offer_overwrites_every_field_including_trailer()
    {
        var s = await OpenAsync();
        s.Stage(JobPlanner.Plan(s.Original, BoardOf(s),
            new JobRequest(Kaarfor, Dfh, "empty_palet", JobSlotMode.ReplaceOffer, "_nameless.a", ExpirationHours: 168)));

        await s.ApplyAsync(new ApplyTarget.Overwrite(), default);

        var job = Load(_gameSii).Find("_nameless.a")!;
        Assert.Equal("\"dfh.berlin\"", job.Get("target"));
        Assert.Equal("cargo.empty_palet", job.Get("cargo"));
        Assert.Equal("trailer.palet", job.Get("trailer_variant"));
        Assert.Equal("trailer_def.palet", job.Get("trailer_definition"));
        Assert.Equal("truck_c", job.Get("company_truck"));
        Assert.Equal("12", job.Get("units_count"));
        Assert.Equal((1000 + 168 * 60).ToString(), job.Get("expiration_time"));
        Assert.Equal("\"\"", Load(_gameSii).Find("_nameless.b")!.Get("target")); // a vaga livre não foi tocada
    }

    [Fact]
    public async Task Overwrite_edited_pending_change_writes_only_the_final_version()
    {
        var s = await OpenAsync();
        s.Stage(JobPlanner.Plan(s.Original, BoardOf(s), new JobRequest(Kaarfor, Dfh, "used_plast", JobSlotMode.FillEmpty)));
        s.Stage(JobPlanner.Plan(s.Original, BoardOf(s),
            new JobRequest(Kaarfor, Lkwlog, "used_plast", JobSlotMode.FillEmpty, FillSlotId: "_nameless.b", ExpirationHours: 24)));

        var result = await s.ApplyAsync(new ApplyTarget.Overwrite(), default);

        Assert.Equal(1, result.ChangeSetCount);
        var job = Load(_gameSii).Find("_nameless.b")!;
        Assert.Equal("\"lkwlog.kosice\"", job.Get("target"));
        Assert.Equal((1000 + 24 * 60).ToString(), job.Get("expiration_time"));
    }

    [Fact]
    public async Task NewSlot_writes_the_job_in_a_new_slot_and_leaves_the_original_untouched()
    {
        var s = await OpenAsync();
        s.Stage(JobPlanner.Plan(s.Original, BoardOf(s), new JobRequest(Kaarfor, Lkwlog, "used_plast", JobSlotMode.FillEmpty)));

        var result = await s.ApplyAsync(new ApplyTarget.NewSlot("Com frete"), default);

        Assert.Null(result.BackupPath);
        Assert.Equal(JobsSave.Text(), File.ReadAllText(_gameSii)); // original intacto
        Assert.NotEqual(_gameSii, result.WrittenSlot.GameSiiPath);
        Assert.Equal("\"lkwlog.kosice\"", Load(result.WrittenSlot.GameSiiPath).Find("_nameless.b")!.Get("target"));
        Assert.Contains("Com frete", File.ReadAllText(Path.Combine(Path.GetDirectoryName(result.WrittenSlot.GameSiiPath)!, "info.sii")));
        Assert.Equal(result.WrittenSlot.GameSiiPath, s.Save.GameSiiPath); // a sessão passa a apontar para o slot novo
    }

    [Fact]
    public async Task After_apply_a_new_board_sees_the_slot_as_used()
    {
        var s = await OpenAsync();
        s.Stage(JobPlanner.Plan(s.Original, BoardOf(s), new JobRequest(Kaarfor, Lkwlog, "used_plast", JobSlotMode.FillEmpty)));
        await s.ApplyAsync(new ApplyTarget.Overwrite(), default);

        var board = BoardOf(s);

        Assert.Equal(0, board.FreeSlots(Kaarfor));
        Assert.Equal(2, board.ReplaceableOffers(Kaarfor).Count);
        Assert.Throws<CoreException>(() =>
            JobPlanner.Plan(s.Original, board, new JobRequest(Kaarfor, Lkwlog, "used_plast", JobSlotMode.FillEmpty)));
    }

    [Fact]
    public async Task Distances_learned_from_the_opened_save_reach_the_cache_file()
    {
        var s = await OpenAsync();
        BoardOf(s);
        await _sp.GetRequiredService<OwnExpeditor.Data.Distances.IDistanceCache>().FlushAsync(); // a gravação é adiada

        Assert.True(File.Exists(_t.Combine("settings", "distances-ets2.json")));
        Assert.Contains("budapest|kosice", File.ReadAllText(_t.Combine("settings", "distances-ets2.json")));
    }

    [Fact]
    public async Task Refresh_scans_all_saves_of_all_profiles()
    {
        var hex = Convert.ToHexString("p1"u8.ToArray());
        _t.File(JobsSave.Text().Replace("lkwlog.kosice\"", "dfh.berlin\"", StringComparison.Ordinal),
            "docs", "Euro Truck Simulator 2", "profiles", hex, "save", "2", "game.sii");

        var added = await _sp.GetRequiredService<IJobBoardFactory>().RefreshDistancesAsync(GameKind.Ets2, default);

        Assert.Equal(2, added);
    }

    [Fact]
    public async Task Estimated_distance_written_to_the_save_stays_estimated_after_reopen_and_scan()
    {
        var factory = _sp.GetRequiredService<IJobBoardFactory>();
        var cache = _sp.GetRequiredService<OwnExpeditor.Data.Distances.IDistanceCache>();
        var pair = CityPair.Of("budapest", "berlin");
        var s = await OpenAsync();
        var board = factory.Create(s);
        Assert.Equal(DistanceSource.Estimated, board.Distance("budapest", "berlin").Source);

        s.Stage(factory.Plan(s, board, new JobRequest(Kaarfor, Dfh, "used_plast", JobSlotMode.FillEmpty)));
        await s.ApplyAsync(new ApplyTarget.Overwrite(), default);
        var written = int.Parse(Load(_gameSii).Find("_nameless.b")!.Get("shortest_distance_km")!);
        Assert.Equal(board.Distance("budapest", "berlin").Km, written);

        var reopened = await OpenAsync();
        var again = factory.Create(reopened); // aprende do save aberto: a estimativa própria não conta
        Assert.Equal(new RouteDistance(written, DistanceSource.Estimated), again.Distance("budapest", "berlin"));
        Assert.False(cache.Load(GameKind.Ets2).ContainsKey(pair));

        await factory.RefreshDistancesAsync(GameKind.Ets2, default); // varredura dos saves também
        Assert.False(cache.Load(GameKind.Ets2).ContainsKey(pair));
        Assert.Equal(DistanceSource.Estimated, factory.Create(reopened).Distance("budapest", "berlin").Source);
    }

    [Fact]
    public async Task A_different_real_value_for_an_estimated_pair_becomes_known()
    {
        var factory = _sp.GetRequiredService<IJobBoardFactory>();
        var s = await OpenAsync();
        s.Stage(factory.Plan(s, factory.Create(s), new JobRequest(Kaarfor, Dfh, "used_plast", JobSlotMode.FillEmpty)));
        await s.ApplyAsync(new ApplyTarget.Overwrite(), default);
        var text = File.ReadAllText(_gameSii);
        var marker = text.IndexOf("job_offer_data : _nameless.b", StringComparison.Ordinal);
        var edited = text[..marker] + text[marker..].Replace("shortest_distance_km: 226", "shortest_distance_km: 777", StringComparison.Ordinal);
        File.WriteAllText(_gameSii, edited);

        var board = factory.Create(await OpenAsync());

        Assert.Equal(new RouteDistance(777, DistanceSource.Known), board.Distance("budapest", "berlin"));
    }
}
