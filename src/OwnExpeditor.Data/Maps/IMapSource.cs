namespace OwnExpeditor.Data.Maps;

public interface IMapSource
{
    /// <summary>Carrega o índice do mapa (cache ou setores). Falhas esperadas viram <c>MapStatus</c>, não exceção.</summary>
    Task<MapLoadResult> LoadAsync(MapRequest request, IProgress<double>? progress, CancellationToken ct);
}
