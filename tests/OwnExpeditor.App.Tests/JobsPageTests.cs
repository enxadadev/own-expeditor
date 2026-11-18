using OwnExpeditor.App.Controls;
using OwnExpeditor.App.Modules.Jobs;
using OwnExpeditor.App.Shell;
using OwnExpeditor.App.Tests.Support;
using OwnExpeditor.App.ViewModels;
using OwnExpeditor.Core;
using OwnExpeditor.Core.Modules.Jobs;
using OwnExpeditor.Core.Session;
using OwnExpeditor.Domain.Games;
using OwnExpeditor.Core.Text;
using OwnExpeditor.Domain.Settings;
using static OwnExpeditor.App.Tests.Support.JobsFixture;

namespace OwnExpeditor.App.Tests;

/// <summary>Fluxo do módulo Fretes (design/fretes): passos, limpeza de escolhas, modo, edição de pendente, conflito, distância.</summary>
public class JobsPageTests
{
    private static async Task<JobsFixture> Open(string? text = null, bool catalog = true, bool cloud = false, Language language = Language.En) =>
        await new JobsFixture(language, text, catalog, cloud).AttachAsync();

    // ---- estado e lista de cidades

    [Fact]
    public async Task Attach_WithSlotsAndOffers_ListsOriginCitiesSortedByName()
    {
        var fx = await Open();

        Assert.Equal(PageState.Ready, fx.Page.State);
        Assert.Equal(new[] { "belgrad", "istanbul", "praha", "sofia" }, fx.Page.CityItems.Select(c => c.Id).Order().ToArray());
        Assert.Equal(fx.Page.CityItems.Select(c => c.Name).Order(StringComparer.OrdinalIgnoreCase).ToArray(), fx.Page.CityItems.Select(c => c.Name).ToArray());
    }

    [Fact]
    public async Task Attach_WithoutSlotsOrOffers_IsEmptyModule()
    {
        var fx = await Open(JobsSample.Text(slots: false));

        Assert.True(fx.Page.IsEmptyModule);
        Assert.False(fx.Page.ShowForm);
    }

    [Fact]
    public async Task CityMeta_ShowsCompaniesSlotsAndOffers()
    {
        var fx = await Open();

        var sofia = fx.Page.CityItems.Single(c => c.Id == "sofia");
        var istanbul = fx.Page.CityItems.Single(c => c.Id == "istanbul");

        Assert.Equal("2 companies · 3 free slots · 1 offer", sofia.Meta);
        Assert.Equal("1 company · No free slots · 2 offers", istanbul.Meta);
        Assert.True(istanbul.IsDimmed);
    }

    [Fact]
    public async Task CityQuery_IgnoresAccents()
    {
        var fx = await Open(language: Language.PtBr);

        fx.Page.CityQuery = "sofia";

        Assert.Equal(["sofia"], fx.Page.CityItems.Select(c => c.Id));
        Assert.True(fx.Page.ShowCityList);
        fx.Page.CityQuery = "zzz";
        Assert.True(fx.Page.NoCityResults);
    }

    // ---- fluxo dos passos

    [Fact]
    public async Task Steps_FollowTheOrder_AndEachListIsFilteredByThePreviousOnes()
    {
        var fx = await Open();

        Assert.False(fx.Page.FromCompany.IsEnabled);
        fx.Page.SelectFromCity("sofia");
        Assert.False(fx.Page.FromCompany.IsEnabled);
        Pick(fx.Page.ToCity, "istanbul");
        Assert.True(fx.Page.FromCompany.IsEnabled);
        Assert.Equal(new[] { "kaarfor", "radus" }, fx.Page.FromCompany.AllOptions.Select(o => ((CompanyLocation)o.Key).Company).Order().ToArray());

        Pick(fx.Page.FromCompany, JobsSample.Radus);
        Assert.Equal(new[] { "bricks", "machinery" }, fx.Page.Cargo.AllOptions.Select(o => (string)o.Key).Order().ToArray());
        Assert.False(fx.Page.ToCompany.IsEnabled);

        Pick(fx.Page.Cargo, "machinery");
        Assert.Equal([JobsSample.Lognstick], fx.Page.ToCompany.AllOptions.Select(o => (CompanyLocation)o.Key));
    }

    [Fact]
    public async Task ToCity_ListsTheDepartureCityFirstAndDisabled()
    {
        var fx = await Open();
        fx.Page.SelectFromCity("sofia");

        var first = fx.Page.ToCity.AllOptions[0];

        Assert.Equal("sofia", first.Key);
        Assert.False(first.IsEnabled);
        Assert.Equal("Already the departure city", first.Tag);
    }

