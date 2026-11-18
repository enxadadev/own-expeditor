using System.Globalization;
using OwnExpeditor.Core.Games;
using OwnExpeditor.Core.Session;
using OwnExpeditor.Domain.Games;

namespace OwnExpeditor.Core.Modules.Jobs;

/// <summary>
/// Índice somente leitura sobre uma <see cref="ISaveView"/> (<c>View</c>: já inclui as mudanças pendentes) + <see cref="IGameData"/>.
/// Regras de carga = catálogo do jogo ∪ regras reveladas pelas ofertas do save (mods; sem catálogo, só as do save).
/// Responde na ordem do fluxo: cidade de saída → cidade de destino → empresa de saída → carga → empresa de chegada.
/// Listas ordenadas por id (ordinal); a App ordena pelo nome traduzido. Nunca lança por dado ruim do save.
/// </summary>
public sealed class JobBoard
{
    private const string CompanyPrefix = "company.volatile.";

    private sealed class Slots(CompanyLocation location)
    {
        public CompanyLocation Location { get; } = location;
        public List<string> Free { get; } = [];
        public List<ExistingOffer> Offers { get; } = [];
    }

    private readonly IGameData _data;
    private readonly Dictionary<string, Slots> _companies = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<Slots>> _byCity = new(StringComparer.Ordinal);
    private readonly Dictionary<string, JobTemplate> _templates = new(StringComparer.Ordinal);
    private readonly Dictionary<(string Company, string Cargo), JobTemplate> _companyTemplates = [];
    private readonly Dictionary<string, HashSet<string>> _learnedOut = new(StringComparer.Ordinal);
    private readonly Dictionary<string, HashSet<string>> _learnedIn = new(StringComparer.Ordinal);
    private readonly Dictionary<CityPair, int> _saveDistances = [];
    private DistanceTable _distances = new([]);
    private IReadOnlyDictionary<CityPair, int>? _ownEstimates;
    // Memos (o board é imutável depois do Build): cargas enviáveis por empresa e cidades que aceitam cada carga.
    private readonly Dictionary<string, string[]> _shipped = new(StringComparer.Ordinal);
    private Dictionary<string, HashSet<string>>? _acceptingCities;

    private JobBoard(IGameData data) => _data = data;

    /// <param name="knownDistances">Pares do cache (todos os saves do usuário); os do save aberto têm prioridade.</param>
    /// <param name="ownEstimates">
    /// Estimativas que nós mesmos gravamos num frete: um par do save com exatamente esse km não é distância real
    /// (continua estimado); um valor diferente é real e vale.
    /// </param>
    public static JobBoard Build(ISaveView view, IGameData data, IReadOnlyDictionary<CityPair, int>? knownDistances = null,
        IReadOnlyDictionary<CityPair, int>? ownEstimates = null)
    {
        ArgumentNullException.ThrowIfNull(view);
        ArgumentNullException.ThrowIfNull(data);
        var board = new JobBoard(data) { _ownEstimates = ownEstimates };
        board.Index(view);
        var all = new Dictionary<CityPair, int>();
        if (knownDistances is not null)
            foreach (var (pair, km) in knownDistances) all[pair] = km;
        foreach (var (pair, km) in board._saveDistances) all[pair] = km;
        board._distances = new DistanceTable(all);
        return board;
    }

    /// <summary>Pares de distância lidos das ofertas do save (sem as pendentes): é o que alimenta o cache.</summary>
    public IReadOnlyDictionary<CityPair, int> DistancesFromSave => _saveDistances;

    // ---- fluxo

    /// <summary>Todas as cidades que têm empresa no save (seletor de destino: cada uma com <see cref="HasMatch"/>).</summary>
    public IReadOnlyList<string> Cities() => _byCity.Keys.Order(StringComparer.Ordinal).ToList();

    /// <summary>Cidades com ao menos uma empresa utilizável: vaga livre ou oferta substituível e carga com reboque conhecido.</summary>
    public IReadOnlyList<string> OriginCities() =>
        _byCity.Where(c => c.Value.Any(IsUsable)).Select(c => c.Key).Order(StringComparer.Ordinal).ToList();

