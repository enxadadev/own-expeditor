using System.Threading.Tasks;
using OwnExpeditor.Core.Text;

namespace OwnExpeditor.App.Services;

/// <summary>Seletor de pasta do sistema; <c>null</c> = cancelou.</summary>
public interface IFolderPicker
{
    Task<string?> PickFolderAsync(TextRef title, string? startPath);
}
