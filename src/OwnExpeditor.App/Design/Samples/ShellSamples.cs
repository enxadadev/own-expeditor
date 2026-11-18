using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using OwnExpeditor.App.Services;
using OwnExpeditor.App.Shell;
using OwnExpeditor.App.ViewModels;
using OwnExpeditor.Core;
using OwnExpeditor.Core.Games;
using OwnExpeditor.Core.Profiles;
using OwnExpeditor.Core.Session;
using OwnExpeditor.Core.Settings;
using OwnExpeditor.Domain.Games;
using OwnExpeditor.Domain.Saves;
using OwnExpeditor.Domain.Settings;

namespace OwnExpeditor.App.Design.Samples;

/// <summary>Amostras do shell: cada uma monta o <c>ShellViewModel</c> real com serviços falsos e o leva ao estado.</summary>
internal static class ShellSamples
{
    public static IReadOnlyList<DesignSample> All() =>
    [
        new("Shell_Welcome", () => Welcome()),
        new("Shell_WelcomeLast", () => Welcome(last: true)),
        new("Shell_WelcomeEmpty", () => Welcome(noProfiles: true)),
        new("Shell_Loading", Loading),
        new("Shell_ErrorParse", () => OpenError(CoreError.SaveParseFailed)),
        new("Shell_ErrorCorrupt", () => OpenError(CoreError.SaveCorrupt, line: 18204)),
        new("Shell_ErrorNotFound", () => OpenError(CoreError.SaveNotFound)),
        new("Shell_Module", () => Open()),
        new("Shell_ModuleGameRunning", () => Open(gameRunning: true)),
        new("Shell_Review", () => Review()),
        new("Shell_ReviewEmpty", () => Review(discardAll: true)),
        new("Shell_Confirm", () => Confirm()),
        new("Shell_ConfirmNew", () => Confirm(cloud: true, newSlot: true)),
        new("Shell_ConfirmInvalidName", () => Confirm(newSlot: true, name: "")),
        new("Shell_ConfirmApplying", () => Confirm(apply: Never)),
        new("Shell_ConfirmError", () => Confirm(apply: Fail(CoreError.GameRunning))),
        new("Shell_Result", () => Result()),
        new("Shell_ResultNew", () => Result(cloud: true, newSlot: true)),
        new("Shell_Settings", Settings),
        new("Shell_SwitchSave", SwitchSave),
        new("Shell_SavePicker", SavePicker),
    ];

    internal static ShellViewModel Boot(DesignHost host, bool noProfiles = false, string? lastSavePath = null, bool running = false,
        Action<IServiceCollection>? extra = null)
    {
        var language = Localizer.Instance.Language;
        var profiles = noProfiles ? [] : new[] { DesignData.ProfileA(), DesignData.ProfileB, DesignData.ProfileAts };
        var provider = AppComposition.Build(s =>
        {
            s.AddSingleton<IUiDispatcher, ImmediateUiDispatcher>();
            s.AddSingleton<IEditSessionHost>(host);
            s.AddSingleton<IProfileService>(new DesignProfiles(profiles));
            s.AddSingleton<IAppSettingsService>(new DesignSettings(new AppSettings(ThemePreference.System, language, lastSavePath, new GameFolders())));
            s.AddSingleton<IGameDataService>(new DesignGameData());
            s.AddSingleton<IGameStatusMonitor>(new DesignMonitor(running));
            s.AddSingleton<IShellLauncher>(new DesignLauncher());
            s.AddSingleton<IClipboardService>(new DesignClipboard());
            s.AddSingleton<IFolderPicker>(new DesignFolderPicker());
            extra?.Invoke(s);
        });
        return provider.GetRequiredService<ShellViewModel>();
    }

    private static T Wait<T>(Task<T> task) => task.GetAwaiter().GetResult();

    private static ShellViewModel Welcome(bool last = false, bool noProfiles = false)
    {
        var save = DesignData.Save4();
        var shell = Boot(new DesignHost(), noProfiles, last ? save.GameSiiPath : null);
        shell.Welcome.ActivateAsync(CancellationToken.None).GetAwaiter().GetResult();
        if (last) shell.Welcome.Picker.Select(save);
        return shell;
    }

