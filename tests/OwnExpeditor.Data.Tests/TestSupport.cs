using System.Security.Cryptography;
using System.Text;
using OwnExpeditor.Domain.Games;
using OwnExpeditor.Domain.Saves;

namespace OwnExpeditor.Data.Tests;

/// <summary>Diretório temporário único, apagado ao final (permissões restauradas antes).</summary>
public sealed class TempDir : IDisposable
{
    public string Path { get; } = Directory.CreateTempSubdirectory("ownexp-data-").FullName;

    public string Combine(params string[] parts) => System.IO.Path.Combine([Path, .. parts]);

    public string Dir(params string[] parts)
    {
        var p = Combine(parts);
        Directory.CreateDirectory(p);
        return p;
    }

    public string File(string content, params string[] parts)
    {
        var p = Combine(parts);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(p)!);
        System.IO.File.WriteAllText(p, content, new UTF8Encoding(false));
        return p;
    }

    public void Dispose()
    {
        if (!OperatingSystem.IsWindows())
            foreach (var d in Directory.EnumerateDirectories(Path, "*", SearchOption.AllDirectories).Prepend(Path))
                System.IO.File.SetUnixFileMode(d, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        Directory.Delete(Path, recursive: true);
    }
}

/// <summary>Relógio fixo; hora local = UTC para o nome do backup ser previsível.</summary>
public sealed class FixedTime(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
}

public static class Sii
{
    public const string Game =
        "SiiNunit\r\n{\r\neconomy : _nameless.1 {\r\n bank: _nameless.2\r\n}\r\n\r\n}\r\n";

    public const string GameEdited =
        "SiiNunit\r\n{\r\neconomy : _nameless.1 {\r\n bank: _nameless.9\r\n}\r\n\r\n}\r\n";

    public static string Info(string name) =>
        $"SiiNunit\r\n{{\r\nsave_container : _nameless.0 {{\r\n name: \"{name}\"\r\n time: 100\r\n}}\r\n\r\n}}\r\n";

    public static string Sha256(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));

    public static string Hex(string name) => Convert.ToHexString(Encoding.UTF8.GetBytes(name));
}

public static class Layout
{
    /// <summary>Cria perfil com saves {slot: (game.sii, nome do info.sii)} e devolve o ProfileInfo.</summary>
    public static ProfileInfo Profile(TempDir t, string docs, string sub, string id, GameKind game,
        params (string Slot, string Name)[] saves)
    {
        var dir = t.Dir(docs, sub, id);
        t.Dir(docs, sub, id, "save");
        foreach (var (slot, name) in saves)
        {
            t.File(Sii.Game, docs, sub, id, "save", slot, "game.sii");
            t.File(Sii.Info(name), docs, sub, id, "save", slot, "info.sii");
        }
        return new ProfileInfo(game, id, ProfileNames.FromFolderName(id), dir, sub == "steam_profiles");
    }
}