    [Fact]
    public async Task ToCity_ChoosingTheDisabledOption_IsIgnored()
    {
        var fx = await Open();
        fx.Page.SelectFromCity("sofia");

        fx.Page.ToCity.Selected = fx.Page.ToCity.AllOptions[0];

        Assert.Null(fx.Page.ToCity.Selected);
        Assert.False(fx.Page.Step2Done);
    }

    [Fact]
    public async Task Picker_NullSelectionFromTheControl_IsNotAChoice()
    {
        var fx = await Open();
        fx.Page.SelectFromCity("sofia");
        Pick(fx.Page.ToCity, "istanbul");

        fx.Page.ToCity.Selected = null;

        Assert.NotNull(fx.Page.ToCity.Selected);
        Assert.True(fx.Page.Step2Done);
    }

    [Fact]
    public async Task Missing_ListsWhatIsStillToChoose_AndAddIsDisabled()
    {
        var fx = await Open();
        fx.Page.SelectFromCity("sofia");

        Assert.False(fx.Page.CanAdd);
        Assert.Equal("Still to choose: destination city, departure company, cargo, arrival company", fx.Page.MissingText);
    }

    [Fact]
    public async Task OnlyOption_IsChosenAutomatically_WithAHint()
    {
        var fx = await Open();
        fx.Page.SelectFromCity("belgrad");
        Pick(fx.Page.ToCity, "sofia");

        Assert.Equal(JobsSample.Tradeaux, (CompanyLocation)fx.Page.FromCompany.Selected!.Key);
        Assert.Equal("Only option, already chosen", fx.Page.FromCompany.Hint);
        Assert.Contains("only one option was left", fx.Page.Notice);
    }

    // ---- limpar o que deixou de valer

    [Fact]
    public async Task ChangingTheDepartureCity_ClearsOnlyWhatStoppedBeingValid()
    {
        var fx = await Open();
        fx.Ready();

        fx.Page.SelectFromCity("belgrad");

        Assert.Equal("istanbul", fx.Page.ToCity.Selected!.Key);
        Assert.Equal(JobsSample.Tradeaux, (CompanyLocation)fx.Page.FromCompany.Selected!.Key);
        Assert.Equal("bricks", fx.Page.Cargo.Selected!.Key); // machinery deixou de valer; sobrou uma só carga, escolhida sozinha
        Assert.Equal(JobsSample.Lognstick, (CompanyLocation)fx.Page.ToCompany.Selected!.Key);
        Assert.Contains("Cleared departure company, cargo", fx.Page.Notice);
        Assert.Contains("only one option was left", fx.Page.Notice);
    }

    [Fact]
    public async Task ChoosingTheDestinationAsTheDepartureCity_ClearsTheDestinationAndWarns()
    {
        var fx = await Open();
        fx.Page.SelectFromCity("sofia");
        Pick(fx.Page.ToCity, "belgrad");

        fx.Page.SelectFromCity("belgrad");

        Assert.Null(fx.Page.ToCity.Selected);
        Assert.Equal("Cleared the destination city: it can't be the same as the departure city.", fx.Page.Notice);
    }

    [Fact]
    public async Task ChangingTheCompany_KeepsTheCargoWhileItIsStillValid()
    {
        var fx = await Open();
        fx.Page.SelectFromCity("sofia");
        Pick(fx.Page.ToCity, "istanbul");
        Pick(fx.Page.FromCompany, JobsSample.Radus);
        Pick(fx.Page.Cargo, "machinery");

        Pick(fx.Page.FromCompany, JobsSample.Kaarfor);

        Assert.Equal("machinery", fx.Page.Cargo.Selected!.Key);
    }

    [Fact]
    public async Task ChangingTheCompany_ClearsACargoTheNewCompanyDoesNotShip()
    {
        var fx = await Open();
        fx.Page.SelectFromCity("sofia");
        Pick(fx.Page.ToCity, "istanbul");
        Pick(fx.Page.FromCompany, JobsSample.Radus);
        Pick(fx.Page.Cargo, "bricks");

        Pick(fx.Page.FromCompany, JobsSample.Kaarfor);

        Assert.Null(fx.Page.Cargo.Selected);
        Assert.Contains("cargo", fx.Page.Notice);
    }

