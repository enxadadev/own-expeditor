# Revisar e aplicar — telas

Tamanho de referência 1280×800, tema escuro padrão, claro conferido. Tokens e
componentes de `ia/skills/design-system`. Marcação **NOVO** = ainda não existe
no catálogo. Lateral e barra de sessão aparecem simplificadas: o desenho
oficial é `design/shell/` (UX-1).

Dados de exemplo (anonimizados): frete **Sofia → Istanbul** trocado para
**Sofia → Beograd** (destino, carga, distância), um segundo frete Wien → Praha e,
só para mostrar o agrupamento, uma mudança do futuro módulo Perfil (dinheiro).

## Mudança em relação ao catálogo
`design-system` diz que, depois da confirmação, aparece um `Toast` com o caminho
do backup. Aqui o resultado é uma **tela** (pedido da UX-2), porque o caminho
precisa ser lido, copiado e aberto. O `Toast` fica para avisos curtos (desfazer
descarte, caminho copiado). Atualizar o padrão de tela nº 2 da skill.

## 1. Revisar mudanças (`Revisar.dc.html`)
Página do shell (não de módulo). Aberta pelo selo "Revisar (n)" ou `Ctrl+R`.

```
┌ lateral ┬ barra: PERFIL 1 · SAVE 4 [ETS2] ........................ [Revisar (3)] ┐
│ FRETES  │ REVISAR MUDANÇAS                                      [Descartar tudo] │
│ FROTA   │ 3 mudanças em 2 módulos                                                │
│ PERFIL  │ CAMPO      ANTES                     DEPOIS            ┌ MUDANÇAS 3 ┐ MÓDULOS 2 │
│ EMPRESA │ FRETES (2)                       [Descartar módulo]    │ Antes de gravar          │
│ MAPA    │ ┌ Frete Sofia → Beograd              [x Descartar] ┐   │ ✓ backup ao lado         │
│         │ │ Destino  ~~LogNStick · Istanbul~~ → Tradeaux · Beograd │ ✓ tudo de uma vez        │
│         │ │ Carga    ~~Concrete beams~~       → Machinery      │   │ ! feche o jogo           │
│         │ │ Distância ~~569 km~~              → 394 km         │   │                          │
│         │ └────────────────────────────────────────────────┘   │ [Continuar editando]     │
│ tema    │ ┌ Frete Wien → Praha ... ┐   PERFIL (1) ...           │ [Aplicar mudanças] (perigo)
│ idioma  │                                                        │ Nada é gravado sem confirmação
└─────────┴────────────────────────────────────────────────────────┴──────────────────────────┘
```

**Componentes**
- `PageHeader` (título, subtítulo "3 mudanças em 2 módulos", ação "Descartar tudo" em estilo fantasma/perigo).
- `ChangeReviewList` com três níveis: cabeçalho de módulo (`Badge` com a contagem + "Descartar módulo"), cartão por `ChangeSet` (`ChangeSet.Title` + "Descartar"), linhas `DiffLine` em grade **Campo | Antes | → | Depois**. Cabeçalho de colunas fixo acima da lista (decorativo; a leitura de acessibilidade vem do nome da linha).
- `StatTile` ×2 (mudanças, módulos), painel "Antes de gravar" (3 itens), `Button` secundário e `Button` perigo.
- **NOVO `Button.danger`** (preenchido, ação destrutiva): fundo `DangerColor`, texto `OnDangerColor`. Estados `:pointerover` (+8% claro), `:pressed` (scale padrão do Fluent), `:disabled` (opacidade .4, sem hit-test), `:focus-visible` (anel 2 px `AccentColor`, offset 2). Altura 48–52, hit area ≥ 44.
- **NOVO token `OnDangerColor`**: `#1A0606` no escuro, `#FFFFFF` no claro (contraste ≥ 4.5:1 sobre `DangerColor` nos dois temas). Os demais tokens são os existentes.
- **NOVO `Button.ghost`** (texto, sem borda; variante perigo para "Descartar tudo"), altura 44; ícone ✕ de 14 px só em "Descartar" de cartão, com texto visível.

