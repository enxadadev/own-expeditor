# Plano de implementação — ADR 0005 (shell modular e sessão de edição)

Fonte: `chief-architect`, 2026-11-17. Decisão em
[`../decisions/0005-shell-modular-e-sessao-de-edicao.md`](../decisions/0005-shell-modular-e-sessao-de-edicao.md).
Desenhos de tela em `design/` (o plano original citava `docs/ux/`; o projeto
usa `design/`). Cada tarefa recebe só o contexto listado.

## Contratos

### Domain (sem dependências)
```csharp
namespace OwnExpeditor.Domain.Games;
public enum GameKind { Ets2, Ats }

namespace OwnExpeditor.Domain.Saves;
public sealed record ProfileInfo(GameKind Game, string Id, string DisplayName, string DirectoryPath);
public sealed record SaveSlotInfo(ProfileInfo Profile, string Id, string DisplayName, string GameSiiPath, DateTime LastWriteUtc);

namespace OwnExpeditor.Domain.Settings;
public enum ThemePreference { System, Light, Dark }
public enum Language { PtBr, En }
public sealed record AppSettings(ThemePreference Theme, Language Language, string? LastSavePath);
```

### Sii (mudanças mínimas)
```csharp
public sealed class SiiDocument
{
    // índice por tipo montado no Parse (hoje é LINQ linear)
    public IReadOnlyList<SiiUnit> OfType(string type);
    [Obsolete("Gravação de save: Core IEditSession.ApplyAsync → Data ISaveFileStore. ADR 0005.")]
    public string? Save(string path);
}
```

### Data (interfaces públicas; implementações `internal`)
```csharp
namespace OwnExpeditor.Data.Saves;
public sealed record SaveFingerprint(long Length, DateTime LastWriteUtc, string Sha256);
public sealed record SaveFile(string Path, byte[] Bytes, SaveFingerprint Fingerprint);
public sealed record SaveWriteRequest(string Path, SaveFingerprint Expected, string NewText,
                                      Func<string, bool> VerifyWritten); // recebe o texto relido do .tmp
public sealed record SaveWriteReceipt(string BackupPath, SaveFingerprint NewFingerprint);
public enum SaveWriteFailure { NotFound, ChangedOnDisk, BackupExists, BackupFailed, WriteFailed, VerifyFailed, AccessDenied }
public sealed class SaveWriteException(SaveWriteFailure failure, string message, Exception? inner = null)
    : IOException(message, inner) { public SaveWriteFailure Failure { get; } = failure; }

public interface ISaveFileStore
{
    Task<SaveFile> ReadAsync(string path, CancellationToken ct);
    // confere Expected → backup game.sii.bak-yyyyMMdd-HHmmss (overwrite:false)
    // → game.sii.tmp UTF-8 sem BOM + Flush(true) → VerifyWritten → File.Replace/Move.
    // Qualquer falha: apaga .tmp, original intacto, SaveWriteException.
    Task<SaveWriteReceipt> WriteAtomicAsync(SaveWriteRequest request, CancellationToken ct);
}
public interface IProfileLocator
{
    IReadOnlyList<ProfileInfo> FindProfiles();               // ETS2+ATS, profiles/ e steam_profiles/
    IReadOnlyList<SaveSlotInfo> FindSaves(ProfileInfo profile);
}
public interface IGameProcessMonitor { bool IsRunning(GameKind game); } // eurotrucks2 / amtrucks

namespace OwnExpeditor.Data.Settings;
public interface ISettingsStore   // ApplicationData/OwnExpeditor/settings.json
{
    Task<AppSettings?> LoadAsync(CancellationToken ct);       // null = primeira execução
    Task SaveAsync(AppSettings settings, CancellationToken ct);
}

namespace OwnExpeditor.Data;
public sealed record DataOptions { public string? DocumentsRoot { get; init; } public string? SettingsDirectory { get; init; } }
public static class DataServiceCollectionExtensions
{ public static IServiceCollection AddOwnExpeditorData(this IServiceCollection s, DataOptions? options = null); }
```

