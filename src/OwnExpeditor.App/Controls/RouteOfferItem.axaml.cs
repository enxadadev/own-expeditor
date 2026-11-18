using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using OwnExpeditor.App.Services;

namespace OwnExpeditor.App.Controls;

/// <summary>
/// Item de lista "Origem ⋯ Destino · km" com carga e reboque (design/controles §2.13). Vai dentro de um
/// <c>ListBoxItem</c> (a lista cuida de seleção e setas); <see cref="IsSelected"/> vem do container.
/// </summary>
public partial class RouteOfferItem : UserControl
{
    public static readonly StyledProperty<string> OriginProperty = AvaloniaProperty.Register<RouteOfferItem, string>(nameof(Origin), "");
    public static readonly StyledProperty<string> DestinationProperty = AvaloniaProperty.Register<RouteOfferItem, string>(nameof(Destination), "");
    public static readonly StyledProperty<string?> OriginCompanyProperty = AvaloniaProperty.Register<RouteOfferItem, string?>(nameof(OriginCompany));
    public static readonly StyledProperty<string?> DestinationCompanyProperty = AvaloniaProperty.Register<RouteOfferItem, string?>(nameof(DestinationCompany));
    public static readonly StyledProperty<string> DistanceTextProperty = AvaloniaProperty.Register<RouteOfferItem, string>(nameof(DistanceText), "");
    public static readonly StyledProperty<string> CargoTextProperty = AvaloniaProperty.Register<RouteOfferItem, string>(nameof(CargoText), "");
    public static readonly StyledProperty<string?> TrailerTextProperty = AvaloniaProperty.Register<RouteOfferItem, string?>(nameof(TrailerText));
    public static readonly StyledProperty<bool> IsSelectedProperty = AvaloniaProperty.Register<RouteOfferItem, bool>(nameof(IsSelected));
    public static readonly StyledProperty<bool> IsModifiedProperty = AvaloniaProperty.Register<RouteOfferItem, bool>(nameof(IsModified));
    public static readonly StyledProperty<string?> StatusTextProperty = AvaloniaProperty.Register<RouteOfferItem, string?>(nameof(StatusText));
    public static readonly StyledProperty<bool> IsDimmedProperty = AvaloniaProperty.Register<RouteOfferItem, bool>(nameof(IsDimmed));
    public static readonly DirectProperty<RouteOfferItem, string> SecondLineProperty = AvaloniaProperty.RegisterDirect<RouteOfferItem, string>(nameof(SecondLine), o => o.SecondLine);
    public static readonly DirectProperty<RouteOfferItem, string> FullTooltipProperty = AvaloniaProperty.RegisterDirect<RouteOfferItem, string>(nameof(FullTooltip), o => o.FullTooltip);
    public static readonly DirectProperty<RouteOfferItem, double> CityMaxWidthProperty = AvaloniaProperty.RegisterDirect<RouteOfferItem, double>(nameof(CityMaxWidth), o => o.CityMaxWidth);
    public static readonly DirectProperty<RouteOfferItem, bool> ShowDashProperty = AvaloniaProperty.RegisterDirect<RouteOfferItem, bool>(nameof(ShowDash), o => o.ShowDash);

    public RouteOfferItem()
    {
        InitializeComponent();
        Automation.Track(this, UpdateAutomation, OriginProperty, DestinationProperty, OriginCompanyProperty, DestinationCompanyProperty,
            DistanceTextProperty, CargoTextProperty, TrailerTextProperty, IsModifiedProperty, StatusTextProperty);
        // O ListBoxItem pai marca :selected; espelhamos em IsSelected quando o item vive numa lista.
        AttachedToVisualTree += (_, _) => { if (Parent is ListBoxItem item) item.PropertyChanged += (_, e) => { if (e.Property == ListBoxItem.IsSelectedProperty) IsSelected = item.IsSelected; }; };
    }

    public string Origin { get => GetValue(OriginProperty); set => SetValue(OriginProperty, value); }
    public string Destination { get => GetValue(DestinationProperty); set => SetValue(DestinationProperty, value); }
    public string? OriginCompany { get => GetValue(OriginCompanyProperty); set => SetValue(OriginCompanyProperty, value); }
    public string? DestinationCompany { get => GetValue(DestinationCompanyProperty); set => SetValue(DestinationCompanyProperty, value); }

