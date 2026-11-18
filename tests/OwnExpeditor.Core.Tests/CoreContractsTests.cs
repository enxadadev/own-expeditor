using OwnExpeditor.Core.Legacy;
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

    [Fact]
    public void LegacyGameData_adapts_catalog()
    {
        var cat = new GameCatalog();
        var kaarfor = new CompanyDef("kaarfor") { DisplayName = "Kaarfor" };
        kaarfor.CargoOut.Add("used_plast");
        var dfh = new CompanyDef("dfh");
        dfh.CargoIn.Add("used_plast");
        cat.Companies["kaarfor"] = kaarfor;
        cat.Companies["dfh"] = dfh;
        var data = new LegacyGameData(cat);

        Assert.True(data.IsAvailable);
        Assert.Equal(GameKind.Ets2, data.Game);
        Assert.Equal("Kaarfor", data.Get(GameNameKind.Company, "kaarfor"));
        Assert.Equal("Plumb suppl", data.Get(GameNameKind.Cargo, "plumb_suppl"));
        Assert.Equal("Mercedes actros", data.Get(GameNameKind.Truck, "mercedes_actros"));
        Assert.True(data.Ships("kaarfor", "used_plast"));
        Assert.True(data.Accepts("dfh", "used_plast"));
        Assert.Equal(["used_plast"], data.CargoOut("kaarfor"));
        Assert.Empty(data.CargoOut("desconhecida"));
        Assert.False(new LegacyGameData(new GameCatalog()).IsAvailable);
    }

    [Fact]
    public void SaveSession_accepts_IGameData()
    {
        var cat = new GameCatalog();
        var k = new CompanyDef("kaarfor");
        k.CargoOut.Add("used_plast");
        cat.Companies["kaarfor"] = k;
        var save = "SiiNunit\r\n{\r\neconomy : _nameless.1 {\r\n game_time: 1000\r\n}\r\n\r\n" +
                   "company : company.volatile.kaarfor.budapest {\r\n job_offer: 0\r\n}\r\n\r\n}\r\n";
        var session = new SaveSession(SiiDocument.Parse(save), (OwnExpeditor.Core.Games.IGameData)new LegacyGameData(cat));
        Assert.Empty(session.Origins());
        Assert.Equal("Used plast", session.CargoName("used_plast"));
    }
}