**Regras de conteúdo**
- Antes: `TextMutedColor` + riscado (`TextDecoration.Strikethrough`). Depois: peso 600. Não depender de cor: seta e riscado já distinguem.
- `DiffValue.Empty` aparece como "—" (nome acessível `Diff_Empty`) e sem riscado. Isso acontece nos fretes em **vaga livre** (o slot estava vazio); em **substituir uma oferta** o antes traz os valores da oferta atual. Distância estimada pelo app aparece com "≈" (`Format_KmApprox`), antes e depois.
- Números (km, dinheiro) em `FontDisplay`, `+tnum`, formatados pela cultura (`1.250.000 €` pt-BR, `€1,250,000` en). Texto longo (mods): uma linha com reticências e `ToolTip` com o valor inteiro.
- Módulos na ordem de `ModuleDescriptor.Order`; cartões na ordem de `Stage`.
- O total do cabeçalho é a **quantidade de `ChangeSet`s** (o que o usuário consegue descartar), não `PatchCount` (termo interno).
- A lista rola sozinha; resumo e botões ficam fixos à direita. "Aplicar mudanças" sempre no canto inferior direito (direção visual).

**Estados**
| Estado | O que mostra |
| --- | --- |
| Carregando | não existe: `GetDiff()` é síncrono e local |
| Vazio | tela 5 (cabeçalho "Nenhuma mudança pendente") |
| Conteúdo | acima; Aplicar habilitado |
| Descartou algo | Toast "Mudança descartada. Desfazer" (6 s, `role=status`) |
| Descartar tudo | diálogo: "Descartar todas as mudanças?" / "Todas as mudanças pendentes serão perdidas. O save não foi alterado." / [Cancelar] (foco) [Descartar tudo] (perigo) |
| Erro | não há erro de leitura aqui; erros ao aplicar vão para a tela 4 |

**Ordem de foco (Tab):** Descartar tudo → por módulo: Descartar módulo → por cartão: Descartar → Continuar editando → Aplicar mudanças.
**Atalhos:** `Ctrl+R` abre a tela; `Ctrl+Enter` abre a confirmação; `Esc` volta ao módulo; `Delete` descarta o cartão focado.
**Nomes acessíveis:** cada botão de descarte diz o alvo ("Descartar mudança: Frete Sofia → Beograd", "Descartar todas as mudanças de Fretes"); cada linha é um grupo "Destino: antes LogNStick · Istanbul, depois Tradeaux · Beograd"; lista com nome "Mudanças pendentes"; Toast com `role=status`.
**Tema/idioma:** conferido nos dois; inglês ~30% mais curto nos rótulos, mensagens mais longas em pt-BR.

## 2. Confirmação destrutiva (`Confirmar.dc.html`)
Modal sobre a tela Revisar (`ConfirmApplyDialog`, `IDialogService.ConfirmAsync(..., destructive: true)`), largura 620, altura máx. 724 (o corpo rola; **os botões ficam fixos embaixo**). O usuário **escolhe como gravar** (ADR 0006, decisão 5).

