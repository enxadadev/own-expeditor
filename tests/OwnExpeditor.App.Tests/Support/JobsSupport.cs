using OwnExpeditor.App.Modules.Jobs;
using OwnExpeditor.App.Services;
using OwnExpeditor.Core;
using OwnExpeditor.Core.Games;
using OwnExpeditor.Core.Modules.Jobs;
using OwnExpeditor.Core.Session;
using OwnExpeditor.Core.Text;
using OwnExpeditor.Domain.Games;
using OwnExpeditor.Domain.Saves;
using OwnExpeditor.Domain.Settings;

namespace OwnExpeditor.App.Tests.Support;

/// <summary>Regras de carga em memória; nomes = id (fáceis de afirmar).</summary>
public sealed class FakeRules(bool available = true) : IGameData
{
    public Dictionary<string, HashSet<string>> Out { get; } = [];
    public Dictionary<string, HashSet<string>> In { get; } = [];
    public GameKind Game => GameKind.Ets2;
    public bool IsAvailable => available;
    public string Get(GameNameKind kind, string id) => id switch { "sofia" => "Sófia", "belgrad" => "Belgrado", _ => id };
    public bool Ships(string company, string cargo) => Out.TryGetValue(company, out var s) && s.Contains(cargo);
    public bool Accepts(string company, string cargo) => In.TryGetValue(company, out var s) && s.Contains(cargo);
    public IReadOnlyCollection<string> CargoOut(string company) => Out.TryGetValue(company, out var s) ? s : [];
}

/// <summary>
/// Save sintético do módulo (game_time 1000). sofia: radus (oferta a + vagas f1, f2), kaarfor (vaga f3, sem ofertas);
/// belgrad: tradeaux (oferta b + vaga f4); istanbul: lognstick (ofertas c, d, sem vagas); praha: euroacres (vaga f5, sem rota conhecida);
/// oslo: nordfrakt (sem regras).
/// </summary>
public static class JobsSample
{
    public static string Job(string id, string target, string cargo, int km, string variant, int units = 33, string expiration = "1500") =>
        $"job_offer_data : {id} {{\r\n target: {target}\r\n expiration_time: {expiration}\r\n urgency: 0\r\n" +
        $" shortest_distance_km: {km}\r\n ferry_time: 0\r\n ferry_price: 0\r\n cargo: {cargo}\r\n" +
        $" company_truck: truck_x\r\n trailer_variant: {variant}\r\n trailer_definition: trailer_def.x\r\n" +
        $" units_count: {units}\r\n fill_ratio: 1\r\n trailer_place: 0\r\n}}\r\n\r\n";

    public static string Free(string id) => Job(id, "\"\"", "null", 0, "null", 0, "nil");

    public static string Company(string key, params string[] offers) =>
        $"company : company.volatile.{key} {{\r\n job_offer: {offers.Length}\r\n" +
        string.Concat(offers.Select((o, i) => $" job_offer[{i}]: {o}\r\n")) + "}\r\n\r\n";

    public static string Text(bool economy = true, bool slots = true) =>
        "SiiNunit\r\n{\r\n" + (economy ? "economy : _nameless.1 {\r\n game_time: 1000\r\n}\r\n\r\n" : "") +
        (slots
            ? Company("radus.sofia", "_nameless.a", "_nameless.f1", "_nameless.f2") + Company("kaarfor.sofia", "_nameless.f3") +
              Company("tradeaux.belgrad", "_nameless.b", "_nameless.f4") + Company("lognstick.istanbul", "_nameless.c", "_nameless.d") +
              Company("euroacres.praha", "_nameless.f5") + Company("nordfrakt.oslo") +
              Job("_nameless.a", "\"tradeaux.belgrad\"", "cargo.machinery", 394, "trailer.lowboy", 1) +
              Job("_nameless.b", "\"radus.sofia\"", "cargo.beams", 394, "trailer.flatbed", 24) +
              Job("_nameless.c", "\"tradeaux.belgrad\"", "cargo.bricks", 980, "trailer.flatbed", 22) +
              Job("_nameless.d", "\"radus.sofia\"", "cargo.plastic", 980, "trailer.curtain", 33) +
              string.Concat(Enumerable.Range(1, 5).Select(i => Free($"_nameless.f{i}")))
            : Company("radus.sofia") + Company("tradeaux.belgrad")) +
        "}\r\n";

    public static FakeRules Rules(bool available = true)
    {
        var r = new FakeRules(available);
        r.Out["radus"] = ["machinery", "bricks"];
        r.Out["kaarfor"] = ["machinery", "plastic"];
        r.Out["tradeaux"] = ["beams", "bricks"];
        r.Out["lognstick"] = ["bricks", "beams"];
        r.Out["euroacres"] = ["plastic"];
        r.In["tradeaux"] = ["machinery", "plastic"];
        r.In["radus"] = ["beams", "bricks"];
        r.In["lognstick"] = ["machinery", "plastic", "bricks"];
        r.In["kaarfor"] = ["beams"];
        r.In["euroacres"] = ["machinery"];
        return r;
    }

