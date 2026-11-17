# Camadas e dependências

O código fica em `src/` (cinco projetos de biblioteca/app) e `tests/` (dois
projetos xUnit). A regra do `CLAUDE.md` é que as dependências **só apontam para
baixo**:

```
App → Core → Data → Sii → Domain
        ↘ Sii ↗   ↘ Domain ↗
```

Em outras palavras: `App` fala só com `Core`; `Core` usa `Data` e `Sii`;
`Data` e `Sii` usam `Domain`.

Na prática, pelos `ProjectReference` dos `.csproj`:

| Projeto | Referencia |
|---------|-----------|
| `OwnExpeditor.App` | `Core` |
| `OwnExpeditor.Core` | `Domain`, `Sii`, `Data` |
| `OwnExpeditor.Data` | `Domain`, `Sii` |
| `OwnExpeditor.Sii` | `Domain` |
| `OwnExpeditor.Domain` | nada |

Nenhum projeto referencia uma camada acima dela, e o `Domain` não tem
dependências. Isso bate com a regra.

## Responsabilidade e estado atual de cada projeto

| Projeto | Responsabilidade (alvo) | Estado no código hoje |
|---------|-------------------------|-----------------------|
| `src/OwnExpeditor.App` | UI Avalonia em MVVM | Esqueleto do template: `App.axaml`, `Views/MainWindow.axaml` com um `TextBox` ligado a `MainViewModel.Greeting`. Nenhuma tela de jobs ainda. Há uma pasta `Models\` declarada no `.csproj` que não existe. |
| `src/OwnExpeditor.Core` | Casos de uso: ler save, listar opções, injetar job, backup | Só o `.csproj`. Nenhum `.cs` de produção. |
| `src/OwnExpeditor.Data` | Perfis e saves no disco, cache de distâncias | Só o `.csproj`. |
| `src/OwnExpeditor.Sii` | Decodificar ScsC e parsear/gravar o texto SiiNunit | Implementado: `SiiDecoder.cs` (detecção de formato, descriptografia ScsC) e `SiiDocument.cs` (parse, `ToText`, `Save` com backup). |
| `src/OwnExpeditor.Domain` | Entidades puras | Só o `.csproj`. Ver [domain.md](domain.md). |

Testes:

| Projeto de teste | Cobre | Estado |
|------------------|-------|--------|
| `tests/OwnExpeditor.Sii.Tests` | `Sii` | 7 testes com strings inline em `SiiDocumentTests.cs` e `RealSaveTests.cs` (saves reais de `samples/`, fora do git) |
| `tests/OwnExpeditor.Core.Tests` | `Core` | `UnitTest1.cs`, um teste vazio gerado pelo template |

> TODO: confirmar quando `Core`, `Data` e `Domain` ganharem código, e se o
> `Models\` do App será usado ou removido.

## Regras que a estrutura precisa manter

- Nenhuma camada inferior conhece a superior. Por exemplo, `Sii` não pode
  referenciar `Core` nem `App`.
- A UI (`App`) não lê nem grava `game.sii` diretamente; deve passar pelo `Core`.
  Hoje o `App` nem referencia `Sii`, então isso vale como alvo, não como fato
  verificado.
- Não há listas fixas de cidades, cargas ou reboques em nenhum projeto (ver
  [ADR 0003](../decisions/0003-sem-listas-fixas.md)).

Ver também: [overview.md](overview.md) para o diagrama e
[data-flow.md](data-flow.md) para a sequência de leitura e escrita.
