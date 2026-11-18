namespace OwnExpeditor.Domain.Settings;

public sealed record AppSettings(ThemePreference Theme, Language Language, string? LastSavePath, GameFolders GameFolders);
