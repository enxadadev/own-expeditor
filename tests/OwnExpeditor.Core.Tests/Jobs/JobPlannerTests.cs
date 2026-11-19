using OwnExpeditor.Core.Session;
using OwnExpeditor.Core.Text;
using OwnExpeditor.Domain.Games;
using static OwnExpeditor.Core.Modules.Jobs.Tests.JobsSave;

namespace OwnExpeditor.Core.Modules.Jobs.Tests;

public class JobPlannerTests
{
    private static JobRequest Fill(CompanyLocation? to = null, string cargo = "used_plast", string? slot = null, int hours = 72) =>
        new(Kaarfor, to ?? Dfh, cargo, JobSlotMode.FillEmpty, FillSlotId: slot, ExpirationHours: hours);

    private static JobRequest Replace(string? offer = "_nameless.a", string cargo = "empty_palet") =>
        new(Kaarfor, Dfh, cargo, JobSlotMode.ReplaceOffer, offer);

    private static ChangeSet Plan(IEditSession s, JobRequest r) => JobPlanner.Plan(s.Original, Board(s), r);

    private static string New(ChangeSet c, string attribute) =>
        c.Patches.OfType<SetAttribute>().Single(p => p.Attribute == attribute).NewValue;

    private static string Old(ChangeSet c, string attribute) =>
        c.Patches.OfType<SetAttribute>().Single(p => p.Attribute == attribute).OldValue;

    private static CoreException Invalid(Action act, string reasonKey)
    {
        var e = Assert.Throws<CoreException>(act);
        Assert.Equal(CoreError.InvalidEdit, e.Code);
        Assert.Equal(reasonKey, e.Reason?.Key);
        return e;
    }

    // ---- vaga livre

    [Fact]
    public void FillEmpty_uses_the_free_slot_with_one_key_per_slot()
    {
        var change = Plan(Open(), Fill());

        Assert.Equal("jobs", change.ModuleId);
        Assert.Equal("jobs:slot:_nameless.b", change.Key);
        Assert.All(change.Patches, p => Assert.Equal("_nameless.b", p.UnitId));
        Assert.All(change.Patches, p => Assert.IsType<SetAttribute>(p));
    }

    [Fact]
    public void FillEmpty_writes_every_job_field_from_template_and_request()
    {
        var change = Plan(Open(), Fill(hours: 48));

        Assert.Equal("\"dfh.berlin\"", New(change, "target"));
        Assert.Equal("cargo.used_plast", New(change, "cargo"));
        Assert.Equal((1000 + 48 * 60).ToString(), New(change, "expiration_time"));
        Assert.Equal("trailer.curtain", New(change, "trailer_variant"));
        Assert.Equal("trailer_def.curtain", New(change, "trailer_definition"));
        Assert.Equal("truck_a", New(change, "company_truck"));
        Assert.Equal("33", New(change, "units_count"));
    }

    [Fact]
    public void FillEmpty_old_values_are_the_current_raw_values()
    {
        var change = Plan(Open(), Fill());

        Assert.Equal("\"\"", Old(change, "target"));
        Assert.Equal("null", Old(change, "cargo"));
    }

    [Fact]
    public void Distance_is_filled_by_the_board_never_by_the_caller()
    {
        var change = Plan(Open(), Fill(Lkwlog));
        Assert.Equal("226", New(change, "shortest_distance_km")); // conhecida (oferta a)
    }

    [Fact]
    public void Estimated_distance_is_written_and_marked_on_the_review_line()
    {
        var change = Plan(Open(), Fill(Dfh));

        Assert.True(int.Parse(New(change, "shortest_distance_km")) > 0);
        var line = change.Lines.Single(l => l.Label.Key == JobsTextKeys.FieldDistance);
        Assert.Equal("Format_KmApprox", Assert.IsType<DiffValue.Number>(line.After).FormatKey);
    }

    [Fact]
    public void Known_distance_line_uses_exact_format()
    {
        var line = Plan(Open(), Fill(Lkwlog)).Lines.Single(l => l.Label.Key == JobsTextKeys.FieldDistance);
        Assert.Equal("Format_Km", Assert.IsType<DiffValue.Number>(line.After).FormatKey);
    }