### Core
```csharp
namespace OwnExpeditor.Core;
public enum CoreError { SaveNotFound, SaveFormatUnsupported, SaveCorrupt, NoSession, SessionBusy, ReadOnlySession,
    Conflict, StalePatch, UnknownUnit, UnknownAttribute, NothingToApply, GameRunning, SaveChangedOnDisk,
    BackupExists, BackupFailed, WriteFailed, VerifyFailed, AccessDenied }
public sealed class CoreException(CoreError code, string? detail = null, Exception? inner = null)
    : Exception(detail ?? code.ToString(), inner) { public CoreError Code { get; } = code; }
public static class CoreServiceCollectionExtensions
{ public static IServiceCollection AddOwnExpeditorCore(this IServiceCollection s); } // AddOwnExpeditorData + AddXxxModule

namespace OwnExpeditor.Core.Text;
public sealed record TextRef(string Key, IReadOnlyList<object?> Args)
{ public static TextRef Of(string key, params object?[] args) => new(key, args); }
public abstract record DiffValue
{
    public sealed record Empty : DiffValue;
    public sealed record Raw(string Value) : DiffValue;
    public sealed record GameName(string Id) : DiffValue;                  // id → nome legível
    public sealed record Number(decimal Value, string FormatKey) : DiffValue; // "Format_Km" = "{0:N0} km"
    public sealed record Money(long Amount, GameKind Game) : DiffValue;
    public sealed record Localized(TextRef Text) : DiffValue;
}
public static class GameIds { public static string Humanize(string id); }

namespace OwnExpeditor.Core.Session;
public abstract record SavePatch(string UnitId);
public sealed record SetAttribute(string UnitId, string Attribute, string OldValue, string NewValue) : SavePatch(UnitId);
// Reservados para ADRs de módulo: SetArray, AddUnit, RemoveUnit (não implementar agora).

public sealed record DiffLine(TextRef Label, DiffValue Before, DiffValue After);
public sealed record ChangeSet(string ModuleId, string Key, TextRef Title,
                               IReadOnlyList<DiffLine> Lines, IReadOnlyList<SavePatch> Patches)
{ public Guid Id { get; init; } = Guid.NewGuid(); }
public sealed record ModuleDiff(string ModuleId, IReadOnlyList<ChangeSet> Changes);
public sealed record SessionDiff(IReadOnlyList<ModuleDiff> Modules, int PatchCount);
public sealed record ApplyResult(string SavePath, string BackupPath, int ChangeSetCount, int PatchCount);
public sealed record LoadProgress(int Percent, string StageKey);

public sealed class SaveUnitView
{
    public string Type { get; }
    public string Id { get; }
    public string? Get(string attribute);
    public IReadOnlyList<string> GetArray(string attribute);
}
public interface ISaveView
{
    SaveUnitView? Find(string unitId);
    IReadOnlyList<SaveUnitView> OfType(string type);
}

public interface IEditSession
{
    SaveSlotInfo Save { get; }
    GameKind Game { get; }
    bool IsReadOnly { get; }                 // true para EditSessionFactory.FromText
    ISaveView Original { get; }
    ISaveView View { get; }                  // original + pendentes
    IReadOnlyList<ChangeSet> Pending { get; }
    bool HasChanges { get; }
    event EventHandler? Changed;
    void Stage(ChangeSet change);            // mesma Key substitui; Conflict/StalePatch/UnknownUnit/UnknownAttribute
    bool Discard(Guid changeSetId);
    void DiscardModule(string moduleId);
    void DiscardAll();
    SessionDiff GetDiff();
    Task<ApplyResult> ApplyAsync(CancellationToken ct); // GameRunning→fingerprint→parse bytes→patches→texto→Data→nova base
}
public sealed class SessionChangedEventArgs(IEditSession? previous, IEditSession? current) : EventArgs
{ public IEditSession? Previous { get; } = previous; public IEditSession? Current { get; } = current; }
public interface IEditSessionHost
{
    IEditSession? Current { get; }
    event EventHandler<SessionChangedEventArgs>? SessionChanged;
    Task<IEditSession> OpenAsync(SaveSlotInfo save, IProgress<LoadProgress>? progress, CancellationToken ct);
    void Close();
}
public static class EditSessionFactory
{ public static IEditSession FromText(string siiText, SaveSlotInfo slot); } // ApplyAsync → ReadOnlySession

namespace OwnExpeditor.Core.Profiles;
public interface IProfileService
{
    Task<IReadOnlyList<ProfileInfo>> GetProfilesAsync(CancellationToken ct);
    Task<IReadOnlyList<SaveSlotInfo>> GetSavesAsync(ProfileInfo profile, CancellationToken ct);
}
namespace OwnExpeditor.Core.Settings;
public interface IAppSettingsService        // padrão: tema System; idioma pt-BR se SO pt-*, senão en
{
    AppSettings Current { get; }
    Task LoadAsync(CancellationToken ct);
    Task UpdateAsync(AppSettings settings, CancellationToken ct);
    event EventHandler<AppSettings>? Changed;
}

namespace OwnExpeditor.Core.Modules.Jobs;
public static class JobsModule { public const string Id = "jobs"; }
public static class JobsTextKeys { }
public static class JobsModuleRegistration { public static IServiceCollection AddJobsModule(this IServiceCollection s); }
```

