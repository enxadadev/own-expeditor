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

/// <summary>
/// Ligação do módulo Fretes com a tela (design/fretes-pais-cidade): a cadeia de regras é do Core (JobSelectionRules);
/// aqui: visibilidade, habilitação, dicas, avisos, foco, fila de pendentes. Chaves dos seletores: país = id ("" = Outros),
/// cidade = id, empresa = CompanyLocation, vaga = JobSlotChoice, carga = id.
/// </summary>
public class JobsPageTests
{
    private static readonly JobSlotChoice Free = JobSlotChoice.Free;

    private static async Task<JobsFixture> Open(string? text = null, bool catalog = true, bool cloud = false,
        Language language = Language.En, CountryLayout countries = CountryLayout.Mixed) =>
        await new JobsFixture(language, text, catalog, cloud, countries).AttachAsync();

    private static string[] Keys(JobPickerViewModel p) => p.AllOptions.Select(o => (string)o.Key).ToArray();

    // ---- abertura e países

    [Fact]
    public async Task Attach_WithSlotsAndOffers_ListsDepartureCountriesWithOtherLast()
    {
        var fx = await Open();

        Assert.Equal(PageState.Ready, fx.Page.State);
        Assert.True(fx.Page.ShowCountries);
        Assert.Equal(["bulgaria", "serbia", "turkey", ""], Keys(fx.Page.FromCountry));
        Assert.Equal("Other", fx.Page.FromCountry.AllOptions[^1].Primary);
        Assert.Equal("1 city", fx.Page.FromCountry.AllOptions[0].Trailing);
    }

    [Fact]
    public async Task Attach_WithoutSlotsOrOffers_IsEmptyModule()
    {
        var fx = await Open(JobsSample.Text(slots: false));

        Assert.True(fx.Page.IsEmptyModule);
        Assert.False(fx.Page.ShowForm);
    }

    [Fact]
    public async Task Attach_WithoutGameFolder_HidesCountriesAndListsEveryCity()
    {
        var fx = await Open(catalog: false);

        Assert.True(fx.Page.ShowNoCatalog);
        Assert.False(fx.Page.ShowCountries);
        Assert.Equal(["belgrad", "istanbul", "praha", "sofia"], Keys(fx.Page.FromCity).Order().ToArray());
        Assert.True(fx.Page.FromCity.IsEnabled);
    }

    [Fact]
    public async Task Attach_CatalogWithoutCountries_HidesCountries()
    {
        var fx = await Open(countries: CountryLayout.None);

        Assert.False(fx.Page.ShowCountries);
        Assert.False(fx.Page.ShowNoCatalog);
        Assert.Equal(4, fx.Page.FromCity.AllOptions.Count);
    }

    [Fact]
    public async Task Attach_SingleCountry_ChoosesItWithoutANotice()
    {
        var fx = await Open(countries: CountryLayout.Single);

        Assert.Equal("bulgaria", fx.Page.FromCountry.Selected!.Key);
        Assert.True(fx.Page.FromCountry.IsSingleOption);
        Assert.False(fx.Page.HasNotice);
        Assert.Null(fx.Page.Notice);
    }

    [Fact]
    public async Task Attach_FocusGoesToTheFirstEmptyEnabledField()
    {
        var mixed = new JobsFixture();
        var single = new JobsFixture(countries: CountryLayout.Single);
        var mixedTargets = new List<JobsFocusTarget>();
        var singleTargets = new List<JobsFocusTarget>();
        mixed.Page.FocusRequested += (_, t) => mixedTargets.Add(t);
        single.Page.FocusRequested += (_, t) => singleTargets.Add(t);

        await mixed.AttachAsync();
        await single.AttachAsync();

        Assert.Equal(JobsFocusTarget.FromCountry, mixedTargets.First());
        Assert.Equal(JobsFocusTarget.FromCity, singleTargets.First());
    }

    // ---- campos: habilitação e dicas

