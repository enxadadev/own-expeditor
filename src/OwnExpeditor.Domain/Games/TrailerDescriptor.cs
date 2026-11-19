using System.Text.RegularExpressions;

namespace OwnExpeditor.Domain.Games;

public enum TrailerChain { Unknown, Single, Double, BDouble, Hct }

public enum TrailerLength { Unknown, Short, Long }

/// <summary>
/// Reboque decomposto a partir do id da definição (ex.: <c>scs.box.double_3_2.curtain</c>), sem texto de tela.
/// <see cref="Tokens"/> são os segmentos após o dono, sem o da cadeia: o App busca o acabamento do último ao primeiro
/// e a carroceria do primeiro ao último. <see cref="Body"/> e <see cref="Finish"/> são só a posição estrutural.
/// </summary>
public sealed partial record TrailerDescriptor(
    string Definition,
    string? Variant,
    string? Owner,
    string? Body,
    string? Finish,
    TrailerChain Chain,
    IReadOnlyList<int> AxleGroups,
    TrailerLength Length,
    IReadOnlyList<string> Tokens)
{
    public int TotalAxles => AxleGroups.Sum();

    /// <summary>Nunca lança: entrada vazia/estranha vira um descritor sem cadeia, eixos nem comprimento.</summary>
    public static TrailerDescriptor Parse(string? definition, string? variant)
    {
        var cleanVariant = Clean(variant);
        var id = StripPrefix(Clean(definition), "trailer_def.");
        if (id.Length == 0) id = StripPrefix(StripPrefix(cleanVariant, "trailer_def."), "trailer.");

        var segments = id.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        string? owner = segments.Length > 1 ? segments[0] : null;
        var rest = segments.Length > 1 ? segments[1..] : segments;

        var chain = TrailerChain.Unknown;
        var axles = new List<int>();
        var length = TrailerLength.Unknown;
        var tokens = new List<string>();
        foreach (var segment in rest)
        {
            if (chain == TrailerChain.Unknown && ChainRegex().Match(segment) is { Success: true } m)
            {
                chain = ChainOf(m.Groups[1].Value.ToLowerInvariant());
                var (a, l) = ParseChainParts(chain, segment.Split('_', StringSplitOptions.RemoveEmptyEntries)[1..]);
                axles = a;
                length = l;
            }
            else tokens.Add(segment);
        }

        if (length == TrailerLength.Unknown && rest.Length > 0 && !ChainRegex().IsMatch(rest[^1]))
            length = LengthOf(rest[^1]);

        var body = tokens.Count > 0 ? tokens[0] : null;
        var finish = tokens.Count > 1 && LengthOf(tokens[^1]) == TrailerLength.Unknown ? tokens[^1] : null;
        return new TrailerDescriptor(id, cleanVariant.Length == 0 ? null : cleanVariant, owner, body, finish, chain, axles, length, tokens);
    }

    private static (List<int> Axles, TrailerLength Length) ParseChainParts(TrailerChain chain, string[] parts)
    {
        var expected = chain switch { TrailerChain.Double or TrailerChain.BDouble => 2, TrailerChain.Hct => 3, _ => 1 };
        var axles = new List<int>();
        var length = TrailerLength.Unknown;
        var stopped = false;
        foreach (var part in parts)
        {
            var partLength = part.ToLowerInvariant() switch { "sh" => TrailerLength.Short, "ln" => TrailerLength.Long, _ => TrailerLength.Unknown };
            if (partLength != TrailerLength.Unknown) { length = partLength; continue; }
            if (stopped || axles.Count >= expected) continue;
            if (AxleRegex().IsMatch(part)) axles.Add(part[0] - '0');
            else if (axles.Count > 0) stopped = true; // grupos têm de ser consecutivos
        }

        return (axles.Count == expected ? axles : [], length);
    }

    private static TrailerChain ChainOf(string word) => word switch
    {
        "double" => TrailerChain.Double,
        "bdouble" => TrailerChain.BDouble,
        "hct" => TrailerChain.Hct,
        _ => TrailerChain.Single,
    };

    private static TrailerLength LengthOf(string token) =>
        token.Equals("short", StringComparison.OrdinalIgnoreCase) ? TrailerLength.Short
        : token.Equals("long", StringComparison.OrdinalIgnoreCase) ? TrailerLength.Long
        : TrailerLength.Unknown;

    private static string Clean(string? value)
    {
        var s = value?.Trim().Trim('"').Trim() ?? "";
        return s is "nil" ? "" : s;
    }

    private static string StripPrefix(string value, string prefix) =>
        value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ? value[prefix.Length..] : value;

    [GeneratedRegex(@"^(single|singl|double|bdouble|hct)(_.*)?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ChainRegex();

    [GeneratedRegex(@"^[0-9][a-z]?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex AxleRegex();
}
