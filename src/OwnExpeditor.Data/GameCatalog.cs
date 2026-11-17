using System.Text;
using OwnExpeditor.Data.Scs;

namespace OwnExpeditor.Data;

public sealed class CompanyDef(string name)
{
    public string Name { get; } = name;
    public SortedSet<string> Cities { get; } = new(StringComparer.Ordinal);
    public SortedSet<string> CargoIn { get; } = new(StringComparer.Ordinal);
    public SortedSet<string> CargoOut { get; } = new(StringComparer.Ordinal);
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

    /// <summary>A empresa envia a carga (origem válida).</summary>
    public bool Ships(string company, string cargo) =>
        Companies.TryGetValue(company, out var c) && c.CargoOut.Contains(cargo);

    /// <summary>A empresa recebe a carga (destino válido).</summary>
    public bool Accepts(string company, string cargo) =>
        Companies.TryGetValue(company, out var c) && c.CargoIn.Contains(cargo);

    /// <summary>Carrega def.scs e dlc_*.scs da pasta do jogo, nessa ordem.</summary>
    public static GameCatalog LoadFromGameDir(string gameDir)
    {
        var files = new List<string>();
        var def = Path.Combine(gameDir, "def.scs");
        if (File.Exists(def)) files.Add(def);
        files.AddRange(Directory.EnumerateFiles(gameDir, "dlc_*.scs").Order(StringComparer.Ordinal));
        if (files.Count == 0) throw new FileNotFoundException($"Nenhum def.scs/dlc_*.scs em '{gameDir}'.");
        return Load(files);
    }

    public static GameCatalog Load(IEnumerable<string> archives)
    {
        var catalog = new GameCatalog();
        foreach (var path in archives)
        {
            using var scs = HashFsArchive.Open(path);
            catalog.AddCompanies(scs);
            catalog.AddCargos(scs);
        }
        return catalog;
    }

    private void AddCompanies(HashFsArchive scs)
    {
        foreach (var name in scs.List("def/company").Directories)
        {
            if (!Companies.TryGetValue(name, out var company))
                Companies[name] = company = new CompanyDef(name);

            var root = $"def/company/{name}";
            foreach (var f in scs.List(root + "/in").Files) company.CargoIn.Add(Stem(f));
            foreach (var f in scs.List(root + "/out").Files) company.CargoOut.Add(Stem(f));
            foreach (var f in scs.List(root + "/editor").Files)
            {
                var data = scs.Read($"{root}/editor/{f}");
                var city = data is null ? null : Field(Encoding.UTF8.GetString(data), "city");
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
            var text = Encoding.UTF8.GetString(data);
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
            if (line.StartsWith(key + ":", StringComparison.Ordinal))
                yield return line[(key.Length + 1)..].Trim();
        }
    }
}
