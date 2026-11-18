using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace OwnExpeditor.App.Shell;

/// <summary>Conversores de apresentação do shell (nada de regra de negócio).</summary>
public static class ShellConverters
{
    /// <summary>Chave de ícone (<c>IconNavJobs</c>) → geometria dos recursos.</summary>
    public static readonly FuncValueConverter<string?, Geometry?> Icon = new(key =>
        key is not null && Application.Current is { } app && app.TryGetResource(key, app.ActualThemeVariant, out var v) ? v as Geometry : null);

    /// <summary>Passo de carga pendente fica esmaecido.</summary>
    public static readonly FuncValueConverter<bool, double> DimWhen = new(dim => dim ? 0.55 : 1.0);

    public static readonly FuncValueConverter<bool, string> Chevron = new(open => open ? "▾" : "▸");
}
