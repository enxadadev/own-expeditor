---
name: design-system
description: Catálogo do design system — tokens, componentes de src/OwnExpeditor.App/Controls e padrões de tela (shell, ciclo de módulo). Use ao desenhar, implementar ou revisar tela; reutilize antes de criar.
---

# Design system

**Regra 1: reutilize.** Antes de criar um controle, procure aqui e na
Galeria (Debug). Componente novo só se nenhum serve — e então ele entra
neste catálogo e na Galeria no mesmo PR.

> Direção visual: **A+C "Cabine com mapa da rota"** (aprovada) —
> `design/direcao-visual/`. Mockup de referência:
> `design/direcao-visual/canvas/CabineRota.dc.html`.

## Tokens (`src/OwnExpeditor.App/Styles/Tokens.axaml` + `Colors.axaml`)
Leia `reference.md` (seção Tokens) ao escolher cor, tipografia, espaço, raio, elevação ou movimento.

## Componentes (`Controls/`)
Leia `reference.md` (seção Componentes) antes de criar qualquer controle, para checar se já existe.

## Padrões de tela
1. **Shell**: lateral compacta (ícone + rótulo curto em caixa alta: FRETES, FROTA, PERFIL, EMPRESA, MAPA);
   topo com perfil/save ativo, tema e idioma. Badge com nº de mudanças pendentes.
2. **Ciclo de módulo**: explorar (lista + busca) → selecionar → editar no
   `DetailPanel` → "Revisar mudanças (N)" → `ChangeReviewList` →
   `ConfirmApplyDialog` → página **Resultado** com caminho do backup. Atalhos: Ctrl+R revisar, Ctrl+Enter (na revisão) abre a confirmação.
   - **Destino na confirmação** (ADR 0006): duas opções, com a padrão marcada.
     **Sobrescrever este save** (padrão): cria backup `game.sii.bak-<timestamp>`
     e substitui o original. **Criar um novo save**: grava um slot novo no mesmo
     perfil e deixa o original intacto; com Steam Cloud ativo, mostra o aviso de
     que o jogo pode não listar o slot. O resultado informa qual destino foi usado.
3. **Nada grava sem revisão**: ações de módulo só alteram a sessão de edição.
4. Toda tela tem os 4 estados (carregando/vazio/erro/conteúdo).

## Galeria
`Views/Debug/GalleryView` (só em Debug): cada componente em todos os estados,
alternando Light/Dark e pt-BR/en. Screenshots: `toolkit/screenshots.sh`.
