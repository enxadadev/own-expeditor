namespace OwnExpeditor.App.Controls;

/// <summary>
/// Ponto do <see cref="RouteMap"/>: nome e, se houver, posição em metros do mundo do jogo (X para leste, Y = -Z para norte).
/// <c>Population</c> só pesa no tamanho do pino e na prioridade do nome de cidades de fundo.
/// </summary>
public sealed record MapPoint(string Label, double? X = null, double? Y = null, int? Population = null)
{
    public bool HasCoordinates => X is not null && Y is not null;
}
