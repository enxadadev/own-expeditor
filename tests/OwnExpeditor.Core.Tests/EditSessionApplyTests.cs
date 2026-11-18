using System.Text;
using OwnExpeditor.Core.Session;
using OwnExpeditor.Data.Saves;
using OwnExpeditor.Domain.Games;
using OwnExpeditor.Domain.Saves;
using OwnExpeditor.Sii;

namespace OwnExpeditor.Core.Tests;

public class EditSessionApplyTests
{
    private sealed class Env
    {
        public FakeSaveFileStore Store { get; } = new();
        public FakeProcessMonitor Process { get; } = new();
        public EditSessionHost Host { get; }
        public SaveSlotInfo Slot { get; } = SampleSave.Slot();
        public IEditSession Session { get; private set; } = null!;

        public Env()
        {
            Store.Add(Slot.GameSiiPath, SampleSave.Text);
            Host = new EditSessionHost(Store, new FakeGameDataService(), Process);
        }

        public async Task<Env> OpenAsync()
        {
            Session = await Host.OpenAsync(Slot, null, default);
            Store.Log.Clear();
            return this;
        }

        public Task<ApplyResult> Overwrite() => Session.ApplyAsync(new ApplyTarget.Overwrite(), default);
    }

    private static async Task<Env> Ready(bool stage = true)
    {
        var env = await new Env().OpenAsync();
        if (stage) env.Session.Stage(SampleSave.FillTarget());
        return env;
    }

    private static async Task<CoreError> Fails(Func<Task> act) => (await Assert.ThrowsAsync<CoreException>(act)).Code;

    [Fact]
    public async Task Overwrite_WritesOnlyThroughStoreWithOriginalFingerprint()
    {
        var env = await Ready();

        var result = await env.Overwrite();

        var request = Assert.Single(env.Store.AtomicRequests);
        Assert.Empty(env.Store.NewSlotRequests);
        Assert.Equal(env.Slot.GameSiiPath, request.Path);
        Assert.Equal(FakeSaveFileStore.FingerprintOf(SampleSave.Bytes).Sha256, request.Expected.Sha256);
        Assert.Contains("target: \"demo_co.city_c\"", request.NewText);
        Assert.Equal(env.Store.BackupPath, result.BackupPath);
        Assert.Equal(env.Slot.GameSiiPath, result.WrittenSlot.GameSiiPath);
        Assert.Equal(1, result.ChangeSetCount);
        Assert.Equal(1, result.PatchCount);
    }

    [Fact]
    public async Task Overwrite_OnlyChangesTheEditedLine()
    {
        var env = await Ready();

        await env.Overwrite();

        var expected = SampleSave.Text.Replace(" target: \"\"\r\n cargo: null", " target: \"demo_co.city_c\"\r\n cargo: null");
        Assert.Equal(expected, env.Store.AtomicRequests[0].NewText);
    }

    [Fact]
    public async Task Overwrite_Success_MakesWrittenStateTheNewBase()
    {
        var env = await Ready();

        await env.Overwrite();

        Assert.False(env.Session.HasChanges);
        Assert.Equal("\"demo_co.city_c\"", env.Session.Original.Find("_nameless.b")!.Get("target"));
        Assert.Equal("\"demo_co.city_c\"", env.Session.View.Find("_nameless.b")!.Get("target"));
    }

    [Fact]
    public async Task Overwrite_Success_RaisesChanged()
    {
        var env = await Ready();
        var raised = 0;
        env.Session.Changed += (_, _) => raised++;

        await env.Overwrite();

        Assert.True(raised >= 1);
    }

    [Fact]
    public async Task Overwrite_Twice_SecondUsesFingerprintOfFirstWrite()
    {
        var env = await Ready();
        await env.Overwrite();
        env.Session.Stage(SampleSave.Change("k2", "_nameless.a", "urgency", "0", "3"));

        await env.Overwrite();

        Assert.Equal(2, env.Store.AtomicRequests.Count);
        Assert.Equal(FakeSaveFileStore.FingerprintOf(Encoding.UTF8.GetBytes(env.Store.AtomicRequests[0].NewText)).Sha256,
            env.Store.AtomicRequests[1].Expected.Sha256);
    }

