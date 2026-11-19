using OwnExpeditor.Core.Session;
using static OwnExpeditor.Core.Modules.Jobs.Tests.CountrySave;

namespace OwnExpeditor.Core.Modules.Jobs.Tests;

public class JobSelectionRulesTests
{
    private static JobSlotChoice Free => JobSlotChoice.Free;

    /// <summary>Seleção completa e válida: hu/budapest/kaarfor, vaga livre → de/berlin/dfh, used_plast.</summary>
    private static JobSelection Full() => new("hu", "budapest", Budapest, Free, "de", "berlin", Berlin, "used_plast");

    private static JobSelectionResult Revalidate(JobSelection s, bool locked = false) =>
        JobSelectionRules.Revalidate(Board(), s, locked);

    // ---- Revalidate: o que fica e a abertura

    [Fact]
    public void Revalidate_empty_selection_with_several_options_changes_nothing()
    {
        var result = Revalidate(new JobSelection());

        Assert.Equal(new JobSelection(), result.Selection);
        Assert.Empty(result.Cleared);
        Assert.Empty(result.AutoPicked);
    }

    [Fact]
    public void Revalidate_valid_selection_is_kept()
    {
        var result = Revalidate(Full());

        Assert.Equal(Full(), result.Selection);
        Assert.Empty(result.Cleared);
        Assert.Empty(result.AutoPicked);
    }

    [Fact]
    public void Revalidate_single_departure_country_is_picked_on_open()
    {
        var board = JobsSave.Board(JobsSave.Open(data: SingleCountryData()));

        var result = JobSelectionRules.Revalidate(board, new JobSelection());

        Assert.Equal("hu", result.Selection.FromCountry);
        Assert.Equal([JobField.FromCountry], result.AutoPicked);
    }

    [Fact]
    public void Revalidate_single_options_cascade_down_the_whole_chain()
    {
        var board = JobsSave.Board(JobsSave.Open(data: SingleCountryData()));

        var result = JobSelectionRules.Revalidate(board, new JobSelection(FromCountry: "hu", FromCity: "kosice", ToCity: "budapest"));

        // berlin também é hu: a cidade de chegada tem 2 opções, então fica a escolhida pelo usuário
        Assert.Equal(new JobSelection("hu", "kosice", Kosice, JobSlotChoice.Replace("_nameless.c"), "hu", "budapest", Budapest,
            "empty_palet"), result.Selection);
        JobField[] expected = [JobField.FromCompany, JobField.Slot, JobField.ToCountry, JobField.ToCompany, JobField.Cargo];
        Assert.Equal(expected, result.AutoPicked);
        Assert.Empty(result.Cleared);
    }

    // ---- Revalidate: tabela de flows.md §2

    [Fact]
    public void Revalidate_departure_country_changed_clears_city_of_another_country_and_what_follows()
    {
        var result = Revalidate(new JobSelection("sk", "budapest", Budapest, Free));

        Assert.Equal(new JobSelection(FromCountry: "sk"), result.Selection);
        Assert.Equal([JobField.FromCity, JobField.FromCompany, JobField.Slot], result.Cleared);
    }

    [Fact]
    public void Revalidate_departure_city_changed_clears_company_missing_in_new_city()
    {
        var result = Revalidate(new JobSelection("hu", "budapest", Gyor, Free));

        Assert.Equal(new JobSelection("hu", "budapest"), result.Selection);
        Assert.Equal([JobField.FromCompany, JobField.Slot], result.Cleared);
    }

    [Fact]
    public void Revalidate_departure_city_with_a_single_company_picks_it()
    {
        var result = Revalidate(new JobSelection("hu", "gyor"));

        Assert.Equal(Gyor, result.Selection.FromCompany);
        Assert.Equal(Free, result.Selection.Slot);
        Assert.Contains(JobField.FromCompany, result.AutoPicked);
    }

    [Fact]
    public void Revalidate_departure_company_changed_keeps_free_slot_and_revalidates_arrival_and_cargo()
    {
        // lkwlog.budapest só envia empty_palet; dfh o aceita, então a chegada fica e used_plast sai
        var result = Revalidate(Full() with { FromCompany = LkwlogBudapest });

        Assert.Equal(Free, result.Selection.Slot);
        Assert.Equal(("de", "berlin", Berlin), (result.Selection.ToCountry, result.Selection.ToCity, result.Selection.ToCompany));
        Assert.Equal("empty_palet", result.Selection.Cargo);
        Assert.Equal([JobField.Cargo], result.Cleared);
        Assert.Equal([JobField.Cargo], result.AutoPicked);
    }

