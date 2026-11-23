using OwnExpeditor.Domain.Saves;

namespace OwnExpeditor.Data.Maps;

public interface IModLocator
{
    /// <summary>Mods ativos do perfil, por prioridade. Na fase 1a devolve vazio.</summary>
    IReadOnlyList<ModPackage> Active(ProfileInfo profile);
}