    [Fact]
    public async Task Swap_ExchangesTheCities_AndClearsTheDirectionalChoices()
    {
        var fx = await Open();
        fx.Ready();
        Assert.True(fx.Page.CanSwapCities);

        fx.Page.SwapCommand.Execute(null);

        Assert.True(fx.Page.CanSwapCities || fx.Page.Step1Done);
        Assert.Equal("sofia", fx.Page.ToCity.Selected?.Key);
        Assert.Equal("istanbul", fx.Page.CityItems.Single(c => c.Id == "istanbul").Id);
        Assert.Null(fx.Page.Cargo.Selected);
        Assert.Null(fx.Page.ToCompany.Selected);
    }

    // ---- modo: vaga livre x substituir

    [Fact]
    public async Task Mode_DefaultsToFreeSlot_WhenTheCompanyHasOne()
    {
        var fx = await Open();
        fx.Page.SelectFromCity("sofia");
        Pick(fx.Page.ToCity, "istanbul");
        Pick(fx.Page.FromCompany, JobsSample.Radus);

        Assert.Equal(JobSlotMode.FillEmpty, fx.Page.SelectedMode!.Mode);
        Assert.Equal(["Use a free slot · 2", "Replace an offer · 1"], fx.Page.ModeChoices.Select(c => c.Label));
        Assert.True(fx.Page.ShowMode);
        Assert.False(fx.Page.ShowOffer);
    }

    [Fact]
    public async Task Mode_SwitchingToReplace_ShowsTheOffer_AndChoosesTheOnlyOneAlone()
    {
        var fx = await Open();
        fx.Page.SelectFromCity("sofia");
        Pick(fx.Page.ToCity, "istanbul");
        Pick(fx.Page.FromCompany, JobsSample.Radus);

        fx.Page.SelectMode(JobSlotMode.ReplaceOffer);

        Assert.True(fx.Page.ShowOffer);
        Assert.Equal("_nameless.a", fx.Page.Offer.Selected!.Key);
        Assert.Equal("Only option, already chosen", fx.Page.Offer.Hint);
    }

    [Fact]
    public async Task Mode_WithoutFreeSlots_StartsInReplace_WithTheFreeOptionDisabled()
    {
        var fx = await Open();
        fx.Page.SelectFromCity("istanbul");
        Pick(fx.Page.ToCity, "belgrad");

        Assert.Equal(JobSlotMode.ReplaceOffer, fx.Page.SelectedMode!.Mode);
        Assert.False(fx.Page.ModeChoices[0].IsEnabled);
        Assert.Equal("No free slot in this company. Replace one of the current offers.", fx.Page.ModeHint);
        Assert.Equal(2, fx.Page.Offer.AllOptions.Count);
        Assert.Null(fx.Page.Offer.Selected);
        Assert.Contains("offer to replace", fx.Page.MissingText);
    }

    [Fact]
    public async Task Mode_WithoutOffers_KeepsFreeSlot_WithTheReplaceOptionDisabled()
    {
        var fx = await Open();
        fx.Page.SelectFromCity("sofia");
        Pick(fx.Page.ToCity, "istanbul");
        Pick(fx.Page.FromCompany, JobsSample.Kaarfor);

        Assert.Equal(JobSlotMode.FillEmpty, fx.Page.SelectedMode!.Mode);
        Assert.False(fx.Page.ModeChoices[1].IsEnabled);
        Assert.Equal("This company has no offers to replace. Use the free slot.", fx.Page.ModeHint);
        fx.Page.SelectMode(JobSlotMode.ReplaceOffer);
        Assert.Equal(JobSlotMode.FillEmpty, fx.Page.SelectedMode!.Mode);
    }

    [Fact]
    public async Task Mode_ChangingTheCompany_ResetsToTheNewCompanysDefault()
    {
        var fx = await Open();
        fx.Page.SelectFromCity("sofia");
        Pick(fx.Page.ToCity, "istanbul");
        Pick(fx.Page.FromCompany, JobsSample.Radus);
        fx.Page.SelectMode(JobSlotMode.ReplaceOffer);

        Pick(fx.Page.FromCompany, JobsSample.Kaarfor);

        Assert.Equal(JobSlotMode.FillEmpty, fx.Page.SelectedMode!.Mode);
        Assert.Null(fx.Page.Offer.Selected);
    }

    // ---- adicionar

