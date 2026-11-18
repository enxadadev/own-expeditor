using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using OwnExpeditor.App.Controls;

namespace OwnExpeditor.App.Gallery;

/// <summary>
/// Força estados visuais na Galeria (<c>g:Gallery.ForceState="Hover|Pressed|Focus|Disabled"</c>), pois não dá para
/// segurar o mouse numa captura. Liga as pseudo-classes em vez de depender de entrada real.
/// </summary>
public static class Gallery
{
    public static readonly AttachedProperty<string?> ForceStateProperty =
        AvaloniaProperty.RegisterAttached<Control, Control, string?>("ForceState");

    static Gallery()
    {
        ForceStateProperty.Changed.AddClassHandler<Control>((c, e) =>
        {
            var state = e.NewValue as string;
            if (c.IsLoaded) Apply(c, state); else c.Loaded += (_, _) => Apply(c, state);
        });
    }

    public static string? GetForceState(Control c) => c.GetValue(ForceStateProperty);

    public static void SetForceState(Control c, string? value) => c.SetValue(ForceStateProperty, value);

    private static void Apply(Control c, string? state)
    {
        if (state == "Disabled") { c.IsEnabled = false; return; }
        var targets = new[] { c }.Concat(c.GetVisualDescendants().OfType<Control>());
        foreach (var t in targets)
        {
            var pc = (IPseudoClasses)t.Classes;
            if (t is Button)
            {
                pc.Set(":pointerover", state == "Hover");
                pc.Set(":pressed", state == "Pressed");
                pc.Set(":focus-visible", state == "Focus");
            }
            else if (t is Border b && b.Classes.Contains("field"))
            {
                pc.Set(":pointerover", state == "Hover");
                pc.Set(":focus-within", state == "Focus");
            }
        }
        if (c is RouteOfferItem item) item.ForceVisualState(state);
    }
}
