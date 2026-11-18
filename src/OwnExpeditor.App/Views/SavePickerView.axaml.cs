using Avalonia.Controls;
using Avalonia.Input;
using OwnExpeditor.App.ViewModels;

namespace OwnExpeditor.App.Views;

public partial class SavePickerView : UserControl
{
    public SavePickerView()
    {
        InitializeComponent();
        var saves = this.FindControl<ListBox>("SaveList")!;
        saves.DoubleTapped += (_, _) => ActivateSelected();
        saves.KeyDown += (_, e) =>
        {
            if (e.Key != Key.Enter) return;
            ActivateSelected();
            e.Handled = true;
        };
    }

    /// <summary>Foco inicial: a lista de perfis (Ctrl+O sem sessão).</summary>
    public void FocusProfiles() => this.FindControl<ListBox>("ProfileList")?.Focus();

    private void ActivateSelected()
    {
        if (DataContext is SavePickerViewModel { SelectedSave: { } save } vm) vm.Activate(save);
    }
}
