using OwnExpeditor.Data;
using OwnExpeditor.Data.Scs;

namespace OwnExpeditor.Core.Tests;

public class ScsTests
{
    // Vetores do teste de referência (scs_tools src/cityhash.rs).
    [Theory]
    [InlineData("", 0x9ae16a3b2f90404fUL)]
    [InlineData("a", 0x2420662cd003acfaUL)]
    [InlineData("abc", 0x3a912f483a4ece31UL)]
    [InlineData("def/city.sii", 0x9a5cb1ebaac98229UL)]
    [InlineData("version.txt", 0x9df28354d1b80f29UL)]
    [InlineData("xxxxxxxxxxxxxxxxx", 0xccde5b4c17d97655UL)]
    [InlineData("xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx", 0x83d2f4e954e9c837UL)]
    [InlineData("xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx", 0x3613c0a0c0ee3c93UL)]
    [InlineData("yyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyy", 0x07a9c72386a250d6UL)]
    [InlineData("automat/01/0123456789abcdef.dds", 0xf567a715b38ab762UL)]
    public void CityHash64_matches_reference(string input, ulong expected)
    {
        Assert.Equal(expected, CityHash.Hash64(input));
    }

    /// <summary>Usa os .scs reais em dataset/scs (fora do git). Sem eles, não faz nada.</summary>
    [Fact]
    public void Real_game_catalog_has_companies_and_cargo()
    {
        var dir = FindDir("dataset/scs");
        if (dir is null || !File.Exists(Path.Combine(dir, "def.scs"))) return;

        var cat = GameCatalog.LoadFromGameDir(dir);

        Assert.True(cat.Companies.Count > 200);
        Assert.True(cat.Cargos.Count > 200);
        Assert.True(cat.Ships("kaarfor", "used_plast"));
        Assert.True(cat.Accepts("dfh", "used_plast"));
        Assert.Contains("budapest", cat.Companies["kaarfor"].Cities);
        Assert.Contains("dryvan", cat.Cargos["plumb_suppl"].BodyTypes);
    }

    private static string? FindDir(string relative)
    {
        for (var d = new DirectoryInfo(AppContext.BaseDirectory); d is not null; d = d.Parent)
        {
            var candidate = Path.Combine(d.FullName, relative);
            if (Directory.Exists(candidate)) return candidate;
        }
        return null;
    }
}
