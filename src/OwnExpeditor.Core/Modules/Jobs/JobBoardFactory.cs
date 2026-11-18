using OwnExpeditor.Core.Session;
using OwnExpeditor.Data.Distances;
using OwnExpeditor.Data.Saves;
using OwnExpeditor.Domain.Games;

namespace OwnExpeditor.Core.Modules.Jobs;

/// <summary>Monta o <see cref="JobBoard"/> da sessão com o cache de distâncias do usuário.</summary>
public interface IJobBoardFactory
{
    /// <summary>Board sobre <c>session.View</c>; as distâncias lidas do save entram no cache (crescem a cada save aberto).</summary>
    JobBoard Create(IEditSession session);

    /// <summary>
    /// Planeja o frete (<see cref="JobPlanner.Plan"/> sobre <c>session.Original</c>). Se a distância gravada é estimada,
    /// registra-a no cache como estimativa própria, para não ser relida do save como distância real.
    /// </summary>
    ChangeSet Plan(IEditSession session, JobBoard board, JobRequest request) =>
        JobPlanner.Plan(session.Original, board, request);

    /// <summary>Carrega o cache do disco fora da thread de chamada, antes do primeiro <see cref="Create"/>.</summary>
    Task WarmUpAsync(GameKind game, CancellationToken ct) => Task.CompletedTask;

    /// <summary>Lê os saves de todos os perfis do jogo e soma os pares ao cache (em segundo plano; refaça o board depois).</summary>
    Task<int> RefreshDistancesAsync(GameKind game, CancellationToken ct);
}

internal sealed class JobBoardFactory(IDistanceCache cache, IProfileLocator profiles) : IJobBoardFactory
{
    public JobBoard Create(IEditSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        var game = session.Game;
        var board = JobBoard.Build(session.View, session.GameData, cache.Load(game), cache.LoadEstimates(game));
        cache.Merge(game, board.DistancesFromSave);
        return board;
    }

    public ChangeSet Plan(IEditSession session, JobBoard board, JobRequest request)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(board);
        ArgumentNullException.ThrowIfNull(request);
        var change = JobPlanner.Plan(session.Original, board, request);
        var distance = board.Distance(request.Origin.City, request.Target.City);
        if (distance.Source == DistanceSource.Estimated)
            cache.RecordEstimate(session.Game, CityPair.Of(request.Origin.City, request.Target.City), distance.Km);
        return change;
    }

    public Task WarmUpAsync(GameKind game, CancellationToken ct) => cache.PreloadAsync(game, ct);

    public Task<int> RefreshDistancesAsync(GameKind game, CancellationToken ct) =>
        Task.Run(() =>
        {
            var saves = profiles.FindProfiles().Where(p => p.Game == game).SelectMany(profiles.FindSaves).ToList();
            return cache.ScanAsync(game, saves, ct);
        }, ct);
}
