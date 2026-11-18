using System.Text.RegularExpressions;

namespace OwnExpeditor.App.Tests;

/// <summary>Toda chave de texto usada literalmente no código do App existe nos .resx (nenhum "!Chave!" na tela).</summary>
public class SourceKeysTests
{
    private static readonly Regex[] Patterns =
    [
        new(@"\{loc:Tr\s+([A-Za-z][A-Za-z0-9_]*)\}", RegexOptions.Compiled),
        new(@"(?:Localizer\.Instance|_loc|loc|\bL)\[""([A-Za-z][A-Za-z0-9_]*)""\]", RegexOptions.Compiled),
        new(@"\.Get\(""([A-Za-z][A-Za-z0-9_]*)""", RegexOptions.Compiled),
        new(@"\bL\(""([A-Za-z][A-Za-z0-9_]*)""\)", RegexOptions.Compiled),
    ];

    private static readonly Regex PluralCall = new(@"GetPlural\(""([A-Za-z][A-Za-z0-9_]*)""", RegexOptions.Compiled);

    private static string SourceDir()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "OwnExpeditor.sln"))) dir = dir.Parent;
        return Path.Combine(dir!.FullName, "src", "OwnExpeditor.App");
    }

    private static IEnumerable<string> SourceFiles() =>
        Directory.EnumerateFiles(SourceDir(), "*.*", SearchOption.AllDirectories)
            .Where(f => (f.EndsWith(".axaml") || f.EndsWith(".cs")) && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                        && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"));

    /// <summary>Texto do arquivo sem linhas de comentário (///, //, XML comments de uma linha).</summary>
    private static string Code(string file) =>
        string.Join('\n', File.ReadAllLines(file).Where(l => !l.TrimStart().StartsWith("//") && !l.TrimStart().StartsWith("<!--")));

    private static HashSet<string> AllKeys() =>
        ResxFiles.Bundles.SelectMany(b => ResxFiles.Read(b, null).Keys).ToHashSet();

    [Fact]
    public void Keys_UsedInSource_ExistInResx()
    {
        var keys = AllKeys();

        var missing = SourceFiles()
            .SelectMany(f => Patterns.SelectMany(p => p.Matches(Code(f)).Select(m => $"{Path.GetFileName(f)}: {m.Groups[1].Value}")))
            .Where(entry => !keys.Contains(entry[(entry.IndexOf(": ", StringComparison.Ordinal) + 2)..]))
            .Distinct();

        Assert.Empty(missing);
    }

    [Fact]
    public void PluralKeys_UsedInSource_HaveOneAndOther()
    {
        var keys = AllKeys();

        var missing = SourceFiles()
            .SelectMany(f => PluralCall.Matches(Code(f)).Select(m => m.Groups[1].Value))
            .Distinct()
            .Where(b => !keys.Contains(b + "_One") || !keys.Contains(b + "_Other"));

        Assert.Empty(missing);
    }

    [Fact]
    public void Source_Axaml_HasNoLiteralColors()
    {
        var offenders = SourceFiles()
            .Where(f => f.EndsWith(".axaml") && !f.EndsWith("Colors.axaml") && !f.EndsWith("Tokens.axaml"))
            .Where(f => Regex.IsMatch(Code(f), @"(Background|Foreground|Fill|Stroke|BorderBrush)=""#[0-9A-Fa-f]{3,8}"""))
            .Select(Path.GetFileName);

        Assert.Empty(offenders);
    }
}
