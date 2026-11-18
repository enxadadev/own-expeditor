using Microsoft.Extensions.DependencyInjection;
using OwnExpeditor.App.Services;
using OwnExpeditor.App.Shell;
using OwnExpeditor.App.Tests.Support;
using OwnExpeditor.App.ViewModels;
using OwnExpeditor.Core.Session;
using OwnExpeditor.Domain.Games;
using OwnExpeditor.Domain.Saves;
using OwnExpeditor.Core.Settings;
using OwnExpeditor.Domain.Settings;

namespace OwnExpeditor.App.Tests;

/// <summary>Saves reais em samples/ (fora do git). Sem eles, os testes não fazem nada.</summary>
public class RealSaveTests
{
    private static string? SampleSave(string slot)
    {
        for (var d = new DirectoryInfo(AppContext.BaseDirectory); d is not null; d = d.Parent)
        {
            var file = Path.Combine(d.FullName, "samples", "4A2E204F776E657469", "save", slot, "game.sii");
            if (File.Exists(file)) return file;
        }
        return null;
    }

    private static SaveSlotInfo Slot(string file, string id)
    {
        var dir = Path.GetDirectoryName(file)!;
        var profileDir = Path.GetDirectoryName(Path.GetDirectoryName(dir))!;
        var profile = new ProfileInfo(GameKind.Ets2, "4A2E204F776E657469", "Demo", profileDir, false);
        return new SaveSlotInfo(profile, id, id, file, File.GetLastWriteTimeUtc(file), new FileInfo(file).Length);
    }

    [Theory]
    [InlineData("3")] // binário (BSII)
    [InlineData("4")] // texto
    public async Task Shell_OpensRealSave_ThroughTheRealHost_WithoutHanging(string slotId)
    {
        var file = SampleSave(slotId);
        if (file is null) return;
        var loc = new Localizer(Language.PtBr);
        var settings = new FakeSettings();
        using var provider = (ServiceProvider)AppComposition.Build(s =>
        {
            s.AddSingleton<ILocalizer>(loc);
            s.AddSingleton<IUiDispatcher, ImmediateUiDispatcher>();
            s.AddSingleton<IAppSettingsService>(settings); // nada grava o settings.json real
        });
        var shell = provider.GetRequiredService<ShellViewModel>();

        var opening = shell.OpenSaveAsync(Slot(file, slotId));
        var finished = await Task.WhenAny(opening, Task.Delay(TimeSpan.FromSeconds(90)));

        Assert.Same(opening, finished);
        await opening;
        Assert.Equal(ShellPage.Module, shell.Page);
        Assert.True(shell.HasSession);
        Assert.NotEmpty(shell.Session!.View.OfType("economy"));
        Assert.Equal(1.0, shell.Loading.Progress);
        Assert.Equal(0, shell.PendingCount);
    }
}