    [Fact]
    public async Task Fields_OnAFreshForm_AreDisabledWithTheHintOfTheStepBefore()
    {
        var fx = await Open();
        var p = fx.Page;

        Assert.True(p.FromCountry.IsEnabled);
        Assert.False(p.FromCity.IsEnabled);
        Assert.Equal("Choose the country first", p.FromCity.Hint);
        Assert.False(p.FromCompany.IsEnabled);
        Assert.Equal("Choose the city first", p.FromCompany.Hint);
        Assert.False(p.Cargo.IsEnabled);
        Assert.Equal("Choose the arrival company first", p.Cargo.Hint);
    }

    [Fact]
    public async Task Arrival_UntilTheDepartureCompanyIsChosen_IsDisabledWithTheHint()
    {
        var fx = await Open();
        fx.Page.FromCountry.Selected = fx.Page.FromCountry.AllOptions.First(o => (string)o.Key == "bulgaria");

        Assert.Equal("sofia", fx.Page.FromCity.Selected!.Key);
        Assert.Null(fx.Page.FromCompany.Selected);
        Assert.All(new[] { fx.Page.ToCountry, fx.Page.ToCity, fx.Page.ToCompany }, p =>
        {
            Assert.False(p.IsEnabled);
            Assert.Equal("Choose the departure company first", p.Hint);
        });
    }

    [Fact]
    public async Task Arrival_AfterTheDepartureCompany_OpensCountryFirst_ThenCityAndCompanyHints()
    {
        var fx = await Open();
        fx.Page.FromCountry.Selected = fx.Page.FromCountry.AllOptions.First(o => (string)o.Key == "bulgaria");

        Pick(fx.Page.FromCompany, JobsSample.Radus);

        Assert.True(fx.Page.ToCountry.IsEnabled);
        Assert.False(fx.Page.ToCity.IsEnabled);
        Assert.Equal("Choose the country first", fx.Page.ToCity.Hint);
        Assert.False(fx.Page.ToCompany.IsEnabled);
        Assert.Equal("Choose the city first", fx.Page.ToCompany.Hint);
        Assert.Equal(["serbia", "turkey", ""], Keys(fx.Page.ToCountry));
    }

    [Fact]
    public async Task Fields_AreFilteredByThePreviousOnes()
    {
        var fx = await Open();

        Pick(fx.Page.FromCountry, "bulgaria");
        Assert.Equal(["sofia"], Keys(fx.Page.FromCity));
        Assert.Equal(["kaarfor", "radus"], fx.Page.FromCompany.AllOptions.Select(o => ((CompanyLocation)o.Key).Company).Order().ToArray());
        Pick(fx.Page.FromCompany, JobsSample.Radus);
        Pick(fx.Page.ToCountry, "turkey");

        Assert.Equal(["istanbul"], Keys(fx.Page.ToCity));
        Assert.Equal([JobsSample.Lognstick], fx.Page.ToCompany.AllOptions.Select(o => (CompanyLocation)o.Key));
        Assert.Equal(["bricks", "machinery"], Keys(fx.Page.Cargo).Order().ToArray());
    }

    [Fact]
    public async Task Picker_NullSelectionFromTheControl_IsNotAChoice()
    {
        var fx = await Open();
        Pick(fx.Page.FromCountry, "bulgaria");

        fx.Page.FromCountry.Selected = null;

        Assert.Equal("bulgaria", fx.Page.FromCountry.Selected!.Key);
    }

    [Fact]
    public async Task Choosing_AField_DoesNotOpenTheNextPopup()
    {
        var fx = await Open();

        Pick(fx.Page.FromCountry, "bulgaria");

        Assert.False(fx.Page.FromCity.IsOpen);
        Assert.False(fx.Page.FromCompany.IsOpen);
    }

    [Fact]
    public async Task OnlyOption_IsChosenAutomatically_AndMarked()
    {
        var fx = await Open();

        Pick(fx.Page.FromCountry, "turkey");

        Assert.Equal("istanbul", fx.Page.FromCity.Selected!.Key);
        Assert.True(fx.Page.FromCity.IsSingleOption);
        Assert.Equal(JobsSample.Lognstick, (CompanyLocation)fx.Page.FromCompany.Selected!.Key);
        Assert.True(fx.Page.FromCompany.IsSingleOption);
        Assert.False(fx.Page.FromCountry.IsSingleOption);
    }

