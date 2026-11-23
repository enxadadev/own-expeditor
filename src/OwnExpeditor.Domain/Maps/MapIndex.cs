namespace OwnExpeditor.Domain.Maps;

public sealed record MapIndex(
    IReadOnlyList<MapCity> Cities,
    IReadOnlyList<MapCountry> Countries,
    IReadOnlyList<MapCompany> Companies,
    IReadOnlyList<MapPolyline> Roads,
    MapGraph? Graph);
