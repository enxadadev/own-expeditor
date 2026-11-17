using OwnExpeditor.Data;
using OwnExpeditor.Sii;

namespace OwnExpeditor.Core.Tests;

public class SaveSessionTests
{
    private static string Job(string id, string target, string cargo, int km, string variant = "trailer.scs_curt_s") =>
        $"job_offer_data : {id} {{\r\n target: {target}\r\n expiration_time: nil\r\n urgency: nil\r\n" +
        $" shortest_distance_km: {km}\r\n ferry_time: 0\r\n ferry_price: 0\r\n cargo: {cargo}\r\n" +
        $" company_truck: mercedes_4x2_b\r\n trailer_variant: {variant}\r\n" +
        " trailer_definition: trailer_def.scs.box.single_3.curtain\r\n units_count: 33\r\n" +
        " fill_ratio: 1\r\n trailer_place: 0\r\n}\r\n\r\n";

    private static readonly string Save =
        "SiiNunit\r\n{\r\n" +
        "economy : _nameless.1 {\r\n game_time: 1000\r\n}\r\n\r\n" +
        "company : company.volatile.kaarfor.budapest {\r\n job_offer: 2\r\n" +
        " job_offer[0]: _nameless.a\r\n job_offer[1]: _nameless.b\r\n}\r\n\r\n" +
        "company : company.volatile.lkwlog.kosice {\r\n job_offer: 0\r\n}\r\n\r\n" +
        "company : company.volatile.dfh.berlin {\r\n job_offer: 0\r\n}\r\n\r\n" +
        "company : company.volatile.dfh.budapest {\r\n job_offer: 0\r\n}\r\n\r\n" +
        Job("_nameless.a", "\"lkwlog.kosice\"", "cargo.used_plast", 226) +
        Job("_nameless.b", "\"\"", "null", 0, "null") +
        "}\r\n";

    private static GameCatalog Catalog()
    {
        var cat = new GameCatalog();
        var kaarfor = new CompanyDef("kaarfor");
        kaarfor.CargoOut.UnionWith(["used_plast", "empty_palet"]);
        var dfh = new CompanyDef("dfh");
        dfh.CargoIn.Add("used_plast");
        var lkwlog = new CompanyDef("lkwlog");
        lkwlog.CargoIn.Add("used_plast");
        cat.Companies["kaarfor"] = kaarfor;
        cat.Companies["dfh"] = dfh;
        cat.Companies["lkwlog"] = lkwlog;
        return cat;
    }

    private static SaveSession Session() => new(SiiDocument.Parse(Save), Catalog());

    [Fact]
    public void Origins_are_companies_with_free_slot()
    {
        Assert.Equal(["Budapest – Kaarfor"], Session().Origins().Select(o => o.ToString()));
    }

    [Fact]
    public void Cargos_only_with_known_trailer_template()
    {
        var s = Session();
        // empty_palet é enviada pela kaarfor, mas não há reboque de referência no save
        Assert.Equal(["used_plast"], s.CargosFor(s.Origins()[0]));
    }

    [Fact]
    public void Destinations_accept_cargo_and_exclude_same_city()
    {
        var s = Session();
        var dests = s.Destinations(s.Origins()[0], "used_plast").Select(d => d.Key);
        Assert.Equal(["dfh.berlin", "lkwlog.kosice"], dests);
    }

    [Fact]
    public void Distance_is_learned_from_existing_jobs_both_ways()
    {
        var s = Session();
        Assert.Equal(226, s.Distance("budapest", "kosice"));
        Assert.Equal(226, s.Distance("kosice", "budapest"));
        Assert.Null(s.Distance("budapest", "berlin"));
    }

    [Fact]
    public void Inject_uses_template_and_consumes_slot()
    {
        var s = Session();
        var origin = s.Origins()[0];
        var target = s.Destinations(origin, "used_plast")[0];

        var id = s.Inject(origin, target, "used_plast", 847);

        var job = s.Document.Find(id)!;
        Assert.Equal("\"dfh.berlin\"", job.Get("target"));
        Assert.Equal("trailer.scs_curt_s", job.Get("trailer_variant"));
        Assert.Equal("33", job.Get("units_count"));
        Assert.Empty(s.Origins());
    }
}
