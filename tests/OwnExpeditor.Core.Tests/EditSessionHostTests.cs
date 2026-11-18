using OwnExpeditor.Core.Session;
using OwnExpeditor.Domain.Games;
using OwnExpeditor.Domain.Saves;
using Microsoft.Extensions.Time.Testing;

namespace OwnExpeditor.Core.Tests;

public class EditSessionHostTests
{
    private static SaveSlotInfo Slot => SampleSave.Slot();

    private static (EditSessionHost Host, FakeSaveFileStore Store, FakeGameDataService Games) Create(TimeProvider? time = null)
    {
        var store = new FakeSaveFileStore();
        store.Add(Slot.GameSiiPath, SampleSave.Text);
        var games = new FakeGameDataService();
        return (new EditSessionHost(store, games, new FakeProcessMonitor(), time ?? new FakeTimeProvider()), store, games);
    }

    private static string BigSave()
    {
        var sb = new System.Text.StringBuilder("SiiNunit\r\n{\r\n");
        for (var i = 0; i < 300; i++)
            sb.Append($"economy : _nameless.{i} {{\r\n a: 1\r\n b: 2\r\n}}\r\n\r\n");
        return sb.Append("}\r\n").ToString();
    }

    private static async Task<CoreException> OpenFails(EditSessionHost host, SaveSlotInfo slot, CancellationToken ct = default) =>
        await Assert.ThrowsAsync<CoreException>(() => host.OpenAsync(slot, null, ct));

    [Fact]
    public async Task OpenAsync_Success_SetsCurrentAndRaisesSessionChanged()
    {
        var (host, _, _) = Create();
        SessionChangedEventArgs? args = null;
        host.SessionChanged += (_, e) => args = e;

        var session = await host.OpenAsync(Slot, null, default);

        Assert.Same(session, host.Current);
        Assert.NotNull(args);
        Assert.Null(args.Previous);
        Assert.Same(session, args.Current);
    }

    [Fact]
    public async Task OpenAsync_Success_SessionIsEditableAndUsesGameDataOfTheSaveGame()
    {
        var (_, _, games) = Create();
        var ats = SampleSave.Slot(GameKind.Ats, "/profiles/p1/save/9/game.sii", "9");
        var store = new FakeSaveFileStore();
        store.Add(ats.GameSiiPath, SampleSave.Text);
        var atsHost = new EditSessionHost(store, games, new FakeProcessMonitor());

        var session = await atsHost.OpenAsync(ats, null, default);

        Assert.False(session.IsReadOnly);
        Assert.Equal(GameKind.Ats, session.Game);
        Assert.Same(games.Data[GameKind.Ats], session.GameData);
        Assert.Equal(ats, session.Save);
    }

    [Fact]
    public async Task OpenAsync_Success_ParsesSaveContent()
    {
        var (host, _, _) = Create();

        var session = await host.OpenAsync(Slot, null, default);

        Assert.Equal("\"demo_co.city_b\"", session.Original.Find("_nameless.a")!.Get("target"));
        Assert.Equal(2, session.Original.OfType("job_offer_data").Count);
    }

    [Fact]
    public async Task OpenAsync_Progress_RunsThroughReadParseIndexInOrder()
    {
        var (host, _, _) = Create();
        var progress = new CollectingProgress();

        await host.OpenAsync(Slot, progress, default);

        var stages = progress.Reports.Select(r => r.StageKey).Distinct().ToList();
        Assert.Equal([LoadStages.Read, LoadStages.Parse, LoadStages.Index], stages);
    }

    [Fact]
    public async Task OpenAsync_Progress_StartsAtZeroEndsAtHundredAndNeverGoesBack()
    {
        var (host, _, _) = Create(new SteppingTime());
        var progress = new CollectingProgress();

        await host.OpenAsync(Slot, progress, default);

        var percents = progress.Reports.Select(r => r.Percent).ToList();
        Assert.Equal(0, percents[0]);
        Assert.Equal(100, percents[^1]);
        Assert.Equal(percents.Order().ToList(), percents);
    }

    [Fact]
    public async Task OpenAsync_Progress_StagesStayInsideTheirRanges()
    {
        var (host, store, _) = Create(new SteppingTime());
        store.Add(Slot.GameSiiPath, BigSave());
        var progress = new CollectingProgress();

        await host.OpenAsync(Slot, progress, default);

        Assert.All(progress.Reports.Where(r => r.StageKey == LoadStages.Read), r => Assert.InRange(r.Percent, 0, 30));
        Assert.All(progress.Reports.Where(r => r.StageKey == LoadStages.Parse), r => Assert.InRange(r.Percent, 30, 85));
        Assert.All(progress.Reports.Where(r => r.StageKey == LoadStages.Index), r => Assert.InRange(r.Percent, 85, 100));
        Assert.True(progress.Reports.Count(r => r.StageKey == LoadStages.Parse) > 5);
    }

