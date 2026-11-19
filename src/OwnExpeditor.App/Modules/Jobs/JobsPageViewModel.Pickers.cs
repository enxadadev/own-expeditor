using System;
using System.Collections.Generic;
using System.Linq;
using OwnExpeditor.App.Controls;
using OwnExpeditor.App.ViewModels;
using OwnExpeditor.Core.Modules.Jobs;
using OwnExpeditor.Domain.Games;

namespace OwnExpeditor.App.Modules.Jobs;

/// <summary>
/// Repopulação dos oito seletores a partir da <see cref="JobSelection"/> (design/fretes-pais-cidade/flows.md). Chaves das
/// opções: país = id (<see cref="JobBoard.OtherCountry"/> = Outros), cidade = id, empresa = <see cref="CompanyLocation"/>,
/// vaga = <see cref="JobSlotChoice"/>, carga = id.
/// </summary>
public sealed partial class JobsPageViewModel
{
    private readonly Dictionary<JobPickerViewModel, string> _sides = [];
    private IReadOnlyList<JobField> _missing = [];
    private RouteDistance? _distance;
    private bool _hasDepartureCompanies = true;

    private IEnumerable<JobPickerViewModel> Pickers => [FromCountry, FromCity, FromCompany, Slot, ToCountry, ToCity, ToCompany, Cargo];

    public bool IsEditing => _editing is not null;

    private string CityName(string id) => _board!.Names.Get(GameNameKind.City, id);

    private string CompanyName(CompanyLocation c) => _board!.Names.Get(GameNameKind.Company, c.Company);

    private string CargoName(string id) => _board!.Names.Get(GameNameKind.Cargo, id);

    private string CountryName(string id) =>
        id == JobBoard.OtherCountry ? Loc["Jobs_Country_Other"] : _board!.Names.Get(GameNameKind.Country, id);

    private IEnumerable<(string Key, Staged Staged)> PendingJobs()
    {
        if (_session is null) return [];
        return _session.Pending.Where(c => c.ModuleId == Id && _staged.ContainsKey(c.Key)).Select(c => (c.Key, _staged[c.Key]));
    }

    /// <summary>Recalcula tudo a partir do board atual e da seleção: nada aqui chega ao save.</summary>
    private void Refresh(bool revalidate = true)
    {
        if (_board is null || _session is null)
        {
            Notify();
            return;
        }

        _refreshing = true;
        try
        {
            if (_editing is not null && _session.Pending.All(c => c.Key != _editingKey))
            {
                _editing = null;
                _editingKey = null;
            }

            if (revalidate) _sel = JobSelectionRules.Revalidate(_board, _sel, IsEditing).Selection; // silencioso: só o usuário gera aviso
            _missing = JobSelectionRules.Missing(_board, _sel);
            _distance = _sel.FromCity is not null && _sel.ToCity is not null ? _board.Distance(_sel.FromCity, _sel.ToCity) : null;
            _hasDepartureCompanies = IsEditing || _sel.FromCity is null || _board.DepartureCompanies(_sel.FromCity).Count > 0;
            BuildPickers();
            _pendingRows = BuildPending();
            _expirationChoices = [.. JobPlanner.AllowedExpirationHours.Select(h => new JobExpirationChoice(h, HoursText(h)))];
            State = _board.DepartureCities(null).Count == 0 && !PendingJobs().Any() ? PageState.Empty : PageState.Ready;
            Notify();
        }
        finally
        {
            _refreshing = false;
        }
    }

    private void Notify()
    {
        SwapCommand.NotifyCanExecuteChanged();
        AddCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(string.Empty);
    }

    private JobsFocusTarget NextFocus() => _missing.Count == 0 ? JobsFocusTarget.Add : _missing[0] switch
    {
        JobField.FromCountry => JobsFocusTarget.FromCountry,
        JobField.FromCity => JobsFocusTarget.FromCity,
        JobField.FromCompany => JobsFocusTarget.FromCompany,
        JobField.Slot => JobsFocusTarget.Slot,
        JobField.ToCountry => JobsFocusTarget.ToCountry,
        JobField.ToCity => JobsFocusTarget.ToCity,
        JobField.ToCompany => JobsFocusTarget.ToCompany,
        _ => JobsFocusTarget.Cargo,
    };

