using Avalonia.Controls;

namespace OwnExpeditor.App.Modules.Jobs;

/// <summary>Campo do formulário de Fretes (um <c>JobPickerViewModel</c> no DataContext). Só visual.</summary>
public partial class JobPickerField : UserControl
{
    public JobPickerField() => InitializeComponent();

    /// <summary>Põe o foco no campo, sem abrir o popup.</summary>
    public void FocusField() => Picker.FocusField();
}
