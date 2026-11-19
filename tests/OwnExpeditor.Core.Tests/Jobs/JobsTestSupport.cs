using OwnExpeditor.Core.Games;
using OwnExpeditor.Core.Session;
using OwnExpeditor.Core.Tests;
using OwnExpeditor.Domain.Games;

namespace OwnExpeditor.Core.Modules.Jobs.Tests;

/// <summary>Regras de carga em memória (catálogo do jogo sintético).</summary>
public sealed class RulesGameData : IGameData
{
    public Dictionary<string, HashSet<string>> Out { get; } = [];
    public Dictionary<string, HashSet<string>> In { get; } = [];
    public GameKind Game => GameKind.Ets2;
    public bool IsAvailable => true;
    public string Get(GameNameKind kind, string id) => $"{kind}:{id}";
    public bool Ships(string company, string cargo) => Out.TryGetValue(company, out var s) && s.Contains(cargo);
    public bool Accepts(string company, string cargo) => In.TryGetValue(company, out var s) && s.Contains(cargo);
    public IReadOnlyCollection<string> CargoOut(string company) => Out.TryGetValue(company, out var s) ? s : [];
    public Dictionary<string, string> Countries { get; } = [];
    public string? CityCountry(string city) => Countries.GetValueOrDefault(city);

    public static RulesGameData Demo()
    {
        var d = new RulesGameData();
        d.Out["kaarfor"] = ["used_plast", "empty_palet", "ghost"];
        d.Out["lkwlog"] = ["empty_palet"];
        d.In["dfh"] = ["used_plast", "empty_palet", "ghost"];
        d.In["lkwlog"] = ["used_plast"];
        d.In["kaarfor"] = ["empty_palet"];
        return d;
    }
}

/// <summary>
/// Save sintético (ids inventados). Empresas: kaarfor.budapest (oferta a + vaga livre b), dfh.berlin (sem slots),
/// lkwlog.kosice (só oferta c), tradeaux.wien (vaga livre d, sem carga com reboque). game_time = 1000.
/// </summary>
public static class JobsSave
{
    public static string Job(string id, string target, string cargo, int km, string variant, string definition = "trailer_def.x",
        string truck = "truck_x", int units = 33, string expiration = "1500") =>
        $"job_offer_data : {id} {{\r\n target: {target}\r\n expiration_time: {expiration}\r\n urgency: 0\r\n" +
        $" shortest_distance_km: {km}\r\n ferry_time: 0\r\n ferry_price: 0\r\n cargo: {cargo}\r\n" +
        $" company_truck: {truck}\r\n trailer_variant: {variant}\r\n trailer_definition: {definition}\r\n" +
        $" units_count: {units}\r\n fill_ratio: 1\r\n trailer_place: 0\r\n}}\r\n\r\n";

    public static string Company(string key, params string[] offers) =>
        $"company : company.volatile.{key} {{\r\n job_offer: {offers.Length}\r\n" +
        string.Concat(offers.Select((o, i) => $" job_offer[{i}]: {o}\r\n")) + "}\r\n\r\n";

    public static string Text(bool economy = true) =>
        "SiiNunit\r\n{\r\n" +
        (economy ? "economy : _nameless.1 {\r\n game_time: 1000\r\n}\r\n\r\n" : "") +
        Company("kaarfor.budapest", "_nameless.a", "_nameless.b") +
        Company("dfh.berlin") +
        Company("lkwlog.kosice", "_nameless.c") +
        Company("tradeaux.wien", "_nameless.d") +
        Job("_nameless.a", "\"lkwlog.kosice\"", "cargo.used_plast", 226, "trailer.curtain", "trailer_def.curtain", "truck_a", 33) +
        Job("_nameless.b", "\"\"", "null", 0, "null", "null", "null", 0, "nil") +
        Job("_nameless.c", "\"kaarfor.budapest\"", "cargo.empty_palet", 226, "trailer.palet", "trailer_def.palet", "truck_c", 12) +
        Job("_nameless.d", "\"\"", "null", 0, "null", "null", "null", 0, "nil") +
        "}\r\n";

