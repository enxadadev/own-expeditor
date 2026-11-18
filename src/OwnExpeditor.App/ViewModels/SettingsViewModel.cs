using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using OwnExpeditor.App.Services;
using OwnExpeditor.Core.Games;
using OwnExpeditor.Core.Settings;
using OwnExpeditor.Domain.Games;
using OwnExpeditor.Domain.Settings;

namespace OwnExpeditor.App.ViewModels;

public enum SettingsStatus { None, Saved, Failed }

/// <summary>
/// Configurações: tema, idioma e pasta de instalação por jogo (design/shell, seção 6). Sem botão Salvar: cada escolha
/// vale na hora e é persistida; se a gravação falhar a troca continua valendo até fechar o app.
/// </summary>
public sealed partial class SettingsViewModel : LocalizedViewModel
{
    private static readonly TimeSpan SavedLifetime = TimeSpan.FromSeconds(3);

    private readonly IAppSettingsService _settings;
    private readonly IThemeService _theme;
    private readonly IUiDispatcher _ui;
    private readonly TimeProvider _time;
    private ITimer? _savedTimer;
    private bool _syncing;

    public SettingsViewModel(ILocalizer loc, IAppSettingsService settings, IThemeService theme, IGameDataService data,
        IFolderPicker picker, IShellLauncher launcher, IUiDispatcher ui, TimeProvider time) : base(loc)
    {
        _settings = settings;
        _theme = theme;
        _ui = ui;
        _time = time;
        ThemeOptions =
        [
            new(loc, ThemePreference.Light, "Settings_Theme_Light"),
            new(loc, ThemePreference.Dark, "Settings_Theme_Dark"),
            new(loc, ThemePreference.System, "Settings_Theme_System"),
        ];
        LanguageOptions =
        [
            new(loc, Language.PtBr, "Settings_Language_PtBr"),
            new(loc, Language.En, "Settings_Language_En"),
        ];
        Folders = [.. Enum.GetValues<GameKind>().Select(g => new GameFolderRowViewModel(loc, g, data, picker, launcher))];
        Sync();
    }

    public IReadOnlyList<ChoiceItemViewModel> ThemeOptions { get; }

    public IReadOnlyList<ChoiceItemViewModel> LanguageOptions { get; }

    public IReadOnlyList<GameFolderRowViewModel> Folders { get; }

    [ObservableProperty] private ChoiceItemViewModel? _selectedTheme;
    [ObservableProperty] private ChoiceItemViewModel? _selectedLanguage;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(ShowSaved), nameof(ShowFailed))] private SettingsStatus _status;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(ShowPendingNote))] private int _pendingCount;

    public bool ShowSaved => Status == SettingsStatus.Saved;

    public bool ShowFailed => Status == SettingsStatus.Failed;

    public bool ShowPendingNote => PendingCount > 0;

    public string Version => Loc.Get("Settings_Version", AppInfo.Version);

    /// <summary>Lê o estado atual (valores salvos e pastas dos jogos).</summary>
    public void Refresh()
    {
        Sync();
        foreach (var f in Folders) f.Refresh();
    }

    private void Sync()
    {
        _syncing = true;
        SelectedTheme = ThemeOptions.First(o => (ThemePreference)o.Value == _settings.Current.Theme);
        SelectedLanguage = LanguageOptions.First(o => (Language)o.Value == _settings.Current.Language);
        _syncing = false;
    }

    partial void OnSelectedThemeChanged(ChoiceItemViewModel? value)
    {
        if (_syncing || value is null) return;
        var theme = (ThemePreference)value.Value;
        _theme.Apply(theme);
        _ = PersistAsync(s => s with { Theme = theme });
    }

    partial void OnSelectedLanguageChanged(ChoiceItemViewModel? value)
    {
        if (_syncing || value is null) return;
        var language = (Language)value.Value;
        Loc.SetLanguage(language);
        _ = PersistAsync(s => s with { Language = language });
    }

    private async Task PersistAsync(Func<AppSettings, AppSettings> change)
    {
        try
        {
            await _settings.UpdateAsync(change(_settings.Current), CancellationToken.None);
        }
        catch (Exception e) when (e is not OutOfMemoryException)
        {
            // a troca continua valendo na sessão; só avisa que não ficou salva
            _ui.Post(() => Status = SettingsStatus.Failed);
            return;
        }
        _ui.Post(() =>
        {
            Status = SettingsStatus.Saved;
            _savedTimer?.Dispose();
            _savedTimer = _time.CreateTimer(_ => _ui.Post(() => { if (Status == SettingsStatus.Saved) Status = SettingsStatus.None; }),
                null, SavedLifetime, Timeout.InfiniteTimeSpan);
        });
    }

    protected override void OnDisposed() => _savedTimer?.Dispose();
}