```
┌ GRAVAR MUDANÇAS NO SAVE? ───────────────────────────────┐
│ 3 mudanças serão gravadas em Perfil 1 · Save 4.         │
│ (!) Feche o jogo antes de continuar                     │   <- avisos no topo
│ (▲) Este perfil usa Steam Cloud (só com Steam Cloud)    │
│ O QUE SERÁ GRAVADO                                      │
│ ┌ FRETES                                   2 mudanças ┐ │
│ │ Frete Sófia → Belgrado · Frete Viena → Praga        │ │
│ │ PERFIL                                   1 mudança  │ │
│ │ Dinheiro do perfil                                  │ │
│ COMO GRAVAR                                             │
│ (●) Sobrescrever este save  [Padrão]                    │
│     Grava no save aberto (Save 4). Um backup é criado.  │
│ ( ) Criar um novo save                                  │
│     Cria um save novo com as mudanças. O original não   │
│     é tocado.                                           │
│ ── detalhe do destino escolhido (ver abaixo) ──         │
│ Para desfazer depois, basta restaurar o backup.         │
├─────────────────────────────────────────────────────────┤
│                           [Cancelar]  [Gravar no save]  │
└─────────────────────────────────────────────────────────┘
```
- **O que será gravado**: um bloco por módulo (nome em caixa alta, contagem) com os títulos dos `ChangeSet`s numa linha (reticências + `ToolTip`). O diff completo já foi visto na tela anterior. (Compactado em relação à versão anterior para dar lugar à escolha.)
- **Como gravar**: `radiogroup` com dois **cartões de rádio** (NOVO `ChoiceCard`, abaixo). O padrão é **Sobrescrever este save** (etiqueta "Padrão"). Setas ↑/↓ movem e escolhem; Espaço também escolhe.
- **Detalhe do destino**, abaixo dos cartões:
  | Destino | O que aparece |
  | --- | --- |
  | Sobrescrever este save | "Onde fica o backup": caminho da pasta do save + padrão `game.sii.bak-AAAAMMDD-HHMMSS` em âmbar (`PathBox`), nota "A cópia do save original é criada nesta pasta antes de gravar…" |
  | Criar um novo save | **Nome do novo save** (`TextBox` 44 px, valor inicial `{save} (editado)`, 1 a 40 caracteres, `maxlength=40`, dica "É o nome que aparece na lista de saves do jogo.", erro inline `Confirm_NewName_Invalid` com `aria-invalid`); "Onde fica o novo save": pasta `…\save\` + "(próximo slot livre)" em âmbar e a nota "O app cria uma pasta nova no próximo slot livre do perfil {perfil}. O save original continua como está." Não há backup (o original não muda) |
- Texto de apoio abaixo: sobrescrever → "Para desfazer depois, basta restaurar o backup…"; novo save → "O save original não muda. Para descartar o novo save, basta apagá-lo…". Intro e botão mudam com o destino: `Confirm_Intro_*` / `Confirm_IntroNew_*`; **Gravar no save** / **Criar novo save**.
- **Avisos** (no topo do corpo, 8 px de padding, sempre visíveis ao abrir):
  - Jogo: caixa `Danger` com ícone ▲. Se `IGameStatusMonitor.IsRunning` já for verdadeiro ao abrir, o título vira "O jogo está aberto" (`Confirm_Warn_*_Running`); o botão continua habilitado, porque a checagem que vale é a do `ApplyAsync`.
  - **Steam Cloud** (só se `ProfileInfo.IsSteamCloud`): caixa `Warning` (`Selection` + borda `AccentText`, ícone ▲) com `Confirm_Cloud_Title` e o texto do destino escolhido (`Confirm_Cloud_Overwrite` / `Confirm_Cloud_New`). Informa; não bloqueia nenhuma opção.
- **Foco inicial em Cancelar**; `Esc` cancela; foco preso no diálogo; o campo de nome só recebe foco por Tab ou ao escolher "Criar um novo save" (o foco vai para o cartão escolhido, não para o campo). Gravar é `Button.danger`; com nome inválido fica `aria-disabled` (opacidade 0,4) e o motivo está no texto do campo. Não há `Ctrl+Enter` aqui.
- **Gravando…**: os botões dão lugar a spinner + "Gravando…" + dica do destino (`Confirm_Applying_Hint` / `_New`) (`role=status`, `aria-live=polite`). Sem cancelar. Animação do spinner respeita "reduzir movimento".
- `aria-labelledby` = título, `aria-describedby` = intro; `radiogroup` com nome "Como gravar".
- Estados: pergunta (dois destinos × nuvem × jogo aberto × nome inválido) → gravando → (sucesso: tela 3 | erro: tela 4).

## 3. Resultado (`Resultado.dc.html`)
Página do shell mostrada após `ApplyResult` (agora `ApplyResult(WrittenSlot, BackupPath?, ChangeSetCount, PatchCount)`).

```
(✓) MUDANÇAS GRAVADAS            |   (✓) NOVO SAVE CRIADO
    3 mudanças gravadas no save. |       3 mudanças gravadas no novo save “Save 4 (editado)”.
