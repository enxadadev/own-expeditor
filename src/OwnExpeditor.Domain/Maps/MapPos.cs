namespace OwnExpeditor.Domain.Maps;

/// <summary>Posição em metros do mundo do jogo; a tela usa (X, -Z).</summary>
public readonly record struct MapPos(float X, float Z);