    public static IEditSession Open(string? text = null, IGameData? data = null) =>
        EditSessionFactory.FromText(text ?? Text(), SampleSave.Slot(), data ?? RulesGameData.Demo());

    public static JobBoard Board(IEditSession s) => JobBoard.Build(s.View, s.GameData);

    public static readonly CompanyLocation Kaarfor = new("kaarfor", "budapest");
    public static readonly CompanyLocation Dfh = new("dfh", "berlin");
    public static readonly CompanyLocation Lkwlog = new("lkwlog", "kosice");
}

/// <summary>
/// Save sintético com países (ids inventados). hu: budapest (kaarfor com oferta a + vaga b; lkwlog vaga h) e gyor (kaarfor, vaga e);
/// sk: kosice (lkwlog, ofertas c e i, sem vaga livre) e bratislava (lkwlog, vaga g); de: berlin (dfh, sem slots);
/// sem país: praha (lkwlog, vaga f) e wien (tradeaux, vaga d, sem carga com reboque). Regras de carga = <see cref="RulesGameData.Demo"/>.
/// </summary>
public static class CountrySave
{
    public static string Text() =>
        "SiiNunit\r\n{\r\neconomy : _nameless.1 {\r\n game_time: 1000\r\n}\r\n\r\n" +
        JobsSave.Company("kaarfor.budapest", "_nameless.a", "_nameless.b") +
        JobsSave.Company("lkwlog.budapest", "_nameless.h") +
        JobsSave.Company("kaarfor.gyor", "_nameless.e") +
        JobsSave.Company("dfh.berlin") +
        JobsSave.Company("lkwlog.kosice", "_nameless.c", "_nameless.i") +
        JobsSave.Company("lkwlog.bratislava", "_nameless.g") +
        JobsSave.Company("lkwlog.praha", "_nameless.f") +
        JobsSave.Company("tradeaux.wien", "_nameless.d") +
        JobsSave.Job("_nameless.a", "\"lkwlog.kosice\"", "cargo.used_plast", 226, "trailer.curtain", "trailer_def.curtain", "truck_a", 33) +
        JobsSave.Job("_nameless.c", "\"kaarfor.budapest\"", "cargo.empty_palet", 226, "trailer.palet", "trailer_def.palet", "truck_c", 12) +
        JobsSave.Job("_nameless.i", "\"kaarfor.gyor\"", "cargo.empty_palet", 300, "trailer.palet", "trailer_def.palet", "truck_c", 12) +
        string.Concat(new[] { "b", "d", "e", "f", "g", "h" }.Select(id =>
            JobsSave.Job("_nameless." + id, "\"\"", "null", 0, "null", "null", "null", 0, "nil"))) +
        "}\r\n";

    public static RulesGameData Data()
    {
        var d = RulesGameData.Demo();
        d.Countries["budapest"] = "hu";
        d.Countries["gyor"] = "hu";
        d.Countries["kosice"] = "sk";
        d.Countries["bratislava"] = "sk";
        d.Countries["berlin"] = "de";
        return d;
    }

    /// <summary>Todas as cidades do save padrão (<see cref="JobsSave.Text"/>) no mesmo país.</summary>
    public static RulesGameData SingleCountryData()
    {
        var d = RulesGameData.Demo();
        foreach (var city in new[] { "budapest", "berlin", "kosice", "wien" }) d.Countries[city] = "hu";
        return d;
    }

    public static IEditSession Open() => JobsSave.Open(Text(), Data());

    public static JobBoard Board(IEditSession? session = null) => JobsSave.Board(session ?? Open());

    public static readonly CompanyLocation Budapest = JobsSave.Kaarfor;
    public static readonly CompanyLocation LkwlogBudapest = new("lkwlog", "budapest");
    public static readonly CompanyLocation Gyor = new("kaarfor", "gyor");
    public static readonly CompanyLocation Berlin = JobsSave.Dfh;
    public static readonly CompanyLocation Kosice = JobsSave.Lkwlog;
    public static readonly CompanyLocation Bratislava = new("lkwlog", "bratislava");
    public static readonly CompanyLocation Praha = new("lkwlog", "praha");
}