    // ---- opções dos seletores

    private void BuildPickers()
    {
        var b = _board!;
        var s = _sel;
        var culture = StringComparer.Create(Loc.Culture, ignoreCase: true);
        var locked = IsEditing;
        var countries = b.HasCountries;
        var lockedHint = Loc["Jobs_Hint_Locked"];

        // ---- saída
        Setup(FromCountry, "Jobs_Side_From", "Jobs_Field_Country", "Jobs_Country_Placeholder", "Jobs_Country_Search",
            locked ? LockedCountry(s.FromCountry) : CountryOptions(b.DepartureCountries(), culture), s.FromCountry);
        Enable(FromCountry, !locked, null, locked, lockedHint);

        var cityOptions = locked ? LockedCity(s.FromCity)
            : !countries || s.FromCountry is not null ? b.DepartureCities(countries ? s.FromCountry : null).Select(DepartureCityOption).OrderBy(o => o.Primary, culture).ToList() : [];
        Setup(FromCity, "Jobs_Side_From", "Jobs_Field_City", "Jobs_City_Placeholder", "Jobs_Picker_SearchCity", cityOptions, s.FromCity);
        Enable(FromCity, !locked && (!countries || s.FromCountry is not null), "Jobs_Hint_ChooseCountry", locked, lockedHint);

        var companies = locked ? s.FromCompany is { } fc ? [fc] : []
            : s.FromCity is null ? [] : b.DepartureCompanies(s.FromCity);
        var companyOptions = companies.Select(c => new PickerOption(c, CompanyName(c), locked ? CityName(c.City) : SlotsOffersMeta(c)))
            .OrderBy(o => o.Primary, culture).ToList();
        Setup(FromCompany, "Jobs_Side_From", "Jobs_Field_Company", "Jobs_Company_Placeholder", "Jobs_Picker_SearchCompany", companyOptions, s.FromCompany);
        Enable(FromCompany, !locked && s.FromCity is not null, "Jobs_Hint_ChooseCity", locked, lockedHint);

        Setup(Slot, "Jobs_Side_From", "Jobs_Field_Slot", "Jobs_Slot_Placeholder", "Jobs_Slot_Search", SlotOptions(), s.Slot);
        Enable(Slot, !locked && s.FromCompany is not null, "Jobs_Hint_ChooseFromCompany", locked, lockedHint);

        // ---- chegada (depende da empresa de saída)
        var from = s.FromCompany;
        Setup(ToCountry, "Jobs_Side_To", "Jobs_Field_Country", "Jobs_Country_Placeholder", "Jobs_Country_Search",
            from is null ? [] : CountryOptions(b.ArrivalCountries(from), culture), s.ToCountry);
        Enable(ToCountry, from is not null, "Jobs_Hint_ChooseFromCompany");

        var toCityOptions = from is null || (countries && s.ToCountry is null) ? []
            : ArrivalCityOptions(from, countries ? s.ToCountry : null, culture);
        Setup(ToCity, "Jobs_Side_To", "Jobs_Field_City", "Jobs_City_Placeholder", "Jobs_Picker_SearchCity", toCityOptions, s.ToCity);
        Enable(ToCity, from is not null && (!countries || s.ToCountry is not null),
            from is null ? "Jobs_Hint_ChooseFromCompany" : "Jobs_Hint_ChooseCountry");

        var toCompanyOptions = from is null || s.ToCity is null ? []
            : b.ArrivalCompanies(from, s.ToCity).Select(c => new PickerOption(c, CompanyName(c), CommonCargos(from, c)))
                .OrderBy(o => o.Primary, culture).ToList();
        Setup(ToCompany, "Jobs_Side_To", "Jobs_Field_Company", "Jobs_Company_Placeholder", "Jobs_Picker_SearchCompany", toCompanyOptions, s.ToCompany);
        Enable(ToCompany, from is not null && s.ToCity is not null, from is null ? "Jobs_Hint_ChooseFromCompany" : "Jobs_Hint_ChooseCity");

        // ---- carga
        var cargoOptions = from is null || s.ToCompany is null ? []
            : b.Cargos(from, s.ToCompany).Select(c => new PickerOption(c, CargoName(c), TrailerName(c))).OrderBy(o => o.Primary, culture).ToList();
        Setup(Cargo, "Jobs_Step_Cargo", "Jobs_Step_Cargo", "Jobs_Cargo_Placeholder", "Jobs_Picker_SearchCargo", cargoOptions, s.Cargo);
        Enable(Cargo, s.ToCompany is not null, "Jobs_Hint_ChooseToCompany");
    }

