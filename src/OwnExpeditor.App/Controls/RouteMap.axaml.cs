using System.Collections.Generic;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using OwnExpeditor.App.Services;

namespace OwnExpeditor.App.Controls;

/// <summary>
/// Mapa esquemático origem → destino; geográfico quando as duas pontas têm coordenadas (design/controles §2.14).
/// Não interativo na v1.
/// </summary>
public partial class RouteMap : UserControl
{
    public static readonly StyledProperty<MapPoint?> OriginProperty = AvaloniaProperty.Register<RouteMap, MapPoint?>(nameof(Origin));
    public static readonly StyledProperty<MapPoint?> DestinationProperty = AvaloniaProperty.Register<RouteMap, MapPoint?>(nameof(Destination));
    public static readonly StyledProperty<IEnumerable<MapPoint>?> CitiesProperty = AvaloniaProperty.Register<RouteMap, IEnumerable<MapPoint>?>(nameof(Cities));
    public static readonly StyledProperty<string?> DistanceTextProperty = AvaloniaProperty.Register<RouteMap, string?>(nameof(DistanceText));
    public static readonly StyledProperty<string?> RouteLabelProperty = AvaloniaProperty.Register<RouteMap, string?>(nameof(RouteLabel));
    public static readonly StyledProperty<RouteMapMode> ModeProperty = AvaloniaProperty.Register<RouteMap, RouteMapMode>(nameof(Mode));
    public static readonly StyledProperty<bool> IsLoadingProperty = AvaloniaProperty.Register<RouteMap, bool>(nameof(IsLoading));
    public static readonly DirectProperty<RouteMap, bool> IsEmptyProperty = AvaloniaProperty.RegisterDirect<RouteMap, bool>(nameof(IsEmpty), o => o.IsEmpty);
    public static readonly DirectProperty<RouteMap, bool> ShowChipProperty = AvaloniaProperty.RegisterDirect<RouteMap, bool>(nameof(ShowChip), o => o.ShowChip);
    public static readonly DirectProperty<RouteMap, bool> ShowDistanceProperty = AvaloniaProperty.RegisterDirect<RouteMap, bool>(nameof(ShowDistance), o => o.ShowDistance);
    public static readonly DirectProperty<RouteMap, double> ChipMaxWidthProperty = AvaloniaProperty.RegisterDirect<RouteMap, double>(nameof(ChipMaxWidth), o => o.ChipMaxWidth);

    public RouteMap()
    {
        InitializeComponent();
        Automation.Track(this, UpdateAutomation, OriginProperty, DestinationProperty, DistanceTextProperty);
    }

    public MapPoint? Origin { get => GetValue(OriginProperty); set => SetValue(OriginProperty, value); }
    public MapPoint? Destination { get => GetValue(DestinationProperty); set => SetValue(DestinationProperty, value); }

    /// <summary>Cidades de fundo; só aparecem no modo geográfico.</summary>
    public IEnumerable<MapPoint>? Cities { get => GetValue(CitiesProperty); set => SetValue(CitiesProperty, value); }
    public string? DistanceText { get => GetValue(DistanceTextProperty); set => SetValue(DistanceTextProperty, value); }
    public string? RouteLabel { get => GetValue(RouteLabelProperty); set => SetValue(RouteLabelProperty, value); }
    public RouteMapMode Mode { get => GetValue(ModeProperty); set => SetValue(ModeProperty, value); }
    public bool IsLoading { get => GetValue(IsLoadingProperty); set => SetValue(IsLoadingProperty, value); }

    public bool IsEmpty => !IsLoading && (Origin is null || Destination is null);
    public bool ShowChip => !string.IsNullOrEmpty(RouteLabel) && !IsEmpty;
    public bool ShowDistance => !string.IsNullOrEmpty(DistanceText) && !IsEmpty;
    public double ChipMaxWidth => Bounds.Width > 0 ? Bounds.Width * 0.62 : 400;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == OriginProperty) Canvas.Origin = Origin;
        else if (change.Property == DestinationProperty) Canvas.Destination = Destination;
        else if (change.Property == CitiesProperty) Canvas.Cities = Cities;
        else if (change.Property == ModeProperty) Canvas.Mode = Mode;
        else if (change.Property == BoundsProperty) RaisePropertyChanged(ChipMaxWidthProperty, 0, ChipMaxWidth);

        if (change.Property == OriginProperty || change.Property == DestinationProperty || change.Property == IsLoadingProperty
            || change.Property == RouteLabelProperty || change.Property == DistanceTextProperty)
        {
            RaisePropertyChanged(IsEmptyProperty, !IsEmpty, IsEmpty);
            RaisePropertyChanged(ShowChipProperty, !ShowChip, ShowChip);
            RaisePropertyChanged(ShowDistanceProperty, !ShowDistance, ShowDistance);
        }
    }

    private void UpdateAutomation()
    {
        var loc = Localizer.Instance;
        AutomationProperties.SetName(this, Origin is null || Destination is null
            ? loc["Route_A11y_Empty"]
            : loc.Get("Route_A11y", Origin.Label, Destination.Label, DistanceText ?? ""));
    }
}