    [Fact]
    public async Task Verify_ExpectedText_ReturnsTrue()
    {
        var env = await Ready();
        await env.Overwrite();

        Assert.True(env.Store.AtomicRequests[0].VerifyWritten(env.Store.AtomicRequests[0].NewText));
    }

    [Fact]
    public async Task Verify_NewValueMissing_ReturnsFalse()
    {
        var env = await Ready();
        await env.Overwrite();

        Assert.False(env.Store.AtomicRequests[0].VerifyWritten(SampleSave.Text));
    }

    [Fact]
    public async Task Verify_BlockCountDiffers_ReturnsFalse()
    {
        var env = await Ready();
        await env.Overwrite();
        var text = env.Store.AtomicRequests[0].NewText;
        var withoutLastBlock = text[..text.IndexOf("job_offer_data : _nameless.b", StringComparison.Ordinal)] + "}\r\n";

        Assert.False(env.Store.AtomicRequests[0].VerifyWritten(withoutLastBlock));
    }

    [Theory]
    [InlineData("")]
    [InlineData("garbage")]
    [InlineData("SiiNunit\r\n{\r\neconomy : _nameless.1 {\r\n")]
    public async Task Verify_UnparsableText_ReturnsFalseWithoutThrowing(string text)
    {
        var env = await Ready();
        await env.Overwrite();

        Assert.False(env.Store.AtomicRequests[0].VerifyWritten(text));
    }

    [Fact]
    public async Task NewSlot_WritesToNewSlotThroughStoreAndSessionFollowsIt()
    {
        var env = await Ready();

        var result = await env.Session.ApplyAsync(new ApplyTarget.NewSlot("Meu save"), default);

        var request = Assert.Single(env.Store.NewSlotRequests);
        Assert.Empty(env.Store.AtomicRequests);
        Assert.Equal("Meu save", request.DisplayName);
        Assert.Equal(env.Slot, request.Source);
        Assert.Equal("/profiles/p1/save/2/game.sii", result.WrittenSlot.GameSiiPath);
        Assert.Null(result.BackupPath);
        Assert.Equal(result.WrittenSlot, env.Session.Save);
        Assert.False(env.Session.HasChanges);
        Assert.Equal("\"demo_co.city_c\"", env.Session.Original.Find("_nameless.b")!.Get("target"));
    }

    [Fact]
    public async Task NewSlot_ThenOverwrite_TargetsTheNewSlotFile()
    {
        var env = await Ready();
        await env.Session.ApplyAsync(new ApplyTarget.NewSlot("Meu save"), default);
        env.Session.Stage(SampleSave.Change("k2", "_nameless.a", "urgency", "0", "3"));

        await env.Overwrite();

        var request = Assert.Single(env.Store.AtomicRequests);
        Assert.Equal("/profiles/p1/save/2/game.sii", request.Path);
        Assert.Equal(FakeSaveFileStore.FingerprintOf(Encoding.UTF8.GetBytes(env.Store.NewSlotRequests[0].NewText)).Sha256,
            request.Expected.Sha256);
    }