    private void Setup(JobPickerViewModel picker, string sideKey, string labelKey, string placeholderKey, string searchKey,
        IReadOnlyList<PickerOption> options, object? selected)
    {
        picker.Label = Loc[labelKey];
        picker.Placeholder = Loc[placeholderKey];
        picker.SearchWatermark = Loc[searchKey];
        picker.EmptyText = Loc["Jobs_Picker_NoItems"];
        picker.Set(options, selected);
        _sides[picker] = sideKey; // o nome acessível é completado em Enable, quando o estado do campo é conhecido
    }

    /// <summary>Habilitação, dica (campo desabilitado ou travado), etiqueta e nome acessível do seletor.</summary>
    private void Enable(JobPickerViewModel picker, bool enabled, string? blockedHintKey, bool locked = false, string? lockedHint = null)
    {
        picker.IsEnabled = enabled;
        picker.IsLocked = locked;
        picker.IsSingleOption = enabled && picker.Selected is not null && picker.AllOptions.Count(o => o.IsEnabled) == 1;
        picker.Hint = locked ? lockedHint : enabled || blockedHintKey is null ? null : Loc[blockedHintKey];
        picker.Badge = locked ? lockedHint : picker.IsSingleOption ? Loc["Jobs_Single_Option"] : null;
        if (!enabled && !locked && picker.Hint is not null) picker.Placeholder = picker.Hint; // a dica ocupa o lugar do valor
        var value = picker.Selected?.Primary ?? picker.Placeholder;
        picker.AutomationName = Loc.Get("Jobs_Field_Aria", Loc[_sides[picker]], picker.Label, value);
    }

    private List<PickerOption> CountryOptions(IReadOnlyList<CountryInfo> countries, StringComparer culture) =>
        [.. countries.Where(c => c.Country != JobBoard.OtherCountry).Select(CountryOption).OrderBy(o => o.Primary, culture),
            .. countries.Where(c => c.Country == JobBoard.OtherCountry).Select(CountryOption)];

    private PickerOption CountryOption(CountryInfo c) =>
        new(c.Country, CountryName(c.Country), null, null, true, Loc.GetPlural("Jobs_Country_Cities", c.Cities));

    private List<PickerOption> LockedCountry(string? country) =>
        country is null ? [] : [new PickerOption(country, CountryName(country))];

    private List<PickerOption> LockedCity(string? city) => city is null ? [] : [new PickerOption(city, CityName(city))];

    /// <summary>"2 empresas · 4 vagas livres · 3 ofertas" (ou "Sem vagas livres") e o selo de pendentes.</summary>
    private PickerOption DepartureCityOption(DepartureCity c)
    {
        var parts = new List<string>();
        if (c.Companies > 0) parts.Add(Loc.GetPlural("Jobs_Companies", c.Companies));
        parts.Add(Loc.GetPlural("Jobs_Slots", c.FreeSlots));
        if (c.Offers > 0) parts.Add(Loc.GetPlural("Jobs_Offers", c.Offers));
        var pending = c.PendingSlots > 0 ? Loc.GetPlural("Jobs_Pending", c.PendingSlots) : null;
        return new PickerOption(c.City, CityName(c.City), string.Join(" · ", parts), pending);
    }

