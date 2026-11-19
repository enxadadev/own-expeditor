using OwnExpeditor.Core.Session;
using OwnExpeditor.Core.Tests;
using static OwnExpeditor.Core.Modules.Jobs.Tests.JobsSave;

namespace OwnExpeditor.Core.Modules.Jobs.Tests;

public class JobBoardTests
{
    private static JobRequest Fill(CompanyLocation to, string cargo = "used_plast", string? slot = null) =>
        new(Kaarfor, to, cargo, JobSlotMode.FillEmpty, FillSlotId: slot);

    [Fact]
    public void DepartureCities_are_cities_with_usable_company()
    {
        // berlin: sem slots; wien: vaga livre mas não envia carga com reboque conhecido
        Assert.Equal(["budapest", "kosice"], Board(Open()).DepartureCities(null).Select(c => c.City));
    }

    [Fact]
    public void DepartureCompanies_equal_brute_force_and_agree_with_DepartureCities_and_Cargos()
    {
        var b = Board(Open());
        var cities = b.DepartureCities(null).ToDictionary(c => c.City);
        foreach (var city in b.DepartureCities(null).Select(c => c.City).Concat(["wien", "berlin"]).Distinct())
        {
            var companies = b.DepartureCompanies(city);
            foreach (var from in companies)
            {
                var hasCargo = b.ArrivalCities(from, null)
                    .SelectMany(to => b.ArrivalCompanies(from, to)).Any(to => b.Cargos(from, to).Count > 0);
                Assert.True(hasCargo);
                Assert.True(b.FreeSlots(from) + b.ReplaceableOffers(from).Count > 0);
            }
            if (cities.TryGetValue(city, out var info))
            {
                Assert.Equal(companies.Count, info.Companies);
                Assert.Equal(companies.Sum(c => b.FreeSlots(c)), info.FreeSlots);
                Assert.Equal(companies.Sum(c => b.ReplaceableOffers(c).Count), info.Offers);
            }
        }
    }

    [Fact]
    public void ArrivalCities_are_reachable_by_some_combination_and_exclude_origin()
    {
        Assert.Equal(["berlin", "kosice"], Board(Open()).ArrivalCities(Kaarfor, null));
    }

    [Fact]
    public void DepartureCompanies_with_free_slot_or_offer_and_a_cargo_some_arrival_accepts()
    {
        var board = Board(Open());
        Assert.Equal([Kaarfor], board.DepartureCompanies("budapest"));
        Assert.Empty(board.DepartureCompanies("wien"));
    }

    [Fact]
    public void DepartureCompanies_qualifies_with_only_a_replaceable_offer()
    {
        // lkwlog.kosice não tem vaga livre, só a oferta c; envia empty_palet e a kaarfor (budapest) a recebe
        var board = Board(Open());
        Assert.Equal(0, board.FreeSlots(Lkwlog));
        Assert.Equal([Lkwlog], board.DepartureCompanies("kosice"));
    }

    [Fact]
    public void Cargos_are_shipped_by_origin_accepted_at_destination_and_have_a_trailer()
    {
        var board = Board(Open());
        // kaarfor envia used_plast, empty_palet e ghost (sem reboque no save); kosice só recebe used_plast
        Assert.Equal(["used_plast"], board.Cargos(Kaarfor, Lkwlog));
        Assert.Equal(["empty_palet", "used_plast"], board.Cargos(Kaarfor, Dfh));
    }

    [Fact]
    public void ArrivalCompanies_accept_a_cargo_the_origin_sends()
    {
        var board = Board(Open());
        Assert.Equal([Dfh], board.ArrivalCompanies(Kaarfor, "berlin"));
        Assert.Equal([Lkwlog], board.ArrivalCompanies(Kaarfor, "kosice"));
        Assert.Empty(board.ArrivalCompanies(Kaarfor, "budapest"));
    }

    [Fact]
    public void FreeSlots_and_ReplaceableOffers_describe_the_company()
    {
        var board = Board(Open());
        Assert.Equal(1, board.FreeSlots(Kaarfor));
        Assert.Equal(["_nameless.b"], board.FreeSlotIds(Kaarfor));
        var offer = Assert.Single(board.ReplaceableOffers(Kaarfor));
        Assert.Equal(new ExistingOffer("_nameless.a", Lkwlog, "used_plast", 226, TimeSpan.FromMinutes(500)), offer);
        Assert.Empty(board.ReplaceableOffers(Dfh));
    }

    [Fact]
    public void Pending_change_takes_the_slot_off_the_free_count()
    {
        var s = Open();
        s.Stage(JobPlanner.Plan(s.Original, Board(s), Fill(Dfh)));

        var board = Board(s);
        Assert.Equal(0, board.FreeSlots(Kaarfor));
        Assert.Single(board.ReplaceableOffers(Kaarfor)); // a continua substituível; b (pendente) não aparece
    }

