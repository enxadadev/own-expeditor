using System.Text;
using OwnExpeditor.Data.Scs;

namespace OwnExpeditor.Data;

public sealed class CompanyDef(string name)
{
    public string Name { get; } = name;
    public SortedSet<string> Cities { get; } = new(StringComparer.Ordinal);
    public SortedSet<string> CargoIn { get; } = new(StringComparer.Ordinal);
    public SortedSet<string> CargoOut { get; } = new(StringComparer.Ordinal);
    public string? DisplayName { get; set; }      // ex.: "Kaarfor"
}

public sealed class CityDef(string name)
{
    public string Name { get; } = name;          // ex.: budapest
    public string? NativeName { get; set; }      // city_name: "Budapest"
    public string? LocalizationKey { get; set; } // city_name_localized: "@@budapest@@"
    public string? Country { get; set; }
}

public sealed class CargoDef(string name)
{
    public string Name { get; } = name;          // ex.: plumb_suppl (sem "cargo.")
    public string? LocalizationKey { get; set; } // ex.: @@cn_plumbing_supplies@@
    public List<string> BodyTypes { get; } = [];
    public List<string> Groups { get; } = [];
    public double? UnitRewardPerKm { get; set; }
}

/// <summary>
/// Empresas e cargas definidas nos arquivos .scs do jogo (def.scs + DLCs).
/// Lido em tempo de execução da instalação do usuário. Ver docs/sii-format.md §8.
/// </summary>
public sealed class GameCatalog
{
    public Dictionary<string, CompanyDef> Companies { get; } = new(StringComparer.Ordinal);
    public Dictionary<string, CargoDef> Cargos { get; } = new(StringComparer.Ordinal);
    public Dictionary<string, CityDef> Cities { get; } = new(StringComparer.Ordinal);
    public Localization Texts { get; private set; } = new();

    public string CompanyName(string id) =>
        Companies.GetValueOrDefault(id)?.DisplayName ?? Localization.Humanize(id);

    public string CityName(string id)
    {
        var city = Cities.GetValueOrDefault(id);
        return Texts.Resolve(city?.LocalizationKey) ?? city?.NativeName ?? Localization.Humanize(id);
    }

    public string CargoName(string id) =>
        Texts.Resolve(Cargos.GetValueOrDefault(id)?.LocalizationKey) ?? Localization.Humanize(id);

    /// <summary>A empresa envia a carga (origem válida).</summary>
    public bool Ships(string company, string cargo) =>
        Companies.TryGetValue(company, out var c) && c.CargoOut.Contains(cargo);

    /// <summary>A empresa recebe a carga (destino válido).</summary>
    public bool Accepts(string company, string cargo) =>
        Companies.TryGetValue(company, out var c) && c.CargoIn.Contains(cargo);

    /// <summary>
    /// Carrega def.scs e dlc_*.scs da pasta do jogo, nessa ordem, e as traduções
    /// de locale.scs (se existir) no idioma pedido.
    /// </summary>
    public static GameCatalog LoadFromGameDir(string gameDir, string? language = null)
    {
        var files = new List<string>();
        var def = Path.Combine(gameDir, "def.scs");
        if (File.Exists(def)) files.Add(def);
        files.AddRange(Directory.EnumerateFiles(gameDir, "dlc_*.scs").Order(StringComparer.Ordinal));
        if (files.Count == 0) throw new FileNotFoundException($"Nenhum def.scs/dlc_*.scs em '{gameDir}'.");
        var locale = Path.Combine(gameDir, "locale.scs");
        if (File.Exists(locale)) files.Add(locale);
        return Load(files, language);
    }

    public static GameCatalog Load(IEnumerable<string> archives, string? language = null) =>
        Build(archives, withTexts: true, language);

