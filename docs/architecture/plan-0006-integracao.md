# Plano de implementação — ADR 0006 (integração ao shell modular)

Fonte: `chief-architect`, 2026-11-17, delta sobre
[`plan-0005-shell.md`](plan-0005-shell.md). Decisão em
[`../decisions/0006-integracao-do-codigo-de-injecao-ao-shell.md`](../decisions/0006-integracao-do-codigo-de-injecao-ao-shell.md).
Vale o plano 0005 no que este não altera.

**Estado:** fases F0 a F7 concluídas. O app abre sempre o shell modular; a tela
única (`MainViewModel`), `Core/Legacy` (`LegacyJobs`), `SaveSession`, `JobInjector`,
`WriteAsNewSave`, `SiiDocument.Save` e o comando `inject` da CLI foram removidos.
Estado final: 764 testes verdes (`toolkit/check/check.sh`). Pendente: teste no jogo
(item 7.5). As tabelas abaixo ficam como registro do plano; a coluna de situação
diz o que aconteceu.

## Reaproveitamento

| Hoje | Destino | O que muda |
| --- | --- | --- |
| `Sii/BsiiDecoder`, `SiiDecoder` | ficam no Sii | erros tipados (`SiiFormatException` + `SiiErrorKind`) |
| `SiiDocument` | fica | `OfType` indexado (`IReadOnlyList`); `Parse` com progresso; `Save` `[Obsolete]` |
| `Data/Scs/*` | ficam (internos ao catálogo) | — |
| `Data/GameCatalog`, `Localization` | ficam | separar carga de definições (def + dlc) e de textos (locale) para trocar idioma sem reler; `Humanize` → `Domain.Games.GameIds` |
| `Data/SaveLocator` | `IProfileLocator` (interno) | tipos do Domain; `GameKind`, `IsSteamCloud`, nome do hex, raízes Linux/Proton; slot novo passa para `ISaveFileStore` |
| `Core/SaveSession` | `Core/Modules/Jobs/JobBoard` (só leitura, `ISaveView` + `IGameData`) | sem `SiiDocument`; App ordena pelo nome traduzido |
| `CompanyLocation`, `JobTemplate` | `Core/Modules/Jobs` | sem `Display` (nome via `IGameNames`) |
| `Core/JobInjector` | `JobPlanner.Plan(...)` → `ChangeSet` (`Key = jobs:slot:<id>`) | violação → `CoreException(InvalidEdit, Reason=Jobs_Error_*)`; feito: `JobInjector` removido na F7, testes portados para `JobPlanner` |
| `App/MainViewModel` | F1: só Core/Domain via `Core/Legacy/LegacyJobs`; F6 shell em `--shell`; F7 `JobsPageViewModel`, apaga legado | Feito: F7 apagou `MainViewModel`, `MainWindow` e o legado; o shell é o padrão |
| `tools/OwnExpeditor.Cli` | fica (fora da regra de camadas) | feito: comando `inject` removido; restam `decode`, `scs-ls`, `scs-cat` e `catalog` |
| 66 testes | F1 mantém; F7 porta para `JobBoardTests`, `JobPlannerTests`, `EndToEnd` via `ApplyAsync` | feito: 764 testes ao fim da F7 |

## Decisões (ver ADR 0006)
- **Erros:** `SaveCorrupt` ← `Empty, Truncated, DecryptFailed, DecompressFailed, SizeMismatch`;
  `SaveParseFailed` ← `UnknownFormat, UnsupportedVersion, UnknownValueType, Syntax`;
  `FileNotFound/DirectoryNotFound` → `SaveNotFound`; acesso negado/travado → `AccessDenied`.
- **Pasta do jogo:** registro Steam (Windows) → `libraryfolders.vdf` + `appmanifest_227300/270880.acf`
  (Windows, `~/.steam/steam`, `~/.local/share/Steam`, Flatpak) → padrões. `Validate`: `base.scs`,
  `def.scs`, executável certo em `bin/*`. Perfis também em
  `compatdata/<appid>/pfx/drive_c/users/steamuser/Documents/<Jogo>` e `~/.local/share/<Jogo>`.
- **Destino da gravação** (`ApplyTarget`): `Overwrite` (padrão) ou `NewSlot(displayName)`;
  `NewSlot` copia a pasta do slot para o próximo slot livre, grava `game.sii` (`.tmp` +
  verificação) e ajusta o nome em `info.sii`. Steam Cloud → opção com aviso.
