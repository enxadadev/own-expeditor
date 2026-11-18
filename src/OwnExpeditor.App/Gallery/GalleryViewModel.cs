using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OwnExpeditor.App.Controls;
using OwnExpeditor.App.Services;
using OwnExpeditor.App.ViewModels;
using OwnExpeditor.Domain.Settings;

namespace OwnExpeditor.App.Gallery;

public enum GalleryThemeMode { Both, Light, Dark }

/// <summary>Estado da Galeria e dados de amostra sintéticos (ids e nomes fictícios; nada de arte da SCS).</summary>
public partial class GalleryViewModel : ViewModelBase
{
    private readonly ILocalizer _loc = Localizer.Instance;

    [ObservableProperty] private GalleryThemeMode _themeMode = GalleryThemeMode.Both;
    [ObservableProperty] private bool _longTexts;
    [ObservableProperty] private bool _showHitAreas;

    public GalleryViewModel()
    {
        _loc.CultureChanged += (_, _) => OnPropertyChanged(string.Empty);
        SelectedOrigin = Options[0];
    }

    public bool ShowLight => ThemeMode != GalleryThemeMode.Dark;
    public bool ShowDark => ThemeMode != GalleryThemeMode.Light;
    public bool IsPtBr => _loc.Culture.TwoLetterISOLanguageName == "pt";

    partial void OnThemeModeChanged(GalleryThemeMode value)
    {
        OnPropertyChanged(nameof(ShowLight));
        OnPropertyChanged(nameof(ShowDark));
    }

    [RelayCommand] private void SetBoth() => ThemeMode = GalleryThemeMode.Both;
    [RelayCommand] private void SetLight() => ThemeMode = GalleryThemeMode.Light;
    [RelayCommand] private void SetDark() => ThemeMode = GalleryThemeMode.Dark;
    [RelayCommand] private void SetPtBr() => _loc.SetLanguage(Language.PtBr);
    [RelayCommand] private void SetEn() => _loc.SetLanguage(Language.En);
    [RelayCommand] private void Nothing() { }

    public IRelayCommand NoOp => NothingCommand;

    // ---- textos fixos (todos de chaves .resx) ----
    public string L(string key) => _loc[key];
    public string Title => _loc["Gallery_Title"];
    public string Review => _loc["Review_Title"];
    public string Cancel => _loc["Common_Cancel"];
    public string Retry => _loc["Common_Retry"];
    public string Discard => _loc["Diff_Discard"];
    public string ModuleJobs => _loc["Module_jobs"];
    public string Eyebrow => $"{_loc["SessionBar_Profile"]} 1 · {_loc["SessionBar_Save"]} 4";
    public string Count6485 => 6485.ToString("N0", _loc.Culture);
    public string Km569 => _loc.Get("Format_Km", 569);
    public string Km1240 => _loc.Get("Format_Km", 1240);
    public string Money => _loc.FormatMoney(250_000, Domain.Games.GameKind.Ets2);
    public string MoneyBefore => _loc.FormatMoney(120_450, Domain.Games.GameKind.Ets2);
    public string EmptyTitle => _loc["Jobs_Empty_Title"];
    public string EmptyBody => _loc["Jobs_Empty_Tip"];
    public string NoResultsMessage => _loc.Get("Empty_NoResults_Message", "Radus");
    public string ErrTitle => _loc["Error_Title_Apply"];
    public string ErrMsg => _loc["Error_GameRunning"];
    public string WarnTitle => _loc["Banner_Severity_Warning"];
    public string WarnMsg => _loc["SessionBar_GameRunningHint"];
    public string InfoTitle => _loc["Banner_Severity_Info"];
    public string InfoMsg => _loc["Shell_SafetyNote"];
    public string ErrDetails => "C:\\Users\\demo\\profiles\\6D6F\\save\\4\\game.sii · 1.204";
    public string LoadingReading => _loc["Loading_Title"];
    public string LoadingDetail => _loc.Get("Format_Km", 4038); // amostra de linha de apoio com número
    public string PickerLabel => _loc["Route_Origin"];
    public string PickerLabel2 => _loc["Route_Destination"];
    public string PickerPlaceholder => _loc["Picker_None"];
    public string SearchWatermark => _loc["Search_Placeholder_Default"];
    public string Mod => _loc["Picker_Tag_Mod"];