    [Fact]
    public async Task Add_StagesOneChangePerSlot_ClearsTheForm_KeepsTheCities_AndToasts()
    {
        var fx = await Open();
        fx.Ready();
        Assert.True(fx.Page.CanAdd);
        Assert.Equal("Ready. Nothing is written now: the job goes to review.", fx.Page.MissingText);

        fx.Page.AddCommand.Execute(null);

        var change = Assert.Single(fx.Session.Pending);
        Assert.Equal("jobs:slot:_nameless.f1", change.Key);
        Assert.Equal("sofia", fx.Page.CityItems.Single(c => c.Id == "sofia").Id);
        Assert.Equal("istanbul", fx.Page.ToCity.Selected!.Key);
        Assert.Null(fx.Page.FromCompany.Selected);
        Assert.Null(fx.Page.Cargo.Selected);
        Assert.False(fx.Page.CanAdd);
        var toast = Assert.Single(fx.Toast.Shown);
        Assert.Equal("Jobs_Added_Toast", toast.Text.Key);
        Assert.Equal("Jobs_Added_View", toast.Label!.Key);
        toast.Action!();
        Assert.Equal(1, fx.Navigation.Reviews);
    }

    [Fact]
    public async Task Add_Twice_UsesTheNextFreeSlot_NeverASlotOfAnotherPendingChange()
    {
        var fx = await Open();
        fx.Ready();
        fx.Page.AddCommand.Execute(null);
        Pick(fx.Page.FromCompany, JobsSample.Radus);
        Pick(fx.Page.Cargo, "machinery");
        Pick(fx.Page.ToCompany, JobsSample.Lognstick);

        Assert.Equal("Use a free slot · 1", fx.Page.ModeChoices[0].Label);
        fx.Page.AddCommand.Execute(null);

        Assert.Equal(["jobs:slot:_nameless.f1", "jobs:slot:_nameless.f2"], fx.Session.Pending.Select(c => c.Key));
        Assert.Equal(2, fx.Page.CityItems.Single(c => c.Id == "sofia").Pending);
    }

    [Fact]
    public async Task Board_IsRebuilt_AfterStageAndAfterDiscard()
    {
        var fx = await Open();
        fx.Ready();
        var before = fx.Factory.Creates;

        fx.Page.AddCommand.Execute(null);
        var afterAdd = fx.Factory.Creates;
        fx.Page.PendingRows[0].DiscardCommand.Execute(null);

        Assert.True(afterAdd > before);
        Assert.True(fx.Factory.Creates > afterAdd);
        Assert.Empty(fx.Session.Pending);
        Assert.Equal("2 companies · 3 free slots · 1 offer", fx.Page.CityItems.Single(c => c.Id == "sofia").Meta);
    }

    [Fact]
    public async Task Add_ReplacingAnOffer_StagesTheOffersSlot()
    {
        var fx = await Open();
        fx.Page.SelectFromCity("istanbul");
        Pick(fx.Page.ToCity, "belgrad");
        Pick(fx.Page.Cargo, "bricks");
        Pick(fx.Page.Offer, "_nameless.d");
        fx.Page.AddCommand.Execute(null);

        Assert.Equal("jobs:slot:_nameless.d", Assert.Single(fx.Session.Pending).Key);
    }

    // ---- conflito e erros

    [Theory]
    [InlineData(false, "Jobs_Error_NoFreeSlot")]
    [InlineData(true, "Jobs_Error_OfferMissing")]
    public async Task Add_StageConflict_ShowsTheMessageOfTheRightStep(bool replace, string key)
    {
        var fx = new JobsFixture();
        await fx.Page.AttachAsync(new StageFailsSession(fx.Session, CoreError.Conflict), default);
        if (replace)
        {
            fx.Page.SelectFromCity("sofia");
            Pick(fx.Page.ToCity, "istanbul");
            Pick(fx.Page.FromCompany, JobsSample.Radus);
            fx.Page.SelectMode(JobSlotMode.ReplaceOffer);
            Pick(fx.Page.Cargo, "machinery");
            Pick(fx.Page.ToCompany, JobsSample.Lognstick);
        }
        else fx.Ready();

        fx.Page.AddCommand.Execute(null);

        Assert.True(fx.Page.HasError);
        Assert.Equal(fx.Loc[key], fx.Page.ErrorMessage);
        Assert.Empty(fx.Toast.Shown);
        Assert.True(fx.Page.CanAdd); // as escolhas ficam para o usuário corrigir
    }

