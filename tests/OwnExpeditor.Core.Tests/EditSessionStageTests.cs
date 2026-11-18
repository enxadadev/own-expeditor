using OwnExpeditor.Core.Session;
using OwnExpeditor.Core.Text;

namespace OwnExpeditor.Core.Tests;

public class EditSessionStageTests
{
    private const string Company = "company.volatile.demo_co.city_a";

    private static IEditSession Session() => EditSessionFactory.FromText(SampleSave.Text, SampleSave.Slot());

    private static CoreError Code(Action act) => Assert.Throws<CoreException>(act).Code;

    [Fact]
    public void Stage_NewChange_AddsPendingAndRaisesChanged()
    {
        var session = Session();
        var raised = 0;
        session.Changed += (_, _) => raised++;
        var change = SampleSave.FillTarget();

        session.Stage(change);

        Assert.Equal([change], session.Pending);
        Assert.True(session.HasChanges);
        Assert.Equal(1, raised);
    }

    [Fact]
    public void Stage_SameKey_ReplacesPendingChange()
    {
        var session = Session();
        var first = SampleSave.FillTarget("\"demo_co.city_c\"");
        var second = SampleSave.FillTarget("\"demo_co.city_d\"");
        session.Stage(first);

        session.Stage(second);

        Assert.Equal([second], session.Pending);
        Assert.Equal("\"demo_co.city_d\"", session.View.Find("_nameless.b")!.Get("target"));
    }

    [Fact]
    public void Stage_SameKey_KeepsPositionInPending()
    {
        var session = Session();
        session.Stage(SampleSave.FillTarget(key: "k1"));
        session.Stage(SampleSave.Change("k2", "_nameless.a", "urgency", "0", "1"));

        var replacement = SampleSave.FillTarget("\"demo_co.city_d\"", key: "k1");
        session.Stage(replacement);

        Assert.Equal(["k1", "k2"], session.Pending.Select(p => p.Key));
        Assert.Equal(replacement.Id, session.Pending[0].Id);
    }

    [Fact]
    public void Stage_SameKey_ChecksOldValueAgainstBaseNotAgainstReplacedChange()
    {
        var session = Session();
        session.Stage(SampleSave.FillTarget("\"demo_co.city_c\""));
        var staleReplacement = SampleSave.Change("jobs:slot:b", "_nameless.b", "target", "\"demo_co.city_c\"", "\"demo_co.city_d\"");

        Assert.Equal(CoreError.StalePatch, Code(() => session.Stage(staleReplacement)));
    }

    [Fact]
    public void Stage_DifferentKeySameAttribute_ThrowsConflictAndKeepsPending()
    {
        var session = Session();
        var first = SampleSave.FillTarget(key: "k1");
        session.Stage(first);

        var code = Code(() => session.Stage(SampleSave.FillTarget("\"demo_co.city_d\"", key: "k2")));

        Assert.Equal(CoreError.Conflict, code);
        Assert.Equal([first], session.Pending);
    }

    [Fact]
    public void Stage_OldValueDiffersFromSave_ThrowsStalePatch()
    {
        var session = Session();

        var code = Code(() => session.Stage(SampleSave.Change("k", "_nameless.b", "target", "\"other\"", "\"x\"")));

        Assert.Equal(CoreError.StalePatch, code);
        Assert.False(session.HasChanges);
    }

    [Fact]
    public void Stage_UnknownUnit_ThrowsUnknownUnit() =>
        Assert.Equal(CoreError.UnknownUnit,
            Code(() => Session().Stage(SampleSave.Change("k", "_nameless.zzz", "target", "a", "b"))));

    [Fact]
    public void Stage_UnknownAttribute_ThrowsUnknownAttribute() =>
        Assert.Equal(CoreError.UnknownAttribute,
            Code(() => Session().Stage(SampleSave.Change("k", "_nameless.b", "nope", "a", "b"))));

    [Fact]
    public void Stage_ChangeWithoutPatches_ThrowsInvalidEdit()
    {
        var empty = new ChangeSet("jobs", "k", TextRef.Of("t"), [], []);

        Assert.Equal(CoreError.InvalidEdit, Code(() => Session().Stage(empty)));
    }

    [Fact]
    public void Stage_SecondPatchInvalid_LeavesViewUntouched()
    {
        var session = Session();
        var change = new ChangeSet("jobs", "k", TextRef.Of("t"), [],
        [
            new SetAttribute("_nameless.b", "target", "\"\"", "\"x\""),
            new SetAttribute("_nameless.b", "nope", "a", "b"),
        ]);

        Assert.Equal(CoreError.UnknownAttribute, Code(() => session.Stage(change)));

        Assert.Equal("\"\"", session.View.Find("_nameless.b")!.Get("target"));
        Assert.False(session.HasChanges);
    }

