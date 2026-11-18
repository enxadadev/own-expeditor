using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OwnExpeditor.App.Services;
using OwnExpeditor.Core;
using OwnExpeditor.Core.Games;
using OwnExpeditor.Core.Profiles;
using OwnExpeditor.Core.Session;
using OwnExpeditor.Core.Settings;
using OwnExpeditor.Core.Text;
using OwnExpeditor.Domain.Games;
using OwnExpeditor.Domain.Saves;
using OwnExpeditor.Domain.Settings;

namespace OwnExpeditor.App.Design;

/// <summary>Serviços falsos das amostras: nada toca o disco nem o jogo.</summary>
public sealed class DesignHost : IEditSessionHost
{
    private IEditSession? _current;

    public Func<SaveSlotInfo, IProgress<LoadProgress>?, CancellationToken, Task<IEditSession>> Open { get; set; } =
        (save, _, _) => Task.FromResult<IEditSession>(DesignData.SessionWithChanges(save));

    public IEditSession? Current => _current;

    public event EventHandler<SessionChangedEventArgs>? SessionChanged;

    public async Task<IEditSession> OpenAsync(SaveSlotInfo save, IProgress<LoadProgress>? progress, CancellationToken ct)
    {
        var session = await Open(save, progress, ct);
        var previous = _current;
        _current = session;
        SessionChanged?.Invoke(this, new SessionChangedEventArgs(previous, session));
        return session;
    }

    public void Close()
    {
        var previous = _current;
        _current = null;
        if (previous is not null) SessionChanged?.Invoke(this, new SessionChangedEventArgs(previous, null));
    }
}

/// <summary>Sessão com <c>Stage</c>/<c>Discard</c> reais (sobre texto sintético) e gravação simulada.</summary>
public sealed class DesignSession(IEditSession inner, Func<ApplyTarget, CancellationToken, Task<ApplyResult>> apply) : IEditSession
{
    private SaveSlotInfo? _save;

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
        var result = await apply(target, ct);
        _save = result.WrittenSlot;
        inner.DiscardAll();
        return result;
    }
}

public sealed class DesignProfiles(params ProfileInfo[] profiles) : IProfileService
{
    public Task<IReadOnlyList<ProfileInfo>> GetProfilesAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<ProfileInfo>>(profiles);

    public Task<IReadOnlyList<SaveSlotInfo>> GetSavesAsync(ProfileInfo profile, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<SaveSlotInfo>>(profile.DisplayName == "Demo B" ? DesignData.SavesOf(profile)[..2] : DesignData.SavesOf(profile));
}

public sealed class DesignSettings(AppSettings settings) : IAppSettingsService
{
    public AppSettings Current { get; private set; } = settings;

    public event EventHandler<AppSettings>? Changed;

    public Task LoadAsync(CancellationToken ct) => Task.CompletedTask;

    public Task UpdateAsync(AppSettings value, CancellationToken ct)
    {
        Current = value;
        Changed?.Invoke(this, value);
        return Task.CompletedTask;
    }
}

public sealed class DesignGameData : IGameDataService
{
    public Dictionary<GameKind, GameFolderStatus> Folders { get; } = new()
    {
        [GameKind.Ets2] = new(GameKind.Ets2, "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Euro Truck Simulator 2", GameFolderSource.Detected),
        [GameKind.Ats] = new(GameKind.Ats, null, GameFolderSource.NotFound),
    };

    public event EventHandler<GameKind>? Changed { add { } remove { } }

    public Task<IGameData> GetAsync(GameKind game, CancellationToken ct) => throw new NotSupportedException();

    public GameFolderStatus GetFolder(GameKind game) => Folders[game];

    public Task<GameFolderCheck> SetManualFolderAsync(GameKind game, string? path, CancellationToken ct)
    {
        Folders[game] = path is null ? new(game, null, GameFolderSource.NotFound) : new(game, path, GameFolderSource.Manual);
        return Task.FromResult(GameFolderCheck.Ok);
    }
}

public sealed class DesignMonitor(bool running = false) : IGameStatusMonitor
{
    public bool IsRunning { get; } = running;

    public event EventHandler<bool>? RunningChanged { add { } remove { } }
}

public sealed class DesignLauncher : IShellLauncher
{
    public Task<bool> OpenFolderAsync(string folderPath) => Task.FromResult(true);

    public Task<bool> RevealFileAsync(string filePath) => Task.FromResult(true);
}

public sealed class DesignClipboard : IClipboardService
{
    public Task SetTextAsync(string text) => Task.CompletedTask;
}

public sealed class DesignFolderPicker : IFolderPicker
{
    public Task<string?> PickFolderAsync(TextRef title, string? startPath) => Task.FromResult<string?>(null);
}