### App
```csharp
namespace OwnExpeditor.App.Shell;
public sealed record ModuleDescriptor(string Id, string TitleKey, string IconKey, int Order, Type PageType, bool RequiresSession = true);
public static class ModuleRegistration
{
    public static IServiceCollection AddModule<TPage, TView>(this IServiceCollection s, string id, string titleKey,
        string iconKey, int order, bool requiresSession = true)
        where TPage : PageViewModelBase where TView : Control, new();
}
public static class ViewRegistry { public static void Register<TVm, TView>() where TView : Control, new(); public static Control? Build(object vm); }
public static class AppComposition
{
    public static IServiceProvider Build(Action<IServiceCollection>? overrides = null);
    public static void RegisterViews();
}

namespace OwnExpeditor.App.ViewModels;
public abstract class ViewModelBase : ObservableObject   // CultureChangedMessage → OnPropertyChanged("")
{ protected virtual void OnCultureChanged(); }
public enum PageState { Idle, Loading, Empty, Error, Ready }
public abstract partial class PageViewModelBase : ViewModelBase
{
    [ObservableProperty] private PageState _state;
    [ObservableProperty] private TextRef? _error;
    public virtual Task ActivateAsync(CancellationToken ct) => Task.CompletedTask;
}
public abstract partial class ModulePageViewModel : PageViewModelBase
{
    public abstract string ModuleId { get; }
    protected IEditSession? Session { get; }
    public int PendingCount { get; }
    public Task AttachAsync(IEditSession session, CancellationToken ct);
    public void Detach();
    protected abstract Task OnSessionOpenedAsync(IEditSession session, CancellationToken ct);
    protected virtual void OnSessionClosed() { }
}
// NavItemViewModel, SessionBarViewModel, ShellViewModel (Modules, SelectedItem, CurrentPage, SessionBar,
// PendingCount, ReviewCommand, ApplyCommand, DiscardAllCommand), ReviewChangesViewModel,
// ApplyResultViewModel, SettingsViewModel.

namespace OwnExpeditor.App.Services;
public interface ILocalizer
{
    string this[string key] { get; }
    string Get(string key, params object?[] args);
    string Format(TextRef text);
    string Format(DiffValue value);
    string FormatError(CoreError code);       // "Error_<Code>"
    CultureInfo Culture { get; }
    void SetLanguage(Language language);
}
public sealed class Localizer : ILocalizer, INotifyPropertyChanged { public static Localizer Instance { get; } }
public interface IThemeService { void Apply(ThemePreference theme); }
public interface IDialogService
{
    Task<bool> ConfirmAsync(TextRef title, TextRef message, TextRef confirmLabel, bool destructive);
    Task ShowErrorAsync(TextRef message);
}

namespace OwnExpeditor.App.Design;
public sealed record DesignSample(string Name, Func<ViewModelBase> Create, double Width = 1280, double Height = 800);
public interface IDesignSampleProvider { IEnumerable<DesignSample> GetSamples(); }
public static class DesignCatalog { public static IReadOnlyList<IDesignSampleProvider> Providers { get; } }
public static class DesignSaves { public static IEditSession Sample(); public static IEditSession Empty(); }
```

`OwnExpeditor.App.csproj`: `AvaloniaUseCompiledBindingsByDefault=true`,
`DisableTransitiveProjectReferences=true`, pacote
`Microsoft.Extensions.DependencyInjection`, remover `<Folder Include="Models\">`,
excluir `Gallery/**` em Release. Core: `Microsoft.Extensions.DependencyInjection.Abstractions`.

## Contrato de renderização (Views/ViewModels)
1. View com construtor sem parâmetros, só `InitializeComponent()`.
2. `x:DataType` em todas; compiled bindings ligados.
3. Nenhum texto literal (`Localizer.Instance[...]` ou VM).
4. Cores só por `DynamicResource`.
5. Construtor do VM sem efeito colateral (sem I/O, timer, `Task.Run`,
   `DateTime.Now`); tempo via `TimeProvider`; carga em `ActivateAsync`/`OnSessionOpenedAsync`.
