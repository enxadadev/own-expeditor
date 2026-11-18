using Avalonia.Controls;

namespace OwnExpeditor.App.Controls;

/// <summary>Anel girando (única animação contínua do app); o tamanho vem de Width/Height (28 padrão, 14 em campos).</summary>
public partial class Spinner : UserControl
{
    public Spinner()
    {
        InitializeComponent();
        SizeChanged += (_, e) =>
        {
            var thickness = e.NewSize.Width < 20 ? 2 : 3;
            Track.StrokeThickness = Arc.StrokeThickness = thickness;
        };
    }
}
