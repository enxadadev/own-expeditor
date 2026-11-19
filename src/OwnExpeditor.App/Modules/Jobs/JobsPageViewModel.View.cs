using System;
using System.Collections.Generic;
using System.Linq;
using OwnExpeditor.App.Controls;
using OwnExpeditor.App.Services;
using OwnExpeditor.App.ViewModels;
using OwnExpeditor.Core.Modules.Jobs;
using OwnExpeditor.Domain.Games;

namespace OwnExpeditor.App.Modules.Jobs;

/// <summary>O que a View lê: textos, visibilidade e listas derivadas do estado do fluxo. Sem regra de negócio.</summary>
public sealed partial class JobsPageViewModel
{
    private IReadOnlyList<CityItemViewModel> _cityItems = [];
    private CityItemViewModel? _citySelection;
    private string _cityQuery = "";
    private IReadOnlyList<PendingJobViewModel> _pendingRows = [];
    private IReadOnlyList<JobModeChoice> _modeChoices = [];
    private IReadOnlyList<JobExpirationChoice> _expirationChoices = [];

    // ---- página e lista de cidades

    public string Title => Loc["Module_" + Id];

    public bool IsLoading => State == PageState.Loading;

    public bool IsEmptyModule => State == PageState.Empty;

    public bool ShowForm => State == PageState.Ready;

    public string CityCount => _allCityIds.Count.ToString("N0", Loc.Culture);

    public string CityCountLabel => Loc.GetPlural("Jobs_List_Count", _allCityIds.Count);

    public IReadOnlyList<CityItemViewModel> CityItems => _cityItems;

    /// <summary>A escolha do usuário na lista; o controle devolve <c>null</c> quando o filtro esconde a cidade, e isso não é uma escolha.</summary>
    public CityItemViewModel? SelectedCity
    {
        get => _citySelection;
        set
        {
            if (value is null || ReferenceEquals(value, _citySelection)) return;
            _citySelection = value;
            SelectFromCity(value.Id);
        }
    }

    public string CityQuery
    {
        get => _cityQuery;
        set
        {
            if (value == _cityQuery) return;
            _cityQuery = value;
            if (!_refreshing) Refresh();
        }
    }

    public bool ShowCityList => _cityItems.Count > 0;

    public bool NoCityResults => ShowForm && _cityItems.Count == 0;

    public string NoCityResultBody => Loc.Get("Jobs_Search_NoResult_Body", _cityQuery);

    public bool IsListEnabled => !IsEditing;

    // ---- avisos

    public bool ShowNoCatalog => ShowForm && _session is { GameData.IsAvailable: false };

    public string NoCatalogTitle => Loc.Get("Jobs_NoCatalog_Title", Loc["Game_" + _session?.Game]);

    public bool ShowCloud => ShowForm && _session is { Save.Profile.IsSteamCloud: true } && !_cloudDismissed;

    public bool ShowEditing => IsEditing;

    public string? Notice => _notice;

    public bool HasNotice => _notice is not null;

    public string? ErrorMessage => _error;

    public bool HasError => _error is not null;

    // ---- cidades escolhidas

    public string FromChipText => _fromCity is null || _board is null ? Loc["Jobs_FromCity_Placeholder"] : CityName(_fromCity);

    public string FromChipAria => _fromCity is null || _board is null ? Loc["Jobs_Chip_FromHint"] : Loc.Get("Jobs_Chip_FromAria", CityName(_fromCity));

    public bool HasFromCity => _fromCity is not null;

    public bool CanSwapCities => CanSwap;

    public bool ShowPendingCard => ShowForm && !IsEditing && _pendingRows.Count > 0;

    public IReadOnlyList<PendingJobViewModel> PendingRows => _pendingRows;

    // ---- mapa e indicadores

    private bool HasBothCities => _board is not null && _fromCity is not null && _toCity is not null;

    public MapPoint? MapOrigin => HasBothCities ? new MapPoint(CityName(_fromCity!)) : null;

