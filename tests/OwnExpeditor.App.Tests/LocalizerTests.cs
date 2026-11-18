using System.Globalization;
using OwnExpeditor.App.Services;
using OwnExpeditor.Core;
using OwnExpeditor.Core.Text;
using OwnExpeditor.Domain.Games;
using OwnExpeditor.Domain.Settings;

namespace OwnExpeditor.App.Tests;

public class LocalizerTests
{
    private static Localizer Create(Language language) => new(language);

    [Fact]
    public void Get_PtBr_ReturnsPortugueseText()
    {
        var loc = Create(Language.PtBr);

        Assert.Equal("Cancelar", loc.Get("Common_Cancel"));
    }

    [Fact]
    public void Get_En_ReturnsEnglishText()
    {
        var loc = Create(Language.En);

        Assert.Equal("Cancel", loc.Get("Common_Cancel"));
    }

    [Fact]
    public void Indexer_SameAsGetWithoutArgs()
    {
        var loc = Create(Language.En);

        Assert.Equal(loc.Get("Common_Cancel"), loc["Common_Cancel"]);
    }

    [Fact]
    public void Get_WithArgs_FormatsWithAppCulture()
    {
        var loc = Create(Language.PtBr);

        Assert.Equal("1.234 km", loc.Get("Format_Km", 1234));
    }

    [Fact]
    public void Get_WithArgsInEnglish_UsesEnglishNumberFormat()
    {
        var loc = Create(Language.En);

        Assert.Equal("1,234 km", loc.Get("Format_Km", 1234));
    }

    [Fact]
    public void Get_MissingKey_ReturnsVisibleMarkerWithKey()
    {
        var loc = Create(Language.En);

        Assert.Equal("!Nao_Existe!", loc.Get("Nao_Existe"));
    }

    [Theory]
    [InlineData(0, "Nenhuma mudança pendente")]
    [InlineData(1, "1 mudança pendente")]
    [InlineData(2, "2 mudanças pendentes")]
    [InlineData(100, "100 mudanças pendentes")]
    public void GetPlural_PtBr_PicksZeroOneOther(long count, string expected)
    {
        var loc = Create(Language.PtBr);

        Assert.Equal(expected, loc.GetPlural("Pending_Count", count));
    }

    [Theory]
    [InlineData(0, "No pending changes")]
    [InlineData(1, "1 pending change")]
    [InlineData(5, "5 pending changes")]
    public void GetPlural_En_PicksZeroOneOther(long count, string expected)
    {
        var loc = Create(Language.En);

        Assert.Equal(expected, loc.GetPlural("Pending_Count", count));
    }

    [Fact]
    public void GetPlural_ZeroMissing_FallsBackToOther()
    {
        var loc = Create(Language.En);

        Assert.Equal("0 pending changes", loc.GetPlural("Shell_Pending", 0));
    }

    [Fact]
    public void GetPlural_CountIsPlaceholderZero()
    {
        var loc = Create(Language.En);

        Assert.Equal("3 pending changes", loc.GetPlural("Shell_Pending", 3));
    }

    [Fact]
    public void SetLanguage_ChangesCultureAtRuntime()
    {
        var loc = Create(Language.PtBr);

        loc.SetLanguage(Language.En);

        Assert.Equal("Cancel", loc.Get("Common_Cancel"));
        Assert.Equal("en", loc.Culture.TwoLetterISOLanguageName);
    }

    [Fact]
    public void SetLanguage_RaisesIndexerChanged()
    {
        var loc = Create(Language.PtBr);
        var raised = new List<string?>();
        loc.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        loc.SetLanguage(Language.En);

        Assert.Contains("Item[]", raised);
    }

    [Fact]
    public void SetLanguage_SameLanguage_DoesNotRaise()
    {
        var loc = Create(Language.En);
        var raised = 0;
        loc.PropertyChanged += (_, _) => raised++;

        loc.SetLanguage(Language.En);

        Assert.Equal(0, raised);
    }

    [Fact]
    public void Format_TextRefWithCount_UsesPlural()
    {
        var loc = Create(Language.PtBr);

        Assert.Equal("2 mudanças", loc.Format(TextRef.Plural("Count_Changes", 2)));
    }

    [Fact]
    public void Format_TextRefWithArgs_FormatsArgs()
    {
        var loc = Create(Language.En);

        Assert.Equal("1,234 km", loc.Format(TextRef.Of("Format_Km", 1234)));
    }

    [Theory]
    [InlineData(Language.PtBr, GameKind.Ets2, "1.234.567 €")]
    [InlineData(Language.En, GameKind.Ets2, "€1,234,567")]
    [InlineData(Language.PtBr, GameKind.Ats, "US$ 1.234.567")]
    [InlineData(Language.En, GameKind.Ats, "$1,234,567")]
    public void FormatMoney_PerGameAndCulture_UsesFixedCurrency(Language language, GameKind game, string expected)
    {
        var loc = Create(language);

        Assert.Equal(expected, loc.FormatMoney(1_234_567, game));
    }

    [Theory]
    [InlineData(CoreError.GameRunning, "O jogo está aberto")]
    [InlineData(CoreError.SaveNotFound, "Não encontramos o arquivo do save")]
    public void FormatError_CoreError_UsesErrorKey(CoreError code, string startsWith)
    {
        var loc = Create(Language.PtBr);

        Assert.StartsWith(startsWith, loc.FormatError(code));
    }

    [Fact]
    public void FormatDiffValue_Empty_UsesEmptyText()
    {
        var loc = Create(Language.En);

        Assert.Equal("(empty)", loc.Format(new DiffValue.Empty()));
    }

    [Fact]
    public void FormatDiffValue_Money_UsesGameCurrency()
    {
        var loc = Create(Language.En);

        Assert.Equal("€250,000", loc.Format(new DiffValue.Money(250_000, GameKind.Ets2)));
    }

    [Fact]
    public void FormatDiffValue_GameNameWithoutNames_FallsBackToHumanizedId()
    {
        var loc = Create(Language.En);

        Assert.Equal(GameIds.Humanize("log_n_stick"), loc.Format(new DiffValue.GameName(GameNameKind.Company, "log_n_stick")));
    }

#if DEBUG
    [Fact]
    public void Get_GalleryBundleKey_IsResolvedInDebug()
    {
        var loc = Create(Language.En);

        Assert.Equal("Control gallery", loc.Get("Gallery_Title"));
    }
#endif
}
