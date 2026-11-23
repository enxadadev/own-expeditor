using System.Reflection;
using System.Runtime.ExceptionServices;
using OwnExpeditor.Data.Maps;
using OwnExpeditor.Domain.Games;
using OwnExpeditor.Domain.Maps;

namespace OwnExpeditor.Data.Tests.Maps;

/// <summary>
/// A chave do cache é calculada dentro de <see cref="MapSource"/>. Para testá-la sem abrir .scs de verdade,
/// os testes exigem <c>internal static string MapSource.ComputeKey(MapRequest)</c> (ponto de extensão proposto).
/// </summary>
public class MapSourceTests
{
    private static string ComputeKey(MapRequest request)
    {
        var m = typeof(MapSource).GetMethod("ComputeKey",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, [typeof(MapRequest)]);
        Assert.True(m is not null, "Ponto de extensão ausente: internal static string MapSource.ComputeKey(MapRequest).");
        try { return (string)m!.Invoke(null, [request])!; }
        catch (TargetInvocationException e) { ExceptionDispatchInfo.Capture(e.InnerException!).Throw(); throw; }
    }

    private static string Install(TempDir tmp, int baseSize = 10)
    {
        var dir = tmp.Dir("game");
        File.WriteAllBytes(Path.Combine(dir, "base.scs"), new byte[baseSize]);
        File.WriteAllBytes(Path.Combine(dir, "def.scs"), new byte[5]);
        File.SetLastWriteTimeUtc(Path.Combine(dir, "base.scs"), new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        return dir;
    }

    private static ModPackage Mod(TempDir tmp, string name, int priority, int size = 3)
    {
        var path = tmp.Combine("mods", name + ".scs");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, new byte[size]);
        File.SetLastWriteTimeUtc(path, new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc));
        return new ModPackage(name, path, priority);
    }

    private static MapRequest Req(string dir, params ModPackage[] mods)
        => new(GameKind.Ets2, dir, mods, MapDetail.Cities);

    // ---- LoadAsync ----

    [Theory]
    [InlineData("")]
    [InlineData("/caminho/que/nao/existe/ownexp")]
    public async Task LoadAsync_InstallDirMissing_ReturnsNoGameFolder(string installDir)
    {
        using var tmp = new TempDir();
        var source = new MapSource(tmp.Combine("cache"));

        var result = await source.LoadAsync(Req(installDir), null, CancellationToken.None);

        Assert.Equal(MapStatus.NoGameFolder, result.Status);
        Assert.Null(result.Index);
    }

    [Fact]
    public async Task LoadAsync_CachedIndexForCurrentKey_ReturnsItWithoutReadingScs()
    {
        using var tmp = new TempDir();
        var req = Req(Install(tmp));
        var source = new MapSource(tmp.Combine("cache"));
        var cached = MapTestData.SampleIndex();
        await source.Cache.WriteAsync(ComputeKey(req), cached, CancellationToken.None);

        var result = await source.LoadAsync(req, null, CancellationToken.None);

        Assert.Equal(MapStatus.Ready, result.Status);
        MapTestData.AssertEquivalent(cached, result.Index!);
    }

    [Fact]
    public async Task LoadAsync_CancelledToken_ThrowsOperationCanceled()
    {
        using var tmp = new TempDir();
        var source = new MapSource(tmp.Combine("cache"));
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => source.LoadAsync(Req(Install(tmp)), null, cts.Token));
    }

    [Fact]
    public void Ctor_EmptyCacheDirectory_Throws()
    {
        Assert.Throws<ArgumentException>(() => new MapSource(""));
    }

    // ---- chave do cache ----

    [Fact]
    public void ComputeKey_SameInstallAndMods_ReturnsSameKey()
    {
        using var tmp = new TempDir();
        var dir = Install(tmp);
        var mod = Mod(tmp, "promods", 1);

        Assert.Equal(ComputeKey(Req(dir, mod)), ComputeKey(Req(dir, mod)));
    }

    [Fact]
    public void ComputeKey_ScsSizeChanges_ReturnsOtherKey()
    {
        using var tmp = new TempDir();
        var dir = Install(tmp, baseSize: 10);
        var before = ComputeKey(Req(dir));

        File.WriteAllBytes(Path.Combine(dir, "base.scs"), new byte[11]);
        File.SetLastWriteTimeUtc(Path.Combine(dir, "base.scs"), new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        Assert.NotEqual(before, ComputeKey(Req(dir)));
    }

    [Fact]
    public void ComputeKey_ScsDateChanges_ReturnsOtherKey()
    {
        using var tmp = new TempDir();
        var dir = Install(tmp);
        var before = ComputeKey(Req(dir));

        File.SetLastWriteTimeUtc(Path.Combine(dir, "base.scs"), new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc));

        Assert.NotEqual(before, ComputeKey(Req(dir)));
    }

    [Fact]
    public void ComputeKey_DlcPackageAdded_ReturnsOtherKey()
    {
        using var tmp = new TempDir();
        var dir = Install(tmp);
        var before = ComputeKey(Req(dir));

        File.WriteAllBytes(Path.Combine(dir, "dlc_demo.scs"), new byte[7]);

        Assert.NotEqual(before, ComputeKey(Req(dir)));
    }

    [Fact]
    public void ComputeKey_ModAdded_ReturnsOtherKey()
    {
        using var tmp = new TempDir();
        var dir = Install(tmp);

        Assert.NotEqual(ComputeKey(Req(dir)), ComputeKey(Req(dir, Mod(tmp, "promods", 1))));
    }

    [Fact]
    public void ComputeKey_ModOrderChanges_ReturnsOtherKey()
    {
        using var tmp = new TempDir();
        var dir = Install(tmp);
        var a = Mod(tmp, "mod_a", 1);
        var b = Mod(tmp, "mod_b", 2);

        Assert.NotEqual(ComputeKey(Req(dir, a, b)), ComputeKey(Req(dir, b, a)));
    }

    [Fact]
    public void ComputeKey_ModFileSizeChanges_ReturnsOtherKey()
    {
        using var tmp = new TempDir();
        var dir = Install(tmp);
        var mod = Mod(tmp, "promods", 1, size: 3);
        var before = ComputeKey(Req(dir, mod));

        File.WriteAllBytes(mod.Path, new byte[4]);
        File.SetLastWriteTimeUtc(mod.Path, new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc));

        Assert.NotEqual(before, ComputeKey(Req(dir, mod)));
    }
}