    [Theory]
    [InlineData(24, "Format_Hours", 24)]
    [InlineData(48, "Format_Hours", 48)]
    [InlineData(72, "Format_Hours", 72)]
    [InlineData(168, "Format_Days", 7)]
    public void Allowed_expirations(int hours, string key, int arg)
    {
        var change = Plan(Open(), Fill(hours: hours));

        Assert.Equal((1000 + hours * 60).ToString(), New(change, "expiration_time"));
        var line = change.Lines.Single(l => l.Label.Key == JobsTextKeys.FieldExpiration);
        var text = Assert.IsType<DiffValue.Localized>(line.After).Text;
        Assert.Equal(key, text.Key);
        Assert.Equal(arg, text.Args.Single());
    }

    [Fact]
    public void FillEmpty_review_lines_start_empty_and_name_the_new_values()
    {
        var change = Plan(Open(), Fill());

        Assert.All(change.Lines, l => Assert.IsType<DiffValue.Empty>(l.Before));
        var destination = change.Lines.Single(l => l.Label.Key == JobsTextKeys.FieldDestination);
        Assert.Equal(new DiffValue.GameName(GameNameKind.Company, "dfh"), destination.After);
        Assert.Equal(JobsTextKeys.ChangeTitle, change.Title.Key);
    }

    [Fact]
    public void Plan_does_not_change_the_session()
    {
        var s = Open();
        Plan(s, Fill());
        Assert.False(s.HasChanges);
    }

    // ---- substituir

    [Fact]
    public void ReplaceOffer_targets_the_chosen_offer_and_overwrites_trailer_too()
    {
        var change = Plan(Open(), Replace());

        Assert.Equal("jobs:slot:_nameless.a", change.Key);
        Assert.Equal("trailer.palet", New(change, "trailer_variant"));
        Assert.Equal("trailer.curtain", Old(change, "trailer_variant"));
        Assert.Equal("truck_c", New(change, "company_truck"));
        Assert.Equal("12", New(change, "units_count"));
        Assert.Equal("cargo.empty_palet", New(change, "cargo"));
        Assert.Equal("cargo.used_plast", Old(change, "cargo"));
        Assert.Equal("\"lkwlog.kosice\"", Old(change, "target"));
    }

    [Fact]
    public void ReplaceOffer_skips_fields_that_would_not_change()
    {
        var change = Plan(Open(), Replace());
        Assert.DoesNotContain(change.Patches.OfType<SetAttribute>(), p => p.OldValue == p.NewValue);
        Assert.DoesNotContain(change.Patches.OfType<SetAttribute>(), p => p.Attribute == "ferry_time"); // já 0
    }

    [Fact]
    public void ReplaceOffer_review_shows_before_and_after()
    {
        var change = Plan(Open(), Replace());

        var cargo = change.Lines.Single(l => l.Label.Key == JobsTextKeys.FieldCargo);
        Assert.Equal(new DiffValue.GameName(GameNameKind.Cargo, "used_plast"), cargo.Before);
        Assert.Equal(new DiffValue.GameName(GameNameKind.Cargo, "empty_palet"), cargo.After);
        var distance = change.Lines.Single(l => l.Label.Key == JobsTextKeys.FieldDistance);
        Assert.Equal(new DiffValue.Number(226, "Format_Km"), distance.Before);
    }

    [Fact]
    public void ReplaceOffer_trailer_line_carries_definition_and_variant()
    {
        var change = Plan(Open(), Replace());
        var line = change.Lines.Single(l => l.Label.Key == JobsTextKeys.FieldTrailer);

        Assert.Equal(new DiffValue.Trailer("trailer_def.curtain", "trailer.curtain"), line.Before);
        Assert.Equal(new DiffValue.Trailer("trailer_def.palet", "trailer.palet"), line.After);
        Assert.Equal("trailer.palet", New(change, "trailer_variant"));
        Assert.Equal("trailer_def.palet", New(change, "trailer_definition"));
    }

    [Fact]
    public void FillEmpty_trailer_line_has_empty_before()
    {
        var line = Plan(Open(), Fill()).Lines.Single(l => l.Label.Key == JobsTextKeys.FieldTrailer);
        Assert.IsType<DiffValue.Empty>(line.Before);
        Assert.IsType<DiffValue.Trailer>(line.After);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("_nameless.nope")]
    [InlineData("_nameless.b")] // vaga livre não é oferta
    [InlineData("_nameless.c")] // oferta de outra empresa
    public void ReplaceOffer_unknown_offer_is_OfferMissing(string? offer)
    {
        Invalid(() => Plan(Open(), Replace(offer)), JobsTextKeys.ErrorOfferMissing);
    }

