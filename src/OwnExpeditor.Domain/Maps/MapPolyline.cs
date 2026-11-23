namespace OwnExpeditor.Domain.Maps;

public sealed record MapPolyline(MapRoadClass Class, int Lod, IReadOnlyList<MapPos> Points);
