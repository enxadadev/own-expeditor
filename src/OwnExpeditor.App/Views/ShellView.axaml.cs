using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using OwnExpeditor.App.Shell;
using OwnExpeditor.App.ViewModels;

namespace OwnExpeditor.App.Views;

public partial class ShellView : UserControl
{
    private ShellViewModel? _vm;

    public ShellView()
    {
        InitializeComponent();
        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
        DataContextChanged += (_, _) =>
        {
            if (_vm is not null) _vm.FocusPickerRequested -= OnFocusPickerRequested;
            _vm = DataContext as ShellViewModel;
            if (_vm is not null) _vm.FocusPickerRequested += OnFocusPickerRequested;
        };
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is ShellViewModel vm && ShellShortcuts.TryExecute(vm, e.Key, e.KeyModifiers)) e.Handled = true;
    }

    /// <summary>Ctrl+O sem sessão: foco na lista de perfis da tela inicial.</summary>
    private void OnFocusPickerRequested(object? sender, System.EventArgs e) =>
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            this.GetVisualDescendants().OfType<WelcomeView>().FirstOrDefault()?.FocusProfiles(), Avalonia.Threading.DispatcherPriority.Loaded);
}
