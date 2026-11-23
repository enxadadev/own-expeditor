namespace OwnExpeditor.Domain.Maps;

public sealed record MapEdge(int From, int To, float LengthM, float Scale, MapRoadClass Class, int Polyline);
