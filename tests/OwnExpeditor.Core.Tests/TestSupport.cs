using System.Security.Cryptography;
using System.Text;
using OwnExpeditor.Core.Games;
using OwnExpeditor.Core.Session;
using OwnExpeditor.Core.Text;
using OwnExpeditor.Data;
using OwnExpeditor.Data.Games;
using OwnExpeditor.Data.Saves;
using OwnExpeditor.Data.Settings;
using OwnExpeditor.Domain.Games;
using OwnExpeditor.Domain.Saves;
using OwnExpeditor.Domain.Settings;

namespace OwnExpeditor.Core.Tests;

/// <summary>Diretório temporário único, sempre apagado.</summary>
public sealed class TempDir : IDisposable
{
    public string Path { get; } = Directory.CreateTempSubdirectory("ownexp-core-").FullName;

    public string Combine(params string[] parts) => System.IO.Path.Combine([Path, .. parts]);

    public string File(string content, params string[] parts)
    {
        var p = Combine(parts);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(p)!);
        System.IO.File.WriteAllText(p, content, new UTF8Encoding(false));
        return p;
    }

    public void Dispose() => Directory.Delete(Path, recursive: true);
}

/// <summary>Save sintético pequeno (ids inventados, sem dados pessoais).</summary>
public static class SampleSave
{
    public const string Text =
        "SiiNunit\r\n{\r\n" +
        "economy : _nameless.1 {\r\n game_time: 1000\r\n}\r\n\r\n" +
        "company : company.volatile.demo_co.city_a {\r\n job_offer: 2\r\n" +
        " job_offer[0]: _nameless.a\r\n job_offer[1]: _nameless.b\r\n}\r\n\r\n" +
        "job_offer_data : _nameless.a {\r\n target: \"demo_co.city_b\"\r\n cargo: cargo.demo_goods\r\n urgency: 0\r\n}\r\n\r\n" +
        "job_offer_data : _nameless.b {\r\n target: \"\"\r\n cargo: null\r\n urgency: nil\r\n}\r\n\r\n" +
        "}\r\n";

    public const int BlockCount = 4;

    public static byte[] Bytes => new UTF8Encoding(false).GetBytes(Text);

    public static SaveSlotInfo Slot(GameKind game = GameKind.Ets2, string path = "/profiles/p1/save/1/game.sii", string slotId = "1")
    {
        var profile = new ProfileInfo(game, "70317031", "p1", "/profiles/p1", false);
        return new SaveSlotInfo(profile, slotId, "Slot " + slotId, path, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), Bytes.Length);
    }

    public static ChangeSet Change(string key, string unit, string attribute, string oldValue, string newValue,
        string module = "jobs") =>
        new(module, key, TextRef.Of("t", key), [new DiffLine(TextRef.Of("l"), new DiffValue.Raw(oldValue), new DiffValue.Raw(newValue))],
            [new SetAttribute(unit, attribute, oldValue, newValue)]);

    /// <summary>Preenche o slot vazio _nameless.b com um destino.</summary>
    public static ChangeSet FillTarget(string value = "\"demo_co.city_c\"", string key = "jobs:slot:b") =>
        Change(key, "_nameless.b", "target", "\"\"", value);
}

public sealed class FakeGameData(GameKind game = GameKind.Ets2, bool available = true) : IGameData
{
    public GameKind Game { get; } = game;
    public bool IsAvailable { get; } = available;
    public string Get(GameNameKind kind, string id) => $"{kind}:{id}";
    public bool Ships(string company, string cargo) => false;
    public bool Accepts(string company, string cargo) => false;
    public IReadOnlyCollection<string> CargoOut(string company) => [];
}

public sealed class FakeGameDataService : IGameDataService
{
    public Dictionary<GameKind, IGameData> Data { get; } = new()
    {
        [GameKind.Ets2] = new FakeGameData(GameKind.Ets2),
        [GameKind.Ats] = new FakeGameData(GameKind.Ats),
    };

