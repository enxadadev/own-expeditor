using OwnExpeditor.Data.Saves;
using OwnExpeditor.Domain.Saves;

namespace OwnExpeditor.Core.Profiles;

/// <summary>Perfis e saves do disco, fora da thread da UI.</summary>
internal sealed class ProfileService(IProfileLocator locator) : IProfileService
{
    public Task<IReadOnlyList<ProfileInfo>> GetProfilesAsync(CancellationToken ct) =>
        RunAsync(locator.FindProfiles, ct);

    public Task<IReadOnlyList<SaveSlotInfo>> GetSavesAsync(ProfileInfo profile, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(profile);
        return RunAsync(() => locator.FindSaves(profile), ct);
    }

    private static async Task<IReadOnlyList<T>> RunAsync<T>(Func<IReadOnlyList<T>> read, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        try
        {
            return await Task.Run(read, ct).ConfigureAwait(false);
        }
        catch (UnauthorizedAccessException e)
        {
            throw new CoreException(CoreError.AccessDenied, e.Message, e);
        }
    }
}