    /// <summary>Cidades (exceto a de saída) com empresa que recebe alguma carga enviada por uma empresa utilizável da saída.</summary>
    public IReadOnlyList<string> DestinationCities(string originCity)
    {
        ArgumentNullException.ThrowIfNull(originCity);
        if (!_byCity.TryGetValue(originCity, out var origins)) return [];
        var cities = new HashSet<string>(StringComparer.Ordinal);
        foreach (var o in origins)
            if (IsUsable(o))
                foreach (var cargo in ShippedWithTemplate(o.Location)) cities.UnionWith(AcceptingCities(cargo));
        cities.Remove(originCity);
        return cities.Order(StringComparer.Ordinal).ToList();
    }

    /// <summary>Há alguma empresa de saída com vaga ou oferta que envie uma carga aceita em <paramref name="destinationCity"/>?</summary>
    public bool HasMatch(string originCity, string destinationCity)
    {
        ArgumentNullException.ThrowIfNull(originCity);
        ArgumentNullException.ThrowIfNull(destinationCity);
        if (originCity == destinationCity || !_byCity.TryGetValue(originCity, out var companies)) return false;
        return companies.Any(c => c.Free.Count + c.Offers.Count > 0 &&
            ShippedWithTemplate(c.Location).Any(cargo => AcceptingCities(cargo).Contains(destinationCity)));
    }

    /// <summary>
    /// Empresas da cidade utilizáveis para algum destino (vaga ou oferta e carga aceita em outra cidade) — o mesmo que
    /// unir <see cref="OriginCompanies"/> sobre <see cref="DestinationCities"/>, sem varrer todas as cidades.
    /// </summary>
    public IReadOnlyList<CompanyLocation> UsableOriginCompanies(string originCity)
    {
        ArgumentNullException.ThrowIfNull(originCity);
        if (!_byCity.TryGetValue(originCity, out var companies)) return [];
        return companies
            .Where(c => c.Free.Count + c.Offers.Count > 0 && ShippedWithTemplate(c.Location)
                .Any(cargo => AcceptingCities(cargo).Any(city => city != originCity)))
            .Select(c => c.Location).OrderBy(l => l.Key, StringComparer.Ordinal).ToList();
    }

    /// <summary>Empresas da cidade com vaga livre OU oferta substituível e ao menos uma carga que a cidade de destino aceita.</summary>
    public IReadOnlyList<CompanyLocation> OriginCompanies(string originCity, string destinationCity)
    {
        ArgumentNullException.ThrowIfNull(originCity);
        ArgumentNullException.ThrowIfNull(destinationCity);
        if (originCity == destinationCity || !_byCity.TryGetValue(originCity, out var companies)) return [];
        return companies
            .Where(c => c.Free.Count + c.Offers.Count > 0 && Cargos(c.Location, destinationCity).Count > 0)
            .Select(c => c.Location).OrderBy(l => l.Key, StringComparer.Ordinal).ToList();
    }

    /// <summary>Cargas que a empresa envia, com reboque conhecido no save, e que alguma empresa de <paramref name="destinationCity"/> recebe.</summary>
    public IReadOnlyList<string> Cargos(CompanyLocation origin, string destinationCity)
    {
        ArgumentNullException.ThrowIfNull(origin);
        ArgumentNullException.ThrowIfNull(destinationCity);
        if (!_byCity.ContainsKey(destinationCity)) return [];
        return ShippedWithTemplate(origin)
            .Where(cargo => AcceptingCities(cargo).Contains(destinationCity))
            .Order(StringComparer.Ordinal).ToList();
    }

    /// <summary>Empresas da cidade de destino que recebem a carga.</summary>
    public IReadOnlyList<CompanyLocation> DestinationCompanies(string destinationCity, string cargo)
    {
        ArgumentNullException.ThrowIfNull(destinationCity);
        ArgumentNullException.ThrowIfNull(cargo);
        if (!_byCity.TryGetValue(destinationCity, out var companies)) return [];
        return companies.Where(c => Accepts(c.Location.Company, cargo))
            .Select(c => c.Location).OrderBy(l => l.Key, StringComparer.Ordinal).ToList();
    }

