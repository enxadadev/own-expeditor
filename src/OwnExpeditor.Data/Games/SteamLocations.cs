using System.Runtime.Versioning;
using Microsoft.Win32;
using OwnExpeditor.Domain.Games;

namespace OwnExpeditor.Data.Games;

/// <summary>Nomes e pastas conhecidos de cada jogo e da Steam (raízes, bibliotecas).</summary>
internal static class SteamLocations
{
    public static string FolderName(GameKind game) => game switch
    {
        GameKind.Ets2 => "Euro Truck Simulator 2",
        GameKind.Ats => "American Truck Simulator",
        _ => throw new ArgumentOutOfRangeException(nameof(game), game, null),
    };

    public static int AppId(GameKind game) => game switch
    {
        GameKind.Ets2 => 227300,
        GameKind.Ats => 270880,
        _ => throw new ArgumentOutOfRangeException(nameof(game), game, null),
    };

    /// <summary>Nome do executável sem extensão (<c>eurotrucks2</c> / <c>amtrucks</c>).</summary>
    public static string ProcessName(GameKind game) => game switch
    {
        GameKind.Ets2 => "eurotrucks2",
        GameKind.Ats => "amtrucks",
        _ => throw new ArgumentOutOfRangeException(nameof(game), game, null),
    };

    public static string Home(DataOptions options) =>
        options.HomeDirectory ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    /// <summary>
    /// Raízes da Steam existentes: as de <see cref="DataOptions.SteamRoots"/> ou, sem elas, registro (Windows),
    /// <c>~/.steam/steam</c>, <c>~/.local/share/Steam</c>, Flatpak e pastas padrão do Windows.
    /// </summary>
    public static IReadOnlyList<string> Roots(DataOptions options)
    {
        IEnumerable<string> candidates = options.SteamRoots ?? Detected(options);
        return Distinct(candidates.Where(Directory.Exists).Select(Resolve));
    }

    /// <summary>As raízes mais as bibliotecas de <c>libraryfolders.vdf</c>.</summary>
    public static IReadOnlyList<string> Libraries(IEnumerable<string> roots)
    {
        var all = new List<string>();
        foreach (var root in roots)
        {
            all.Add(root);
            foreach (var vdf in new[]
                     {
                         Path.Combine(root, "steamapps", "libraryfolders.vdf"),
                         Path.Combine(root, "config", "libraryfolders.vdf"),
                     })
            {
                var text = ReadOrNull(vdf);
                if (text is null) continue;
                all.AddRange(SteamVdf.LibraryPaths(text).Where(Directory.Exists));
            }
        }
        return Distinct(all.Select(Resolve));
    }

    public static IReadOnlyList<string> Distinct(IEnumerable<string> paths)
    {
        var seen = new HashSet<string>(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
        var result = new List<string>();
        foreach (var p in paths)
        {
            var full = Path.TrimEndingDirectorySeparator(Path.GetFullPath(p));
            if (seen.Add(full)) result.Add(full);
        }
        return result;
    }

    public static string? ReadOrNull(string path)
    {
        try
        {
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static string Resolve(string path)
    {
        try
        {
            return new DirectoryInfo(path).ResolveLinkTarget(returnFinalTarget: true)?.FullName ?? path;
        }
        catch (IOException)
        {
            return path;
        }
    }

    private static IEnumerable<string> Detected(DataOptions options)
    {
        if (options.UseRegistry && OperatingSystem.IsWindows())
            foreach (var r in RegistryRoots()) yield return r;

        var home = Home(options);
        if (home.Length > 0)
        {
            yield return Path.Combine(home, ".steam", "steam");
            yield return Path.Combine(home, ".steam", "root");
            yield return Path.Combine(home, ".local", "share", "Steam");
            var flatpak = Path.Combine(home, ".var", "app", "com.valvesoftware.Steam");
            yield return Path.Combine(flatpak, ".local", "share", "Steam");
            yield return Path.Combine(flatpak, ".steam", "steam");
        }

        foreach (var pf in new[] { Environment.SpecialFolder.ProgramFilesX86, Environment.SpecialFolder.ProgramFiles })
        {
            var dir = Environment.GetFolderPath(pf);
            if (dir.Length > 0) yield return Path.Combine(dir, "Steam");
        }
    }

    [SupportedOSPlatform("windows")]
    private static IEnumerable<string> RegistryRoots()
    {
        var found = new List<string>();
        try
        {
            using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam"))
                if (key?.GetValue("SteamPath") is string p && p.Length > 0) found.Add(p);
            using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam"))
                if (key?.GetValue("InstallPath") is string p && p.Length > 0) found.Add(p);
        }
        catch (Exception e) when (e is System.Security.SecurityException or UnauthorizedAccessException or IOException)
        {
            // sem acesso ao registro: segue com as pastas padrão
        }
        return found;
    }
}
