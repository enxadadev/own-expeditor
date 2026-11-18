using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using OwnExpeditor.App.Services;
using OwnExpeditor.App.Shell;
using OwnExpeditor.App.ViewModels;
using OwnExpeditor.Core;
using OwnExpeditor.Core.Games;
using OwnExpeditor.Core.Profiles;
using OwnExpeditor.Core.Session;
using OwnExpeditor.Core.Settings;
using OwnExpeditor.Core.Text;
using OwnExpeditor.Domain.Games;
using OwnExpeditor.Domain.Saves;
using OwnExpeditor.Domain.Settings;

namespace OwnExpeditor.App.Tests.Support;

/// <summary>Save sintético pequeno (ids inventados, sem dados pessoais).</summary>
public static class Sample
{
    public const string Text =
        "SiiNunit\r\n{\r\n" +
        "economy : _nameless.1 {\r\n game_time: 1000\r\n money_account: 100\r\n}\r\n\r\n" +
        "job_offer_data : _nameless.a {\r\n target: \"demo_co.city_b\"\r\n cargo: cargo.demo_goods\r\n}\r\n\r\n" +
        "job_offer_data : _nameless.b {\r\n target: \"demo_co.city_c\"\r\n cargo: cargo.demo_other\r\n}\r\n\r\n" +
        "}\r\n";

    public static ProfileInfo Profile(string name = "Demo A", bool cloud = false, GameKind game = GameKind.Ets2) =>
        new(game, "44656D6F41", name, "/profiles/44656D6F41", cloud);

    public static SaveSlotInfo Slot(string id = "4", string name = "Save 4", bool cloud = false, ProfileInfo? profile = null) =>
        new(profile ?? Profile(cloud: cloud), id, name, $"/profiles/44656D6F41/save/{id}/game.sii",
            new DateTime(2026, 11, 17, 14, 32, 0, DateTimeKind.Utc), 90_000_000);

    public static ChangeSet Job(string key, string unit, string oldTarget, string newTarget, string module = "jobs") =>
        new(module, key, TextRef.Of("Jobs_Change_Title", "A", "B"),
            [new DiffLine(TextRef.Of("Jobs_Field_Destination"), new DiffValue.Raw(oldTarget), new DiffValue.Raw(newTarget))],
            [new SetAttribute(unit, "target", $"\"{oldTarget}\"", $"\"{newTarget}\"")]);

    public static ChangeSet Money(long to) =>
        new("profile", "profile:money", TextRef.Of("Profile_Change_Money_Title"),
            [new DiffLine(TextRef.Of("Profile_Field_Money"), new DiffValue.Money(100, GameKind.Ets2), new DiffValue.Money(to, GameKind.Ets2))],
            [new SetAttribute("_nameless.1", "money_account", "100", to.ToString())]);

    /// <summary>Sessão real (Stage/Discard/GetDiff de verdade) com gravação simulada.</summary>
    public static TestSession Session(SaveSlotInfo? save = null, int changes = 0)
    {
        var s = new TestSession(EditSessionFactory.FromText(Text, save ?? Slot()));
        if (changes >= 1) s.Stage(Job("jobs:slot:a", "_nameless.a", "demo_co.city_b", "demo_co.city_x"));
        if (changes >= 2) s.Stage(Job("jobs:slot:b", "_nameless.b", "demo_co.city_c", "demo_co.city_y"));
        if (changes >= 3) s.Stage(Money(500));
        return s;
    }
}

public sealed class TestSession(IEditSession inner) : IEditSession
{
    private SaveSlotInfo? _save;

    public List<ApplyTarget> Applied { get; } = [];

    public Func<ApplyTarget, Task<ApplyResult>>? OnApply { get; set; }

    public SaveSlotInfo Save => _save ?? inner.Save;

    public GameKind Game => inner.Game;

    public bool IsReadOnly => false;

    public ISaveView Original => inner.Original;

    public ISaveView View => inner.View;

    public IGameData GameData => inner.GameData;

    public IReadOnlyList<ChangeSet> Pending => inner.Pending;

    public bool HasChanges => inner.HasChanges;

    public event EventHandler? Changed
    {
        add => inner.Changed += value;
        remove => inner.Changed -= value;
    }

    public void Stage(ChangeSet change) => inner.Stage(change);

