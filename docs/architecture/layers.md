# Camadas e dependências

O código fica em `src/` (cinco projetos de biblioteca e app) e `tests/` (quatro
projetos xUnit). A regra do `CLAUDE.md` é que as dependências **só apontam para
baixo**:

```
App → Core → Data → Sii → Domain
  ↘ Domain        ↘ Domain
```

Em outras palavras: `App` fala com `Core` e, para records puros, com `Domain`;
`Core` usa `Data`, `Sii` e `Domain`; `Data` usa `Sii` e `Domain`; `Sii` usa
`Domain`. O `Domain` não tem dependências.

## Referências reais

Pelos `ProjectReference` dos `.csproj`:

| Projeto | Referencia |
|---------|-----------|
| `OwnExpeditor.App` | `Core`, `Domain`. Com `DisableTransitiveProjectReferences`, `Data` e `Sii` não entram por transitividade |
| `OwnExpeditor.Core` | `Domain`, `Sii`, `Data` |
| `OwnExpeditor.Data` | `Domain`, `Sii` |
| `OwnExpeditor.Sii` | `Domain` |
| `OwnExpeditor.Domain` | nada |

`toolkit/check/check-layers.sh` confere isso em três níveis: `ProjectReference`, `using`
e `global using`, e uso qualificado (`OwnExpeditor.Sii.Foo`) em `.cs` e `.axaml`.
Ele roda em `toolkit/check/check.sh`. A App só pode usar `Core` e `Domain`.

Fora da regra: `tools/OwnExpeditor.Cli` (ferramenta de desenvolvimento com
`decode`, `scs-ls`, `scs-cat` e `catalog`) e `toolkit/` (scripts e
`SiiTool`). Só `src/` é verificado.

### App: Domain sim, Sii e Data não

A [ADR 0005](../decisions/0005-shell-modular-e-sessao-de-edicao.md) (item 8)
define o que a App pode usar:

- **Pode** referenciar `Domain`, para os records puros (`ProfileInfo`,
  `SaveSlotInfo`, `AppSettings`, `GameKind`, etc.).
- **Não pode** referenciar `Sii` nem `Data`. Ela acessa o save só por `Core`, e
  os tipos da Data que aparecem na tela (como `GameCatalog`) chegam por meio de
  interfaces do `Core`.

## Responsabilidade de cada projeto

| Projeto | Responsabilidade | Principais tipos |
|---------|------------------|------------------|
| `src/OwnExpeditor.App` | UI Avalonia em MVVM: shell, módulos, revisão, confirmação, resultado, configurações, temas e idiomas | `App`, `Program`, `Shell/AppComposition`, `ViewRegistry`, `ShellViewModel`, `SessionBarViewModel`, `WelcomeViewModel`, `LoadingViewModel`, `ReviewViewModel`, `ConfirmApplyViewModel`, `ResultViewModel`, `SettingsViewModel`, `Modules/Jobs/JobsPageViewModel`, `Services/Localizer`, `Services/ThemeService`, `Controls/*` |
| `src/OwnExpeditor.Core` | Casos de uso: sessão de edição, mudanças, Jobs, jogo, perfis e ajustes | `EditSession`, `EditSessionHost`, `ChangeSet`, `SavePatch`, `ApplyTarget`, `ISaveView`, `CoreError`, `CoreException`, `Modules/Jobs/JobBoard`, `JobPlanner`, `DistanceTable`, `GameDataService`, `GameStatusMonitor`, `ProfileService`, `AppSettingsService`, `IGameNames` |
| `src/OwnExpeditor.Data` | Perfis e saves no disco, gravação segura, catálogo do jogo, cache de distâncias, ajustes em JSON | `ProfileLocator`, `SaveFileStore` (`ISaveFileStore`), `SaveLocator` (utilitários internos), `GameCatalog`, `GameInstallLocator`, `GameProcessMonitor`, `Localization`, `Scs/HashFsArchive`, `Distances/JsonDistanceCache`, `Settings/JsonSettingsStore` |
| `src/OwnExpeditor.Sii` | Decodificar ScsC, BSII e 3nK; parsear e gravar o texto SiiNunit | `SiiDecoder`, `BsiiDecoder`, `SiiDocument`, `SiiFormatException`, `SiiErrorKind` |
| `src/OwnExpeditor.Domain` | Records e enums puros, sem I/O | `GameKind`, `GameIds`, `GameNameKind`, `GameFolderCheck`, `CityPair`, `ProfileInfo`, `SaveSlotInfo`, `ProfileNames`, `AppSettings`, `GameFolders`, `Language`, `ThemePreference` |