- **Etapas:** Read 0–30% (bytes), Parse 30–85%, Index 85–100% (+ `IGameData` com cache);
  `Percent < 0` indeterminado; ≤ 1 relatório/100 ms; `FileSizeBytes`.
- **Monitor:** 2 s só com sessão; `eurotrucks2[.exe]`/`amtrucks[.exe]`; `TimeProvider`.
- **Plural:** `_Zero` opcional → `_One` (n=1) → `_Other`; `TextRef.Plural`. **Moeda:** fixa por jogo.
- **Atalhos:** Ctrl+R `ReviewCommand`; Ctrl+Enter `RequestApplyCommand` só na Revisar; Ctrl+O,
  Ctrl+,, Ctrl+1…5, Esc conforme `design/shell`; Ctrl+S livre.
- **Desfazer** = re-`Stage` (StalePatch/Conflict → Toast de falha). Backup no diálogo = padrão de nome.

## Contratos (delta)

```csharp
// Domain
namespace OwnExpeditor.Domain.Games;
public enum GameNameKind { City, Company, Cargo, Trailer, Truck }
public enum GameFolderCheck { Ok, NotFound, NotAGameFolder, WrongGame }
public static class GameIds { public static string Humanize(string id); }
namespace OwnExpeditor.Domain.Saves;
public sealed record ProfileInfo(GameKind Game, string Id, string DisplayName, string DirectoryPath, bool IsSteamCloud);
public sealed record SaveSlotInfo(ProfileInfo Profile, string Id, string DisplayName, string GameSiiPath,
                                  DateTime LastWriteUtc, long SizeBytes);
namespace OwnExpeditor.Domain.Settings;
public sealed record GameFolders(string? Ets2 = null, string? Ats = null)
{ public string? For(GameKind g); public GameFolders With(GameKind g, string? path); }
public sealed record AppSettings(ThemePreference Theme, Language Language, string? LastSavePath, GameFolders GameFolders);

// Sii
public enum SiiErrorKind { Empty, UnknownFormat, Truncated, DecryptFailed, DecompressFailed, SizeMismatch,
                           UnsupportedVersion, UnknownValueType, Syntax }
public sealed class SiiFormatException(SiiErrorKind kind, string message, int? line = null, long? offset = null,
    Exception? inner = null) : FormatException(message, inner)
{ public SiiErrorKind Kind { get; } public int? Line { get; } public long? Offset { get; } public bool IsUnreadable { get; } }
// SiiDocument.Parse(string, IProgress<double>? = null); OfType → IReadOnlyList (índice); Save [Obsolete]

// Data
namespace OwnExpeditor.Data.Games;
public interface IGameInstallLocator
{ IReadOnlyList<string> Detect(GameKind game); GameFolderCheck Validate(GameKind game, string path); }
public interface IGameCatalogSource   // cache por (pasta, mtime dos .scs)
{
    GameCatalog LoadDefinitions(string installDir);
    Localization LoadTexts(string installDir, IReadOnlyList<string> languages);
    string? GameLanguage(ProfileInfo profile);   // config.cfg g_lang_init
}
namespace OwnExpeditor.Data.Saves;    // ISaveFileStore do plano 0005 +
public sealed record NewSlotWriteRequest(SaveSlotInfo Source, SaveFingerprint Expected, string NewText,
                                         string DisplayName, Func<string, bool> VerifyWritten);
public sealed record NewSlotReceipt(SaveSlotInfo NewSlot);
// ISaveFileStore.WriteNewSlotAsync(NewSlotWriteRequest, CancellationToken): próximo slot livre,
// copia a pasta, grava game.sii (.tmp + verify), ajusta info.sii; falha → apaga a pasta nova.

// Core
public enum CoreError { SaveNotFound, SaveParseFailed, SaveCorrupt, NoSession, SessionBusy, ReadOnlySession,
    Conflict, StalePatch, UnknownUnit, UnknownAttribute, InvalidEdit, NothingToApply, GameRunning,
    SaveChangedOnDisk, BackupExists, BackupFailed, WriteFailed, VerifyFailed, AccessDenied, SlotUnavailable }
// CoreException + TextRef? Reason, string? StageKey, int? Line
// TextRef(string Key, IReadOnlyList<object?> Args, long? Count = null) + TextRef.Plural(baseKey, count, args)
// DiffValue.GameName(GameNameKind Kind, string Id)
namespace OwnExpeditor.Core.Session;
public static class LoadStages { public const string Read = "Loading_Stage_Read", Parse = "Loading_Stage_Parse", Index = "Loading_Stage_Index"; }
public sealed record LoadProgress(int Percent, string StageKey, long? FileSizeBytes = null);
public abstract record ApplyTarget
{ public sealed record Overwrite : ApplyTarget; public sealed record NewSlot(string DisplayName) : ApplyTarget; }
public sealed record ApplyResult(SaveSlotInfo WrittenSlot, string? BackupPath, int ChangeSetCount, int PatchCount);
// IEditSession: + IGameData GameData; IGameNames Names => GameData;
//               Task<ApplyResult> ApplyAsync(ApplyTarget target, CancellationToken ct);
//               NewSlot: a sessão passa a apontar para o slot novo (nova base) após gravar.
namespace OwnExpeditor.Core.Games;
public interface IGameNames { string Get(GameNameKind kind, string id); }   // nunca null
public interface IGameData : IGameNames
{
    GameKind Game { get; } bool IsAvailable { get; }
    bool Ships(string company, string cargo); bool Accepts(string company, string cargo);
    IReadOnlyCollection<string> CargoOut(string company);
}
public enum GameFolderSource { Detected, Manual, NotFound }
public sealed record GameFolderStatus(GameKind Game, string? Path, GameFolderSource Source);
public interface IGameDataService
{
    Task<IGameData> GetAsync(GameKind game, CancellationToken ct);   // nunca lança por falta de pasta
    GameFolderStatus GetFolder(GameKind game);
    Task<GameFolderCheck> SetManualFolderAsync(GameKind game, string? path, CancellationToken ct);
    event EventHandler<GameKind>? Changed;
}
public interface IGameStatusMonitor { bool IsRunning { get; } event EventHandler<bool>? RunningChanged; }
namespace OwnExpeditor.Core.Modules.Jobs;
public sealed record CompanyLocation(string Company, string City) { public string Key { get; } public string SaveId { get; } }
public sealed record JobTemplate(string TrailerVariant, string TrailerDefinition, string CompanyTruck, int UnitsCount);
public enum JobSlotMode { FillEmpty, ReplaceOffer }          // escolhido pelo usuário
public sealed record JobRequest(CompanyLocation Origin, CompanyLocation Target, string Cargo,
                                JobSlotMode Mode, string? ReplaceOfferId = null,
                                int Urgency = 0, int ExpirationHours = 72);   // prazos: 24, 48, 72, 168
public enum DistanceSource { Known, Estimated }
public sealed record RouteDistance(int Km, DistanceSource Source);
public sealed class JobBoard
{
    public static JobBoard Build(ISaveView view, IGameData data);   // catálogo ∪ regras do save (mods)
    // Fluxo pedido pelo usuário: cidade de saída → cidade de destino → empresa de saída → carga → empresa de chegada.
    public IReadOnlyList<string> OriginCities();                                   // cidades com empresa com slot livre
    public IReadOnlyList<string> DestinationCities(string originCity);             // alcançáveis por alguma combinação
    public IReadOnlyList<CompanyLocation> OriginCompanies(string originCity, string destinationCity);
    public IReadOnlyList<string> Cargos(CompanyLocation origin, string destinationCity);
    public IReadOnlyList<CompanyLocation> DestinationCompanies(string destinationCity, string cargo);
    public RouteDistance Distance(string fromCity, string toCity);   // nunca pede ao usuário
    public IReadOnlyList<string> ReplaceableOffers(CompanyLocation origin); // ofertas existentes da empresa
    public int FreeSlots(CompanyLocation origin);                    // já desconta pendentes (View inclui pendentes)
    public JobTemplate? Template(string cargo);
}
public static class JobPlanner { public static ChangeSet Plan(ISaveView view, JobBoard board, JobRequest request); }
// JobInjector.Inject(SiiDocument, ...) = adaptador; JobsTextKeys; AddJobsModule
namespace OwnExpeditor.Core.Legacy;   // F1→F7, apagado em F7
// [Obsolete] LegacyJobs: LoadGameData, Profiles, Saves, Open (SaveSession), WriteAsNewSave

// App
// ILocalizer + GetPlural(baseKey, count, args), Format(DiffValue, IGameNames?), FormatMoney(amount, GameKind)
public interface IShellLauncher { Task<bool> OpenFolderAsync(string folderPath); Task<bool> RevealFileAsync(string filePath); }
public interface IFolderPicker { Task<string?> PickFolderAsync(TextRef title, string? startPath); }
public interface IClipboardService { Task SetTextAsync(string text); }
// ShellViewModel: ReviewCommand, RequestApplyCommand, OpenSaveCommand, OpenSettingsCommand, NavigateCommand(int), BackCommand
```

