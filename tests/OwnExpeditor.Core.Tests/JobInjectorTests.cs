using OwnExpeditor.Data;
using OwnExpeditor.Core;
using OwnExpeditor.Sii;

namespace OwnExpeditor.Core.Tests;

public class JobInjectorTests
{
    private static readonly string Save =
        "SiiNunit\r\n{\r\n" +
        "economy : _nameless.1 {\r\n game_time: 1000\r\n}\r\n\r\n" +
        "company : company.volatile.tradeaux.budapest {\r\n job_offer: 2\r\n" +
        " job_offer[0]: _nameless.a\r\n job_offer[1]: _nameless.b\r\n}\r\n\r\n" +
        "company : company.volatile.dfh.berlin {\r\n job_offer: 0\r\n}\r\n\r\n" +
        Job("_nameless.a", "\"stokes.wien\"", "cargo.plumb_suppl") +
        Job("_nameless.b", "\"\"", "null") +
        "}\r\n";

    private static string Job(string id, string target, string cargo) =>
        $"job_offer_data : {id} {{\r\n target: {target}\r\n expiration_time: nil\r\n urgency: nil\r\n" +
        $" shortest_distance_km: 0\r\n ferry_time: 0\r\n ferry_price: 0\r\n cargo: {cargo}\r\n" +
        " company_truck: x\r\n trailer_variant: null\r\n trailer_definition: y\r\n units_count: 0\r\n" +
        " fill_ratio: 1\r\n trailer_place: 0\r\n}\r\n\r\n";

    private static readonly JobSpec Spec = new("dfh.berlin", "cargo.plumb_suppl", "trailer.scs_box",
        "trailer_def.scs.box.single_3.dryvan", "truck_x", 847);

    [Fact]
    public void Fills_empty_slot_and_keeps_existing_offer()
    {
        var doc = SiiDocument.Parse(Save);

        var id = JobInjector.Inject(doc, "company.volatile.tradeaux.budapest", Spec);

        Assert.Equal("_nameless.b", id);
        var job = doc.Find(id)!;
        Assert.Equal("\"dfh.berlin\"", job.Get("target"));
        Assert.Equal("cargo.plumb_suppl", job.Get("cargo"));
        Assert.Equal("847", job.Get("shortest_distance_km"));
        Assert.Equal("5320", job.Get("expiration_time")); // 1000 + 72h * 60
        Assert.Equal("\"stokes.wien\"", doc.Find("_nameless.a")!.Get("target"));
    }

    [Fact]
    public void Fails_when_no_empty_slot()
    {
        var doc = SiiDocument.Parse(Save);
        JobInjector.Inject(doc, "company.volatile.tradeaux.budapest", Spec);

        Assert.Throws<JobInjectionException>(() =>
            JobInjector.Inject(doc, "company.volatile.tradeaux.budapest", Spec));
    }

    private static GameCatalog Catalog(bool berlinAccepts)
    {
        var cat = new GameCatalog();
        var origin = new CompanyDef("tradeaux");
        origin.CargoOut.Add("plumb_suppl");
        var dest = new CompanyDef("dfh");
        if (berlinAccepts) dest.CargoIn.Add("plumb_suppl");
        cat.Companies["tradeaux"] = origin;
        cat.Companies["dfh"] = dest;
        return cat;
    }

    [Fact]
    public void Catalog_allows_valid_combination()
    {
        var doc = SiiDocument.Parse(Save);
        JobInjector.Inject(doc, "company.volatile.tradeaux.budapest", Spec, Catalog(berlinAccepts: true));
    }

    [Fact]
    public void Catalog_rejects_destination_that_does_not_accept_cargo()
    {
        var doc = SiiDocument.Parse(Save);
        var ex = Assert.Throws<JobInjectionException>(() =>
            JobInjector.Inject(doc, "company.volatile.tradeaux.budapest", Spec, Catalog(berlinAccepts: false)));
        Assert.Contains("não recebe", ex.Message);
        Assert.Equal("\"\"", doc.Find("_nameless.b")!.Get("target")); // nada foi alterado
    }

    [Fact]
    public void Fails_for_unknown_destination()
    {
        var doc = SiiDocument.Parse(Save);
        Assert.Throws<JobInjectionException>(() =>
            JobInjector.Inject(doc, "company.volatile.tradeaux.budapest", Spec with { Target = "x.paris" }));
    }
}
