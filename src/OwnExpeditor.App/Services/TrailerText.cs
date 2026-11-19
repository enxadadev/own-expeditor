using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using OwnExpeditor.Domain.Games;

namespace OwnExpeditor.App.Services;

/// <summary>Texto de um reboque: <see cref="Name"/> curto, <see cref="Details"/> (eixos · comprimento, pode ser vazio) e o id sem prefixo.</summary>
public sealed record TrailerLabel(string Name, string Details, string RawId)
{
    public bool HasValue => Name.Length > 0;
}

/// <summary>Regra do nome de reboque (design/nomes-reboques/screens.md §1) sobre o vocabulário do .resx; nunca devolve <c>!chave!</c>.</summary>
public static class TrailerText
{
    public static TrailerLabel Format(ILocalizer loc, TrailerDescriptor d)
    {
        var tokens = d.Tokens.Select(t => t.ToLowerInvariant()).ToList();
        var baseName = FindFinish(loc, tokens) ?? FindBody(loc, tokens) ?? Fallback(d);
        var chain = d.Chain switch
        {
            TrailerChain.Double => "Trailer_Chain_double",
            TrailerChain.BDouble => "Trailer_Chain_bdouble",
            TrailerChain.Hct => "Trailer_Chain_hct",
            _ => null,
        };
        var name = chain is not null && loc.TryGet(chain, out var chainText) ? loc.Get("Trailer_Name_WithChain", baseName, chainText) : baseName;
        return new TrailerLabel(name, Details(loc, d), d.Definition);
    }

    /// <summary>Diff e nome acessível: "Nome (detalhes)" ou só o nome.</summary>
    public static string WithDetails(ILocalizer loc, TrailerLabel label) =>
        label.Details.Length == 0 ? label.Name : loc.Get("Trailer_Details_Paren", label.Name, label.Details);

    private static string? FindFinish(ILocalizer loc, List<string> tokens)
    {
        for (var i = tokens.Count - 1; i >= 0; i--)
            if (loc.TryGet("Trailer_Finish_" + tokens[i], out var v)) return v;
        return null;
    }

    private static string? FindBody(ILocalizer loc, List<string> tokens)
    {
        foreach (var t in tokens)
            if (loc.TryGet("Trailer_Body_" + t, out var v)) return v;
        return null;
    }

    private static string Fallback(TrailerDescriptor d) =>
        GameIds.Humanize(d.Body ?? (d.Definition.Length > 0 ? d.Definition : d.Variant ?? ""));

    private static string Details(ILocalizer loc, TrailerDescriptor d)
    {
        var parts = new List<string>(2);
        var groups = d.AxleGroups;
        if (groups.Count == 1) parts.Add(loc.GetPlural("Trailer_Axles", groups[0]));
        else if (groups.Count > 1) parts.Add(loc.Get("Trailer_Axles_Groups", string.Join("+", groups.Select(g => g.ToString(CultureInfo.InvariantCulture)))));

        var lengthKey = d.Length switch { TrailerLength.Short => "Trailer_Length_short", TrailerLength.Long => "Trailer_Length_long", _ => null };
        if (lengthKey is not null && loc.TryGet(lengthKey, out var length)) parts.Add(length);

        return parts.Count == 2 ? loc.Get("Trailer_Details_Join", parts[0], parts[1]) : parts.Count == 1 ? parts[0] : "";
    }
}
