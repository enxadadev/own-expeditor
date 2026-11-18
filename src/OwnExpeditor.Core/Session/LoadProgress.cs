namespace OwnExpeditor.Core.Session;

public static class LoadStages
{
    public const string Read = "Loading_Stage_Read";
    public const string Parse = "Loading_Stage_Parse";
    public const string Index = "Loading_Stage_Index";
}

/// <summary><c>Percent &lt; 0</c> = indeterminado.</summary>
public sealed record LoadProgress(int Percent, string StageKey, long? FileSizeBytes = null);
