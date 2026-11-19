using System;
using System.Collections.Generic;
using System.Linq;
using OwnExpeditor.Domain.Games;

namespace OwnExpeditor.App.Services;

/// <summary>Ícone de um reboque: chave do recurso <c>IconTrailer*</c> e nº de reboques da cadeia (selo ×N; 1 = sem selo).</summary>
public sealed record TrailerIconInfo(string Key, int Units);

/// <summary>Regra token → ícone (design/nomes-reboques/screens.md §7.5). Só decoração.</summary>
public static class TrailerIcons
{
    public const string Generic = "IconTrailerGeneric";

    private static readonly Dictionary<string, string> Finish = new(StringComparer.Ordinal)
    {
        ["dryvan"] = "IconTrailerBox", ["dryvan_m"] = "IconTrailerBox", ["insulated"] = "IconTrailerBox",
        ["curtain"] = "IconTrailerCurtain",
        ["reefer"] = "IconTrailerReefer",
        ["brick"] = "IconTrailerFlatbed",
        ["container"] = "IconTrailerContainer",
        ["car_trans"] = "IconTrailerCarTransporter", ["van_trans"] = "IconTrailerCarTransporter", ["truck_trans"] = "IconTrailerCarTransporter",
        ["panel_trans"] = "IconTrailerPanel", ["gls_trailer"] = "IconTrailerPanel", ["glass"] = "IconTrailerPanel",
    };

    private static readonly Dictionary<string, string> Body = new(StringComparer.Ordinal)
    {
        ["box"] = "IconTrailerBox",
        ["flatbed"] = "IconTrailerFlatbed",
        ["lowbed"] = "IconTrailerLowbed", ["lowloader"] = "IconTrailerLowbed",
        ["gooseneck"] = "IconTrailerContainer",
        ["dumper"] = "IconTrailerDumper",
        ["log"] = "IconTrailerLog",
        ["foodtank"] = "IconTrailerTank",
        ["silo"] = "IconTrailerSilo",
        ["livestock"] = "IconTrailerLivestock",
    };

    /// <summary>Todas as chaves que o resolvedor pode devolver (para testes e Galeria).</summary>
    public static IReadOnlyCollection<string> AllKeys { get; } =
        [.. Finish.Values.Concat(Body.Values).Append(Generic).Distinct().Order(StringComparer.Ordinal)];

    /// <summary><c>null</c> só se não houver definição nem variante.</summary>
    public static TrailerIconInfo? Resolve(TrailerDescriptor d)
    {
        if (d.Definition.Length == 0 && d.Variant is null) return null;
        var tokens = d.Tokens;
        var key = Generic;
        var found = false;
        for (var i = tokens.Count - 1; i >= 0 && !found; i--)
            found = Finish.TryGetValue(tokens[i].ToLowerInvariant(), out key!);
        for (var i = 0; i < tokens.Count && !found; i++)
            found = Body.TryGetValue(tokens[i].ToLowerInvariant(), out key!);
        if (!found) key = Generic;
        var units = d.Chain switch { TrailerChain.Double or TrailerChain.BDouble => 2, TrailerChain.Hct => 3, _ => 1 };
        return new TrailerIconInfo(key, units);
    }
}