    [Fact]
    public void Pending_replacement_takes_the_offer_off_the_replaceable_list()
    {
        var s = Open();
        s.Stage(JobPlanner.Plan(s.Original, Board(s),
            new JobRequest(Kaarfor, Dfh, "empty_palet", JobSlotMode.ReplaceOffer, "_nameless.a")));

        Assert.Empty(Board(s).ReplaceableOffers(Kaarfor));
    }

    [Fact]
    public void Company_with_every_slot_pending_is_no_longer_a_departure()
    {
        var s = Open();
        s.Stage(JobPlanner.Plan(s.Original, Board(s), Fill(Dfh)));
        s.Stage(JobPlanner.Plan(s.Original, Board(s),
            new JobRequest(Kaarfor, Dfh, "empty_palet", JobSlotMode.ReplaceOffer, "_nameless.a")));

        Assert.Empty(Board(s).DepartureCompanies("budapest"));
    }

    [Fact]
    public void Template_is_copied_from_a_save_job_with_the_cargo()
    {
        var board = Board(Open());
        Assert.Equal(new JobTemplate("trailer.curtain", "trailer_def.curtain", "truck_a", 33), board.Template("used_plast"));
        Assert.Null(board.Template("ghost"));
    }

    [Fact]
    public void Without_catalog_rules_come_from_the_save()
    {
        var s = Open(data: new FakeGameData());
        var board = Board(s);

        // a oferta a revela: kaarfor envia used_plast e lkwlog recebe
        Assert.Equal(["used_plast"], board.Cargos(Kaarfor, Lkwlog));
        Assert.Equal([Kaarfor], board.DepartureCompanies("budapest"));
        Assert.Equal(["kosice"], board.ArrivalCities(Kaarfor, null));
    }

    [Fact]
    public void Catalog_and_save_rules_are_merged()
    {
        var data = new RulesGameData();
        data.In["dfh"] = ["used_plast"]; // o catálogo só diz que dfh recebe; quem envia vem do save (oferta a)
        var board = Board(Open(data: data));

        Assert.Equal(["berlin", "kosice"], board.ArrivalCities(Kaarfor, null));
    }

    [Fact]
    public void Board_never_throws_on_odd_data()
    {
        var text = "SiiNunit\r\n{\r\ncompany : company.volatile.weird {\r\n job_offer: 1\r\n job_offer[0]: _nameless.zz\r\n}\r\n\r\n" +
                   "company : company.volatile.x.y {\r\n job_offer: 1\r\n job_offer[0]: _nameless.missing\r\n}\r\n\r\n}\r\n";
        var board = Board(Open(text));
        Assert.Empty(board.DepartureCities(null));
        Assert.Equal(0, board.FreeSlots(new CompanyLocation("x", "y")));
    }

    [Fact]
    public void Board_is_read_only_the_session_is_untouched()
    {
        var s = Open();
        Board(s).ReplaceableOffers(Kaarfor);
        Assert.False(s.HasChanges);
    }

    [Fact]
    public void Pending_job_does_not_teach_distance_or_rules()
    {
        var s = Open();
        s.Stage(JobPlanner.Plan(s.Original, Board(s), Fill(Dfh)));

        var board = Board(s);
        Assert.Equal(DistanceSource.Estimated, board.Distance("budapest", "berlin").Source);
        Assert.DoesNotContain(CityPairOf("budapest", "berlin"), board.DistancesFromSave.Keys);
    }

    [Fact]
    public void Replacing_the_only_offer_of_a_cargo_keeps_its_template_and_distance()
    {
        var s = Open();
        s.Stage(JobPlanner.Plan(s.Original, Board(s),
            new JobRequest(Kaarfor, Dfh, "empty_palet", JobSlotMode.ReplaceOffer, "_nameless.a")));

        var board = Board(s);
        Assert.NotNull(board.Template("used_plast"));
        Assert.Equal(new RouteDistance(226, DistanceSource.Known), board.Distance("budapest", "kosice"));
    }

    private static OwnExpeditor.Domain.Games.CityPair CityPairOf(string a, string b) => OwnExpeditor.Domain.Games.CityPair.Of(a, b);

    [Theory]
    [InlineData(" trailer_definition: trailer_def.curtain\r\n")]
    [InlineData(" company_truck: truck_a\r\n")]
    public void Job_without_trailer_definition_or_truck_teaches_no_template(string removed)
    {
        var board = Board(Open(JobsSave.Text().Replace(removed, "", StringComparison.Ordinal)));
        Assert.Null(board.Template("used_plast"));
        Assert.Null(board.Template("kaarfor", "used_plast"));
    }
}
