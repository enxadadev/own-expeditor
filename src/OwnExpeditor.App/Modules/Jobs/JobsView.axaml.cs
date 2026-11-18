using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.VisualTree;
using OwnExpeditor.App.Controls;

namespace OwnExpeditor.App.Modules.Jobs;

/// <summary>Só o que é puramente visual: levar o foco ao próximo passo incompleto quando o ViewModel pede.</summary>
public partial class JobsView : UserControl
{
    private JobsPageViewModel? _vm;

    public JobsView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
        {
            if (_vm is not null) _vm.FocusRequested -= OnFocusRequested;
            _vm = DataContext as JobsPageViewModel;
            if (_vm is not null) _vm.FocusRequested += OnFocusRequested;
        };
    }

    private void OnFocusRequested(object? sender, JobsFocusTarget target)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            switch (target)
            {
                case JobsFocusTarget.CityList:
                    if (CityList.ContainerFromIndex(Math.Max(CityList.SelectedIndex, 0)) is { } item) item.Focus();
                    else CityList.Focus();
                    break;
                case JobsFocusTarget.ToCity: ToCityPicker.FocusField(); break;
                case JobsFocusTarget.FromCompany: FromCompanyPicker.FocusField(); break;
                case JobsFocusTarget.Cargo: CargoPicker.FocusField(); break;
                case JobsFocusTarget.ToCompany: ToCompanyPicker.FocusField(); break;
                case JobsFocusTarget.Offer: OfferPicker.FocusField(); break;
                case JobsFocusTarget.Mode: ModeList.Focus(); break;
                case JobsFocusTarget.Add: AddButton.Focus(); break;
            }
        });
    }
}