    public static readonly CompanyLocation Radus = new("radus", "sofia");
    public static readonly CompanyLocation Kaarfor = new("kaarfor", "sofia");
    public static readonly CompanyLocation Tradeaux = new("tradeaux", "belgrad");
    public static readonly CompanyLocation Lognstick = new("lognstick", "istanbul");
}

/// <summary>Fábrica de board sem disco: monta o board real sobre a <c>View</c>, conta as atualizações de distância.</summary>
public sealed class FakeBoardFactory : IJobBoardFactory
{
    public Dictionary<CityPair, int> Known { get; } = [];

    public int Creates { get; private set; }

    public List<GameKind> Refreshes { get; } = [];

    public int RefreshAdds { get; set; }

    public JobBoard Create(IEditSession session)
    {
        Creates++;
        return JobBoard.Build(session.View, session.GameData, Known);
    }

    public Task<int> RefreshDistancesAsync(GameKind game, CancellationToken ct)
    {
        Refreshes.Add(game);
        return Task.FromResult(RefreshAdds);
    }
}

public sealed class FakeToast : IToastService
{
    public List<(TextRef Text, TextRef? Label, Action? Action)> Shown { get; } = [];

    public void Show(TextRef text, TextRef? actionLabel = null, Action? action = null) => Shown.Add((text, actionLabel, action));
}

public sealed class FakeNavigation : OwnExpeditor.App.ViewModels.IShellNavigation
{
    public int Reviews { get; private set; }
    public int Settings { get; private set; }
    public int Saves { get; private set; }

    public void OpenReview() => Reviews++;

    public void OpenSettings() => Settings++;

    public void ChooseSave() => Saves++;
}

/// <summary>Sessão que recusa o <c>Stage</c> com o erro dado (conflito, patch velho...).</summary>
public sealed class StageFailsSession(IEditSession inner, CoreError code) : IEditSession
{
    public SaveSlotInfo Save => inner.Save;
    public GameKind Game => inner.Game;
    public bool IsReadOnly => inner.IsReadOnly;
    public ISaveView Original => inner.Original;
    public ISaveView View => inner.View;
    public IGameData GameData => inner.GameData;
    public IReadOnlyList<ChangeSet> Pending => inner.Pending;
    public bool HasChanges => inner.HasChanges;
    public event EventHandler? Changed { add => inner.Changed += value; remove => inner.Changed -= value; }
    public void Stage(ChangeSet change) => throw new CoreException(code, "recusado");
    public bool Discard(Guid changeSetId) => inner.Discard(changeSetId);
    public void DiscardModule(string moduleId) => inner.DiscardModule(moduleId);
    public void DiscardAll() => inner.DiscardAll();
    public SessionDiff GetDiff() => inner.GetDiff();
    public Task<ApplyResult> ApplyAsync(ApplyTarget target, CancellationToken ct) => inner.ApplyAsync(target, ct);
}

/// <summary>Módulo Fretes montado com fakes; a sessão é real (Stage/Discard/Original/View de verdade) sobre o save sintético.</summary>
public sealed class JobsFixture
{
    public JobsFixture(Language language = Language.En, string? text = null, bool catalog = true, bool cloud = false)
    {
        Loc = new Localizer(language);
        Session = new TestSession(EditSessionFactory.FromText(text ?? JobsSample.Text(), Sample.Slot(cloud: cloud), JobsSample.Rules(catalog)));
        Page = new JobsPageViewModel(Loc, Factory, Toast, new ImmediateUiDispatcher()) { Navigation = Navigation };
    }

    public Localizer Loc { get; }
    public TestSession Session { get; }
    public FakeBoardFactory Factory { get; } = new();
    public FakeToast Toast { get; } = new();
    public FakeNavigation Navigation { get; } = new();
    public JobsPageViewModel Page { get; }

    public async Task<JobsFixture> AttachAsync(IEditSession? session = null)
    {
        await Page.AttachAsync(session ?? Session, default);
        return this;
    }

    public static void Pick(JobPickerViewModel picker, object key) =>
        picker.Selected = picker.AllOptions.First(o => Equals(o.Key, key));

    /// <summary>Monta o frete completo sofia → istanbul (radus, machinery, lognstick).</summary>
    public void Ready()
    {
        Page.SelectFromCity("sofia");
        Pick(Page.ToCity, "istanbul");
        Pick(Page.FromCompany, JobsSample.Radus);
        Pick(Page.Cargo, "machinery");
        Pick(Page.ToCompany, JobsSample.Lognstick);
    }
}
