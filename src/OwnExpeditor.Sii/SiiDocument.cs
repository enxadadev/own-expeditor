using System.Text;

namespace OwnExpeditor.Sii;

/// <summary>Um atributo "nome: valor". Arrays aparecem como "nome: n" + "nome[i]: valor".</summary>
public sealed class SiiAttribute(string name, string value)
{
    public string Name { get; } = name;
    public string Value { get; set; } = value;
}

/// <summary>Um bloco "tipo : id { ... }".</summary>
public sealed class SiiUnit(string type, string id)
{
    public string Type { get; } = type;
    public string Id { get; } = id;
    public List<SiiAttribute> Attributes { get; } = [];

    public string? Get(string name) => Attributes.FirstOrDefault(a => a.Name == name)?.Value;

    public void Set(string name, string value)
    {
        var attr = Attributes.FirstOrDefault(a => a.Name == name)
            ?? throw new KeyNotFoundException($"Atributo '{name}' não existe em {Type} : {Id}.");
        attr.Value = value;
    }

    /// <summary>Valores de um array (ex.: job_offer → job_offer[0..n-1]).</summary>
    public IReadOnlyList<string> GetArray(string name)
    {
        var prefix = name + "[";
        return Attributes.Where(a => a.Name.StartsWith(prefix, StringComparison.Ordinal))
            .Select(a => a.Value).ToList();
    }
}

/// <summary>
/// Documento SII em texto. Preserva a ordem dos blocos e atributos para que
/// ler e gravar sem alterações produza o mesmo arquivo.
/// </summary>
public sealed class SiiDocument
{
    private const string Newline = "\r\n";

    private readonly Dictionary<string, SiiUnit> _byId = new(StringComparer.Ordinal);

    public List<SiiUnit> Units { get; } = [];

    public SiiUnit? Find(string id) => _byId.GetValueOrDefault(id);

    public IEnumerable<SiiUnit> OfType(string type) => Units.Where(u => u.Type == type);

    public static SiiDocument Load(string path) => Parse(SiiDecoder.DecodeToText(File.ReadAllBytes(path)));

    public static SiiDocument Parse(string text)
    {
        var doc = new SiiDocument();
        using var reader = new StringReader(text.TrimStart('﻿'));
        var lineNo = 0;
        SiiUnit? current = null;
        var seenHeader = false;

        while (reader.ReadLine() is { } line)
        {
            lineNo++;
            var trimmed = line.Trim();
            if (trimmed.Length == 0) continue;

            if (!seenHeader)
            {
                if (trimmed != "SiiNunit") throw Error(lineNo, "esperado 'SiiNunit'");
                seenHeader = true;
                continue;
            }

            if (current is null)
            {
                if (trimmed is "{" or "}") continue;
                current = ParseUnitHeader(trimmed, lineNo);
                doc.Units.Add(current);
                doc._byId.TryAdd(current.Id, current);
                continue;
            }

            if (trimmed == "}")
            {
                current = null;
                continue;
            }

            var sep = trimmed.IndexOf(": ", StringComparison.Ordinal);
            if (sep <= 0)
            {
                // atributo com valor vazio ("nome:")
                if (trimmed.EndsWith(':'))
                {
                    current.Attributes.Add(new SiiAttribute(trimmed[..^1], ""));
                    continue;
                }
                throw Error(lineNo, $"atributo inválido: '{trimmed}'");
            }
            current.Attributes.Add(new SiiAttribute(trimmed[..sep], trimmed[(sep + 2)..]));
        }

        if (!seenHeader) throw new InvalidDataException("Documento SII vazio.");
        if (current is not null) throw new InvalidDataException($"Bloco '{current.Id}' não foi fechado.");
        return doc;
    }

    public string ToText()
    {
        var sb = new StringBuilder();
        sb.Append("SiiNunit").Append(Newline).Append('{').Append(Newline);
        foreach (var unit in Units)
        {
            sb.Append(unit.Type).Append(" : ").Append(unit.Id).Append(" {").Append(Newline);
            foreach (var a in unit.Attributes)
                sb.Append(' ').Append(a.Name).Append(": ").Append(a.Value).Append(Newline);
            sb.Append('}').Append(Newline).Append(Newline);
        }
        sb.Append('}').Append(Newline);
        return sb.ToString();
    }

    /// <summary>Grava em texto puro, criando antes um backup do arquivo existente.</summary>
    public string? Save(string path)
    {
        string? backup = null;
        if (File.Exists(path))
        {
            backup = $"{path}.bak-{DateTime.Now:yyyyMMdd-HHmmss}";
            File.Copy(path, backup, overwrite: false);
        }
        File.WriteAllText(path, ToText(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        return backup;
    }

    private static SiiUnit ParseUnitHeader(string line, int lineNo)
    {
        // "tipo : id {"
        if (!line.EndsWith('{')) throw Error(lineNo, $"esperado início de bloco: '{line}'");
        var parts = line[..^1].Split(" : ", 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2 || parts[0].Length == 0 || parts[1].Length == 0)
            throw Error(lineNo, $"cabeçalho de bloco inválido: '{line}'");
        return new SiiUnit(parts[0], parts[1]);
    }

    private static InvalidDataException Error(int line, string msg) => new($"Linha {line}: {msg}.");
}
