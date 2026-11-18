using OwnExpeditor.Core.Games;
using OwnExpeditor.Data;
using OwnExpeditor.Domain.Games;

namespace OwnExpeditor.Core.Legacy;

/// <summary>Catálogo do jogo carregado (adaptador de <c>GameCatalog</c> para <see cref="IGameData"/>). Apagado em F7.</summary>
public sealed class LegacyGameData : IGameData
{
    internal LegacyGameData(GameCatalog catalog, GameKind game = GameKind.Ets2)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        Catalog = catalog;
        Game = game;
    }

    internal GameCatalog Catalog { get; }

    public GameKind Game { get; }
    public bool IsAvailable => Catalog.Companies.Count > 0;

    public int CompanyCount => Catalog.Companies.Count;
    public int CargoCount => Catalog.Cargos.Count;

    /// <summary>Quantidade de textos traduzidos lidos de locale.scs (0 = não encontrado).</summary>
    public int TextCount => Catalog.Texts.Count;

    public string? TextLanguage => Catalog.Texts.Language;

    public string Get(GameNameKind kind, string id)
    {
        ArgumentNullException.ThrowIfNull(id);
        return kind switch
        {
            GameNameKind.City => Catalog.CityName(id),
            GameNameKind.Company => Catalog.CompanyName(id),
            GameNameKind.Cargo => Catalog.CargoName(id),
            _ => GameIds.Humanize(id),
        };
    }

    public bool Ships(string company, string cargo) => Catalog.Ships(company, cargo);

    public bool Accepts(string company, string cargo) => Catalog.Accepts(company, cargo);

    public IReadOnlyCollection<string> CargoOut(string company) =>
        Catalog.Companies.TryGetValue(company, out var c) ? c.CargoOut : [];
}
