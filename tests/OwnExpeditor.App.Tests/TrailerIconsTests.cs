using OwnExpeditor.App.Services;
using OwnExpeditor.Domain.Games;

namespace OwnExpeditor.App.Tests;

public class TrailerIconsTests
{
    public static TheoryData<string, string, int> Examples => new()
    {
        { "scs.box.single_3.reefer", "IconTrailerReefer", 1 },
        { "scs.box.double_3_2.curtain", "IconTrailerCurtain", 2 },
        { "scs.box.bdouble_3_3.dryvan", "IconTrailerBox", 2 },
        { "scs.box.hct_3_3_3.dryvan", "IconTrailerBox", 3 },
        { "scs.lowbed.single_41_ln.long", "IconTrailerLowbed", 1 },
        { "scs.lowloader.single_4_sh.short", "IconTrailerLowbed", 1 },
        { "scs.gooseneck.singl_3e_40.container", "IconTrailerContainer", 1 },
        { "scs.dumper.single_3.steel", "IconTrailerDumper", 1 },
        { "scs.silo.single_3_10.silo", "IconTrailerSilo", 1 },
        { "scs.flatbed.single_3.flatbed_w", "IconTrailerFlatbed", 1 },
        { "car_d.car_trans", "IconTrailerCarTransporter", 1 },
        { "glass.panel_trans.glass", "IconTrailerPanel", 1 },
        { "acme_mods.cargo_box.double_2_2.frozen", "IconTrailerGeneric", 2 },
        { "my_trailer", "IconTrailerGeneric", 1 },
    };

    [Theory]
    [MemberData(nameof(Examples))]
    public void Resolve_DesignExample_ReturnsKeyAndUnits(string id, string key, int units)
    {
        var d = TrailerDescriptor.Parse(id, null);

        var info = TrailerIcons.Resolve(d);

        Assert.Equal(new TrailerIconInfo(key, units), info);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("nil", "nil")]
    public void Resolve_NoDefinitionNorVariant_ReturnsNull(string? definition, string? variant)
    {
        var d = TrailerDescriptor.Parse(definition, variant);

        Assert.Null(TrailerIcons.Resolve(d));
    }

    [Fact]
    public void Resolve_OnlyVariant_ResolvesFromVariant()
    {
        var d = TrailerDescriptor.Parse("", "trailer.scs.box.single_3.reefer");

        Assert.Equal("IconTrailerReefer", TrailerIcons.Resolve(d)?.Key);
    }

    [Theory]
    [InlineData("zz_mod.unknown_thing.single_2.whatever")]
    [InlineData("zz_mod.foo.bar.baz")]
    [InlineData("zz_mod")]
    public void Resolve_UnknownTokens_ReturnsGeneric(string id)
    {
        var d = TrailerDescriptor.Parse(id, null);

        Assert.Equal("IconTrailerGeneric", TrailerIcons.Resolve(d)?.Key);
    }

    [Fact]
    public void Resolve_GlassPanelTrans_ReturnsPanelNotBodyOrFinishPosition()
    {
        var d = TrailerDescriptor.Parse("glass.panel_trans.glass", null);

        Assert.Equal("IconTrailerPanel", TrailerIcons.Resolve(d)?.Key);
    }

    [Fact]
    public void Resolve_TwoFinishTokens_LastFinishWins()
    {
        var d = TrailerDescriptor.Parse("x.box.reefer.curtain", null);

        Assert.Equal("IconTrailerCurtain", TrailerIcons.Resolve(d)?.Key);
    }

    [Fact]
    public void Resolve_TwoFinishTokensReversed_LastFinishWins()
    {
        var d = TrailerDescriptor.Parse("x.box.curtain.reefer", null);

        Assert.Equal("IconTrailerReefer", TrailerIcons.Resolve(d)?.Key);
    }

    [Fact]
    public void Resolve_TwoBodyTokens_FirstBodyWins()
    {
        var d = TrailerDescriptor.Parse("x.lowbed.flatbed", null);

        Assert.Equal("IconTrailerLowbed", TrailerIcons.Resolve(d)?.Key);
    }

    [Fact]
    public void Resolve_TwoBodyTokensReversed_FirstBodyWins()
    {
        var d = TrailerDescriptor.Parse("x.flatbed.lowbed", null);

        Assert.Equal("IconTrailerFlatbed", TrailerIcons.Resolve(d)?.Key);
    }

    [Fact]
    public void Resolve_FinishAndBody_FinishBeatsBody()
    {
        var d = TrailerDescriptor.Parse("x.dumper.reefer", null);

        Assert.Equal("IconTrailerReefer", TrailerIcons.Resolve(d)?.Key);
    }

    [Fact]
    public void Resolve_UpperCaseId_IsCaseInsensitive()
    {
        var d = TrailerDescriptor.Parse("SCS.BOX.DOUBLE_3_2.CURTAIN", null);

        Assert.Equal(new TrailerIconInfo("IconTrailerCurtain", 2), TrailerIcons.Resolve(d));
    }

    [Theory]
    [InlineData("...")]
    [InlineData("a..b")]
    [InlineData("  ")]
    [InlineData("scs.box.double_x_y.")]
    [InlineData("\"nil\"")]
    public void Resolve_OddInput_DoesNotThrow(string id)
    {
        var d = TrailerDescriptor.Parse(id, null);

        var ex = Record.Exception(() => TrailerIcons.Resolve(d));

        Assert.Null(ex);
    }
}