    public Task<IGameData> GetAsync(GameKind game, CancellationToken ct) => Task.FromResult(Data[game]);
    public GameFolderStatus GetFolder(GameKind game) => new(game, null, GameFolderSource.NotFound);
    public Task<GameFolderCheck> SetManualFolderAsync(GameKind game, string? path, CancellationToken ct) =>
        Task.FromResult(GameFolderCheck.Ok);
    public event EventHandler<GameKind>? Changed;
    public void RaiseChanged(GameKind game) => Changed?.Invoke(this, game);
}

public sealed class FakeProcessMonitor : IGameProcessMonitor
{
    private readonly Dictionary<GameKind, bool> _running = [];
    public List<GameKind> Calls { get; } = [];
    public void Set(GameKind game, bool running) => _running[game] = running;

    public bool IsRunning(GameKind game)
    {
        Calls.Add(game);
        return _running.GetValueOrDefault(game);
    }
}

/// <summary>Store em memória com as mesmas regras de ChangedOnDisk/Verify do real, para testar o Core isolado.</summary>
public sealed class FakeSaveFileStore : ISaveFileStore
{
    public Dictionary<string, byte[]> Files { get; } = [];
    public List<string> Log { get; } = [];
    public List<SaveWriteRequest> AtomicRequests { get; } = [];
    public List<NewSlotWriteRequest> NewSlotRequests { get; } = [];
    public SaveWriteFailure? FailWith { get; set; }
    public Exception? ReadThrows { get; set; }
    public TaskCompletionSource? Gate { get; set; }
    public SaveSlotInfo? NextSlotResult { get; set; }
    public string BackupPath { get; set; } = "/profiles/p1/save/1/game.sii.bak-20260101-000000";

    public static SaveFingerprint FingerprintOf(byte[] bytes) =>
        new(bytes.LongLength, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), Convert.ToHexString(SHA256.HashData(bytes)));

    public void Add(string path, string text) => Files[path] = new UTF8Encoding(false).GetBytes(text);

    public Task<SaveFile> ReadAsync(string path, CancellationToken ct)
    {
        Log.Add("read");
        ct.ThrowIfCancellationRequested();
        if (ReadThrows is not null) throw ReadThrows;
        if (!Files.TryGetValue(path, out var bytes)) throw new FileNotFoundException(path);
        return Task.FromResult(new SaveFile(path, bytes, FingerprintOf(bytes)));
    }

    public async Task<SaveWriteReceipt> WriteAtomicAsync(SaveWriteRequest request, CancellationToken ct)
    {
        Log.Add("write");
        AtomicRequests.Add(request);
        if (Gate is not null) await Gate.Task;
        ct.ThrowIfCancellationRequested();
        if (FailWith is { } f) throw new SaveWriteException(f, "falha simulada");
        var current = Files[request.Path];
        if (FingerprintOf(current).Sha256 != request.Expected.Sha256)
            throw new SaveWriteException(SaveWriteFailure.ChangedOnDisk, "mudou");
        if (!request.VerifyWritten(request.NewText))
            throw new SaveWriteException(SaveWriteFailure.VerifyFailed, "verificação");
        var bytes = new UTF8Encoding(false).GetBytes(request.NewText);
        Files[request.Path] = bytes;
        return new SaveWriteReceipt(BackupPath, FingerprintOf(bytes));
    }

    public Task<NewSlotReceipt> WriteNewSlotAsync(NewSlotWriteRequest request, CancellationToken ct)
    {
        Log.Add("write-new");
        NewSlotRequests.Add(request);
        ct.ThrowIfCancellationRequested();
        if (FailWith is { } f) throw new SaveWriteException(f, "falha simulada");
        if (!request.VerifyWritten(request.NewText))
            throw new SaveWriteException(SaveWriteFailure.VerifyFailed, "verificação");
        var bytes = new UTF8Encoding(false).GetBytes(request.NewText);
        var slot = NextSlotResult ?? request.Source with
        {
            Id = "2",
            DisplayName = request.DisplayName,
            GameSiiPath = "/profiles/p1/save/2/game.sii",
            SizeBytes = bytes.Length,
            LastWriteUtc = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc),
        };
        Files[slot.GameSiiPath] = bytes;
        return Task.FromResult(new NewSlotReceipt(slot));
    }
}

public sealed class CollectingProgress : IProgress<LoadProgress>
{
    public List<LoadProgress> Reports { get; } = [];
    public void Report(LoadProgress value) => Reports.Add(value);
}

