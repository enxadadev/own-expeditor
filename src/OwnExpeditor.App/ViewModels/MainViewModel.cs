using CommunityToolkit.Mvvm.ComponentModel;

namespace OwnExpeditor.App.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _greeting = "Own Expeditor";
}
