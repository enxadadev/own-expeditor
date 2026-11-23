using OwnExpeditor.Domain.Maps;

namespace OwnExpeditor.Data.Tests.Maps;

internal static class MapTestData
{
    public static MapIndex SampleIndex() => new(
        Cities:
        [
            new MapCity("city_a", new MapPos(100.5f, -40.25f), "country_x", 12000, [new MapPos(0, 0), new MapPos(-3, -44)]),
            new MapCity("city_b", new MapPos(-7000f, 3000f), null, null, []),
        ],
        Countries: [new MapCountry("country_x", "Pais X", new MapPos(15500f, -51000f))],
        Companies: [new MapCompany("co_one", "city_a", new MapPos(110f, -30f), 7)],
        Roads: [new MapPolyline(MapRoadClass.Motorway, 1, [new MapPos(0, 0), new MapPos(10, 10), new MapPos(20, 5)])],
        Graph: new MapGraph([new MapPos(0, 0), new MapPos(10, 10)], [new MapEdge(0, 1, 14.1f, 1f, MapRoadClass.Main, 0)]));

    public static void AssertEquivalent(MapIndex expected, MapIndex actual)
    {
        Assert.Equal(expected.Cities.Count, actual.Cities.Count);
        for (var i = 0; i < expected.Cities.Count; i++)
        {
            var (e, a) = (expected.Cities[i], actual.Cities[i]);
            Assert.Equal((e.Token, e.Center, e.Country, e.Population), (a.Token, a.Center, a.Country, a.Population));
            Assert.Equal(e.LabelOffsets, a.LabelOffsets);
        }
        Assert.Equal(expected.Countries, actual.Countries);
        Assert.Equal(expected.Companies, actual.Companies);
        Assert.Equal(expected.Roads.Count, actual.Roads.Count);
        for (var i = 0; i < expected.Roads.Count; i++)
        {
            var (e, a) = (expected.Roads[i], actual.Roads[i]);
            Assert.Equal((e.Class, e.Lod), (a.Class, a.Lod));
            Assert.Equal(e.Points, a.Points);
        }
        Assert.NotNull(actual.Graph);
        Assert.Equal(expected.Graph!.Nodes, actual.Graph!.Nodes);
        Assert.Equal(expected.Graph.Edges, actual.Graph.Edges);
    }
}
