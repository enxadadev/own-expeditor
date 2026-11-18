using OwnExpeditor.Core.Session;
using OwnExpeditor.Core.Tests;
using static OwnExpeditor.Core.Modules.Jobs.Tests.JobsSave;

namespace OwnExpeditor.Core.Modules.Jobs.Tests;

public class JobBoardTests
{
    private static JobRequest Fill(CompanyLocation to, string cargo = "used_plast", string? slot = null) =>
        new(Kaarfor, to, cargo, JobSlotMode.FillEmpty, FillSlotId: slot);

    [Fact]
    public void OriginCities_are_cities_with_usable_company()
    {
        // berlin: sem slots; wien: vaga livre mas não envia carga com reboque conhecido
        Assert.Equal(["budapest", "kosice"], Board(Open()).OriginCities());
    }

    [Fact]
    public void UsableOriginCompanies_equals_union_of_OriginCompanies_over_DestinationCities()
    {
        var b = Board(Open());
        foreach (var city in b.Cities())
        {
            var expected = b.DestinationCities(city).SelectMany(d => b.OriginCompanies(city, d)).Distinct()
                .OrderBy(l => l.Key, StringComparer.Ordinal).ToList();
            Assert.Equal(expected, b.UsableOriginCompanies(city));
        }
        Assert.Equal([Kaarfor], b.UsableOriginCompanies("budapest"));
        Assert.Empty(b.UsableOriginCompanies("berlin"));
    }

    [Fact]
    public void HasMatch_agrees_with_OriginCompanies()
    {
        var b = Board(Open());
        foreach (var from in b.Cities())
            foreach (var to in b.Cities())
                Assert.Equal(b.OriginCompanies(from, to).Count > 0, b.HasMatch(from, to));
    }

    [Fact]
    public void Cities_lists_every_city_of_the_save()
    {
        Assert.Equal(["berlin", "budapest", "kosice", "wien"], Board(Open()).Cities());
    }

    [Fact]
    public void DestinationCities_are_reachable_by_some_combination_and_exclude_origin()
    {
        Assert.Equal(["berlin", "kosice"], Board(Open()).DestinationCities("budapest"));
    }

    [Fact]
    public void DestinationCities_unknown_or_unusable_origin_is_empty()
    {
        var board = Board(Open());
        Assert.Empty(board.DestinationCities("wien"));
        Assert.Empty(board.DestinationCities("atlantis"));
    }

    [Fact]
    public void OriginCompanies_with_free_slot_or_offer_and_a_cargo_the_destination_accepts()
    {
        var board = Board(Open());
        Assert.Equal([Kaarfor], board.OriginCompanies("budapest", "berlin"));
        Assert.Empty(board.OriginCompanies("budapest", "wien"));
    }

    [Fact]
    public void OriginCompanies_qualifies_with_only_a_replaceable_offer()
    {
        // lkwlog.kosice não tem vaga livre, só a oferta c; envia empty_palet e a kaarfor (budapest) a recebe
        var board = Board(Open());
        Assert.Equal(0, board.FreeSlots(Lkwlog));
        Assert.Equal([Lkwlog], board.OriginCompanies("kosice", "budapest"));
    }

    [Fact]
    public void OriginCompanies_same_city_is_empty()
    {
        Assert.Empty(Board(Open()).OriginCompanies("budapest", "budapest"));
    }

    [Fact]
    public void HasMatch_tells_whether_the_pair_has_a_combination()
    {
        var board = Board(Open());
        Assert.True(board.HasMatch("budapest", "berlin"));
        Assert.False(board.HasMatch("berlin", "budapest"));
    }

    [Fact]
    public void Cargos_are_shipped_by_origin_accepted_at_destination_and_have_a_trailer()
    {
        var board = Board(Open());
        // kaarfor envia used_plast, empty_palet e ghost (sem reboque no save); kosice só recebe used_plast
        Assert.Equal(["used_plast"], board.Cargos(Kaarfor, "kosice"));
        Assert.Equal(["empty_palet", "used_plast"], board.Cargos(Kaarfor, "berlin"));
    }

    [Fact]
    public void DestinationCompanies_accept_the_cargo()
    {
        var board = Board(Open());
        Assert.Equal([Dfh], board.DestinationCompanies("berlin", "used_plast"));
        Assert.Empty(board.DestinationCompanies("kosice", "empty_palet"));
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
    public void Company_with_every_slot_pending_is_no_longer_an_origin()
    {
        var s = Open();
        s.Stage(JobPlanner.Plan(s.Original, Board(s), Fill(Dfh)));
        s.Stage(JobPlanner.Plan(s.Original, Board(s),
            new JobRequest(Kaarfor, Dfh, "empty_palet", JobSlotMode.ReplaceOffer, "_nameless.a")));

        Assert.Empty(Board(s).OriginCompanies("budapest", "berlin"));
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
        Assert.Equal(["used_plast"], board.Cargos(Kaarfor, "kosice"));
        Assert.Equal([Kaarfor], board.OriginCompanies("budapest", "kosice"));
        Assert.Equal(["kosice"], board.DestinationCities("budapest"));
    }

    [Fact]
    public void Catalog_and_save_rules_are_merged()
    {
        var data = new RulesGameData();
        data.In["dfh"] = ["used_plast"]; // o catálogo só diz que dfh recebe; quem envia vem do save (oferta a)
        var board = Board(Open(data: data));

        Assert.Equal(["berlin", "kosice"], board.DestinationCities("budapest"));
    }

    [Fact]
    public void Board_never_throws_on_odd_data()
    {
        var text = "SiiNunit\r\n{\r\ncompany : company.volatile.weird {\r\n job_offer: 1\r\n job_offer[0]: _nameless.zz\r\n}\r\n\r\n" +
                   "company : company.volatile.x.y {\r\n job_offer: 1\r\n job_offer[0]: _nameless.missing\r\n}\r\n\r\n}\r\n";
        var board = Board(Open(text));
        Assert.Empty(board.OriginCities());
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
