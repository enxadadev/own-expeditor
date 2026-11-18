using OwnExpeditor.Core.Games;
using OwnExpeditor.Core.Legacy;
using OwnExpeditor.Data;
using OwnExpeditor.Domain.Games;
using OwnExpeditor.Sii;

namespace OwnExpeditor.Core;

/// <summary>Uma empresa numa cidade, como aparece no save ("kaarfor", "budapest").</summary>
public sealed record CompanyLocation(string Company, string City)
{
    public string Key => $"{Company}.{City}";
    public string SaveId => "company.volatile." + Key;

    /// <summary>Texto para a tela, ex.: "Budapeste – Kaarfor". Não entra na igualdade.</summary>
    public string? Display { get; init; }

    public bool Equals(CompanyLocation? other) => other is not null && Key == other.Key;
    public override int GetHashCode() => Key.GetHashCode(StringComparison.Ordinal);
    public override string ToString() => Display ?? $"{City} – {Company}";
}

/// <summary>Caminhão e reboque copiados de um frete existente da mesma carga.</summary>
public sealed record JobTemplate(string TrailerVariant, string TrailerDefinition, string CompanyTruck, int UnitsCount);

/// <summary>
/// Save carregado + catálogo do jogo. Responde às perguntas da tela:
/// quais origens, cargas e destinos são possíveis, e qual a distância.
/// </summary>
public sealed class SaveSession
{
    private readonly IGameData _data;
    private readonly Dictionary<string, CompanyLocation> _locations = new(StringComparer.Ordinal);
    private readonly HashSet<string> _withEmptySlot = new(StringComparer.Ordinal);
    private readonly Dictionary<(string, string), int> _distances = [];
    private readonly Dictionary<string, JobTemplate> _templates = new(StringComparer.Ordinal);

    public SiiDocument Document { get; }

    public SaveSession(SiiDocument document, GameCatalog catalog)
        : this(document, new LegacyGameData(catalog))
    {
    }

    public SaveSession(SiiDocument document, IGameData data)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(data);
        Document = document;
        _data = data;

        foreach (var company in document.OfType("company"))
        {
            var key = company.Id["company.volatile.".Length..];
            var dot = key.LastIndexOf('.');
            if (dot <= 0) continue;
            var (companyId, cityId) = (key[..dot], key[(dot + 1)..]);
            var location = new CompanyLocation(companyId, cityId)
            {
                Display = $"{data.Get(GameNameKind.City, cityId)} – {data.Get(GameNameKind.Company, companyId)}",
            };
            _locations[key] = location;

            foreach (var jobId in company.GetArray("job_offer"))
            {
                var job = document.Find(jobId);
                if (job is null) continue;
                var target = job.Get("target")?.Trim('"') ?? "";
                if (target.Length == 0)
                {
                    _withEmptySlot.Add(key);
                    continue;
                }
                Learn(location, target, job);
            }
        }
    }

    private void Learn(CompanyLocation origin, string target, SiiUnit job)
    {
        var targetCity = target[(target.LastIndexOf('.') + 1)..];
        if (int.TryParse(job.Get("shortest_distance_km"), out var km) && km > 0)
        {
            _distances.TryAdd((origin.City, targetCity), km);
            _distances.TryAdd((targetCity, origin.City), km);
        }

        var cargo = job.Get("cargo")?.Replace("cargo.", "");
        var variant = job.Get("trailer_variant");
        if (cargo is null || cargo == "null" || variant is null or "null") return;
        _templates.TryAdd(cargo, new JobTemplate(
            variant,
            job.Get("trailer_definition") ?? "",
            job.Get("company_truck") ?? "",
            int.TryParse(job.Get("units_count"), out var u) && u > 0 ? u : 1));
    }

    /// <summary>Empresas com slot livre que enviam ao menos uma carga com modelo conhecido.</summary>
    public IReadOnlyList<CompanyLocation> Origins() =>
        _withEmptySlot.Select(k => _locations[k])
            .Where(l => CargosFor(l).Count > 0)
            .OrderBy(l => l.ToString(), StringComparer.CurrentCulture)
            .ToList();

    /// <summary>Cargas que a origem envia e para as quais existe modelo de reboque no save.</summary>
    public IReadOnlyList<string> CargosFor(CompanyLocation origin) =>
        _data.CargoOut(origin.Company).Where(_templates.ContainsKey).ToList();

    /// <summary>Empresas do save que recebem a carga, exceto na mesma cidade.</summary>
    public IReadOnlyList<CompanyLocation> Destinations(CompanyLocation origin, string cargo) =>
        _locations.Values
            .Where(l => l.City != origin.City && _data.Accepts(l.Company, cargo))
            .OrderBy(l => l.ToString(), StringComparer.CurrentCulture)
            .ToList();

    /// <summary>Distância conhecida no save entre as duas cidades, ou null.</summary>
    public int? Distance(string fromCity, string toCity) =>
        _distances.TryGetValue((fromCity, toCity), out var km) ? km : null;

    public string CargoName(string cargo) => _data.Get(GameNameKind.Cargo, cargo);

    public JobTemplate? Template(string cargo) => _templates.GetValueOrDefault(cargo);

    public string Inject(CompanyLocation origin, CompanyLocation target, string cargo, int distanceKm, int urgency = 0)
    {
        var t = Template(cargo) ?? throw new JobInjectionException($"Sem modelo de reboque para '{cargo}' no save.");
        var spec = new JobSpec(target.Key, "cargo." + cargo, t.TrailerVariant, t.TrailerDefinition,
            t.CompanyTruck, distanceKm, t.UnitsCount, urgency);
        var id = JobInjector.Inject(Document, origin.SaveId, spec, _data);
        _withEmptySlot.Remove(origin.Key);
        foreach (var jobId in Document.Find(origin.SaveId)!.GetArray("job_offer"))
            if (Document.Find(jobId)?.Get("target") == "\"\"") { _withEmptySlot.Add(origin.Key); break; }
        return id;
    }
}
