using Avalonia.Controls;
using OwnExpeditor.App.ViewModels;

namespace OwnExpeditor.App.Views;

public partial class ConfirmDialogView : UserControl
{
    public ConfirmDialogView()
    {
        InitializeComponent();
        // Destrutivo: foco inicial em Cancelar; sem cancelar, no único botão.
        AttachedToVisualTree += (_, _) =>
        {
            var target = DataContext is ConfirmDialogViewModel { ShowCancel: true } ? "CancelButton" : "ConfirmButton";
            this.FindControl<Button>(target)?.Focus();
        };
    }
}
