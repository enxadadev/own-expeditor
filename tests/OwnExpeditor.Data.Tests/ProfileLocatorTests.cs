using OwnExpeditor.Data;
using OwnExpeditor.Data.Saves;
using OwnExpeditor.Domain.Games;

namespace OwnExpeditor.Data.Tests;

public class ProfileLocatorTests : IDisposable
{
    private const string Ets2 = "Euro Truck Simulator 2";
    private const string Ats = "American Truck Simulator";
    private readonly TempDir _t = new();

    public void Dispose() => _t.Dispose();

    private ProfileLocator Locator(IReadOnlyList<string>? steam = null, string? docs = "docs", string? home = null) =>
        new(new DataOptions
        {
            DocumentsRoot = docs is null ? null : _t.Combine(docs),
            SteamRoots = steam ?? [],
            HomeDirectory = home ?? _t.Combine("home"),
            UseRegistry = false,
        });

    [Fact]
    public void FindProfiles_ProfilesFolder_ReturnsProfileWithDecodedNameAndNoCloud()
    {
        Layout.Profile(_t, $"docs/{Ets2}", "profiles", Sii.Hex("Joao Pedro"), GameKind.Ets2, ("1", "a"));

        var result = Locator().FindProfiles();

        var p = Assert.Single(result);
        Assert.Equal(GameKind.Ets2, p.Game);
        Assert.Equal("Joao Pedro", p.DisplayName);
        Assert.Equal(Sii.Hex("Joao Pedro"), p.Id);
        Assert.False(p.IsSteamCloud);
        Assert.Equal(_t.Combine("docs", Ets2, "profiles", Sii.Hex("Joao Pedro")), p.DirectoryPath);
    }

    [Fact]
    public void FindProfiles_SteamProfiles_MarkedAsSteamCloud()
    {
        Layout.Profile(_t, $"docs/{Ets2}", "steam_profiles", "76561198000000000", GameKind.Ets2, ("1", "a"));

        var p = Assert.Single(Locator().FindProfiles());

        Assert.True(p.IsSteamCloud);
        Assert.Equal("76561198000000000", p.Id);
    }

    [Fact]
    public void FindProfiles_BothGames_TagsEachWithItsGame()
    {
        Layout.Profile(_t, $"docs/{Ets2}", "profiles", Sii.Hex("E"), GameKind.Ets2, ("1", "a"));
        Layout.Profile(_t, $"docs/{Ats}", "profiles", Sii.Hex("A"), GameKind.Ats, ("1", "a"));

        var result = Locator().FindProfiles();

        Assert.Equal([GameKind.Ets2, GameKind.Ats], result.Select(p => p.Game).Order().ToArray());
        Assert.Equal(GameKind.Ats, result.Single(p => p.DisplayName == "A").Game);
    }

    [Fact]
    public void FindProfiles_ProfileWithoutSaveFolder_IsIgnored()
    {
        _t.Dir("docs", Ets2, "profiles", Sii.Hex("Vazio"));

        Assert.Empty(Locator().FindProfiles());
    }

    [Fact]
    public void FindProfiles_NothingOnDisk_ReturnsEmpty()
    {
        Assert.Empty(Locator().FindProfiles());
    }

    [Fact]
    public void FindProfiles_ProtonPrefix_FindsProfilesInCompatdata()
    {
        var steam = _t.Dir("steam");
        var docs = $"steam/steamapps/compatdata/227300/pfx/drive_c/users/steamuser/Documents/{Ets2}";
        Layout.Profile(_t, docs, "profiles", Sii.Hex("Proton"), GameKind.Ets2, ("1", "a"));

        var p = Assert.Single(Locator([steam]).FindProfiles());

        Assert.Equal("Proton", p.DisplayName);
        Assert.Equal(GameKind.Ets2, p.Game);
    }

    [Fact]
    public void FindProfiles_ProtonPrefixInSecondaryLibrary_UsesLibraryFolders()
    {
        var steam = _t.Dir("steam");
        var lib = _t.Dir("lib2");
        _t.File($"\"libraryfolders\"\n{{\n\t\"0\"\n\t{{\n\t\t\"path\"\t\t\"{lib.Replace("\\", "\\\\")}\"\n\t}}\n}}\n",
            "steam", "steamapps", "libraryfolders.vdf");
        var docs = $"lib2/steamapps/compatdata/270880/pfx/drive_c/users/steamuser/Documents/{Ats}";
        Layout.Profile(_t, docs, "profiles", Sii.Hex("Ame"), GameKind.Ats, ("1", "a"));

        var p = Assert.Single(Locator([steam]).FindProfiles());

        Assert.Equal(GameKind.Ats, p.Game);
        Assert.Equal("Ame", p.DisplayName);
    }