    [Fact]
    public async Task NewSlot_LeavesOriginalFileUntouched()
    {
        var env = await Ready();

        await env.Session.ApplyAsync(new ApplyTarget.NewSlot("Meu save"), default);

        Assert.Equal(SampleSave.Bytes, env.Store.Files[env.Slot.GameSiiPath]);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task NewSlot_BlankName_ThrowsInvalidEditWithoutTouchingStore(string name)
    {
        var env = await Ready();

        var code = await Fails(() => env.Session.ApplyAsync(new ApplyTarget.NewSlot(name), default));

        Assert.Equal(CoreError.InvalidEdit, code);
        Assert.Empty(env.Store.Log);
    }

    [Fact]
    public async Task Apply_NothingPending_ThrowsNothingToApply()
    {
        var env = await Ready(stage: false);

        Assert.Equal(CoreError.NothingToApply, await Fails(env.Overwrite));
        Assert.Empty(env.Store.Log);
    }

    [Fact]
    public async Task Apply_GameRunning_RefusesBeforeAnyIo()
    {
        var env = await Ready();
        env.Process.Set(GameKind.Ets2, true);

        Assert.Equal(CoreError.GameRunning, await Fails(env.Overwrite));

        Assert.Empty(env.Store.Log);
        Assert.True(env.Session.HasChanges);
    }

    [Fact]
    public async Task Apply_GameRunning_ChecksTheSessionGameAtApplyTime()
    {
        var env = await Ready();
        env.Process.Set(GameKind.Ats, true);
        await env.Overwrite();
        env.Session.Stage(SampleSave.Change("k2", "_nameless.a", "urgency", "0", "3"));
        env.Process.Set(GameKind.Ets2, true);

        Assert.Equal(CoreError.GameRunning, await Fails(env.Overwrite));
    }

    [Fact]
    public async Task Apply_GameRunning_AlsoBlocksNewSlot()
    {
        var env = await Ready();
        env.Process.Set(GameKind.Ets2, true);

        var code = await Fails(() => env.Session.ApplyAsync(new ApplyTarget.NewSlot("x"), default));

        Assert.Equal(CoreError.GameRunning, code);
        Assert.Empty(env.Store.Log);
    }

    [Fact]
    public async Task Apply_FileChangedSinceOpen_ThrowsSaveChangedOnDiskAndDoesNotWrite()
    {
        var env = await Ready();
        env.Store.Add(env.Slot.GameSiiPath, SampleSave.Text.Replace("1000", "2000"));

        Assert.Equal(CoreError.SaveChangedOnDisk, await Fails(env.Overwrite));

        Assert.Empty(env.Store.AtomicRequests);
        Assert.True(env.Session.HasChanges);
    }

    [Fact]
    public async Task Apply_FileMissing_ThrowsSaveNotFound()
    {
        var env = await Ready();
        env.Store.Files.Clear();

        Assert.Equal(CoreError.SaveNotFound, await Fails(env.Overwrite));
    }

    [Fact]
    public async Task Apply_ReadDenied_ThrowsAccessDenied()
    {
        var env = await Ready();
        env.Store.ReadThrows = new UnauthorizedAccessException();

        Assert.Equal(CoreError.AccessDenied, await Fails(env.Overwrite));
    }

    [Theory]
    [InlineData(SaveWriteFailure.NotFound, CoreError.SaveNotFound)]
    [InlineData(SaveWriteFailure.ChangedOnDisk, CoreError.SaveChangedOnDisk)]
    [InlineData(SaveWriteFailure.BackupExists, CoreError.BackupExists)]
    [InlineData(SaveWriteFailure.BackupFailed, CoreError.BackupFailed)]
    [InlineData(SaveWriteFailure.WriteFailed, CoreError.WriteFailed)]
    [InlineData(SaveWriteFailure.VerifyFailed, CoreError.VerifyFailed)]
    [InlineData(SaveWriteFailure.AccessDenied, CoreError.AccessDenied)]
    [InlineData(SaveWriteFailure.SlotUnavailable, CoreError.SlotUnavailable)]
    public async Task Overwrite_StoreFailure_MapsToCoreErrorAndKeepsPending(SaveWriteFailure failure, CoreError expected)
    {
        var env = await Ready();
        env.Store.FailWith = failure;

        var e = await Assert.ThrowsAsync<CoreException>(env.Overwrite);

        Assert.Equal(expected, e.Code);
        Assert.IsType<SaveWriteException>(e.InnerException);
        Assert.True(env.Session.HasChanges);
        Assert.Equal("\"\"", env.Session.Original.Find("_nameless.b")!.Get("target"));
    }

    [Theory]
    [InlineData(SaveWriteFailure.SlotUnavailable, CoreError.SlotUnavailable)]
    [InlineData(SaveWriteFailure.WriteFailed, CoreError.WriteFailed)]
    [InlineData(SaveWriteFailure.ChangedOnDisk, CoreError.SaveChangedOnDisk)]
    public async Task NewSlot_StoreFailure_MapsToCoreErrorAndKeepsSession(SaveWriteFailure failure, CoreError expected)
    {
        var env = await Ready();
        env.Store.FailWith = failure;

        var code = await Fails(() => env.Session.ApplyAsync(new ApplyTarget.NewSlot("x"), default));

        Assert.Equal(expected, code);
        Assert.Equal(env.Slot, env.Session.Save);
        Assert.True(env.Session.HasChanges);
    }

    [Fact]
    public async Task Apply_AfterFailure_CanRetry()
    {
        var env = await Ready();
        env.Store.FailWith = SaveWriteFailure.WriteFailed;
        await Fails(env.Overwrite);
        env.Store.FailWith = null;

        var result = await env.Overwrite();

        Assert.Equal(1, result.PatchCount);
    }

    [Fact]
    public async Task Apply_WhileAnotherApplyRuns_ThrowsSessionBusy()
    {
        var env = await Ready();
        env.Store.Gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var first = env.Overwrite();
        while (env.Store.AtomicRequests.Count == 0) await Task.Yield();

        Assert.Equal(CoreError.SessionBusy, await Fails(env.Overwrite));

        env.Store.Gate.SetResult();
        await first;
    }

    [Fact]
    public async Task Stage_WhileApplying_ThrowsSessionBusy()
    {
        var env = await Ready();
        env.Store.Gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var first = env.Overwrite();
        while (env.Store.AtomicRequests.Count == 0) await Task.Yield();

        var e = Assert.Throws<CoreException>(() => env.Session.Stage(SampleSave.Change("k2", "_nameless.a", "urgency", "0", "3")));

        Assert.Equal(CoreError.SessionBusy, e.Code);
        env.Store.Gate.SetResult();
        await first;
    }

    [Fact]
    public async Task Apply_Cancelled_ThrowsOperationCanceledAndKeepsPending()
    {
        var env = await Ready();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => env.Session.ApplyAsync(new ApplyTarget.Overwrite(), cts.Token));

        Assert.True(env.Session.HasChanges);
        Assert.Empty(env.Store.AtomicRequests);
    }

