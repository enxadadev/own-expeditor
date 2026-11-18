using System.Text.RegularExpressions;
using OwnExpeditor.Core;

namespace OwnExpeditor.App.Tests;

public class ResxParityTests
{
    public static IEnumerable<object[]> Bundles() => ResxFiles.BundleNames();

    public static IEnumerable<object[]> CoreErrors() =>
        Enum.GetValues<CoreError>().Select(e => new object[] { e });

    [Theory, MemberData(nameof(Bundles))]
    public void Keys_PtBrAndEn_AreIdentical(string bundle)
    {
        var en = ResxFiles.Read(bundle, null).Keys.ToHashSet();
        var pt = ResxFiles.Read(bundle, "pt-BR").Keys.ToHashSet();

        Assert.Empty(en.Except(pt));
        Assert.Empty(pt.Except(en));
    }

    [Theory, MemberData(nameof(Bundles))]
    public void Values_BothLanguages_AreNotEmpty(string bundle)
    {
        var empty = ResxFiles.Read(bundle, null).Concat(ResxFiles.Read(bundle, "pt-BR"))
            .Where(kv => string.IsNullOrWhiteSpace(kv.Value)).Select(kv => kv.Key);

        Assert.Empty(empty);
    }

    [Theory, MemberData(nameof(Bundles))]
    public void Placeholders_PtBrAndEn_UseTheSameIndexes(string bundle)
    {
        var en = ResxFiles.Read(bundle, null);
        var pt = ResxFiles.Read(bundle, "pt-BR");

        var different = en.Where(kv => pt.ContainsKey(kv.Key) && Indexes(kv.Value) != Indexes(pt[kv.Key]))
            .Select(kv => kv.Key);

        Assert.Empty(different);
    }

    [Theory, MemberData(nameof(Bundles))]
    public void PluralKeys_OneAndZero_HaveOther(string bundle)
    {
        foreach (var culture in new string?[] { null, "pt-BR" })
        {
            var keys = ResxFiles.Read(bundle, culture).Keys.ToHashSet();
            var missing = keys
                .Where(k => k.EndsWith("_One") || k.EndsWith("_Zero"))
                .Select(k => k[..k.LastIndexOf('_')])
                .Where(b => !keys.Contains(b + "_Other"))
                .Distinct();

            Assert.Empty(missing);
        }
    }

    [Theory, MemberData(nameof(Bundles))]
    public void PluralKeys_OtherWithOneSibling_HaveOne(string bundle)
    {
        var keys = ResxFiles.Read(bundle, null).Keys.ToHashSet();

        var missing = keys.Where(k => k.EndsWith("_Zero")).Select(k => k[..^5])
            .Where(b => !keys.Contains(b + "_One"));

        Assert.Empty(missing);
    }

    [Fact]
    public void PluralKeys_Design_AreAllPresent()
    {
        var keys = ResxFiles.Read("Strings", null).Keys.ToHashSet();

        Assert.Contains("Pending_Count_One", keys);
        Assert.Contains("Pending_Count_Other", keys);
        Assert.Contains("Count_Changes_One", keys);
        Assert.Contains("Count_Changes_Other", keys);
    }

    [Theory, MemberData(nameof(CoreErrors))]
    public void ErrorKey_EachCoreError_ExistsInBothLanguages(CoreError code)
    {
        var key = $"Error_{code}";

        Assert.False(string.IsNullOrWhiteSpace(ResxFiles.Read("Strings", null).GetValueOrDefault(key)));
        Assert.False(string.IsNullOrWhiteSpace(ResxFiles.Read("Strings", "pt-BR").GetValueOrDefault(key)));
    }

    [Theory]
    [InlineData("Format_Money_Ets2")]
    [InlineData("Format_Money_Ats")]
    public void MoneyFormat_PerGame_HasAmountPlaceholderInBothLanguages(string key)
    {
        Assert.Contains("{0", ResxFiles.Read("Strings", null)[key]);
        Assert.Contains("{0", ResxFiles.Read("Strings", "pt-BR")[key]);
    }

    [Fact]
    public void MoneyFormat_Ets2AndAts_UseTheirCurrency()
    {
        var en = ResxFiles.Read("Strings", null);
        var pt = ResxFiles.Read("Strings", "pt-BR");

        Assert.Contains("€", en["Format_Money_Ets2"]);
        Assert.Contains("€", pt["Format_Money_Ets2"]);
        Assert.Contains("$", en["Format_Money_Ats"]);
        Assert.Contains("$", pt["Format_Money_Ats"]);
    }

    [Fact]
    public void Keys_Mockups_AreNotShipped()
    {
        var all = ResxFiles.Bundles.SelectMany(b => ResxFiles.Read(b, null).Keys);

        Assert.DoesNotContain(all, k => k.StartsWith("Canvas_"));
    }

    private static string Indexes(string value) =>
        string.Join(",", Regex.Matches(value, @"\{(\d+)[^}]*\}").Select(m => m.Groups[1].Value).Distinct().OrderBy(x => x));
}
