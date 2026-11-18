using Avalonia.Controls;

namespace OwnExpeditor.App.Views;

public partial class ResultView : UserControl
{
    public ResultView()
    {
        InitializeComponent();
        // Foco inicial no título; Tab segue para Abrir pasta.
        AttachedToVisualTree += (_, _) => this.FindControl<TextBlock>("TitleText")?.Focus();
    }
}