    private static ShellViewModel Loading()
    {
        var host = new DesignHost
        {
            Open = async (save, progress, ct) =>
            {
                progress?.Report(new LoadProgress(62, LoadStages.Parse, 90_600_000));
                await Task.Delay(Timeout.Infinite, ct);
                return null!;
            },
        };
        var shell = Boot(host);
        shell.Loading.ShowDelay = TimeSpan.Zero;
        _ = shell.OpenSaveAsync(DesignData.Save4());
        return shell;
    }

    private static ShellViewModel OpenError(CoreError code, int? line = null)
    {
        var host = new DesignHost
        {
            Open = (_, _, _) => throw new CoreException(code, "demo") { Line = line, StageKey = LoadStages.Parse },
        };
        var shell = Boot(host);
        Wait(shell.OpenSaveAsync(DesignData.Save4()).ContinueWith(_ => 0));
        return shell;
    }

    private static Func<ApplyTarget, CancellationToken, Task<ApplyResult>> Succeed(SaveSlotInfo save) => (target, _) =>
    {
        var folder = System.IO.Path.GetDirectoryName(save.GameSiiPath)!;
        return Task.FromResult(target is ApplyTarget.NewSlot n
            ? new ApplyResult(DesignData.Slot(save.Profile, "7", n.DisplayName, 0), null, 3, 7)
            : new ApplyResult(save, $"{folder}/game.sii.bak-20261117-143022", 3, 7));
    };

    private static Func<ApplyTarget, CancellationToken, Task<ApplyResult>> Never => async (_, ct) =>
    {
        await Task.Delay(Timeout.Infinite, ct);
        return null!;
    };

    private static Func<ApplyTarget, CancellationToken, Task<ApplyResult>> Fail(CoreError code) =>
        (_, _) => throw new CoreException(code, "O jogo está aberto; feche-o antes de gravar.");

    private static ShellViewModel Open(bool gameRunning = false, bool cloud = false,
        Func<ApplyTarget, CancellationToken, Task<ApplyResult>>? apply = null)
    {
        var save = DesignData.Save4(cloud);
        var host = new DesignHost
        {
            Open = (s, _, _) => Task.FromResult<IEditSession>(new DesignSession(DesignData.SessionWithChanges(s), apply ?? Succeed(s))),
        };
        var shell = Boot(host, running: gameRunning);
        Wait(shell.OpenSaveAsync(save).ContinueWith(_ => 0));
        return shell;
    }

    private static ShellViewModel Review(bool discardAll = false)
    {
        var shell = Open();
        shell.ReviewCommand.Execute(null);
        if (discardAll) shell.Session!.DiscardAll();
        return shell;
    }

    private static ShellViewModel Confirm(bool cloud = false, bool newSlot = false, string? name = null,
        Func<ApplyTarget, CancellationToken, Task<ApplyResult>>? apply = null)
    {
        var shell = Open(cloud: cloud, apply: apply);
        shell.ReviewCommand.Execute(null);
        _ = shell.Review.RequestApplyAsync();
        if (shell.Dialog is ConfirmApplyViewModel dialog)
        {
            if (newSlot) dialog.IsNewSlot = true;
            if (name is not null) dialog.NewName = name;
            if (apply is not null) dialog.ConfirmCommand.Execute(null);
        }
        return shell;
    }

    private static ShellViewModel Result(bool cloud = false, bool newSlot = false)
    {
        var shell = Open(cloud: cloud);
        shell.ReviewCommand.Execute(null);
        _ = shell.Review.RequestApplyAsync();
        var dialog = (ConfirmApplyViewModel)shell.Dialog!;
        if (newSlot) dialog.IsNewSlot = true;
        dialog.ConfirmCommand.Execute(null);
        return shell;
    }

    private static ShellViewModel Settings()
    {
        var shell = Open();
        shell.OpenSettingsCommand.Execute(null);
        return shell;
    }

    private static ShellViewModel SavePicker()
    {
        var shell = Open();
        shell.OpenSaveCommand.Execute(null);
        return shell;
    }

    private static ShellViewModel SwitchSave()
    {
        var shell = Open();
        _ = shell.OpenSaveAsync(DesignData.Slot(DesignData.ProfileB, "1", "Save 1", 300));
        return shell;
    }
}
