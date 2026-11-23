using OwnExpeditor.Domain.Games;
using OwnExpeditor.Domain.Saves;

namespace OwnExpeditor.Core.Maps;

/// <summary>
/// Porta para o mapa do jogo, só com tipos do Domain/Core (o App não referencia a Data).
/// Nunca lança por formato de mapa desconhecido: devolve o status. Cancelamento lança <see cref="OperationCanceledException"/>.
/// </summary>
public interface IGameMapProvider
{
    /// <param name="progress">Reservado: o <see cref="IMapService"/> ainda não reporta progresso, então hoje é ignorado.</param>
    Task<GameMapResult> LoadCitiesAsync(GameKind game, ProfileInfo profile, IProgress<double>? progress, CancellationToken ct);
}