    // ---- Stage e Key por slot

    [Fact]
    public void Planned_change_stages_and_shows_in_view()
    {
        var s = Open();
        s.Stage(Plan(s, Fill()));

        Assert.Equal("\"dfh.berlin\"", s.View.Find("_nameless.b")!.Get("target"));
        Assert.Equal("\"\"", s.Original.Find("_nameless.b")!.Get("target"));
    }

    [Fact]
    public void Editing_a_pending_change_restages_the_same_key_without_stale_patch()
    {
        var s = Open();
        s.Stage(Plan(s, Fill(Dfh)));

        // editar: mesmo slot, outro destino; o OldValue sai do base (Original), não da View
        var edited = Plan(s, Fill(Lkwlog, slot: "_nameless.b", hours: 168));
        s.Stage(edited);

        var pending = Assert.Single(s.Pending);
        Assert.Equal("jobs:slot:_nameless.b", pending.Key);
        Assert.Equal("\"lkwlog.kosice\"", s.View.Find("_nameless.b")!.Get("target"));
        Assert.Equal("\"\"", Old(edited, "target"));
    }

    [Fact]
    public void Planning_an_edit_from_the_View_would_be_stale()
    {
        var s = Open();
        s.Stage(Plan(s, Replace()));

        var fromView = JobPlanner.Plan(s.View, Board(s),
            new JobRequest(Kaarfor, Lkwlog, "used_plast", JobSlotMode.ReplaceOffer, "_nameless.a"));

        var e = Assert.Throws<CoreException>(() => s.Stage(fromView));
        Assert.Equal(CoreError.StalePatch, e.Code);
    }

    [Fact]
    public void Editing_a_pending_replacement_restages_without_conflict()
    {
        var s = Open();
        s.Stage(Plan(s, Replace()));

        s.Stage(Plan(s, new JobRequest(Kaarfor, Lkwlog, "used_plast", JobSlotMode.ReplaceOffer, "_nameless.a")));

        Assert.Single(s.Pending);
        Assert.Equal("\"lkwlog.kosice\"", s.View.Find("_nameless.a")!.Get("target"));
    }

    [Fact]
    public void Next_free_slot_is_used_per_pending_and_then_none_is_left()
    {
        var s = Open();
        s.Stage(Plan(s, Fill()));

        Invalid(() => Plan(s, Fill()), JobsTextKeys.ErrorNoFreeSlot);
    }

    [Fact]
    public void FillSlotId_that_is_not_a_free_slot_of_the_company_is_NoFreeSlot()
    {
        Invalid(() => Plan(Open(), Fill(slot: "_nameless.a")), JobsTextKeys.ErrorNoFreeSlot); // oferta, não vaga
        Invalid(() => Plan(Open(), Fill(slot: "_nameless.d")), JobsTextKeys.ErrorNoFreeSlot); // de outra empresa
    }

    [Fact]
    public void Two_slots_two_keys_both_stage()
    {
        var s = Open();
        s.Stage(Plan(s, Fill()));
        s.Stage(Plan(s, Replace()));
        Assert.Equal(2, s.Pending.Count);
        Assert.NotEqual(s.Pending[0].Key, s.Pending[1].Key);
    }

    // ---- violações (Jobs_Error_*)

    [Fact]
    public void SameCity_is_rejected()
    {
        Invalid(() => Plan(Open(), Fill(new CompanyLocation("dfh", "budapest"))), JobsTextKeys.ErrorSameCity);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(12)]
    [InlineData(96)]
    public void Expiration_outside_the_allowed_values_is_rejected(int hours)
    {
        Invalid(() => Plan(Open(), Fill(hours: hours)), JobsTextKeys.ErrorExpirationInvalid);
    }

    [Fact]
    public void Missing_origin_company()
    {
        var r = Fill() with { Origin = new CompanyLocation("nobody", "budapest") };
        Invalid(() => Plan(Open(), r), JobsTextKeys.ErrorOriginMissing);
    }

