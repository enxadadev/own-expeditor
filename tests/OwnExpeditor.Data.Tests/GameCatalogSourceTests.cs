using OwnExpeditor.Data.Games;
using OwnExpeditor.Domain.Games;
using OwnExpeditor.Domain.Saves;

namespace OwnExpeditor.Data.Tests;

public class GameCatalogSourceTests : IDisposable
{
    private const string Local =
        "SiiNunit\r\n{\r\nlocalization_db : .localization\r\n{\r\n key[]: \"a\"\r\n val[]: \"A\"\r\n}\r\n}\r\n";

    private readonly TempDir _t = new();
    private int _defLoads;
    private readonly List<(IReadOnlyList<string> Files, string? Lang)> _textLoads = [];
    private readonly HashSet<string> _unavailable = [];

    public void Dispose() => _t.Dispose();

    private GameCatalogSource Source() => new(
        files => { _defLoads++; _lastDefFiles = files; return new GameCatalog(); },
        (files, lang) =>
        {
            _textLoads.Add((files, lang));
            // imita o loader real: idioma ausente cai em en_us
            return Localization.FromText(Local, lang is not null && !_unavailable.Contains(lang) ? lang : "en_us");
        });

    private IReadOnlyList<string>? _lastDefFiles;

    private string Install(params string[] scs)
    {
        var dir = _t.Dir("game");
        foreach (var f in scs) File.WriteAllText(Path.Combine(dir, f), "x");
        return dir;
    }

    // ---- definições

    [Fact]
    public void LoadDefinitions_SameFolderTwice_LoadsOnceAndReturnsSameInstance()
    {
        var dir = Install("def.scs", "base.scs");
        var source = Source();

        var a = source.LoadDefinitions(dir);
        var b = source.LoadDefinitions(dir);

        Assert.Same(a, b);
        Assert.Equal(1, _defLoads);
    }

    [Fact]
    public void LoadDefinitions_PassesDefThenSortedDlcsAndNotLocale()
    {
        var dir = Install("def.scs", "dlc_b.scs", "dlc_a.scs", "locale.scs", "base.scs");

        Source().LoadDefinitions(dir);

        Assert.Equal(["def.scs", "dlc_a.scs", "dlc_b.scs"], _lastDefFiles!.Select(Path.GetFileName).ToArray());
    }

    [Fact]
    public void LoadDefinitions_ScsFileModified_ReloadsCatalog()
    {
        var dir = Install("def.scs");
        var source = Source();
        source.LoadDefinitions(dir);

        File.SetLastWriteTimeUtc(Path.Combine(dir, "def.scs"), DateTime.UtcNow.AddHours(-5));
        source.LoadDefinitions(dir);

        Assert.Equal(2, _defLoads);
    }

    [Fact]
    public void LoadDefinitions_NewDlcAdded_ReloadsCatalog()
    {
        var dir = Install("def.scs");
        var source = Source();
        source.LoadDefinitions(dir);

        File.WriteAllText(Path.Combine(dir, "dlc_new.scs"), "x");
        source.LoadDefinitions(dir);

        Assert.Equal(2, _defLoads);
    }

    [Fact]
    public void LoadDefinitions_TwoFolders_AreCachedSeparately()
    {
        var a = Install("def.scs");
        var b = _t.Dir("game2");
        File.WriteAllText(Path.Combine(b, "def.scs"), "x");
        var source = Source();

        var ca = source.LoadDefinitions(a);
        var cb = source.LoadDefinitions(b);

        Assert.NotSame(ca, cb);
        Assert.Equal(2, _defLoads);
    }

    [Fact]
    public void LoadDefinitions_MissingFolder_ThrowsIOException()
    {
        Assert.ThrowsAny<IOException>(() => Source().LoadDefinitions(_t.Combine("nada")));
    }

    [Fact]
    public void LoadDefinitions_NoDefOrDlc_ThrowsFileNotFound()
    {
        var dir = Install("base.scs");

        Assert.Throws<FileNotFoundException>(() => Source().LoadDefinitions(dir));
    }

    // ---- textos

