using OwnExpeditor.Core.Session;
using OwnExpeditor.Core.Text;
using OwnExpeditor.Data;
using OwnExpeditor.Domain.Games;
using OwnExpeditor.Sii;

namespace OwnExpeditor.Core.Tests;

public class CoreContractsTests
{
    [Fact]
    public void TextRef_compares_args_by_value()
    {
        Assert.Equal(TextRef.Of("k", 1), TextRef.Of("k", 1));
        Assert.Equal(TextRef.Of("k", 1).GetHashCode(), TextRef.Of("k", 1).GetHashCode());
        Assert.NotEqual(TextRef.Of("k", 1), TextRef.Of("k", 2));
        Assert.NotEqual(TextRef.Of("k", 1), TextRef.Of("j", 1));
        Assert.NotEqual(TextRef.Plural("k", 1, 1), TextRef.Of("k", 1));
        Assert.Equal(TextRef.Plural("k", 3), TextRef.Plural("k", 3));
    }

    private static ChangeSet Change(Guid id, string newValue) => new("jobs", "jobs:slot:1", TextRef.Of("t", 1),
        [new DiffLine(TextRef.Of("l"), new DiffValue.Raw("a"), new DiffValue.Raw(newValue))],
        [new SetAttribute("u", "target", "a", newValue)])
    { Id = id };

    [Fact]
    public void ChangeSet_compares_content_by_value()
    {
        var id = Guid.NewGuid();
        Assert.Equal(Change(id, "b"), Change(id, "b"));
        Assert.Equal(Change(id, "b").GetHashCode(), Change(id, "b").GetHashCode());
        Assert.NotEqual(Change(id, "b"), Change(id, "c"));
        Assert.NotEqual(Change(id, "b"), Change(Guid.NewGuid(), "b"));
    }
}
