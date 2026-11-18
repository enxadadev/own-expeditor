using OwnExpeditor.Data;
using OwnExpeditor.Data.Games;
using OwnExpeditor.Domain.Games;

namespace OwnExpeditor.Data.Tests;

public class GameInstallLocatorTests : IDisposable
{
    private const string Ets2Name = "Euro Truck Simulator 2";
    private const string AtsName = "American Truck Simulator";
    private readonly TempDir _t = new();

    public void Dispose() => _t.Dispose();

    private string Install(string relative, string exe = "bin/win_x64/eurotrucks2.exe",
        bool baseScs = true, bool defScs = true)
    {
        var dir = _t.Dir(relative.Split('/'));
        if (baseScs) _t.File("", [.. relative.Split('/'), "base.scs"]);
        if (defScs) _t.File("", [.. relative.Split('/'), "def.scs"]);
        if (exe.Length > 0) _t.File("", [.. relative.Split('/'), .. exe.Split('/')]);
        return dir;
    }

    private GameInstallLocator Locator(IReadOnlyList<string>? steam = null, bool registry = false) =>
        new(new DataOptions { SteamRoots = steam, HomeDirectory = _t.Combine("home"), UseRegistry = registry });

    // ---- Validate

    [Theory]
    [InlineData("bin/win_x64/eurotrucks2.exe")]
    [InlineData("bin/linux/eurotrucks2")]
    public void Validate_Ets2Install_ReturnsOk(string exe)
    {
        var dir = Install("game", exe);

        Assert.Equal(GameFolderCheck.Ok, Locator().Validate(GameKind.Ets2, dir));
    }

    [Theory]
    [InlineData("bin/win_x64/amtrucks.exe")]
    [InlineData("bin/linux/amtrucks")]
    public void Validate_AtsInstall_ReturnsOk(string exe)
    {
        var dir = Install("game", exe);

        Assert.Equal(GameFolderCheck.Ok, Locator().Validate(GameKind.Ats, dir));
    }

    [Fact]
    public void Validate_MissingFolder_ReturnsNotFound()
    {
        Assert.Equal(GameFolderCheck.NotFound, Locator().Validate(GameKind.Ets2, _t.Combine("nao-existe")));
    }

    [Fact]
    public void Validate_BlankPath_ReturnsNotFound()
    {
        Assert.Equal(GameFolderCheck.NotFound, Locator().Validate(GameKind.Ets2, "  "));
    }

