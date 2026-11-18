using System.Text;
using OwnExpeditor.Data.Scs;
using OwnExpeditor.Sii;

namespace OwnExpeditor.Data;

/// <summary>
/// Textos traduzidos do jogo: locale/&lt;idioma&gt;/local*.sii (pares key[]/val[]),
/// referenciados nas definições como "@@chave@@".
/// </summary>
public sealed class Localization
{
    private readonly Dictionary<string, string> _strings = new(StringComparer.Ordinal);

    public string? Language { get; private set; }
    public int Count => _strings.Count;

    /// <summary>Texto de uma referência @@chave@@; null se não houver tradução.</summary>
    public string? Resolve(string? text)
    {
        if (string.IsNullOrEmpty(text)) return null;
        text = text.Trim('"');
        if (text.StartsWith("@@") && text.EndsWith("@@") && text.Length > 4)
            return _strings.GetValueOrDefault(text[2..^2]);
        return text;
    }

    /// <summary>Idioma configurado no jogo (config.cfg: uset g_lang_init "pt_br").</summary>
    public static string? GameLanguage(string documentsDir)
    {
        var cfg = Path.Combine(documentsDir, "config.cfg");
        if (!File.Exists(cfg)) return null;
        foreach (var line in File.ReadLines(cfg))
        {
            var parts = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (parts is ["uset", "g_lang_init", var value, ..])
                return value.Trim('"') is { Length: > 0 } lang ? lang : null;
        }
        return null;
    }

    /// <summary>
    /// Lê as traduções de um idioma em todos os pacotes que tiverem a pasta locale.
    /// Se o idioma não existir, tenta en_us e en_gb.
    /// </summary>
    public static Localization Load(IEnumerable<HashFsArchive> archives, string? language)
    {
        var loc = new Localization();
        var list = archives.ToList();
        foreach (var lang in new[] { language, "en_us", "en_gb" }.OfType<string>().Distinct())
        {
            foreach (var scs in list)
                loc.AddFrom(scs, $"locale/{lang}");
            if (loc.Count > 0)
            {
                loc.Language = lang;
                break;
            }
        }
        return loc;
    }

    private void AddFrom(HashFsArchive scs, string dir)
    {
        foreach (var file in scs.List(dir).Files
                     .Where(f => f.StartsWith("local", StringComparison.Ordinal) && f.EndsWith(".sii", StringComparison.Ordinal))
                     .Order(StringComparer.Ordinal))
            AddText(scs, dir, file, depth: 0);
    }

    private void AddText(HashFsArchive scs, string dir, string file, int depth)
    {
        var data = scs.Read($"{dir}/{file}");
        if (data is null || depth > 4) return;
        Parse(SiiDecoder.DecodeLenient(data), include => AddText(scs, dir, include, depth + 1));
    }

    /// <summary>Traduções a partir do texto de um local.sii (sem @include). Útil em testes.</summary>
    public static Localization FromText(string text, string language = "test")
    {
        var loc = new Localization { Language = language };
        loc.Parse(text, _ => { });
        return loc;
    }

    private void Parse(string text, Action<string> include)
    {
        string? key = null;
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.Trim();
            if (line.StartsWith("@include", StringComparison.Ordinal))
            {
                include(Unquote(line["@include".Length..].Trim()));
                continue;
            }
            if (line.StartsWith("key[", StringComparison.Ordinal) && Value(line) is { } k) key = k;
            else if (line.StartsWith("val[", StringComparison.Ordinal) && Value(line) is { } v && key is not null)
            {
                _strings[key] = v;
                key = null;
            }
        }
    }

    private static string? Value(string line)
    {
        var sep = line.IndexOf(':');
        return sep < 0 ? null : Unquote(line[(sep + 1)..].Trim());
    }

    private static string Unquote(string s)
    {
        if (s.Length < 2 || s[0] != '"' || s[^1] != '"') return s;
        // \xNN são bytes UTF-8: junta os bytes e decodifica no fim.
        var bytes = new List<byte>(s.Length);
        Span<byte> buf = stackalloc byte[4];
        for (var i = 1; i < s.Length - 1; i++)
        {
            char c = s[i];
            if (c == '\\' && i + 1 < s.Length - 1)
            {
                var n = s[++i];
                if (n == 'x' && i + 2 < s.Length - 1 &&
                    byte.TryParse(s.AsSpan(i + 1, 2), System.Globalization.NumberStyles.HexNumber, null, out var b))
                {
                    bytes.Add(b);
                    i += 2;
                    continue;
                }
                c = n switch { 'n' => '\n', 't' => '\t', _ => n };
            }
            var len = Encoding.UTF8.GetBytes([c], buf);
            for (var k = 0; k < len; k++) bytes.Add(buf[k]);
        }
        return Encoding.UTF8.GetString(bytes.ToArray());
    }

    /// <summary>Nome legível a partir de um id. Delega a <see cref="Domain.Games.GameIds.Humanize"/>.</summary>
    public static string Humanize(string id) => Domain.Games.GameIds.Humanize(id);
}
