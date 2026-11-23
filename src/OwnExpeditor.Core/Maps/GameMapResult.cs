using OwnExpeditor.Domain.Maps;

namespace OwnExpeditor.Core.Maps;

/// <summary>Índice do mapa do jogo e o motivo de não haver um (<see cref="MapStatus"/>).</summary>
public sealed record GameMapResult(MapStatus Status, MapIndex? Index);