6. Todo estado alcançável por amostra (`EditSessionFactory.FromText` + fakes).
7. Amostras Loading/Empty/Error/Ready em `App/Design/Samples/<Page>Samples.cs`.
8. Dados de amostra só com ids sintéticos (`demo_co.city_a`).
9. Fontes embutidas (Barlow/Barlow Condensed, direção A+C).

## Tarefas

| # | Agente | Faz | Pronto quando |
| --- | --- | --- | --- |
| UX-1 | ux-designer | `design/shell/`: shell, barra de sessão, estados sem sessão/carregando/erro (BSII, corrompido), Tab, atalhos (Ctrl+S revisar, Ctrl+Enter aplicar), strings | todas as chaves em pt-BR/en; canvas publicado; usuário aprova |
| UX-2 | ux-designer | `design/review-apply/`: revisar (por módulo, antes→depois, descartar), confirmação destrutiva, resultado com backup e "Abrir pasta", mensagem por `CoreError` | cada `CoreError` com `Error_<Code>` pt-BR/en; aprovado |
| UX-3 | ux-designer | `design/controles/`: tokens e cada controle (props, estados, hit area, a11y), layout da galeria | aprovado |
| D1 | core-dev | contratos Domain | compila; check-layers ok |
| S1 | sii-engine | índice por tipo + `Save` obsoleto + nota em `sii-format.md` | roundtrip ok; `OfType` O(1) |
| DA1 | core-dev | contratos Data (stubs) | compila |
| C1 | core-dev | contratos Core (stubs) | compila; check-layers ok |
| A1 | ui-dev | contratos App (stubs) + csproj | compila com compiled bindings; `MainViewModel` → `ShellViewModel` |
| T1 | test-engineer | `tests/OwnExpeditor.Data.Tests/` (save-safety: backup idêntico, ChangedOnDisk, verify falha, BackupExists, sem permissão, UTF-8 sem BOM; perfis em dir temporário) | falham pelos motivos certos |
| T2 | test-engineer | sessão: Key substitui, Conflict, StalePatch, View vs Original, Discard*, GetDiff, ApplyAsync (GameRunning, patches, nova base), mapeamento de erros, FromText ReadOnly | idem |
| T3 | test-engineer | `tests/OwnExpeditor.App.Tests` (Avalonia.Headless.XUnit 11.3 + Skia): screenshots por amostra, erro de binding falha, paridade `.resx`, `*TextKeys` e `Error_*` existem | idem (pode exigir xUnit 2.9.x) |
| DA2 | core-dev | `SaveFileStore` atômico | T1 verde |
| DA3 | core-dev | `ProfileLocator`, `GameProcessMonitor`, `JsonSettingsStore` | testes de perfis verdes |
| C2 | core-dev | `SaveView`, `EditSession` (Stage/Discard/GetDiff), `EditSessionFactory` | T2 parcial verde |
| C3 | core-dev | `ApplyAsync`, `EditSessionHost`, `ProfileService`, `AppSettingsService`, `AddOwnExpeditorCore` | T2 verde |
| A2 | ui-dev | `Localizer` + `.resx`, `ThemeService`, Configurações, `CultureChangedMessage` | troca em runtime; paridade verde |
| A3 | ui-dev | tokens e controles (UX-3) | cada controle com `DesignSample` |
| A4 | ui-dev | shell (UX-1): `ShellView`, `SessionBar`, `ViewRegistry`, `AppComposition`, DI | abre; sem save, módulos desabilitados; abrir save não trava |
| A5 | ui-dev | revisar/aplicar/resultado (UX-2), `DialogService`, confirmar descarte | fluxo com ChangeSet de amostra |
| A6 | ui-dev | galeria (Debug) + amostras de todas as páginas | screenshots sem erro de binding; Release sem `Gallery*` |
| A7 | ui-dev + core-dev | esqueleto do módulo Fretes (Empty "em breve") | navegação mostra Fretes |

Fechamento: test-runner → code-reviewer + ux-reviewer (com `.screenshots/` e
`design/`) → docs-keeper → git-ops (bump **minor**).

## Riscos
Corrida com o jogo (fingerprint dentro de `WriteAtomicAsync`); patch velho
(OldValue no Stage e no Apply); save em texto sobre ScsC não validado no jogo
(ADR 0002); patches futuros (`AddUnit`, `SetArray`); memória ~2× no Apply;
backup no mesmo segundo (`BackupExists`); `File.Replace` bloqueado por
antivírus (`WriteFailed`); headless pode exigir xUnit mais novo; galeria
vazando para Release; chaves do Core sem tradução (teste de paridade).
