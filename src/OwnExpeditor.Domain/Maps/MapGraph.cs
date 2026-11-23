namespace OwnExpeditor.Domain.Maps;

public sealed record MapGraph(IReadOnlyList<MapPos> Nodes, IReadOnlyList<MapEdge> Edges);
