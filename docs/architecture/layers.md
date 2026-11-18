# Camadas e dependências

O código fica em `src/` (cinco projetos de biblioteca/app) e `tests/` (dois
projetos xUnit). A regra do `CLAUDE.md` é que as dependências **só apontam para
baixo**:

```
App → Core → Data → Sii → Domain
        ↘ Sii ↗   ↘ Domain ↗
```

Em outras palavras: `App` fala com `Core` e, para records puros, com `Domain`;
`Core` usa `Data`, `Sii` e `Domain`; `Data` usa `Sii` e `Domain`; `Sii` usa `Domain`.

Na prática, pelos `ProjectReference` dos `.csproj`:

| Projeto | Referencia |
|---------|-----------|
| `OwnExpeditor.App` | `Core` (hoje). `Domain` é permitido pela [ADR 0005](../decisions/0005-shell-modular-e-sessao-de-edicao.md), ainda não referenciado |
| `OwnExpeditor.Core` | `Domain`, `Sii`, `Data` |
| `OwnExpeditor.Data` | `Domain`, `Sii` |
| `OwnExpeditor.Sii` | `Domain` |
| `OwnExpeditor.Domain` | nada |

Nenhum projeto referencia uma camada acima dela, e o `Domain` não tem
dependências. Isso bate com a regra.

### App: Domain sim, Sii e Data não

A [ADR 0005](../decisions/0005-shell-modular-e-sessao-de-edicao.md) (item 8)
define o que a App pode usar:

- **Pode** referenciar `Domain`, porque são só records puros (`ProfileInfo`,
  `AppSettings` etc.), sem I/O.
- **Não pode** referenciar `Sii` nem `Data`. Ela só acessa o save por `Core`.
- O `csproj` da App vai usar `DisableTransitiveProjectReferences`. Assim, a
  referência ao `Core` não traz `Data` e `Sii` por transitividade.
- `toolkit/check-layers.sh` barra `using OwnExpeditor.Sii` e
  `using OwnExpeditor.Data` na App, e também `ProjectReference` fora da lista
  permitida (App: `Core` e `Domain`).

Estado: o `check-layers.sh` já aplica essas regras. O `csproj` da App ainda
não tem `DisableTransitiveProjectReferences` nem referência a `Domain`; isso
vem com a tarefa A1 do [plano 0005](plan-0005-shell.md) e com a fase F1 do
[plano 0006](plan-0006-integracao.md).
> TODO: confirmar quando A1 (plano 0005) ou 1.4 (plano 0006) forem implementadas.

Hoje a App ainda usa tipos da Data por meio do `Core`: `MainViewModel` chama
`GameCatalog.LoadFromGameDir` e `SaveLocator.Profiles` diretamente, e
`Localization.GameLanguage`. Isso fere a regra "App não conhece a Data" e
será corrigido pela fachada `Core/Legacy/LegacyJobs` (plano 0006, fase F1).
> TODO: confirmar quando a fase F1 for mergeada.

## Responsabilidade e estado atual de cada projeto

