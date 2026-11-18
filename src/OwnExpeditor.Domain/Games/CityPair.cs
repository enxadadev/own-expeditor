namespace OwnExpeditor.Domain.Games;

/// <summary>Par não ordenado de cidades (ids do save): <c>Of("a","b") == Of("b","a")</c>.</summary>
public readonly record struct CityPair(string A, string B)
{
    public static CityPair Of(string x, string y)
    {
        ArgumentNullException.ThrowIfNull(x);
        ArgumentNullException.ThrowIfNull(y);
        return string.CompareOrdinal(x, y) <= 0 ? new CityPair(x, y) : new CityPair(y, x);
    }
}
