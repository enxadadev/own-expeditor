using OwnExpeditor.Domain.Games;
using OwnExpeditor.Domain.Saves;
using OwnExpeditor.Sii;

namespace OwnExpeditor.Data;

/// <summary>Save de um perfil no formato legado (pasta/slot/nome). Substituído por <see cref="SaveSlotInfo"/> em F3.</summary>
public sealed record SaveInfo(string Folder, string Slot, string Name)
{
    public string GameSii => System.IO.Path.Combine(Folder, "game.sii");
    public override string ToString() => $"{Name}  [{Slot}]";
}

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

    public static IReadOnlyList<SaveInfo> Saves(ProfileInfo profile)
    {
        var saveDir = Path.Combine(profile.DirectoryPath, "save");
        return Directory.EnumerateDirectories(saveDir)
            .Where(d => File.Exists(Path.Combine(d, "game.sii")))
            .Select(d => new SaveInfo(d, Path.GetFileName(d), ReadName(d) ?? Path.GetFileName(d)))
            .OrderByDescending(s => File.GetLastWriteTimeUtc(s.GameSii))
            .ToList();
    }

    /// <summary>Saves do perfil como <see cref="SaveSlotInfo"/>, do mais recente ao mais antigo.</summary>
    public static IReadOnlyList<SaveSlotInfo> Slots(ProfileInfo profile) =>
        Saves(profile).Select(s =>
        {
            var file = new FileInfo(s.GameSii);
            return new SaveSlotInfo(profile, s.Slot, s.Name, s.GameSii, file.LastWriteTimeUtc, file.Length);
        }).ToList();

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

    /// <summary>Grava o save editado numa pasta nova, com info.sii renomeado. Não toca no original.</summary>
    public static string WriteAsNewSave(ProfileInfo profile, SaveInfo source, SiiDocument game, string name)
    {
        var target = NextSlot(profile);
        Directory.CreateDirectory(target);
        game.Save(Path.Combine(target, "game.sii"));

        var infoPath = Path.Combine(source.Folder, "info.sii");
        if (File.Exists(infoPath))
        {
            var info = SiiDocument.Load(infoPath);
            var unit = info.Units.FirstOrDefault();
            if (unit?.Get("name") is not null) unit.Set("name", $"\"{name.Replace("\"", "")}\"");
            info.Save(Path.Combine(target, "info.sii"));
        }
        return target;
    }
}
