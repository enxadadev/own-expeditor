using OwnExpeditor.Core.Session;
using OwnExpeditor.Domain.Games;

namespace OwnExpeditor.Core.Tests;

public class SaveViewTests
{
    private static IEditSession Session() => EditSessionFactory.FromText(SampleSave.Text, SampleSave.Slot());

    [Fact]
    public void Find_ExistingUnit_ExposesTypeIdAndAttributes()
    {
        var unit = Session().Original.Find("_nameless.a");

        Assert.NotNull(unit);
        Assert.Equal("job_offer_data", unit.Type);
        Assert.Equal("_nameless.a", unit.Id);
        Assert.Equal("\"demo_co.city_b\"", unit.Get("target"));
    }

    [Fact]
    public void Find_UnknownUnit_ReturnsNull() =>
        Assert.Null(Session().Original.Find("_nameless.zzz"));

    [Fact]
    public void Get_MissingAttribute_ReturnsNull() =>
        Assert.Null(Session().Original.Find("_nameless.a")!.Get("nope"));

    [Fact]
    public void GetArray_ReturnsValuesInOrder() =>
        Assert.Equal(["_nameless.a", "_nameless.b"],
            Session().Original.Find("company.volatile.demo_co.city_a")!.GetArray("job_offer"));

    [Fact]
    public void GetArray_MissingAttribute_ReturnsEmpty() =>
        Assert.Empty(Session().Original.Find("_nameless.a")!.GetArray("nope"));

    [Fact]
    public void OfType_ReturnsUnitsInFileOrder() =>
        Assert.Equal(["_nameless.a", "_nameless.b"],
            Session().Original.OfType("job_offer_data").Select(u => u.Id));

    [Fact]
    public void OfType_UnknownType_ReturnsEmpty() =>
        Assert.Empty(Session().Original.OfType("nothing"));

    [Fact]
    public void Find_SameUnitTwice_ReturnsSameInstance()
    {
        var view = Session().Original;

        Assert.Same(view.Find("_nameless.a"), view.OfType("job_offer_data")[0]);
    }

    [Fact]
    public void Find_PendingChangeHidesInViewButNotInOriginal()
    {
        var session = Session();
        session.Stage(SampleSave.FillTarget("\"demo_co.city_c\""));

        Assert.Equal("\"demo_co.city_c\"", session.View.Find("_nameless.b")!.Get("target"));
        Assert.Equal("\"\"", session.Original.Find("_nameless.b")!.Get("target"));
    }

    [Fact]
    public void OfType_InViewReflectsPendingValues()
    {
        var session = Session();
        session.Stage(SampleSave.FillTarget());

        Assert.Equal(["\"demo_co.city_b\"", "\"demo_co.city_c\""],
            session.View.OfType("job_offer_data").Select(u => u.Get("target")));
    }

    [Fact]
    public void GetArray_InViewReflectsPendingValues()
    {
        var session = Session();
        session.Stage(SampleSave.Change("k", "company.volatile.demo_co.city_a", "job_offer[1]", "_nameless.b", "_nameless.a"));

        Assert.Equal(["_nameless.a", "_nameless.a"],
            session.View.Find("company.volatile.demo_co.city_a")!.GetArray("job_offer"));
        Assert.Equal(["_nameless.a", "_nameless.b"],
            session.Original.Find("company.volatile.demo_co.city_a")!.GetArray("job_offer"));
    }

    [Fact]
    public void View_AfterDiscard_ReturnsToOriginalValues()
    {
        var session = Session();
        var change = SampleSave.FillTarget();
        session.Stage(change);
        session.Discard(change.Id);

        Assert.Equal("\"\"", session.View.Find("_nameless.b")!.Get("target"));
    }

    [Fact]
    public void FromText_Metadata_ComesFromSlotAndIsReadOnly()
    {
        var slot = SampleSave.Slot(GameKind.Ats);
        var session = EditSessionFactory.FromText(SampleSave.Text, slot);

        Assert.True(session.IsReadOnly);
        Assert.Equal(GameKind.Ats, session.Game);
        Assert.Equal(slot, session.Save);
        Assert.False(session.HasChanges);
        Assert.Empty(session.Pending);
    }

    [Fact]
    public void FromText_WithoutGameData_NamesFallBackToReadableId()
    {
        var session = EditSessionFactory.FromText(SampleSave.Text, SampleSave.Slot());

        Assert.False(session.GameData.IsAvailable);
        Assert.Equal("Demo co", session.Names.Get(GameNameKind.Company, "demo_co"));
    }

    [Fact]
    public void FromText_WithGameData_UsesIt()
    {
        var data = new FakeGameData();
        var session = EditSessionFactory.FromText(SampleSave.Text, SampleSave.Slot(), data);

        Assert.Same(data, session.GameData);
        Assert.Equal("City:x", session.Names.Get(GameNameKind.City, "x"));
    }

    [Theory]
    [InlineData("", CoreError.SaveCorrupt)]
    [InlineData("SiiNunit\r\n{\r\neconomy : _nameless.1 {\r\n a: 1\r\n", CoreError.SaveCorrupt)]
    [InlineData("SiiNunit\r\n{\r\neconomy : _nameless.1 {\r\n???\r\n}\r\n}\r\n", CoreError.SaveParseFailed)]
    public void FromText_InvalidText_ThrowsTypedError(string text, CoreError expected)
    {
        var e = Assert.Throws<CoreException>(() => EditSessionFactory.FromText(text, SampleSave.Slot()));

        Assert.Equal(expected, e.Code);
        Assert.IsType<OwnExpeditor.Sii.SiiFormatException>(e.InnerException);
    }

    [Fact]
    public async Task ApplyAsync_ReadOnlySession_ThrowsReadOnlySession()
    {
        var session = Session();
        session.Stage(SampleSave.FillTarget());

        var e = await Assert.ThrowsAsync<CoreException>(() => session.ApplyAsync(new ApplyTarget.Overwrite(), default));

        Assert.Equal(CoreError.ReadOnlySession, e.Code);
    }
}
