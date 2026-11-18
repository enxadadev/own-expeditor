using System.Globalization;
using OwnExpeditor.Core.Internal;
using OwnExpeditor.Data;
using OwnExpeditor.Domain.Saves;
using OwnExpeditor.Sii;

namespace OwnExpeditor.Core.Legacy;

/// <summary>
/// Fachada do fluxo atual (ler save, editar fretes, gravar novo save) para o App antigo, que não
/// referencia Data/Sii. Existe até a F7 (ADR 0006), quando é apagada junto com <see cref="SaveSession"/>.
/// Não estender: gravação em slot novo sem <c>.tmp</c> (risco 3 do plano 0006).
/// </summary>
[Obsolete("Fachada transitória do shell antigo (ADR 0006). Use IEditSession / IProfileService.")]
public static class LegacyJobs
{
    public static string DefaultGameDir() => SaveLocator.DefaultGameDir();

    public static string DefaultDocumentsDir() => SaveLocator.DefaultDocumentsDir();

    /// <summary>Lê def/dlc/locale da pasta do jogo; o idioma vem do config.cfg ou da cultura da UI.</summary>
    public static LegacyGameData LoadGameData(string gameDir, string documentsDir)
    {
        var language = Localization.GameLanguage(documentsDir)
            ?? CultureInfo.CurrentUICulture.Name.Replace('-', '_').ToLowerInvariant();
        return new LegacyGameData(GameCatalog.LoadFromGameDir(gameDir, language));
    }

    public static IReadOnlyList<ProfileInfo> Profiles(string documentsDir) => SaveLocator.Profiles(documentsDir);

    public static IReadOnlyList<SaveSlotInfo> Saves(ProfileInfo profile) => SaveLocator.Slots(profile);

    public static SaveSession Open(SaveSlotInfo save, LegacyGameData data)
    {
        ArgumentNullException.ThrowIfNull(save);
        ArgumentNullException.ThrowIfNull(data);
        SiiDocument doc;
        try
        {
            doc = SiiDocument.Load(save.GameSiiPath);
        }
        catch (SiiFormatException e)
        {
            throw ToCoreError(e);
        }
        return new SaveSession(doc, data.Catalog);
    }

    /// <summary>Grava o save editado em pasta nova; o original não é tocado. Retorna a pasta criada.</summary>
    public static string WriteAsNewSave(SaveSlotInfo source, SaveSession session, string name)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(session);
        var folder = Path.GetDirectoryName(source.GameSiiPath)
            ?? throw new InvalidDataException($"Caminho de save inválido: '{source.GameSiiPath}'.");
        var legacy = new SaveInfo(folder, source.Id, source.DisplayName);
        return SaveLocator.WriteAsNewSave(source.Profile, legacy, session.Document, name);
    }

    internal static CoreException ToCoreError(SiiFormatException e) => CoreErrors.FromSii(e);
}
