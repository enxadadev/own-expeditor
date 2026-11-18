using OwnExpeditor.Sii;

namespace OwnExpeditor.Core.Session;

/// <summary>Unidade (bloco) do save, somente leitura. Valores já incluem as mudanças pendentes quando vem de <c>View</c>.</summary>
public sealed class SaveUnitView
{
    private readonly SiiUnit _unit;
    private readonly IReadOnlyDictionary<string, string>? _overrides;

    internal SaveUnitView(SiiUnit unit, IReadOnlyDictionary<string, string>? overrides)
    {
        _unit = unit;
        _overrides = overrides;
    }

    public string Type => _unit.Type;

    public string Id => _unit.Id;

    public string? Get(string attribute)
    {
        ArgumentNullException.ThrowIfNull(attribute);
        return _overrides is not null && _overrides.TryGetValue(attribute, out var value) ? value : _unit.Get(attribute);
    }

    /// <summary>Valores de um array (<c>nome[0]</c>, <c>nome[1]</c>, ...) na ordem do arquivo; vazio se não existir.</summary>
    public IReadOnlyList<string> GetArray(string attribute)
    {
        ArgumentNullException.ThrowIfNull(attribute);
        var prefix = attribute + "[";
        var values = new List<string>();
        foreach (var a in _unit.Attributes)
        {
            if (!a.Name.StartsWith(prefix, StringComparison.Ordinal)) continue;
            values.Add(_overrides is not null && _overrides.TryGetValue(a.Name, out var value) ? value : a.Value);
        }
        return values;
    }
}

public interface ISaveView
{
    SaveUnitView? Find(string unitId);
    IReadOnlyList<SaveUnitView> OfType(string type);
}
