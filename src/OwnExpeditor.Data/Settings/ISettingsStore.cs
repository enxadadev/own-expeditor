using OwnExpeditor.Domain.Settings;

namespace OwnExpeditor.Data.Settings;

public interface ISettingsStore
{
    /// <summary>null = primeira execução (sem arquivo). JSON corrompido devolve valores padrão, sem lançar.</summary>
    Task<AppSettings?> LoadAsync(CancellationToken ct);

    Task SaveAsync(AppSettings settings, CancellationToken ct);
}
