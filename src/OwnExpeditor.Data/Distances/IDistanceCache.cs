using OwnExpeditor.Domain.Games;
using OwnExpeditor.Domain.Saves;

namespace OwnExpeditor.Data.Distances;

/// <summary>
/// Tabela de distâncias conhecidas (<c>shortest_distance_km</c> de ofertas) agregada de todos os saves do usuário,
/// por jogo, em disco. Pares não direcionais; o primeiro valor visto vence. Ver docs/sii-format.md §10.
/// Distâncias que nós mesmos estimamos e gravamos num frete (<see cref="RecordEstimate"/>) ficam à parte, como
/// "estimativa própria": relidas de um save, não viram distância real.
/// </summary>
public interface IDistanceCache
{
    /// <summary>Lê o cache do disco (fora da thread que chama) para que <see cref="Load"/> não bloqueie; opcional.</summary>
    Task PreloadAsync(GameKind game, CancellationToken ct);

    /// <summary>Pares conhecidos do jogo: instantâneo imutável (não é recopiado a cada chamada); vazio se não há cache ou ele está ilegível.</summary>
    IReadOnlyDictionary<CityPair, int> Load(GameKind game);

    /// <summary>Estimativas próprias registradas (par → km): um valor igual a elas lido de um save não é distância real.</summary>
    IReadOnlyDictionary<CityPair, int> LoadEstimates(GameKind game);

    /// <summary>
    /// Acrescenta pares ausentes (não altera os existentes) e agenda a gravação (adiada, em segundo plano; falha de disco não lança).
    /// Par igual à estimativa própria é ignorado; valor real diferente o substitui e apaga a marca.
    /// </summary>
    void Merge(GameKind game, IEnumerable<KeyValuePair<CityPair, int>> pairs);

    /// <summary>Marca <paramref name="km"/> como estimativa própria do par (ignorado se o par já tem distância conhecida).</summary>
    void RecordEstimate(GameKind game, CityPair pair, int km);

    /// <summary>Grava já o que estiver pendente.</summary>
    Task FlushAsync(CancellationToken ct = default);

    /// <summary>
    /// Lê as ofertas dos saves informados e junta os pares ao cache. Saves já lidos e sem mudança
    /// (tamanho e data) são pulados; ilegíveis são ignorados. Retorna quantos pares novos entraram.
    /// </summary>
    Task<int> ScanAsync(GameKind game, IEnumerable<SaveSlotInfo> saves, CancellationToken ct);
}
