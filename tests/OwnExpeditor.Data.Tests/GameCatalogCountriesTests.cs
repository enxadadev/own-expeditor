namespace OwnExpeditor.Data.Tests;

/// <summary>Usa os .scs reais em dataset/scs (fora do git). Sem eles, os testes não fazem nada.</summary>
public class GameCatalogCountriesTests
{
    private static GameCatalog? LoadReal()
    {
        for (var d = new DirectoryInfo(AppContext.BaseDirectory); d is not null; d = d.Parent)
        {
            var dir = Path.Combine(d.FullName, "dataset", "scs");
            if (File.Exists(Path.Combine(dir, "def.scs"))) return GameCatalog.LoadFromGameDir(dir);
        }
        return null;
    }

    [Fact]
    public void Countries_AreReadFromDef()
    {
        if (LoadReal() is not { } cat) return;

        Assert.True(cat.Countries.Count >= 30);
        var hu = cat.Countries["hungary"];
        Assert.Equal("hungary", hu.Name);
        Assert.Equal("Magyarország", hu.NativeName);
        Assert.Equal("@@hungary@@", hu.LocalizationKey);
        Assert.Equal("United Kingdom", cat.CountryName("uk"));
    }

    [Fact]
    public void CityCountry_MatchesACountryKey()
    {
        if (LoadReal() is not { } cat) return;

        Assert.Equal("hungary", cat.Cities["budapest"].Country);
        Assert.All(cat.Cities.Values.Where(c => c.Country is not null),
            c => Assert.True(cat.Countries.ContainsKey(c.Country!), $"{c.Name} -> {c.Country}"));
    }
}
