using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using OwnExpeditor.App.Modules.Jobs;
using OwnExpeditor.App.Services;
using OwnExpeditor.App.Shell;
using OwnExpeditor.App.Tests.Support;
using OwnExpeditor.App.ViewModels;
using OwnExpeditor.Core.Modules.Jobs;
using OwnExpeditor.Core.Session;
using OwnExpeditor.Core.Settings;
using OwnExpeditor.Domain.Games;
using OwnExpeditor.Domain.Saves;
using OwnExpeditor.Domain.Settings;

namespace OwnExpeditor.App.Tests;

/// <summary>Save real em samples/ (fora do git): abre pelo host real e monta um frete pelo módulo, com o JobBoard real. Sem o save, não faz nada.</summary>
public class JobsRealSaveTests(Xunit.Abstractions.ITestOutputHelper output)
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

    [Fact]
    public async Task Jobs_RealSave_BuildsTheBoardQuickly_AndPlansOneJobThroughTheRealPlanner()
    {
        var file = SampleSave("4");
        if (file is null) return;
        var dir = Path.GetDirectoryName(file)!;
        var profile = new ProfileInfo(GameKind.Ets2, "4A2E204F776E657469", "Demo", Path.GetDirectoryName(Path.GetDirectoryName(dir))!, false);
        var slot = new SaveSlotInfo(profile, "4", "4", file, File.GetLastWriteTimeUtc(file), new FileInfo(file).Length);
        using var provider = (ServiceProvider)AppComposition.Build(s =>
        {
            s.AddSingleton<ILocalizer>(new Localizer(Language.En));
            s.AddSingleton<IUiDispatcher, ImmediateUiDispatcher>();
            s.AddSingleton<IAppSettingsService>(new FakeSettings());
            s.AddSingleton<IJobBoardFactory>(new FakeBoardFactory()); // nada de cache de distâncias real; o board é o real
        });
        var shell = provider.GetRequiredService<ShellViewModel>();
        await shell.OpenSaveAsync(slot);
        var page = Assert.IsType<JobsPageViewModel>(shell.CurrentPage);
        Assert.NotEqual(PageState.Loading, page.State);
        if (page.State != PageState.Ready) return; // save sem vagas nem ofertas: nada a planejar

        var timer = Stopwatch.StartNew();
        var board = JobBoard.Build(shell.Session!.View, shell.Session.GameData);
        output.WriteLine($"JobBoard: {timer.ElapsedMilliseconds} ms, {board.Cities().Count} cidades, {board.OriginCities().Count} de saída");
        Assert.True(timer.Elapsed < TimeSpan.FromSeconds(5), $"JobBoard demorou {timer.Elapsed}");

        // acha um par de cidades com combinação e monta o frete pelos passos da tela
        var from = board.OriginCities().First(c => board.DestinationCities(c).Count > 0);
        var to = board.DestinationCities(from).First();
        page.SelectFromCity(from);
        page.ToCity.Selected = page.ToCity.AllOptions.First(o => Equals(o.Key, to));
        Assert.True(page.ShowSteps, "o par escolhido tem combinação");
        if (page.FromCompany.Selected is null) page.FromCompany.Selected = page.FromCompany.AllOptions.First();
        if (page.SelectedMode?.Mode == JobSlotMode.ReplaceOffer && page.Offer.Selected is null) page.Offer.Selected = page.Offer.AllOptions.First();
        if (page.Cargo.Selected is null) page.Cargo.Selected = page.Cargo.AllOptions.First();
        if (page.ToCompany.Selected is null) page.ToCompany.Selected = page.ToCompany.AllOptions.First();

        Assert.True(page.CanAdd, page.MissingText);
        page.AddCommand.Execute(null);

        Assert.False(page.HasError, page.ErrorMessage);
        var change = Assert.Single(shell.Session.Pending);
        Assert.Equal("jobs", change.ModuleId);
        Assert.StartsWith("jobs:slot:", change.Key);
        Assert.NotEmpty(change.Patches);
        Assert.Equal(1, shell.PendingCount);
    }
}