    /// <summary>Vagas livres (alvo vazio); já desconta as pendentes, porque a <c>View</c> as inclui.</summary>
    public int FreeSlots(CompanyLocation origin) => FreeSlotIds(origin).Count;

    /// <summary>Ids dos slots livres, na ordem do save.</summary>
    public IReadOnlyList<string> FreeSlotIds(CompanyLocation origin)
    {
        ArgumentNullException.ThrowIfNull(origin);
        return _companies.TryGetValue(origin.Key, out var c) ? c.Free : [];
    }

    /// <summary>Ofertas atuais da empresa que ainda podem ser substituídas (sem mudança pendente), na ordem do save.</summary>
    public IReadOnlyList<ExistingOffer> ReplaceableOffers(CompanyLocation origin)
    {
        ArgumentNullException.ThrowIfNull(origin);
        return _companies.TryGetValue(origin.Key, out var c) ? c.Offers : [];
    }

    /// <summary>Distância entre as cidades: conhecida (tabela) ou estimada. Nunca nula; o usuário não digita.</summary>
    public RouteDistance Distance(string fromCity, string toCity) => _distances.Get(fromCity, toCity);

    /// <summary>Reboque, caminhão e unidades copiados de um frete do save com a carga (qualquer empresa); nulo se nenhum.</summary>
    public JobTemplate? Template(string cargo)
    {
        ArgumentNullException.ThrowIfNull(cargo);
        return _templates.GetValueOrDefault(cargo);
    }

    /// <summary>Como <see cref="Template(string)"/>, preferindo um frete da mesma empresa de saída (caminhão e reboque dela).</summary>
    public JobTemplate? Template(string originCompany, string cargo)
    {
        ArgumentNullException.ThrowIfNull(originCompany);
        ArgumentNullException.ThrowIfNull(cargo);
        return _companyTemplates.GetValueOrDefault((originCompany, cargo)) ?? Template(cargo);
    }

    /// <summary>Nome (tradução → local → id legível) para a tela.</summary>
    public IGameNames Names => _data;

    // ---- usado pelo JobPlanner

    internal bool Contains(CompanyLocation company) => _companies.ContainsKey(company.Key);

    internal bool Ships(string company, string cargo) =>
        _data.Ships(company, cargo) || (_learnedOut.TryGetValue(company, out var set) && set.Contains(cargo));

    internal bool Accepts(string company, string cargo) =>
        _data.Accepts(company, cargo) || (_learnedIn.TryGetValue(company, out var set) && set.Contains(cargo));

    // ---- índice

    private bool IsUsable(Slots c) => c.Free.Count + c.Offers.Count > 0 && ShippedWithTemplate(c.Location).Length > 0;

    private string[] ShippedWithTemplate(CompanyLocation origin)
    {
        lock (_shipped)
        {
            if (_shipped.TryGetValue(origin.Company, out var hit)) return hit;
            var shipped = new HashSet<string>(_data.CargoOut(origin.Company), StringComparer.Ordinal);
            if (_learnedOut.TryGetValue(origin.Company, out var learned)) shipped.UnionWith(learned);
            return _shipped[origin.Company] = shipped.Where(_templates.ContainsKey).ToArray();
        }
    }

    /// <summary>Cidades com alguma empresa que aceita a carga (só cargas com reboque conhecido; montado uma vez).</summary>
    private IReadOnlySet<string> AcceptingCities(string cargo)
    {
        var index = Volatile.Read(ref _acceptingCities);
        if (index is null)
        {
            index = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            foreach (var t in _templates.Keys) index[t] = new HashSet<string>(StringComparer.Ordinal);
            foreach (var (city, companies) in _byCity)
                foreach (var c in companies)
                    foreach (var (t, set) in index)
                        if (!set.Contains(city) && Accepts(c.Location.Company, t)) set.Add(city);
            Volatile.Write(ref _acceptingCities, index);
        }
        return index.TryGetValue(cargo, out var cities) ? cities : EmptySet;
    }

    private static readonly HashSet<string> EmptySet = new(StringComparer.Ordinal);

