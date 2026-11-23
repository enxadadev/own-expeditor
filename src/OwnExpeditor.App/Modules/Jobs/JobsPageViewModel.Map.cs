using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OwnExpeditor.App.Controls;
using OwnExpeditor.App.Services;
using OwnExpeditor.Core.Maps;
using OwnExpeditor.Core;
using OwnExpeditor.Core.Session;
using OwnExpeditor.Domain.Games;
using OwnExpeditor.Domain.Maps;

namespace OwnExpeditor.App.Modules.Jobs;

/// <summary>
/// Mapa do jogo no cartão de rota (ADR 0007, fase 1a): o índice é carregado em segundo plano e nunca bloqueia a tela;
/// enquanto não chega, ou se faltar posição de alguma ponta, o mapa fica esquemático (o <c>Auto</c> do <see cref="RouteMap"/>).
/// O fundo são só as cidades do save que têm posição; nada de lista fixa.
/// </summary>
public sealed partial class JobsPageViewModel
{
    private readonly IGameMapProvider? _maps;
    private CancellationTokenSource? _mapCts;
    private Dictionary<string, MapCity>? _mapCities;
    private IReadOnlyDictionary<string, MapCountry> _mapCountries = new Dictionary<string, MapCountry>();
    private MapStatus? _mapStatus;
    private bool _mapLoading;

    private object? _backdropBoard;
    private string? _backdropCulture;
    private IReadOnlyList<MapPoint> _backdropCities = [];
    private IReadOnlyList<MapPoint> _backdropCountries = [];

    /// <summary>Índice do mapa sendo montado ou lido (só informativo; a tela nunca espera por ele).</summary>
    public bool IsMapLoading => _mapLoading;

    /// <summary>Cidades do save (com posição no mapa do jogo) para o fundo; vazio sem índice.</summary>
    public IReadOnlyList<MapPoint> MapCities
    {
        get
        {
            EnsureBackdrop();
            return _backdropCities;
        }
    }

    /// <summary>Rótulos dos países das cidades do fundo.</summary>
    public IReadOnlyList<MapPoint> MapCountries
    {
        get
        {
            EnsureBackdrop();
            return _backdropCountries;
        }
    }

    /// <summary>
    /// Nota discreta por status do mapa; no modo esquemático (par escolhido sem posição) cai no aviso de posições.
    /// Nula quando o mapa está pronto e geográfico, ou sem save aberto.
    /// </summary>
    public string? MapStatusNote
    {
        get
        {
            if (_board is null) return null;
            if (_maps is not null)
            {
                if (_mapLoading) return Loc["Map_Building"];
                switch (_mapStatus)
                {
                    case MapStatus.NoGameFolder: return Loc["Map_Hint_NoGame"];
                    case MapStatus.UnsupportedVersion: return Loc["Map_Error_Version"];
                    case MapStatus.Failed: return Loc["Map_Error_Read"];
                }
            }
            return IsMapSchematic ? Loc["Jobs_Map_Schematic"] : null;
        }
    }

    /// <summary>Ícone da nota: erro em vermelho, "sem pasta" em cinza, demais sem ícone.</summary>
    public MapNoteKind MapNoteKind => _maps is null || _mapLoading ? MapNoteKind.Info : _mapStatus switch
    {
        MapStatus.UnsupportedVersion or MapStatus.Failed => MapNoteKind.Error,
        MapStatus.NoGameFolder => MapNoteKind.Hint,
        _ => MapNoteKind.Info,
    };

    /// <summary>Par escolhido, mas sem posição real em alguma ponta: o mapa cai no esquema.</summary>
    private bool IsMapSchematic => HasBothCities && !(MapOrigin?.HasCoordinates == true && MapDestination?.HasCoordinates == true);

    private MapPoint MapPointOf(string city)
    {
        var name = CityName(city);
        return _mapCities is not null && _mapCities.TryGetValue(city, out var c)
            ? new MapPoint(name, c.Center.X, -c.Center.Z, c.Population)
            : new MapPoint(name);
    }

    private void EnsureBackdrop()
    {
        if (_board is null || _mapCities is null)
        {
            _backdropBoard = null;
            _backdropCities = _backdropCountries = [];
            return;
        }
        if (ReferenceEquals(_backdropBoard, _board) && _backdropCulture == Loc.Culture.Name) return;
        _backdropBoard = _board;
        _backdropCulture = Loc.Culture.Name;

        var tokens = _board.DepartureCities(null).Select(c => c.City).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var cities = new List<MapPoint>();
        var countries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var token in tokens)
        {
            if (!_mapCities.TryGetValue(token, out var c)) continue;
            cities.Add(MapPointOf(token));
            if (!string.IsNullOrEmpty(c.Country)) countries.Add(c.Country);
        }
        _backdropCities = cities;
        _backdropCountries = [.. countries.Select(t => _mapCountries.GetValueOrDefault(t)).OfType<MapCountry>()
            .Select(c => new MapPoint(CountryLabel(c), c.Pos.X, -c.Pos.Z))];
    }

    private string CountryLabel(MapCountry c)
    {
        var name = _board!.Names.Get(GameNameKind.Country, c.Token);
        return name != c.Token || string.IsNullOrEmpty(c.Name) ? name : c.Name;
    }

    private void StartMapLoad(IEditSession session)
    {
        CancelMapLoad();
        _mapCities = null;
        _mapCountries = new Dictionary<string, MapCountry>();
        _mapStatus = null;
        _backdropBoard = null;
        if (_maps is null) return;

        var cts = _mapCts = new CancellationTokenSource();
        var ct = cts.Token;
        var game = session.Game;
        var profile = session.Save.Profile; // lidos na thread de UI, antes do Task.Run
        _mapLoading = true;
        _ = RunAsync();

        async Task RunAsync()
        {
            GameMapResult? result = null;
            try
            {
                result = await Task.Run(() => _maps.LoadCitiesAsync(game, profile, null, ct), ct); // sem progresso: a nota "Preparando…" basta
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception e) when (e is not OutOfMemoryException)
            {
                result = new GameMapResult(MapStatus.Failed, null); // o mapa é conforto: nunca derruba a tela
            }
            _ui.Post(() =>
            {
                if (cts.IsCancellationRequested || !ReferenceEquals(_mapCts, cts)) return;
                ApplyMap(result);
            });
        }
    }

    private void ApplyMap(GameMapResult result)
    {
        _mapLoading = false;
        _mapStatus = result.Status;
        if (result.Status is MapStatus.Ready or MapStatus.PartiallyRead && result.Index is { } index)
        {
            _mapCities = index.Cities.GroupBy(c => c.Token, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
            _mapCountries = index.Countries.GroupBy(c => c.Token, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        }
        _backdropBoard = null;
        Notify();
    }

    private void CancelMapLoad()
    {
        var cts = _mapCts;
        _mapCts = null;
        _mapLoading = false;
        if (cts is null) return;
        cts.Cancel();
        cts.Dispose();
    }
}