    [Fact]
    public async Task OpenAsync_Progress_ReportsFileSizeInTheReadStage()
    {
        var (host, _, _) = Create();
        var progress = new CollectingProgress();

        await host.OpenAsync(Slot, progress, default);

        Assert.Equal(SampleSave.Bytes.Length, progress.Reports.First(r => r.StageKey == LoadStages.Read).FileSizeBytes);
    }

    [Fact]
    public async Task OpenAsync_Progress_IsThrottledWhenClockDoesNotAdvance()
    {
        var (host, store, _) = Create(new FakeTimeProvider());
        store.Add(Slot.GameSiiPath, BigSave());
        var progress = new CollectingProgress();

        await host.OpenAsync(Slot, progress, default);

        Assert.True(progress.Reports.Count(r => r.StageKey == LoadStages.Parse) <= 2);
    }

    [Fact]
    public async Task OpenAsync_FileMissing_ThrowsSaveNotFoundInReadStage()
    {
        var (host, store, _) = Create();
        store.Files.Clear();

        var e = await OpenFails(host, Slot);

        Assert.Equal(CoreError.SaveNotFound, e.Code);
        Assert.Equal(LoadStages.Read, e.StageKey);
        Assert.Null(host.Current);
    }

    [Fact]
    public async Task OpenAsync_DirectoryMissing_ThrowsSaveNotFound()
    {
        var (host, store, _) = Create();
        store.ReadThrows = new DirectoryNotFoundException();

        Assert.Equal(CoreError.SaveNotFound, (await OpenFails(host, Slot)).Code);
    }

    [Theory]
    [InlineData(typeof(UnauthorizedAccessException))]
    [InlineData(typeof(IOException))]
    public async Task OpenAsync_AccessDeniedOrLocked_ThrowsAccessDenied(Type exceptionType)
    {
        var (host, store, _) = Create();
        store.ReadThrows = (Exception)Activator.CreateInstance(exceptionType)!;

        var e = await OpenFails(host, Slot);

        Assert.Equal(CoreError.AccessDenied, e.Code);
        Assert.Equal(LoadStages.Read, e.StageKey);
    }

    [Fact]
    public async Task OpenAsync_TruncatedSave_ThrowsSaveCorruptInParseStageWithLine()
    {
        var (host, store, _) = Create();
        store.Add(Slot.GameSiiPath, "SiiNunit\r\n{\r\neconomy : _nameless.1 {\r\n a: 1\r\n");

        var e = await OpenFails(host, Slot);

        Assert.Equal(CoreError.SaveCorrupt, e.Code);
        Assert.Equal(LoadStages.Parse, e.StageKey);
        Assert.Equal(3, e.Line);
    }

    [Fact]
    public async Task OpenAsync_EmptySave_ThrowsSaveCorrupt()
    {
        var (host, store, _) = Create();
        store.Add(Slot.GameSiiPath, "");

        var e = await OpenFails(host, Slot);

        Assert.Equal(CoreError.SaveCorrupt, e.Code);
        Assert.Equal(LoadStages.Parse, e.StageKey);
    }

    [Fact]
    public async Task OpenAsync_SyntaxError_ThrowsSaveParseFailedWithLine()
    {
        var (host, store, _) = Create();
        store.Add(Slot.GameSiiPath, "SiiNunit\r\n{\r\neconomy : _nameless.1 {\r\n???\r\n}\r\n}\r\n");

        var e = await OpenFails(host, Slot);

        Assert.Equal(CoreError.SaveParseFailed, e.Code);
        Assert.Equal(LoadStages.Parse, e.StageKey);
        Assert.Equal(4, e.Line);
    }

    [Fact]
    public async Task OpenAsync_Failure_KeepsPreviousSessionAndRaisesNothing()
    {
        var (host, store, _) = Create();
        var first = await host.OpenAsync(Slot, null, default);
        var raised = 0;
        host.SessionChanged += (_, _) => raised++;
        store.Files.Clear();

        await OpenFails(host, Slot);

        Assert.Same(first, host.Current);
        Assert.Equal(0, raised);
    }

    [Fact]
    public async Task OpenAsync_AlreadyCancelled_ThrowsOperationCanceledAndOpensNothing()
    {
        var (host, store, _) = Create();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => host.OpenAsync(Slot, null, cts.Token));

