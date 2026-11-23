using System;
using System.Collections.Generic;
using System.ComponentModel;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Media;
using OwnExpeditor.App.Services;

namespace OwnExpeditor.App.Controls;

/// <summary>
/// Mapa esquemático origem → destino; geográfico quando as duas pontas têm coordenadas (design/controles §2.14).
/// Não interativo na v1 e, por isso, fora da ordem de Tab (zoom, arrastar e foco ficam para a fase 2 do mapa do jogo).
/// </summary>
public partial class RouteMap : UserControl
{
    public static readonly StyledProperty<MapPoint?> OriginProperty = AvaloniaProperty.Register<RouteMap, MapPoint?>(nameof(Origin));
    public static readonly StyledProperty<MapPoint?> DestinationProperty = AvaloniaProperty.Register<RouteMap, MapPoint?>(nameof(Destination));
    public static readonly StyledProperty<IEnumerable<MapPoint>?> CitiesProperty = AvaloniaProperty.Register<RouteMap, IEnumerable<MapPoint>?>(nameof(Cities));
    public static readonly StyledProperty<IEnumerable<MapPoint>?> CountriesProperty = AvaloniaProperty.Register<RouteMap, IEnumerable<MapPoint>?>(nameof(Countries));
    public static readonly StyledProperty<string?> NoteProperty = AvaloniaProperty.Register<RouteMap, string?>(nameof(Note));
    public static readonly DirectProperty<RouteMap, bool> ShowNoteProperty = AvaloniaProperty.RegisterDirect<RouteMap, bool>(nameof(ShowNote), o => o.ShowNote);
    public static readonly StyledProperty<string?> DistanceTextProperty = AvaloniaProperty.Register<RouteMap, string?>(nameof(DistanceText));
    public static readonly StyledProperty<string?> RouteLabelProperty = AvaloniaProperty.Register<RouteMap, string?>(nameof(RouteLabel));
    public static readonly StyledProperty<RouteMapMode> ModeProperty = AvaloniaProperty.Register<RouteMap, RouteMapMode>(nameof(Mode));
    public static readonly StyledProperty<MapNoteKind> NoteKindProperty = AvaloniaProperty.Register<RouteMap, MapNoteKind>(nameof(NoteKind));
    public static readonly StyledProperty<string?> EmptyTitleProperty = AvaloniaProperty.Register<RouteMap, string?>(nameof(EmptyTitle));
    public static readonly StyledProperty<string?> EmptyMessageProperty = AvaloniaProperty.Register<RouteMap, string?>(nameof(EmptyMessage));
    public static readonly DirectProperty<RouteMap, bool> ShowCustomEmptyProperty = AvaloniaProperty.RegisterDirect<RouteMap, bool>(nameof(ShowCustomEmpty), o => o.ShowCustomEmpty);
    public static readonly DirectProperty<RouteMap, bool> ShowDefaultEmptyProperty = AvaloniaProperty.RegisterDirect<RouteMap, bool>(nameof(ShowDefaultEmpty), o => o.ShowDefaultEmpty);
    public static readonly DirectProperty<RouteMap, bool> IsEmptyProperty = AvaloniaProperty.RegisterDirect<RouteMap, bool>(nameof(IsEmpty), o => o.IsEmpty);
    public static readonly DirectProperty<RouteMap, bool> ShowStraightLineProperty = AvaloniaProperty.RegisterDirect<RouteMap, bool>(nameof(ShowStraightLine), o => o.ShowStraightLine);
    public static readonly DirectProperty<RouteMap, bool> ShowChipProperty = AvaloniaProperty.RegisterDirect<RouteMap, bool>(nameof(ShowChip), o => o.ShowChip);
    public static readonly DirectProperty<RouteMap, bool> ShowDistanceProperty = AvaloniaProperty.RegisterDirect<RouteMap, bool>(nameof(ShowDistance), o => o.ShowDistance);
    public static readonly DirectProperty<RouteMap, double> ChipMaxWidthProperty = AvaloniaProperty.RegisterDirect<RouteMap, double>(nameof(ChipMaxWidth), o => o.ChipMaxWidth);

