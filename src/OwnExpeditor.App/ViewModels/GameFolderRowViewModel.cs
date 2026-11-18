using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OwnExpeditor.App.Services;
using OwnExpeditor.Core;
using OwnExpeditor.Core.Games;
using OwnExpeditor.Core.Text;
using OwnExpeditor.Domain.Games;

namespace OwnExpeditor.App.ViewModels;

/// <summary>Uma linha "Pasta de instalação do jogo" (FolderField): detectada, escolhida, não encontrada ou inválida.</summary>
public sealed partial class GameFolderRowViewModel : LocalizedViewModel
{
    private readonly GameKind _game;
    private readonly IGameDataService _data;
    private readonly IFolderPicker _picker;
    private readonly IShellLauncher _launcher;
    private GameFolderStatus _status;

    public GameFolderRowViewModel(ILocalizer loc, GameKind game, IGameDataService data, IFolderPicker picker, IShellLauncher launcher)
        : base(loc)
    {
        _game = game;
        _data = data;
        _picker = picker;
        _launcher = launcher;
        _status = new GameFolderStatus(game, null, GameFolderSource.NotFound);
    }

    [ObservableProperty] private bool _showInvalid;

    public GameKind Game => _game;

    public string GameName => Loc[_game == GameKind.Ats ? "Game_Ats" : "Game_Ets2"];

    public string? Path => _status.Path;

    public string PathText => _status.Path ?? "—";

    public GameFolderSource Source => _status.Source;

    public bool IsFound => _status.Source != GameFolderSource.NotFound;

    public bool IsManual => _status.Source == GameFolderSource.Manual;

    public bool CanOpen => _status.Path is not null;

    public string StatusText => Loc[_status.Source switch
    {
        GameFolderSource.Manual => "Settings_GameFolder_Manual",
        GameFolderSource.Detected => "Settings_GameFolder_Auto",
        _ => "Settings_GameFolder_NotFound",
    }];

    public string NotFoundHelp => Loc.Get("Settings_GameFolder_NotFoundHelp", GameName);

    public string InvalidText => Loc.Get("Settings_GameFolder_Invalid", GameName);

    public string AutomationName => Loc.Get("Settings_GameFolder_Automation", GameName, _status.Path ?? StatusText);

    /// <summary>Relê o estado atual (detectada, manual ou não encontrada).</summary>
    public void Refresh()
    {
        _status = _data.GetFolder(_game);
        OnPropertyChanged(string.Empty);
    }

    [RelayCommand]
    private async Task ChooseAsync(CancellationToken ct)
    {
        var picked = await _picker.PickFolderAsync(TextRef.Of("Settings_GameFolder"), _status.Path);
        if (picked is null) return;
        await SetAsync(picked, ct);
    }

    [RelayCommand]
    private Task ResetAsync(CancellationToken ct) => SetAsync(null, ct);

    [RelayCommand]
    private async Task OpenFolderAsync()
    {
        if (_status.Path is { } path) await _launcher.OpenFolderAsync(path);
    }

    private async Task SetAsync(string? path, CancellationToken ct)
    {
        try
        {
            var check = await _data.SetManualFolderAsync(_game, path, ct);
            ShowInvalid = check != GameFolderCheck.Ok; // inválida: nada muda, a anterior continua valendo
        }
        catch (CoreException)
        {
            ShowInvalid = true;
        }
        Refresh();
    }
}
