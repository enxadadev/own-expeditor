using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OwnExpeditor.App.Services;
using OwnExpeditor.Domain.Games;
using OwnExpeditor.Domain.Saves;

namespace OwnExpeditor.App.ViewModels;

/// <summary>Barra do topo: perfil, save, jogo, estado do jogo e o selo "Revisar (n)" (design/shell, seção 1).</summary>
public sealed partial class SessionBarViewModel : LocalizedViewModel
{
    public SessionBarViewModel(ILocalizer loc, SavePickerViewModel picker) : base(loc)
    {
        Picker = picker;
        ChangeSaveCommand = new RelayCommand(() => IsPickerOpen = !IsPickerOpen);
        ReviewCommand = new RelayCommand(() => ReviewRequested?.Invoke(this, EventArgs.Empty), () => ReviewEnabled);
    }

    public SavePickerViewModel Picker { get; }

    public RelayCommand ChangeSaveCommand { get; }

    public RelayCommand ReviewCommand { get; }

    public event EventHandler? ReviewRequested;

    [ObservableProperty] private SaveSlotInfo? _save;
    [ObservableProperty] private bool _hasError;
    [ObservableProperty] private bool _isPickerOpen;
    [ObservableProperty] private bool _isGameRunning;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(ReviewEnabled), nameof(ReviewText), nameof(ReviewAutomation))] private int _pendingCount;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(ShowReview), nameof(ShowCloud))] private bool _isSessionOpen;
    [ObservableProperty] private bool _isReviewActive;

    public bool HasSave => Save is not null;

    /// <summary>Chip "Steam Cloud" (design/fretes §4.7): sempre visível com a sessão aberta num perfil com Steam Cloud.</summary>
    public bool ShowCloud => IsSessionOpen && Save?.Profile.IsSteamCloud == true;

    public string CloudChip => Loc["Jobs_Cloud_Chip"];

    public string CloudAutomation => Loc["Jobs_Cloud_Title"];

    public bool NoSession => Save is null;

    public string NoSessionText => Loc["SessionBar_NoSession"];

    public string ProfileName => Save?.Profile.DisplayName ?? "";

    public string SaveName => Save?.DisplayName ?? "";

    public string GameBadge => Loc[Save?.Profile.Game == GameKind.Ats ? "Game_Ats_Short" : "Game_Ets2_Short"];

    public string GameName => Loc[Save?.Profile.Game == GameKind.Ats ? "Game_Ats" : "Game_Ets2"];

    public string ProfileAutomation => Loc.Get("SessionBar_ProfileAutomation", ProfileName);

    public string SaveAutomation => Loc.Get("SessionBar_SaveAutomation", SaveName);

    public string GameStatusText => Loc[IsGameRunning ? "SessionBar_GameRunning" : "SessionBar_GameClosed"];

    public string GameStatusHint => Loc[IsGameRunning ? "SessionBar_GameRunningHint" : "SessionBar_GameClosed"];

    /// <summary>O selo e o estado do jogo só existem com a sessão aberta (não durante carga ou erro).</summary>
    public bool ShowReview => IsSessionOpen;

    public bool ReviewEnabled => IsSessionOpen && PendingCount > 0;

    public string ReviewText => PendingCount > 0 ? Loc.Get("SessionBar_Review", PendingCount) : Loc["SessionBar_ReviewNone"];

    public string ReviewAutomation => PendingCount > 0
        ? Loc.Get("SessionBar_ReviewAutomation", Loc.GetPlural("Shell_Pending", PendingCount))
        : Loc["SessionBar_ReviewAutomationNone"];

    public string ReviewHint => Loc["SessionBar_ReviewHint"];

    public string ChangeHint => Loc["SessionBar_ChangeHint"];

    public string ProfileLabel => Loc["SessionBar_Profile"];

    public string SaveLabel => Loc["SessionBar_Save"];

    partial void OnSaveChanged(SaveSlotInfo? value)
    {
        OnPropertyChanged(string.Empty);
        ReviewCommand.NotifyCanExecuteChanged();
    }

    partial void OnPendingCountChanged(int value) => ReviewCommand.NotifyCanExecuteChanged();

    partial void OnIsSessionOpenChanged(bool value) => ReviewCommand.NotifyCanExecuteChanged();

    /// <summary>Ao abrir o seletor relê perfis e saves (o jogo pode ter salvado de novo) e já marca o perfil atual.</summary>
    partial void OnIsPickerOpenChanged(bool value)
    {
        if (value) _ = ReloadPickerAsync();
    }

    private async Task ReloadPickerAsync()
    {
        await Picker.LoadAsync(CancellationToken.None);
        if (Save is { } save) Picker.SelectProfile(save.Profile);
    }

    partial void OnIsGameRunningChanged(bool value) => OnPropertyChanged(string.Empty);
}
