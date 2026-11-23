using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Threading;
using OwnExpeditor.App.Controls;

namespace OwnExpeditor.App.Tests;

/// <summary>RouteCanvas geográfico: enquadramento (puro) e render real com Skia headless nos dois temas.</summary>
public class RouteMapGeographicTests
{
    private static readonly MapPoint A = new("Sofia", 0, 0, 1_200_000);
    private static readonly MapPoint B = new("Istanbul", 440_000, -190_000, 15_000_000);

    [Fact]
    public void Frame_PutsBothPointsInsideThePaddedViewAndCentersTheirMidpoint()
    {
        var view = RouteCanvas.Frame(A, B, 800, 300);
        var a = view.ToScreen(0, 0);
        var b = view.ToScreen(440_000, -190_000);

        Assert.All(new[] { a, b }, p => { Assert.InRange(p.X, 60, 740); Assert.InRange(p.Y, RouteCanvas.TopBand + 24 - 0.001, 300 - RouteCanvas.BottomBand - 24 + 0.001); });
        Assert.Equal(400, (a.X + b.X) / 2, 3);
        Assert.Equal(RouteCanvas.TopBand + (300 - RouteCanvas.TopBand - RouteCanvas.BottomBand) / 2, (a.Y + b.Y) / 2, 3);
        Assert.True(a.Y < b.Y); // sul (Y menor) fica mais abaixo na tela
    }

    [Fact]
    public void Frame_SameCity_DoesNotZoomToInfinity()
    {
        var view = RouteCanvas.Frame(A, A, 800, 300);

        Assert.True(double.IsFinite(view.Scale) && view.Scale <= 148.0 / 30_000 + 1e-9);
    }

    [Fact]
    public void PinRadius_GrowsWithPopulationWithinBounds()
    {
        Assert.Equal(2.5, RouteCanvas.PinRadius(null));
        Assert.True(RouteCanvas.PinRadius(2_000_000) > RouteCanvas.PinRadius(50_000));
        Assert.Equal(6, RouteCanvas.PinRadius(int.MaxValue));
    }

    [Theory]
    [InlineData("Dark", 0xF5, 0xA5, 0x24)]
    [InlineData("Light", 0xB8, 0x6E, 0x00)]
    public void Render_Geographic_DrawsAccentRouteOverThemedBackground(string theme, byte r, byte g, byte b)
    {
        var (accentPixels, corner) = Run(theme, geographic: true, r, g, b);

        Assert.True(accentPixels > 150, $"poucos pixels de destaque: {accentPixels}");
        Assert.Equal(theme == "Dark" ? (byte)0x10 : (byte)0xEF, corner); // fundo vem do token MapColor, sem cor fixa
    }

    [Fact]
    public void Render_Schematic_StillDrawsTheRouteWhenNoPositions()
    {
        var (accentPixels, _) = Run("Dark", geographic: false, 0xF5, 0xA5, 0x24);

        Assert.True(accentPixels > 150);
    }

    public static class Entry
    {
        public static AppBuilder BuildAvaloniaApp() =>
            AppBuilder.Configure<App>().UseSkia().UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
    }

    private static readonly Lazy<HeadlessUnitTestSession> Session = new(() => HeadlessUnitTestSession.StartNew(typeof(Entry)));

    private static (int Accent, byte CornerRed) Run(string theme, bool geographic, byte r, byte g, byte b)
    {
        return Session.Value.Dispatch(() =>
        {
            var map = new RouteMap
            {
                Width = 600, Height = 300, Mode = RouteMapMode.Auto,
                Origin = geographic ? A : new MapPoint("Sofia"), Destination = geographic ? B : new MapPoint("Istanbul"),
                Cities = geographic ? [new("Plovdiv", 130_000, -20_000, 340_000), new("Edirne", 300_000, -150_000, 170_000)] : null,
                Countries = geographic ? [new("Bulgaria", 120_000, 70_000)] : null,
            };
            var window = new Window { Width = 600, Height = 300, Content = map, RequestedThemeVariant = theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light };
            window.Show();
            Dispatcher.UIThread.RunJobs();
            var bmp = window.CaptureRenderedFrame()!;
            if (Environment.GetEnvironmentVariable("MAP_DUMP") is { } d) bmp.Save(System.IO.Path.Combine(d, $"{theme}-{geographic}.png"));
            using var l = bmp.Lock();
            var data = new byte[l.RowBytes * l.Size.Height];
            System.Runtime.InteropServices.Marshal.Copy(l.Address, data, 0, data.Length);
            var count = 0;
            byte corner = 0;
            for (var y = 0; y < l.Size.Height; y++)
                for (var x = 0; x < l.Size.Width; x++)
                {
                    var o = y * l.RowBytes + x * 4;
                    var (ri, bi) = l.Format == PixelFormat.Bgra8888 ? (2, 0) : (0, 2);
                    if (x == 15 && y == 15) corner = data[o + ri];
                    if (Math.Abs(data[o + ri] - r) < 10 && Math.Abs(data[o + 1] - g) < 10 && Math.Abs(data[o + bi] - b) < 10) count++;
                }
            window.Close();
            return (count, corner);
        }, default).GetAwaiter().GetResult();
    }
}
