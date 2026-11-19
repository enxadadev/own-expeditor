using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace OwnExpeditor.App.Modules.Jobs;

/// <summary>Só o que é puramente visual: levar o foco ao próximo campo incompleto quando o ViewModel pede.</summary>
public partial class JobsView : UserControl
{
    /// <summary>Abaixo disto (px úteis) os cartões Saída e Chegada empilham e o ⇄ vira o botão "Trocar".</summary>
    private const double NarrowBelow = 1100;

    private JobsPageViewModel? _vm;
    private bool? _narrow;

    public JobsView()
    {
        InitializeComponent();
        SizeChanged += (_, e) => ApplyLayout(e.NewSize.Width < NarrowBelow);
        DataContextChanged += (_, _) =>
        {
            if (_vm is not null) _vm.FocusRequested -= OnFocusRequested;
            _vm = DataContext as JobsPageViewModel;
            if (_vm is not null) _vm.FocusRequested += OnFocusRequested;
        };
    }

    private void ApplyLayout(bool narrow)
    {
        if (_narrow == narrow) return;
        _narrow = narrow;
        SidesGrid.Classes.Set("narrow", narrow);
        SidesGrid.ColumnDefinitions = ColumnDefinitions.Parse(narrow ? "*" : "*,Auto,*");
        SidesGrid.RowDefinitions = RowDefinitions.Parse(narrow ? "Auto,Auto,Auto" : "Auto");
        SidesGrid.RowSpacing = narrow ? 8 : 0;
        Place(FromCard, 0, 0);
        Place(SwapButton, narrow ? 1 : 0, narrow ? 0 : 1);
        Place(ToCard, narrow ? 2 : 0, narrow ? 0 : 2);
        SwapButton.Width = narrow ? double.NaN : 44;
        SwapButton.MinWidth = 44;
        SwapButton.Padding = narrow ? new Thickness(14, 0) : default;
        SwapLabel.IsVisible = narrow;
        SwapIcon.RenderTransform = narrow ? new RotateTransform(90) : null; // ⇅ quando empilhados
    }

    private static void Place(Control c, int row, int column)
    {
        Grid.SetRow(c, row);
        Grid.SetColumn(c, column);
    }

    private void OnFocusRequested(object? sender, JobsFocusTarget target)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            switch (target)
            {
                case JobsFocusTarget.FromCountry: FromCountryField.FocusField(); break;
                case JobsFocusTarget.FromCity: FromCityField.FocusField(); break;
                case JobsFocusTarget.FromCompany: FromCompanyField.FocusField(); break;
                case JobsFocusTarget.Slot: SlotField.FocusField(); break;
                case JobsFocusTarget.Swap: SwapButton.Focus(); break;
                case JobsFocusTarget.ToCountry: ToCountryField.FocusField(); break;
                case JobsFocusTarget.ToCity: ToCityField.FocusField(); break;
                case JobsFocusTarget.ToCompany: ToCompanyField.FocusField(); break;
                case JobsFocusTarget.Cargo: CargoField.FocusField(); break;
                case JobsFocusTarget.Add: AddButton.Focus(); break;
            }
        });
    }
}