    /// <summary>Só as definições (empresas, cidades, cargas); <see cref="Texts"/> fica vazio (carregue com <see cref="Localization"/>).</summary>
    public static GameCatalog LoadDefinitions(IEnumerable<string> archives) =>
        Build(archives, withTexts: false, language: null);

    private static GameCatalog Build(IEnumerable<string> archives, bool withTexts, string? language)
    {
        var catalog = new GameCatalog();
        var opened = archives.Select(HashFsArchive.Open).ToList();
        try
        {
            foreach (var scs in opened)
            {
                catalog.AddCompanies(scs);
                catalog.AddCargos(scs);
                catalog.AddCities(scs);
            }
            if (withTexts) catalog.Texts = Localization.Load(opened, language);
        }
        finally
        {
            foreach (var scs in opened) scs.Dispose();
        }
        return catalog;
    }

    private void AddCities(HashFsArchive scs)
    {
        foreach (var f in scs.List("def/city").Files.Where(f => f.EndsWith(".sui", StringComparison.Ordinal)))
        {
            var data = scs.Read("def/city/" + f);
            if (data is null) continue;
            var text = OwnExpeditor.Sii.SiiDecoder.DecodeLenient(data);
            var id = Field(text, "city_data")?.Replace("city.", "") ?? Stem(f);
            Cities[id] = new CityDef(id)
            {
                NativeName = Field(text, "city_name")?.Trim('"'),
                LocalizationKey = Field(text, "city_name_localized")?.Trim('"'),
                Country = Field(text, "country"),
            };
        }
    }

    private void AddCompanies(HashFsArchive scs)
    {
        foreach (var name in scs.List("def/company").Directories)
        {
            if (!Companies.TryGetValue(name, out var company))
                Companies[name] = company = new CompanyDef(name);

            var sui = scs.Read($"def/company/{name}.sui");
            if (sui is not null && Field(OwnExpeditor.Sii.SiiDecoder.DecodeLenient(sui), "name") is { } display)
                company.DisplayName = display.Trim('"');

            var root = $"def/company/{name}";
            foreach (var f in scs.List(root + "/in").Files) company.CargoIn.Add(Stem(f));
            foreach (var f in scs.List(root + "/out").Files) company.CargoOut.Add(Stem(f));
            foreach (var f in scs.List(root + "/editor").Files)
            {
                var data = scs.Read($"{root}/editor/{f}");
                var city = data is null ? null : Field(OwnExpeditor.Sii.SiiDecoder.DecodeLenient(data), "city");
                company.Cities.Add(city ?? Stem(f));
            }
        }
    }

    private void AddCargos(HashFsArchive scs)
    {
        foreach (var f in scs.List("def/cargo").Files.Where(f => f.EndsWith(".sui", StringComparison.Ordinal)))
        {
            var data = scs.Read("def/cargo/" + f);
            if (data is null) continue;
            var text = OwnExpeditor.Sii.SiiDecoder.DecodeLenient(data);
            var id = Field(text, "cargo_data")?.Replace("cargo.", "") ?? Stem(f);

            var cargo = new CargoDef(id) { LocalizationKey = Field(text, "name")?.Trim('"') };
            if (double.TryParse(Field(text, "unit_reward_per_km"), System.Globalization.CultureInfo.InvariantCulture, out var r))
                cargo.UnitRewardPerKm = r;
            cargo.BodyTypes.AddRange(Fields(text, "body_types[]"));
            cargo.Groups.AddRange(Fields(text, "group[]"));
            Cargos[id] = cargo;
        }
    }

    private static string Stem(string file) => file[..file.IndexOf('.')];

    private static string? Field(string text, string key) => Fields(text, key).FirstOrDefault();

    private static IEnumerable<string> Fields(string text, string key)
    {
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.Trim();
            if (!line.StartsWith(key, StringComparison.Ordinal)) continue;
            var rest = line[key.Length..].TrimStart();
            if (rest.StartsWith(':')) yield return rest[1..].Trim();
        }
    }
}