## Fases

Regras: contratos/stubs primeiro, testes antes da implementação; a cada fase
`toolkit/check/check.sh` verde, `toolkit/check/check-layers.sh` ok e o app faz o fluxo atual.

### F0 — sem código — concluída
| # | Agente | Faz | Pronto quando |
| --- | --- | --- | --- |
| 0.1 | ux-designer | `design/fretes/` (distância desconhecida, catálogo indisponível, aviso Steam Cloud); em `design/review-apply`: escolha **Sobrescrever / Criar novo save** na confirmação e no resultado, `Error_SaveParseFailed`, `Error_InvalidEdit`, `Error_SlotUnavailable`, `Jobs_Error_*`, `_Zero` | usuário aprova |
| 0.2 | docs-keeper | ADR 0002 (BSII suportado), skills `i18n-theming` (nomes do jogo) e `design-system`, `docs/architecture/*` | `check-docs` ok |

### F1 — contratos e camadas (app idêntico, 66 testes verdes) — concluída
| # | Agente | Faz | Pronto quando |
| --- | --- | --- | --- |
| 1.1 | core-dev | Domain (Games, Saves, Settings) | compila; `Localization.Humanize` delega |
| 1.2 | core-dev | `SaveLocator` com tipos do Domain | 66 verdes |
| 1.3 | core-dev | Core: erros, Text, Session, Games, Profiles, Settings (stubs) + `LegacyJobs` real + `SaveSession(…, IGameData)` | compila; 66 verdes |
| 1.4 | ui-dev | `MainViewModel`/`MainWindow` só via `LegacyJobs`; App.csproj `DisableTransitiveProjectReferences` + ref Domain | check-layers ok; fluxo igual |
| 1.5 | core-dev | `check-layers.sh` pega uso qualificado e `global using`; `tools/` fora | acusa violação plantada |
| 1.6 | test-engineer | `DomainTests` (Humanize, nome hex, GameFolders) | verde |