    // ---- vaga

    [Fact]
    public async Task Slot_WithFreeSlotAndAnOffer_IsShown_AndDefaultsToTheFreeSlot()
    {
        var fx = await Open();
        Pick(fx.Page.FromCountry, "bulgaria");

        Pick(fx.Page.FromCompany, JobsSample.Radus);

        Assert.True(fx.Page.ShowSlot);
        Assert.Equal([Free, JobSlotChoice.Replace("_nameless.a")], fx.Page.Slot.AllOptions.Select(o => (JobSlotChoice)o.Key));
        Assert.Equal(Free, fx.Page.Slot.Selected!.Key);
        Assert.Equal("Free slot (2)", fx.Page.Slot.AllOptions[0].Primary);
    }

    [Fact]
    public async Task Slot_WithASingleOption_IsHidden()
    {
        var fx = await Open();
        Pick(fx.Page.FromCountry, "bulgaria");

        Pick(fx.Page.FromCompany, JobsSample.Kaarfor);

        Assert.False(fx.Page.ShowSlot);
    }

    [Fact]
    public async Task Slot_OffersOnly_StartsEmpty_AndTheMissingBarAsksForIt()
    {
        var fx = await Open();
        Pick(fx.Page.FromCountry, "turkey");
        Pick(fx.Page.ToCountry, "serbia");
        Pick(fx.Page.Cargo, "bricks");

        Assert.True(fx.Page.ShowSlot);
        Assert.Null(fx.Page.Slot.Selected);
        Assert.Equal(2, fx.Page.Slot.AllOptions.Count);
        Assert.Equal("Still to choose: slot", fx.Page.MissingText);
        Assert.False(fx.Page.AddCommand.CanExecute(null));
    }

    // ---- barra "Falta escolher"

    [Fact]
    public async Task Missing_OnAFreshForm_GroupsWhenThereAreMoreThanThree()
    {
        var fx = await Open();

        Assert.False(fx.Page.AddCommand.CanExecute(null));
        Assert.Equal("Still to choose: departure, arrival, cargo", fx.Page.MissingText);
    }

    [Fact]
    public async Task Missing_OnlyArrivalAndCargoLeft_GroupsJustThose()
    {
        var fx = await Open();
        Pick(fx.Page.FromCountry, "bulgaria");
        Pick(fx.Page.FromCompany, JobsSample.Radus);

        Assert.Equal("Still to choose: arrival, cargo", fx.Page.MissingText);
    }

    [Fact]
    public async Task Missing_UpToThreeFields_ListsThemInChainOrder()
    {
        var fx = await Open(countries: CountryLayout.None);
        Pick(fx.Page.FromCity, "sofia");
        Pick(fx.Page.FromCompany, JobsSample.Radus);

        Assert.Equal("Still to choose: arrival city, arrival company, cargo", fx.Page.MissingText);
    }

    [Fact]
    public async Task Missing_OnlyCargoLeft_NamesIt()
    {
        var fx = await Open();
        Pick(fx.Page.FromCountry, "bulgaria");
        Pick(fx.Page.FromCompany, JobsSample.Radus);
        Pick(fx.Page.ToCountry, "turkey");

        Assert.Equal("Still to choose: cargo", fx.Page.MissingText);
    }

    [Fact]
    public async Task Ready_EnablesAdd_WithTheReadyText()
    {
        var fx = await Open();

        fx.Ready();

        Assert.True(fx.Page.AddCommand.CanExecute(null));
        Assert.Equal("Ready. Nothing is written now: the job goes to review.", fx.Page.MissingText);
        Assert.True(fx.Page.ShowDetails);
        Assert.True(fx.Page.FromDone);
        Assert.True(fx.Page.ToDone);
        Assert.True(fx.Page.CargoDone);
    }

    [Fact]
    public async Task Details_WithoutCargo_AreHidden()
    {
        var fx = await Open();

        Assert.False(fx.Page.ShowDetails);
        Assert.False(fx.Page.FromDone);
    }