        Assert.Null(host.Current);
        Assert.Empty(store.Log.Where(l => l == "read"));
    }

    [Fact]
    public async Task OpenAsync_CancelledDuringParse_ThrowsOperationCanceledAndKeepsCurrent()
    {
        var (host, store, _) = Create(new SteppingTime());
        store.Add(Slot.GameSiiPath, BigSave());
        using var cts = new CancellationTokenSource();
        var progress = new CancelOnParse(cts);
        var raised = 0;
        host.SessionChanged += (_, _) => raised++;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => host.OpenAsync(Slot, progress, cts.Token));

        Assert.Null(host.Current);
        Assert.Equal(0, raised);
    }

    private sealed class CancelOnParse(CancellationTokenSource cts) : IProgress<LoadProgress>
    {
        private int _parse;

        public void Report(LoadProgress value)
        {
            if (value.StageKey == LoadStages.Parse && ++_parse == 2) cts.Cancel();
        }
    }

    [Fact]
    public async Task OpenAsync_WhileAnotherOpenRuns_ThrowsSessionBusy()
    {
        var (host, store, _) = Create();
        store.Gate = null;
        var gate = new SlowReadStore(store);
        var slowHost = new EditSessionHost(gate, new FakeGameDataService(), new FakeProcessMonitor());
        var first = slowHost.OpenAsync(Slot, null, default);
        await gate.Started.Task;

        var e = await Assert.ThrowsAsync<CoreException>(() => slowHost.OpenAsync(Slot, null, default));

        Assert.Equal(CoreError.SessionBusy, e.Code);
        gate.Release.SetResult();
        await first;
    }

    private sealed class SlowReadStore(FakeSaveFileStore inner) : OwnExpeditor.Data.Saves.ISaveFileStore
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task<OwnExpeditor.Data.Saves.SaveFile> ReadAsync(string path, CancellationToken ct)
        {
            Started.TrySetResult();
            await Release.Task;
            return await inner.ReadAsync(path, ct);
        }

        public Task<OwnExpeditor.Data.Saves.SaveWriteReceipt> WriteAtomicAsync(OwnExpeditor.Data.Saves.SaveWriteRequest request, CancellationToken ct) =>
            inner.WriteAtomicAsync(request, ct);

        public Task<OwnExpeditor.Data.Saves.NewSlotReceipt> WriteNewSlotAsync(OwnExpeditor.Data.Saves.NewSlotWriteRequest request, CancellationToken ct) =>
            inner.WriteNewSlotAsync(request, ct);
    }

    [Fact]
    public async Task OpenAsync_SecondSession_ReplacesFirstAndInvalidatesIt()
    {
        var (host, _, _) = Create();
        var first = await host.OpenAsync(Slot, null, default);
        SessionChangedEventArgs? args = null;
        host.SessionChanged += (_, e) => args = e;

        var second = await host.OpenAsync(Slot, null, default);

        Assert.Same(second, host.Current);
        Assert.Same(first, args!.Previous);
        Assert.Same(second, args.Current);
        var ex = Assert.Throws<CoreException>(() => first.Stage(SampleSave.FillTarget()));
        Assert.Equal(CoreError.NoSession, ex.Code);
    }

    [Fact]
    public async Task Close_WithSession_ClearsCurrentAndRaisesSessionChanged()
    {
        var (host, _, _) = Create();
        var session = await host.OpenAsync(Slot, null, default);
        SessionChangedEventArgs? args = null;
        host.SessionChanged += (_, e) => args = e;

        host.Close();

        Assert.Null(host.Current);
        Assert.Same(session, args!.Previous);
        Assert.Null(args.Current);
    }

    [Fact]
    public void Close_WithoutSession_DoesNothing()
    {
        var (host, _, _) = Create();
        var raised = 0;
        host.SessionChanged += (_, _) => raised++;

        host.Close();

        Assert.Equal(0, raised);
    }

    [Fact]
    public async Task GameDataChanged_ForTheSessionGame_RefreshesGameDataAndRaisesSessionChanged()
    {
        var (host, _, games) = Create();
        var session = await host.OpenAsync(Slot, null, default);
        var changed = new TaskCompletionSource();
        session.Changed += (_, _) => changed.TrySetResult();
        var fresh = new FakeGameData(GameKind.Ets2);
        games.Data[GameKind.Ets2] = fresh;

        games.RaiseChanged(GameKind.Ets2);
        await changed.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Same(fresh, session.GameData);
    }

    [Fact]
    public async Task GameDataChanged_ForTheOtherGame_LeavesGameDataAlone()
    {
        var (host, _, games) = Create();
        var session = await host.OpenAsync(Slot, null, default);
        var before = session.GameData;
        games.Data[GameKind.Ats] = new FakeGameData(GameKind.Ats);

        games.RaiseChanged(GameKind.Ats);
        await Task.Delay(50);

        Assert.Same(before, session.GameData);
    }

    [Fact]
    public async Task GameDataChanged_KeepsPendingChanges()
    {
        var (host, _, games) = Create();
        var session = await host.OpenAsync(Slot, null, default);
        session.Stage(SampleSave.FillTarget());
        var changed = new TaskCompletionSource();
        session.Changed += (_, _) => changed.TrySetResult();

        games.RaiseChanged(GameKind.Ets2);
        await changed.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Single(session.Pending);
    }
}