    // ---- amostras que mudam com "Textos longos" ----
    public string CityA => LongTexts ? "Sofia-Kremikovtsi industrial district, Balkans Expansion" : "Sofia";
    public string CityB => LongTexts ? "Istanbul — Ataturk freight terminal and logistics park" : "Istanbul";
    public string CompanyA => LongTexts ? "Radus Bulgaria Regional Distribution Center" : "Radus";
    public string CompanyB => "LogNStick";
    public string CargoLine => LongTexts ? "Concrete beams, oversize load with escort vehicles" : _loc["Gallery_State_Normal"];
    public string TrailerLine => LongTexts ? "Heavy lowboy trailer (Balkans Expansion)" : "Flatbed";
    public string PageTitle => LongTexts ? ModuleJobs + " — Balkans Expansion regional freight market" : ModuleJobs;
    public string ButtonLong => LongTexts ? _loc["Review_Title"] + " " + _loc["Review_Title"] : Review;
    public string SelectedPickerText => LongTexts ? "Sofia-Kremikovtsi industrial district, Balkans Expansion" : "Sofia";

    partial void OnLongTextsChanged(bool value) => OnPropertyChanged(string.Empty);

    public MapPoint Origin => new(CityA, 23.32, 42.70);
    public MapPoint Destination => new(CityB, 28.98, 41.01);
    public MapPoint OriginNoGeo => new(CityA);
    public MapPoint DestinationNoGeo => new(CityB);
    public IReadOnlyList<MapPoint> Cities { get; } =
    [
        new("Plovdiv", 24.75, 42.15), new("Varna", 27.91, 43.21), new("Edirne", 26.56, 41.68),
        new("Bursa", 29.06, 40.18), new("Burgas", 27.47, 42.50), new("Nis", 21.90, 43.32),
    ];

    public string RouteChip => $"{CompanyA} · {CityA} → {CompanyB} · {CityB}";

    public IReadOnlyList<PickerOption> Options { get; } =
    [
        new("a", "Sofia", "Radus", null), new("b", "Istanbul", "LogNStick", null),
        new("c", "Praha", "Tradeaux", "Mod"), new("d", "Varna", "Radus", null),
    ];

    public IReadOnlyList<PickerOption> LongOptions { get; } =
    [
        new("l1", "Sofia-Kremikovtsi industrial district, Balkans Expansion", "Radus Bulgaria Regional Distribution Center", "Mod"),
        new("l2", "Istanbul — Ataturk freight terminal and logistics park", "LogNStick International Freight Services", null),
    ];

    [ObservableProperty] private PickerOption? _selectedOrigin;
    public PickerOption? SelectedNone => null;

    public IReadOnlyList<IDiffGroup> DiffGroups => new IDiffGroup[]
    {
        new DiffGroup(_loc["Module_jobs"], $"{CompanyA} · {CityA} → {CompanyB} · {CityB}",
        [
            new DiffLine(_loc["Route_Destination"], $"{CompanyB} · {CityB}", $"Tradeaux · Praha"),
            new DiffLine("Km", _loc.Get("Format_Km", 569), _loc.Get("Format_Km", 612), true),
            new DiffLine(_loc["Gallery_State_Empty"], null, "Maquinário"),
        ], NothingCommand),
        new DiffGroup(_loc["Module_profile"], "—",
        [
            new DiffLine("€", MoneyBefore, Money, true),
        ], NothingCommand),
    };

    public IReadOnlyList<IDiffGroup> DiffOne => DiffGroups.Take(1).ToList();
    public IReadOnlyList<IDiffGroup> DiffNone { get; } = [];
}