    // ---- avisos de limpeza (só por ação do usuário)

    [Fact]
    public async Task Notice_AutoPickedByTheUser_SaysOnlyOneOptionWasLeft()
    {
        var fx = await Open();

        Pick(fx.Page.FromCountry, "turkey");

        Assert.Contains("only one option was left", fx.Page.Notice);
    }

    [Fact]
    public async Task Notice_ChangingTheDepartureCountry_SaysWhatWasCleared()
    {
        var fx = await Open();
        fx.Ready();

        Pick(fx.Page.FromCountry, "serbia");

        Assert.True(fx.Page.HasNotice);
        Assert.Contains("Cleared", fx.Page.Notice);
    }

    [Fact]
    public async Task Notice_DisappearsOnTheNextChoice()
    {
        var fx = await Open();
        Pick(fx.Page.FromCountry, "turkey");
        Assert.True(fx.Page.HasNotice);

        Pick(fx.Page.Slot, JobSlotChoice.Replace("_nameless.c"));

        Assert.False(fx.Page.HasNotice);
    }

    [Fact]
    public async Task ChangingTheArrivalCountry_KeepsTheDeparture()
    {
        var fx = await Open();
        fx.Ready();

        Pick(fx.Page.ToCountry, "serbia");

        Assert.Equal("sofia", fx.Page.FromCity.Selected!.Key);
        Assert.Equal(JobsSample.Radus, (CompanyLocation)fx.Page.FromCompany.Selected!.Key);
        Assert.Equal("belgrad", fx.Page.ToCity.Selected!.Key);
        Assert.Equal(JobsSample.Tradeaux, (CompanyLocation)fx.Page.ToCompany.Selected!.Key);
    }

    // ---- foco

    [Fact]
    public async Task FocusRequested_PointsToTheNextIncompleteField()
    {
        var fx = await Open();
        var targets = new List<JobsFocusTarget>();
        fx.Page.FocusRequested += (_, t) => targets.Add(t);

        Pick(fx.Page.FromCountry, "bulgaria");
        Pick(fx.Page.FromCompany, JobsSample.Radus);
        Pick(fx.Page.ToCountry, "turkey");

        Assert.Equal([JobsFocusTarget.FromCompany, JobsFocusTarget.ToCountry, JobsFocusTarget.Cargo], targets);
    }

    [Fact]
    public async Task FocusRequested_AfterADepartureWithOnlyOffers_PointsToTheSlot()
    {
        var fx = await Open();
        var targets = new List<JobsFocusTarget>();
        fx.Page.FocusRequested += (_, t) => targets.Add(t);

        Pick(fx.Page.FromCountry, "turkey");

        Assert.Equal([JobsFocusTarget.Slot], targets);
    }

    // ---- trocar

    [Fact]
    public async Task Swap_WithoutBothCities_IsDisabled()
    {
        var fx = await Open();
        Pick(fx.Page.FromCountry, "bulgaria");

        Assert.False(fx.Page.SwapCommand.CanExecute(null));
    }

    [Fact]
    public async Task Swap_ExchangesCountriesAndCities_AndClearsTheDirectionalChoices()
    {
        var fx = await Open();
        fx.Ready();
        Assert.True(fx.Page.SwapCommand.CanExecute(null));

        fx.Page.SwapCommand.Execute(null);

        Assert.Equal("turkey", fx.Page.FromCountry.Selected!.Key);
        Assert.Equal("istanbul", fx.Page.FromCity.Selected!.Key);
        Assert.Equal("bulgaria", fx.Page.ToCountry.Selected!.Key);
        Assert.Equal("sofia", fx.Page.ToCity.Selected!.Key);
        Assert.Null(fx.Page.ToCompany.Selected);
        Assert.Null(fx.Page.Cargo.Selected);
    }

    // ---- adicionar

