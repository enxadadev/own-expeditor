using OwnExpeditor.Core.Settings;
using OwnExpeditor.Data;
using OwnExpeditor.Data.Games;
using OwnExpeditor.Domain.Games;
using OwnExpeditor.Domain.Settings;

namespace OwnExpeditor.Core.Games;

/// <summary>
/// Dados do jogo por <see cref="GameKind"/>, com cache. Pasta: manual válida → detectada → nenhuma.
/// Idioma dos nomes = idioma do app (pt-BR → pt_br; en → en_us, en_gb); trocar o idioma só relê os textos.
/// </summary>
internal sealed class GameDataService : IGameDataService
{
    private readonly IGameInstallLocator _locator;
    private readonly IGameCatalogSource _source;
    private readonly IAppSettingsService _settings;
    private readonly object _gate = new();
    private readonly Dictionary<GameKind, Task<IGameData>> _loaded = [];
    private readonly Dictionary<GameKind, IReadOnlyList<string>> _detected = [];
    private AppSettings _last;

    public GameDataService(IGameInstallLocator locator, IGameCatalogSource source, IAppSettingsService settings)
    {
        _locator = locator;
        _source = source;
        _settings = settings;
        _last = settings.Current;
        settings.Changed += OnSettingsChanged;
    }

    public event EventHandler<GameKind>? Changed;

    public async Task<IGameData> GetAsync(GameKind game, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        Task<IGameData> task;
        lock (_gate)
        {
            if (!_loaded.TryGetValue(game, out task!))
                _loaded[game] = task = Task.Run(() => Load(game, _settings.Current.Language));
        }
        // A carga é compartilhada: cancelar quem pediu não cancela quem espera pelo mesmo resultado.
        return await task.WaitAsync(ct).ConfigureAwait(false);
    }

    public GameFolderStatus GetFolder(GameKind game)
    {
        var manual = _settings.Current.GameFolders.For(game);
        if (manual is not null && _locator.Validate(game, manual) == GameFolderCheck.Ok)
            return new GameFolderStatus(game, manual, GameFolderSource.Manual);
        var detected = Detected(game).FirstOrDefault();
        return detected is null
            ? new GameFolderStatus(game, null, GameFolderSource.NotFound)
            : new GameFolderStatus(game, detected, GameFolderSource.Detected);
    }

    public async Task<GameFolderCheck> SetManualFolderAsync(GameKind game, string? path, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (path is not null)
        {
            var check = await Task.Run(() => _locator.Validate(game, path), ct).ConfigureAwait(false);
            if (check != GameFolderCheck.Ok) return check; // pasta inválida nunca chega às configurações
        }
        var current = _settings.Current;
        await _settings.UpdateAsync(current with { GameFolders = current.GameFolders.With(game, path) }, ct).ConfigureAwait(false);
        return GameFolderCheck.Ok;
    }

    private IReadOnlyList<string> Detected(GameKind game)
    {
        lock (_gate)
        {
            if (_detected.TryGetValue(game, out var hit)) return hit;
            return _detected[game] = _locator.Detect(game);
        }
    }

    private IGameData Load(GameKind game, Language language)
    {
        var folder = GetFolder(game);
        if (folder.Path is null) return new UnavailableGameData(game);
        try
        {
            var catalog = _source.LoadDefinitions(folder.Path);
            if (catalog.Companies.Count == 0) return new UnavailableGameData(game);
            return new GameData(game, catalog, LoadTexts(folder.Path, language));
        }
        catch (Exception)
        {
            // O catálogo nunca derruba a sessão (risco 5): .scs ilegível, pasta sumida, formato novo → dados indisponíveis.
            return new UnavailableGameData(game);
        }
    }

    private Localization LoadTexts(string folder, Language language)
    {
        try
        {
            return _source.LoadTexts(folder, Languages(language));
        }
        catch (Exception)
        {
            // Sem textos traduzidos os nomes caem no nome local / id legível.
            return new Localization();
        }
    }

    private static IReadOnlyList<string> Languages(Language language) =>
        language == Language.PtBr ? ["pt_br"] : ["en_us", "en_gb"];

    private void OnSettingsChanged(object? sender, AppSettings settings)
    {
        var folderChanged = new List<GameKind>();
        var retexted = new List<(GameKind Game, Task<IGameData> Task)>();
        lock (_gate)
        {
            var previous = _last;
            _last = settings;
            foreach (var game in Enum.GetValues<GameKind>())
            {
                if (previous.GameFolders.For(game) == settings.GameFolders.For(game)) continue;
                _loaded.Remove(game);
                _detected.Remove(game);
                folderChanged.Add(game);
            }

            if (previous.Language != settings.Language)
                foreach (var (game, task) in _loaded.Where(l => !folderChanged.Contains(l.Key)).ToList())
                {
                    var language = settings.Language;
                    var reloaded = Task.Run(async () => await task.ConfigureAwait(false) is GameData data
                        ? Retext(game, data, language)
                        : await task.ConfigureAwait(false));
                    _loaded[game] = reloaded;
                    retexted.Add((game, reloaded));
                }
        }

        foreach (var game in folderChanged) Changed?.Invoke(this, game);
        foreach (var (game, task) in retexted) _ = NotifyWhenDone(game, task);
    }

    private IGameData Retext(GameKind game, GameData data, Language language)
    {
        var folder = GetFolder(game);
        return folder.Path is null ? data : data.WithTexts(LoadTexts(folder.Path, language));
    }

    private async Task NotifyWhenDone(GameKind game, Task task)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch (Exception)
        {
            return; // Retext/Load não lançam; se algo escapar, mantém os dados anteriores sem avisar a UI
        }
        Changed?.Invoke(this, game);
    }
}
