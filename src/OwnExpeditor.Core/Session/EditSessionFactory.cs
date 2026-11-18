using OwnExpeditor.Core.Games;
using OwnExpeditor.Core.Internal;
using OwnExpeditor.Domain.Saves;
using OwnExpeditor.Sii;

namespace OwnExpeditor.Core.Session;

public static class EditSessionFactory
{
    /// <summary>Sessão somente leitura a partir de texto SII (amostras e testes); ApplyAsync → ReadOnlySession.</summary>
    public static IEditSession FromText(string siiText, SaveSlotInfo slot) =>
        FromText(siiText, slot, new UnavailableGameData(slot.Profile.Game));

    /// <summary>Igual a <see cref="FromText(string, SaveSlotInfo)"/>, com os dados do jogo informados (nomes, regras).</summary>
    public static IEditSession FromText(string siiText, SaveSlotInfo slot, IGameData gameData)
    {
        ArgumentNullException.ThrowIfNull(siiText);
        ArgumentNullException.ThrowIfNull(slot);
        ArgumentNullException.ThrowIfNull(gameData);
        try
        {
            return new EditSession(slot, SiiDocument.Parse(siiText), gameData, context: null);
        }
        catch (SiiFormatException e)
        {
            throw CoreErrors.FromSii(e);
        }
    }
}