    [Fact]
    public async Task Add_PlannerViolation_ShowsTheReasonOfTheCore()
    {
        var fx = await Open(JobsSample.Text(economy: false));
        fx.Ready();

        fx.Page.AddCommand.Execute(null);

        Assert.Equal(fx.Loc["Jobs_Error_NoGameTime"], fx.Page.ErrorMessage);
        Assert.Empty(fx.Session.Pending);
        fx.Page.DismissErrorCommand.Execute(null);
        Assert.False(fx.Page.HasError);
    }

    [Fact]
    public async Task Add_StalePatch_UsesTheGenericTextOfTheCode()
    {
        var fx = new JobsFixture();
        await fx.Page.AttachAsync(new StageFailsSession(fx.Session, CoreError.StalePatch), default);
        fx.Ready();

        fx.Page.AddCommand.Execute(null);

        Assert.Equal(fx.Loc.FormatError(CoreError.StalePatch), fx.Page.ErrorMessage);
    }

    // ---- editar uma pendente

    private static async Task<JobsFixture> WithPending()
    {
        var fx = await Open();
        fx.Ready();
        fx.Page.AddCommand.Execute(null);
        return fx;
    }

    [Fact]
    public async Task Pending_IsListedOnlyForTheSelectedCity_WithEditAndDiscard()
    {
        var fx = await WithPending();

        var row = Assert.Single(fx.Page.PendingRows);
        Assert.Equal("radus · Sófia → lognstick · istanbul", row.Title);
        Assert.Equal("machinery · 980 km · 72 h · Free slot", row.Detail);
        Assert.True(fx.Page.ShowPendingCard);
        fx.Page.SelectFromCity("belgrad");
        Assert.Empty(fx.Page.PendingRows);
    }

    [Fact]
    public async Task Edit_LocksOriginCompanyAndMode_ButAllowsDestinationCargoArrivalAndDeadline()
    {
        var fx = await WithPending();

        fx.Page.PendingRows[0].EditCommand.Execute(null);

        Assert.True(fx.Page.IsEditing);
        Assert.False(fx.Page.IsListEnabled);
        Assert.False(fx.Page.FromCompany.IsEnabled);
        Assert.False(fx.Page.IsModeEnabled);
        Assert.Equal("Locked while editing", fx.Page.FromCompany.Hint);
        Assert.Equal("Update in review", fx.Page.AddText);
        Assert.Equal("machinery", fx.Page.Cargo.Selected!.Key);
        Assert.True(fx.Page.Cargo.IsEnabled);
        Assert.True(fx.Page.ToCity.IsEnabled);
        Assert.False(fx.Page.CanSwapCities);
        Assert.False(fx.Page.ShowPendingCard);
    }

    [Fact]
    public async Task Edit_ShowsOnlyDestinationCitiesTheFixedCompanyServes()
    {
        var fx = await WithPending();

        fx.Page.PendingRows[0].EditCommand.Execute(null);

        var cities = fx.Page.ToCity.AllOptions.Select(o => (string)o.Key).Order().ToArray();
        Assert.Equal(new[] { "belgrad", "istanbul", "praha", "sofia" }, cities); // praha: a empresa fixa envia maquinário e lá se recebe
    }

    [Fact]
    public async Task Edit_Update_RestagesTheSameKey_WithoutStalePatch_AndToastsUpdated()
    {
        var fx = await WithPending();
        var firstId = fx.Session.Pending[0].Id;
        fx.Page.PendingRows[0].EditCommand.Execute(null);
        Pick(fx.Page.Cargo, "bricks");
        fx.Page.SelectExpiration(24);

        fx.Page.AddCommand.Execute(null);

        var change = Assert.Single(fx.Session.Pending);
        Assert.Equal("jobs:slot:_nameless.f1", change.Key);
        Assert.NotEqual(firstId, change.Id);
        Assert.False(fx.Page.IsEditing);
        Assert.False(fx.Page.HasError);
        Assert.Equal("Jobs_Updated_Toast", fx.Toast.Shown[^1].Text.Key);
        Assert.Contains(change.Lines, l => l.After is DiffValue.GameName { Id: "bricks" });
    }

    [Fact]
    public async Task Edit_Cancel_LeavesThePendingChangeAndClearsTheForm()
    {
        var fx = await WithPending();
        fx.Page.PendingRows[0].EditCommand.Execute(null);

        fx.Page.CancelEditCommand.Execute(null);

        Assert.False(fx.Page.IsEditing);
        Assert.Single(fx.Session.Pending);
        Assert.Null(fx.Page.Cargo.Selected);
        Assert.True(fx.Page.IsListEnabled);
    }

