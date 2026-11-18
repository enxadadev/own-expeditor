# Revisar e aplicar — fluxos

Parte do ciclo explorar → editar → **revisar → aplicar**. Contratos: ADR 0005
e `docs/architecture/plan-0005-shell.md` (`IEditSession`, `ChangeSet`,
`CoreError`, `ApplyResult`), com o delta da ADR 0006 / plano 0006
(`ApplyAsync(ApplyTarget)`, `ApplyTarget.Overwrite` / `NewSlot(displayName)`,
`ApplyResult(WrittenSlot, BackupPath?, ChangeSetCount, PatchCount)`).

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
    R -->|Aplicar mudanças ou Ctrl+Enter| K["Diálogo de confirmação destrutiva<br/>escolher como gravar"]
    K -->|Cancelar ou Esc| R
    K -->|"Sobrescrever este save (padrão)"| W["Gravando…<br/>ApplyAsync(Overwrite)"]
    K -->|"Criar um novo save + nome"| W2["Gravando…<br/>ApplyAsync(NewSlot(nome))"]
    W -->|ApplyResult| OK["Tela Resultado<br/>slot gravado + caminho do backup"]
    W2 -->|ApplyResult| OK2["Tela Resultado<br/>novo slot, original intacto"]
    W2 -->|CoreException| ER
    W -->|CoreException| ER["Erro no próprio diálogo<br/>(ver fluxo 2)"]
    OK -->|Abrir pasta| F["Explorador de arquivos"]
    OK -->|Continuar editando| A
    OK2 -->|Continuar editando| A2["Módulo, agora sobre o novo save<br/>(nova base da sessão)"]
```

Regras do fluxo:
- O selo "Revisar (n)" mostra o número de `ChangeSet`s pendentes. Com 0, o selo
  continua visível (sem número) e leva à tela Vazio.
- Descartar **item** e **módulo** é imediato e reversível (Toast "Desfazer" por
  6 s: a App guarda os `ChangeSet`s descartados e os re-enfileira). Descartar
  **tudo** pede confirmação, porque é a única ação que perde o trabalho todo.
- Depois do sucesso a sessão tem nova base e `Pending` vazio: o selo some o
  número e voltar ao módulo mostra os dados já gravados. Com **Criar um novo
  save** a nova base é o **slot novo** (a barra do shell passa a mostrar o nome
  novo) e o save original fica como estava.
- A escolha do destino é feita **só na confirmação**; "Sobrescrever este save"
  vem selecionado, o nome do novo save vem preenchido (`{save} (editado)`) e
  o botão muda de "Gravar no save" para "Criar novo save". Nome vazio ou com
  mais de 40 caracteres desabilita o botão e mostra o erro ao lado do campo.
- Perfil com Steam Cloud: aviso na confirmação (texto por destino) e uma linha no
  Resultado; nenhuma das opções é bloqueada.

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
    Q -->|ok| D{"Destino"}
    D -->|Overwrite| B["Backup game.sii.bak-data-hora"]
    B -->|já existe| E10["BackupExists"]
    B -->|falhou| E11["BackupFailed"]
    B -->|ok| T["Grava game.sii.tmp e verifica"]
    T -->|sem permissão| E12["AccessDenied"]
    T -->|falha de IO| E13["WriteFailed"]
    T -->|verificação falhou| E14["VerifyFailed"]
    T -->|ok| M["Substitui game.sii<br/>nova base da sessão"]
    M --> OK["ApplyResult → tela Resultado"]
    D -->|"NewSlot(nome)"| N1["Escolhe o próximo slot livre do perfil"]
    N1 -->|sem slot livre ou pasta não criada| E15["SlotUnavailable"]
    N1 -->|ok| N2["Copia a pasta do slot,<br/>grava game.sii.tmp e verifica,<br/>ajusta o nome em info.sii"]
    N2 -->|"sem permissão / IO / verificação"| E12
    N2 -->|ok| N3["Slot novo vira a base da sessão<br/>original intocado"]
    N3 --> OK
```

Erros fora do `ApplyAsync` (mesmo vocabulário `CoreError`, agora **20 códigos**):
`SaveParseFailed` (renomeado de `SaveFormatUnsupported`), `SaveCorrupt`, `SaveNotFound`, `NoSession` ao **abrir** o save (UX-1);
`Conflict`, `StalePatch`, `UnknownUnit`, `UnknownAttribute`, `InvalidEdit` (regra do módulo, com `Reason`, ex.: `Jobs_Error_*`) ao **enfileirar** (`Stage`) no módulo.

Em todo erro do `ApplyAsync` o save original fica intacto (ADR 0005); a tela
diz isso ("Nada foi gravado. Seu save continua como estava."). No destino
**novo save**, uma falha depois de criar a pasta apaga a pasta nova (o Core
garante), então a frase continua verdadeira.

## 3. Recuperação por erro

```mermaid
flowchart LR
    X["Erro ao aplicar"] --> T{"Tipo"}
    T -->|"Tentar de novo resolve"| R["GameRunning, SessionBusy, BackupExists,<br/>BackupFailed, WriteFailed, VerifyFailed, AccessDenied,<br/>SlotUnavailable (depois de liberar um slot)"]
    R --> R1["Botão Tentar de novo<br/>mudanças pendentes continuam<br/>(SlotUnavailable: voltar à revisão e escolher Sobrescrever)"]
    T -->|"Mudança inválida"| I["StalePatch, UnknownUnit, UnknownAttribute, InvalidEdit"]
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
