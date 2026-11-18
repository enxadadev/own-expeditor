using OwnExpeditor.Domain.Settings;

namespace OwnExpeditor.Core.Settings;

public interface IAppSettingsService
{
    AppSettings Current { get; }
    Task LoadAsync(CancellationToken ct);
    Task UpdateAsync(AppSettings settings, CancellationToken ct);
    event EventHandler<AppSettings>? Changed;
}