    public MapPoint? MapDestination => HasBothCities ? new MapPoint(CityName(_toCity!)) : null;

    public string? MapRouteLabel => !HasBothCities ? null
        : _fromCompany is not null && _toCompany is not null
            ? $"{CompanyName(_fromCompany)} · {CityName(_fromCompany.City)} → {CompanyName(_toCompany)} · {CityName(_toCompany.City)}"
            : $"{Loc["Jobs_Map_Route"]}: {CityName(_fromCity!)} → {CityName(_toCity!)}";

    public string? MapDistanceText => _distance is null ? null
        : _distance.Source == DistanceSource.Estimated ? $"{KmText(_distance)} · {Loc["Jobs_Distance_EstimatedShort"]}" : KmText(_distance);

    /// <summary>O mapa encolhe (150 → 128) quando há avisos, pendências ou erro, para o formulário caber (design/fretes §1).</summary>
    public double MapHeight => ShowNoCatalog || ShowCloud || ShowEditing || ShowPendingCard || HasNotice || HasError ? 128 : 150;

    public string MapEmptyTitle => Loc[_fromCity is null ? "Jobs_Map_PickFrom" : "Jobs_Map_PickTo"];

    public string MapNote => Loc["Jobs_Map_Schematic"];

    /// <summary>Traço de "sem valor" dos indicadores (símbolo, igual nos dois idiomas; ver design/fretes §4.1).</summary>
    private const string NoValue = "—";

    public string DistanceValue => _distance is null ? NoValue : KmText(_distance);

    public bool IsDistanceEstimated => _distance?.Source == DistanceSource.Estimated;

    public string? DistanceTag => IsDistanceEstimated ? Loc["Jobs_Distance_Estimated"] : null;

    public string DistanceHint => _distance is null ? Loc["Jobs_Distance_Pending"]
        : IsDistanceEstimated ? Loc["Jobs_Distance_Estimated_Help"] : Loc["Jobs_Distance_FromSave"];

    public string DistanceAutomation => _distance is null
        ? $"{Loc["Jobs_Field_Distance"]}: {Loc["Jobs_Distance_Pending"]}"
        : $"{Loc["Jobs_Field_Distance"]}: {DistanceValue}. {(IsDistanceEstimated ? Loc["Jobs_Distance_Estimated"] : Loc["Jobs_Distance_FromSave"])}";

    public IReadOnlyList<JobExpirationChoice> ExpirationChoices => _expirationChoices;

    public JobExpirationChoice? SelectedExpiration
    {
        get => _expirationChoices.FirstOrDefault(c => c.Hours == _hours);
        set
        {
            if (value is not null) SelectExpiration(value.Hours);
        }
    }

    public string ExpirationHelp => Loc["Jobs_Expiration_Help"];

    private JobTemplate? Template => _board is not null && _cargo is not null ? _board.Template(_cargo) : null;

    public string UnitsValue => Template is { } t ? t.UnitsCount.ToString("N0", Loc.Culture) : NoValue;

    /// <summary>Rótulo e ícone de um reboque, calculados uma vez por template/cultura (não por propriedade nem por reconstrução do seletor).</summary>
    private sealed record TrailerInfo(TrailerLabel Label, TrailerIconInfo? Icon, string Hint, string AutomationName);

    private readonly Dictionary<JobTemplate, TrailerInfo?> _trailerCache = new(ReferenceEqualityComparer.Instance);
    private string? _trailerCacheCulture;
    private object? _trailerCacheBoard;

    /// <summary>Resolve o ícone (decoração); trocável em teste para provar que a lista de opções não depende dele.</summary>
    public Func<TrailerDescriptor, TrailerIconInfo?> IconResolver
    {
        get => _iconResolver;
        set
        {
            _iconResolver = value;
            ClearTrailerCache();
        }
    }

    private Func<TrailerDescriptor, TrailerIconInfo?> _iconResolver = TrailerIcons.Resolve;

    private void ClearTrailerCache()
    {
        _trailerCache.Clear();
        _trailerCacheBoard = null;
        _trailerCacheCulture = null;
    }

