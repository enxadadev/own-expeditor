using Avalonia.Controls;
using OwnExpeditor.App.ViewModels;

namespace OwnExpeditor.App.Views;

public partial class ShellWindow : Window
{
    private bool _exitConfirmed;

    public ShellWindow() => InitializeComponent();

    /// <summary>Fechar com pendências pede confirmação ("Sair sem aplicar?"); sem pendências fecha direto.</summary>
    protected override async void OnClosing(WindowClosingEventArgs e)
    {
        if (_exitConfirmed || DataContext is not ShellViewModel { PendingCount: > 0 } vm)
        {
            base.OnClosing(e);
            return;
        }
        e.Cancel = true;
        if (!await vm.ConfirmExitAsync()) return;
        _exitConfirmed = true;
        Close();
    }
}
