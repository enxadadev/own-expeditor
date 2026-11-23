namespace OwnExpeditor.Domain.Maps;

/// <summary>Cidade do mapa. <c>Country</c> = token do país (def/city); <c>LabelOffsets</c> = deslocamento do rótulo por nível de zoom (X = map_x_offsets, Z = map_y_offsets).</summary>
public sealed record MapCity(
    string Token,
    MapPos Center,
    string? Country,
    int? Population,
    IReadOnlyList<MapPos> LabelOffsets);