### F2 — Sii — concluída
| 2.1 | sii-engine | `docs/sii-format.md` §7: classificação de erros e índice | doc antes do código |
| 2.2 | test-engineer | `SiiErrorTests`: um caso por `SiiErrorKind` | vermelhos certos |
| 2.3 | sii-engine | `SiiDecoder`, `BsiiDecoder`, `SiiDocument` (+ ajustes em SaveLocator, CLI, LegacyJobs) | 2.2 verde; round-trip ok |

### F3 — Data (`tests/OwnExpeditor.Data.Tests` novo) — concluída
| 3.1 | test-engineer | save-safety (sobrescrever e slot novo), perfis (profiles, steam_profiles, Proton), `libraryfolders.vdf`, `Validate`, settings com `GameFolders` e JSON corrompido | vermelhos certos |
| 3.2 | core-dev | `SaveFileStore` (`WriteAtomicAsync` + `WriteNewSlotAsync`) | save-safety verde |
| 3.3 | core-dev | `ProfileLocator`, `GameProcessMonitor` | perfis verdes |
| 3.4 | core-dev | `GameInstallLocator` | detecção/validação verdes |
| 3.5 | core-dev | `JsonSettingsStore`, `GameCatalogSource` (cache), `AddOwnExpeditorData` | verde; app igual |

### F4 — Core — concluída
| 4.1 | test-engineer | sessão (plano 0005 T2), `ApplyTarget` nos dois modos, mapeamento de erros, etapas, monitor com `FakeTimeProvider`, pasta manual inválida não grava, troca de idioma só relê textos | vermelhos certos |
| 4.2 | core-dev | `SaveView`, `EditSession` (Stage/Discard/GetDiff), `EditSessionFactory` | parte verde |
| 4.3 | core-dev | `ApplyAsync(ApplyTarget)` (verify: contagem de blocos + valores novos), `EditSessionHost.OpenAsync` (etapas, cancelamento, StageKey/Line) | verde |
| 4.4 | core-dev | `GameDataService`, `GameStatusMonitor`, `ProfileService`, `AppSettingsService`, `AddOwnExpeditorCore` | 4.1 verde |

