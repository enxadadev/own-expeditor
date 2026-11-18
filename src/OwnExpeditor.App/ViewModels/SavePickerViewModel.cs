using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OwnExpeditor.App.Services;
using OwnExpeditor.Core.Profiles;
using OwnExpeditor.Domain.Games;
using OwnExpeditor.Domain.Saves;

namespace OwnExpeditor.App.ViewModels;

public sealed class ProfileItemViewModel(ILocalizer loc, ProfileInfo info, int? saveCount) : LocalizedViewModel(loc)
{
    public ProfileInfo Info { get; } = info;

    public string Name => Info.DisplayName;

    public string GameBadge => Loc[Info.Game == GameKind.Ats ? "Game_Ats_Short" : "Game_Ets2_Short"];

    public string GameName => Loc[Info.Game == GameKind.Ats ? "Game_Ats" : "Game_Ets2"];

    public string CountText => saveCount is { } n ? Loc.Get("Welcome_ProfileSaveCount", n) : "";
}

public sealed class SaveItemViewModel(ILocalizer loc, SaveSlotInfo slot) : LocalizedViewModel(loc)
{
    public SaveSlotInfo Info { get; } = slot;

    public string Name => Info.DisplayName;

    public string DateText => Info.LastWriteUtc.ToLocalTime().ToString("g", Loc.Culture);

    /// <summary>Automático / Rápido / Manual, pelo nome da pasta do slot ("autosave", "quicksave", "1"…).</summary>
    public string KindText => Loc[Info.Id.StartsWith("quicksave", StringComparison.OrdinalIgnoreCase) ? "SaveKind_Quick"
        : Info.Id.StartsWith("autosave", StringComparison.OrdinalIgnoreCase) ? "SaveKind_Auto" : "SaveKind_Manual"];
}

/// <summary>Perfis e saves em duas listas ("ProfileSavePicker"): na tela inicial e no seletor da barra de sessão.</summary>
public sealed partial class SavePickerViewModel : LocalizedViewModel
{
    private readonly IProfileService _profiles;
    private readonly Dictionary<string, IReadOnlyList<SaveSlotInfo>> _saves = [];
    private CancellationTokenSource? _loading;

    public SavePickerViewModel(ILocalizer loc, IProfileService profiles) : base(loc)
    {
        _profiles = profiles;
        OpenCommand = new RelayCommand(Open, () => SelectedSave is not null);
    }

    public ObservableCollection<ProfileItemViewModel> Profiles { get; } = [];

    public ObservableCollection<SaveItemViewModel> Saves { get; } = [];

    [ObservableProperty] private ProfileItemViewModel? _selectedProfile;
    [ObservableProperty] private SaveItemViewModel? _selectedSave;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(ShowLists))] [NotifyPropertyChangedFor(nameof(ShowNoProfiles))]
    private bool _isLoading;

    /// <summary>Pediu para abrir (botão, Enter ou duplo clique na lista).</summary>
    public event EventHandler<SaveSlotInfo>? OpenRequested;

    public RelayCommand OpenCommand { get; }

    public bool ShowLists => !IsLoading && Profiles.Count > 0;

    public bool ShowNoProfiles => !IsLoading && Profiles.Count == 0;

    public bool ShowNoSaves => SelectedProfile is not null && Saves.Count == 0;

    public string ProfilesLabel => Loc["Welcome_Profiles"].ToUpper(Loc.Culture);

    public string SavesLabelUpper => SavesLabel.ToUpper(Loc.Culture);

    public string SavesLabel => Loc.Get("Welcome_SavesOf", SelectedProfile?.Name ?? "");

    public string Hint => SelectedSave is { } save
        ? Loc.Get("Welcome_PickedHint", SelectedProfile?.Name ?? "", save.Name)
        : Loc["Welcome_PickHint"];

    public SaveSlotInfo? SelectedSlot => SelectedSave?.Info;

    /// <summary>Todos os saves já listados (para achar o "último save").</summary>
    public IEnumerable<SaveSlotInfo> AllSaves => _saves.Values.SelectMany(s => s);

    public async Task LoadAsync(CancellationToken ct)
    {
        _loading?.Cancel();
        var cts = _loading = CancellationTokenSource.CreateLinkedTokenSource(ct);
        IsLoading = true;
        try
        {
            var profiles = await _profiles.GetProfilesAsync(cts.Token);
            _saves.Clear();
            foreach (var p in profiles)
                _saves[p.DirectoryPath] = await TrySavesAsync(p, cts.Token);
            Replace(Profiles, profiles.Select(p => new ProfileItemViewModel(Loc, p, _saves[p.DirectoryPath].Count)));
            SelectedProfile = Profiles.FirstOrDefault();
        }
        catch (OperationCanceledException)
        {
            // recarregou ou o app está fechando: nada a mostrar
        }
        catch (Core.CoreException)
        {
            Replace(Profiles, []);
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(ShowLists));
            OnPropertyChanged(nameof(ShowNoProfiles));
        }
    }

    /// <summary>Seleciona o save (e o perfil dele), se estiver na lista.</summary>
    public bool Select(SaveSlotInfo slot)
    {
        var profile = Profiles.FirstOrDefault(p => p.Info.DirectoryPath == slot.Profile.DirectoryPath);
        if (profile is null) return false;
        SelectedProfile = profile;
        SelectedSave = Saves.FirstOrDefault(s => s.Info.GameSiiPath == slot.GameSiiPath);
        return SelectedSave is not null;
    }

    /// <summary>Marca o perfil (sem escolher save).</summary>
    public void SelectProfile(ProfileInfo profile)
    {
        if (Profiles.FirstOrDefault(p => p.Info.DirectoryPath == profile.DirectoryPath) is { } item) SelectedProfile = item;
    }

    partial void OnSelectedProfileChanged(ProfileItemViewModel? value)
    {
        Replace(Saves, value is null ? [] : (_saves.TryGetValue(value.Info.DirectoryPath, out var list) ? list : [])
            .OrderByDescending(s => s.LastWriteUtc).Select(s => new SaveItemViewModel(Loc, s)));
        SelectedSave = null;
        OnPropertyChanged(nameof(ShowNoSaves));
        OnPropertyChanged(nameof(SavesLabel));
        OnPropertyChanged(nameof(Hint));
    }

    partial void OnSelectedSaveChanged(SaveItemViewModel? value)
    {
        OpenCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(Hint));
        OnPropertyChanged(nameof(SelectedSlot));
    }

    /// <summary>Enter ou duplo clique num save.</summary>
    public void Activate(SaveItemViewModel save)
    {
        SelectedSave = save;
        Open();
    }

    private void Open()
    {
        if (SelectedSave is { } save) OpenRequested?.Invoke(this, save.Info);
    }

    private async Task<IReadOnlyList<SaveSlotInfo>> TrySavesAsync(ProfileInfo profile, CancellationToken ct)
    {
        try
        {
            return await _profiles.GetSavesAsync(profile, ct);
        }
        catch (Core.CoreException)
        {
            return [];
        }
    }

    private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> items) where T : IDisposable
    {
        var old = target.ToList();
        target.Clear();
        foreach (var item in items) target.Add(item);
        foreach (var o in old) o.Dispose();
    }
}