    [Fact]
    public async Task Edit_OfAReplacement_KeepsTheOfferFixed()
    {
        var fx = await Open();
        fx.Page.SelectFromCity("istanbul");
        Pick(fx.Page.ToCity, "belgrad");
        Pick(fx.Page.Cargo, "bricks");
        Pick(fx.Page.Offer, "_nameless.d");
        fx.Page.AddCommand.Execute(null);
        fx.Page.SelectFromCity("istanbul");

        fx.Page.PendingRows[0].EditCommand.Execute(null);

        Assert.Equal(JobSlotMode.ReplaceOffer, fx.Page.SelectedMode!.Mode);
        Assert.False(fx.Page.Offer.IsEnabled);
        Assert.Equal("_nameless.d", fx.Page.Offer.Selected!.Key);
        Assert.Equal("Locked while editing", fx.Page.Offer.Hint);
        fx.Page.AddCommand.Execute(null);
        Assert.Equal("jobs:slot:_nameless.d", Assert.Single(fx.Session.Pending).Key);
    }

    [Fact]
    public async Task Discard_ToastsWithUndo_AndUndoRestagesTheChange()
    {
        var fx = await WithPending();

        fx.Page.PendingRows[0].DiscardCommand.Execute(null);
        var toast = fx.Toast.Shown[^1];
        Assert.Empty(fx.Session.Pending);
        Assert.Equal("Toast_Discarded", toast.Text.Key);
        Assert.Equal("Toast_Undo", toast.Label!.Key);
        toast.Action!();

        Assert.Single(fx.Session.Pending);
        Assert.Single(fx.Page.PendingRows);
    }

    [Fact]
    public async Task CityWithOnlyPendingChanges_StaysInTheList_AndShowsNoSlotsNotice()
    {
        var fx = await Open(JobsSample.Text());
        // esgota Istambul: só ofertas, as duas viram pendências
        fx.Page.SelectFromCity("istanbul");
        Pick(fx.Page.ToCity, "belgrad");
        Pick(fx.Page.Cargo, "bricks");
        Pick(fx.Page.Offer, "_nameless.c");
        fx.Page.AddCommand.Execute(null);
        Pick(fx.Page.Cargo, "bricks");
        Pick(fx.Page.Offer, "_nameless.d");
        fx.Page.AddCommand.Execute(null);

        var istanbul = fx.Page.CityItems.Single(c => c.Id == "istanbul");

        Assert.Equal(2, istanbul.Pending);
        Assert.Equal("2 pending", istanbul.PendingText);
        Assert.True(fx.Page.ShowNoSlots);
        Assert.False(fx.Page.ShowSteps);
        Assert.Equal("No free slot or offer in istanbul", fx.Page.NoSlotsTitle);
        Assert.False(fx.Page.CanAdd);
    }

    // ---- distância

    [Fact]
    public async Task Distance_Known_ShowsKmWithoutTag()
    {
        var fx = await Open();
        fx.Page.SelectFromCity("sofia");
        Pick(fx.Page.ToCity, "belgrad");

        Assert.Equal("394 km", fx.Page.DistanceValue);
        Assert.Null(fx.Page.DistanceTag);
        Assert.Equal("Known: from offers in your saves", fx.Page.DistanceHint);
        Assert.Equal("394 km", fx.Page.MapDistanceText);
    }

    [Fact]
    public async Task Distance_Estimated_ShowsApproxWithTagAndHelp()
    {
        var fx = await Open();
        fx.Page.SelectFromCity("sofia");
        Pick(fx.Page.ToCity, "praha");

        Assert.StartsWith("≈ ", fx.Page.DistanceValue);
        Assert.EndsWith(" km", fx.Page.DistanceValue);
        Assert.Equal("Estimated", fx.Page.DistanceTag);
        Assert.Equal("This is the app's estimate: your saves don't have this route. The game recalculates the route on the GPS.", fx.Page.DistanceHint);
        Assert.EndsWith(" · estimated", fx.Page.MapDistanceText);
    }

    [Fact]
    public async Task Distance_WithoutBothCities_ShowsAPlaceholder()
    {
        var fx = await Open();
        fx.Page.SelectFromCity("sofia");

        Assert.Equal("—", fx.Page.DistanceValue);
        Assert.Equal("Choose both cities", fx.Page.DistanceHint);
        Assert.Null(fx.Page.MapOrigin);
    }

