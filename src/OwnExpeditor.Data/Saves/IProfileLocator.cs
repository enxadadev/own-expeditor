using OwnExpeditor.Domain.Saves;

namespace OwnExpeditor.Data.Saves;

public interface IProfileLocator
{
    /// <summary>Perfis de ETS2 e ATS em <c>profiles/</c> e <c>steam_profiles/</c> (Windows, Linux nativo e Proton).</summary>
    IReadOnlyList<ProfileInfo> FindProfiles();

    /// <summary>Saves do perfil, do mais recente ao mais antigo; pasta <c>save/</c> ausente = lista vazia.</summary>
    IReadOnlyList<SaveSlotInfo> FindSaves(ProfileInfo profile);
}
