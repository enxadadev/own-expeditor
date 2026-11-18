using OwnExpeditor.Data.Games;
using OwnExpeditor.Domain.Games;
using OwnExpeditor.Domain.Saves;

namespace OwnExpeditor.Data.Saves;

/// <summary>Procura perfis em Documentos (Windows), <c>~/.local/share</c> (Linux nativo) e no prefixo Proton.</summary>
internal sealed class ProfileLocator(DataOptions? options = null) : IProfileLocator
{
    private readonly DataOptions _options = options ?? new DataOptions();

    public IReadOnlyList<ProfileInfo> FindProfiles()
    {
        var result = new List<ProfileInfo>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var game in Enum.GetValues<GameKind>())
        foreach (var docs in DocumentRoots(game))
        foreach (var profile in SaveLocator.Profiles(docs, game))
            if (seen.Add(Path.GetFullPath(profile.DirectoryPath))) result.Add(profile);
        return result;
    }

    public IReadOnlyList<SaveSlotInfo> FindSaves(ProfileInfo profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        return Directory.Exists(Path.Combine(profile.DirectoryPath, "save")) ? SaveLocator.Slots(profile) : [];
    }

    private IReadOnlyList<string> DocumentRoots(GameKind game)
    {
        var name = SteamLocations.FolderName(game);
        var roots = new List<string>();
        if (_options.DocumentsRoot is { } docs) roots.Add(Path.Combine(docs, name));
        else
        {
            roots.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), name));
            var home = SteamLocations.Home(_options);
            if (home.Length > 0) roots.Add(Path.Combine(home, ".local", "share", name));
        }

        var steamOptions = _options.SteamRoots is null && _options.DocumentsRoot is not null
            ? _options with { SteamRoots = [] }
            : _options;
        foreach (var library in SteamLocations.Libraries(SteamLocations.Roots(steamOptions)))
            roots.Add(Path.Combine(library, "steamapps", "compatdata", SteamLocations.AppId(game).ToString(),
                "pfx", "drive_c", "users", "steamuser", "Documents", name));
        return SteamLocations.Distinct(roots.Where(Directory.Exists));
    }
}