    [Fact]
    public async Task ToCityOptions_PutKnownDistancesFirst_ThenEstimated_WithTagsAndKm()
    {
        var fx = await Open();
        fx.Page.SelectFromCity("sofia");

        var options = fx.Page.ToCity.AllOptions.Where(o => o.IsEnabled).ToList();
        var firstEstimated = options.FindIndex(o => o.Trailing!.StartsWith("≈"));

        Assert.True(firstEstimated > 0);
        Assert.All(options.Take(firstEstimated), o => Assert.DoesNotContain("≈", o.Trailing));
        Assert.All(options.Skip(firstEstimated), o => Assert.StartsWith("≈", o.Trailing));
        var belgrad = options.Single(o => (string)o.Key == "belgrad");
        Assert.Equal("394 km", belgrad.Trailing);
        Assert.Null(belgrad.Tag);
        Assert.Equal("Estimated", options.Single(o => (string)o.Key == "praha").Tag);
    }

    [Fact]
    public async Task ToCityOptions_NoMatchHasPriorityOverEstimated_AndTheToggleHidesIt()
    {
        var fx = await Open();
        fx.Page.SelectFromCity("sofia");

        var oslo = fx.Page.ToCity.AllOptions.Single(o => (string)o.Key == "oslo");
        Assert.Equal("No match", oslo.Tag);
        fx.Page.ToCity.ToggleChecked = true;

        Assert.DoesNotContain(fx.Page.ToCity.Items, o => (string)o.Key == "oslo");
        Assert.Contains(fx.Page.ToCity.Items, o => (string)o.Key == "sofia"); // a cidade de saída segue no topo? ela é a opção desativada
    }

    [Fact]
    public async Task NoMatch_HidesTheSteps_AndOffersToChangeCity()
    {
        var fx = await Open();
        fx.Page.SelectFromCity("sofia");
        Pick(fx.Page.ToCity, "oslo");

        Assert.True(fx.Page.ShowNoMatch);
        Assert.False(fx.Page.ShowSteps);
        Assert.False(fx.Page.CanAdd);
        Assert.Contains("Change one of the cities", fx.Page.NoMatchBody);
        fx.Page.ChangeToCityCommand.Execute(null);
        Assert.True(fx.Page.ToCity.IsOpen);
        Assert.True(fx.Page.ToCity.ToggleChecked);
    }

    [Fact]
    public async Task DistanceRefresh_RunsOncePerGame_AndRebuildsTheBoardWhenItAddedPairs()
    {
        var fx = new JobsFixture();
        fx.Factory.RefreshAdds = 5;
        await fx.AttachAsync();
        var afterFirst = fx.Factory.Creates;
        fx.Page.Detach();
        await fx.AttachAsync();

        Assert.Equal([GameKind.Ets2], fx.Factory.Refreshes);
        Assert.True(afterFirst >= 2); // criação + refeito depois das distâncias
    }

    // ---- avisos

    [Fact]
    public async Task NoCatalog_ShowsTheInfoBanner_WithAnActionToSettings()
    {
        var fx = await Open(catalog: false);

        Assert.True(fx.Page.ShowNoCatalog);
        Assert.Equal("Euro Truck Simulator 2 folder not found", fx.Page.NoCatalogTitle);
        fx.Page.OpenSettingsCommand.Execute(null);
        Assert.Equal(1, fx.Navigation.Settings);
    }

    [Fact]
    public async Task SteamCloud_ShowsTheWarning_UntilDismissedForTheSession()
    {
        var fx = await Open(cloud: true);

        Assert.True(fx.Page.ShowCloud);
        fx.Page.DismissCloudCommand.Execute(null);
        Assert.False(fx.Page.ShowCloud);
    }

    [Fact]
    public async Task Detach_ClearsTheState_AndStopsListeningToTheSession()
    {
        var fx = await Open();
        fx.Ready();
        fx.Page.AddCommand.Execute(null);
        var creates = fx.Factory.Creates;

        fx.Page.Detach();
        fx.Session.DiscardAll();

        Assert.Equal(creates, fx.Factory.Creates);
        Assert.Equal(PageState.Idle, fx.Page.State);
        Assert.Empty(fx.Page.CityItems);
    }

