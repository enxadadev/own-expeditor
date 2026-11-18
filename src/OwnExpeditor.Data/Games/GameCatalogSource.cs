using OwnExpeditor.Data.Scs;
using OwnExpeditor.Domain.Saves;

namespace OwnExpeditor.Data.Games;

/// <summary>Cache por (pasta, nome/tamanho/data dos .scs): relê só se algum arquivo mudou.</summary>
internal sealed class GameCatalogSource(
    Func<IReadOnlyList<string>, GameCatalog>? loadDefinitions = null,
    Func<IReadOnlyList<string>, string?, Localization>? loadTexts = null) : IGameCatalogSource
{
    private readonly Func<IReadOnlyList<string>, GameCatalog> _loadDefinitions = loadDefinitions ?? LoadDefinitionsFromDisk;
    private readonly Func<IReadOnlyList<string>, string?, Localization> _loadTexts = loadTexts ?? LoadTextsFromDisk;
    private readonly object _gate = new();
    private readonly Dictionary<string, (string Signature, GameCatalog Value)> _definitions = [];
    private readonly Dictionary<string, (string Signature, Localization Value)> _texts = [];

    public GameCatalog LoadDefinitions(string installDir)
    {
        var files = ArchiveFiles(installDir, includeLocale: false);
        if (files.Count == 0) throw new FileNotFoundException($"Nenhum def.scs/dlc_*.scs em '{installDir}'.");
        var key = Path.GetFullPath(installDir);
        var signature = Signature(files);
        lock (_gate)
        {
            if (_definitions.TryGetValue(key, out var hit) && hit.Signature == signature) return hit.Value;
            var catalog = _loadDefinitions(files);
            _definitions[key] = (signature, catalog);
            return catalog;
        }
    }

    public Localization LoadTexts(string installDir, IReadOnlyList<string> languages)
    {
        ArgumentNullException.ThrowIfNull(languages);
        var files = ArchiveFiles(installDir, includeLocale: true);
        if (files.Count == 0) return new Localization();
        var key = Path.GetFullPath(installDir) + "|" + string.Join(",", languages);
        var signature = Signature(files);
        lock (_gate)
        {
            if (_texts.TryGetValue(key, out var hit) && hit.Signature == signature) return hit.Value;
            var texts = Pick(files, languages);
            _texts[key] = (signature, texts);
            return texts;
        }
    }

    public string? GameLanguage(ProfileInfo profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        try
        {
            var profiles = Directory.GetParent(profile.DirectoryPath);
            foreach (var dir in new[] { profile.DirectoryPath, profiles?.Parent?.FullName })
                if (dir is not null && Localization.GameLanguage(dir) is { } language) return language;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // config.cfg ilegível: idioma desconhecido
        }
        return null;
    }

    /// <summary>Primeiro idioma que o jogo realmente tem; senão o primeiro texto não vazio (fallback en_us/en_gb).</summary>
    private Localization Pick(IReadOnlyList<string> files, IReadOnlyList<string> languages)
    {
        if (languages.Count == 0) return _loadTexts(files, null);
        Localization? fallback = null, last = null;
        foreach (var language in languages)
        {
            last = _loadTexts(files, language);
            if (last.Count > 0 && last.Language == language) return last;
            if (last.Count > 0) fallback ??= last;
        }
        return fallback ?? last!;
    }

    private static List<string> ArchiveFiles(string installDir, bool includeLocale)
    {
        if (!Directory.Exists(installDir)) throw new DirectoryNotFoundException($"Pasta do jogo não encontrada: '{installDir}'.");
        var files = new List<string>();
        var def = Path.Combine(installDir, "def.scs");
        if (File.Exists(def)) files.Add(def);
        files.AddRange(Directory.EnumerateFiles(installDir, "dlc_*.scs").Order(StringComparer.Ordinal));
        var locale = Path.Combine(installDir, "locale.scs");
        if (includeLocale && File.Exists(locale)) files.Add(locale);
        return files;
    }

    private static string Signature(IEnumerable<string> files) =>
        string.Join(";", files.Select(f =>
        {
            var info = new FileInfo(f);
            return $"{info.Name}|{info.Length}|{info.LastWriteTimeUtc.Ticks}";
        }));

    private static GameCatalog LoadDefinitionsFromDisk(IReadOnlyList<string> files) => GameCatalog.LoadDefinitions(files);

    private static Localization LoadTextsFromDisk(IReadOnlyList<string> files, string? language)
    {
        var opened = new List<HashFsArchive>();
        try
        {
            foreach (var f in files) opened.Add(HashFsArchive.Open(f));
            return Localization.Load(opened, language);
        }
        finally
        {
            foreach (var a in opened) a.Dispose();
        }
    }
}
