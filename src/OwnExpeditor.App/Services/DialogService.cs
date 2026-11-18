using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using OwnExpeditor.App.ViewModels;
using OwnExpeditor.Core.Text;

namespace OwnExpeditor.App.Services;

public sealed class DialogService(ILocalizer loc) : ObservableObject, IDialogService
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private DialogViewModel? _current;

    public DialogViewModel? Current
    {
        get => _current;
        private set => SetProperty(ref _current, value);
    }

    public async Task ShowAsync(DialogViewModel dialog)
    {
        await _gate.WaitAsync();
        try
        {
            Current = dialog;
            await dialog.WhenClosed;
        }
        finally
        {
            Current = null;
            dialog.Dispose();
            _gate.Release();
        }
    }

    public async Task<bool> ConfirmAsync(TextRef title, TextRef message, TextRef confirmLabel, bool destructive)
    {
        var dialog = new ConfirmDialogViewModel(loc, title, message, confirmLabel, destructive);
        await ShowAsync(dialog);
        return dialog.Result;
    }

    public Task ShowErrorAsync(TextRef message) =>
        ShowAsync(new ConfirmDialogViewModel(loc, TextRef.Of("Error_Title_Apply"), message, TextRef.Of("Common_Close"),
            destructive: false, showCancel: false));
}