A lista de entidades do jogo (empresas, cidades, cargas, reboques) não tem
projeto próprio: `GameCatalog` (`Data`) guarda as definições lidas dos `.scs`, e
`JobBoard` (`Core`) as expõe como opções. Ver [domain.md](domain.md).

### Testes

| Projeto | Cobre | Observação |
|---------|-------|-----------|
| `tests/OwnExpeditor.Sii.Tests` | `Sii` | `SiiDocumentTests`, `SiiErrorTests` (um caso por `SiiErrorKind`), `RealSaveTests` (saves de `samples/`) |
| `tests/OwnExpeditor.Data.Tests` | `Data` | Gravação segura (`SaveFileStoreTests`), perfis, instalação do jogo, monitor de processo, ajustes JSON, cache de catálogo |
| `tests/OwnExpeditor.Core.Tests` | `Core` (e `Domain`) | Sessão e aplicação (`EditSession*`), `JobBoardTests`, `JobPlannerTests`, `DistanceTests`, `JobsEndToEndTests`, `ProfileServiceTests`, `ScsTests` |
| `tests/OwnExpeditor.App.Tests` | `App` | Paridade `.resx`, textos `_One`/`_Other`, `Error_*`, atalhos e navegação do shell, tema, página de Fretes, `JobsRealSaveTests` |

Total: 764 testes passando (`toolkit/check/check.sh`). Os testes com saves reais
(`RealSaveTests`, `JobsRealSaveTests`) só rodam se `samples/` existir; ver
[development.md](../development.md).

## Tema e idioma por camada

Regra da [ADR 0004](../decisions/0004-suporte-a-tema-e-idioma.md) e da skill
`i18n-theming`.

- **App**: todo texto visível vem de `Resources/Strings.resx` (en) e
  `Strings.pt-BR.resx`, lidos pelo `Localizer`. As tabelas de origem ficam em
  `design/*/strings.md`, e `toolkit/gen/gen-resx.py` gera os `.resx` (com `--check`
  para conferir). As cores vêm de tokens em `Styles/` (Light e Dark).
- **Core**: devolve `CoreError` e chaves de texto, não frases. Por exemplo,
  `InvalidEdit` vem com `Reason = Jobs_Error_*`, e a App traduz.
- **Data**: lê e grava `settings.json` (`JsonSettingsStore`) com tema, idioma,
  última pasta e pastas do jogo. `ISettingsStore` fica na Data; `IAppSettingsService`
  (`Core`) a expõe à App.
- **Nomes do jogo**: cidades, empresas, cargas e reboques são traduzidos pelo
  `locale.scs` do próprio jogo, no idioma escolhido no app. `IGameNames` (`Core`)
  faz essa resolução.
- **Domain**: não tem texto nem cor.

## Regras que a estrutura precisa manter

- Nenhuma camada inferior conhece a superior. Por exemplo, `Sii` não pode
  referenciar `Core` nem `App`. `check-layers.sh` confere.
- A UI (`App`) não lê nem grava `game.sii` diretamente; passa pelo `Core`
  (`IEditSessionHost`, `IEditSession`, `IProfileService`, `IGameDataService`).
- A gravação passa só por `ISaveFileStore` (`Data`), chamado por
  `EditSession.ApplyAsync`. Não há outro caminho de escrita de save.
- Não há listas fixas de cidades, cargas ou reboques em nenhum projeto (ver
  [ADR 0003](../decisions/0003-sem-listas-fixas.md)).

Ver também: [overview.md](overview.md) para o diagrama e
[data-flow.md](data-flow.md) para a sequência de leitura e escrita.
