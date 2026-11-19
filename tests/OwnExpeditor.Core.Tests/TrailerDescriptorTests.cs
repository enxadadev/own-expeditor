using OwnExpeditor.Core.Games;
using OwnExpeditor.Domain.Games;

namespace OwnExpeditor.Core.Tests;

public class TrailerDescriptorTests
{
    [Theory]
    [InlineData("scs.box.single_3.reefer", TrailerChain.Single, "3", TrailerLength.Unknown, "box,reefer", "box", "reefer")]
    [InlineData("scs.box.double_3_2.curtain", TrailerChain.Double, "3,2", TrailerLength.Unknown, "box,curtain", "box", "curtain")]
    [InlineData("scs.box.bdouble_3_3.dryvan", TrailerChain.BDouble, "3,3", TrailerLength.Unknown, "box,dryvan", "box", "dryvan")]
    [InlineData("scs.lowbed.single_41_ln.long", TrailerChain.Single, "", TrailerLength.Long, "lowbed,long", "lowbed", null)]
    [InlineData("scs.lowloader.single_4_sh.short", TrailerChain.Single, "4", TrailerLength.Short, "lowloader,short", "lowloader", null)]
    [InlineData("scs.gooseneck.singl_3e_40.container", TrailerChain.Single, "3", TrailerLength.Unknown, "gooseneck,container", "gooseneck", "container")]
    [InlineData("scs.dumper.single_3.steel", TrailerChain.Single, "3", TrailerLength.Unknown, "dumper,steel", "dumper", "steel")]
    [InlineData("scs.silo.single_3_10.silo", TrailerChain.Single, "3", TrailerLength.Unknown, "silo,silo", "silo", "silo")]
    [InlineData("scs.flatbed.single_3.flatbed_w", TrailerChain.Single, "3", TrailerLength.Unknown, "flatbed,flatbed_w", "flatbed", "flatbed_w")]
    [InlineData("car_d.car_trans", TrailerChain.Unknown, "", TrailerLength.Unknown, "car_trans", "car_trans", null)]
    [InlineData("glass.panel_trans.glass", TrailerChain.Unknown, "", TrailerLength.Unknown, "panel_trans,glass", "panel_trans", "glass")]
    [InlineData("acme_mods.cargo_box.double_2_2.frozen", TrailerChain.Double, "2,2", TrailerLength.Unknown, "cargo_box,frozen", "cargo_box", "frozen")]
    [InlineData("my_trailer", TrailerChain.Unknown, "", TrailerLength.Unknown, "my_trailer", "my_trailer", null)]
    [InlineData("scs.box.hct_3_2_3.curtain", TrailerChain.Hct, "3,2,3", TrailerLength.Unknown, "box,curtain", "box", "curtain")]
    [InlineData("scs.box.double_3_41.curtain", TrailerChain.Double, "", TrailerLength.Unknown, "box,curtain", "box", "curtain")]
    public void Parse_follows_the_design_rule(string id, TrailerChain chain, string axles, TrailerLength length,
        string tokens, string body, string? finish)
    {
        var d = TrailerDescriptor.Parse("trailer_def." + id, null);

        Assert.Equal(id, d.Definition);
        Assert.Equal(chain, d.Chain);
        Assert.Equal(axles, string.Join(',', d.AxleGroups));
        Assert.Equal(length, d.Length);
        Assert.Equal(tokens, string.Join(',', d.Tokens));
        Assert.Equal(body, d.Body);
        Assert.Equal(finish, d.Finish);
    }

    [Fact]
    public void Parse_exposes_owner_and_total_axles()
    {
        var d = TrailerDescriptor.Parse("trailer_def.scs.box.bdouble_3_3.dryvan", null);
        Assert.Equal("scs", d.Owner);
        Assert.Equal(6, d.TotalAxles);
        Assert.Null(TrailerDescriptor.Parse("my_trailer", null).Owner);
    }

    [Fact]
    public void Parse_falls_back_to_variant_when_definition_is_missing()
    {
        var d = TrailerDescriptor.Parse(null, "trailer.scs_flat_b");
        Assert.Equal("scs_flat_b", d.Body);
        Assert.Equal("trailer.scs_flat_b", d.Variant);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("nil")]
    [InlineData("trailer_def.")]
    [InlineData("..")]
    [InlineData("scs..double__")]
    [InlineData("\"scs.box.single_3.reefer\"")]
    public void Parse_never_throws(string? definition)
    {
        var d = TrailerDescriptor.Parse(definition, null);
        Assert.NotNull(d.AxleGroups);
        Assert.NotNull(d.Tokens);
    }

    [Fact]
    public void Parse_rejects_non_ascii_digits_as_axle_group()
    {
        var d = TrailerDescriptor.Parse("trailer_def.scs.box.single_\u0663.dryvan", null);
        Assert.Empty(d.AxleGroups);
    }

    [Theory]
    [InlineData("trailer_def.scs.box.single_3.dryvan.Long", TrailerLength.Long)]
    [InlineData("trailer_def.scs.box.single_3.dryvan.SHORT", TrailerLength.Short)]
    [InlineData("trailer_def.scs.box.single_3_SH.dryvan", TrailerLength.Short)]
    [InlineData("trailer_def.scs.box.single_3_LN.dryvan", TrailerLength.Long)]
    public void Parse_length_is_case_insensitive(string id, TrailerLength expected) =>
        Assert.Equal(expected, TrailerDescriptor.Parse(id, null).Length);

    [Fact]
    public void Default_interface_method_parses() =>
        Assert.Equal(TrailerChain.Double, ((IGameNames)new Names()).Trailer("trailer_def.scs.box.double_3_2.curtain", null).Chain);

    private sealed class Names : IGameNames
    {
        public string Get(GameNameKind kind, string id) => id;
    }
}
