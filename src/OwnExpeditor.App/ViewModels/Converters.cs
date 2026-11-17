using Avalonia.Data.Converters;
using Avalonia.Media;

namespace OwnExpeditor.App.ViewModels;

public static class Converters
{
    public static readonly IValueConverter ErrorBrush =
        new FuncValueConverter<bool, IBrush?>(isError => isError ? Brushes.OrangeRed : null);
}
