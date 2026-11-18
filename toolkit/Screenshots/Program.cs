using System.Globalization;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Styling;
using Avalonia.Threading;
using OwnExpeditor.App;
using OwnExpeditor.App.Services;
using OwnExpeditor.Domain.Settings;

// Renderiza cada View do App (Window/UserControl em OwnExpeditor.App.Views*) sem abrir janela,
// em Light/Dark × pt-BR/en (a Galeria só existe em Debug), e grava PNGs em <out>/<View>/<tema>-<cultura>.png.
// DataContext: <Nome>ViewModel com construtor sem parâmetros (dados de exemplo), se existir.
// uso: Screenshots <out> [filtro-de-nome] [largura] [altura]
var outDir = args.Length > 0 ? args[0] : ".screenshots";
var filter = args.Length > 1 ? args[1] : "";
var width = args.Length > 2 ? int.Parse(args[2]) : 1280;
var height = args.Length > 3 ? int.Parse(args[3]) : 800;

AppBuilder.Configure<App>()
    .UseSkia()
    .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
    .SetupWithoutStarting();

var asm = typeof(App).Assembly;
var views = asm.GetTypes()
    .Where(t => t is { IsAbstract: false, IsPublic: true } && (t.Namespace?.StartsWith("OwnExpeditor.App.Views") == true || t.Namespace == "OwnExpeditor.App.Gallery")
                && (typeof(Window).IsAssignableFrom(t) || typeof(UserControl).IsAssignableFrom(t))
                && t.GetConstructor(Type.EmptyTypes) is not null
                && t.Name.Contains(filter, StringComparison.OrdinalIgnoreCase))
    .OrderBy(t => t.Name).ToList();
if (views.Count == 0) { Console.Error.WriteLine("nenhuma View encontrada"); return 1; }

var themes = new[] { ("light", ThemeVariant.Light), ("dark", ThemeVariant.Dark) };
var cultures = new[] { "pt-BR", "en" };
var count = 0; var failures = 0;
foreach (var type in views)
foreach (var (themeName, theme) in themes)
foreach (var culture in cultures)
{
    try
    {
        CultureInfo.CurrentUICulture = CultureInfo.CurrentCulture = new CultureInfo(culture);
        Localizer.Instance.SetLanguage(culture == "pt-BR" ? Language.PtBr : Language.En);
        Application.Current!.RequestedThemeVariant = theme;
        var view = (Control)Activator.CreateInstance(type)!;
        view.DataContext ??= CreateViewModel(asm, type);
        var window = view as Window ?? new Window { Content = view };
        window.Width = width; window.Height = height;
        window.Show();
        // Vários ciclos com pausa curta: transições (150-200 ms) e animações precisam de tempo de relógio.
        for (var i = 0; i < 8; i++)
        {
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Thread.Sleep(40);
        }
        var frame = window.CaptureRenderedFrame() ?? throw new InvalidOperationException("frame vazio");
        var path = Path.Combine(outDir, type.Name, $"{themeName}-{culture}.png");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        frame.Save(path);
        window.Close();
        count++;
    }
    catch (Exception e) { failures++; Console.Error.WriteLine($"FALHA {type.Name} {themeName} {culture}: {e.GetType().Name}: {e.Message}"); }
}
Console.WriteLine($"SCREENSHOTS: {count} em {Path.GetFullPath(outDir)} ({views.Count} views, {failures} falhas)");
return failures == 0 ? 0 : 1;

static object? CreateViewModel(Assembly asm, Type view)
{
    var baseName = view.Name.EndsWith("View") ? view.Name[..^4] : view.Name == "MainWindow" ? "Main" : view.Name;
    var vm = asm.GetTypes().FirstOrDefault(t => t.Name == baseName + "ViewModel" && t.GetConstructor(Type.EmptyTypes) is not null);
    return vm is null ? null : Activator.CreateInstance(vm);
}