    [Fact]
    public void Revalidate_departure_company_that_no_longer_reaches_the_arrival_clears_it()
    {
        // lkwlog.budapest só envia empty_palet e ninguém em sk o aceita
        var selection = new JobSelection("hu", "budapest", LkwlogBudapest, Free, "sk", "kosice", Kosice, "used_plast");

        var result = Revalidate(selection);

        Assert.Equal(new JobSelection("hu", "budapest", LkwlogBudapest, Free), result.Selection);
        Assert.Equal([JobField.ToCountry, JobField.ToCity, JobField.ToCompany, JobField.Cargo], result.Cleared);
    }

    [Fact]
    public void Revalidate_slot_changed_leaves_the_rest_alone()
    {
        var selection = Full() with { Slot = JobSlotChoice.Replace("_nameless.a") };

        var result = Revalidate(selection);

        Assert.Equal(selection, result.Selection);
        Assert.Empty(result.Cleared);
    }

    [Fact]
    public void Revalidate_invalid_slot_falls_back_to_the_free_slot()
    {
        var selection = new JobSelection("hu", "gyor", Gyor, JobSlotChoice.Replace("_nameless.zz"));

        var result = Revalidate(selection);

        Assert.Equal(Free, result.Selection.Slot);
        Assert.Contains(JobField.Slot, result.Cleared);
    }

    [Fact]
    public void Revalidate_arrival_country_changed_clears_city_company_and_cargo()
    {
        var result = Revalidate(Full() with { ToCountry = "sk" });

        Assert.Equal(new JobSelection("hu", "budapest", Budapest, Free, "sk"), result.Selection);
        Assert.Equal([JobField.ToCity, JobField.ToCompany, JobField.Cargo], result.Cleared);
    }

    [Fact]
    public void Revalidate_arrival_city_changed_picks_the_single_company_and_keeps_valid_cargo()
    {
        var result = Revalidate(Full() with { ToCountry = "sk", ToCity = "bratislava" });

        Assert.Equal(Bratislava, result.Selection.ToCompany);
        Assert.Equal("used_plast", result.Selection.Cargo); // lkwlog.bratislava aceita used_plast
        Assert.Equal([JobField.ToCompany], result.Cleared);
        Assert.Equal([JobField.ToCompany], result.AutoPicked);
    }

    [Fact]
    public void Revalidate_arrival_company_changed_picks_the_single_remaining_cargo()
    {
        // lkwlog.kosice só aceita used_plast
        var selection = Full() with { ToCountry = "sk", ToCity = "kosice", ToCompany = Kosice, Cargo = "empty_palet" };

        var result = Revalidate(selection);

        Assert.Equal("used_plast", result.Selection.Cargo);
        Assert.Equal([JobField.Cargo], result.Cleared);
        Assert.Equal([JobField.Cargo], result.AutoPicked);
    }

    [Fact]
    public void Revalidate_cargo_not_accepted_with_several_options_is_cleared_and_left_empty()
    {
        var result = Revalidate(Full() with { Cargo = "ghost" });

        Assert.Null(result.Selection.Cargo);
        Assert.Equal([JobField.Cargo], result.Cleared);
        Assert.Empty(result.AutoPicked);
    }

    // ---- Revalidate: Vaga

    [Fact]
    public void Revalidate_company_with_free_slot_and_offer_defaults_to_the_free_slot()
    {
        var result = Revalidate(new JobSelection("hu", "budapest", Budapest));

        Assert.Equal(Free, result.Selection.Slot);
        Assert.Contains(JobField.Slot, result.AutoPicked);
    }

    [Fact]
    public void Revalidate_company_with_only_offers_and_two_or_more_leaves_slot_empty()
    {
        var result = Revalidate(new JobSelection("sk", "kosice", Kosice));

        Assert.Null(result.Selection.Slot);
    }