┌ SAVE GRAVADO  Save 4 · slot 4        O save aberto foi atualizado. ┐
│ [C:\...\profile_1\save\4\]                                      │
┌ BACKUP DO SAVE ORIGINAL  (só ao sobrescrever) ──────────┐
│ [C:\...\save\4\game.sii.bak-20261117-143022]            │
│ [Abrir pasta] [Copiar caminho]                          │
(▲) Se o jogo trouxer o save antigo de volta (Steam Cloud)…   (só com Steam Cloud)
┌ AGORA NO JOGO ────────────────┐ ┌ COMO RESTAURAR O BACKUP ┐
│ Abra o jogo e carregue...     │ │ 1. Feche o jogo.        │
│ A edição continua no novo save│ │ 2. renomeie game.sii... │
│                                                [Continuar editando]
```
- **Sobrescreveu**: título "Mudanças gravadas"; cartão **Save gravado** com nome e slot ("Save 4 · slot 4") e a pasta; cartão **Backup do save original** com `BackupPath` completo (pasta + nome real), **Abrir pasta** (explorador com o arquivo selecionado; falhou → Toast `Result_OpenFolder_Failed` com o caminho) e **Copiar caminho** (Toast "Caminho copiado"). "Como restaurar o backup" (3 passos, da skill `save-safety`).
- **Criou um novo save**: título "Novo save criado"; cartão **Novo save** com o nome, o slot ("Save 4 (editado) · slot 7"), a pasta (`WrittenSlot`) e os botões **Abrir pasta** / **Copiar caminho** no próprio cartão; **sem** cartão de backup (não há); "Agora no jogo" cita o nome do save novo e diz "A edição continua no novo save." (a sessão aponta para o slot novo; "Continuar editando" edita o novo save); o segundo cartão vira **Como descartar o novo save** (2 passos: fechar o jogo; apagar a pasta do novo save). A nota "grava em texto simples" muda para "…se o jogo não carregar o novo save, apague-o: o original continua intacto."
- **Steam Cloud**: uma linha `Warning` entre os cartões, por destino (`Result_Cloud_Overwrite` / `Result_Cloud_New`).
- "Continuar editando" é a ação principal (âmbar) e volta ao último módulo. Foco inicial no título (`tabindex=-1`); Tab segue para Abrir pasta.
- A nota "grava em texto simples" cobre o risco aberto da ADR 0002; remover quando validado no jogo.
- Componentes: `PageHeader`-like com ícone de sucesso (`SuccessColor`), cartões do catálogo, `Button` primário/secundário, `PathBox` (NOVO, ver abaixo).
- Estados: só conteúdo. `ChangeSetCount == 1` usa `Result_Written_One` / `Result_WrittenNew_One`.

## 4. Erros ao aplicar (`ErroJogoAberto.dc.html`)
O artboard mostra `GameRunning`; a faixa inferior (fora do app, só no desenho) alterna entre os **20** `CoreError` e os dois idiomas.

```
┌ (!) NÃO FOI POSSÍVEL GRAVAR ────────────────────────────┐
│ <Error_<Code>: o que aconteceu + o que fazer>           │
│ ✓ Nada foi gravado. Seu save continua como estava.      │
│ Suas mudanças pendentes continuam aqui.                 │
│ ─ DETALHES  GameRunning                                 │
│                        [Voltar à revisão] [Já fechei, tentar de novo]│
└─────────────────────────────────────────────────────────┘
```
Usa `ErrorState` dentro do próprio diálogo de confirmação (o modal troca de conteúdo; sem segundo diálogo). `role=alertdialog`, foco no botão primário. Título por contexto: `Error_Title_Apply` (aplicar), `Error_Title_Open` (abrir save), `Error_Title_Edit` (enfileirar). A linha "Nada foi gravado…" só nos contextos em que é verdade e útil; a linha de pendentes depende de manter ou perder.

### Erros por CoreError
Ação primária em âmbar, secundária em contorno. "Pend." = o que acontece com as mudanças pendentes.

| Código | Onde aparece | Primária | Secundária | Pend. |
| --- | --- | --- | --- | --- |
| SaveNotFound | abrir save; aplicar (arquivo sumiu) | Escolher outro save | — | n/a |
| SaveParseFailed | abrir save (renomeado de `SaveFormatUnsupported`) | Escolher outro save | — | n/a |
| SaveCorrupt | abrir save | Escolher outro save | — | n/a |
| NoSession | qualquer ação sem save (shell) | Escolher outro save | — | n/a |
| SessionBusy | aplicar | Tentar de novo | Voltar à revisão | mantidas |
| ReadOnlySession | aplicar | Fechar | — | mantidas |
| Conflict | módulo ao enfileirar (InfoBar no módulo) | Abrir revisão | Fechar | mantidas |
| InvalidEdit | módulo ao enfileirar (regra do módulo; `ErrorBanner` no módulo, ver `design/fretes/`) | Fechar | Abrir revisão | mantidas |
| StalePatch | aplicar | Voltar à revisão | Abrir o save de novo | mantidas |
| UnknownUnit | aplicar | Voltar à revisão | Abrir o save de novo | mantidas |
| UnknownAttribute | aplicar | Voltar à revisão | Abrir o save de novo | mantidas |
| NothingToApply | aplicar | Fechar | — | n/a |
| GameRunning | aplicar | Já fechei, tentar de novo | Voltar à revisão | mantidas |
| SaveChangedOnDisk | aplicar | Abrir o save de novo | — | descartadas |
| BackupExists | aplicar | Tentar de novo | — | mantidas |
| BackupFailed | aplicar | Tentar de novo | Voltar à revisão | mantidas |
| WriteFailed | aplicar | Tentar de novo | Voltar à revisão | mantidas |
| VerifyFailed | aplicar | Tentar de novo | Voltar à revisão | mantidas |
| AccessDenied | aplicar | Tentar de novo | Abrir pasta | mantidas |
| SlotUnavailable | aplicar com **Criar um novo save** (sem slot livre ou pasta não criada) | Tentar de novo | Voltar à revisão | mantidas |

Notas:
- Os erros de abrir o save pertencem ao UX-1 (estados sem sessão/erro); estão aqui para ter **todas** as mensagens `Error_<Code>` num lugar só, com o mesmo vocabulário.
- `Conflict` e `InvalidEdit` acontecem no `Stage`, fora do fluxo de aplicar: o módulo mostra a mensagem no `ErrorBanner` (não é modal). `InvalidEdit` traz um motivo do módulo (`CoreException.Reason`, ex.: `Jobs_Error_NoFreeSlot`); **se houver `Reason`, a tela mostra o texto dele no lugar de `Error_InvalidEdit`** (que é o texto genérico de reserva). Título `Error_Title_Edit`.
- `SlotUnavailable`: nada foi gravado e a pasta nova, se chegou a ser criada, é apagada; a mensagem sugere voltar à revisão e escolher "Sobrescrever este save" (ou liberar um slot no jogo). A ação "Tentar de novo" repete a gravação com o mesmo destino.
- `Error_Details` mostra o código e o `CoreException.Message` em mono para o usuário colar num relato. Sem termos do SII na mensagem principal.
- Texto completo pt-BR/en de cada código: `strings.md`.

## 5. Nada para revisar (`Vazio.dc.html`)
`EmptyState` dentro da página Revisar: ícone de prancheta, "Nada para revisar", explicação, **Ir para Fretes** (primária; vai ao primeiro módulo habilitado), dica de atalho. "Aplicar mudanças" aparece desabilitado (`aria-disabled`) com a dica "Aplicar fica disponível quando houver mudanças." Também é o destino quando o usuário descarta tudo. O selo da barra continua sem número.

## Componentes novos (resumo)
| Nome | Para quê | Estados |
| --- | --- | --- |
| `Button.danger` | gravar no save, descartar tudo (diálogo) | normal, hover, pressed, disabled, focus-visible |
| `Button.ghost` | descartar item/módulo/tudo | idem; variante `danger` |
| `OnDangerColor` (token) | texto sobre `DangerColor` | Light/Dark |
| `PathBox` | caminho mono com quebra, destaque do arquivo, ToolTip | normal |
| `ChoiceCard` (grupo `radiogroup`) | escolher o destino da gravação: ponto de rádio, rótulo, etiqueta opcional ("Padrão"), ajuda de uma linha | normal, hover, selecionado (`Selection` + borda `AccentText`), `:focus-visible`, desabilitado (opacidade 0,4) |
| `TextBox` com erro inline (nome do novo save) | nome do save novo | normal, foco, inválido (borda `Danger` + texto de erro, `aria-invalid`) |

Pedido ao UX-3 (`design/controles/`): incluir esses itens e o `ConfirmApplyDialog`
em modo pergunta (com os dois destinos) / gravando / erro. A `ChoiceCard` e o campo de nome com erro entram no catálogo na implementação.