    [Fact]
    public async Task Add_StagesOneChangePerSlot_AndToasts()
    {
        var fx = await Open();
        fx.Ready();

        fx.Page.AddCommand.Execute(null);

        var change = Assert.Single(fx.Session.Pending);
        Assert.Equal("jobs:slot:_nameless.f1", change.Key);
        var toast = Assert.Single(fx.Toast.Shown);
        Assert.Equal("Jobs_Added_Toast", toast.Text.Key);
        Assert.Equal("Jobs_Added_View", toast.Label!.Key);
        toast.Action!();
        Assert.Equal(1, fx.Navigation.Reviews);
    }

    [Fact]
    public async Task Add_Success_KeepsCountriesCitiesAndDepartureCompany_AndClearsTheCargo()
    {
        var fx = await Open();
        fx.Ready();

        fx.Page.AddCommand.Execute(null);

        Assert.Equal("bulgaria", fx.Page.FromCountry.Selected!.Key);
        Assert.Equal("sofia", fx.Page.FromCity.Selected!.Key);
        Assert.Equal(JobsSample.Radus, (CompanyLocation)fx.Page.FromCompany.Selected!.Key);
        Assert.Equal("turkey", fx.Page.ToCountry.Selected!.Key);
        Assert.Equal("istanbul", fx.Page.ToCity.Selected!.Key);
        Assert.Null(fx.Page.Cargo.Selected);
        Assert.False(fx.Page.AddCommand.CanExecute(null));
    }

    [Fact]
    public async Task Add_Twice_UsesTheNextFreeSlot_NeverASlotOfAnotherPendingChange()
    {
        var fx = await Open();
        fx.Ready();
        fx.Page.AddCommand.Execute(null);
        Pick(fx.Page.Cargo, "machinery");

        Assert.Equal("Free slot (1)", fx.Page.Slot.AllOptions[0].Primary);
        fx.Page.AddCommand.Execute(null);

        Assert.Equal(["jobs:slot:_nameless.f1", "jobs:slot:_nameless.f2"], fx.Session.Pending.Select(c => c.Key));
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
    }

    [Fact]
    public async Task Add_ReplacingAnOffer_StagesTheOffersSlot()
    {
        var fx = await Open();
        Pick(fx.Page.FromCountry, "turkey");
        Pick(fx.Page.Slot, JobSlotChoice.Replace("_nameless.d"));
        Pick(fx.Page.ToCountry, "serbia");
        Pick(fx.Page.Cargo, "bricks");

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
            Pick(fx.Page.FromCountry, "turkey");
            Pick(fx.Page.Slot, JobSlotChoice.Replace("_nameless.c"));
            Pick(fx.Page.ToCountry, "serbia");
            Pick(fx.Page.Cargo, "bricks");
        }
        else fx.Ready();

        fx.Page.AddCommand.Execute(null);

        Assert.True(fx.Page.HasError);
        Assert.Equal(fx.Loc[key], fx.Page.ErrorMessage);
        Assert.Empty(fx.Toast.Shown);
        Assert.True(fx.Page.AddCommand.CanExecute(null)); // as escolhas ficam para o usuário corrigir
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
    public async Task Pending_IsListedOnlyForTheDepartureCity_WithEditAndDiscard()
    {
        var fx = await WithPending();

        var row = Assert.Single(fx.Page.PendingRows);
        Assert.Equal("radus · Sófia → lognstick · istanbul", row.Title);
        Assert.Equal("machinery · 980 km · 72 h · Free slot", row.Detail);
        Assert.True(fx.Page.ShowPendingCard);
        Pick(fx.Page.FromCountry, "serbia");
        Assert.Empty(fx.Page.PendingRows);
    }

    [Fact]
    public async Task Edit_LocksTheDeparture_ButKeepsTheArrivalEditable()
    {
        var fx = await WithPending();

        fx.Page.PendingRows[0].EditCommand.Execute(null);

        Assert.True(fx.Page.IsEditing);
        Assert.True(fx.Page.IsDepartureLocked);
        Assert.True(fx.Page.FromCountry.IsLocked);
        Assert.True(fx.Page.FromCity.IsLocked);
        Assert.True(fx.Page.FromCompany.IsLocked);
        Assert.True(fx.Page.Slot.IsLocked);
        Assert.Equal("Locked while editing", fx.Page.FromCompany.Hint);
        Assert.False(fx.Page.ToCountry.IsLocked);
        Assert.True(fx.Page.ToCountry.IsEnabled);
        Assert.True(fx.Page.Cargo.IsEnabled);
        Assert.Equal("Update in review", fx.Page.AddText);
        Assert.Equal("machinery", fx.Page.Cargo.Selected!.Key);
        Assert.False(fx.Page.SwapCommand.CanExecute(null));
        Assert.False(fx.Page.ShowPendingCard);
    }

