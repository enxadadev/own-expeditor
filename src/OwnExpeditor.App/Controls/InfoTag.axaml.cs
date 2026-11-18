using Avalonia;
using Avalonia.Controls;

namespace OwnExpeditor.App.Controls;

/// <summary>Etiqueta em pílula ("Estimada", "Sem combinação"): nunca só por cor, sempre com texto (design/fretes §2).</summary>
public partial class InfoTag : UserControl
{
    public static readonly StyledProperty<string?> TextProperty = AvaloniaProperty.Register<InfoTag, string?>(nameof(Text));

    public InfoTag() => InitializeComponent();

    public string? Text { get => GetValue(TextProperty); set => SetValue(TextProperty, value); }
}
