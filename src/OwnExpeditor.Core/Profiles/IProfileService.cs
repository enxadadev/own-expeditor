using OwnExpeditor.Domain.Saves;

namespace OwnExpeditor.Core.Profiles;

public interface IProfileService
{
    Task<IReadOnlyList<ProfileInfo>> GetProfilesAsync(CancellationToken ct);
    Task<IReadOnlyList<SaveSlotInfo>> GetSavesAsync(ProfileInfo profile, CancellationToken ct);
}