### F5 — App base (paralelo a F3/F4) — concluída
| 5.1 | test-engineer | `tests/OwnExpeditor.App.Tests`: paridade `.resx`, `_One`/`_Other`, `Error_*` dos 20 códigos, `Format_Money_*` | vermelhos certos |
| 5.2 | ui-dev | `Localizer`, `Resources/*.resx` (de `design/*/strings.md`), `ThemeService` | paridade verde |
| 5.3 | ui-dev | `Styles/`, `Controls/`, `Gallery/` (Debug) | amostras Light/Dark × pt-BR/en |

### F6 — Shell atrás de `--shell` — concluída (o `--shell` foi removido na F7)
| 6.1 | ui-dev | `AppComposition`, `ViewRegistry`, `Shell/*`, `SessionBar`, atalhos | abre; módulos desabilitados sem sessão |
| 6.2 | ui-dev | `ProfileSavePicker`, Carregando, erros (ilegível/não entendido), `IClipboardService`, `IShellLauncher` | save BSII de `samples/` abre sem travar |
| 6.3 | ui-dev | Configurações + `FolderField`, `IFolderPicker` | escolher/detectar; inválida recusada |
| 6.4 | ui-dev | Revisar, Confirmar (com destino), Resultado, `DialogService`, Desfazer | fluxo completo com amostra; ux-reviewer ok |

### F7 — módulo Fretes (última) — concluída, falta o teste no jogo (7.5)
| 7.1 | test-engineer | `JobBoardTests`, `JobPlannerTests`, EndToEnd via `ApplyAsync` (ambos destinos) | vermelhos certos |
| 7.2 | core-dev | `Core/Modules/Jobs/*` | verde; `JobInjectorTests` ainda verdes |
| 7.3 | ui-dev | `App/Modules/Jobs/*` a partir de `design/fretes` | screenshots ok |
| 7.4 | ui-dev + core-dev | shell padrão; apaga MainViewModel, MainWindow, LegacyJobs, SaveSession, WriteAsNewSave, Converters | check-layers ok; sem `Obsolete` fora da CLI |
| 7.5 | fechamento | test-runner → code-reviewer + ux-reviewer → docs-keeper → git-ops (minor) | **teste no jogo:** sobrescrever save BSII, criar slot novo, perfil com Steam Cloud. **Pendente** |

## Distância (decisão 2026-11-17)
O usuário nunca digita distância nem monta rota; o jogo recalcula a rota no GPS.
- **Conhecida**: tabela cidade→cidade extraída de `shortest_distance_km` de todas as
  ofertas de todos os saves do usuário, em cache na Data (`IDistanceCache`), crescendo a
  cada save aberto. Medido num save real: 386 cidades, 4.526 pares (≈3% de todos os pares).
- **Estimada**: quando não houver valor conhecido, estimativa a partir de coordenadas das
  cidades (descoberta pendente em `.scs`) ou, sem elas, de pares vizinhos conhecidos; marcada
  como "estimada" na UI.
- **Descoberta obrigatória** (`wf-sii-discovery` + teste no jogo): o jogo recalcula
  `shortest_distance_km`/pagamento ao carregar ou ao aceitar? Se recalcular, a estimativa só
  afeta a exibição antes de aceitar.
- Editar mudança pendente = `Stage` com a mesma `Key` (substitui); sem operação nova.

## Riscos
1. Sobrescrever com Steam Cloud: a nuvem pode restaurar ou pedir conflito — validar no jogo e avisar.
2. Slot novo com Steam Cloud pode não aparecer no jogo — aviso na opção.
3. Dois caminhos de gravação até F7 (legado em slot novo sem `.tmp`): não estender o legado. Resolvido na F7: só `SaveFileStore` grava.
4. BSII gravado como texto: só o slot 3 validado; `ApplyAsync` reparseia (custo 2×).
5. Mods: `mod/` e HashFS v1 não lidos; regras tiradas do save são obrigatórias; catálogo nunca derruba a sessão.
6. `locale.scs` real não testado (idiomas misturados).
7. Frete descartado pelo jogo sem aviso (caso `plumb_suppl`).
8. Unidade de `expiration_time` não confirmada no jogo. O cache de distâncias existe desde a F7 (`Data/Distances`).
9. Detecção de processo em Proton/Wine e fora da Steam.
10. Registro do Windows e trimming.
11. Classificação de erro errada (baixo impacto).
12. Pasta do jogo trocada com sessão aberta: pendências seguem; `JobBoard` recalcula.