    [Fact]
    public void Revalidate_free_slot_taken_by_pending_picks_the_single_remaining_offer()
    {
        var s = Open();
        s.Stage(JobPlanner.Plan(s.Original, Board(s), new JobRequest(Budapest, Berlin, "used_plast", JobSlotMode.FillEmpty)));

        var result = JobSelectionRules.Revalidate(Board(s), new JobSelection("hu", "budapest", Budapest, Free));

        Assert.Equal(JobSlotChoice.Replace("_nameless.a"), result.Selection.Slot);
    }

    // ---- Revalidate: sem empresa de saída e travado

    [Fact]
    public void Revalidate_without_departure_company_does_not_touch_arrival()
    {
        var selection = new JobSelection("hu", "budapest", null, null, "zz", "nowhere", new CompanyLocation("x", "nowhere"), "none");

        var result = Revalidate(selection);

        Assert.Equal(selection, result.Selection);
        Assert.Empty(result.Cleared);
    }

    [Fact]
    public void Revalidate_departure_locked_keeps_departure_fields_even_if_invalid()
    {
        var selection = new JobSelection("xx", "nowhere", Budapest, JobSlotChoice.Replace("bogus"), "zz");

        var result = Revalidate(selection, locked: true);

        Assert.Equal(("xx", "nowhere", Budapest, JobSlotChoice.Replace("bogus")),
            (result.Selection.FromCountry, result.Selection.FromCity, result.Selection.FromCompany, result.Selection.Slot));
    }

    [Fact]
    public void Revalidate_departure_locked_still_revalidates_arrival()
    {
        var selection = new JobSelection("xx", "nowhere", Budapest, Free, "zz");

        var result = Revalidate(selection, locked: true);

        Assert.Null(result.Selection.ToCountry);
        Assert.Equal([JobField.ToCountry], result.Cleared);
    }

    // ---- Swap

    [Fact]
    public void Swap_exchanges_country_and_city_and_clears_companies_slot_and_cargo()
    {
        var selection = new JobSelection("sk", "bratislava", Bratislava, Free, "hu", "budapest", Budapest, "used_plast");

        var result = JobSelectionRules.Swap(Board(), selection);

        Assert.Equal(new JobSelection("hu", "budapest", ToCountry: "sk", ToCity: "bratislava"), result.Selection);
    }

    [Fact]
    public void Swap_new_departure_without_slot_or_offer_clears_departure_country_and_city()
    {
        var selection = new JobSelection("hu", "budapest", Budapest, Free, "de", "berlin", Berlin, "used_plast");

        var result = JobSelectionRules.Swap(Board(), selection);

        Assert.Equal(new JobSelection(ToCountry: "hu", ToCity: "budapest"), result.Selection);
        Assert.Contains(JobField.FromCountry, result.Cleared);
        Assert.Contains(JobField.FromCity, result.Cleared);
    }

    // ---- NextJob

    [Fact]
    public void NextJob_clears_slot_arrival_company_and_cargo_and_revalidates()
    {
        var selection = Full() with { Slot = JobSlotChoice.Replace("_nameless.a") };

        var result = JobSelectionRules.NextJob(Board(), selection);

        // berlin tem só a dfh (escolhida sozinha); duas cargas possíveis, então Carga fica vazia
        Assert.Equal(new JobSelection("hu", "budapest", Budapest, Free, "de", "berlin", Berlin), result.Selection);
    }

    [Fact]
    public void NextJob_after_free_slot_used_picks_the_remaining_offer()
    {
        var s = Open();
        s.Stage(JobPlanner.Plan(s.Original, Board(s), new JobRequest(Budapest, Berlin, "used_plast", JobSlotMode.FillEmpty)));

        var result = JobSelectionRules.NextJob(Board(s), Full());

        Assert.Equal(JobSlotChoice.Replace("_nameless.a"), result.Selection.Slot);
        Assert.Null(result.Selection.Cargo);
    }

    // ---- Missing

    [Fact]
    public void Missing_empty_selection_with_countries_lists_the_chain_without_slot()
    {
        Assert.Equal([JobField.FromCountry, JobField.FromCity, JobField.FromCompany, JobField.ToCountry, JobField.ToCity,
            JobField.ToCompany, JobField.Cargo], JobSelectionRules.Missing(Board(), new JobSelection()));
    }

