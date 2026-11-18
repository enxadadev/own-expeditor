using System.Globalization;
using System.Text;
using System.Text.Json;
using OwnExpeditor.Domain.Settings;

namespace OwnExpeditor.Data.Settings;

/// <summary><c>settings.json</c> em <paramref name="directory"/>. Arquivo ilegível vira valores padrão, nunca exceção.</summary>
internal sealed class JsonSettingsStore(string directory) : ISettingsStore
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    private string FilePath => Path.Combine(directory, "settings.json");

    public static AppSettings Defaults() => new(
        ThemePreference.System,
        CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "pt" ? Language.PtBr : Language.En,
        null,
        new GameFolders());

    public async Task<AppSettings?> LoadAsync(CancellationToken ct)
    {
        string text;
        try
        {
            if (!File.Exists(FilePath)) return null;
            text = await File.ReadAllTextAsync(FilePath, ct);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return Defaults();
        }

        Dto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<Dto>(text, Json);
        }
        catch (JsonException)
        {
            return Defaults();
        }
        if (dto is null) return Defaults();

        var d = Defaults();
        return new AppSettings(
            ParseEnum(dto.Theme, d.Theme),
            ParseEnum(dto.Language, d.Language),
            Clean(dto.LastSavePath),
            new GameFolders(Clean(dto.GameFolders?.Ets2), Clean(dto.GameFolders?.Ats)));
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var dto = new Dto
        {
            Theme = settings.Theme.ToString(),
            Language = settings.Language.ToString(),
            LastSavePath = settings.LastSavePath,
            GameFolders = new FoldersDto { Ets2 = settings.GameFolders.Ets2, Ats = settings.GameFolders.Ats },
        };
        Directory.CreateDirectory(directory);
        var tmp = FilePath + ".tmp";
        try
        {
            await using (var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await fs.WriteAsync(new UTF8Encoding(false).GetBytes(JsonSerializer.Serialize(dto, Json)), ct);
                fs.Flush(true);
            }
            File.Move(tmp, FilePath, overwrite: true);
        }
        catch
        {
            try { File.Delete(tmp); } catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
            throw;
        }
    }

    private static T ParseEnum<T>(string? text, T fallback) where T : struct, Enum =>
        Enum.TryParse<T>(text, ignoreCase: true, out var value) && Enum.IsDefined(value) && !int.TryParse(text, out _)
            ? value
            : fallback;

    private static string? Clean(string? path) => string.IsNullOrWhiteSpace(path) ? null : path;

    private sealed class Dto
    {
        public string? Theme { get; set; }
        public string? Language { get; set; }
        public string? LastSavePath { get; set; }
        public FoldersDto? GameFolders { get; set; }
    }

    private sealed class FoldersDto
    {
        public string? Ets2 { get; set; }
        public string? Ats { get; set; }
    }
}
