using OwnExpeditor.Sii;

namespace OwnExpeditor.Core.Session;

/// <summary>
/// Visão somente leitura de um <see cref="SiiDocument"/> (nunca alterado) com um overlay de valores
/// pendentes (unidade → atributo → valor). Os <see cref="SaveUnitView"/> são criados sob demanda.
/// </summary>
internal sealed class SaveView(SiiDocument document, IReadOnlyDictionary<string, Dictionary<string, string>>? overlay = null)
    : ISaveView
{
    private readonly object _gate = new();
    private readonly Dictionary<SiiUnit, SaveUnitView> _units = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<string, IReadOnlyList<SaveUnitView>> _types = new(StringComparer.Ordinal);

    public SaveUnitView? Find(string unitId)
    {
        ArgumentNullException.ThrowIfNull(unitId);
        var unit = document.Find(unitId);
        return unit is null ? null : Wrap(unit);
    }

    public IReadOnlyList<SaveUnitView> OfType(string type)
    {
        ArgumentNullException.ThrowIfNull(type);
        lock (_gate)
        {
            if (_types.TryGetValue(type, out var hit)) return hit;
            var views = document.OfType(type).Select(WrapLocked).ToList();
            _types[type] = views;
            return views;
        }
    }

    /// <summary>Monta o índice por tipo do documento (etapa Index do carregamento).</summary>
    public void EnsureIndexed() => document.OfType(string.Empty);

    private SaveUnitView Wrap(SiiUnit unit)
    {
        lock (_gate) return WrapLocked(unit);
    }

    private SaveUnitView WrapLocked(SiiUnit unit)
    {
        if (_units.TryGetValue(unit, out var hit)) return hit;
        // Id repetido: o documento resolve Find pelo primeiro bloco, então só ele recebe o overlay.
        IReadOnlyDictionary<string, string>? values = null;
        if (overlay is not null && overlay.TryGetValue(unit.Id, out var map) && ReferenceEquals(document.Find(unit.Id), unit))
            values = map;
        var view = new SaveUnitView(unit, values);
        _units[unit] = view;
        return view;
    }
}
