using System.Threading.Tasks;

namespace OwnExpeditor.App.Services;

/// <summary>Abre pastas e arquivos no explorador do sistema.</summary>
public interface IShellLauncher
{
    Task<bool> OpenFolderAsync(string folderPath);

    /// <summary>Abre a pasta do arquivo (no Windows, com o arquivo selecionado).</summary>
    Task<bool> RevealFileAsync(string filePath);
}
