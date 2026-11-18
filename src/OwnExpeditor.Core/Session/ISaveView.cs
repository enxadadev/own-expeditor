namespace OwnExpeditor.Core.Session;

/// <summary>Unidade (bloco) do save, somente leitura.</summary>
public sealed class SaveUnitView
{
    internal SaveUnitView() { }

    public string Type => throw new NotImplementedException();
    public string Id => throw new NotImplementedException();
    public string? Get(string attribute) => throw new NotImplementedException();
    public IReadOnlyList<string> GetArray(string attribute) => throw new NotImplementedException();
}

public interface ISaveView
{
    SaveUnitView? Find(string unitId);
    IReadOnlyList<SaveUnitView> OfType(string type);
}
