using System;
using System.Collections.Generic;
using System.Linq;
using OwnExpeditor.App.Controls;
using OwnExpeditor.App.ViewModels;
using OwnExpeditor.Core.Modules.Jobs;
using OwnExpeditor.Core.Session;
using OwnExpeditor.Domain.Games;

namespace OwnExpeditor.App.Modules.Jobs;

/// <summary>Revalidação em cadeia das escolhas e montagem das opções (design/fretes/flows.md §2).</summary>
public sealed partial class JobsPageViewModel
{
    private HashSet<string> _originCityIds = [];
    private HashSet<string> _allCityIds = [];
    private HashSet<string> _usableCityIds = [];
    private IReadOnlyList<string> _cities = [];
    private IReadOnlyList<CompanyLocation> _companies = [];
    private IReadOnlyList<string> _cargos = [];
    private IReadOnlyList<CompanyLocation> _toCompanies = [];
    private IReadOnlyList<ExistingOffer> _offers = [];
    private RouteDistance? _distance;
    private bool _hasMatch = true;
    private bool _hasUsableCompanies = true;
    private IReadOnlyList<CityItemViewModel> _allCityItems = [];

    public bool IsEditing => _editing is not null;

    private bool IsOriginCity(string city) => _originCityIds.Contains(city);

    private string CityName(string id) => _board!.Names.Get(GameNameKind.City, id);

    private string CompanyName(CompanyLocation c) => _board!.Names.Get(GameNameKind.Company, c.Company);

    private string CargoName(string id) => _board!.Names.Get(GameNameKind.Cargo, id);

    private IEnumerable<(string Key, Staged Staged)> PendingJobs()
    {
        if (_session is null) return [];
        return _session.Pending.Where(c => c.ModuleId == Id && _staged.ContainsKey(c.Key)).Select(c => (c.Key, _staged[c.Key]));
    }

    /// <summary>Recalcula tudo a partir do board atual e das escolhas: nada aqui chega ao save.</summary>
    private void Refresh()
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

            BuildCities();
            Revalidate();
            BuildPickers();
            _pendingRows = BuildPending();
            BuildChoices();
            State = _allCityIds.Count == 0 && !PendingJobs().Any() ? PageState.Empty : PageState.Ready;
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

    // ---- cidades de saída (passo 1)

    private void BuildCities()
    {
        var b = _board!;
        var pendingByCity = PendingJobs().GroupBy(p => p.Staged.Request.Origin.City).ToDictionary(g => g.Key, g => g.Count());
        var ids = new HashSet<string>(b.OriginCities(), StringComparer.Ordinal);
        ids.UnionWith(pendingByCity.Keys);
        _usableCityIds = new HashSet<string>(b.OriginCities(), StringComparer.Ordinal);
        _originCityIds = ids;
        _allCityIds = ids;
        _allCityItems = ids.Select(id => new CityItemViewModel(id, CityName(id), pendingByCity.GetValueOrDefault(id),
                pendingByCity.TryGetValue(id, out var n) ? Loc.GetPlural("Jobs_Pending", n) : "", () => CityMeta(id)))
            .OrderBy(c => c.Name, StringComparer.Create(Loc.Culture, ignoreCase: true)).ToList();

        var query = JobPickerViewModel.Normalize(_cityQuery);
        _cityItems = query.Length == 0
            ? _allCityItems
            : _allCityItems.Where(c => JobPickerViewModel.Normalize(c.Name).Contains(query, StringComparison.Ordinal)).ToList();
        _citySelection = _cityItems.FirstOrDefault(c => c.Id == _fromCity);
    }

    /// <summary>"2 empresas · 4 vagas livres · 3 ofertas": empresas utilizáveis da cidade (as que têm vaga ou oferta e uma carga que alguém aceita).</summary>
    private (string, bool) CityMeta(string city)
    {
        var b = _board!;
        var companies = b.UsableOriginCompanies(city);
        var slots = companies.Sum(c => b.FreeSlots(c));
        var offers = companies.Sum(c => b.ReplaceableOffers(c).Count);
        var parts = new List<string>();
        if (companies.Count > 0) parts.Add(Loc.GetPlural("Jobs_Companies", companies.Count));
        parts.Add(Loc.GetPlural("Jobs_Slots", slots));
        if (offers > 0) parts.Add(Loc.GetPlural("Jobs_Offers", offers));
        return (string.Join(" · ", parts), slots == 0);
    }