    [Fact]
    public async Task Apply_AfterHostClose_ThrowsNoSession()
    {
        var env = await Ready();
        env.Host.Close();

        Assert.Equal(CoreError.NoSession, await Fails(env.Overwrite));
        Assert.Empty(env.Store.Log);
    }

    [Fact]
    public async Task Stage_AfterHostClose_ThrowsNoSession()
    {
        var env = await Ready(stage: false);
        env.Host.Close();

        var e = Assert.Throws<CoreException>(() => env.Session.Stage(SampleSave.FillTarget()));

        Assert.Equal(CoreError.NoSession, e.Code);
    }

    [Fact]
    public async Task Apply_NullTarget_ThrowsArgumentNull()
    {
        var env = await Ready();

        await Assert.ThrowsAsync<ArgumentNullException>(() => env.Session.ApplyAsync(null!, default));
    }

    [Fact]
    public async Task Open_SaveWithSyntaxError_ThrowsSaveParseFailed()
    {
        var env = new Env();
        var bad = SampleSave.Text.Replace(" game_time: 1000\r\n", " game_time: 1000\r\n???\r\n");
        env.Store.Add(env.Slot.GameSiiPath, bad);
        var opened = await Assert.ThrowsAsync<CoreException>(() => env.Host.OpenAsync(env.Slot, null, default));

        Assert.Equal(CoreError.SaveParseFailed, opened.Code);
        Assert.IsType<SiiFormatException>(opened.InnerException);
    }
}
