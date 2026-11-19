using OwnExpeditor.Domain.Games;

namespace OwnExpeditor.Core.Games;

public interface IGameData : IGameNames
{
    GameKind Game { get; }
    bool IsAvailable { get; }
    bool Ships(string company, string cargo);
    bool Accepts(string company, string cargo);
    IReadOnlyCollection<string> CargoOut(string company);

    /// <summary>Id do país da cidade; null se desconhecido.</summary>
    string? CityCountry(string city);
}
