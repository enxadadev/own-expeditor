using System;
using Avalonia;
using Avalonia.Controls;
using OwnExpeditor.App.Services;

namespace OwnExpeditor.App.Controls;

/// <summary>Mantém nomes acessíveis compostos (ex.: "{0}: {1}") atualizados ao mudar propriedade ou idioma.</summary>
internal static class Automation
{
    public static void Track(Control control, Action update, params AvaloniaProperty[] properties)
    {
        void OnCulture(object? s, EventArgs e) => update();
        control.AttachedToVisualTree += (_, _) => { Localizer.Instance.CultureChanged += OnCulture; update(); };
        control.DetachedFromVisualTree += (_, _) => Localizer.Instance.CultureChanged -= OnCulture;
        control.PropertyChanged += (_, e) =>
        {
            if (Array.IndexOf(properties, e.Property) >= 0) update();
        };
    }
}
