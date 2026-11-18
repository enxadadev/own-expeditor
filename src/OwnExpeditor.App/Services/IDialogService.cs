using System.ComponentModel;
using System.Threading.Tasks;
using OwnExpeditor.App.ViewModels;
using OwnExpeditor.Core.Text;

namespace OwnExpeditor.App.Services;

/// <summary>Diálogos modais do shell (uma camada sobre a página). Um de cada vez; os seguintes esperam.</summary>
public interface IDialogService : INotifyPropertyChanged
{
    /// <summary>Diálogo aberto agora (ou <c>null</c>).</summary>
    DialogViewModel? Current { get; }

    /// <summary>Mostra o diálogo e espera ele fechar.</summary>
    Task ShowAsync(DialogViewModel dialog);

    Task<bool> ConfirmAsync(TextRef title, TextRef message, TextRef confirmLabel, bool destructive);

    Task ShowErrorAsync(TextRef message);
}