    // valores anteriores dos DirectProperty, para o evento carregar o antigo de verdade
    private bool _isEmpty = true, _showCustomEmpty, _showDefaultEmpty = true, _showChip, _showDistance, _showNote, _showStraightLine;
    private double _chipMaxWidth = 400;

    public RouteMap()
    {
        InitializeComponent();
        Automation.Track(this, UpdateAutomation, OriginProperty, DestinationProperty, DistanceTextProperty);
        Canvas.PropertyChanged += (_, e) => { if (e.Property == BoundsProperty) UpdateStraightLine(); }; // o segmento depende do tamanho já medido
        UpdateAutomation();
        Localizer.Instance.PropertyChanged += OnLocalizerChanged; // o nome acessível acompanha o idioma
        DetachedFromVisualTree += (_, _) => Localizer.Instance.PropertyChanged -= OnLocalizerChanged;
        AttachedToVisualTree += (_, _) =>
        {
            Localizer.Instance.PropertyChanged -= OnLocalizerChanged;
            Localizer.Instance.PropertyChanged += OnLocalizerChanged;
            UpdateAutomation();
        };
    }

    private void OnLocalizerChanged(object? sender, PropertyChangedEventArgs e) => UpdateAutomation();

    public MapPoint? Origin { get => GetValue(OriginProperty); set => SetValue(OriginProperty, value); }
    public MapPoint? Destination { get => GetValue(DestinationProperty); set => SetValue(DestinationProperty, value); }

    /// <summary>Cidades de fundo; só aparecem no modo geográfico.</summary>
    public IEnumerable<MapPoint>? Cities { get => GetValue(CitiesProperty); set => SetValue(CitiesProperty, value); }

    /// <summary>Rótulos de país do fundo; só aparecem no modo geográfico.</summary>
    public IEnumerable<MapPoint>? Countries { get => GetValue(CountriesProperty); set => SetValue(CountriesProperty, value); }

    /// <summary>Tipo da nota: define o ícone (nenhum, dica em cinza ou erro em vermelho).</summary>
    public MapNoteKind NoteKind { get => GetValue(NoteKindProperty); set => SetValue(NoteKindProperty, value); }

    /// <summary>Nota discreta no rodapé (ex.: "Preparando o mapa do jogo…"); vazia some.</summary>
    public string? Note { get => GetValue(NoteProperty); set => SetValue(NoteProperty, value); }
    public bool ShowNote => !string.IsNullOrEmpty(Note);

    /// <summary>Linha reta tracejada visível (modo geográfico, fase 1): tooltip e "linha reta" no chip de distância.</summary>
    public bool ShowStraightLine => !IsEmpty && Canvas.StraightSegment() is not null;
    public string? DistanceText { get => GetValue(DistanceTextProperty); set => SetValue(DistanceTextProperty, value); }
    public string? RouteLabel { get => GetValue(RouteLabelProperty); set => SetValue(RouteLabelProperty, value); }
    public RouteMapMode Mode { get => GetValue(ModeProperty); set => SetValue(ModeProperty, value); }

    /// <summary>Texto do estado vazio do módulo que usa o mapa (ex.: "Escolha a cidade de saída"); sem valor, o texto padrão.</summary>
    public string? EmptyTitle { get => GetValue(EmptyTitleProperty); set => SetValue(EmptyTitleProperty, value); }
    public string? EmptyMessage { get => GetValue(EmptyMessageProperty); set => SetValue(EmptyMessageProperty, value); }
    public bool ShowCustomEmpty => IsEmpty && !string.IsNullOrEmpty(EmptyTitle);
    public bool ShowDefaultEmpty => IsEmpty && string.IsNullOrEmpty(EmptyTitle);