    public bool Discard(Guid changeSetId) => inner.Discard(changeSetId);

    public void DiscardModule(string moduleId) => inner.DiscardModule(moduleId);

    public void DiscardAll() => inner.DiscardAll();

    public SessionDiff GetDiff() => inner.GetDiff();

    public async Task<ApplyResult> ApplyAsync(ApplyTarget target, CancellationToken ct)
    {
        Applied.Add(target);
        var result = OnApply is null
            ? new ApplyResult(Save, "/profiles/44656D6F41/save/4/game.sii.bak-20261117-143022", Pending.Count, Pending.Count)
            : await OnApply(target);
        _save = result.WrittenSlot;
        inner.DiscardAll();
        return result;
    }
}

public sealed class FakeHost : IEditSessionHost
{
    public Func<SaveSlotInfo, IProgress<LoadProgress>?, CancellationToken, Task<IEditSession>> Open { get; set; } =
        (save, _, _) => Task.FromResult<IEditSession>(Sample.Session(save));

    public int CloseCalls { get; private set; }

    public List<SaveSlotInfo> Opened { get; } = [];

    public IEditSession? Current { get; private set; }

    public event EventHandler<SessionChangedEventArgs>? SessionChanged;

    public async Task<IEditSession> OpenAsync(SaveSlotInfo save, IProgress<LoadProgress>? progress, CancellationToken ct)
    {
        Opened.Add(save);
        var session = await Open(save, progress, ct);
        var previous = Current;
        Current = session;
        SessionChanged?.Invoke(this, new SessionChangedEventArgs(previous, session));
        return session;
    }

    public void Close()
    {
        CloseCalls++;
        var previous = Current;
        Current = null;
        if (previous is not null) SessionChanged?.Invoke(this, new SessionChangedEventArgs(previous, null));
    }
}

public sealed class FakeProfiles : IProfileService
{
    public List<ProfileInfo> Profiles { get; } = [Sample.Profile()];

    public Dictionary<string, SaveSlotInfo[]> Saves { get; } = new() { ["/profiles/44656D6F41"] = [Sample.Slot(), Sample.Slot("autosave", "Autosave")] };

    public Task<IReadOnlyList<ProfileInfo>> GetProfilesAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<ProfileInfo>>(Profiles);

    public Task<IReadOnlyList<SaveSlotInfo>> GetSavesAsync(ProfileInfo profile, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<SaveSlotInfo>>(Saves.GetValueOrDefault(profile.DirectoryPath) ?? []);
}

public sealed class FakeSettings : IAppSettingsService
{
    public AppSettings Current { get; set; } = new(ThemePreference.System, Language.En, null, new GameFolders());

    public Exception? FailWith { get; set; }

    public Exception? LoadFailWith { get; set; }

    public List<AppSettings> Updates { get; } = [];

    public int Loads { get; private set; }

    public event EventHandler<AppSettings>? Changed;

    public Task LoadAsync(CancellationToken ct)
    {
        Loads++;
        return LoadFailWith is null ? Task.CompletedTask : Task.FromException(LoadFailWith);
    }

    public Task UpdateAsync(AppSettings settings, CancellationToken ct)
    {
        if (FailWith is not null) return Task.FromException(FailWith);
        Updates.Add(settings);
        Current = settings;
        Changed?.Invoke(this, settings);
        return Task.CompletedTask;
    }
}

public sealed class FakeGameDataService : IGameDataService
{
    public Dictionary<GameKind, GameFolderStatus> Folders { get; } = new()
    {
        [GameKind.Ets2] = new(GameKind.Ets2, "/games/ets2", GameFolderSource.Detected),
        [GameKind.Ats] = new(GameKind.Ats, null, GameFolderSource.NotFound),
    };

    public GameFolderCheck NextCheck { get; set; } = GameFolderCheck.Ok;

    public List<(GameKind Game, string? Path)> Sets { get; } = [];

    public event EventHandler<GameKind>? Changed { add { } remove { } }

    public Task<IGameData> GetAsync(GameKind game, CancellationToken ct) => throw new NotSupportedException();

    public GameFolderStatus GetFolder(GameKind game) => Folders[game];

