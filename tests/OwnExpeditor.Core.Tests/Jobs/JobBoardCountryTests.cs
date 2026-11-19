using OwnExpeditor.Core.Session;
using static OwnExpeditor.Core.Modules.Jobs.Tests.CountrySave;

namespace OwnExpeditor.Core.Modules.Jobs.Tests;

public class JobBoardCountryTests
{
    // ---- países

    [Fact]
    public void HasCountries_some_city_has_country_is_true()
    {
        Assert.True(Board().HasCountries);
    }

    [Fact]
    public void HasCountries_no_city_has_country_is_false()
    {
        Assert.False(JobsSave.Board(JobsSave.Open()).HasCountries);
    }

    [Fact]
    public void CountryOf_known_city_returns_its_country()
    {
        Assert.Equal("hu", Board().CountryOf("budapest"));
    }

    [Fact]
    public void CountryOf_city_without_country_is_null()
    {
        Assert.Null(Board().CountryOf("praha"));
    }

    [Fact]
    public void DepartureCountries_lists_countries_with_departure_cities_and_others_last()
    {
        var result = Board().DepartureCountries();

        Assert.Equal([new CountryInfo("hu", 2), new CountryInfo("sk", 2), new CountryInfo(JobBoard.OtherCountry, 1)], result);
    }

    [Fact]
    public void DepartureCountries_skips_country_without_departure_city()
    {
        // de: berlin sem slots
        Assert.DoesNotContain(Board().DepartureCountries(), c => c.Country == "de");
    }

    // ---- cidades de saída

    [Fact]
    public void DepartureCities_null_country_lists_all_departure_cities()
    {
        var cities = Board().DepartureCities(null).Select(c => c.City);

        Assert.Equal(["bratislava", "budapest", "gyor", "kosice", "praha"], cities);
    }

    [Fact]
    public void DepartureCities_country_lists_only_its_cities()
    {
        Assert.Equal(["budapest", "gyor"], Board().DepartureCities("hu").Select(c => c.City));
    }

    [Fact]
    public void DepartureCities_other_country_lists_cities_without_country()
    {
        Assert.Equal(["praha"], Board().DepartureCities(JobBoard.OtherCountry).Select(c => c.City));
    }

    [Fact]
    public void DepartureCities_unknown_country_is_empty()
    {
        Assert.Empty(Board().DepartureCities("zz"));
    }

    [Fact]
    public void DepartureCities_carry_country_slots_and_offers()
    {
        var budapest = Board().DepartureCities("hu").Single(c => c.City == "budapest");

        Assert.Equal("hu", budapest.Country);
        Assert.Equal(2, budapest.Companies);
        Assert.Equal(2, budapest.FreeSlots); // kaarfor.budapest + lkwlog.budapest
        Assert.Equal(1, budapest.Offers);
        Assert.Equal(0, budapest.PendingSlots);
    }

    [Fact]
    public void DepartureCities_city_with_only_pending_changes_stays_listed()
    {
        var s = Open();
        s.Stage(JobPlanner.Plan(s.Original, Board(s), new JobRequest(Praha, Gyor, "empty_palet", JobSlotMode.FillEmpty)));

        var board = Board(s);
        var praha = board.DepartureCities(null).Single(c => c.City == "praha");

        Assert.Equal(1, praha.PendingSlots);
        Assert.Equal(0, praha.FreeSlots);
        Assert.Empty(board.DepartureCompanies("praha"));
    }

    [Fact]
    public void DepartureCities_city_without_slots_or_pendings_is_not_listed()
    {
        var cities = Board().DepartureCities(null).Select(c => c.City);

        Assert.DoesNotContain("berlin", cities); // sem slots
        Assert.DoesNotContain("wien", cities);   // vaga livre, mas nenhuma carga com reboque
    }

    [Theory]
    [InlineData("gyor", 1)]
    [InlineData("kosice", 1)]
    public void DepartureCities_companies_counts_departure_companies(string city, int expected)
    {
        Assert.Equal(expected, Board().DepartureCities(null).Single(c => c.City == city).Companies);
    }

    // ---- empresas de saída

    private static string OwnCityOnlyText(bool withGyor) =>
        "SiiNunit\r\n{\r\neconomy : _nameless.1 {\r\n game_time: 1000\r\n}\r\n\r\n" +
        JobsSave.Company("kaarfor.budapest", "_nameless.a", "_nameless.b") +
        (withGyor ? JobsSave.Company("kaarfor.gyor") : "") +
        JobsSave.Job("_nameless.a", "\"kaarfor.budapest\"", "cargo.used_plast", 1, "trailer.curtain", "trailer_def.curtain", "truck_a", 33) +
        JobsSave.Job("_nameless.b", "\"\"", "null", 0, "null", "null", "null", 0, "nil") +
        "}\r\n";

