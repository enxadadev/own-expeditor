using OwnExpeditor.Core;
using OwnExpeditor.Sii;

// Ferramenta de desenvolvimento.
//   decode <entrada.sii> <saida.sii>
//   inject <game.sii> <saida.sii> <empresa_origem> <destino> <cargo> <trailer_variant> <trailer_def> <company_truck> <km>

if (args.Length == 0)
{
    Console.Error.WriteLine("uso: decode <in> <out> | inject <in> <out> <origem> <destino> <cargo> <trailer_variant> <trailer_def> <truck> <km>");
    return 1;
}

try
{
    switch (args[0])
    {
        case "scs-ls" when args.Length is 2 or 3:
        {
            using var scs = OwnExpeditor.Data.Scs.HashFsArchive.Open(args[1]);
            var (dirs, files) = scs.List(args.Length == 3 ? args[2] : "");
            foreach (var d in dirs) Console.WriteLine(d + "/");
            foreach (var f in files) Console.WriteLine(f);
            return 0;
        }

        case "catalog" when args.Length is 2 or 3:
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var cat = OwnExpeditor.Data.GameCatalog.LoadFromGameDir(args[1]);
            Console.WriteLine($"{cat.Companies.Count} empresas, {cat.Cargos.Count} cargas em {sw.ElapsedMilliseconds} ms");
            if (args.Length == 3 && cat.Companies.TryGetValue(args[2], out var c))
            {
                Console.WriteLine($"cidades: {string.Join(", ", c.Cities)}");
                Console.WriteLine($"envia ({c.CargoOut.Count}): {string.Join(", ", c.CargoOut)}");
                Console.WriteLine($"recebe ({c.CargoIn.Count}): {string.Join(", ", c.CargoIn)}");
            }
            return 0;
        }

        case "scs-cat" when args.Length == 3:
        {
            using var scs = OwnExpeditor.Data.Scs.HashFsArchive.Open(args[1]);
            var data = scs.Read(args[2]) ?? throw new IOException($"'{args[2]}' não existe no pacote.");
            Console.WriteLine(SiiDecoder.DetectFormat(data) == SiiFormat.Unknown
                ? System.Text.Encoding.UTF8.GetString(data)
                : SiiDecoder.DecodeToText(data));
            return 0;
        }

        case "decode" when args.Length == 3:
            File.WriteAllText(args[2], SiiDecoder.DecodeToText(File.ReadAllBytes(args[1])));
            Console.WriteLine($"ok: {args[2]}");
            return 0;

        case "inject" when args.Length is 10 or 11:
        {
            var doc = SiiDocument.Load(args[1]);
            var spec = new JobSpec(args[4], args[5], args[6], args[7], args[8], int.Parse(args[9]),
                UnitsCount: args.Length == 11 ? int.Parse(args[10]) : 1);
            var id = JobInjector.Inject(doc, args[3], spec);
            var backup = doc.Save(args[2]);
            Console.WriteLine($"ok: job {id} gravado em {args[2]}" + (backup is null ? "" : $" (backup: {backup})"));
            return 0;
        }

        default:
            Console.Error.WriteLine("argumentos inválidos");
            return 1;
    }
}
catch (Exception e) when (e is JobInjectionException or InvalidDataException or NotSupportedException or IOException)
{
    Console.Error.WriteLine($"erro: {e.Message}");
    return 2;
}