    // ---- revalidação

    private void Revalidate()
    {
        var b = _board!;
        var cleared = new List<string>();
        var picked = new List<string>();
        var sameCity = false;

        _cities = b.Cities();
        if (!IsEditing && _fromCity is not null && !_originCityIds.Contains(_fromCity)) _fromCity = null;
        if (_toCity is not null && !_cities.Contains(_toCity)) _toCity = null;
        if (_fromCity is not null && _fromCity == _toCity)
        {
            _toCity = null;
            sameCity = true;
        }

        // empresa de saída (modo e oferta acompanham)
        _companies = IsEditing ? [_editing!.Request.Origin]
            : _fromCity is not null && _toCity is not null ? b.OriginCompanies(_fromCity, _toCity) : [];
        if (_fromCompany is not null && !_companies.Contains(_fromCompany))
        {
            _fromCompany = null;
            cleared.Add("FromCompany");
        }
        var companyChanged = _fromCompany is null;
        if (_fromCompany is null && _companies.Count == 1 && !IsEditing)
        {
            _fromCompany = _companies[0];
            picked.Add("FromCompany");
        }
        NormalizeMode(forceDefault: companyChanged, cleared, picked);
        _offers = _fromCompany is null ? [] : b.ReplaceableOffers(_fromCompany);

        // carga
        _cargos = _fromCompany is not null && _toCity is not null ? b.Cargos(_fromCompany, _toCity) : [];
        if (_cargo is not null && !_cargos.Contains(_cargo))
        {
            _cargo = null;
            cleared.Add("Cargo");
        }
        if (_cargo is null && _cargos.Count == 1)
        {
            _cargo = _cargos[0];
            picked.Add("Cargo");
        }

        // empresa de chegada
        _toCompanies = _toCity is not null && _cargo is not null ? b.DestinationCompanies(_toCity, _cargo) : [];
        if (_toCompany is not null && !_toCompanies.Contains(_toCompany))
        {
            _toCompany = null;
            cleared.Add("ToCompany");
        }
        if (_toCompany is null && _toCompanies.Count == 1)
        {
            _toCompany = _toCompanies[0];
            picked.Add("ToCompany");
        }

        _distance = _fromCity is not null && _toCity is not null ? b.Distance(_fromCity, _toCity) : null;
        _hasMatch = IsEditing || _fromCity is null || _toCity is null || _companies.Count > 0;
        _hasUsableCompanies = IsEditing || _fromCity is null || _usableCityIds.Contains(_fromCity);

        var notices = new List<string>();
        if (sameCity) notices.Add(Loc["Jobs_Notice_SameCity"]);
        if (cleared.Count > 0) notices.Add(Loc.Get("Jobs_Notice_Cleared", string.Join(", ", cleared.Select(k => Loc["Jobs_Missing_" + k]))));
        if (picked.Count > 0) notices.Add(Loc.Get("Jobs_Notice_Replaced", string.Join(", ", picked.Select(k => Loc[k == "Offer" ? "Jobs_Step_Offer" : "Jobs_Step_" + k]))));
        if (notices.Count > 0) _notice = string.Join(" ", notices);
    }

    /// <summary>
    /// Vaga livre × substituir: padrão = vaga livre se houver, senão substituir; opção sem itens é descartada; oferta única é
    /// escolhida sozinha. Na edição o modo, a oferta e a vaga ficam como estavam.
    /// </summary>
    private void NormalizeMode(bool forceDefault, List<string> cleared, List<string> picked)
    {
        if (IsEditing) return;
        if (_fromCompany is null)
        {
            _mode = JobSlotMode.FillEmpty;
            _offerId = null;
            return;
        }

        var free = _board!.FreeSlots(_fromCompany);
        var offers = _board.ReplaceableOffers(_fromCompany);
        if (forceDefault)
        {
            _mode = free > 0 ? JobSlotMode.FillEmpty : JobSlotMode.ReplaceOffer;
            _offerId = null;
        }
        else if (_mode == JobSlotMode.FillEmpty && free == 0 && offers.Count > 0)
        {
            _mode = JobSlotMode.ReplaceOffer;
            _offerId = null;
        }
        else if (_mode == JobSlotMode.ReplaceOffer && offers.Count == 0)
        {
            _mode = JobSlotMode.FillEmpty;
            if (_offerId is not null) cleared.Add("Offer");
            _offerId = null;
        }

        if (_mode == JobSlotMode.FillEmpty)
        {
            _offerId = null;
            return;
        }
        if (_offerId is not null && offers.All(o => o.OfferId != _offerId))
        {
            _offerId = null;
            cleared.Add("Offer");
        }
        if (_offerId is null && offers.Count == 1)
        {
            _offerId = offers[0].OfferId;
            picked.Add("Offer");
        }
    }

