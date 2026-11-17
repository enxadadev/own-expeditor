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
        Assert.Equal("Kaarfor", cat.CompanyName("kaarfor"));
        Assert.Equal("Budapest", cat.CityName("budapest"));
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

public class LocalizationTests
{
    private const string Local =
        "SiiNunit\r\n{\r\nlocalization_db : .localization\r\n{\r\n" +
        " key[]: \"cn_plumbing_supplies\"\r\n val[]: \"Materiais hidr\\xc3\\xa1ulicos\"\r\n" +
        " key[]: \"budapest\"\r\n val[]: \"Budapeste\"\r\n" +
        "}\r\n}\r\n";

    [Fact]
    public void Resolves_keys_and_decodes_utf8_escapes()
    {
        var loc = Localization.FromText(Local);
        Assert.Equal("Materiais hidráulicos", loc.Resolve("@@cn_plumbing_supplies@@"));
        Assert.Equal("Budapeste", loc.Resolve("\"@@budapest@@\""));
        Assert.Null(loc.Resolve("@@desconhecida@@"));
        Assert.Equal("Texto fixo", loc.Resolve("Texto fixo"));
    }

    [Fact]
    public void Catalog_names_prefer_translation_then_native_then_id()
    {
        var cat = new GameCatalog();
        cat.Cities["budapest"] = new CityDef("budapest") { NativeName = "Budapest", LocalizationKey = "@@budapest@@" };
        cat.Cities["wien"] = new CityDef("wien") { NativeName = "Wien", LocalizationKey = "@@wien@@" };
        cat.Cargos["plumb_suppl"] = new CargoDef("plumb_suppl") { LocalizationKey = "@@cn_plumbing_supplies@@" };
        typeof(GameCatalog).GetProperty(nameof(GameCatalog.Texts))!.SetValue(cat, Localization.FromText(Local));

        Assert.Equal("Budapeste", cat.CityName("budapest"));
        Assert.Equal("Wien", cat.CityName("wien"));
        Assert.Equal("Materiais hidráulicos", cat.CargoName("plumb_suppl"));
        Assert.Equal("Used plast", cat.CargoName("used_plast"));
    }

    [Fact]
    public void ThreeNK_decodes_with_seed()
    {
        var plain = "SiiNunit"u8.ToArray();
        byte seed = 7;
        var data = new byte[6 + plain.Length];
        "3nK\u0001"u8.CopyTo(data);
        data[5] = seed;
        for (var j = 0; j < plain.Length; j++)
        {
            var b = (byte)((seed + j) & 0xFF);
            data[6 + j] = (byte)(plain[j] ^ (byte)((((byte)(b << 2) ^ (byte)~b) << 3) ^ b));
        }
        Assert.Equal(plain, OwnExpeditor.Sii.SiiDecoder.Decode3nK(data));
    }
}
