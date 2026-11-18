using OwnExpeditor.Domain.Games;
using OwnExpeditor.Sii;

namespace OwnExpeditor.Data.Distances;

internal static class DistancePairs
{
    private const string CompanyPrefix = "company.volatile.";

    /// <summary>Pares (cidade da empresa, cidade do destino) com distância &gt; 0 das ofertas do documento.</summary>
    public static Dictionary<CityPair, int> Extract(SiiDocument doc)
    {
        var pairs = new Dictionary<CityPair, int>();
        foreach (var company in doc.OfType("company"))
        {
            if (!company.Id.StartsWith(CompanyPrefix, StringComparison.Ordinal)) continue;
            var key = company.Id[CompanyPrefix.Length..];
            var dot = key.LastIndexOf('.');
            if (dot <= 0) continue;
            var city = key[(dot + 1)..];
            foreach (var jobId in company.GetArray("job_offer"))
            {
                var job = doc.Find(jobId);
                var target = job?.Get("target")?.Trim('"');
                if (string.IsNullOrEmpty(target)) continue;
                var targetCity = target[(target.LastIndexOf('.') + 1)..];
                if (targetCity.Length == 0 || targetCity == city) continue;
                if (int.TryParse(job!.Get("shortest_distance_km"), out var km) && km > 0)
                    pairs.TryAdd(CityPair.Of(city, targetCity), km);
            }
        }
        return pairs;
    }
}