    private JobsFocusTarget? NextIncomplete() =>
        _fromCity is null ? JobsFocusTarget.CityList
        : _toCity is null ? JobsFocusTarget.ToCity
        : !_hasMatch ? null
        : _fromCompany is null ? JobsFocusTarget.FromCompany
        : _mode == JobSlotMode.ReplaceOffer && _offerId is null ? JobsFocusTarget.Offer
        : _cargo is null ? JobsFocusTarget.Cargo
        : _toCompany is null ? JobsFocusTarget.ToCompany
        : null;

    // ---- opções dos seletores

    private void BuildPickers()
    {
        var b = _board!;
        var from = _fromCity;

        // cidade de destino: a de saída no topo e desativada; conhecidas antes das estimadas; "Sem combinação" tem prioridade
        var culture = StringComparer.Create(Loc.Culture, ignoreCase: true);
        var rows = new List<(PickerOption Option, bool Known, string Name)>();
        PickerOption? sameCity = null;
        foreach (var city in _cities)
        {
            var name = CityName(city);
            if (city == from)
            {
                sameCity = new PickerOption(city, name, null, Loc["Jobs_Picker_SameCity"], IsEnabled: false);
                continue;
            }
            if (IsEditing && b.Cargos(_editing!.Request.Origin, city).Count == 0) continue;
            var distance = from is null ? null : b.Distance(from, city);
            var noMatch = !IsEditing && from is not null && !b.HasMatch(from, city);
            var estimated = distance?.Source == DistanceSource.Estimated;
            var tag = noMatch ? Loc["Jobs_Picker_NoMatch"] : estimated ? Loc["Jobs_Distance_Estimated"] : null;
            rows.Add((new PickerOption(city, name, null, tag, IsEnabled: true, Trailing: distance is null ? null : KmText(distance)), !estimated, name));
        }
        var options = new List<PickerOption>();
        if (sameCity is not null) options.Add(sameCity);
        options.AddRange(rows.OrderByDescending(r => r.Known).ThenBy(r => r.Name, culture).Select(r => r.Option));

        Setup(ToCity, "Jobs_Step_ToCity", "Jobs_ToCity_Placeholder", "Jobs_Picker_SearchCity", options, _toCity);
        ToCity.ToggleText = IsEditing ? null : Loc["Jobs_Picker_OnlyPossible"];
        var noMatchTag = Loc["Jobs_Picker_NoMatch"];
        ToCity.ToggleFilter = o => !o.IsEnabled || o.Tag != noMatchTag; // a cidade de saída (desativada) segue no topo
        ToCity.IsEnabled = true;
        ToCity.Hint = null;

        // empresa de saída
        var companyOptions = _companies.Select(c =>
        {
            var free = b.FreeSlots(c);
            var offers = b.ReplaceableOffers(c).Count;
            var parts = new List<string> { CityName(c.City), Loc.GetPlural("Jobs_Slots", free) };
            if (offers > 0) parts.Add(Loc.GetPlural("Jobs_Offers", offers));
            return new PickerOption(c, CompanyName(c), IsEditing ? CityName(c.City) : string.Join(" · ", parts));
        }).OrderBy(o => o.Primary, culture).ToList();
        Setup(FromCompany, "Jobs_Step_FromCompany", "Jobs_Step_Choose", "Jobs_Picker_SearchCompany", companyOptions, _fromCompany);
        FromCompany.IsEnabled = !IsEditing && from is not null && _toCity is not null && _hasMatch;
        FromCompany.Hint = Hint(IsEditing, from is null || _toCity is null, "Jobs_Hint_NeedCities", companyOptions.Count, _fromCompany is not null);

        // oferta a substituir
        var offerOptions = new List<PickerOption>();
        if (IsEditing && _editing!.Offer is { } fixedOffer) offerOptions.Add(OfferOption(fixedOffer));
        else offerOptions.AddRange(_offers.Select(OfferOption));
        Setup(Offer, "Jobs_Step_Offer", "Jobs_Step_Choose", "Jobs_Picker_SearchOffer", offerOptions, _offerId);
        Offer.IsEnabled = !IsEditing && offerOptions.Count > 0;
        Offer.Hint = IsEditing ? Loc["Jobs_Hint_Locked"] : offerOptions.Count == 1 && _offerId is not null ? Loc["Jobs_Hint_OnlyOption"]
            : Loc.GetPlural("Jobs_Hint_Options", offerOptions.Count);

        // carga
        var cargoOptions = _cargos.Select(CargoOption).OrderBy(o => o.Primary, culture).ToList();
        Setup(Cargo, "Jobs_Step_Cargo", "Jobs_Step_Choose", "Jobs_Picker_SearchCargo", cargoOptions, _cargo);
        Cargo.IsEnabled = _fromCompany is not null && _hasMatch;
        Cargo.Hint = Hint(false, _fromCompany is null, "Jobs_Hint_NeedFromCompany", cargoOptions.Count, _cargo is not null);

        // empresa de chegada
        var toOptions = _toCompanies.Select(c => new PickerOption(c, CompanyName(c), CityName(c.City))).OrderBy(o => o.Primary, culture).ToList();
        Setup(ToCompany, "Jobs_Step_ToCompany", "Jobs_Step_Choose", "Jobs_Picker_SearchCompany", toOptions, _toCompany);
        ToCompany.IsEnabled = _cargo is not null && _hasMatch;
        ToCompany.Hint = Hint(false, _cargo is null, "Jobs_Hint_NeedCargo", toOptions.Count, _toCompany is not null);
    }

