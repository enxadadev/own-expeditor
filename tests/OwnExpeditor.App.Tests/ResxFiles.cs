using System.Xml.Linq;

namespace OwnExpeditor.App.Tests;

/// <summary>Lê os .resx do App (copiados para a saída) como dicionário chave → valor.</summary>
internal static class ResxFiles
{
    public static readonly string[] Bundles = ["Strings", "Gallery"];

    public static IReadOnlyDictionary<string, string> Read(string bundle, string? culture)
    {
        var name = culture is null ? $"{bundle}.resx" : $"{bundle}.{culture}.resx";
        var path = Path.Combine(AppContext.BaseDirectory, "Resources", name);
        return XDocument.Load(path).Root!.Elements("data")
            .ToDictionary(e => (string)e.Attribute("name")!, e => (string)e.Element("value")!);
    }

    public static IEnumerable<object[]> BundleNames() => Bundles.Select(b => new object[] { b });
}