    [Fact]
    public void Validate_EmptyFolder_ReturnsNotAGameFolder()
    {
        Assert.Equal(GameFolderCheck.NotAGameFolder, Locator().Validate(GameKind.Ets2, _t.Dir("vazia")));
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void Validate_MissingBaseOrDef_ReturnsNotAGameFolder(bool baseScs, bool defScs)
    {
        var dir = Install("game", baseScs: baseScs, defScs: defScs);

        Assert.Equal(GameFolderCheck.NotAGameFolder, Locator().Validate(GameKind.Ets2, dir));
    }

    [Fact]
    public void Validate_NoGameExecutable_ReturnsNotAGameFolder()
    {
        var dir = Install("game", exe: "");

        Assert.Equal(GameFolderCheck.NotAGameFolder, Locator().Validate(GameKind.Ets2, dir));
    }

    [Fact]
    public void Validate_AtsFolderChosenForEts2_ReturnsWrongGame()
    {
        var dir = Install("game", "bin/win_x64/amtrucks.exe");

        Assert.Equal(GameFolderCheck.WrongGame, Locator().Validate(GameKind.Ets2, dir));
    }

    [Fact]
    public void Validate_Ets2FolderChosenForAts_ReturnsWrongGame()
    {
        var dir = Install("game");

        Assert.Equal(GameFolderCheck.WrongGame, Locator().Validate(GameKind.Ats, dir));
    }

    // ---- Detect

    [Fact]
    public void Detect_DefaultSteamCommonFolder_IsFound()
    {
        var dir = Install($"steam/steamapps/common/{Ets2Name}");

        var found = Locator([_t.Combine("steam")]).Detect(GameKind.Ets2);

        Assert.Equal([dir], found);
    }

    [Fact]
    public void Detect_LibraryFoldersAndAppManifest_FindsCustomInstallDirInOtherLibrary()
    {
        _t.Dir("steam", "steamapps");
        var lib = _t.Dir("lib2");
        _t.File($"\"libraryfolders\"\n{{\n\t\"0\"\n\t{{\n\t\t\"path\"\t\t\"{_t.Combine("steam")}\"\n\t}}\n" +
                $"\t\"1\"\n\t{{\n\t\t\"path\"\t\t\"{lib}\"\n\t\t\"apps\"\n\t\t{{\n\t\t\t\"227300\"\t\t\"1\"\n\t\t}}\n\t}}\n}}\n",
            "steam", "steamapps", "libraryfolders.vdf");
        _t.File("\"AppState\"\n{\n\t\"appid\"\t\t\"227300\"\n\t\"installdir\"\t\t\"ETS2 Custom\"\n}\n",
            "lib2", "steamapps", "appmanifest_227300.acf");
        var dir = Install("lib2/steamapps/common/ETS2 Custom");

        var found = Locator([_t.Combine("steam")]).Detect(GameKind.Ets2);

        Assert.Equal([dir], found);
    }

    [Fact]
    public void Detect_AtsAppManifest_UsesAtsAppId()
    {
        _t.File("\"AppState\"\n{\n\t\"appid\"\t\t\"270880\"\n\t\"installdir\"\t\t\"ATS Dir\"\n}\n",
            "steam", "steamapps", "appmanifest_270880.acf");
        var dir = Install("steam/steamapps/common/ATS Dir", "bin/linux/amtrucks");
        Install($"steam/steamapps/common/{Ets2Name}");

        var ats = Locator([_t.Combine("steam")]).Detect(GameKind.Ats);

        Assert.Equal([dir], ats);
    }

    [Theory]
    [InlineData(".steam/steam")]
    [InlineData(".local/share/Steam")]
    [InlineData(".var/app/com.valvesoftware.Steam/.local/share/Steam")]
    [InlineData(".var/app/com.valvesoftware.Steam/.steam/steam")]
    public void Detect_LinuxAndFlatpakSteamRoots_AreSearched(string root)
    {
        var dir = Install($"home/{root}/steamapps/common/{AtsName}", "bin/linux/amtrucks");

        var found = Locator().Detect(GameKind.Ats);

        Assert.Contains(dir, found);
    }

    [Fact]
    public void Detect_InvalidCandidate_IsExcluded()
    {
        Install($"steam/steamapps/common/{Ets2Name}", "bin/win_x64/amtrucks.exe");

        Assert.Empty(Locator([_t.Combine("steam")]).Detect(GameKind.Ets2));
    }

    [Fact]
    public void Detect_SameInstallFromTwoRoots_ListedOnce()
    {
        Install($"steam/steamapps/common/{Ets2Name}");

        var found = Locator([_t.Combine("steam"), _t.Combine("steam")]).Detect(GameKind.Ets2);

        Assert.Single(found);
    }

    [Fact]
    public void Detect_NoSteam_ReturnsEmpty()
    {
        Assert.Empty(Locator([]).Detect(GameKind.Ets2));
    }

    [Fact]
    public void Detect_RegistryEnabledOnAnyPlatform_DoesNotThrow()
    {
        var found = Locator([], registry: true).Detect(GameKind.Ets2);

        Assert.NotNull(found);
    }

    // ---- libraryfolders.vdf

    [Fact]
    public void LibraryPaths_NewFormat_ReadsEachPathAndUnescapesBackslashes()
    {
        const string vdf = "\"libraryfolders\"\n{\n\t\"0\"\n\t{\n\t\t\"path\"\t\t\"C:\\\\Program Files (x86)\\\\Steam\"\n" +
                           "\t\t\"label\"\t\t\"\"\n\t}\n\t\"1\"\n\t{\n\t\t\"path\"\t\t\"D:\\\\Games\\\\SteamLibrary\"\n\t}\n}\n";

        var paths = SteamVdf.LibraryPaths(vdf);

        Assert.Equal([@"C:\Program Files (x86)\Steam", @"D:\Games\SteamLibrary"], paths);
    }

    [Fact]
    public void LibraryPaths_OldFormat_ReadsNumericEntries()
    {
        const string vdf = "\"LibraryFolders\"\n{\n\t\"TimeNextStatsReport\"\t\t\"123\"\n\t\"1\"\t\t\"/mnt/games/steam\"\n}\n";

        Assert.Equal(["/mnt/games/steam"], SteamVdf.LibraryPaths(vdf));
    }

    [Fact]
    public void LibraryPaths_Garbage_ReturnsEmptyWithoutThrowing()
    {
        Assert.Empty(SteamVdf.LibraryPaths("\"libraryfolders\" { \"0\" { \"path\" "));
    }

    [Fact]
    public void InstallDir_AppManifest_ReadsInstalldir()
    {
        const string acf = "\"AppState\"\n{\n\t\"appid\"\t\t\"227300\"\n\t\"installdir\"\t\t\"Euro Truck Simulator 2\"\n}\n";

        Assert.Equal("Euro Truck Simulator 2", SteamVdf.InstallDir(acf));
    }
}
