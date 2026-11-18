using System.Security.Cryptography;
using System.Text;
using OwnExpeditor.Domain.Saves;
using OwnExpeditor.Sii;

namespace OwnExpeditor.Data.Saves;

/// <summary>
/// Único caminho de gravação de save (skill save-safety): confere o arquivo, faz backup, grava em <c>.tmp</c>
/// com <c>Flush(true)</c>, relê e verifica, e só então substitui. Falha = original intacto.
/// </summary>
internal sealed class SaveFileStore(TimeProvider? time = null) : ISaveFileStore
{
    private static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false);
    private readonly TimeProvider _time = time ?? TimeProvider.System;

    public async Task<SaveFile> ReadAsync(string path, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        ct.ThrowIfCancellationRequested();
        var bytes = await File.ReadAllBytesAsync(path, ct);
        return new SaveFile(path, bytes, FingerprintOf(path, bytes));
    }

    public async Task<SaveWriteReceipt> WriteAtomicAsync(SaveWriteRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        ct.ThrowIfCancellationRequested();
        var path = request.Path;

        await EnsureUnchangedAsync(path, request.Expected, ct);
        var backup = CreateBackup(path);
        var fingerprint = await WriteVerifiedAsync(path, request.NewText, request.VerifyWritten, ct);
        return new SaveWriteReceipt(backup, fingerprint);
    }

    public async Task<NewSlotReceipt> WriteNewSlotAsync(NewSlotWriteRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var name = SanitizeName(request.DisplayName);
        ct.ThrowIfCancellationRequested();

        var source = request.Source;
        await EnsureUnchangedAsync(source.GameSiiPath, request.Expected, ct);
        var sourceDir = Path.GetDirectoryName(source.GameSiiPath)
            ?? throw new SaveWriteException(SaveWriteFailure.NotFound, $"Caminho de save inválido: '{source.GameSiiPath}'.");

        var target = AllocateSlot(source.Profile);
        var newGameSii = Path.Combine(target, "game.sii");
        try
        {
            CopyTree(sourceDir, target, topLevel: true, ct);
            var fingerprint = await WriteVerifiedAsync(newGameSii, request.NewText, request.VerifyWritten, ct);
            await RenameInfoAsync(Path.Combine(target, "info.sii"), name, ct);
            return new NewSlotReceipt(new SaveSlotInfo(source.Profile, Path.GetFileName(target), name, newGameSii,
                fingerprint.LastWriteUtc, fingerprint.Length));
        }
        catch (Exception e)
        {
            TryDeleteDirectory(target);
            throw Wrap(e, SaveWriteFailure.WriteFailed, "Não foi possível criar o novo save.");
        }
    }

    // ---- etapas

    private static async Task EnsureUnchangedAsync(string path, SaveFingerprint expected, CancellationToken ct)
    {
        byte[] current;
        try
        {
            current = await File.ReadAllBytesAsync(path, ct);
        }
        catch (Exception e) when (e is FileNotFoundException or DirectoryNotFoundException)
        {
            throw new SaveWriteException(SaveWriteFailure.NotFound, $"Save não encontrado: '{path}'.", e);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            throw Wrap(e, SaveWriteFailure.WriteFailed, $"Não foi possível ler '{path}'.");
        }

        if (current.LongLength != expected.Length || Sha256Hex(current) != expected.Sha256)
            throw new SaveWriteException(SaveWriteFailure.ChangedOnDisk,
                $"O arquivo mudou desde que foi lido: '{path}'.");
    }

    /// <summary>Copia <c>game.sii</c> para <c>game.sii.bak-yyyyMMdd-HHmmss</c> ao lado; nunca sobrescreve.</summary>
    private string CreateBackup(string path)
    {
        var backup = $"{path}.bak-{_time.GetLocalNow():yyyyMMdd-HHmmss}";
        if (File.Exists(backup))
            throw new SaveWriteException(SaveWriteFailure.BackupExists, $"Já existe um backup com este nome: '{backup}'.");
        try
        {
            File.Copy(path, backup, overwrite: false);
            if (new FileInfo(backup).Length != new FileInfo(path).Length)
                throw new IOException("O backup ficou com tamanho diferente do original.");
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            TryDeleteFile(backup); // cópia parcial nunca fica como "backup"
            throw e is UnauthorizedAccessException
                ? new SaveWriteException(SaveWriteFailure.AccessDenied, $"Sem permissão para criar o backup em '{backup}'.", e)
                : new SaveWriteException(SaveWriteFailure.BackupFailed, $"Não foi possível criar o backup '{backup}'.", e);
        }
        return backup;
    }

    /// <summary>.tmp (UTF-8 sem BOM, Flush(true)) → relê → confere → verifica → substitui o destino.</summary>
    private static async Task<SaveFingerprint> WriteVerifiedAsync(string path, string text,
        Func<string, bool> verify, CancellationToken ct)
    {
        var tmp = path + ".tmp";
        try
        {
            var data = Utf8.GetBytes(text);
            await using (var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None,
                             bufferSize: 81920, useAsync: true))
            {
                await fs.WriteAsync(data, ct);
                await fs.FlushAsync(ct);
                fs.Flush(flushToDisk: true);
            }

            ct.ThrowIfCancellationRequested();
            var readBack = await File.ReadAllBytesAsync(tmp, ct);
            if (!readBack.AsSpan().SequenceEqual(data))
                throw new SaveWriteException(SaveWriteFailure.WriteFailed, "O arquivo gravado difere do esperado.");

            bool ok;
            try
            {
                ok = verify(Utf8.GetString(readBack));
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                throw new SaveWriteException(SaveWriteFailure.VerifyFailed, $"A verificação falhou: {e.Message}", e);
            }
            if (!ok) throw new SaveWriteException(SaveWriteFailure.VerifyFailed, "A verificação do arquivo gravado falhou.");

            ct.ThrowIfCancellationRequested();
            File.Move(tmp, path, overwrite: true);
            return new FingerprintBuilder(path, readBack).Build();
        }
        catch (Exception e)
        {
            TryDeleteFile(tmp);
            throw Wrap(e, SaveWriteFailure.WriteFailed, $"Não foi possível gravar '{path}'.");
        }
    }

    private static async Task RenameInfoAsync(string infoPath, string name, CancellationToken ct)
    {
        if (!File.Exists(infoPath)) return;
        SiiDocument info;
        try
        {
            info = SiiDocument.Load(infoPath);
        }
        catch (Exception e) when (e is SiiFormatException or InvalidDataException or NotSupportedException)
        {
            throw new SaveWriteException(SaveWriteFailure.WriteFailed, "Não foi possível ler o info.sii do slot.", e);
        }

        var unit = info.Units.FirstOrDefault();
        if (unit?.Get("name") is null) return;
        unit.Set("name", $"\"{name}\"");
        var expected = info.ToText();
        await WriteVerifiedAsync(infoPath, expected, t => t == expected, ct);
    }

    // ---- slot novo

    private static string AllocateSlot(ProfileInfo profile)
    {
        try
        {
            var target = SaveLocator.NextSlot(profile);
            if (Directory.Exists(target) || File.Exists(target))
                throw new SaveWriteException(SaveWriteFailure.SlotUnavailable, $"O slot '{target}' já existe.");
            Directory.CreateDirectory(target);
            return target;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            throw Wrap(e, SaveWriteFailure.SlotUnavailable, "Não foi possível criar a pasta do novo slot.");
        }
    }

    private static void CopyTree(string from, string to, bool topLevel, CancellationToken ct)
    {
        Directory.CreateDirectory(to);
        foreach (var file in Directory.EnumerateFiles(from))
        {
            ct.ThrowIfCancellationRequested();
            var name = Path.GetFileName(file);
            // game.sii vem do NewText; backups e temporários do slot original não são clonados
            var skip = (topLevel && name.StartsWith("game.sii", StringComparison.OrdinalIgnoreCase))
                       || name.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase);
            if (skip) continue;
            File.Copy(file, Path.Combine(to, name), overwrite: false);
        }
        foreach (var dir in Directory.EnumerateDirectories(from))
            CopyTree(dir, Path.Combine(to, Path.GetFileName(dir)), topLevel: false, ct);
    }

    private static string SanitizeName(string displayName)
    {
        ArgumentNullException.ThrowIfNull(displayName);
        var name = new string(displayName.Where(c => c != '"' && !char.IsControl(c)).ToArray()).Trim();
        if (name.Length == 0) throw new ArgumentException("O nome do novo save não pode ser vazio.", nameof(displayName));
        return name;
    }

    // ---- utilidades

    private static SaveFingerprint FingerprintOf(string path, byte[] bytes) =>
        new FingerprintBuilder(path, bytes).Build();

    private static string Sha256Hex(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));

    private readonly record struct FingerprintBuilder(string Path, byte[] Bytes)
    {
        public SaveFingerprint Build() =>
            new(Bytes.LongLength, File.GetLastWriteTimeUtc(Path), Sha256Hex(Bytes));
    }

    /// <summary>Exceções do domínio de gravação e cancelamento passam; E/S vira <see cref="SaveWriteException"/>.</summary>
    private static Exception Wrap(Exception e, SaveWriteFailure fallback, string message) => e switch
    {
        SaveWriteException or OperationCanceledException or ArgumentException => e,
        UnauthorizedAccessException => new SaveWriteException(SaveWriteFailure.AccessDenied, "Sem permissão de acesso.", e),
        IOException io when IsSharingViolation(io) => new SaveWriteException(SaveWriteFailure.AccessDenied, "Arquivo em uso por outro programa.", e),
        IOException => new SaveWriteException(fallback, message, e),
        _ => e,
    };

    private static bool IsSharingViolation(IOException e) => OperatingSystem.IsWindows() && (e.HResult & 0xFFFF) is 32 or 33;

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // limpeza de melhor esforço
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path)) Directory.Delete(path, recursive: true);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // limpeza de melhor esforço
        }
    }
}
