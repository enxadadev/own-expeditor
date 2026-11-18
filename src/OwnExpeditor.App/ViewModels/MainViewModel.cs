using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OwnExpeditor.Core;
using OwnExpeditor.Core.Legacy;
using OwnExpeditor.Domain.Saves;

// LegacyJobs é a fachada transitória do shell antigo (ADR 0006); some junto com este ViewModel na F7.
#pragma warning disable CS0618

namespace OwnExpeditor.App.ViewModels;

public sealed record CargoOption(string Id, string Name)
{
    public override string ToString() => Name;
}

/// <summary>Perfil na lista; o texto exibido é o mesmo de antes (nome + Steam Cloud).</summary>
public sealed record ProfileOption(ProfileInfo Info)
{
    public override string ToString() => Info.IsSteamCloud ? $"{Info.Id} (Steam Cloud)" : Info.Id;
}

public sealed record SaveOption(SaveSlotInfo Info)
{
    public override string ToString() => $"{Info.DisplayName}  [{Info.Id}]";
}

public partial class MainViewModel : ViewModelBase
{
    private LegacyGameData? _gameData;
    private SaveSession? _session;
    private int _injected;

    [ObservableProperty] private string _gameDir = LegacyJobs.DefaultGameDir();
    [ObservableProperty] private string _documentsDir = LegacyJobs.DefaultDocumentsDir();
    [ObservableProperty] private string _status = "Confira as pastas e clique em Carregar.";
    [ObservableProperty] private bool _isError;

    public ObservableCollection<ProfileOption> Profiles { get; } = [];
    public ObservableCollection<SaveOption> Saves { get; } = [];
    public ObservableCollection<CompanyLocation> Origins { get; } = [];
    public ObservableCollection<CargoOption> Cargos { get; } = [];
    public ObservableCollection<CompanyLocation> Destinations { get; } = [];

    [ObservableProperty] private ProfileOption? _selectedProfile;
    [ObservableProperty] private SaveOption? _selectedSave;
    [ObservableProperty] private CompanyLocation? _selectedOrigin;
    [ObservableProperty] private CargoOption? _selectedCargo;
    [ObservableProperty] private CompanyLocation? _selectedDestination;
    [ObservableProperty] private string _distanceKm = "";
    [ObservableProperty] private string _distanceHint = "";
    [ObservableProperty] private int _urgency;
    [ObservableProperty] private string _saveName = "Own Expeditor";
    [ObservableProperty] private string _injectedSummary = "";

    public string[] UrgencyOptions { get; } = ["Padrão", "Importante", "Urgente"];

    [RelayCommand]
    private void Load()
    {
        Run(() =>
        {
            _gameData = LegacyJobs.LoadGameData(GameDir, DocumentsDir);
            Profiles.Clear();
            foreach (var p in LegacyJobs.Profiles(DocumentsDir)) Profiles.Add(new ProfileOption(p));
            SelectedProfile = Profiles.FirstOrDefault();
            return Profiles.Count == 0
                ? throw new IOException($"Nenhum perfil encontrado em '{DocumentsDir}'.")
                : $"{_gameData.CompanyCount} empresas e {_gameData.CargoCount} cargas lidas do jogo. " +
                  (_gameData.TextCount > 0
                      ? $"Textos no idioma {_gameData.TextLanguage}."
                      : "locale.scs não encontrado: nomes de cargas sem tradução.");
        });
    }

    partial void OnSelectedProfileChanged(ProfileOption? value)
    {
        Saves.Clear();
        if (value is null) return;
        if (value.Info.IsSteamCloud)
            Warn("Perfil no Steam Cloud: o jogo não vai mostrar saves novos. Desative o Steam Cloud no perfil (Editar perfil) e use a cópia em 'profiles'.");
        foreach (var s in LegacyJobs.Saves(value.Info)) Saves.Add(new SaveOption(s));
        SelectedSave = Saves.FirstOrDefault();
    }

    partial void OnSelectedSaveChanged(SaveOption? value)
    {
        _session = null;
        _injected = 0;
        InjectedSummary = "";
        Origins.Clear();
        if (value is null || _gameData is null) return;
        Run(() =>
        {
            _session = LegacyJobs.Open(value.Info, _gameData);
            foreach (var o in _session.Origins()) Origins.Add(o);
            SaveName = $"{value.Info.DisplayName} + Own Expeditor";
            return $"Save '{value.Info.DisplayName}' carregado: {Origins.Count} empresas com vaga para frete.";
        });
    }

    partial void OnSelectedOriginChanged(CompanyLocation? value)
    {
        Cargos.Clear();
        if (value is null || _session is null) return;
        foreach (var c in _session.CargosFor(value)
                     .Select(id => new CargoOption(id, _session.CargoName(id)))
                     .OrderBy(c => c.Name, StringComparer.CurrentCulture))
            Cargos.Add(c);
    }

    partial void OnSelectedCargoChanged(CargoOption? value)
    {
        Destinations.Clear();
        if (value is null || SelectedOrigin is null || _session is null) return;
        foreach (var d in _session.Destinations(SelectedOrigin, value.Id)) Destinations.Add(d);
    }

    partial void OnSelectedDestinationChanged(CompanyLocation? value)
    {
        if (value is null || SelectedOrigin is null || _session is null)
        {
            DistanceHint = "";
            return;
        }
        var km = _session.Distance(SelectedOrigin.City, value.City);
        DistanceKm = km?.ToString() ?? "";
        DistanceHint = km is null
            ? "Distância desconhecida no save: informe em km (o jogo usa para pagamento e prazo)."
            : "Distância tirada de outro frete do save.";
    }

    [RelayCommand]
    private void Inject()
    {
        Run(() =>
        {
            if (_session is null || SelectedOrigin is null || SelectedCargo is null || SelectedDestination is null)
                throw new InvalidOperationException("Escolha origem, carga e destino.");
            if (!int.TryParse(DistanceKm, out var km) || km <= 0)
                throw new InvalidOperationException("Informe a distância em km.");

            _session.Inject(SelectedOrigin, SelectedDestination, SelectedCargo.Id, km, Urgency);
            _injected++;
            InjectedSummary += $"• {SelectedOrigin} → {SelectedDestination}: {SelectedCargo}, {km} km\n";

            var origin = SelectedOrigin;
            Origins.Clear();
            foreach (var o in _session.Origins()) Origins.Add(o);
            SelectedOrigin = Origins.Contains(origin) ? origin : null;
            return $"Frete adicionado ({_injected} no total). Clique em Gravar para criar o save.";
        });
    }

    [RelayCommand]
    private void Write()
    {
        Run(() =>
        {
            if (_session is null || SelectedProfile is null || SelectedSave is null || _injected == 0)
                throw new InvalidOperationException("Adicione ao menos um frete antes de gravar.");
            var folder = LegacyJobs.WriteAsNewSave(SelectedSave.Info, _session, SaveName);
            var done = $"Save '{SaveName}' gravado em {folder}. O save original não foi alterado.";
            var profile = SelectedProfile;
            SelectedProfile = null;
            SelectedProfile = profile;
            return done;
        });
    }

    private void Run(Func<string> action)
    {
        try
        {
            Status = action();
            IsError = false;
        }
        catch (Exception e) when (e is IOException or InvalidDataException or NotSupportedException
                                       or InvalidOperationException or JobInjectionException
                                       or UnauthorizedAccessException)
        {
            Status = e.Message;
            IsError = true;
        }
    }

    private void Warn(string message)
    {
        Status = message;
        IsError = true;
    }
}
