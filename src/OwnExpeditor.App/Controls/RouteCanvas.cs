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
    public static readonly StyledProperty<IEnumerable<MapPoint>?> CountriesProperty = AvaloniaProperty.Register<RouteCanvas, IEnumerable<MapPoint>?>(nameof(Countries));
    public static readonly StyledProperty<RouteMapMode> ModeProperty = AvaloniaProperty.Register<RouteCanvas, RouteMapMode>(nameof(Mode));

    static RouteCanvas()
    {
        AffectsRender<RouteCanvas>(OriginProperty, DestinationProperty, CitiesProperty, CountriesProperty, ModeProperty);
    }

    public RouteCanvas()
    {
        IsHitTestVisible = false;
        ActualThemeVariantChanged += (_, _) => InvalidateVisual();
    }

    public MapPoint? Origin { get => GetValue(OriginProperty); set => SetValue(OriginProperty, value); }
    public MapPoint? Destination { get => GetValue(DestinationProperty); set => SetValue(DestinationProperty, value); }
    public IEnumerable<MapPoint>? Cities { get => GetValue(CitiesProperty); set => SetValue(CitiesProperty, value); }
    /// <summary>Rótulos de país (posição = âncora do nome); só no modo geográfico.</summary>
    public IEnumerable<MapPoint>? Countries { get => GetValue(CountriesProperty); set => SetValue(CountriesProperty, value); }
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
            for (var x = 32.0; x < w; x += 32) context.DrawLine(pen, new Point(x + 0.5, 0), new Point(x + 0.5, h));
            for (var y = 32.0; y < h; y += 32) context.DrawLine(pen, new Point(0, y + 0.5), new Point(w, y + 0.5));
        }

        if (Origin is null || Destination is null) return;
        if (IsGeographic)
        {
            RenderGeographic(context, w, h);
            return;
        }
        var (a, b) = (new Point(w * 0.2, h * 0.66), new Point(w * 0.8, h * 0.34));
        var sameCity = Origin.Label == Destination.Label;

        if (Brush("AccentBrush") is not { } accent || Brush("TextBrush") is not { } text || Brush("MapBrush") is not { } fill) return; // sem recurso do tema, não desenha
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

        context.DrawEllipse(fill, new Pen(accent, 4), a, 9, 9);
        if (!sameCity) context.DrawEllipse(accent, null, b, 10, 10);

        DrawLabel(context, Origin.Label, a, text, below: true, w);
        if (!sameCity) DrawLabel(context, Destination.Label, b, text, below: false, w);
    }

    // ---- modo geográfico (design/mapa-jogo): metros do mundo, norte para cima, enquadrando saída e chegada

    private const double GeoPad = 72;
    /// <summary>Faixas reservadas aos chips de rota (topo) e de distância/nota (base); a rota não passa por elas.</summary>
    internal const double TopBand = 56;
    internal const double BottomBand = 48;
    private const double VerticalPad = 24;
    private const double MinSpanMeters = 30_000;

    /// <summary>Projeção metros → pixels: uma escala só nos dois eixos; centro entre as duas pontas.</summary>
    internal readonly record struct GeoView(double CenterX, double CenterY, double Scale, double W, double H, double ScreenCenterY)
    {
        public Point ToScreen(double x, double y) => new(W / 2 + (x - CenterX) * Scale, ScreenCenterY - (y - CenterY) * Scale);
    }

    internal static GeoView Frame(MapPoint a, MapPoint b, double w, double h)
    {
        var spanX = Math.Max(MinSpanMeters, Math.Abs(a.X!.Value - b.X!.Value));
        var spanY = Math.Max(MinSpanMeters, Math.Abs(a.Y!.Value - b.Y!.Value));
        var usableH = h - TopBand - BottomBand;
        var scale = Math.Min(Math.Max(1, w - 2 * GeoPad) / spanX, Math.Max(1, usableH - 2 * VerticalPad) / spanY);
        return new GeoView((a.X.Value + b.X.Value) / 2, (a.Y!.Value + b.Y.Value) / 2, scale, w, h, TopBand + usableH / 2);
    }

    /// <summary>Pontas da linha reta em pixels (modo geográfico com as duas pontas distantes); nulo fora disso. Serve ao overlay de tooltip.</summary>
    internal (Point A, Point B)? StraightSegment()
    {
        if (!IsGeographic || Origin is null || Destination is null || Bounds.Width <= 0 || Bounds.Height <= 0) return null;
        var view = Frame(Origin, Destination, Bounds.Width, Bounds.Height);
        var a = view.ToScreen(Origin.X!.Value, Origin.Y!.Value);
        var b = view.ToScreen(Destination.X!.Value, Destination.Y!.Value);
        return Math.Abs(a.X - b.X) < 6 && Math.Abs(a.Y - b.Y) < 6 ? null : (a, b);
    }

    private void RenderGeographic(DrawingContext context, double w, double h)
    {
        var view = Frame(Origin!, Destination!, w, h);
        var a = view.ToScreen(Origin!.X!.Value, Origin.Y!.Value);
        var b = view.ToScreen(Destination!.X!.Value, Destination.Y!.Value);
        if (Brush("AccentBrush") is not { } accent || Brush("TextBrush") is not { } text || Brush("OnAccentBrush") is not { } onAccent) return;
        var area = new Rect(0, 0, w, h);
        var taken = new List<Rect>();

        // nomes das pontas primeiro (têm prioridade): reservam o espaço antes dos países e das cidades de fundo
        var near = Math.Abs(a.X - b.X) < 6 && Math.Abs(a.Y - b.Y) < 6;
        var oLabel = LabelRect(Origin.Label, a, near ? -1 : (a.Y <= b.Y ? -1 : 1), w, text);
        var dLabel = LabelRect(Destination.Label, b, near ? -1 : (b.Y < a.Y ? -1 : 1), w, text);
        taken.Add(oLabel.Rect.Inflate(3));
        if (!near) taken.Add(dLabel.Rect.Inflate(3));

        DrawCountries(context, view, area, taken);

        // fase 1: linha reta tracejada, sem halo (o halo é só da rota sólida)
        if (!near) context.DrawLine(new Pen(accent, 3, new DashStyle([2.2, 2.2], 0), PenLineCap.Round), a, b);

        DrawCities(context, view, area, taken, [oLabel.Rect, dLabel.Rect], a, b);

        DrawFlag(context, a, accent, onAccent, finish: false);
        DrawFlag(context, b, accent, onAccent, finish: true);
        context.DrawText(oLabel.Text, oLabel.Rect.TopLeft);
        if (!near) context.DrawText(dLabel.Text, dLabel.Rect.TopLeft);
    }

    private void DrawCountries(DrawingContext context, GeoView view, Rect area, List<Rect> taken)
    {
        if (Countries is null || Brush("MapCountryTextBrush") is not { } brush) return;
        foreach (var c in Countries.Where(c => c.HasCoordinates))
        {
            var p = view.ToScreen(c.X!.Value, c.Y!.Value);
            var ft = Format(c.Label.ToUpper(CultureInfo.CurrentUICulture), 12, brush, FontWeight.SemiBold);
            ft.MaxTextWidth = 180;
            ft.MaxLineCount = 1;
            ft.Trimming = TextTrimming.CharacterEllipsis;
            var r = new Rect(p.X - ft.Width / 2, p.Y - ft.Height / 2, ft.Width, ft.Height);
            if (!area.Contains(r)) continue;
            context.DrawText(ft, r.TopLeft);
            taken.Add(r.Inflate(2));
        }
    }

    private void DrawCities(DrawingContext context, GeoView view, Rect area, List<Rect> taken, Rect[] reserved, Point a, Point b)
    {
        if (Cities is null) return;
        var dot = Brush("MapCityDotBrush");
        var label = Brush("MapCityTextBrush");
        if (dot is null) return;
        var placed = new List<Rect>(reserved);
        var drawn = 0;
        foreach (var c in Cities.Where(c => c.HasCoordinates).OrderByDescending(c => c.Population ?? 0).ThenBy(c => c.Label, StringComparer.Ordinal))
        {
            var p = view.ToScreen(c.X!.Value, c.Y!.Value);
            if (!area.Contains(p) || Near(p, a) || Near(p, b)) continue;
            var r = PinRadius(c.Population);
            context.DrawEllipse(dot, null, p, r, r);
            if (label is null || drawn >= 80) continue;
            var ft = Format(c.Label, 13, label, FontWeight.Normal);
            var rect = new Rect(p.X + r + 3, p.Y - ft.Height / 2, ft.Width, ft.Height);
            var padded = rect.Inflate(3);
            if (!area.Contains(rect) || placed.Any(x => x.Intersects(padded)) || CrossesLine(padded, a, b) || taken.Any(x => x.Intersects(padded))) continue;
            context.DrawText(ft, rect.TopLeft);
            placed.Add(rect);
            drawn++;
        }

        static bool CrossesLine(Rect r, Point p, Point q)
        {
            var steps = (int)Math.Clamp(Math.Max(Math.Abs(q.X - p.X), Math.Abs(q.Y - p.Y)) / 4, 1, 400);
            for (var i = 0; i <= steps; i++)
                if (r.Contains(new Point(p.X + (q.X - p.X) * i / steps, p.Y + (q.Y - p.Y) * i / steps))) return true;
            return false;
        }

        static bool Near(Point p, Point q) => Math.Abs(p.X - q.X) < 5 && Math.Abs(p.Y - q.Y) < 5;
    }

    /// <summary>Pino por população: de 2,5 a 6 px (log; sem dado, o menor).</summary>
    internal static double PinRadius(int? population) =>
        population is > 0 ? Math.Clamp(2.5 + (Math.Log10(population.Value) - 4) * 1.75, 2.5, 6) : 2.5;

    private (FormattedText Text, Rect Rect) LabelRect(string label, Point node, int dir, double w, IBrush brush)
    {
        var ft = Format(label, 15, brush, FontWeight.SemiBold);
        ft.MaxTextWidth = 160;
        ft.MaxLineCount = 2;
        ft.Trimming = TextTrimming.CharacterEllipsis;
        var x = Math.Clamp(node.X - ft.Width / 2, 8, Math.Max(8, w - ft.Width - 8));
        var y = dir > 0 ? node.Y + 14 : node.Y - FlagHeight - 6 - ft.Height;
        return (ft, new Rect(x, y, ft.Width, ft.Height));
    }

    private const double FlagHeight = 26;

    /// <summary>Bandeira desenhada pelo app: mastro + galhardete (saída) ou bandeira quadriculada (chegada).</summary>
    private static void DrawFlag(DrawingContext context, Point p, IBrush accent, IBrush onAccent, bool finish)
    {
        context.DrawEllipse(accent, null, p, 4, 4);
        var top = new Point(p.X, p.Y - FlagHeight);
        context.DrawLine(new Pen(accent, 2, lineCap: PenLineCap.Round), p, top);
        if (!finish)
        {
            var g = new StreamGeometry();
            using (var c = g.Open())
            {
                c.BeginFigure(top, true);
                c.LineTo(new Point(top.X + 16, top.Y + 5));
                c.LineTo(new Point(top.X, top.Y + 10));
                c.EndFigure(true);
            }
            context.DrawGeometry(accent, null, g);
            return;
        }
        const double cell = 5;
        for (var row = 0; row < 3; row++)
            for (var col = 0; col < 4; col++)
                context.DrawRectangle((row + col) % 2 == 0 ? accent : onAccent, null, new Rect(top.X + 1 + col * cell, top.Y + row * cell, cell, cell));
        context.DrawRectangle(null, new Pen(accent, 1), new Rect(top.X + 1, top.Y, 4 * cell, 3 * cell));
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
