using Avalonia.Controls;

namespace OwnExpeditor.App.Views;

public partial class OpenErrorView : UserControl
{
    public OpenErrorView()
    {
        InitializeComponent();
        // O leitor de tela anuncia o erro: o foco inicial vai para o título.
        AttachedToVisualTree += (_, _) => this.FindControl<TextBlock>("TitleText")?.Focus();
    }
}
