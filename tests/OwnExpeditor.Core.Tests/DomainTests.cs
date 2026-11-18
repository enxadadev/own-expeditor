using OwnExpeditor.Data;
using OwnExpeditor.Domain.Games;
using OwnExpeditor.Domain.Saves;
using OwnExpeditor.Domain.Settings;

namespace OwnExpeditor.Core.Tests;

public class DomainTests
{
    [Theory]
    [InlineData("plumb_suppl", "Plumb suppl")]
    [InlineData("  budapest ", "Budapest")]
    [InlineData("a", "A")]
    [InlineData("_", "_")]
    [InlineData("", "")]
    public void Humanize_formats_ids(string id, string expected) =>
        Assert.Equal(expected, GameIds.Humanize(id));

    [Fact]
    public void Localization_Humanize_delegates_to_Domain() =>
        Assert.Equal(GameIds.Humanize("plumb_suppl"), Localization.Humanize("plumb_suppl"));

    [Theory]
    [InlineData("4A2E204F776E657469", "J. Owneti")]
    [InlineData("C3A9", "é")]
    [InlineData("zzzz", "zzzz")]       // não é hex
    [InlineData("ABC", "ABC")]         // tamanho ímpar
    [InlineData("FFFE", "FFFE")]       // UTF-8 inválido
    [InlineData("0A0B", "0A0B")]       // caracteres de controle
    [InlineData("2020", "2020")]       // só espaços
    [InlineData("", "")]
    public void Profile_folder_name_decodes_hex(string folder, string expected) =>
        Assert.Equal(expected, ProfileNames.FromFolderName(folder));

    [Fact]
    public void GameFolders_For_and_With_are_per_game()
    {
        var f = new GameFolders().With(GameKind.Ats, "/ats").With(GameKind.Ets2, "/ets2");
        Assert.Equal("/ets2", f.For(GameKind.Ets2));
        Assert.Equal("/ats", f.For(GameKind.Ats));
        Assert.Null(f.With(GameKind.Ats, null).For(GameKind.Ats));
        Assert.Equal("/ets2", f.With(GameKind.Ats, null).For(GameKind.Ets2));
    }

    [Fact]
    public void GameFolders_is_immutable_and_compares_by_value()
    {
        var a = new GameFolders("/x");
        var b = a.With(GameKind.Ats, "/y");
        Assert.Null(a.Ats);
        Assert.Equal(new GameFolders("/x", "/y"), b);
    }

    [Fact]
    public void SaveLocator_profiles_use_domain_types()
    {
        var docs = Directory.CreateTempSubdirectory();
        try
        {
            Directory.CreateDirectory(Path.Combine(docs.FullName, "profiles", "4A2E204F776E657469", "save"));
            Directory.CreateDirectory(Path.Combine(docs.FullName, "steam_profiles", "ABC", "save"));
            var profiles = SaveLocator.Profiles(docs.FullName);
            var local = Assert.Single(profiles, p => !p.IsSteamCloud);
            Assert.Equal("J. Owneti", local.DisplayName);
            Assert.Equal(GameKind.Ets2, local.Game);
            Assert.Single(profiles, p => p.IsSteamCloud && p.DisplayName == "ABC");
        }
        finally { docs.Delete(recursive: true); }
    }
}
