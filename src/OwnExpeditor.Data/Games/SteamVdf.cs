using System.Text;

namespace OwnExpeditor.Data.Games;

/// <summary>Leitor mínimo do formato VDF/ACF da Steam (<c>libraryfolders.vdf</c>, <c>appmanifest_*.acf</c>).</summary>
internal static class SteamVdf
{
    /// <summary>Pastas de biblioteca listadas em <c>libraryfolders.vdf</c> (formato novo e antigo). Texto inválido → vazio.</summary>
    public static IReadOnlyList<string> LibraryPaths(string vdfText)
    {
        var root = TryParse(vdfText);
        var folders = root?.Values.OfType<Node>().FirstOrDefault();
        if (folders is null) return [];
        var paths = new List<string>();
        foreach (var (key, value) in folders)
        {
            if (!key.All(char.IsAsciiDigit)) continue;
            if (value is string old && old.Length > 0) paths.Add(old);
            else if (value is Node n && n.GetValueOrDefault("path") is string p && p.Length > 0) paths.Add(p);
        }
        return paths;
    }

    /// <summary>Campo <c>installdir</c> de um <c>appmanifest_*.acf</c>; null se ausente ou inválido.</summary>
    public static string? InstallDir(string acfText)
    {
        var state = TryParse(acfText)?.Values.OfType<Node>().FirstOrDefault();
        return state?.GetValueOrDefault("installdir") as string is { Length: > 0 } dir ? dir : null;
    }

    private sealed class Node : Dictionary<string, object>
    {
        public Node() : base(StringComparer.OrdinalIgnoreCase) { }
    }

    private static Node? TryParse(string text)
    {
        try
        {
            var tokens = Tokenize(text);
            var i = 0;
            return ParseBody(tokens, ref i, topLevel: true);
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private static Node ParseBody(List<string?> tokens, ref int i, bool topLevel)
    {
        var node = new Node();
        while (true)
        {
            if (i >= tokens.Count)
            {
                if (topLevel) return node;
                throw new FormatException("bloco não fechado");
            }
            var key = tokens[i++];
            if (key is null) // '}'
            {
                if (topLevel) throw new FormatException("'}' sobrando");
                return node;
            }
            if (i >= tokens.Count) throw new FormatException("valor ausente");
            var next = tokens[i];
            if (next is null) throw new FormatException("valor esperado");
            if (next == "{")
            {
                i++;
                node[key] = ParseBody(tokens, ref i, topLevel: false);
            }
            else
            {
                i++;
                node[key] = next;
            }
        }
    }

    // Tokens: texto entre aspas (sem as aspas), "{" literal como "{", e null para "}".
    // Uma string "{" entre aspas vira o mesmo token; isso não ocorre em arquivos reais da Steam.
    private static List<string?> Tokenize(string text)
    {
        var tokens = new List<string?>();
        var i = 0;
        while (i < text.Length)
        {
            var c = text[i];
            if (char.IsWhiteSpace(c)) { i++; continue; }
            if (c == '/' && i + 1 < text.Length && text[i + 1] == '/')
            {
                while (i < text.Length && text[i] != '\n') i++;
                continue;
            }
            if (c == '{') { tokens.Add("{"); i++; continue; }
            if (c == '}') { tokens.Add(null); i++; continue; }
            if (c != '"') throw new FormatException($"caractere inesperado '{c}'");
            i++;
            var sb = new StringBuilder();
            while (true)
            {
                if (i >= text.Length) throw new FormatException("texto sem fechar");
                var d = text[i++];
                if (d == '"') break;
                if (d == '\\' && i < text.Length)
                {
                    var e = text[i++];
                    sb.Append(e switch { 'n' => '\n', 't' => '\t', _ => e });
                }
                else sb.Append(d);
            }
            tokens.Add(sb.ToString());
        }
        return tokens;
    }
}