    [Fact]
    public void FindProfiles_LinuxNativeLocalShare_IsSearched()
    {
        var home = _t.Combine("home");
        Layout.Profile(_t, $"home/.local/share/{Ets2}", "profiles", Sii.Hex("Nativo"), GameKind.Ets2, ("1", "a"));

        var result = Locator(docs: null, home: home).FindProfiles();

        Assert.Contains(result, p => p.DisplayName == "Nativo");
    }

    [Fact]
    public void FindProfiles_SameProfileReachableTwice_ListedOnce()
    {
        var steam = _t.Dir("steam");
        Layout.Profile(_t, $"docs/{Ets2}", "profiles", Sii.Hex("Um"), GameKind.Ets2, ("1", "a"));

        var result = Locator([steam, steam]).FindProfiles();

        Assert.Single(result);
    }

    // ---- saves

    [Fact]
    public void FindSaves_ReturnsSlotsWithInfoNameSizeAndPath_NewestFirst()
    {
        var profile = Layout.Profile(_t, $"docs/{Ets2}", "profiles", Sii.Hex("P"), GameKind.Ets2,
            ("1", "Antigo"), ("2", "Novo"));
        File.SetLastWriteTimeUtc(Path.Combine(profile.DirectoryPath, "save", "1", "game.sii"), new DateTime(2020, 1, 1));
        File.SetLastWriteTimeUtc(Path.Combine(profile.DirectoryPath, "save", "2", "game.sii"), new DateTime(2024, 1, 1));

        var saves = Locator().FindSaves(profile);

        Assert.Equal(["Novo", "Antigo"], saves.Select(s => s.DisplayName).ToArray());
        Assert.Equal(["2", "1"], saves.Select(s => s.Id).ToArray());
        Assert.Equal(Path.Combine(profile.DirectoryPath, "save", "2", "game.sii"), saves[0].GameSiiPath);
        Assert.Equal(new DateTime(2024, 1, 1), saves[0].LastWriteUtc);
        Assert.Equal(Sii.Game.Length, saves[0].SizeBytes);
        Assert.Equal(profile, saves[0].Profile);
    }

    [Fact]
    public void FindSaves_SlotWithoutInfo_FallsBackToSlotId()
    {
        var profile = Layout.Profile(_t, $"docs/{Ets2}", "profiles", Sii.Hex("P"), GameKind.Ets2, ("quicksave", "x"));
        File.Delete(Path.Combine(profile.DirectoryPath, "save", "quicksave", "info.sii"));

        var s = Assert.Single(Locator().FindSaves(profile));

        Assert.Equal("quicksave", s.DisplayName);
    }

    [Fact]
    public void FindSaves_SlotWithoutGameSii_IsIgnored()
    {
        var profile = Layout.Profile(_t, $"docs/{Ets2}", "profiles", Sii.Hex("P"), GameKind.Ets2, ("1", "a"));
        _t.Dir("docs", Ets2, "profiles", Sii.Hex("P"), "save", "2");

        Assert.Single(Locator().FindSaves(profile));
    }

    [Fact]
    public void FindSaves_MissingSaveFolder_ReturnsEmpty()
    {
        var profile = Layout.Profile(_t, $"docs/{Ets2}", "profiles", Sii.Hex("P"), GameKind.Ets2);
        Directory.Delete(Path.Combine(profile.DirectoryPath, "save"));

        Assert.Empty(Locator().FindSaves(profile));
    }

    [Fact]
    public void FindSaves_CorruptInfo_FallsBackToSlotId()
    {
        var profile = Layout.Profile(_t, $"docs/{Ets2}", "profiles", Sii.Hex("P"), GameKind.Ets2, ("3", "x"));
        File.WriteAllText(Path.Combine(profile.DirectoryPath, "save", "3", "info.sii"), "isto não é SII");

        var s = Assert.Single(Locator().FindSaves(profile));

        Assert.Equal("3", s.DisplayName);
    }
}
