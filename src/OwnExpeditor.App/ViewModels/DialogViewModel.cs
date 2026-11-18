using System.Threading.Tasks;
using OwnExpeditor.App.Services;

namespace OwnExpeditor.App.ViewModels;

/// <summary>Conteúdo de um diálogo modal (mostrado pelo <see cref="IDialogService"/>).</summary>
public abstract class DialogViewModel(ILocalizer loc) : LocalizedViewModel(loc)
{
    private readonly TaskCompletionSource _closed = new();

    public Task WhenClosed => _closed.Task;

    /// <summary>Esc fecha o diálogo (falso enquanto grava).</summary>
    public virtual bool CanCancelWithEscape => true;

    /// <summary>Esc ou clique fora: o mesmo que Cancelar.</summary>
    public virtual void Cancel() => Close();

    protected void Close() => _closed.TrySetResult();
}

/// <summary>Pergunta com Confirmar/Cancelar; <c>destructive</c> usa o botão de perigo e foco inicial em Cancelar.</summary>
public sealed class ConfirmDialogViewModel : DialogViewModel
{
    private readonly Core.Text.TextRef _title;
    private readonly Core.Text.TextRef _message;
    private readonly Core.Text.TextRef _confirm;

    public ConfirmDialogViewModel(ILocalizer loc, Core.Text.TextRef title, Core.Text.TextRef message,
        Core.Text.TextRef confirmLabel, bool destructive, bool showCancel = true) : base(loc)
    {
        _title = title;
        _message = message;
        _confirm = confirmLabel;
        IsDestructive = destructive;
        ShowCancel = showCancel;
        ConfirmCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(Confirm);
        CancelCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(Cancel);
    }

    public string Title => Loc.Format(_title);

    public string TitleUpper => Title.ToUpper(Loc.Culture);

    public string Message => Loc.Format(_message);

    public string ConfirmLabel => Loc.Format(_confirm);

    public string CancelLabel => Loc["Common_Cancel"];

    public bool IsDestructive { get; }

    public bool ShowCancel { get; }

    public bool Result { get; private set; }

    public System.Windows.Input.ICommand ConfirmCommand { get; }

    public System.Windows.Input.ICommand CancelCommand { get; }

    private void Confirm()
    {
        Result = true;
        Close();
    }
}
