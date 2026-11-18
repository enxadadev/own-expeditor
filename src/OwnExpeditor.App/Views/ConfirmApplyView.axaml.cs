using Avalonia.Controls;

namespace OwnExpeditor.App.Views;

public partial class ConfirmApplyView : UserControl
{
    public ConfirmApplyView()
    {
        InitializeComponent();
        // Diálogo destrutivo: o foco inicial fica em Cancelar (design/review-apply, decisão 5).
        AttachedToVisualTree += (_, _) => this.FindControl<Button>("CancelButton")?.Focus();
    }
}