    [Fact]
    public void Stage_TwoPatchesOnSameAttributeInOneChange_AppliesInSequence()
    {
        var session = Session();
        var change = new ChangeSet("jobs", "k", TextRef.Of("t"), [],
        [
            new SetAttribute("_nameless.b", "target", "\"\"", "\"x\""),
            new SetAttribute("_nameless.b", "target", "\"x\"", "\"y\""),
        ]);

        session.Stage(change);

        Assert.Equal("\"y\"", session.View.Find("_nameless.b")!.Get("target"));
    }

    [Fact]
    public void Stage_ChangesOnDifferentAttributes_BothAccepted()
    {
        var session = Session();

        session.Stage(SampleSave.FillTarget(key: "k1"));
        session.Stage(SampleSave.Change("k2", "_nameless.a", "urgency", "0", "2"));

        Assert.Equal(2, session.Pending.Count);
    }

    [Fact]
    public void Stage_Null_ThrowsArgumentNull() =>
        Assert.Throws<ArgumentNullException>(() => Session().Stage(null!));

    [Fact]
    public void Discard_PendingChange_RemovesItAndRaisesChanged()
    {
        var session = Session();
        var change = SampleSave.FillTarget();
        session.Stage(change);
        var raised = 0;
        session.Changed += (_, _) => raised++;

        var removed = session.Discard(change.Id);

        Assert.True(removed);
        Assert.False(session.HasChanges);
        Assert.Equal(1, raised);
    }

    [Fact]
    public void Discard_UnknownId_ReturnsFalseWithoutEvent()
    {
        var session = Session();
        session.Stage(SampleSave.FillTarget());
        var raised = 0;
        session.Changed += (_, _) => raised++;

        Assert.False(session.Discard(Guid.NewGuid()));
        Assert.Equal(0, raised);
        Assert.Single(session.Pending);
    }

    [Fact]
    public void DiscardModule_RemovesOnlyThatModule()
    {
        var session = Session();
        session.Stage(SampleSave.FillTarget(key: "a"));
        var other = SampleSave.Change("b", "_nameless.a", "urgency", "0", "1", module: "other");
        session.Stage(other);

        session.DiscardModule("jobs");

        Assert.Equal([other], session.Pending);
    }

    [Fact]
    public void DiscardAll_ClearsPendingAndRaisesChangedOnce()
    {
        var session = Session();
        session.Stage(SampleSave.FillTarget(key: "a"));
        session.Stage(SampleSave.Change("b", "_nameless.a", "urgency", "0", "1", module: "other"));
        var raised = 0;
        session.Changed += (_, _) => raised++;

        session.DiscardAll();

        Assert.Empty(session.Pending);
        Assert.Equal(1, raised);
    }

    [Fact]
    public void DiscardAll_NothingPending_DoesNotRaiseChanged()
    {
        var session = Session();
        var raised = 0;
        session.Changed += (_, _) => raised++;

        session.DiscardAll();

        Assert.Equal(0, raised);
    }

    [Fact]
    public void Pending_ReturnsSnapshot()
    {
        var session = Session();
        var before = session.Pending;
        session.Stage(SampleSave.FillTarget());

        Assert.Empty(before);
        Assert.Single(session.Pending);
    }

    [Fact]
    public void GetDiff_NoChanges_IsEmpty()
    {
        var diff = Session().GetDiff();

        Assert.Empty(diff.Modules);
        Assert.Equal(0, diff.PatchCount);
    }

    [Fact]
    public void GetDiff_GroupsByModuleInStagingOrderAndCountsPatches()
    {
        var session = Session();
        var a = SampleSave.FillTarget(key: "a");
        var b = SampleSave.Change("b", "_nameless.a", "urgency", "0", "1", module: "other");
        var c = SampleSave.Change("c", Company, "job_offer[0]", "_nameless.a", "_nameless.b");
        session.Stage(a);
        session.Stage(b);
        session.Stage(c);

        var diff = session.GetDiff();

        Assert.Equal(["jobs", "other"], diff.Modules.Select(m => m.ModuleId));
        Assert.Equal([a, c], diff.Modules[0].Changes);
        Assert.Equal([b], diff.Modules[1].Changes);
        Assert.Equal(3, diff.PatchCount);
    }

    [Fact]
    public void GetDiff_ReflectsReplacement()
    {
        var session = Session();
        session.Stage(SampleSave.FillTarget("\"demo_co.city_c\""));
        var replacement = SampleSave.FillTarget("\"demo_co.city_d\"");
        session.Stage(replacement);

        var diff = session.GetDiff();

        Assert.Equal([replacement], Assert.Single(diff.Modules).Changes);
        Assert.Equal(1, diff.PatchCount);
    }
}
