using OwnExpeditor.Data;
using OwnExpeditor.Sii;

namespace OwnExpeditor.Core;

/// <summary>Dados do job a injetar. Ids no formato do save (ver docs/sii-format.md).</summary>
public sealed record JobSpec(
    string Target,              // "<empresa>.<cidade>", sem aspas
    string Cargo,               // cargo.xxx
    string TrailerVariant,      // trailer.xxx
    string TrailerDefinition,   // trailer_def.xxx
    string CompanyTruck,
    int DistanceKm,
    int UnitsCount = 1,
    int Urgency = 0,
    int ExpirationHours = 72);

public sealed class JobInjectionException(string message) : Exception(message);

public static class JobInjector
{
    /// <summary>
    /// Preenche um slot de oferta vazio da empresa de origem. Não apaga ofertas existentes.
    /// Retorna o id do job_offer_data alterado.
    /// </summary>
    public static string Inject(SiiDocument doc, string originCompanyId, JobSpec spec, GameCatalog? catalog = null)
    {
        var company = doc.Find(originCompanyId);
        if (company is null || company.Type != "company")
            throw new JobInjectionException($"Empresa '{originCompanyId}' não existe no save.");

        if (doc.Find("company.volatile." + spec.Target) is null)
            throw new JobInjectionException($"Destino '{spec.Target}' não existe no save.");

        if (catalog is not null)
        {
            var cargo = spec.Cargo.Replace("cargo.", "");
            var origin = originCompanyId.Replace("company.volatile.", "").Split('.')[0];
            var target = spec.Target.Split('.')[0];
            if (!catalog.Ships(origin, cargo))
                throw new JobInjectionException($"A empresa '{origin}' não envia '{cargo}'.");
            if (!catalog.Accepts(target, cargo))
                throw new JobInjectionException($"A empresa '{target}' não recebe '{cargo}'.");
        }

        var gameTime = int.Parse(doc.OfType("economy").Single().Get("game_time")
            ?? throw new JobInjectionException("economy.game_time não encontrado."));

        var slot = company.GetArray("job_offer")
            .Select(doc.Find)
            .FirstOrDefault(j => j is not null && j.Get("target") == "\"\"")
            ?? throw new JobInjectionException($"Empresa '{originCompanyId}' não tem slot de oferta vazio.");

        slot.Set("target", $"\"{spec.Target}\"");
        slot.Set("expiration_time", (gameTime + spec.ExpirationHours * 60).ToString());
        slot.Set("urgency", spec.Urgency.ToString());
        slot.Set("shortest_distance_km", spec.DistanceKm.ToString());
        slot.Set("ferry_time", "0");
        slot.Set("ferry_price", "0");
        slot.Set("cargo", spec.Cargo);
        slot.Set("company_truck", spec.CompanyTruck);
        slot.Set("trailer_variant", spec.TrailerVariant);
        slot.Set("trailer_definition", spec.TrailerDefinition);
        slot.Set("units_count", spec.UnitsCount.ToString());
        slot.Set("fill_ratio", "1");
        slot.Set("trailer_place", "0");
        return slot.Id;
    }
}