    public bool IsEmpty => Origin is null || Destination is null;
    public bool ShowChip => !string.IsNullOrEmpty(RouteLabel) && !IsEmpty;
    public bool ShowDistance => !string.IsNullOrEmpty(DistanceText) && !IsEmpty;
    public double ChipMaxWidth => Bounds.Width > 0 ? Bounds.Width * 0.62 : 400;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        var p = change.Property;
        if (p == OriginProperty) Canvas.Origin = Origin;
        else if (p == DestinationProperty) Canvas.Destination = Destination;
        else if (p == CitiesProperty) Canvas.Cities = Cities;
        else if (p == CountriesProperty) Canvas.Countries = Countries;
        else if (p == ModeProperty) Canvas.Mode = Mode;
        else if (p == NoteKindProperty) UpdateNoteIcon();

        if (p == NoteProperty) Sync(ShowNoteProperty, ref _showNote, ShowNote);
        if (p == EmptyTitleProperty || p == OriginProperty || p == DestinationProperty)
        {
            Sync(ShowCustomEmptyProperty, ref _showCustomEmpty, ShowCustomEmpty);
            Sync(ShowDefaultEmptyProperty, ref _showDefaultEmpty, ShowDefaultEmpty);
        }
        if (p == OriginProperty || p == DestinationProperty || p == RouteLabelProperty || p == DistanceTextProperty)
        {
            Sync(IsEmptyProperty, ref _isEmpty, IsEmpty);
            Sync(ShowChipProperty, ref _showChip, ShowChip);
            Sync(ShowDistanceProperty, ref _showDistance, ShowDistance);
        }
        if (p == BoundsProperty)
        {
            var old = _chipMaxWidth;
            _chipMaxWidth = ChipMaxWidth;
            RaisePropertyChanged(ChipMaxWidthProperty, old, _chipMaxWidth);
        }
        if (p == OriginProperty || p == DestinationProperty || p == ModeProperty) UpdateStraightLine();
    }

    private void Sync(DirectPropertyBase<bool> property, ref bool cache, bool now)
    {
        var old = cache;
        cache = now;
        if (old != now) RaisePropertyChanged(property, old, now);
    }

    private void UpdateNoteIcon()
    {
        NoteIcon.IsVisible = NoteKind != MapNoteKind.Info;
        NoteIcon.Classes.Set("error", NoteKind == MapNoteKind.Error);
    }

    /// <summary>Posiciona o overlay de tooltip sobre o segmento desenhado e atualiza a visibilidade da nota "linha reta".</summary>
    private void UpdateStraightLine()
    {
        var seg = IsEmpty ? null : Canvas.StraightSegment();
        if (seg is { } s)
        {
            var dx = s.B.X - s.A.X;
            var dy = s.B.Y - s.A.Y;
            LineHit.Width = Math.Max(1, Math.Sqrt(dx * dx + dy * dy));
            LineHit.RenderTransform = new RotateTransform(Math.Atan2(dy, dx) * 180 / Math.PI);
            Canvas_SetPosition(LineHit, s.A.X, s.A.Y - LineHit.Height / 2);
        }
        Sync(ShowStraightLineProperty, ref _showStraightLine, seg is not null);
    }

    private static void Canvas_SetPosition(Control c, double x, double y)
    {
        Avalonia.Controls.Canvas.SetLeft(c, x);
        Avalonia.Controls.Canvas.SetTop(c, y);
    }

    private void UpdateAutomation()
    {
        var loc = Localizer.Instance;
        AutomationProperties.SetName(this, Origin is null || Destination is null
            ? loc["Route_A11y_Empty"]
            : loc.Get("Route_A11y", Origin.Label, Destination.Label, DistanceText ?? "").TrimEnd(',', ' '));
    }
}

/// <summary>Tipo da nota do rodapé do <see cref="RouteMap"/>: sem ícone, dica (ícone cinza) ou erro (ícone vermelho).</summary>
public enum MapNoteKind { Info, Hint, Error }
