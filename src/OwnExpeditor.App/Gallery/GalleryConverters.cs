using System.Globalization;
using Avalonia.Data.Converters;

namespace OwnExpeditor.App.Gallery;

public static class GalleryConverters
{
    public static readonly FuncValueConverter<string?, string?> Upper = new(s => s?.ToUpper(CultureInfo.CurrentCulture));

    /// <summary>Coluna Light ocupa as duas quando o Dark está oculto.</summary>
    public static readonly FuncValueConverter<bool, int> SpanIfAlone = new(darkVisible => darkVisible ? 1 : 2);
}