    [Fact]
    public void Missing_without_countries_skips_the_country_fields()
    {
        var board = JobsSave.Board(JobsSave.Open());

        Assert.Equal([JobField.FromCity, JobField.FromCompany, JobField.ToCity, JobField.ToCompany, JobField.Cargo],
            JobSelectionRules.Missing(board, new JobSelection()));
    }

    [Fact]
    public void Missing_company_with_two_slot_options_and_no_slot_lists_slot_in_chain_order()
    {
        var selection = new JobSelection("hu", "budapest", Budapest);

        Assert.Equal([JobField.Slot, JobField.ToCountry, JobField.ToCity, JobField.ToCompany, JobField.Cargo],
            JobSelectionRules.Missing(Board(), selection));
    }

    [Fact]
    public void Missing_company_with_a_single_slot_option_does_not_list_slot()
    {
        var selection = new JobSelection("hu", "gyor", Gyor);

        Assert.DoesNotContain(JobField.Slot, JobSelectionRules.Missing(Board(), selection));
    }

    [Fact]
    public void Missing_complete_selection_is_empty()
    {
        Assert.Empty(JobSelectionRules.Missing(Board(), Full()));
    }

    // ---- SlotOptionCount

    [Theory]
    [InlineData("kaarfor", "budapest", 2)] // vaga livre + oferta a
    [InlineData("kaarfor", "gyor", 1)]     // só vaga livre
    [InlineData("lkwlog", "kosice", 2)]    // ofertas c e i
    [InlineData("dfh", "berlin", 0)]
    [InlineData("nobody", "nowhere", 0)]
    public void SlotOptionCount_is_free_slot_plus_replaceable_offers(string company, string city, int expected)
    {
        Assert.Equal(expected, JobSelectionRules.SlotOptionCount(Board(), new CompanyLocation(company, city)));
    }

    // ---- ToRequest

    [Fact]
    public void ToRequest_complete_free_slot_builds_a_fill_request()
    {
        var request = JobSelectionRules.ToRequest(Full(), 48);

        Assert.Equal(new JobRequest(Budapest, Berlin, "used_plast", JobSlotMode.FillEmpty, ExpirationHours: 48), request);
    }

    [Fact]
    public void ToRequest_free_slot_with_fill_slot_id_keeps_it()
    {
        var request = JobSelectionRules.ToRequest(Full(), 72, "_nameless.b");

        Assert.Equal("_nameless.b", request!.FillSlotId);
    }

    [Fact]
    public void ToRequest_replace_slot_takes_mode_and_offer_and_ignores_fill_slot_id()
    {
        var selection = Full() with { Slot = JobSlotChoice.Replace("_nameless.a") };

        var request = JobSelectionRules.ToRequest(selection, 72, "_nameless.b")!;

        Assert.Equal(JobSlotMode.ReplaceOffer, request.Mode);
        Assert.Equal("_nameless.a", request.ReplaceOfferId);
        Assert.Null(request.FillSlotId);
    }

    [Fact]
    public void ToRequest_missing_cargo_is_null()
    {
        Assert.Null(JobSelectionRules.ToRequest(Full() with { Cargo = null }, 72));
    }

    [Fact]
    public void ToRequest_missing_slot_is_null()
    {
        Assert.Null(JobSelectionRules.ToRequest(Full() with { Slot = null }, 72));
    }

    [Fact]
    public void ToRequest_missing_arrival_company_is_null()
    {
        Assert.Null(JobSelectionRules.ToRequest(Full() with { ToCompany = null }, 72));
    }

    // ---- edição travada

    [Fact]
    public void Edit_locked_keeps_the_used_free_slot_and_the_request_carries_its_id()
    {
        // b já está numa pendente (o slot não é mais livre), mas a edição o mantém
        var s = Open();
        s.Stage(JobPlanner.Plan(s.Original, Board(s), new JobRequest(Budapest, Berlin, "used_plast", JobSlotMode.FillEmpty)));

        var result = JobSelectionRules.Revalidate(Board(s), Full(), departureLocked: true);
        var request = JobSelectionRules.ToRequest(result.Selection, 72, "_nameless.b");

        Assert.Equal(Free, result.Selection.Slot);
        Assert.Equal("_nameless.b", request!.FillSlotId);
    }
}
