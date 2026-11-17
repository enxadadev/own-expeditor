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
- `DiffValue.Empty` aparece como "—" (nome acessível `Diff_Empty`) e sem riscado.
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
Modal sobre a tela Revisar (`ConfirmApplyDialog`, `IDialogService.ConfirmAsync(..., destructive: true)`), largura 560.

```
┌ GRAVAR MUDANÇAS NO SAVE? ───────────────────────────────┐
│ 3 mudanças serão gravadas em Perfil 1 · Save 4.         │
│ O QUE SERÁ GRAVADO                                      │
│ ┌ FRETES                                   2 mudanças ┐ │
│ │   Frete Sofia → Beograd                             │ │
│ │   Frete Wien → Praha                                │ │
│ │ PERFIL                                   1 mudança  │ │
│ │   Dinheiro do perfil                                │ │
│ ONDE FICA O BACKUP                                      │
│ [C:\...\profiles\profile_1\save\4\game.sii.bak-AAAAMMDD-HHMMSS]│
│ (!) Feche o jogo antes de continuar                     │
│     Com o jogo aberto, ele pode sobrescrever o save...  │
│ Para desfazer depois, basta restaurar o backup.         │
│                           [Cancelar]  [Gravar no save]  │
└─────────────────────────────────────────────────────────┘
```
- Só títulos dos `ChangeSet`s agrupados por módulo (o diff completo já foi visto na tela anterior).
- Caminho do backup: pasta real do save (`SaveSlotInfo.GameSiiPath`) + padrão do nome em destaque âmbar; o nome final usa a hora da gravação. Mono, quebra em qualquer ponto, `ToolTip` com o inteiro.
- Aviso do jogo: caixa `DangerColor` (borda + fundo 12%) com ícone. Se `IGameProcessMonitor.IsRunning` já for verdadeiro ao abrir, o título vira "O jogo está aberto" (`Confirm_Warn_*_Running`); o botão continua habilitado, porque a checagem que vale é a do `ApplyAsync`.
- **Foco inicial em Cancelar**; `Esc` cancela; foco preso no diálogo. Gravar é `Button.danger`. Não há `Ctrl+Enter` aqui.
- **Gravando…**: os botões dão lugar a spinner + "Gravando…" + "Criando o backup e gravando o save. Não feche o app." (`role=status`, `aria-live=polite`). Sem cancelar (backup e substituição não podem ser interrompidos no meio). Animação do spinner respeita "reduzir movimento".
- `aria-labelledby` = título, `aria-describedby` = intro.
- Estados: pergunta → gravando → (sucesso: tela 3 | erro: tela 4).

## 3. Resultado (`Resultado.dc.html`)
Página do shell mostrada após `ApplyResult`.

```
(✓) MUDANÇAS GRAVADAS
    3 mudanças gravadas no save.
┌ BACKUP DO SAVE ORIGINAL ────────────────────────────────┐
│ Guardamos uma cópia do save como ele estava antes...    │
│ [C:\...\save\4\game.sii.bak-20261117-143022]            │
│ [Abrir pasta] [Copiar caminho]                          │
┌ AGORA NO JOGO ────────────────┐ ┌ COMO RESTAURAR O BACKUP ┐
│ Abra o jogo e carregue...     │ │ 1. Feche o jogo.        │
│ O app grava em texto simples..│ │ 2. renomeie game.sii... │
│                                                [Continuar editando]
```
- `ApplyResult.BackupPath` completo (pasta + nome real). **Abrir pasta**: abre o explorador com o arquivo selecionado; falhou → Toast `Result_OpenFolder_Failed` com o caminho. **Copiar caminho**: copia o texto e mostra Toast "Caminho copiado" (`role=status`).
- "Continuar editando" é a ação principal (âmbar) e volta ao último módulo. Foco inicial no título (`tabindex=-1`) para leitores de tela anunciarem o resultado; Tab segue para Abrir pasta.
- A nota "grava em texto simples" cobre o risco aberto da ADR 0002 (jogo carregar texto puro); remover quando validado no jogo.
- Os passos de restauração repetem a seção "Recuperação" da skill `save-safety`.
- Componentes: `PageHeader`-like com ícone de sucesso (`SuccessColor`), cartões do catálogo, `Button` primário/secundário. **NOVO** `PathBox` (caixa mono com caminho que quebra, `ToolTip` e destaque do nome do arquivo; usado aqui e na confirmação).
- Estados: só conteúdo. Se `ApplyResult.ChangeSetCount == 1` usa `Result_Written_One`.

## 4. Erros ao aplicar (`ErroJogoAberto.dc.html`)
O artboard mostra `GameRunning`; a faixa inferior (fora do app, só no desenho) alterna entre os 18 `CoreError` e os dois idiomas.

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
| SaveFormatUnsupported | abrir save | Escolher outro save | — | n/a |
| SaveCorrupt | abrir save | Escolher outro save | — | n/a |
| NoSession | qualquer ação sem save (shell) | Escolher outro save | — | n/a |
| SessionBusy | aplicar | Tentar de novo | Voltar à revisão | mantidas |
| ReadOnlySession | aplicar | Fechar | — | mantidas |
| Conflict | módulo ao enfileirar (InfoBar no módulo) | Abrir revisão | Fechar | mantidas |
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

Notas:
- Os erros de abrir o save pertencem ao UX-1 (estados sem sessão/erro); estão aqui para ter **todas** as mensagens `Error_<Code>` num lugar só, com o mesmo vocabulário.
- `Conflict` acontece no `Stage`, fora do fluxo de aplicar: o módulo mostra a mensagem em `InfoBar` (não é modal) com a ação "Abrir revisão".
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

Pedido ao UX-3 (`design/controles/`): incluir esses itens e o `ConfirmApplyDialog`
em modo pergunta / gravando / erro.
