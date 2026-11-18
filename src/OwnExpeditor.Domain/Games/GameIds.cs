namespace OwnExpeditor.Domain.Games;

public static class GameIds
{
    /// <summary>Nome legível a partir de um id ("plumb_suppl" → "Plumb suppl").</summary>
    public static string Humanize(string id)
    {
        ArgumentNullException.ThrowIfNull(id);
        var s = id.Replace('_', ' ').Trim();
        return s.Length == 0 ? id : char.ToUpperInvariant(s[0]) + s[1..];
    }
}