    private TrailerInfo? TrailerInfoOf(JobTemplate? t)
    {
        if (t is null || _board is null) return null;
        if (_trailerCacheCulture != Loc.Culture.Name || !ReferenceEquals(_trailerCacheBoard, _board))
        {
            _trailerCache.Clear();
            _trailerCacheCulture = Loc.Culture.Name;
            _trailerCacheBoard = _board;
        }
        if (_trailerCache.TryGetValue(t, out var cached)) return cached;
        var descriptor = _board.Names.Trailer(t.TrailerDefinition, t.TrailerVariant);
        var label = TrailerText.Format(Loc, descriptor);
        var info = label.HasValue ? new TrailerInfo(label, _iconResolver(descriptor),
            $"{Loc["Jobs_Trailer_Help"]}\n{Loc.Get("Trailer_IdTip", label.RawId)}",
            label.Details.Length > 0 ? Loc.Get("Trailer_Automation_Details", label.Name, label.Details) : Loc.Get("Trailer_Automation", label.Name)) : null;
        _trailerCache[t] = info;
        return info;
    }

    private TrailerLabel? TrailerLabelOf(JobTemplate? t) => TrailerInfoOf(t)?.Label;

    private TrailerLabel? CurrentTrailer => TrailerLabelOf(Template);

    /// <summary>Chave do ícone do reboque atual (decoração; nulo sem reboque).</summary>
    public string? TrailerIconKey => TrailerInfoOf(Template)?.Icon?.Key;

    /// <summary>Reboques da cadeia (selo ×N quando &gt; 1).</summary>
    public int TrailerUnits => TrailerInfoOf(Template)?.Icon?.Units ?? 1;

    /// <summary>Nome do reboque (ou "—" sem reboque).</summary>
    public string TrailerValue => CurrentTrailer?.Name ?? NoValue;

    /// <summary>Eixos e comprimento; vazio some a linha.</summary>
    public string TrailerDetails => CurrentTrailer?.Details ?? "";

    public bool HasTrailerDetails => TrailerDetails.Length > 0;

    /// <summary>Ajuda e id cru (tooltip); vazio sem reboque.</summary>
    public string? TrailerHint => TrailerInfoOf(Template)?.Hint;

    public bool HasTrailer => CurrentTrailer is not null;

    /// <summary>Nome acessível do cartão.</summary>
    public string TrailerAutomationName => TrailerInfoOf(Template)?.AutomationName ?? Loc["Trailer_Automation_None"];

    private string HoursText(int hours) => hours % 24 == 0 && hours >= 168 ? Loc.Get("Format_Days", hours / 24) : Loc.Get("Format_Hours", hours);

    // ---- modo (vaga livre × substituir)

    public bool ShowNoMatch => ShowForm && !_hasMatch && _hasUsableCompanies;

    public string NoMatchBody => !HasBothCities ? "" : Loc.Get("Jobs_NoMatch_Body", CityName(_fromCity!), CityName(_toCity!))
        + (ShowNoCatalog ? " " + Loc["Jobs_NoMatch_CatalogNote"] : "");

    public bool ShowNoSlots => ShowForm && _fromCity is not null && !_hasUsableCompanies;

    public string NoSlotsTitle => _fromCity is null || _board is null ? "" : Loc.Get("Jobs_NoSlots_Title", CityName(_fromCity));

    public string NoSlotsBody => _fromCity is null || _board is null ? "" : Loc.Get("Jobs_NoSlots_Body", CityName(_fromCity));

    public bool ShowIndicators => !ShowNoSlots;

    public bool ShowSteps => ShowForm && !ShowNoMatch && !ShowNoSlots;

    public bool ShowMode => ShowSteps && _fromCompany is not null;

    public bool ShowOffer => ShowMode && _mode == JobSlotMode.ReplaceOffer;

    public bool IsModeEnabled => !IsEditing;

    public IReadOnlyList<JobModeChoice> ModeChoices => _modeChoices;