    private string SlotsOffersMeta(CompanyLocation c)
    {
        var offers = _board!.ReplaceableOffers(c).Count;
        var parts = new List<string> { Loc.GetPlural("Jobs_Slots", _board.FreeSlots(c)) };
        if (offers > 0) parts.Add(Loc.GetPlural("Jobs_Offers", offers));
        return string.Join(" · ", parts);
    }

    /// <summary>Cidades de chegada: distância conhecida primeiro, "≈ n km" com a etiqueta Estimada depois; nome dentro de cada grupo.</summary>
    private List<PickerOption> ArrivalCityOptions(CompanyLocation from, string? country, StringComparer culture)
    {
        var b = _board!;
        return b.ArrivalCities(from, country).Select(city =>
        {
            var d = b.Distance(from.City, city);
            var estimated = d.Source == DistanceSource.Estimated;
            var meta = Loc.GetPlural("Jobs_Companies", b.ArrivalCompanies(from, city).Count);
            return (Option: new PickerOption(city, CityName(city), meta, estimated ? Loc["Jobs_Distance_Estimated"] : null, true, KmText(d)),
                Known: !estimated);
        }).OrderByDescending(r => r.Known).ThenBy(r => r.Option.Primary, culture).Select(r => r.Option).ToList();
    }

    private string CommonCargos(CompanyLocation from, CompanyLocation to) =>
        string.Join(", ", _board!.Cargos(from, to).Select(CargoName).Order(StringComparer.Create(Loc.Culture, ignoreCase: true)));

    /// <summary>"Vaga livre (n)" (padrão, se houver) e uma linha por oferta a substituir; na edição, só a vaga em uso.</summary>
    private List<PickerOption> SlotOptions()
    {
        var b = _board!;
        if (IsEditing)
        {
            if (_sel.Slot is not { } slot) return [];
            return slot.Mode == JobSlotMode.FillEmpty
                ? [new PickerOption(slot, Loc.Get("Jobs_Slot_Free", 1))]
                : _editing!.Offer is { } offer ? [OfferOption(offer)] : [new PickerOption(slot, Loc["Jobs_Field_Slot"])];
        }
        if (_sel.FromCompany is not { } from) return [];
        var options = new List<PickerOption>();
        var free = b.FreeSlots(from);
        if (free > 0) options.Add(new PickerOption(JobSlotChoice.Free, Loc.Get("Jobs_Slot_Free", free)));
        options.AddRange(b.ReplaceableOffers(from).Select(OfferOption));
        return options;
    }

    private PickerOption OfferOption(ExistingOffer offer)
    {
        var target = offer.Target;
        var primary = Loc.Get("Jobs_Slot_Replace", CityName(target.City), CargoName(offer.Cargo), Loc.Get("Format_Km", offer.DistanceKm));
        var place = $"{CompanyName(target)} · {CityName(target.City)}";
        var secondary = offer.ExpiresIn is { } left ? Loc.Get("Jobs_Slot_Meta", place, Duration(left)) : place;
        return new PickerOption(JobSlotChoice.Replace(offer.OfferId), primary, secondary);
    }

    /// <summary>Tempo restante de uma oferta (horas de jogo; dias a partir de 48 h).</summary>
    private string Duration(TimeSpan span) =>
        span.TotalHours >= 48 ? Loc.Get("Format_Days", (int)Math.Ceiling(span.TotalDays)) : Loc.Get("Format_Hours", (int)Math.Ceiling(span.TotalHours));

    private string TrailerName(string cargo) =>
        _board!.Template(cargo) is { } t ? _board.Names.Get(GameNameKind.Trailer, TrailerId(t)) : "";

    private static string TrailerId(JobTemplate t) =>
        t.TrailerVariant.StartsWith("trailer.", StringComparison.Ordinal) ? t.TrailerVariant["trailer.".Length..] : t.TrailerVariant;

    private string KmText(RouteDistance d) => Loc.Get(d.Source == DistanceSource.Estimated ? "Format_KmApprox" : "Format_Km", d.Km);
}
