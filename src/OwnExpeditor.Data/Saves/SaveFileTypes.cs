using OwnExpeditor.Domain.Saves;

namespace OwnExpeditor.Data.Saves;

/// <summary>Identidade do conteúdo de um arquivo de save em um instante (detecta mudança no disco).</summary>
public sealed record SaveFingerprint(long Length, DateTime LastWriteUtc, string Sha256);

public sealed record SaveFile(string Path, byte[] Bytes, SaveFingerprint Fingerprint);

/// <summary><c>VerifyWritten</c> recebe o texto relido do <c>.tmp</c>; falso (ou exceção) aborta a gravação.</summary>
public sealed record SaveWriteRequest(string Path, SaveFingerprint Expected, string NewText,
                                      Func<string, bool> VerifyWritten);

public sealed record SaveWriteReceipt(string BackupPath, SaveFingerprint NewFingerprint);

public sealed record NewSlotWriteRequest(SaveSlotInfo Source, SaveFingerprint Expected, string NewText,
                                         string DisplayName, Func<string, bool> VerifyWritten);

public sealed record NewSlotReceipt(SaveSlotInfo NewSlot);

public enum SaveWriteFailure
{
    NotFound, ChangedOnDisk, BackupExists, BackupFailed, WriteFailed, VerifyFailed, AccessDenied,
    /// <summary>Não foi possível alocar uma pasta de slot nova (só em <c>WriteNewSlotAsync</c>).</summary>
    SlotUnavailable,
}

public sealed class SaveWriteException(SaveWriteFailure failure, string message, Exception? inner = null)
    : IOException(message, inner)
{
    public SaveWriteFailure Failure { get; } = failure;
}

public interface ISaveFileStore
{
    /// <summary>Lê o arquivo sem alterá-lo. Erros de E/S (arquivo ausente, acesso negado) propagam como são.</summary>
    Task<SaveFile> ReadAsync(string path, CancellationToken ct);

    /// <summary>
    /// Confere <c>Expected</c> → backup <c>game.sii.bak-yyyyMMdd-HHmmss</c> (nunca sobrescreve) →
    /// <c>game.sii.tmp</c> UTF-8 sem BOM + <c>Flush(true)</c> → <c>VerifyWritten</c> → substitui.
    /// Qualquer falha apaga o <c>.tmp</c>, deixa o original intacto e lança <see cref="SaveWriteException"/>.
    /// </summary>
    Task<SaveWriteReceipt> WriteAtomicAsync(SaveWriteRequest request, CancellationToken ct);

    /// <summary>
    /// Copia a pasta do slot para o próximo slot livre do mesmo perfil, grava o <c>game.sii</c> novo
    /// (<c>.tmp</c> + verificação) e ajusta o nome em <c>info.sii</c>. O original não é tocado;
    /// em falha a pasta nova é apagada.
    /// </summary>
    Task<NewSlotReceipt> WriteNewSlotAsync(NewSlotWriteRequest request, CancellationToken ct);
}
