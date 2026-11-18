using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace OwnExpeditor.App.Controls;

public enum RouteMapMode { Auto, Schematic, Geographic }

/// <summary>Desenho do mapa (grade, cidades, rota, nós e rótulos). Cores vêm dos recursos do tema atual.</summary>
internal sealed class RouteCanvas : Control
{
    public static readonly StyledProperty<MapPoint?> OriginProperty = AvaloniaProperty.Register<RouteCanvas, MapPoint?>(nameof(Origin));
    public static readonly StyledProperty<MapPoint?> DestinationProperty = AvaloniaProperty.Register<RouteCanvas, MapPoint?>(nameof(Destination));
    public static readonly StyledProperty<IEnumerable<MapPoint>?> CitiesProperty = AvaloniaProperty.Register<RouteCanvas, IEnumerable<MapPoint>?>(nameof(Cities));
    public static readonly StyledProperty<RouteMapMode> ModeProperty = AvaloniaProperty.Register<RouteCanvas, RouteMapMode>(nameof(Mode));

    static RouteCanvas()
    {
        AffectsRender<RouteCanvas>(OriginProperty, DestinationProperty, CitiesProperty, ModeProperty);
    }

    public RouteCanvas()
    {
        IsHitTestVisible = false;
        ActualThemeVariantChanged += (_, _) => InvalidateVisual();
    }

    public MapPoint? Origin { get => GetValue(OriginProperty); set => SetValue(OriginProperty, value); }
    public MapPoint? Destination { get => GetValue(DestinationProperty); set => SetValue(DestinationProperty, value); }
    public IEnumerable<MapPoint>? Cities { get => GetValue(CitiesProperty); set => SetValue(CitiesProperty, value); }
    public RouteMapMode Mode { get => GetValue(ModeProperty); set => SetValue(ModeProperty, value); }

    public bool IsGeographic =>
        Mode == RouteMapMode.Geographic || (Mode == RouteMapMode.Auto && Origin?.HasCoordinates == true && Destination?.HasCoordinates == true);

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var w = Bounds.Width;
        var h = Bounds.Height;
        if (w <= 0 || h <= 0) return;

        var grid = Brush("MapGridBrush");
        if (grid is not null)
        {
            var pen = new Pen(grid, 1);
            for (var x = 32.0; x < w; x += 32) context.DrawLine(pen, new Point(x, 0), new Point(x, h));
            for (var y = 32.0; y < h; y += 32) context.DrawLine(pen, new Point(0, y), new Point(w, y));
        }

        if (Origin is null || Destination is null) return;
        var geographic = IsGeographic;
        var (a, b) = geographic ? Project(w, h) : (new Point(w * 0.2, h * 0.66), new Point(w * 0.8, h * 0.34));
        var sameCity = Origin.Label == Destination.Label && !geographic;

        if (geographic) DrawCities(context, w, h);

        var accent = Brush("AccentBrush") ?? Brushes.Orange;
        var text = Brush("TextBrush") ?? Brushes.White;
        if (!sameCity)
        {
            var mid = new Point((a.X + b.X) / 2, (a.Y + b.Y) / 2);
            var dx = b.X - a.X;
            var dy = b.Y - a.Y;
            var len = Math.Max(1, Math.Sqrt(dx * dx + dy * dy));
            var ctrl = new Point(mid.X - dy / len * len * 0.18, mid.Y + dx / len * len * 0.18);
            var geo = new StreamGeometry();
            using (var g = geo.Open())
            {
                g.BeginFigure(a, false);
                g.QuadraticBezierTo(ctrl, b);
                g.EndFigure(false);
            }
            if (Brush("RouteGlowBrush") is { } glow)
                context.DrawGeometry(null, new Pen(glow, 12, lineCap: PenLineCap.Round), geo);
            context.DrawGeometry(null, new Pen(accent, 4, lineCap: PenLineCap.Round), geo);
        }

        var fill = Brush("MapBrush") ?? Brushes.Black;
        context.DrawEllipse(fill, new Pen(accent, 4), a, 9, 9);
        if (!sameCity) context.DrawEllipse(accent, null, b, 10, 10);

        DrawLabel(context, sameCity ? Origin.Label : Origin.Label, a, text, below: true, w);
        if (!sameCity) DrawLabel(context, Destination.Label, b, text, below: false, w);
    }

    private (Point, Point) Project(double w, double h)
    {
        var pts = new List<MapPoint> { Origin!, Destination! };
        if (Cities is not null) pts.AddRange(Cities.Where(c => c.HasCoordinates));
        var minX = pts.Where(p => p.HasCoordinates).Min(p => p.Longitude!.Value);
        var maxX = pts.Where(p => p.HasCoordinates).Max(p => p.Longitude!.Value);
        var minY = pts.Where(p => p.HasCoordinates).Min(p => p.Latitude!.Value);
        var maxY = pts.Where(p => p.HasCoordinates).Max(p => p.Latitude!.Value);
        _bounds = (minX, maxX, minY, maxY);
        return (Map(Origin!, w, h), Map(Destination!, w, h));
    }

    private (double MinX, double MaxX, double MinY, double MaxY) _bounds;

    private Point Map(MapPoint p, double w, double h)
    {
        const double pad = 72;
        var spanX = Math.Max(1e-9, _bounds.MaxX - _bounds.MinX);
        var spanY = Math.Max(1e-9, _bounds.MaxY - _bounds.MinY);
        var scale = Math.Min((w - 2 * pad) / spanX, (h - 2 * pad) / spanY);
        var offX = (w - spanX * scale) / 2;
        var offY = (h - spanY * scale) / 2;
        return new Point(offX + (p.Longitude!.Value - _bounds.MinX) * scale, h - (offY + (p.Latitude!.Value - _bounds.MinY) * scale));
    }

    private void DrawCities(DrawingContext context, double w, double h)
    {
        if (Cities is null) return;
        var dot = Brush("MapCityDotBrush");
        var label = Brush("MapCityTextBrush");
        foreach (var c in Cities.Where(c => c.HasCoordinates))
        {
            var p = Map(c, w, h);
            if (dot is not null) context.DrawEllipse(dot, null, p, 3, 3);
            if (label is not null) context.DrawText(Format(c.Label, 13, label, FontWeight.Normal), new Point(p.X + 7, p.Y - 8));
        }
    }

    private void DrawLabel(DrawingContext context, string label, Point node, IBrush brush, bool below, double w)
    {
        var ft = Format(label, 15, brush, FontWeight.SemiBold);
        ft.MaxTextWidth = 160;
        ft.MaxLineCount = 2;
        ft.Trimming = TextTrimming.CharacterEllipsis;
        var x = Math.Clamp(node.X - ft.Width / 2, 8, Math.Max(8, w - ft.Width - 8));
        var y = below ? node.Y + 16 : node.Y - 16 - ft.Height;
        context.DrawText(ft, new Point(x, y));
    }

    private FormattedText Format(string text, double size, IBrush brush, FontWeight weight)
    {
        var family = this.TryFindResource("FontUi", ActualThemeVariant, out var f) && f is FontFamily ff ? ff : FontFamily.Default;
        return new FormattedText(text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface(family, FontStyle.Normal, weight), size, brush);
    }

    private IBrush? Brush(string key) => this.TryFindResource(key, ActualThemeVariant, out var v) ? v as IBrush : null;
}