    [Fact]
    public void DepartureCompanies_cargo_accepted_only_in_its_own_city_does_not_qualify()
    {
        var board = JobsSave.Board(JobsSave.Open(OwnCityOnlyText(withGyor: false), new RulesGameData()));

        Assert.Empty(board.DepartureCompanies("budapest"));
        Assert.Empty(board.DepartureCities(null));
    }

    [Fact]
    public void DepartureCompanies_same_cargo_accepted_by_a_company_of_another_city_qualifies()
    {
        var board = JobsSave.Board(JobsSave.Open(OwnCityOnlyText(withGyor: true), new RulesGameData()));

        Assert.Equal([Budapest], board.DepartureCompanies("budapest"));
    }

    [Fact]
    public void DepartureCompanies_city_lists_companies_with_slot_and_a_cargo_accepted_elsewhere()
    {
        Assert.Equal([Budapest, LkwlogBudapest], Board().DepartureCompanies("budapest"));
    }

    [Fact]
    public void DepartureCompanies_company_with_only_offers_qualifies()
    {
        Assert.Equal([Kosice], Board().DepartureCompanies("kosice"));
    }

    [Theory]
    [InlineData("berlin")]
    [InlineData("wien")]
    [InlineData("atlantis")]
    public void DepartureCompanies_city_without_usable_company_or_unknown_is_empty(string city)
    {
        Assert.Empty(Board().DepartureCompanies(city));
    }

    // ---- chegada

    [Fact]
    public void ArrivalCountries_lists_countries_of_companies_accepting_a_cargo_the_origin_sends()
    {
        var result = Board().ArrivalCountries(Budapest);

        Assert.Equal([new CountryInfo("de", 1), new CountryInfo("hu", 1), new CountryInfo("sk", 2),
            new CountryInfo(JobBoard.OtherCountry, 1)], result);
    }

    [Fact]
    public void ArrivalCities_null_country_never_includes_the_origin_city()
    {
        Assert.Equal(["berlin", "bratislava", "gyor", "kosice", "praha"], Board().ArrivalCities(Budapest, null));
    }

    [Fact]
    public void ArrivalCities_country_of_the_origin_excludes_the_origin_city()
    {
        Assert.Equal(["gyor"], Board().ArrivalCities(Budapest, "hu"));
    }

    [Fact]
    public void ArrivalCities_country_lists_only_its_cities()
    {
        Assert.Equal(["bratislava", "kosice"], Board().ArrivalCities(Budapest, "sk"));
    }

    [Fact]
    public void ArrivalCities_other_country_lists_cities_without_country()
    {
        Assert.Equal(["praha"], Board().ArrivalCities(Budapest, JobBoard.OtherCountry));
    }

    [Fact]
    public void ArrivalCities_unknown_country_or_origin_is_empty()
    {
        var board = Board();

        Assert.Empty(board.ArrivalCities(Budapest, "zz"));
        Assert.Empty(board.ArrivalCities(new CompanyLocation("nobody", "nowhere"), null));
    }

    [Fact]
    public void ArrivalCompanies_lists_companies_accepting_a_cargo_the_origin_sends()
    {
        var board = Board();

        Assert.Equal([Berlin], board.ArrivalCompanies(Budapest, "berlin"));
        Assert.Equal([Kosice], board.ArrivalCompanies(Budapest, "kosice"));
    }

    [Theory]
    [InlineData("budapest")] // cidade de saída
    [InlineData("wien")]     // ninguém aceita
    [InlineData("atlantis")]
    public void ArrivalCompanies_origin_city_or_no_acceptor_or_unknown_is_empty(string city)
    {
        Assert.Empty(Board().ArrivalCompanies(Budapest, city));
    }

    // ---- carga

    [Fact]
    public void Cargos_is_what_the_origin_sends_intersected_with_what_the_arrival_accepts()
    {
        var board = Board();

        Assert.Equal(["empty_palet", "used_plast"], board.Cargos(Budapest, Berlin)); // ghost: sem reboque conhecido
        Assert.Equal(["used_plast"], board.Cargos(Budapest, Kosice));
        Assert.Equal(["empty_palet"], board.Cargos(Kosice, Budapest));
    }

    [Fact]
    public void Cargos_unknown_company_or_nothing_in_common_is_empty()
    {
        var board = Board();

        Assert.Empty(board.Cargos(new CompanyLocation("nobody", "nowhere"), Berlin));
        Assert.Empty(board.Cargos(Budapest, new CompanyLocation("tradeaux", "wien")));
    }
}
