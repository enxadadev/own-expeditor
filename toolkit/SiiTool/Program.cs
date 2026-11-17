using OwnExpeditor.Sii;

// Saída curta e previsível, pensada para agentes: nada de logs extras.
if (args.Length < 2) return Usage();
if (args[0] == "formats")
{
    foreach (var file in args.Skip(1))
    {
        var b = File.ReadAllBytes(file);
        var outer = SiiDecoder.DetectFormat(b);
        var inn = outer == SiiFormat.Encrypted ? SiiDecoder.DetectFormat(SiiDecoder.Decrypt(b)) : outer;
        Console.WriteLine($"{inn,-9} {b.Length,9} {file}");
    }
    return 0;
}
var (cmd, path) = (args[0], args[1]);
if (!File.Exists(path)) { Console.Error.WriteLine($"arquivo não encontrado: {path}"); return 2; }

var bytes = File.ReadAllBytes(path);
try
{
    switch (cmd)
    {
        case "format":
            var f = SiiDecoder.DetectFormat(bytes);
            var inner = f == SiiFormat.Encrypted ? SiiDecoder.DetectFormat(SiiDecoder.Decrypt(bytes)).ToString() : "-";
            Console.WriteLine($"format={f} inner={inner} bytes={bytes.Length}");
            return 0;
        case "decode":
            var text = SiiDecoder.DecodeToText(bytes);
            if (args.Length > 2) { File.WriteAllText(args[2], text); Console.WriteLine($"ok {args[2]} chars={text.Length}"); }
            else Console.Write(text);
            return 0;
    }

    var doc = SiiDocument.Parse(SiiDecoder.DecodeToText(bytes));
    switch (cmd)
    {
        case "types":
            foreach (var g in doc.Units.GroupBy(u => u.Type).OrderByDescending(g => g.Count()))
                Console.WriteLine($"{g.Count(),7} {g.Key}");
            return 0;
        case "list" when args.Length > 2:
            var max = args.Length > 3 ? int.Parse(args[3]) : 20;
            var units = doc.OfType(args[2]).ToList();
            foreach (var u in units.Take(max)) Console.WriteLine(u.Id);
            if (units.Count > max) Console.WriteLine($"... +{units.Count - max} (total {units.Count})");
            return 0;
        case "show" when args.Length > 2:
            var unit = doc.Find(args[2]);
            if (unit is null) { Console.Error.WriteLine($"bloco não encontrado: {args[2]}"); return 3; }
            Console.WriteLine($"{unit.Type} : {unit.Id} {{");
            foreach (var a in unit.Attributes) Console.WriteLine($" {a.Name}: {a.Value}");
            Console.WriteLine("}");
            return 0;
        case "attrs" when args.Length > 2:
            // Atributos de um tipo e quantos blocos têm cada um — mostra o "schema" sem despejar dados.
            var ofType = doc.OfType(args[2]).ToList();
            foreach (var g in ofType.SelectMany(u => u.Attributes.Select(a => a.Name.Split('[')[0])).Distinct()
                         .Select(n => (n, c: ofType.Count(u => u.Attributes.Any(a => a.Name.Split('[')[0] == n)))))
                Console.WriteLine($"{g.c,7} {g.n}");
            Console.WriteLine($"total {ofType.Count}");
            return 0;
        case "roundtrip":
            var src = SiiDecoder.DecodeToText(bytes);
            var same = SiiDocument.Parse(src).ToText() == src;
            Console.WriteLine(same ? "roundtrip=ok" : "roundtrip=DIFF");
            return same ? 0 : 1;
    }
}
catch (Exception e) { Console.Error.WriteLine($"{e.GetType().Name}: {e.Message}"); return 4; }
return Usage();

static int Usage()
{
    Console.Error.WriteLine("""
        uso: sii <comando> <save> [args]
          format    <save>               formato (ScsC/BSII/texto)
          formats   <save>...            formato legível de vários saves
          decode    <save> [saida.txt]   texto SiiNunit
          types     <save>               contagem por tipo de bloco
          list      <save> <tipo> [max]  ids de um tipo (padrão 20)
          show      <save> <id>          um bloco inteiro
          attrs     <save> <tipo>        atributos de um tipo e frequência
          roundtrip <save>               parse+write reproduz o texto?
        """);
    return 1;
}