    [Fact]
    public void Missing_target_company()
    {
        Invalid(() => Plan(Open(), Fill(new CompanyLocation("nobody", "berlin"))), JobsTextKeys.ErrorTargetMissing);
    }

    [Fact]
    public void Cargo_not_shipped_by_origin()
    {
        Invalid(() => Plan(Open(), Fill(cargo: "other_cargo")), JobsTextKeys.ErrorCargoNotShipped);
    }

    [Fact]
    public void Cargo_not_accepted_by_target()
    {
        Invalid(() => Plan(Open(), Fill(Lkwlog, "empty_palet")), JobsTextKeys.ErrorCargoNotAccepted);
    }

    [Fact]
    public void Cargo_without_trailer_template()
    {
        Invalid(() => Plan(Open(), Fill(cargo: "ghost")), JobsTextKeys.ErrorNoTrailer);
    }

    [Fact]
    public void Save_without_game_time()
    {
        Invalid(() => Plan(Open(JobsSave.Text(economy: false)), Fill()), JobsTextKeys.ErrorNoGameTime);
    }

    [Fact]
    public void Company_without_free_slot()
    {
        var r = new JobRequest(Lkwlog, Kaarfor, "empty_palet", JobSlotMode.FillEmpty);
        Invalid(() => Plan(Open(), r), JobsTextKeys.ErrorNoFreeSlot);
    }

    [Fact]
    public void Every_error_is_InvalidEdit_with_a_Jobs_Error_reason()
    {
        var e = Invalid(() => Plan(Open(), Fill(hours: 5)), JobsTextKeys.ErrorExpirationInvalid);
        Assert.StartsWith("Jobs_Error_", e.Reason!.Key);
    }

    // ---- slot sem atributo obrigatório

    [Fact]
    public void Free_slot_without_a_mandatory_attribute_is_NoFreeSlot_instead_of_a_partial_job()
    {
        var s = Open(JobsSave.Text().Replace(" company_truck: null\r\n", "", StringComparison.Ordinal));
        Invalid(() => Plan(s, Fill()), JobsTextKeys.ErrorNoFreeSlot);
    }

    [Theory]
    [InlineData(" trailer_definition: trailer_def.curtain\r\n")]
    [InlineData(" shortest_distance_km: 226\r\n ferry_time")]
    public void Offer_without_a_mandatory_attribute_is_OfferMissing(string removed)
    {
        var tail = removed.EndsWith("ferry_time", StringComparison.Ordinal) ? " ferry_time" : "";
        var text = JobsSave.Text();
        var first = text.IndexOf(removed, StringComparison.Ordinal); // oferta "a" (a primeira do arquivo)
        var s = Open(text.Remove(first, removed.Length).Insert(first, tail));
        Invalid(() => Plan(s, Replace()), JobsTextKeys.ErrorOfferMissing);
    }

    [Fact]
    public void Optional_attribute_missing_is_still_skipped()
    {
        var s = Open(JobsSave.Text().Replace(" trailer_place: 0\r\n}\r\n\r\njob_offer_data : _nameless.b", "}\r\n\r\njob_offer_data : _nameless.b", StringComparison.Ordinal));
        Assert.NotEmpty(Plan(s, Replace()).Patches);
    }

    [Fact]
    public void Template_prefers_the_origin_company_then_falls_back_to_the_global_one()
    {
        var text = JobsSave.Text().Replace("company : company.volatile.kaarfor.budapest",
            Company("radus.sofia", "_nameless.e") + Job("_nameless.e", "\"dfh.berlin\"", "cargo.empty_palet", 300, "trailer.palet", "trailer_def.palet", "truck_e", 8) +
            "company : company.volatile.kaarfor.budapest", StringComparison.Ordinal);
        var s = Open(text);

        // lkwlog tem frete próprio de empty_palet (truck_c); kaarfor não tem: usa o global (o primeiro do save, radus)
        Assert.Equal("truck_c", Board(s).Template("lkwlog", "empty_palet")!.CompanyTruck);
        Assert.Equal("truck_e", Board(s).Template("kaarfor", "empty_palet")!.CompanyTruck);
        Assert.Equal("truck_e", Board(s).Template("empty_palet")!.CompanyTruck);
        Assert.Equal("truck_e", New(Plan(s, Fill(cargo: "empty_palet")), "company_truck"));
    }
}
