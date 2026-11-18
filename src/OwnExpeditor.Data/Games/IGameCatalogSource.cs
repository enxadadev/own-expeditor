using OwnExpeditor.Domain.Saves;

namespace OwnExpeditor.Data.Games;

/// <summary>Carrega catálogo e textos da instalação do jogo com cache por (pasta, data dos .scs).</summary>
public interface IGameCatalogSource
{
    /// <summary>Empresas, cidades e cargas de <c>def.scs</c> + <c>dlc_*.scs</c> (sem textos).</summary>
    GameCatalog LoadDefinitions(string installDir);

    /// <summary>Textos do primeiro idioma disponível da lista (com fallback en_us/en_gb); vazio se nenhum.</summary>
    Localization LoadTexts(string installDir, IReadOnlyList<string> languages);

    /// <summary>Idioma do jogo (<c>config.cfg</c>, <c>g_lang_init</c>) para o perfil; null se desconhecido.</summary>
    string? GameLanguage(ProfileInfo profile);
}