/// <summary>Cada leitura do relógio avança <c>StepMs</c> ms: deixa passar todo relatório de progresso.</summary>
public sealed class SteppingTime(long stepMs = 150) : TimeProvider
{
    private long _now;
    public override long TimestampFrequency => 1000;
    public override long GetTimestamp() => _now += stepMs;
}

public sealed class FakeSessionHost : IEditSessionHost
{
    public IEditSession? Current { get; private set; }
    public event EventHandler<SessionChangedEventArgs>? SessionChanged;

    public void Set(IEditSession? session)
    {
        var previous = Current;
        Current = session;
        SessionChanged?.Invoke(this, new SessionChangedEventArgs(previous, session));
    }

    public Task<IEditSession> OpenAsync(SaveSlotInfo save, IProgress<LoadProgress>? progress, CancellationToken ct) =>
        throw new NotSupportedException();

    public void Close() => Set(null);
}

public sealed class FakeAppSettings : OwnExpeditor.Core.Settings.IAppSettingsService
{
    public AppSettings Current { get; private set; } =
        new(ThemePreference.System, Language.En, null, new GameFolders());
    public List<AppSettings> Updates { get; } = [];
    public event EventHandler<AppSettings>? Changed;
    public Task LoadAsync(CancellationToken ct) => Task.CompletedTask;

    public Task UpdateAsync(AppSettings settings, CancellationToken ct)
    {
        Updates.Add(settings);
        Current = settings;
        Changed?.Invoke(this, settings);
        return Task.CompletedTask;
    }
}

public sealed class FakeInstallLocator : IGameInstallLocator
{
    public Dictionary<GameKind, List<string>> Detected { get; } = [];
    public Dictionary<(GameKind, string), GameFolderCheck> Checks { get; } = [];
    public int DetectCalls { get; private set; }

    public IReadOnlyList<string> Detect(GameKind game)
    {
        DetectCalls++;
        return Detected.TryGetValue(game, out var l) ? l : [];
    }

    public GameFolderCheck Validate(GameKind game, string path) =>
        Checks.TryGetValue((game, path), out var c) ? c : GameFolderCheck.NotFound;
}

public sealed class FakeSettingsStore : ISettingsStore
{
    public AppSettings? Stored { get; set; }
    public List<AppSettings> Saved { get; } = [];
    public Exception? SaveThrows { get; set; }

    public Task<AppSettings?> LoadAsync(CancellationToken ct) => Task.FromResult(Stored);

    public Task SaveAsync(AppSettings settings, CancellationToken ct)
    {
        if (SaveThrows is not null) throw SaveThrows;
        Saved.Add(settings);
        Stored = settings;
        return Task.CompletedTask;
    }
}

public sealed class FakeCatalogSource : IGameCatalogSource
{
    public GameCatalog Catalog { get; set; } = new();
    public Dictionary<string, Localization> Texts { get; } = [];
    public List<string> DefinitionCalls { get; } = [];
    public List<(string Dir, IReadOnlyList<string> Languages)> TextCalls { get; } = [];
    public Exception? DefinitionsThrow { get; set; }

    public GameCatalog LoadDefinitions(string installDir)
    {
        DefinitionCalls.Add(installDir);
        if (DefinitionsThrow is not null) throw DefinitionsThrow;
        return Catalog;
    }

    public Localization LoadTexts(string installDir, IReadOnlyList<string> languages)
    {
        TextCalls.Add((installDir, languages));
        foreach (var l in languages)
            if (Texts.TryGetValue(l, out var t)) return t;
        return new Localization();
    }

    public string? GameLanguage(ProfileInfo profile) => null;
}

public sealed class FakeProfileLocator : IProfileLocator
{
    public List<ProfileInfo> Profiles { get; } = [];
    public Dictionary<ProfileInfo, List<SaveSlotInfo>> Saves { get; } = [];
    public Exception? Throws { get; set; }

    public IReadOnlyList<ProfileInfo> FindProfiles() => Throws is null ? Profiles : throw Throws;

    public IReadOnlyList<SaveSlotInfo> FindSaves(ProfileInfo profile) =>
        Throws is not null ? throw Throws : Saves.TryGetValue(profile, out var l) ? l : [];
}