    [Fact]
    public void LoadTexts_UsesLocaleAndDefArchives_AndFirstAvailableLanguage()
    {
        var dir = Install("def.scs", "locale.scs", "dlc_a.scs");

        var texts = Source().LoadTexts(dir, ["pt_br", "en_us"]);

        Assert.Equal("pt_br", texts.Language);
        var call = Assert.Single(_textLoads);
        Assert.Equal("pt_br", call.Lang);
        Assert.Contains("locale.scs", call.Files.Select(Path.GetFileName));
        Assert.Contains("def.scs", call.Files.Select(Path.GetFileName));
    }

    [Fact]
    public void LoadTexts_FirstLanguageMissing_UsesNextOne()
    {
        var dir = Install("def.scs", "locale.scs");
        _unavailable.Add("pt_br");

        var texts = Source().LoadTexts(dir, ["pt_br", "de_de"]);

        Assert.Equal("de_de", texts.Language);
        Assert.Equal(["pt_br", "de_de"], _textLoads.Select(c => c.Lang).ToArray());
    }

    [Fact]
    public void LoadTexts_NoLanguageMatches_ReturnsFallbackText()
    {
        var dir = Install("def.scs", "locale.scs");
        _unavailable.UnionWith(["pt_br", "de_de"]);

        var texts = Source().LoadTexts(dir, ["pt_br", "de_de"]);

        Assert.Equal("en_us", texts.Language);
        Assert.Equal("A", texts.Resolve("@@a@@"));
    }

    [Fact]
    public void LoadTexts_EmptyLanguageList_ReturnsDefaultLanguageTexts()
    {
        var dir = Install("def.scs", "locale.scs");

        var texts = Source().LoadTexts(dir, []);

        Assert.NotNull(texts);
        Assert.Null(Assert.Single(_textLoads).Lang);
    }

    [Fact]
    public void LoadTexts_SameFolderAndLanguages_LoadsOnce()
    {
        var dir = Install("def.scs", "locale.scs");
        var source = Source();

        var a = source.LoadTexts(dir, ["pt_br"]);
        var b = source.LoadTexts(dir, ["pt_br"]);

        Assert.Same(a, b);
        Assert.Single(_textLoads);
    }

    [Fact]
    public void LoadTexts_OtherLanguage_ReloadsTextsButNotDefinitions()
    {
        var dir = Install("def.scs", "locale.scs");
        var source = Source();
        source.LoadDefinitions(dir);
        source.LoadTexts(dir, ["pt_br"]);

        source.LoadTexts(dir, ["en_us"]);
        source.LoadDefinitions(dir);

        Assert.Equal(2, _textLoads.Count);
        Assert.Equal(1, _defLoads);
    }

    [Fact]
    public void LoadTexts_LocaleModified_Reloads()
    {
        var dir = Install("def.scs", "locale.scs");
        var source = Source();
        source.LoadTexts(dir, ["pt_br"]);

        File.SetLastWriteTimeUtc(Path.Combine(dir, "locale.scs"), DateTime.UtcNow.AddHours(-5));
        source.LoadTexts(dir, ["pt_br"]);

        Assert.Equal(2, _textLoads.Count);
    }

    // ---- idioma do jogo

    private ProfileInfo Profile(string docsGame)
    {
        var p = _t.Dir(docsGame, "profiles", Sii.Hex("P"));
        return new ProfileInfo(GameKind.Ets2, Sii.Hex("P"), "P", p, false);
    }

    [Fact]
    public void GameLanguage_ConfigInDocumentsFolder_ReturnsLanguage()
    {
        var profile = Profile("docs");
        _t.File("uset g_lang_init \"pt_br\"\r\nuset g_x \"1\"\r\n", "docs", "config.cfg");

        Assert.Equal("pt_br", Source().GameLanguage(profile));
    }

    [Fact]
    public void GameLanguage_NoConfig_ReturnsNull()
    {
        Assert.Null(Source().GameLanguage(Profile("docs")));
    }

    [Fact]
    public void GameLanguage_ConfigWithoutLanguageLine_ReturnsNull()
    {
        var profile = Profile("docs");
        _t.File("uset g_x \"1\"\r\n", "docs", "config.cfg");

        Assert.Null(Source().GameLanguage(profile));
    }
}
