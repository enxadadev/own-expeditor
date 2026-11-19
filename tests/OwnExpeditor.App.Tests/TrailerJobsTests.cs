using OwnExpeditor.App.Controls;
using OwnExpeditor.App.Services;
using OwnExpeditor.App.Tests.Support;
using OwnExpeditor.Core.Text;
using OwnExpeditor.Domain.Games;
using OwnExpeditor.Domain.Settings;
using static OwnExpeditor.App.Tests.Support.JobsFixture;

namespace OwnExpeditor.App.Tests;

/// <summary>Reboque no módulo Fretes e na revisão: ícone é só decoração (design/nomes-reboques §7.9).</summary>
public class TrailerJobsTests
{
    private const string DoubleDefinition = "scs.box.double_3_2.curtain";

    private static async Task<JobsFixture> Open(Func<TrailerDescriptor, TrailerIconInfo?>? resolver = null, string? text = null)
    {
        var fx = new JobsFixture(Language.En, text);
        if (resolver is not null) fx.Page.IconResolver = resolver;
        return await fx.AttachAsync();
    }

    private static void ChooseRoute(JobsFixture fx)
    {
        if (fx.Page.ShowCountries) Pick(fx.Page.FromCountry, "bulgaria");
        Pick(fx.Page.FromCity, "sofia");
        Pick(fx.Page.FromCompany, JobsSample.Radus);
        if (fx.Page.ShowCountries) Pick(fx.Page.ToCountry, "turkey");
        Pick(fx.Page.ToCity, "istanbul");
        Pick(fx.Page.ToCompany, JobsSample.Lognstick);
    }

    private static async Task<JobsFixture> WithCargo(Func<TrailerDescriptor, TrailerIconInfo?>? resolver = null, string? text = null)
    {
        var fx = await Open(resolver, text);
        ChooseRoute(fx);
        Pick(fx.Page.Cargo, "machinery");
        return fx;
    }

    [Fact]
    public async Task CargoOptions_NullIconResolver_SameIdsOrderAndSearchTextAsDefault()
    {
        var withIcons = await Open();
        var withoutIcons = await Open(_ => null);
        ChooseRoute(withIcons);
        ChooseRoute(withoutIcons);

        var a = withIcons.Page.Cargo.AllOptions;
        var b = withoutIcons.Page.Cargo.AllOptions;

        Assert.NotEmpty(a);
        Assert.Equal(a.Select(o => o.Key), b.Select(o => o.Key));
        Assert.Equal(a.Select(o => o.Primary), b.Select(o => o.Primary));
        Assert.Equal(a.Select(o => o.Secondary), b.Select(o => o.Secondary));
    }

    [Fact]
    public async Task CargoOptions_DefaultResolver_CarryIconButNullResolverDoesNot()
    {
        var withIcons = await Open();
        var withoutIcons = await Open(_ => null);
        ChooseRoute(withIcons);
        ChooseRoute(withoutIcons);

        Assert.All(withIcons.Page.Cargo.AllOptions, o => Assert.NotNull(o.LeadingIcon));
        Assert.All(withoutIcons.Page.Cargo.AllOptions, o => Assert.Null(o.LeadingIcon));
    }

    [Fact]
    public async Task Search_ByIconKeyText_FindsNothing()
    {
        var fx = await Open();
        ChooseRoute(fx);

        fx.Page.Cargo.Query = "IconTrailer";

        Assert.Empty(fx.Page.Cargo.Items);
    }

    [Fact]
    public async Task TrailerIconKey_WithoutCargo_IsNullAndUnitsIsOne()
    {
        var fx = await Open();

        Assert.Null(fx.Page.TrailerIconKey);
        Assert.Equal(1, fx.Page.TrailerUnits);
    }

    [Fact]
    public async Task TrailerIconKey_WithCargo_IsResolvedKey()
    {
        var fx = await WithCargo(text: JobsSample.Text().Replace("trailer_def.x", "trailer_def.scs.box.single_3.reefer"));

        Assert.Equal("IconTrailerReefer", fx.Page.TrailerIconKey);
        Assert.Equal(1, fx.Page.TrailerUnits);
    }

    [Fact]
    public async Task TrailerUnits_DoubleChain_IsTwo()
    {
        var fx = await WithCargo(text: JobsSample.Text().Replace("trailer_def.x", "trailer_def." + DoubleDefinition));

        Assert.Equal("IconTrailerCurtain", fx.Page.TrailerIconKey);
        Assert.Equal(2, fx.Page.TrailerUnits);
    }

    [Fact]
    public async Task TrailerIconKey_NullResolver_IsNullAndUnitsIsOne()
    {
        var fx = await WithCargo(_ => null);

        Assert.Null(fx.Page.TrailerIconKey);
        Assert.Equal(1, fx.Page.TrailerUnits);
        Assert.NotEqual("—", fx.Page.TrailerValue);
    }

    [Fact]
    public async Task Review_TrailerLine_FillsIconsOnlyOnTrailerValues()
    {
        var fx = new ShellFixture();
        var session = await fx.OpenAsync(changes: 0);
        session.Stage(new OwnExpeditor.Core.Session.ChangeSet("jobs", "jobs:trailer", TextRef.Of("Jobs_Change_Title", "A", "B"),
        [
            new OwnExpeditor.Core.Session.DiffLine(TextRef.Of("Jobs_Field_Trailer"), new DiffValue.Trailer("scs.box.single_3.reefer", null), new DiffValue.Trailer(DoubleDefinition, null)),
            new OwnExpeditor.Core.Session.DiffLine(TextRef.Of("Jobs_Field_Destination"), new DiffValue.Raw("a"), new DiffValue.Raw("b")),
            new OwnExpeditor.Core.Session.DiffLine(TextRef.Of("Jobs_Field_Trailer"), new DiffValue.Empty(), new DiffValue.Trailer(DoubleDefinition, null)),
        ],
        [new OwnExpeditor.Core.Session.SetAttribute("_nameless.a", "target", "\"demo_co.city_b\"", "\"demo_co.city_x\"")]));

        var lines = fx.Shell.Review.Modules.Single().Changes.Single().Lines;

        Assert.Equal(("IconTrailerReefer", "IconTrailerCurtain"), (lines[0].BeforeIcon, lines[0].AfterIcon));
        Assert.Equal((null, null), (lines[1].BeforeIcon, lines[1].AfterIcon));
        Assert.Equal((null, "IconTrailerCurtain"), (lines[2].BeforeIcon, lines[2].AfterIcon));
    }
}