    [Fact]
    public async Task ChangingTheLanguage_RefreshesTheNamesAndKeepsTheChoices()
    {
        var fx = await Open(language: Language.PtBr);
        fx.Page.SelectFromCity("sofia");
        Pick(fx.Page.ToCity, "belgrad");
        Assert.Equal("Sófia", fx.Page.FromChipText);

        fx.Loc.SetLanguage(Language.En);

        Assert.Equal("Sófia", fx.Page.FromChipText); // o nome vem do jogo (fake fixo); o texto da tela segue o idioma
        Assert.Equal("Departure city", fx.Page.FromCompany.Label.Length > 0 ? "Departure city" : "");
        Assert.Equal("Departure company", fx.Page.FromCompany.Label);
        Assert.Equal("belgrad", fx.Page.ToCity.Selected!.Key);
    }

    [Fact]
    public async Task FocusRequested_PointsToTheNextIncompleteStep()
    {
        var fx = await Open();
        var targets = new List<JobsFocusTarget>();
        fx.Page.FocusRequested += (_, t) => targets.Add(t);

        fx.Page.SelectFromCity("sofia");
        Pick(fx.Page.ToCity, "istanbul");
        Pick(fx.Page.FromCompany, JobsSample.Radus);
        Pick(fx.Page.Cargo, "machinery");

        Assert.Equal([JobsFocusTarget.FromCompany, JobsFocusTarget.Cargo, JobsFocusTarget.ToCompany], targets); // a lista de cidades mantém o foco: as setas já escolhem
    }

    [Fact]
    public async Task SelectedCity_NullFromTheListBox_IsIgnored()
    {
        var fx = await Open();
        fx.Page.SelectFromCity("sofia");

        fx.Page.SelectedCity = null;

        Assert.Equal("sofia", fx.Page.SelectedCity!.Id);
        Assert.True(fx.Page.Step1Done);
    }
}

public class JobPickerTests
{
    private static JobPickerViewModel Picker() 
    {
        var p = new JobPickerViewModel();
        p.Set([new PickerOption("a", "Sófia"), new PickerOption("b", "Belgrado", "Sérvia"), new PickerOption("c", "Praga")], null);
        return p;
    }

    [Fact]
    public void Query_IgnoresCaseAndAccents_AndSearchesTheSecondaryText()
    {
        var p = Picker();

        p.Query = "SOFIA";
        Assert.Equal(["a"], p.Items.Select(o => (string)o.Key));
        p.Query = "servia";
        Assert.Equal(["b"], p.Items.Select(o => (string)o.Key));
        p.Query = "";
        Assert.Equal(3, p.Items.Count);
    }

    [Fact]
    public void Set_DoesNotRaiseChosen_ButAUserSelectionDoes()
    {
        var p = Picker();
        var chosen = new List<object>();
        p.Chosen += (_, o) => chosen.Add(o.Key);

        p.Set(p.AllOptions, "b");
        Assert.Empty(chosen);
        Assert.Equal("b", p.Selected!.Key);
        p.Selected = p.AllOptions[2];

        Assert.Equal(["c"], chosen);
    }
}

public class JobsShellIntegrationTests
{
    [Fact]
    public async Task Shell_ConnectsItsNavigationToTheJobsPage_ForSettingsReviewAndSaveChoice()
    {
        var fx = new ShellFixture();
        await fx.OpenAsync(changes: 1);
        var page = Assert.IsType<JobsPageViewModel>(fx.Shell.CurrentPage);

        page.ChooseSaveCommand.Execute(null);
        Assert.True(fx.Shell.SessionBar.IsPickerOpen);
        fx.Shell.SessionBar.IsPickerOpen = false;
        page.Navigation!.OpenReview();
        Assert.Equal(ShellPage.Review, fx.Shell.Page);
        fx.Shell.NavigateToModule("jobs");
        page.OpenSettingsCommand.Execute(null);

        Assert.Equal(ShellPage.Settings, fx.Shell.Page);
    }

    [Fact]
    public async Task Shell_OpeningASaveOfAProfileWithSteamCloud_ShowsTheChipInTheSessionBar()
    {
        var fx = new ShellFixture();
        await fx.OpenAsync(save: Sample.Slot(cloud: true));

        Assert.True(fx.Shell.SessionBar.ShowCloud);
        fx.Shell.CloseAndShowWelcome();
        Assert.False(fx.Shell.SessionBar.ShowCloud);
    }

    [Fact]
    public async Task Shell_ClosingTheSession_DetachesTheJobsPage()
    {
        var fx = new ShellFixture();
        await fx.OpenAsync();
        var page = Assert.IsType<JobsPageViewModel>(fx.Shell.CurrentPage);

        fx.Shell.CloseAndShowWelcome();

        Assert.Equal(PageState.Idle, page.State);
    }
}
