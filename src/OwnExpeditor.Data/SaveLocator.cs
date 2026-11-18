using OwnExpeditor.Domain.Games;
using OwnExpeditor.Domain.Saves;
using OwnExpeditor.Sii;

namespace OwnExpeditor.Data;

/// <summary>Encontra pastas do jogo, perfis e saves no disco.</summary>
public static class SaveLocator
{
    public static string DefaultDocumentsDir() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Euro Truck Simulator 2");

    public static string DefaultGameDir() =>
        @"C:\Program Files (x86)\Steam\steamapps\common\Euro Truck Simulator 2";

    public static IReadOnlyList<ProfileInfo> Profiles(string documentsDir, GameKind game = GameKind.Ets2)
    {
        var result = new List<ProfileInfo>();
        foreach (var (sub, cloud) in new[] { ("profiles", false), ("steam_profiles", true) })
        {
            var dir = Path.Combine(documentsDir, sub);
            if (!Directory.Exists(dir)) continue;
            foreach (var p in Directory.EnumerateDirectories(dir))
            {
                if (!Directory.Exists(Path.Combine(p, "save"))) continue;
                var id = Path.GetFileName(p);
                result.Add(new ProfileInfo(game, id, ProfileNames.FromFolderName(id), p, cloud));
            }
        }
        return result;
    }

    /// <summary>Saves do perfil como <see cref="SaveSlotInfo"/>, do mais recente ao mais antigo.</summary>
    public static IReadOnlyList<SaveSlotInfo> Slots(ProfileInfo profile)
    {
        var saveDir = Path.Combine(profile.DirectoryPath, "save");
        return Directory.EnumerateDirectories(saveDir)
            .Select(d => (Dir: d, Game: Path.Combine(d, "game.sii")))
            .Where(x => File.Exists(x.Game))
            .Select(x =>
            {
                var slot = Path.GetFileName(x.Dir);
                var file = new FileInfo(x.Game);
                return new SaveSlotInfo(profile, slot, ReadName(x.Dir) ?? slot, x.Game, file.LastWriteTimeUtc, file.Length);
            })
            .OrderByDescending(s => s.LastWriteUtc)
            .ToList();
    }

    /// <summary>Próxima pasta numérica livre em save/ (ex.: "8").</summary>
    public static string NextSlot(ProfileInfo profile)
    {
        var saveDir = Path.Combine(profile.DirectoryPath, "save");
        var used = Directory.EnumerateDirectories(saveDir)
            .Select(Path.GetFileName)
            .Select(n => int.TryParse(n, out var i) ? i : 0);
        return Path.Combine(saveDir, (used.DefaultIfEmpty(0).Max() + 1).ToString());
    }

    private static string? ReadName(string folder)
    {
        try
        {
            var info = Path.Combine(folder, "info.sii");
            if (!File.Exists(info)) return null;
            var doc = SiiDocument.Load(info);
            return doc.Units.FirstOrDefault()?.Get("name")?.Trim('"');
        }
        catch (Exception e) when (e is SiiFormatException or InvalidDataException or NotSupportedException or IOException)
        {
            return null;
        }
    }
}