    [Fact]
    public async Task Edit_OfACompanyWithASingleSlot_ShowsTheLockedSlot()
    {
        var fx = await Open();
        Pick(fx.Page.FromCountry, "bulgaria");
        Pick(fx.Page.FromCompany, JobsSample.Kaarfor);
        Pick(fx.Page.ToCountry, "turkey");
        Pick(fx.Page.Cargo, "machinery");
        fx.Page.AddCommand.Execute(null);
        Assert.False(fx.Page.ShowSlot);

        fx.Page.PendingRows[0].EditCommand.Execute(null);

        Assert.True(fx.Page.ShowSlot);
        Assert.True(fx.Page.Slot.IsLocked);
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
    public async Task Edit_Cancel_LeavesThePendingChange_ClearsTheCargo_AndKeepsTheCities()
    {
        var fx = await WithPending();
        fx.Page.PendingRows[0].EditCommand.Execute(null);

        fx.Page.CancelEditCommand.Execute(null);

        Assert.False(fx.Page.IsEditing);
        Assert.False(fx.Page.IsDepartureLocked);
        Assert.False(fx.Page.FromCity.IsLocked);
        Assert.Single(fx.Session.Pending);
        Assert.Null(fx.Page.Cargo.Selected);
        Assert.Equal("sofia", fx.Page.FromCity.Selected!.Key);
        Assert.Equal("istanbul", fx.Page.ToCity.Selected!.Key);
    }

    [Fact]
    public async Task Edit_OfAReplacement_KeepsTheOfferFixed()
    {
        var fx = await Open();
        Pick(fx.Page.FromCountry, "turkey");
        Pick(fx.Page.Slot, JobSlotChoice.Replace("_nameless.d"));
        Pick(fx.Page.ToCountry, "serbia");
        Pick(fx.Page.Cargo, "bricks");
        fx.Page.AddCommand.Execute(null);

        fx.Page.PendingRows[0].EditCommand.Execute(null);

        Assert.True(fx.Page.Slot.IsLocked);
        Assert.Equal(JobSlotChoice.Replace("_nameless.d"), fx.Page.Slot.Selected!.Key);
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
    public async Task DepartureCity_WithAllSlotsInPendingChanges_StaysSelected_WithNoUsableCompany()
    {
        var fx = await Open();
        Pick(fx.Page.FromCountry, "turkey");
        Pick(fx.Page.Slot, JobSlotChoice.Replace("_nameless.c"));
        Pick(fx.Page.ToCountry, "serbia");
        Pick(fx.Page.Cargo, "bricks");
        fx.Page.AddCommand.Execute(null);
        Pick(fx.Page.Cargo, "bricks");
        fx.Page.AddCommand.Execute(null);

        Assert.Equal("istanbul", fx.Page.FromCity.Selected!.Key);
        Assert.Empty(fx.Page.FromCompany.AllOptions);
        Assert.Null(fx.Page.FromCompany.Selected);
        Assert.Equal(2, fx.Page.PendingRows.Count);
        Assert.False(fx.Page.AddCommand.CanExecute(null));
    }

    // ---- Add com dispatcher que enfileira (como em produção)

    private static async Task<(JobsFixture Fx, QueueUiDispatcher Queue)> OpenQueued()
    {
        var queue = new QueueUiDispatcher();
        var fx = await new JobsFixture(ui: queue).AttachAsync();
        return (fx, queue);
    }

    /// <summary>sofia/kaarfor (uma vaga, sem ofertas) → istanbul/lognstick, machinery.</summary>
    private static void ReadyKaarfor(JobsFixture fx)
    {
        Pick(fx.Page.FromCountry, "bulgaria");
        Pick(fx.Page.FromCity, "sofia");
        Pick(fx.Page.FromCompany, JobsSample.Kaarfor);
        Pick(fx.Page.ToCountry, "turkey");
        Pick(fx.Page.ToCity, "istanbul");
        Pick(fx.Page.ToCompany, JobsSample.Lognstick);
        Pick(fx.Page.Cargo, "machinery");
    }

    [Fact]
    public async Task Add_QueuedDispatcher_ExhaustedDepartureCompany_EndsEmptyWithAddDisabled()
    {
        var (fx, queue) = await OpenQueued();
        ReadyKaarfor(fx);

        fx.Page.AddCommand.Execute(null);
        queue.Drain();

        Assert.Single(fx.Session.Pending);
        Assert.Equal("sofia", fx.Page.FromCity.Selected!.Key);
        Assert.Null(fx.Page.FromCompany.Selected);
        Assert.Null(fx.Page.Slot.Selected);
        Assert.Null(fx.Page.Cargo.Selected);
        Assert.False(fx.Page.AddCommand.CanExecute(null));
        Assert.False(fx.Page.CanAdd);
    }

    [Fact]
    public async Task Add_QueuedDispatcher_CompanyWithMoreSlots_KeepsTheRestLikeTheSyncCase()
    {
        var (fx, queue) = await OpenQueued();
        fx.Ready();

        fx.Page.AddCommand.Execute(null);
        queue.Drain();

        Assert.Equal(JobsSample.Radus, (CompanyLocation)fx.Page.FromCompany.Selected!.Key);
        Assert.Equal("istanbul", fx.Page.ToCity.Selected!.Key);
        Assert.Equal("turkey", fx.Page.ToCountry.Selected!.Key);
        Assert.Null(fx.Page.Cargo.Selected);
        Assert.False(fx.Page.CanAdd);
    }

    [Fact]
    public async Task Add_QueuedDispatcher_StageThrowsNonCoreException_DoesNotLeaveTheNextJobFlagStuck()
    {
        var queue = new QueueUiDispatcher();
        var fx = new JobsFixture(ui: queue);
        var session = new StageThrowsSession(fx.Session, new InvalidOperationException("boom"));
        await fx.Page.AttachAsync(session, default);
        fx.Ready();

        Assert.Throws<InvalidOperationException>(() => fx.Page.AddCommand.Execute(null));
        session.RaiseChanged();
        queue.Drain();

        Assert.Equal("machinery", fx.Page.Cargo.Selected!.Key); // revalidou; não aplicou "próximo frete"
    }

    [Fact]
    public async Task Add_QueuedDispatcher_DistanceRefreshQueuedBefore_DoesNotSwallowTheAddEffect()
    {
        var queue = new QueueUiDispatcher();
        var fx = new JobsFixture(ui: queue);
        fx.Factory.RefreshAdds = 1; // o RefreshDistances já enfileira um Rebuild ao abrir
        await fx.AttachAsync();
        ReadyKaarfor(fx);

        fx.Page.AddCommand.Execute(null);
        queue.Drain();

        // um Rebuild seguinte pode escolher sozinha a única empresa restante (opção única); a carga recomeça
        Assert.Single(fx.Session.Pending);
        Assert.Null(fx.Page.Cargo.Selected);
        Assert.False(fx.Page.CanAdd);
    }

    // ---- sem vaga, erro de carga, aviso

    [Fact]
    public async Task Attach_WarmUpFails_GoesToErrorState_AndRetryReloads()
    {
        var fx = new JobsFixture();
        fx.Factory.WarmUpFails = new IOException("disco");

        await fx.AttachAsync();
        Assert.Equal(PageState.Error, fx.Page.State);
        fx.Factory.WarmUpFails = null;
        fx.Page.RetryCommand.Execute(null);

        Assert.Equal(PageState.Ready, fx.Page.State);
    }

    [Fact]
    public async Task Attach_CreateFails_GoesToErrorState_AndRetryReloads()
    {
        var fx = new JobsFixture();
        fx.Factory.CreateFails = new InvalidOperationException("indice");

        await fx.AttachAsync();
        Assert.Equal(PageState.Error, fx.Page.State);
        fx.Factory.CreateFails = null;
        fx.Page.RetryCommand.Execute(null);

        Assert.Equal(PageState.Ready, fx.Page.State);
        Assert.NotEmpty(fx.Page.FromCountry.AllOptions);
    }

    [Fact]
    public async Task Notice_SlotAutoPicked_NeverMentionsTheSlot()
    {
        var fx = await Open();
        Pick(fx.Page.FromCountry, "bulgaria");
        Pick(fx.Page.FromCity, "sofia");

        Pick(fx.Page.FromCompany, JobsSample.Kaarfor); // vaga única, escolhida sozinha

        Assert.DoesNotContain(fx.Loc["Jobs_Missing_Slot"], fx.Page.Notice ?? "", StringComparison.OrdinalIgnoreCase);
    }

    // ---- distância

    [Fact]
    public async Task Distance_Known_ShowsKmWithoutTag()
    {
        var fx = await Open();
        Pick(fx.Page.FromCountry, "bulgaria");
        Pick(fx.Page.FromCompany, JobsSample.Radus);
        Pick(fx.Page.ToCountry, "serbia");

        Assert.Equal("394 km", fx.Page.DistanceValue);
        Assert.Null(fx.Page.DistanceTag);
        Assert.Equal("394 km", fx.Page.MapDistanceText);
    }

    [Fact]
    public async Task Distance_Estimated_ShowsApproxWithTag()
    {
        var fx = await Open();
        Pick(fx.Page.FromCountry, "bulgaria");
        Pick(fx.Page.FromCompany, JobsSample.Radus);
        Pick(fx.Page.ToCountry, "");

        Assert.Equal("praha", fx.Page.ToCity.Selected!.Key);
        Assert.StartsWith("≈ ", fx.Page.DistanceValue);
        Assert.Equal("Estimated", fx.Page.DistanceTag);
        Assert.EndsWith(" · estimated", fx.Page.MapDistanceText);
    }

    [Fact]
    public async Task Distance_BeforeTheArrivalCity_ShowsAPlaceholder()
    {
        var fx = await Open();
        Pick(fx.Page.FromCountry, "bulgaria");

        Assert.Equal("—", fx.Page.DistanceValue);
        Assert.Null(fx.Page.MapOrigin);
    }

    [Fact]
    public async Task ToCityOptions_WithoutCountries_PutKnownDistancesFirst_ThenEstimatedWithTag()
    {
        var fx = await Open(countries: CountryLayout.None);
        Pick(fx.Page.FromCity, "sofia");
        Pick(fx.Page.FromCompany, JobsSample.Radus);

        var options = fx.Page.ToCity.AllOptions.ToList();

        Assert.Equal(["belgrad", "istanbul", "praha"], options.Select(o => (string)o.Key));
        Assert.Equal("394 km", options[0].Trailing);
        Assert.Null(options[0].Tag);
        Assert.StartsWith("≈", options[2].Trailing);
        Assert.Equal("Estimated", options[2].Tag);
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

    // ---- avisos, sessão, idioma

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
        Assert.Empty(fx.Page.FromCountry.AllOptions);
        Assert.Empty(fx.Page.FromCity.AllOptions);
    }

    [Fact]
    public async Task ChangingTheLanguage_RefreshesTheLabels_AndKeepsTheChoices()
    {
        var fx = await Open(language: Language.PtBr);
        Pick(fx.Page.FromCountry, "bulgaria");
        Assert.Equal("Cidade", fx.Page.FromCity.Label);

        fx.Loc.SetLanguage(Language.En);

        Assert.Equal("City", fx.Page.FromCity.Label);
        Assert.Equal("Country", fx.Page.FromCountry.Label);
        Assert.Equal("bulgaria", fx.Page.FromCountry.Selected!.Key);
        Assert.Equal("sofia", fx.Page.FromCity.Selected!.Key);
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