| Projeto | Responsabilidade (alvo) | Estado no código hoje |
|---------|-------------------------|-----------------------|
| `src/OwnExpeditor.App` | UI Avalonia em MVVM | `App.axaml`, `Views/MainWindow.axaml` e `ViewModels/MainViewModel.cs`: tela única para escolher perfil e save, carregar as opções, adicionar fretes e gravar. `Converters.cs` e `ViewModelBase.cs` também existem. Há uma pasta `Models\` declarada no `.csproj` que não existe. |
| `src/OwnExpeditor.Core` | Casos de uso: ler save, listar opções, injetar job | `SaveSession.cs` (origens, cargas, destinos, distâncias, `Inject`, `CompanyLocation`, `JobTemplate`) e `JobInjector.cs` (`JobInjector.Inject` sobre `SiiDocument`, `JobSpec`). Ainda sem `CoreError`. |
| `src/OwnExpeditor.Data` | Perfis e saves no disco, catálogo do jogo, cache de distâncias | `SaveLocator.cs` (perfis, slots, `WriteAsNewSave`), `GameCatalog.cs` (empresas, cidades, cargas, reboques, lido dos `.scs`), `Localization.cs` (nomes do `locale.scs`), `Scs/HashFsArchive.cs` e `Scs/CityHash.cs` (leitor de pacotes HashFS v2). Cache de distâncias: TODO: confirmar, não há código. |
| `src/OwnExpeditor.Sii` | Decodificar ScsC e BSII, parsear/gravar o texto SiiNunit | `SiiDecoder.cs` (detecção de formato, ScsC, texto), `BsiiDecoder.cs` (binário BSII para texto) e `SiiDocument.cs` (parse, `ToText`, `Save` com backup). `3nK` ainda não é lido. |
| `src/OwnExpeditor.Domain` | Entidades puras | Só o `.csproj`, sem código. Ver [domain.md](domain.md). A ADR 0006 planeja `ProfileInfo`, `AppSettings` e outros records aqui. |

Testes:

| Projeto de teste | Cobre | Estado |
|------------------|-------|--------|
| `tests/OwnExpeditor.Sii.Tests` | `Sii` | `SiiDocumentTests.cs` (strings inline) e `RealSaveTests.cs` (saves reais de `samples/`, fora do git) |
| `tests/OwnExpeditor.Core.Tests` | `Core` (e `Data` pelo caminho de `Core`) | `SaveSessionTests.cs`, `JobInjectorTests.cs`, `EndToEndTests.cs` e `ScsTests.cs` |

> TODO: confirmar se `Data` e `Domain` ganharão projeto de teste próprio
> (plano 0006 cria `tests/OwnExpeditor.Data.Tests`) e se o `Models\` do App será
> usado ou removido.

## Tema e idioma por camada

Regra da [ADR 0004](../decisions/0004-suporte-a-tema-e-idioma.md) e da skill
`i18n-theming`. Os itens marcados como TODO ainda não existem no código.

- **App** (`src/OwnExpeditor.App`): todo texto visível vem de `.resx`
  (`Resources/Strings.resx` em en e `Strings.pt-BR.resx`). Hoje não há `.resx`
  e os textos estão em literais. Cores próprias ficam em `ThemeDictionaries`
  (Light e Dark). Hoje só o tema do sistema está ligado
  (`RequestedThemeVariant="Default"`). TODO: confirmar a pasta `Resources/`
  (planejada na fase F5 do plano 0006).
- **Core** (`src/OwnExpeditor.Core`): devolve códigos de erro, não mensagens. A
  App traduz o código. O Core não referencia recursos de UI. Hoje ele ainda
  lança exceções com texto (`JobInjectionException`); `CoreError` e
  `CoreException` vêm com a ADR 0006 (item 3).
- **Data** (`src/OwnExpeditor.Data`): lê e grava `settings.json` em
  `ApplicationData/OwnExpeditor/`, com tema e idioma. Não há código de
  configurações em `Data` ainda. A interface (`ISettingsStore`) fica na própria
  Data e é implementada lá. O Core consome essa interface e expõe
  `IAppSettingsService` à App. Isso corrige a ADR 0004 (ver a nota de
  atualização nela e a [ADR 0005](../decisions/0005-shell-modular-e-sessao-de-edicao.md), item 2).
  TODO: confirmar quando `Data` ganhar código de configurações.
- **Nomes do jogo**: cidades, empresas, cargas e reboques são traduzidos pela
  localização do próprio jogo (`Localization` na Data, `locale.scs`). A ADR
  0006 (item 2) passa essa resolução para o Core (`IGameNames`) e faz o idioma
  seguir o do app. Hoje o idioma vem do `config.cfg` do jogo
  (`Localization.GameLanguage`).
- **Domain**: não tem texto nem cor. Nenhum nome de cidade, empresa ou carga
  é traduzido aqui.

## Regras que a estrutura precisa manter

- Nenhuma camada inferior conhece a superior. Por exemplo, `Sii` não pode
  referenciar `Core` nem `App`.
- A UI (`App`) não lê nem grava `game.sii` diretamente; deve passar pelo `Core`.
  Hoje o `App` não referencia `Sii`, mas usa `Data` por meio de `MainViewModel`,
  como descrito acima.
- Não há listas fixas de cidades, cargas ou reboques em nenhum projeto (ver
  [ADR 0003](../decisions/0003-sem-listas-fixas.md)).

Ver também: [overview.md](overview.md) para o diagrama e
[data-flow.md](data-flow.md) para a sequência de leitura e escrita.
