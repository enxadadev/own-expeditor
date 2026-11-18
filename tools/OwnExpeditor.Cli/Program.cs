using OwnExpeditor.Sii;

// Ferramenta de desenvolvimento.
//   decode <entrada.sii> <saida.sii>

if (args.Length == 0)
{
    Console.Error.WriteLine("uso: decode <in> <out> | scs-ls | scs-cat | catalog");
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

        default:
            Console.Error.WriteLine("argumentos inválidos");
            return 1;
    }
}
catch (Exception e) when (e is InvalidDataException or NotSupportedException or IOException)
{
    Console.Error.WriteLine($"erro: {e.Message}");
    return 2;
}
