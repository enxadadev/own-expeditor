using OwnExpeditor.Domain.Saves;

namespace OwnExpeditor.Core.Session;

public static class EditSessionFactory
{
    /// <summary>Sessão somente leitura a partir de texto SII (amostras e testes); ApplyAsync → ReadOnlySession.</summary>
    public static IEditSession FromText(string siiText, SaveSlotInfo slot) => throw new NotImplementedException();
}
