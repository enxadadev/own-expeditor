using Microsoft.Extensions.Time.Testing;
using OwnExpeditor.Core.Games;
using OwnExpeditor.Core.Session;
using OwnExpeditor.Domain.Games;

namespace OwnExpeditor.Core.Tests;

public class GameStatusMonitorTests
{
    private static readonly TimeSpan Poll = TimeSpan.FromSeconds(2);

    private sealed class Env
    {
        public FakeTimeProvider Time { get; } = new();
        public FakeSessionHost Host { get; } = new();
        public FakeProcessMonitor Process { get; } = new();
        public GameStatusMonitor Monitor { get; }
        public List<bool> Events { get; } = [];

        public Env()
        {
            Monitor = new GameStatusMonitor(Host, Process, Time);
            Monitor.RunningChanged += (_, running) => Events.Add(running);
        }

        public void Open(GameKind game = GameKind.Ets2) =>
            Host.Set(EditSessionFactory.FromText(SampleSave.Text, SampleSave.Slot(game)));
    }

    [Fact]
    public void NoSession_NeverPollsAndIsNotRunning()
    {
        var env = new Env();
        env.Process.Set(GameKind.Ets2, true);

        env.Time.Advance(TimeSpan.FromSeconds(20));

        Assert.Empty(env.Process.Calls);
        Assert.False(env.Monitor.IsRunning);
        Assert.Empty(env.Events);
    }

    [Fact]
    public void SessionOpened_ChecksImmediately()
    {
        var env = new Env();
        env.Process.Set(GameKind.Ets2, true);

        env.Open();

        Assert.True(env.Monitor.IsRunning);
        Assert.Equal([true], env.Events);
    }

    [Fact]
    public void Polling_HappensEveryTwoSeconds()
    {
        var env = new Env();
        env.Open();
        var afterOpen = env.Process.Calls.Count;

        env.Time.Advance(Poll - TimeSpan.FromMilliseconds(1));
        Assert.Equal(afterOpen, env.Process.Calls.Count);

        env.Time.Advance(TimeSpan.FromMilliseconds(1));
        Assert.Equal(afterOpen + 1, env.Process.Calls.Count);

        env.Time.Advance(Poll);
        Assert.Equal(afterOpen + 2, env.Process.Calls.Count);
    }

    [Fact]
    public void GameStarts_RaisesRunningChangedTrueOnce()
    {
        var env = new Env();
        env.Open();
        env.Process.Set(GameKind.Ets2, true);

        env.Time.Advance(Poll);
        env.Time.Advance(Poll);

        Assert.True(env.Monitor.IsRunning);
        Assert.Equal([true], env.Events);
    }

    [Fact]
    public void GameStops_RaisesRunningChangedFalse()
    {
        var env = new Env();
        env.Process.Set(GameKind.Ets2, true);
        env.Open();
        env.Process.Set(GameKind.Ets2, false);

        env.Time.Advance(Poll);

        Assert.False(env.Monitor.IsRunning);
        Assert.Equal([true, false], env.Events);
    }

    [Fact]
    public void Polling_QueriesTheGameOfTheOpenSession()
    {
        var env = new Env();
        env.Open(GameKind.Ats);

        env.Time.Advance(Poll);

        Assert.All(env.Process.Calls, g => Assert.Equal(GameKind.Ats, g));
    }

    [Fact]
    public void OtherGameRunning_IsIgnored()
    {
        var env = new Env();
        env.Process.Set(GameKind.Ats, true);

        env.Open(GameKind.Ets2);
        env.Time.Advance(Poll);

        Assert.False(env.Monitor.IsRunning);
        Assert.Empty(env.Events);
    }

    [Fact]
    public void SessionClosed_StopsPollingAndClearsRunning()
    {
        var env = new Env();
        env.Process.Set(GameKind.Ets2, true);
        env.Open();
        env.Host.Close();
        var calls = env.Process.Calls.Count;

        env.Time.Advance(TimeSpan.FromSeconds(10));

        Assert.Equal(calls, env.Process.Calls.Count);
        Assert.False(env.Monitor.IsRunning);
        Assert.Equal([true, false], env.Events);
    }

    [Fact]
    public void SessionReplaced_ByOtherGame_RechecksTheNewGame()
    {
        var env = new Env();
        env.Process.Set(GameKind.Ets2, true);
        env.Open(GameKind.Ets2);

        env.Open(GameKind.Ats);

        Assert.False(env.Monitor.IsRunning);
        Assert.Equal([true, false], env.Events);
    }

    [Fact]
    public void Dispose_StopsPolling()
    {
        var env = new Env();
        env.Open();
        env.Monitor.Dispose();
        var calls = env.Process.Calls.Count;

        env.Time.Advance(TimeSpan.FromSeconds(10));

        Assert.Equal(calls, env.Process.Calls.Count);
    }

    [Fact]
    public void ProcessMonitorThrows_IsTreatedAsNotRunningAndPollingContinues()
    {
        var env = new Env();
        var throwing = new ThrowingMonitor();
        var monitor = new GameStatusMonitor(env.Host, throwing, env.Time);
        env.Host.Set(EditSessionFactory.FromText(SampleSave.Text, SampleSave.Slot()));

        env.Time.Advance(Poll);
        env.Time.Advance(Poll);

        Assert.False(monitor.IsRunning);
        Assert.True(throwing.Calls >= 2);
    }

    private sealed class ThrowingMonitor : OwnExpeditor.Data.Games.IGameProcessMonitor
    {
        public int Calls { get; private set; }

        public bool IsRunning(GameKind game)
        {
            Calls++;
            throw new InvalidOperationException("processo");
        }
    }
}
