# Revisar e aplicar — fluxos

Parte do ciclo explorar → editar → **revisar → aplicar**. Contratos: ADR 0005
e `docs/architecture/plan-0005-shell.md` (`IEditSession`, `ChangeSet`,
`CoreError`, `ApplyResult`).

## 1. Fluxo principal

```mermaid
flowchart TD
    A["Módulo: editar e enfileirar mudança<br/>(Stage)"] --> B["Selo 'Revisar (n)' na barra do save"]
    B -->|clique ou Ctrl+R| C{"Há mudanças?"}
    C -->|não| V["Tela Vazio: nada para revisar"]
    V -->|Ir para módulo| A
    C -->|sim| R["Tela Revisar<br/>por módulo, antes → depois"]
    R -->|Descartar item ou módulo| D1["Remove na hora<br/>+ Toast com Desfazer"]
    D1 --> R2{"Sobrou algo?"}
    R2 -->|sim| R
    R2 -->|não| V
    R -->|Descartar tudo| D2["Confirmar descarte"]
    D2 -->|confirma| V
    D2 -->|cancela| R
    R -->|Continuar editando ou Esc| A
    R -->|Aplicar mudanças ou Ctrl+Enter| K["Diálogo de confirmação destrutiva"]
    K -->|Cancelar ou Esc| R
    K -->|Gravar no save| W["Gravando…<br/>ApplyAsync"]
    W -->|ApplyResult| OK["Tela Resultado<br/>caminho do backup"]
    W -->|CoreException| ER["Erro no próprio diálogo<br/>(ver fluxo 2)"]
    OK -->|Abrir pasta| F["Explorador de arquivos"]
    OK -->|Continuar editando| A
```

Regras do fluxo:
- O selo "Revisar (n)" mostra o número de `ChangeSet`s pendentes. Com 0, o selo
  continua visível (sem número) e leva à tela Vazio.
- Descartar **item** e **módulo** é imediato e reversível (Toast "Desfazer" por
  6 s: a App guarda os `ChangeSet`s descartados e os re-enfileira). Descartar
  **tudo** pede confirmação, porque é a única ação que perde o trabalho todo.
- Depois do sucesso a sessão tem nova base e `Pending` vazio: o selo some o
  número e voltar ao módulo mostra os dados já gravados.

## 2. Aplicar: etapas e onde cada erro aparece

```mermaid
flowchart TD
    S["ApplyAsync"] --> G{"Jogo aberto?"}
    G -->|sim| E1["GameRunning"]
    G -->|não| P{"Sessão ok?"}
    P -->|somente leitura| E2["ReadOnlySession"]
    P -->|já gravando| E3["SessionBusy"]
    P -->|sem mudanças| E4["NothingToApply"]
    P -->|ok| H{"Arquivo mudou no disco?<br/>tamanho, data, SHA-256"}
    H -->|sim| E5["SaveChangedOnDisk"]
    H -->|arquivo sumiu| E6["SaveNotFound"]
    H -->|não| Q["Parse da cópia e patches"]
    Q -->|item não existe| E7["UnknownUnit"]
    Q -->|campo não existe| E8["UnknownAttribute"]
    Q -->|valor antigo difere| E9["StalePatch"]
    Q -->|ok| B["Backup game.sii.bak-data-hora"]
    B -->|já existe| E10["BackupExists"]
    B -->|falhou| E11["BackupFailed"]
    B -->|ok| T["Grava game.sii.tmp e verifica"]
    T -->|sem permissão| E12["AccessDenied"]
    T -->|falha de IO| E13["WriteFailed"]
    T -->|verificação falhou| E14["VerifyFailed"]
    T -->|ok| M["Substitui game.sii<br/>nova base da sessão"]
    M --> OK["ApplyResult → tela Resultado"]
```

Erros fora do `ApplyAsync` (mesmo vocabulário `CoreError`):
`SaveFormatUnsupported`, `SaveCorrupt`, `SaveNotFound`, `NoSession` ao **abrir** o save (UX-1);
`Conflict`, `StalePatch`, `UnknownUnit`, `UnknownAttribute` ao **enfileirar** (`Stage`) no módulo.

Em todo erro do `ApplyAsync` o save original fica intacto (ADR 0005); a tela
diz isso ("Nada foi gravado. Seu save continua como estava.").

## 3. Recuperação por erro

```mermaid
flowchart LR
    X["Erro ao aplicar"] --> T{"Tipo"}
    T -->|"Tentar de novo resolve"| R["GameRunning, SessionBusy, BackupExists,<br/>BackupFailed, WriteFailed, VerifyFailed, AccessDenied"]
    R --> R1["Botão Tentar de novo<br/>mudanças pendentes continuam"]
    T -->|"Mudança inválida"| I["StalePatch, UnknownUnit, UnknownAttribute"]
    I --> I1["Voltar à revisão: descartar a mudança<br/>ou Abrir o save de novo"]
    T -->|"Save mudou ou sumiu"| C["SaveChangedOnDisk, SaveNotFound"]
    C --> C1["Abrir o save de novo / escolher outro<br/>pendentes são descartadas"]
    T -->|"Sem ação possível"| N["ReadOnlySession, NothingToApply"]
    N --> N1["Fechar"]
```

Detalhes por código estão em `screens.md` (tabela "Erros por CoreError").

## 4. Teclado

```mermaid
flowchart LR
    CS["Ctrl+R"] --> RV["Abre Revisar (ou Vazio)"]
    CE["Ctrl+Enter na tela Revisar"] --> DG["Abre o diálogo de confirmação"]
    ES["Esc"] --> BK["Revisar: volta ao módulo<br/>Diálogos: cancela/fecha"]
    DEL["Delete no item focado"] --> DI["Descarta o item (com Desfazer)"]
```

`Ctrl+Enter` **não** grava direto: só abre o diálogo. Dentro do diálogo não há
atalho que confirme; o foco inicial fica em **Cancelar**.