    /// <summary>Distância já formatada pela cultura ("569 km").</summary>
    public string DistanceText { get => GetValue(DistanceTextProperty); set => SetValue(DistanceTextProperty, value); }
    public string CargoText { get => GetValue(CargoTextProperty); set => SetValue(CargoTextProperty, value); }
    public string? TrailerText { get => GetValue(TrailerTextProperty); set => SetValue(TrailerTextProperty, value); }
    public bool IsSelected { get => GetValue(IsSelectedProperty); set => SetValue(IsSelectedProperty, value); }
    public bool IsModified { get => GetValue(IsModifiedProperty); set => SetValue(IsModifiedProperty, value); }

    /// <summary>Etiqueta de dado ("Expirada"); o item fica a 70 % de opacidade.</summary>
    public string? StatusText { get => GetValue(StatusTextProperty); set => SetValue(StatusTextProperty, value); }

    /// <summary>Oferta de outra fonte, indisponível: 40 %.</summary>
    public bool IsDimmed { get => GetValue(IsDimmedProperty); set => SetValue(IsDimmedProperty, value); }

    public string SecondLine => string.IsNullOrEmpty(TrailerText) ? CargoText : $"{CargoText} · {TrailerText}";
    public string FullTooltip => $"{Place(OriginCompany, Origin)} → {Place(DestinationCompany, Destination)}";
    public double CityMaxWidth => Bounds.Width > 0 ? Bounds.Width * 0.38 : 220;
    public bool ShowDash => Bounds.Width >= 280;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == CargoTextProperty || change.Property == TrailerTextProperty)
            RaisePropertyChanged(SecondLineProperty, "", SecondLine);
        else if (change.Property == OriginProperty || change.Property == DestinationProperty
                 || change.Property == OriginCompanyProperty || change.Property == DestinationCompanyProperty)
            RaisePropertyChanged(FullTooltipProperty, "", FullTooltip);
        else if (change.Property == BoundsProperty)
        {
            RaisePropertyChanged(CityMaxWidthProperty, 0, CityMaxWidth);
            RaisePropertyChanged(ShowDashProperty, !ShowDash, ShowDash);
        }
        else if (change.Property == IsSelectedProperty) Card.Classes.Set("selected", IsSelected);
        else if (change.Property == StatusTextProperty) Card.Classes.Set("dim", !string.IsNullOrEmpty(StatusText));
        else if (change.Property == IsDimmedProperty) Card.Classes.Set("disabled", IsDimmed);
    }

    protected override void OnPointerEntered(PointerEventArgs e) { base.OnPointerEntered(e); Card.Classes.Set("hover", true); }

    protected override void OnPointerExited(PointerEventArgs e) { base.OnPointerExited(e); Card.Classes.Set("hover", false); Card.Classes.Set("pressed", false); }

    protected override void OnPointerPressed(PointerPressedEventArgs e) { base.OnPointerPressed(e); Card.Classes.Set("pressed", true); }

    protected override void OnPointerReleased(PointerReleasedEventArgs e) { base.OnPointerReleased(e); Card.Classes.Set("pressed", false); }

    /// <summary>Só para a Galeria: liga hover/pressed/focus sem entrada real.</summary>
    internal void ForceVisualState(string? state)
    {
        Card.Classes.Set("hover", state == "Hover");
        Card.Classes.Set("pressed", state == "Pressed");
        Card.Classes.Set("focus", state == "Focus");
    }

    private static string Place(string? company, string city) => string.IsNullOrEmpty(company) ? city : $"{company} · {city}";

    private void UpdateAutomation()
    {
        var loc = Localizer.Instance;
        var name = loc.Get("Offer_A11y", Place(OriginCompany, Origin), Place(DestinationCompany, Destination), DistanceText, SecondLine);
        if (IsModified) name += ", " + loc["Offer_Modified"];
        if (!string.IsNullOrEmpty(StatusText)) name += ", " + StatusText;
        AutomationProperties.SetName(this, name);
    }
}