    private string Hint(bool locked, bool blocked, string blockedKey, int count, bool hasValue) =>
        locked ? Loc["Jobs_Hint_Locked"]
        : blocked ? Loc[blockedKey]
        : hasValue && count == 1 ? Loc["Jobs_Hint_OnlyOption"]
        : Loc.GetPlural("Jobs_Hint_Options", count);

    private void Setup(JobPickerViewModel picker, string labelKey, string placeholderKey, string searchKey,
        IReadOnlyList<PickerOption> options, object? selected)
    {
        picker.Label = Loc[labelKey];
        picker.Placeholder = Loc[placeholderKey];
        picker.SearchWatermark = Loc[searchKey];
        picker.EmptyText = Loc["Jobs_Picker_NoItems"];
        picker.Set(options, selected);
    }

    private PickerOption OfferOption(ExistingOffer offer)
    {
        var cargo = CargoName(offer.Cargo);
        var km = Loc.Get("Format_Km", offer.DistanceKm);
        var meta = offer.ExpiresIn is { } left ? Loc.Get("Jobs_Offer_Meta", cargo, km, Loc.Get("Jobs_Offer_Expires", Duration(left))) : $"{cargo} · {km}";
        var target = offer.Target;
        return new PickerOption(offer.OfferId, $"→ {CompanyName(target)} · {CityName(target.City)} · {cargo}", meta);
    }

    /// <summary>Tempo restante de uma oferta (horas de jogo; dias a partir de 48 h).</summary>
    private string Duration(TimeSpan span) =>
        span.TotalHours >= 48 ? Loc.Get("Format_Days", (int)Math.Ceiling(span.TotalDays)) : Loc.Get("Format_Hours", (int)Math.Ceiling(span.TotalHours));

    private PickerOption CargoOption(string cargo)
    {
        var info = TrailerInfoOf(_board!.Template(cargo));
        return new PickerOption(cargo, CargoName(cargo), info?.Label.Name ?? "", LeadingIcon: info?.Icon?.Key, LeadingBadge: info?.Icon?.Units ?? 1, ReserveLeading: true);
    }

    private string KmText(RouteDistance d) => Loc.Get(d.Source == DistanceSource.Estimated ? "Format_KmApprox" : "Format_Km", d.Km);
}
