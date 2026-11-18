using Avalonia.Controls;

namespace OwnExpeditor.App.Views;

public partial class WelcomeView : UserControl
{
    public WelcomeView() => InitializeComponent();

    /// <summary>Ctrl+O sem sessão: foco na lista de perfis.</summary>
    public void FocusProfiles() => this.FindControl<SavePickerView>("Picker")?.FocusProfiles();
}
