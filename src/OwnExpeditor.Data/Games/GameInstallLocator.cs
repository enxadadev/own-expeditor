using OwnExpeditor.Domain.Games;

namespace OwnExpeditor.Data.Games;

internal sealed class GameInstallLocator(DataOptions? options = null) : IGameInstallLocator
{
    private readonly DataOptions _options = options ?? new DataOptions();

    public IReadOnlyList<string> Detect(GameKind game)
    {
        var candidates = new List<string>();
        foreach (var library in SteamLocations.Libraries(SteamLocations.Roots(_options)))
        {
            var common = Path.Combine(library, "steamapps", "common");
            var manifest = SteamLocations.ReadOrNull(
                Path.Combine(library, "steamapps", $"appmanifest_{SteamLocations.AppId(game)}.acf"));
            if (manifest is not null && SteamVdf.InstallDir(manifest) is { } installDir)
                candidates.Add(Path.Combine(common, installDir));
            candidates.Add(Path.Combine(common, SteamLocations.FolderName(game)));
        }
        return SteamLocations.Distinct(candidates.Where(c => Validate(game, c) == GameFolderCheck.Ok));
    }

    public GameFolderCheck Validate(GameKind game, string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path)) return GameFolderCheck.NotFound;
        try
        {
            if (!File.Exists(Path.Combine(path, "base.scs")) || !File.Exists(Path.Combine(path, "def.scs")))
                return GameFolderCheck.NotAGameFolder;

            if (HasExecutable(path, game)) return GameFolderCheck.Ok;
            var other = game == GameKind.Ets2 ? GameKind.Ats : GameKind.Ets2;
            return HasExecutable(path, other) ? GameFolderCheck.WrongGame : GameFolderCheck.NotAGameFolder;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return GameFolderCheck.NotAGameFolder;
        }
    }

    private static bool HasExecutable(string installDir, GameKind game)
    {
        var bin = Path.Combine(installDir, "bin");
        if (!Directory.Exists(bin)) return false;
        var name = SteamLocations.ProcessName(game);
        foreach (var dir in Directory.EnumerateDirectories(bin).Prepend(bin))
            if (File.Exists(Path.Combine(dir, name + ".exe")) || File.Exists(Path.Combine(dir, name)))
                return true;
        return false;
    }
}
