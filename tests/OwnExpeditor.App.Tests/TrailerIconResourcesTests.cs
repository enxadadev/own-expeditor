using System.Xml.Linq;
using OwnExpeditor.App.Services;

namespace OwnExpeditor.App.Tests;

/// <summary>Toda chave que o resolvedor pode devolver existe como StreamGeometry em Icons.axaml (design/nomes-reboques §7.9).</summary>
public class TrailerIconResourcesTests
{
    private static readonly XNamespace Xaml = "http://schemas.microsoft.com/winfx/2006/xaml";

    private static string IconsPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "src", "OwnExpeditor.App", "Styles", "Icons.axaml")))
            dir = dir.Parent;
        return Path.Combine(dir!.FullName, "src", "OwnExpeditor.App", "Styles", "Icons.axaml");
    }

    private static Dictionary<string, string> Geometries() =>
        XDocument.Load(IconsPath()).Root!.Elements()
            .Where(e => e.Name.LocalName == "StreamGeometry")
            .ToDictionary(e => (string)e.Attribute(Xaml + "Key")!, e => e.Value.Trim());

    public static TheoryData<string> Keys()
    {
        var data = new TheoryData<string>();
        foreach (var key in TrailerIcons.AllKeys) data.Add(key);
        return data;
    }

    [Theory]
    [MemberData(nameof(Keys))]
    public void AllKeys_Key_ExistsAsNonEmptyStreamGeometry(string key)
    {
        var geometries = Geometries();

        Assert.True(geometries.TryGetValue(key, out var data), $"Missing StreamGeometry {key}");
        Assert.NotEmpty(data!);
    }

    [Fact]
    public void AllKeys_Count_IsFourteen()
    {
        Assert.Equal(14, TrailerIcons.AllKeys.Count);
    }

    [Fact]
    public void Icons_TrailerGeometries_MatchAllKeysExactly()
    {
        var declared = Geometries().Keys.Where(k => k.StartsWith("IconTrailer", StringComparison.Ordinal)).Order(StringComparer.Ordinal);

        Assert.Equal(TrailerIcons.AllKeys, declared);
    }
}
