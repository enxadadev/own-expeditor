using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace OwnExpeditor.App.Services;

public sealed class ShellLauncher : IShellLauncher
{
    public Task<bool> OpenFolderAsync(string folderPath) =>
        Task.FromResult(Directory.Exists(folderPath) && Start(folderPath, null));

    public Task<bool> RevealFileAsync(string filePath)
    {
        if (OperatingSystem.IsWindows() && File.Exists(filePath))
            return Task.FromResult(Start("explorer.exe", $"/select,\"{filePath}\""));
        var dir = Path.GetDirectoryName(filePath);
        return dir is null ? Task.FromResult(false) : OpenFolderAsync(dir);
    }

    private static bool Start(string target, string? arguments)
    {
        try
        {
            ProcessStartInfo info;
            if (arguments is not null) info = new ProcessStartInfo(target, arguments) { UseShellExecute = true };
            else if (OperatingSystem.IsWindows()) info = new ProcessStartInfo(target) { UseShellExecute = true };
            else
            {
                info = new ProcessStartInfo(OperatingSystem.IsMacOS() ? "open" : "xdg-open") { UseShellExecute = false };
                info.ArgumentList.Add(target);
            }
            using var process = Process.Start(info);
            return process is not null;
        }
        catch (Exception)
        {
            return false; // sem explorador disponível: a UI mostra o caminho para o usuário abrir à mão
        }
    }
}