    private void BuildChoices()
    {
        var free = _board is not null && _fromCompany is not null ? _board.FreeSlots(_fromCompany) : 0;
        var offers = _offers.Count;
        _modeChoices =
        [
            new(JobSlotMode.FillEmpty, IsEditing ? Loc["Jobs_Mode_Free"] : $"{Loc["Jobs_Mode_Free"]} · {free.ToString("N0", Loc.Culture)}", free > 0),
            new(JobSlotMode.ReplaceOffer, IsEditing ? Loc["Jobs_Mode_Replace"] : $"{Loc["Jobs_Mode_Replace"]} · {offers.ToString("N0", Loc.Culture)}", offers > 0),
        ];
        _expirationChoices = [.. JobPlanner.AllowedExpirationHours.Select(h => new JobExpirationChoice(h, HoursText(h)))];
    }

    public JobModeChoice? SelectedMode
    {
        get => _modeChoices.FirstOrDefault(c => c.Mode == _mode);
        set
        {
            if (value is not null && value.IsEnabled) SelectMode(value.Mode);
        }
    }

    public string ModeHint => IsEditing ? Loc["Jobs_Mode_Locked"]
        : _fromCompany is not null && _board!.FreeSlots(_fromCompany) == 0 ? Loc["Jobs_Mode_NoFree"]
        : _offers.Count == 0 ? Loc["Jobs_Mode_NoOffers"]
        : Loc["Jobs_Mode_Help"];

    // ---- ação

    private List<string> Missing()
    {
        var missing = new List<string>();
        if (_fromCity is null) missing.Add("FromCity");
        if (_toCity is null) missing.Add("ToCity");
        if (_fromCompany is null) missing.Add("FromCompany");
        if (_mode == JobSlotMode.ReplaceOffer && _offerId is null && _fromCompany is not null) missing.Add("Offer");
        if (_cargo is null) missing.Add("Cargo");
        if (_toCompany is null) missing.Add("ToCompany");
        return missing;
    }

    public bool CanAdd => _board is not null && ShowSteps && Missing().Count == 0;

    public string AddText => Loc[IsEditing ? "Jobs_Update" : "Jobs_Add"];

    public string MissingText
    {
        get
        {
            var missing = Missing();
            return missing.Count == 0 || !ShowSteps ? CanAdd ? Loc["Jobs_Ready"] : "" : Loc.Get("Jobs_Missing", string.Join(", ", missing.Select(k => Loc["Jobs_Missing_" + k])));
        }
    }

    public bool Step1Done => _fromCity is not null;

    public bool Step2Done => _toCity is not null;

    public bool Step3Done => _fromCompany is not null;

    public bool Step4Done => _cargo is not null;

    public bool Step5Done => _toCompany is not null;

    // ---- cartão de pendentes

    private IReadOnlyList<PendingJobViewModel> BuildPending()
    {
        if (_board is null || _fromCity is null) return [];
        return PendingJobs().Where(p => p.Staged.Request.Origin.City == _fromCity).Select(p =>
        {
            var (key, staged) = p;
            var r = staged.Request;
            var title = $"{CompanyName(r.Origin)} · {CityName(r.Origin.City)} → {CompanyName(r.Target)} · {CityName(r.Target.City)}";
            var detail = string.Join(" · ", CargoName(r.Cargo), KmText(_board.Distance(r.Origin.City, r.Target.City)),
                HoursText(r.ExpirationHours), Loc[r.Mode == JobSlotMode.ReplaceOffer ? "Jobs_Pending_Mode_Replace" : "Jobs_Pending_Mode_Free"]);
            return new PendingJobViewModel(key, title, detail, Loc.Get("Jobs_Pending_EditAria", title), Loc.Get("Review_DiscardItem_Aria", title),
                new CommunityToolkit.Mvvm.Input.RelayCommand(() => EditPending(key)),
                new CommunityToolkit.Mvvm.Input.RelayCommand(() => DiscardPending(key)));
        }).ToList();
    }
}
