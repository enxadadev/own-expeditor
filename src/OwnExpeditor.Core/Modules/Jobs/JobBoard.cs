using System.Globalization;
using OwnExpeditor.Core.Games;
using OwnExpeditor.Core.Session;
using OwnExpeditor.Domain.Games;

namespace OwnExpeditor.Core.Modules.Jobs;

/// <summary>
/// Índice somente leitura sobre uma <see cref="ISaveView"/> (<c>View</c>: já inclui as mudanças pendentes) + <see cref="IGameData"/>.
/// Regras de carga = catálogo do jogo ∪ regras reveladas pelas ofertas do save (mods; sem catálogo, só as do save).
/// Responde na ordem do fluxo País → Cidade → Empresa: saída (país, cidade, empresa, vaga) → chegada (país, cidade, empresa) → carga.
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
        public int Pending { get; set; }
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
    private bool? _hasCountries;
    private List<DepartureCity>? _departureCities;
    private Dictionary<string, List<Slots>>? _acceptorsByCargo;
    private readonly Dictionary<string, List<Slots>> _acceptorsByOrigin = new(StringComparer.Ordinal);

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

    /// <summary>Id usado para cidades sem país conhecido.</summary>
    public const string OtherCountry = "";

    /// <summary>Alguma cidade com empresa tem país conhecido (senão os campos de país não existem).</summary>
    public bool HasCountries => _hasCountries ??= _byCity.Keys.Any(c => CountryOf(c) is not null);

    /// <summary>País da cidade; nulo se desconhecido (nunca vazio).</summary>
    public string? CountryOf(string city)
    {
        ArgumentNullException.ThrowIfNull(city);
        return _data.CityCountry(city) is { Length: > 0 } country ? country : null;
    }

    /// <summary>Países com cidade de saída (vazio sem países); "Outros" por último.</summary>
    public IReadOnlyList<CountryInfo> DepartureCountries() =>
        HasCountries ? GroupByCountry(DepartureList().Select(c => c.City)) : [];

    /// <summary>Cidades de saída (país nulo = todas; <see cref="OtherCountry"/> = sem país), por id.</summary>
    public IReadOnlyList<DepartureCity> DepartureCities(string? country) =>
        country is null ? DepartureList() : DepartureList().Where(c => (c.Country ?? OtherCountry) == country).ToList();

    /// <summary>Empresas da cidade com vaga livre ou oferta substituível e ao menos uma chegada possível.</summary>
    public IReadOnlyList<CompanyLocation> DepartureCompanies(string fromCity)
    {
        ArgumentNullException.ThrowIfNull(fromCity);
        return _byCity.TryGetValue(fromCity, out var companies)
            ? companies.Where(IsDepartureCompany).Select(c => c.Location).OrderBy(l => l.Key, StringComparer.Ordinal).ToList()
            : [];
    }

    /// <summary>Países das empresas que recebem alguma carga da saída (a cidade de saída fica de fora).</summary>
    public IReadOnlyList<CountryInfo> ArrivalCountries(CompanyLocation from)
    {
        ArgumentNullException.ThrowIfNull(from);
        return HasCountries ? GroupByCountry(Acceptors(from).Select(a => a.Location.City).Distinct(StringComparer.Ordinal)) : [];
    }

    public IReadOnlyList<string> ArrivalCities(CompanyLocation from, string? country)
    {
        ArgumentNullException.ThrowIfNull(from);
        return Acceptors(from).Select(a => a.Location.City).Distinct(StringComparer.Ordinal)
            .Where(city => country is null || (CountryOf(city) ?? OtherCountry) == country)
            .Order(StringComparer.Ordinal).ToList();
    }

    public IReadOnlyList<CompanyLocation> ArrivalCompanies(CompanyLocation from, string toCity)
    {
        ArgumentNullException.ThrowIfNull(from);
        ArgumentNullException.ThrowIfNull(toCity);
        return Acceptors(from).Where(a => a.Location.City == toCity).Select(a => a.Location)
            .OrderBy(l => l.Key, StringComparer.Ordinal).ToList();
    }

    /// <summary>Cargas que a saída envia (com reboque conhecido) e a empresa de chegada recebe.</summary>
    public IReadOnlyList<string> Cargos(CompanyLocation from, CompanyLocation to)
    {
        ArgumentNullException.ThrowIfNull(from);
        ArgumentNullException.ThrowIfNull(to);
        if (from.City == to.City) return [];
        return ShippedWithTemplate(from).Where(cargo => Accepts(to.Company, cargo)).Order(StringComparer.Ordinal).ToList();
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

    private bool IsDepartureCompany(Slots c) => c.Free.Count + c.Offers.Count > 0 && Acceptors(c.Location).Count > 0;

    private List<DepartureCity> DepartureList() => _departureCities ??= _byCity
        .Select(kv =>
        {
            var companies = kv.Value.Where(IsDepartureCompany).ToList();
            var pending = kv.Value.Sum(c => c.Pending);
            return companies.Count == 0 && pending == 0
                ? null
                : new DepartureCity(kv.Key, CountryOf(kv.Key), companies.Count, companies.Sum(c => c.Free.Count),
                    companies.Sum(c => c.Offers.Count), pending);
        })
        .OfType<DepartureCity>().OrderBy(c => c.City, StringComparer.Ordinal).ToList();

    private IReadOnlyList<CountryInfo> GroupByCountry(IEnumerable<string> cities) =>
        cities.GroupBy(c => CountryOf(c) ?? OtherCountry)
            .OrderBy(g => g.Key.Length == 0 ? 1 : 0).ThenBy(g => g.Key, StringComparer.Ordinal)
            .Select(g => new CountryInfo(g.Key, g.Count())).ToList();

    /// <summary>Empresas (fora da cidade de saída) que recebem alguma carga enviada pela empresa, com reboque conhecido.</summary>
    private List<Slots> Acceptors(CompanyLocation from)
    {
        if (_acceptorsByOrigin.TryGetValue(from.Key, out var cached)) return cached;
        if (_acceptorsByCargo is null)
        {
            _acceptorsByCargo = new Dictionary<string, List<Slots>>(StringComparer.Ordinal);
            foreach (var cargo in _templates.Keys)
                _acceptorsByCargo[cargo] = _companies.Values.Where(c => Accepts(c.Location.Company, cargo)).ToList();
        }
        var result = ShippedWithTemplate(from)
            .SelectMany(cargo => _acceptorsByCargo.GetValueOrDefault(cargo) ?? [])
            .Where(a => a.Location.City != from.City).Distinct().ToList();
        return _acceptorsByOrigin[from.Key] = result;
    }

    private IEnumerable<string> ShippedWithTemplate(CompanyLocation origin)
    {
        var shipped = new HashSet<string>(_data.CargoOut(origin.Company), StringComparer.Ordinal);
        if (_learnedOut.TryGetValue(origin.Company, out var learned)) shipped.UnionWith(learned);
        return shipped.Where(_templates.ContainsKey);
    }

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
                if (job.IsPending) slots.Pending++;
                else
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