    public Task<GameFolderCheck> SetManualFolderAsync(GameKind game, string? path, CancellationToken ct)
    {
        Sets.Add((game, path));
        if (NextCheck != GameFolderCheck.Ok) return Task.FromResult(NextCheck);
        Folders[game] = path is null ? new(game, null, GameFolderSource.NotFound) : new(game, path, GameFolderSource.Manual);
        return Task.FromResult(GameFolderCheck.Ok);
    }
}

public sealed class FakeMonitor : IGameStatusMonitor
{
    public bool IsRunning { get; private set; }

    public event EventHandler<bool>? RunningChanged;

    public void Set(bool running)
    {
        IsRunning = running;
        RunningChanged?.Invoke(this, running);
    }
}

public sealed class FakeLauncher : IShellLauncher
{
    public List<string> Folders { get; } = [];

    public List<string> Revealed { get; } = [];

    public bool Succeeds { get; set; } = true;

    public Task<bool> OpenFolderAsync(string folderPath)
    {
        Folders.Add(folderPath);
        return Task.FromResult(Succeeds);
    }

    public Task<bool> RevealFileAsync(string filePath)
    {
        Revealed.Add(filePath);
        return Task.FromResult(Succeeds);
    }
}

public sealed class FakeClipboard : IClipboardService
{
    public List<string> Texts { get; } = [];

    public Task SetTextAsync(string text)
    {
        Texts.Add(text);
        return Task.CompletedTask;
    }
}

public sealed class FakePicker : IFolderPicker
{
    public string? Result { get; set; }

    public Task<string?> PickFolderAsync(TextRef title, string? startPath) => Task.FromResult(Result);
}

public sealed class FakeTheme : IThemeService
{
    public ThemePreference Current { get; private set; } = ThemePreference.System;

    public List<ThemePreference> Applied { get; } = [];

    public event EventHandler? Changed;

    public void Apply(ThemePreference theme)
    {
        Applied.Add(theme);
        Current = theme;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}

/// <summary>Shell completo montado pela raiz de composição real, trocando só o que toca disco/jogo/janela.</summary>
public sealed class ShellFixture
{
    public ShellFixture(Language language = Language.PtBr)
    {
        Loc = new Localizer(language);
        Settings.Current = Settings.Current with { Language = language };
        Provider = AppComposition.Build(s =>
        {
            s.AddSingleton<ILocalizer>(Loc);
            s.AddSingleton<IUiDispatcher, ImmediateUiDispatcher>();
            s.AddSingleton<TimeProvider>(Time);
            s.AddSingleton<IEditSessionHost>(Host);
            s.AddSingleton<IProfileService>(Profiles);
            s.AddSingleton<IAppSettingsService>(Settings);
            s.AddSingleton<IGameDataService>(GameData);
            s.AddSingleton<IGameStatusMonitor>(Monitor);
            s.AddSingleton<IShellLauncher>(Launcher);
            s.AddSingleton<IClipboardService>(Clipboard);
            s.AddSingleton<IFolderPicker>(Picker);
            s.AddSingleton<IThemeService>(Theme);
        });
        Shell = Provider.GetRequiredService<ShellViewModel>();
    }

    public Localizer Loc { get; }

    public FakeTimeProvider Time { get; } = new();

    public FakeHost Host { get; } = new();

    public FakeProfiles Profiles { get; } = new();

    public FakeSettings Settings { get; } = new();

    public FakeGameDataService GameData { get; } = new();

    public FakeMonitor Monitor { get; } = new();

    public FakeLauncher Launcher { get; } = new();

    public FakeClipboard Clipboard { get; } = new();

    public FakePicker Picker { get; } = new();

    public FakeTheme Theme { get; } = new();

    public IServiceProvider Provider { get; }

    public ShellViewModel Shell { get; }

    public IDialogService Dialogs => Shell.Dialogs;

    public ToastViewModel Toast => Shell.Toast;

    /// <summary>Abre o save e devolve a sessão de teste (com as pendências pedidas).</summary>
    public async Task<TestSession> OpenAsync(int changes = 0, SaveSlotInfo? save = null)
    {
        save ??= Sample.Slot();
        TestSession? session = null;
        Host.Open = (s, _, _) => Task.FromResult<IEditSession>(session = Sample.Session(s, changes));
        await Shell.OpenSaveAsync(save);
        return session!;
    }
}