    private void Index(ISaveView view)
    {
        var gameTime = ParseInt(view.OfType("economy").FirstOrDefault()?.Get("game_time"));
        foreach (var company in view.OfType("company"))
        {
            if (!company.Id.StartsWith(CompanyPrefix, StringComparison.Ordinal)) continue;
            var key = company.Id[CompanyPrefix.Length..];
            var dot = key.LastIndexOf('.');
            if (dot <= 0 || dot == key.Length - 1) continue;
            var location = new CompanyLocation(key[..dot], key[(dot + 1)..]);
            var slots = new Slots(location);
            _companies[location.Key] = slots;
            if (!_byCity.TryGetValue(location.City, out var inCity)) _byCity[location.City] = inCity = [];
            inCity.Add(slots);

            foreach (var jobId in company.GetArray("job_offer"))
            {
                var job = view.Find(jobId);
                if (job is null) continue;
                var target = job.Get("target")?.Trim('"') ?? "";
                if (target.Length == 0)
                {
                    slots.Free.Add(jobId);
                    continue;
                }
                if (!job.IsPending)
                {
                    var offerTarget = ParseLocation(target);
                    if (offerTarget is not null)
                        slots.Offers.Add(new ExistingOffer(jobId, offerTarget, CargoId(job.Get("cargo")) ?? "",
                            ParseInt(job.Get("shortest_distance_km")) ?? 0, ExpiresIn(job.Get("expiration_time"), gameTime)));
                }
                LearnFromBase(location, job);
            }
        }
    }

    /// <summary>
    /// Regras, modelo de reboque e distância vêm sempre dos valores do save base: o frete de uma mudança pendente
    /// (valores novos) não ensina nada, e substituir a única oferta de uma carga não apaga o que ela ensinava.
    /// </summary>
    private void LearnFromBase(CompanyLocation location, SaveUnitView job)
    {
        var target = ParseLocation(job.GetBase("target")?.Trim('"') ?? "");
        if (target is null) return;
        var km = ParseInt(job.GetBase("shortest_distance_km")) ?? 0;
        if (km > 0 && target.City != location.City)
        {
            var pair = CityPair.Of(location.City, target.City);
            if (_ownEstimates is null || !_ownEstimates.TryGetValue(pair, out var estimate) || estimate != km)
                _saveDistances.TryAdd(pair, km);
        }
        var cargo = CargoId(job.GetBase("cargo"));
        if (cargo is null) return;
        Learn(_learnedOut, location.Company, cargo);
        Learn(_learnedIn, target.Company, cargo);
        var variant = job.GetBase("trailer_variant");
        var definition = job.GetBase("trailer_definition");
        var truck = job.GetBase("company_truck");
        if (IsSet(variant) && IsSet(definition) && IsSet(truck))
        {
            var template = new JobTemplate(variant!, definition!, truck!, ParseInt(job.GetBase("units_count")) is > 0 and var u ? u : 1);
            _templates.TryAdd(cargo, template);
            _companyTemplates.TryAdd((location.Company, cargo), template);
        }
    }

    private static bool IsSet(string? value) => value is not (null or "" or "null" or "nil");

    private static void Learn(Dictionary<string, HashSet<string>> rules, string company, string cargo)
    {
        if (!rules.TryGetValue(company, out var set)) rules[company] = set = new HashSet<string>(StringComparer.Ordinal);
        set.Add(cargo);
    }

    private static CompanyLocation? ParseLocation(string target)
    {
        var dot = target.LastIndexOf('.');
        return dot <= 0 || dot == target.Length - 1 ? null : new CompanyLocation(target[..dot], target[(dot + 1)..]);
    }

    private static string? CargoId(string? value)
    {
        if (value is null or "" or "null" or "nil") return null;
        return value.StartsWith("cargo.", StringComparison.Ordinal) ? value["cargo.".Length..] : value;
    }

    private static int? ParseInt(string? value) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : null;

    /// <summary>Tempo de jogo restante (minutos de jogo; unidade ainda não confirmada no jogo, risco 8 do plano 0006).</summary>
    private static TimeSpan? ExpiresIn(string? expiration, int? gameTime) =>
        ParseInt(expiration) is { } at && gameTime is { } now ? TimeSpan.FromMinutes(Math.Max(0, at - now)) : null;
}
