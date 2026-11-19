using OwnExpeditor.App.Services;
using OwnExpeditor.Core.Text;
using OwnExpeditor.Domain.Games;
using OwnExpeditor.Domain.Settings;

namespace OwnExpeditor.App.Tests;

public class TrailerTextTests
{
    public static TheoryData<string, string, string, string, string> Cases => new()
    {
        { "scs.box.single_3.reefer", "Baú refrigerado", "3 eixos", "Reefer", "3 axles" },
        { "scs.box.double_3_2.curtain", "Sider · Rodotrem", "3+2 eixos", "Curtainsider · Double", "3+2 axles" },
        { "scs.box.bdouble_3_3.dryvan", "Baú seco · Bitrem", "3+3 eixos", "Dry van · B-double", "3+3 axles" },
        { "scs.lowbed.single_41_ln.long", "Prancha rebaixada", "longo", "Lowbed", "long" },
        { "scs.lowloader.single_4_sh.short", "Prancha baixa", "4 eixos · curto", "Low loader", "4 axles · short" },
        { "scs.gooseneck.singl_3e_40.container", "Porta-contêiner", "3 eixos", "Container chassis", "3 axles" },
        { "scs.dumper.single_3.steel", "Basculante", "3 eixos", "Dump trailer", "3 axles" },
        { "scs.silo.single_3_10.silo", "Silo", "3 eixos", "Silo trailer", "3 axles" },
        { "scs.flatbed.single_3.flatbed_w", "Plataforma", "3 eixos", "Flatbed", "3 axles" },
        { "car_d.car_trans", "Transporte de carros", "", "Car transporter", "" },
        { "glass.panel_trans.glass", "Transporte de painéis", "", "Panel transporter", "" },
        { "acme_mods.cargo_box.double_2_2.frozen", "Cargo box · Rodotrem", "2+2 eixos", "Cargo box · Double", "2+2 axles" },
        { "scs.box.hct_3_3_3.dryvan", "Baú seco · HCT", "3+3+3 eixos", "Dry van · HCT", "3+3+3 axles" },
        { "my_trailer", "My trailer", "", "My trailer", "" },
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void Format_TableFromDesign(string id, string ptName, string ptDetails, string enName, string enDetails)
    {
        var pt = TrailerText.Format(new Localizer(Language.PtBr), TrailerDescriptor.Parse("trailer_def." + id, null));
        var en = TrailerText.Format(new Localizer(Language.En), TrailerDescriptor.Parse("trailer_def." + id, null));

        Assert.Equal((ptName, ptDetails, id), (pt.Name, pt.Details, pt.RawId));
        Assert.Equal((enName, enDetails), (en.Name, en.Details));
    }

    [Fact]
    public void TryGet_MissingKey_ReturnsFalseWithoutMarker()
    {
        var loc = new Localizer(Language.En);

        Assert.False(loc.TryGet("No_Such_Key", out var value));
        Assert.Null(value);
        Assert.True(loc.TryGet("Common_Cancel", out var cancel));
        Assert.Equal("Cancel", cancel);
    }

    [Fact]
    public void Format_DiffValueTrailer_ShowsNameWithDetailsInParentheses()
    {
        var loc = new Localizer(Language.PtBr);

        var text = loc.Format(new DiffValue.Trailer("trailer_def.scs.box.bdouble_3_3.dryvan", "trailer.scs_box_b"));

        Assert.Equal("Baú seco · Bitrem (3+3 eixos)", text);
    }

    [Fact]
    public void Format_DiffValueTrailer_WithoutDetails_ShowsOnlyName()
    {
        var loc = new Localizer(Language.En);

        Assert.Equal("Car transporter", loc.Format(new DiffValue.Trailer("trailer_def.car_d.car_trans", null)));
    }

    [Fact]
    public void Format_DiffValueTrailer_NeverShowsMissingKeyMarker()
    {
        var loc = new Localizer(Language.En);

        Assert.DoesNotContain("!", loc.Format(new DiffValue.Trailer("trailer_def.x.weird.single_9_9.zzz", null)));
    }
}
