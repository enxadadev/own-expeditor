using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using OwnExpeditor.App.Services;
using OwnExpeditor.Core.Settings;
using OwnExpeditor.Domain.Saves;

namespace OwnExpeditor.App.ViewModels;

/// <summary>Sem sessão: "Abra um save" (design/shell, tela 2). Nada abre sozinho; o último save só é oferecido.</summary>
public sealed class WelcomeViewModel : LocalizedViewModel
{
    private readonly IAppSettingsService _settings;
    private SaveSlotInfo? _last;

    public WelcomeViewModel(ILocalizer loc, SavePickerViewModel picker, IAppSettingsService settings) : base(loc)
    {
        Picker = picker;
        _settings = settings;
        ReopenLastCommand = new RelayCommand(() => { if (_last is { } l) OpenRequested?.Invoke(this, l); }, () => _last is not null);
    }

    public SavePickerViewModel Picker { get; }

    public RelayCommand ReopenLastCommand { get; }

    /// <summary>Último save conhecido ainda existente em disco (de <c>LastSavePath</c>).</summary>
    public SaveSlotInfo? LastSave
    {
        get => _last;
        private set
        {
            if (SetProperty(ref _last, value))
            {
                OnPropertyChanged(nameof(HasLastSave));
                OnPropertyChanged(nameof(LastSaveText));
                ReopenLastCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public bool HasLastSave => _last is not null;

    public string LastSaveText => _last is null ? "" : Loc.Get("Welcome_LastSave", _last.Profile.DisplayName, _last.DisplayName,
        _last.LastWriteUtc.ToLocalTime().ToString("g", Loc.Culture));

    public string Title => Loc["Welcome_Title"];

    public string TitleUpper => Title.ToUpper(Loc.Culture);

    public string ContinueLabel => Loc["Welcome_Continue"].ToUpper(Loc.Culture);

    public event EventHandler<SaveSlotInfo>? OpenRequested;

    /// <summary>Procura perfis e saves; não abre nada.</summary>
    public async Task ActivateAsync(CancellationToken ct)
    {
        await Picker.LoadAsync(ct);
        var path = _settings.Current.LastSavePath;
        LastSave = string.IsNullOrEmpty(path) ? null
            : Picker.AllSaves.FirstOrDefault(s => string.Equals(s.GameSiiPath, path, StringComparison.OrdinalIgnoreCase));
    }
}
