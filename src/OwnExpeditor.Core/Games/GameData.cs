using OwnExpeditor.Data;
using OwnExpeditor.Domain.Games;

namespace OwnExpeditor.Core.Games;

/// <summary>
/// Dados de um jogo: regras do catálogo (def.scs + DLCs) e nomes no idioma do app.
/// Nome = tradução → nome local da definição → id legível. Imutável; uma troca de idioma gera outra instância.
/// </summary>
internal sealed class GameData(GameKind game, GameCatalog catalog, Localization texts) : IGameData
{
    public GameKind Game { get; } = game;

    public bool IsAvailable => true;

    public string Get(GameNameKind kind, string id)
    {
        ArgumentNullException.ThrowIfNull(id);
        return kind switch
        {
            GameNameKind.City => CityName(id),
            GameNameKind.Company => Filled(catalog.Companies.GetValueOrDefault(id)?.DisplayName) ?? GameIds.Humanize(id),
            GameNameKind.Cargo => Filled(texts.Resolve(catalog.Cargos.GetValueOrDefault(id)?.LocalizationKey)) ?? GameIds.Humanize(id),
            GameNameKind.Country => CountryName(id),
            _ => GameIds.Humanize(id),
        };
    }

    public bool Ships(string company, string cargo) => catalog.Ships(company, cargo);

    public bool Accepts(string company, string cargo) => catalog.Accepts(company, cargo);

    public string? CityCountry(string city) =>
        Filled(catalog.Cities.GetValueOrDefault(city)?.Country);

    public IReadOnlyCollection<string> CargoOut(string company) =>
        catalog.Companies.TryGetValue(company, out var c) ? c.CargoOut : [];

    /// <summary>Mesmas regras, nomes de outro idioma (troca de idioma não relê as definições).</summary>
    public GameData WithTexts(Localization newTexts) => new(Game, catalog, newTexts);

    private string CityName(string id)
    {
        var city = catalog.Cities.GetValueOrDefault(id);
        return Filled(texts.Resolve(city?.LocalizationKey)) ?? Filled(city?.NativeName) ?? GameIds.Humanize(id);
    }

    /// <summary>Mesma cadeia de <see cref="GameCatalog.CountryName"/>, mas com os textos do idioma desta instância.</summary>
    private string CountryName(string id)
    {
        var country = catalog.Countries.GetValueOrDefault(id);
        return Filled(texts.Resolve(country?.LocalizationKey)) ?? Filled(country?.NativeName) ?? GameIds.Humanize(id);
    }

    private static string? Filled(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
