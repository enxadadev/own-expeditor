using OwnExpeditor.Sii;

namespace OwnExpeditor.Sii.Tests;

/// <summary>
/// Testes com saves reais em samples/ (fora do git). Sem saves, os testes não fazem nada.
/// </summary>
public class RealSaveTests
{
    public static IEnumerable<object[]> Saves()
    {
        var dir = FindSamplesDir();
        if (dir is null) yield break;
        foreach (var f in Directory.EnumerateFiles(dir, "game.sii", SearchOption.AllDirectories))
            yield return [Path.GetRelativePath(dir, f)];
    }

    [Theory]
    [MemberData(nameof(Saves))]
    public void Real_save_decodes_parses_and_roundtrips(string relativePath)
    {
        var path = Path.Combine(FindSamplesDir()!, relativePath);
        var text = SiiDecoder.DecodeToText(File.ReadAllBytes(path));
        var doc = SiiDocument.Parse(text);

        Assert.NotEmpty(doc.OfType("company"));
        Assert.NotEmpty(doc.OfType("job_offer_data"));
        Assert.Equal(text, doc.ToText());
    }

    private static string? FindSamplesDir()
    {
        for (var d = new DirectoryInfo(AppContext.BaseDirectory); d is not null; d = d.Parent)
        {
            var candidate = Path.Combine(d.FullName, "samples");
            if (Directory.Exists(candidate)) return candidate;
        }
        return null;
    }
}
