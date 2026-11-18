namespace OwnExpeditor.Core.Text;

/// <summary>Referência a um texto localizável; <c>Count</c> escolhe a forma plural (_Zero/_One/_Other).</summary>
public sealed record TextRef(string Key, IReadOnlyList<object?> Args, long? Count = null)
{
    public static TextRef Of(string key, params object?[] args) => new(key, args);

    public static TextRef Plural(string baseKey, long count, params object?[] args) => new(baseKey, args, count);

    /// <summary>Igualdade por valor: chave, contagem e argumentos (em sequência).</summary>
    public bool Equals(TextRef? other) =>
        other is not null && Key == other.Key && Count == other.Count && Args.SequenceEqual(other.Args);

    public override int GetHashCode()
    {
        var h = new HashCode();
        h.Add(Key);
        h.Add(Count);
        foreach (var a in Args) h.Add(a);
        return h.ToHashCode();
    }
}
