using System.Security.Cryptography;
using System.Text;
using OwnExpeditor.Core.Games;
using OwnExpeditor.Core.Internal;
using OwnExpeditor.Data.Games;
using OwnExpeditor.Data.Saves;
using OwnExpeditor.Domain.Games;
using OwnExpeditor.Domain.Saves;
using OwnExpeditor.Sii;

namespace OwnExpeditor.Core.Session;

/// <summary>O que a sessão precisa para gravar; ausente = sessão somente leitura (<c>FromText</c>).</summary>
internal sealed record WriteContext(ISaveFileStore Store, IGameProcessMonitor Process, SaveFingerprint Fingerprint);

/// <summary>
/// Sessão de edição: documento base (nunca alterado) + mudanças pendentes. Toda gravação passa por
/// <see cref="ISaveFileStore"/> (skill save-safety). Usada na thread da UI; só <c>ApplyAsync</c> é assíncrono.
/// </summary>
internal sealed class EditSession : IEditSession
{
    private static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false);

    private readonly object _gate = new();
    private SiiDocument _document;
    private SaveSlotInfo _save;
    private IGameData _gameData;
    private WriteContext? _context;
    private List<ChangeSet> _pending = [];
    private SaveView _original;
    private SaveView _view;
    private int _applying;
    private volatile bool _closed;

    public EditSession(SaveSlotInfo save, SiiDocument document, IGameData gameData, WriteContext? context)
    {
        _save = save;
        _document = document;
        _gameData = gameData;
        _context = context;
        _original = _view = new SaveView(document);
    }

    public SaveSlotInfo Save { get { lock (_gate) return _save; } }

    public GameKind Game => Save.Profile.Game;

    public bool IsReadOnly => _context is null;

    public ISaveView Original { get { lock (_gate) return _original; } }

    public ISaveView View { get { lock (_gate) return _view; } }

    public IGameData GameData { get { lock (_gate) return _gameData; } }

    public IReadOnlyList<ChangeSet> Pending { get { lock (_gate) return _pending.ToArray(); } }

    public bool HasChanges { get { lock (_gate) return _pending.Count > 0; } }

    public event EventHandler? Changed;

    /// <summary>Monta o índice por tipo (etapa Index do carregamento).</summary>
    internal void EnsureIndexed() => _original.EnsureIndexed();

    /// <summary>Depois de fechada pelo host, a sessão recusa <c>Stage</c> e <c>ApplyAsync</c> (<c>NoSession</c>).</summary>
    internal void Close() => _closed = true;

    /// <summary>Troca os dados do jogo (pasta ou idioma mudaram); as pendências continuam.</summary>
    internal void UpdateGameData(IGameData data)
    {
        if (_closed) return;
        lock (_gate) _gameData = data;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    // ---- edição

    public void Stage(ChangeSet change)
    {
        ArgumentNullException.ThrowIfNull(change);
        EnsureUsable();
        if (change.Patches.Count == 0)
            throw new CoreException(CoreError.InvalidEdit, $"A mudança '{change.Key}' não tem alterações.");

        lock (_gate)
        {
            var replaced = _pending.FindIndex(c => c.Key == change.Key);
            var others = Overlay(_pending.Where((_, i) => i != replaced));
            Validate(change, others);

            var next = new List<ChangeSet>(_pending);
            if (replaced >= 0) next[replaced] = change;
            else next.Add(change);
            Commit(next);
        }
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public bool Discard(Guid changeSetId)
    {
        lock (_gate)
        {
            var index = _pending.FindIndex(c => c.Id == changeSetId);
            if (index < 0) return false;
            var next = new List<ChangeSet>(_pending);
            next.RemoveAt(index);
            Commit(next);
        }
        Changed?.Invoke(this, EventArgs.Empty);
        return true;
    }

    public void DiscardModule(string moduleId)
    {
        ArgumentNullException.ThrowIfNull(moduleId);
        lock (_gate)
        {
            var next = _pending.Where(c => c.ModuleId != moduleId).ToList();
            if (next.Count == _pending.Count) return;
            Commit(next);
        }
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void DiscardAll()
    {
        lock (_gate)
        {
            if (_pending.Count == 0) return;
            Commit([]);
        }
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public SessionDiff GetDiff()
    {
        ChangeSet[] pending;
        lock (_gate) pending = _pending.ToArray();
        var modules = pending
            .GroupBy(c => c.ModuleId)
            .Select(g => new ModuleDiff(g.Key, g.ToList()))
            .ToList();
        return new SessionDiff(modules, pending.Sum(c => c.Patches.Count));
    }

    private void EnsureUsable()
    {
        if (_closed) throw new CoreException(CoreError.NoSession, "A sessão foi fechada.");
        if (Volatile.Read(ref _applying) != 0) throw new CoreException(CoreError.SessionBusy, "A sessão está gravando.");
    }

    /// <summary>Valida patch a patch contra o documento base + as outras mudanças; não altera nada.</summary>
    private void Validate(ChangeSet change, Dictionary<string, Dictionary<string, string>> others)
    {
        var own = new Dictionary<(string, string), string>();
        foreach (var patch in change.Patches)
        {
            if (patch is not SetAttribute set)
                throw new CoreException(CoreError.InvalidEdit, $"Tipo de patch sem suporte: {patch.GetType().Name}.");
            var unit = _document.Find(set.UnitId)
                ?? throw new CoreException(CoreError.UnknownUnit, $"Unidade '{set.UnitId}' não existe no save.");
            if (!unit.Attributes.Any(a => a.Name == set.Attribute))
                throw new CoreException(CoreError.UnknownAttribute, $"Atributo '{set.Attribute}' não existe em '{set.UnitId}'.");
            if (others.TryGetValue(set.UnitId, out var touched) && touched.ContainsKey(set.Attribute))
                throw new CoreException(CoreError.Conflict, $"'{set.UnitId}.{set.Attribute}' já é alterado por outra mudança pendente.");
            var current = own.TryGetValue((set.UnitId, set.Attribute), out var v) ? v : unit.Get(set.Attribute);
            if (current != set.OldValue)
                throw new CoreException(CoreError.StalePatch,
                    $"'{set.UnitId}.{set.Attribute}' mudou: esperado '{set.OldValue}', atual '{current}'.");
            own[(set.UnitId, set.Attribute)] = set.NewValue;
        }
    }

    private void Commit(List<ChangeSet> pending)
    {
        _pending = pending;
        _view = pending.Count == 0 ? _original : new SaveView(_document, Overlay(pending));
    }

    private static Dictionary<string, Dictionary<string, string>> Overlay(IEnumerable<ChangeSet> changes)
    {
        var overlay = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
        foreach (var patch in changes.SelectMany(c => c.Patches).OfType<SetAttribute>())
        {
            if (!overlay.TryGetValue(patch.UnitId, out var attributes))
                overlay[patch.UnitId] = attributes = new Dictionary<string, string>(StringComparer.Ordinal);
            attributes[patch.Attribute] = patch.NewValue;
        }
        return overlay;
    }

    // ---- gravação

    // Sem ConfigureAwait(false) de propósito: o estado da sessão e o evento Changed voltam ao contexto de quem chamou (UI).
    public async Task<ApplyResult> ApplyAsync(ApplyTarget target, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (_closed) throw new CoreException(CoreError.NoSession, "A sessão foi fechada.");
        if (_context is null) throw new CoreException(CoreError.ReadOnlySession, "Esta sessão é somente leitura.");
        if (Interlocked.CompareExchange(ref _applying, 1, 0) != 0)
            throw new CoreException(CoreError.SessionBusy, "A sessão já está gravando.");
        try
        {
            var result = await ApplyCoreAsync(target, _context, ct);
            Changed?.Invoke(this, EventArgs.Empty);
            return result;
        }
        finally
        {
            Volatile.Write(ref _applying, 0);
        }
    }

    private async Task<ApplyResult> ApplyCoreAsync(ApplyTarget target, WriteContext context, CancellationToken ct)
    {
        string? newName = null;
        switch (target)
        {
            case ApplyTarget.Overwrite:
                break;
            case ApplyTarget.NewSlot named:
                newName = named.DisplayName?.Trim();
                if (string.IsNullOrEmpty(newName))
                    throw new CoreException(CoreError.InvalidEdit, "O nome do novo save não pode ser vazio.");
                break;
            default:
                throw new CoreException(CoreError.InvalidEdit, $"Destino de gravação desconhecido: {target.GetType().Name}.");
        }

        var (pending, save) = Snapshot();
        if (pending.Length == 0) throw new CoreException(CoreError.NothingToApply, "Não há mudanças para gravar.");
        if (context.Process.IsRunning(save.Profile.Game))
            throw new CoreException(CoreError.GameRunning, "O jogo está aberto; feche-o antes de gravar.");

        ct.ThrowIfCancellationRequested();
        SaveFile file;
        try
        {
            file = await context.Store.ReadAsync(save.GameSiiPath, ct);
        }
        catch (Exception e) when (CoreErrors.IsReadFailure(e))
        {
            throw CoreErrors.FromRead(e);
        }
        if (file.Fingerprint.Length != context.Fingerprint.Length || file.Fingerprint.Sha256 != context.Fingerprint.Sha256)
            throw new CoreException(CoreError.SaveChangedOnDisk, "O save mudou no disco desde que foi aberto.");

        var plan = await Task.Run(() => BuildWrite(file.Bytes, pending), ct);
        SaveFingerprint written;
        string? backup;
        SaveSlotInfo slot;
        try
        {
            if (newName is null)
            {
                var receipt = await context.Store.WriteAtomicAsync(
                    new SaveWriteRequest(save.GameSiiPath, context.Fingerprint, plan.Text, plan.Verify), ct);
                written = receipt.NewFingerprint;
                backup = receipt.BackupPath;
                slot = save with { LastWriteUtc = written.LastWriteUtc, SizeBytes = written.Length };
            }
            else
            {
                var receipt = await context.Store.WriteNewSlotAsync(
                    new NewSlotWriteRequest(save, context.Fingerprint, plan.Text, newName, plan.Verify), ct);
                slot = receipt.NewSlot;
                backup = null; // o original não é tocado, não há backup
                written = new SaveFingerprint(slot.SizeBytes, slot.LastWriteUtc, Convert.ToHexString(SHA256.HashData(Utf8.GetBytes(plan.Text))));
            }
        }
        catch (SaveWriteException e)
        {
            throw CoreErrors.FromSaveWrite(e);
        }

        lock (_gate)
        {
            _document = plan.Document;
            _save = slot;
            _context = context with { Fingerprint = written };
            _original = new SaveView(plan.Document);
            Commit([]);
        }
        return new ApplyResult(slot, backup, pending.Length, pending.Sum(c => c.Patches.Count));
    }

    private (ChangeSet[] Pending, SaveSlotInfo Save) Snapshot()
    {
        lock (_gate) return (_pending.ToArray(), _save);
    }

    private sealed record WritePlan(SiiDocument Document, string Text, Func<string, bool> Verify);

    /// <summary>Reparseia os bytes lidos agora, aplica os patches em sequência e monta o texto e a verificação.</summary>
    private static WritePlan BuildWrite(byte[] bytes, ChangeSet[] pending)
    {
        SiiDocument document;
        try
        {
            document = SiiDocument.Parse(SiiDecoder.DecodeToText(bytes));
        }
        catch (SiiFormatException e)
        {
            throw CoreErrors.FromSii(e);
        }

        var finals = new Dictionary<(string Unit, string Attribute), string>();
        foreach (var patch in pending.SelectMany(c => c.Patches).OfType<SetAttribute>())
        {
            var unit = document.Find(patch.UnitId)
                ?? throw new CoreException(CoreError.UnknownUnit, $"Unidade '{patch.UnitId}' não existe no save.");
            var current = unit.Get(patch.Attribute)
                ?? throw new CoreException(CoreError.UnknownAttribute, $"Atributo '{patch.Attribute}' não existe em '{patch.UnitId}'.");
            if (current != patch.OldValue)
                throw new CoreException(CoreError.StalePatch,
                    $"'{patch.UnitId}.{patch.Attribute}' mudou: esperado '{patch.OldValue}', atual '{current}'.");
            unit.Set(patch.Attribute, patch.NewValue);
            finals[(patch.UnitId, patch.Attribute)] = patch.NewValue;
        }

        var expectedBlocks = document.Units.Count;
        bool Verify(string written)
        {
            try
            {
                var check = SiiDocument.Parse(written);
                return check.Units.Count == expectedBlocks
                    && finals.All(f => check.Find(f.Key.Unit)?.Get(f.Key.Attribute) == f.Value);
            }
            catch (SiiFormatException)
            {
                return false;
            }
        }

        return new WritePlan(document, document.ToText(), Verify);
    }
}
