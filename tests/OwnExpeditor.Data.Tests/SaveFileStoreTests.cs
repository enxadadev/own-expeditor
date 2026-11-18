using System.Text;
using OwnExpeditor.Data.Saves;
using OwnExpeditor.Domain.Games;
using OwnExpeditor.Domain.Saves;
using OwnExpeditor.Sii;

namespace OwnExpeditor.Data.Tests;

public class SaveFileStoreTests : IDisposable
{
    private readonly TempDir _t = new();
    private readonly SaveFileStore _store =
        new(new FixedTime(new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero)));

    public void Dispose() => _t.Dispose();

    private string GameSii(string content = Sii.Game) => _t.File(content, "save", "1", "game.sii");

    private async Task<SaveWriteRequest> Request(string path, string newText = Sii.GameEdited,
        Func<string, bool>? verify = null) =>
        new(path, (await _store.ReadAsync(path, default)).Fingerprint, newText, verify ?? (_ => true));

    private static string[] Leftovers(string dir) =>
        Directory.GetFiles(dir).Where(f => f.EndsWith(".tmp")).ToArray();

    // ---- leitura

    [Fact]
    public async Task ReadAsync_ExistingFile_ReturnsBytesAndFingerprint()
    {
        var path = GameSii();

        var file = await _store.ReadAsync(path, default);

        Assert.Equal(File.ReadAllBytes(path), file.Bytes);
        Assert.Equal(file.Bytes.Length, file.Fingerprint.Length);
        Assert.Equal(Sii.Sha256(file.Bytes), file.Fingerprint.Sha256);
        Assert.Equal(path, file.Path);
    }

    [Fact]
    public async Task ReadAsync_MissingFile_ThrowsFileNotFound()
    {
        _t.Dir("save");

        await Assert.ThrowsAsync<FileNotFoundException>(
            () => _store.ReadAsync(_t.Combine("save", "game.sii"), default));
    }

    // ---- sobrescrever

    [Fact]
    public async Task WriteAtomicAsync_ValidRequest_ReplacesContentWithoutBom()
    {
        var path = GameSii();

        await _store.WriteAtomicAsync(await Request(path), default);

        var bytes = File.ReadAllBytes(path);
        Assert.Equal(Sii.GameEdited, Encoding.UTF8.GetString(bytes));
        Assert.NotEqual(0xEF, bytes[0]);
    }

    [Fact]
    public async Task WriteAtomicAsync_ValidRequest_BackupIsIdenticalToOriginalAndNamedByTimestamp()
    {
        var path = GameSii();
        var original = File.ReadAllBytes(path);

        var receipt = await _store.WriteAtomicAsync(await Request(path), default);

        Assert.Equal(path + ".bak-20260102-030405", receipt.BackupPath);
        Assert.Equal(original, File.ReadAllBytes(receipt.BackupPath));
    }

    [Fact]
    public async Task WriteAtomicAsync_ValidRequest_ReceiptHasFingerprintOfNewFile()
    {
        var path = GameSii();

        var receipt = await _store.WriteAtomicAsync(await Request(path), default);

        var now = await _store.ReadAsync(path, default);
        Assert.Equal(now.Fingerprint.Sha256, receipt.NewFingerprint.Sha256);
        Assert.Equal(now.Fingerprint.Length, receipt.NewFingerprint.Length);
    }

    [Fact]
    public async Task WriteAtomicAsync_ValidRequest_LeavesNoTmpFile()
    {
        var path = GameSii();

        await _store.WriteAtomicAsync(await Request(path), default);

        Assert.Empty(Leftovers(Path.GetDirectoryName(path)!));
    }

    [Fact]
    public async Task WriteAtomicAsync_StaleTmpFromEarlierCrash_IsOverwritten()
    {
        var path = GameSii();
        File.WriteAllText(path + ".tmp", "lixo");

        await _store.WriteAtomicAsync(await Request(path), default);

        Assert.Equal(Sii.GameEdited, File.ReadAllText(path));
        Assert.Empty(Leftovers(Path.GetDirectoryName(path)!));
    }

    [Fact]
    public async Task WriteAtomicAsync_Verify_ReceivesTextReadBackFromTmp()
    {
        var path = GameSii();
        string? seen = null;

        await _store.WriteAtomicAsync(await Request(path, verify: t => { seen = t; return true; }), default);

        Assert.Equal(Sii.GameEdited, seen);
    }

    [Fact]
    public async Task WriteAtomicAsync_VerifyReturnsFalse_ThrowsVerifyFailedAndKeepsOriginal()
    {
        var path = GameSii();
        var original = File.ReadAllBytes(path);

        var ex = await Assert.ThrowsAsync<SaveWriteException>(
            async () => await _store.WriteAtomicAsync(await Request(path, verify: _ => false), default));

        Assert.Equal(SaveWriteFailure.VerifyFailed, ex.Failure);
        Assert.Equal(original, File.ReadAllBytes(path));
        Assert.Empty(Leftovers(Path.GetDirectoryName(path)!));
    }

    [Fact]
    public async Task WriteAtomicAsync_VerifyThrows_ThrowsVerifyFailedAndKeepsOriginal()
    {
        var path = GameSii();
        var original = File.ReadAllBytes(path);

        var ex = await Assert.ThrowsAsync<SaveWriteException>(async () =>
            await _store.WriteAtomicAsync(
                await Request(path, verify: _ => throw new SiiFormatException(SiiErrorKind.Syntax, "x")), default));

        Assert.Equal(SaveWriteFailure.VerifyFailed, ex.Failure);
        Assert.Equal(original, File.ReadAllBytes(path));
        Assert.Empty(Leftovers(Path.GetDirectoryName(path)!));
    }

    [Fact]
    public async Task WriteAtomicAsync_FileChangedSinceRead_ThrowsChangedOnDiskWithoutBackup()
    {
        var path = GameSii();
        var request = await Request(path);
        File.WriteAllText(path, Sii.GameEdited + " ");
        var changed = File.ReadAllBytes(path);

        var ex = await Assert.ThrowsAsync<SaveWriteException>(() => _store.WriteAtomicAsync(request, default));

        Assert.Equal(SaveWriteFailure.ChangedOnDisk, ex.Failure);
        Assert.Equal(changed, File.ReadAllBytes(path));
        Assert.Empty(Directory.GetFiles(Path.GetDirectoryName(path)!, "*.bak-*"));
    }

    [Fact]
    public async Task WriteAtomicAsync_MissingFile_ThrowsNotFound()
    {
        var path = GameSii();
        var request = await Request(path);
        File.Delete(path);

        var ex = await Assert.ThrowsAsync<SaveWriteException>(() => _store.WriteAtomicAsync(request, default));

        Assert.Equal(SaveWriteFailure.NotFound, ex.Failure);
        Assert.False(File.Exists(path));
    }

    [Fact]
    public async Task WriteAtomicAsync_BackupAlreadyExists_ThrowsBackupExistsAndKeepsBothFiles()
    {
        var path = GameSii();
        var existing = path + ".bak-20260102-030405";
        File.WriteAllText(existing, "backup antigo");
        var original = File.ReadAllBytes(path);

        var ex = await Assert.ThrowsAsync<SaveWriteException>(
            async () => await _store.WriteAtomicAsync(await Request(path), default));

        Assert.Equal(SaveWriteFailure.BackupExists, ex.Failure);
        Assert.Equal("backup antigo", File.ReadAllText(existing));
        Assert.Equal(original, File.ReadAllBytes(path));
        Assert.Empty(Leftovers(Path.GetDirectoryName(path)!));
    }

    [Fact]
    public async Task WriteAtomicAsync_Cancelled_KeepsOriginalAndLeavesNoTmp()
    {
        var path = GameSii();
        var original = File.ReadAllBytes(path);
        var request = await Request(path);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _store.WriteAtomicAsync(request, cts.Token));

        Assert.Equal(original, File.ReadAllBytes(path));
        Assert.Empty(Leftovers(Path.GetDirectoryName(path)!));
    }

    [Fact]
    public async Task WriteAtomicAsync_TmpPathUnwritable_ThrowsWriteFailureAndKeepsOriginal()
    {
        var path = GameSii();
        Directory.CreateDirectory(path + ".tmp");
        var original = File.ReadAllBytes(path);

        var ex = await Assert.ThrowsAsync<SaveWriteException>(
            async () => await _store.WriteAtomicAsync(await Request(path), default));

        Assert.Contains(ex.Failure, new[] { SaveWriteFailure.WriteFailed, SaveWriteFailure.AccessDenied });
        Assert.Equal(original, File.ReadAllBytes(path));
    }

    [Fact]
    public async Task WriteAtomicAsync_ReadOnlyDirectory_ThrowsAccessDeniedAndKeepsOriginal()
    {
        if (OperatingSystem.IsWindows() || Environment.UserName == "root") return; // chmod não vale
        var path = GameSii();
        var original = File.ReadAllBytes(path);
        var request = await Request(path);
        File.SetUnixFileMode(Path.GetDirectoryName(path)!, UnixFileMode.UserRead | UnixFileMode.UserExecute);

        var ex = await Assert.ThrowsAsync<SaveWriteException>(() => _store.WriteAtomicAsync(request, default));

        Assert.Contains(ex.Failure, new[] { SaveWriteFailure.AccessDenied, SaveWriteFailure.BackupFailed });
        Assert.Equal(original, File.ReadAllBytes(path));
    }

    // ---- slot novo

    private (SaveSlotInfo Source, NewSlotWriteRequest Request) NewSlot(string displayName = "Meu novo",
        Func<string, bool>? verify = null, string[]? slots = null)
    {
        var profile = Layout.Profile(_t, "docs", "profiles", Sii.Hex("Joao"), GameKind.Ets2,
            (slots ?? ["1", "3"]).Select(s => (s, "Original " + s)).ToArray());
        var gameSii = Path.Combine(profile.DirectoryPath, "save", "1", "game.sii");
        var info = new FileInfo(gameSii);
        var source = new SaveSlotInfo(profile, "1", "Original 1", gameSii, info.LastWriteTimeUtc, info.Length);
        var fp = _store.ReadAsync(gameSii, default).Result.Fingerprint;
        return (source, new NewSlotWriteRequest(source, fp, Sii.GameEdited, displayName, verify ?? (_ => true)));
    }

    [Fact]
    public async Task WriteNewSlotAsync_ValidRequest_UsesNextFreeSlotInSameProfile()
    {
        var (source, request) = NewSlot();

        var receipt = await _store.WriteNewSlotAsync(request, default);

        Assert.Equal("4", receipt.NewSlot.Id);
        Assert.Equal(Path.Combine(source.Profile.DirectoryPath, "save", "4", "game.sii"), receipt.NewSlot.GameSiiPath);
        Assert.Equal(source.Profile, receipt.NewSlot.Profile);
        Assert.Equal("Meu novo", receipt.NewSlot.DisplayName);
        Assert.Equal(new FileInfo(receipt.NewSlot.GameSiiPath).Length, receipt.NewSlot.SizeBytes);
    }

    [Fact]
    public async Task WriteNewSlotAsync_ValidRequest_WritesNewGameSiiAndRenamesInfo()
    {
        var (_, request) = NewSlot();

        var receipt = await _store.WriteNewSlotAsync(request, default);

        var folder = Path.GetDirectoryName(receipt.NewSlot.GameSiiPath)!;
        Assert.Equal(Sii.GameEdited, File.ReadAllText(receipt.NewSlot.GameSiiPath));
        var info = SiiDocument.Load(Path.Combine(folder, "info.sii"));
        Assert.Equal("\"Meu novo\"", info.Units[0].Get("name"));
        Assert.Equal("100", info.Units[0].Get("time"));
        Assert.Empty(Leftovers(folder));
    }

    [Fact]
    public async Task WriteNewSlotAsync_ValidRequest_LeavesOriginalSlotUntouched()
    {
        var (source, request) = NewSlot();
        var folder = Path.GetDirectoryName(source.GameSiiPath)!;
        var game = File.ReadAllBytes(source.GameSiiPath);
        var info = File.ReadAllBytes(Path.Combine(folder, "info.sii"));

        await _store.WriteNewSlotAsync(request, default);

        Assert.Equal(game, File.ReadAllBytes(source.GameSiiPath));
        Assert.Equal(info, File.ReadAllBytes(Path.Combine(folder, "info.sii")));
        Assert.Equal(["game.sii", "info.sii"], Directory.GetFiles(folder).Select(Path.GetFileName).Order().ToArray());
    }

    [Fact]
    public async Task WriteNewSlotAsync_SlotWithExtraFiles_CopiesThemRecursively()
    {
        var (source, request) = NewSlot();
        var folder = Path.GetDirectoryName(source.GameSiiPath)!;
        File.WriteAllBytes(Path.Combine(folder, "sub.dat"), [1, 2, 3]);
        Directory.CreateDirectory(Path.Combine(folder, "pasta"));
        File.WriteAllBytes(Path.Combine(folder, "pasta", "x.bin"), [9, 8]);

        var receipt = await _store.WriteNewSlotAsync(request, default);

        var copy = Path.GetDirectoryName(receipt.NewSlot.GameSiiPath)!;
        Assert.Equal([1, 2, 3], File.ReadAllBytes(Path.Combine(copy, "sub.dat")));
        Assert.Equal([9, 8], File.ReadAllBytes(Path.Combine(copy, "pasta", "x.bin")));
    }

    [Fact]
    public async Task WriteNewSlotAsync_NameWithQuotesAndNewlines_IsSanitizedInInfo()
    {
        var (_, request) = NewSlot("Ab\"c\r\nd");

        var receipt = await _store.WriteNewSlotAsync(request, default);

        var info = SiiDocument.Load(Path.Combine(Path.GetDirectoryName(receipt.NewSlot.GameSiiPath)!, "info.sii"));
        Assert.Equal("\"Abcd\"", info.Units[0].Get("name"));
        Assert.Equal("Abcd", receipt.NewSlot.DisplayName);
    }

    [Fact]
    public async Task WriteNewSlotAsync_SourceWithoutInfo_StillCreatesSlot()
    {
        var (source, request) = NewSlot();
        File.Delete(Path.Combine(Path.GetDirectoryName(source.GameSiiPath)!, "info.sii"));

        var receipt = await _store.WriteNewSlotAsync(request, default);

        Assert.Equal(Sii.GameEdited, File.ReadAllText(receipt.NewSlot.GameSiiPath));
    }

    [Fact]
    public async Task WriteNewSlotAsync_VerifyFails_DeletesNewFolderAndKeepsOriginal()
    {
        var (source, request) = NewSlot(verify: _ => false);
        var saveDir = Path.Combine(source.Profile.DirectoryPath, "save");
        var before = Directory.GetDirectories(saveDir).Order().ToArray();
        var game = File.ReadAllBytes(source.GameSiiPath);

        var ex = await Assert.ThrowsAsync<SaveWriteException>(() => _store.WriteNewSlotAsync(request, default));

        Assert.Equal(SaveWriteFailure.VerifyFailed, ex.Failure);
        Assert.Equal(before, Directory.GetDirectories(saveDir).Order().ToArray());
        Assert.Equal(game, File.ReadAllBytes(source.GameSiiPath));
    }

    [Fact]
    public async Task WriteNewSlotAsync_SourceChangedOnDisk_ThrowsAndCreatesNothing()
    {
        var (source, request) = NewSlot();
        File.AppendAllText(source.GameSiiPath, " ");
        var saveDir = Path.Combine(source.Profile.DirectoryPath, "save");
        var before = Directory.GetDirectories(saveDir).Order().ToArray();

        var ex = await Assert.ThrowsAsync<SaveWriteException>(() => _store.WriteNewSlotAsync(request, default));

        Assert.Equal(SaveWriteFailure.ChangedOnDisk, ex.Failure);
        Assert.Equal(before, Directory.GetDirectories(saveDir).Order().ToArray());
    }

    [Fact]
    public async Task WriteNewSlotAsync_SourceMissing_ThrowsNotFound()
    {
        var (source, request) = NewSlot();
        File.Delete(source.GameSiiPath);

        var ex = await Assert.ThrowsAsync<SaveWriteException>(() => _store.WriteNewSlotAsync(request, default));

        Assert.Equal(SaveWriteFailure.NotFound, ex.Failure);
    }

    [Fact]
    public async Task WriteNewSlotAsync_Cancelled_CreatesNothing()
    {
        var (source, request) = NewSlot();
        var saveDir = Path.Combine(source.Profile.DirectoryPath, "save");
        var before = Directory.GetDirectories(saveDir).Order().ToArray();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _store.WriteNewSlotAsync(request, cts.Token));

        Assert.Equal(before, Directory.GetDirectories(saveDir).Order().ToArray());
    }

    [Fact]
    public async Task WriteNewSlotAsync_BlankName_ThrowsArgumentException()
    {
        var (_, request) = NewSlot("  \"\" ");

        await Assert.ThrowsAsync<ArgumentException>(() => _store.WriteNewSlotAsync(request, default));
    }
}
