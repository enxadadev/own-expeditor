using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using OwnExpeditor.Core.Text;

namespace OwnExpeditor.App.Services;

/// <summary>Janela principal do app, de onde saem o seletor de pasta e a área de transferência.</summary>
internal static class MainWindowAccess
{
    public static TopLevel? Current =>
        (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
}

public sealed class AvaloniaFolderPicker(ILocalizer loc) : IFolderPicker
{
    public async Task<string?> PickFolderAsync(TextRef title, string? startPath)
    {
        if (MainWindowAccess.Current is not { } top) return null;
        var options = new FolderPickerOpenOptions { Title = loc.Format(title), AllowMultiple = false };
        if (!string.IsNullOrEmpty(startPath))
            options.SuggestedStartLocation = await top.StorageProvider.TryGetFolderFromPathAsync(startPath);
        var folders = await top.StorageProvider.OpenFolderPickerAsync(options);
        return folders.FirstOrDefault()?.TryGetLocalPath();
    }
}

public sealed class AvaloniaClipboardService : IClipboardService
{
    public async Task SetTextAsync(string text)
    {
        if (MainWindowAccess.Current?.Clipboard is { } clipboard) await clipboard.SetTextAsync(text);
    }
}
