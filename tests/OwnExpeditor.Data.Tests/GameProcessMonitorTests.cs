using OwnExpeditor.Data.Games;
using OwnExpeditor.Domain.Games;

namespace OwnExpeditor.Data.Tests;

public class GameProcessMonitorTests
{
    [Theory]
    [InlineData("eurotrucks2")]
    [InlineData("eurotrucks2.exe")]
    [InlineData("EuroTrucks2")]
    public void IsRunning_Ets2ProcessPresent_ReturnsTrueOnlyForEts2(string name)
    {
        var monitor = new GameProcessMonitor(() => [name, "bash"]);

        Assert.True(monitor.IsRunning(GameKind.Ets2));
        Assert.False(monitor.IsRunning(GameKind.Ats));
    }

    [Theory]
    [InlineData("amtrucks")]
    [InlineData("amtrucks.exe")]
    public void IsRunning_AtsProcessPresent_ReturnsTrueOnlyForAts(string name)
    {
        var monitor = new GameProcessMonitor(() => [name]);

        Assert.True(monitor.IsRunning(GameKind.Ats));
        Assert.False(monitor.IsRunning(GameKind.Ets2));
    }

    [Fact]
    public void IsRunning_NoGameProcess_ReturnsFalse()
    {
        var monitor = new GameProcessMonitor(() => ["steam", "eurotrucks2launcher"]);

        Assert.False(monitor.IsRunning(GameKind.Ets2));
    }

    [Fact]
    public void IsRunning_ProcessListFails_ReturnsFalseInsteadOfThrowing()
    {
        var monitor = new GameProcessMonitor(() => throw new InvalidOperationException("sem acesso"));

        Assert.False(monitor.IsRunning(GameKind.Ets2));
    }

    [Fact]
    public void IsRunning_SystemProcessList_DoesNotThrow()
    {
        var monitor = new GameProcessMonitor();

        _ = monitor.IsRunning(GameKind.Ets2);
        _ = monitor.IsRunning(GameKind.Ats);
    }
}
