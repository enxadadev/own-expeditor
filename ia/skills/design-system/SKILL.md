---
name: design-system
description: Catálogo do design system do Own Expeditor — tokens, componentes reutilizáveis de src/OwnExpeditor.App/Controls, padrões de tela (shell, ciclo explorar→editar→revisar→aplicar), quando usar cada componente. Use ao desenhar (ux-designer), implementar (ui-dev) ou revisar (ux-reviewer) qualquer tela. Reutilize antes de criar.
---

# Design system

**Regra 1: reutilize.** Antes de criar um controle, procure aqui e na
Galeria (Debug). Componente novo só se nenhum serve — e então ele entra
neste catálogo e na Galeria no mesmo PR.

> Direção visual: **A+C "Cabine com mapa da rota"** (aprovada) —
> `design/direcao-visual/`. Mockup de referência:
> `design/direcao-visual/canvas/CabineRota.dc.html`.

## Tokens (`src/OwnExpeditor.App/Styles/Tokens.axaml` + `Colors.axaml`)

### Cores (ThemeDictionaries)
| Token | Dark (padrão) | Light | Uso |
| --- | --- | --- | --- |
| `BgColor` | `#121417` | `#F4F2EE` | fundo da janela |
| `SideColor` | `#0C0E10` | `#EBE7DF` | navegação lateral |
| `SurfaceColor` | `#1A1D21` | `#FFFFFF` | cartões, lista, painéis |
| `MapColor` | `#101316` | `#EFEBE3` | fundo do mapa |
| `LineColor` | `#2A2F35` | `#D9D3C7` | bordas, divisores |
| `TextColor` | `#EEF0F2` | `#1B1D20` | texto principal |
| `TextMutedColor` | `#9AA3AD` | `#5F6670` | rótulos, secundário (≥ 4.5:1) |
| `AccentColor` | `#F5A524` | `#B86E00` | seleção, rota, ação principal |
| `OnAccentColor` | `#1A1206` | `#1A1206` | texto sobre âmbar |
| `SelectionColor` | `#2A2414` | `#FBECD2` | item selecionado |
| `DangerColor` | `#F87171` | `#B42318` | erro, ação destrutiva |
| `SuccessColor` | `#4ADE80` | `#157F3B` | sucesso |
Cada cor tem o `…Brush` correspondente. Âmbar só para seleção, rota e a
ação principal — nunca decoração.

### Tipografia
| Token | Valor |
| --- | --- |
| `FontUi` | Barlow (embutida) — corpo e controles |
| `FontDisplay` | Barlow Condensed (embutida) — números, títulos em caixa alta |
| Tamanhos | `Caption 11–12` (rótulo caixa alta, `LetterSpacing` ~0.08em), `Body 14`, `Subtitle 16`, `Title 22–26` (condensed, caixa alta), `Metric 22`, `Hero 34–44` (condensed) |
Números sempre `FontFeatures="+tnum"`.

### Espaço, raio, elevação, movimento
| Grupo | Tokens |
| --- | --- |
| Espaço | `Space2 4`, `Space3 8`, `Space4 12`, `Space5 16`, `Space6 20`, `Space7 24` |
| Raio | `RadiusSm 8`, `RadiusMd 10`, `RadiusLg 12`, `RadiusXl 16` (mapa) |
| Elevação | borda `LineColor` como padrão; `ShadowPopup` só em flyouts/diálogos |
| Movimento | `DurFast 150ms`, `DurBase 200ms`, `EaseOut` |

## Componentes (`Controls/`)
| Componente | Para quê | Não use para |
| --- | --- | --- |
| `AppShell` | barra lateral de módulos + topo (perfil/save, tema, idioma) | — (um só) |
| `PageHeader` | título do módulo, contagem, ações | cabeçalho de seção interna |
| `SearchablePicker` | escolher 1 entre milhares (cidade, empresa, carga, reboque) com busca/virtualização | listas < 10 itens (use `ComboBox`) |
| `OfferList` / `DataList` | lista virtualizada selecionável com colunas | grade editável |
| `DetailPanel` | painel lateral de edição do item selecionado | diálogos |
| `FieldRow` | rótulo + valor/controle, com erro inline | — |
| `StatTile` | número grande + rótulo (`+tnum`) | texto longo |
| `RouteMap` | mapa esquemático origem → destino (+ cidades se houver coordenadas) | navegação geral de mapa |
| `RouteOfferItem` | item de lista "Origem ⋯ Destino · km" + carga | tabelas genéricas |
| `EmptyState` | lista vazia com motivo e ação | erro |
| `ErrorState` | o que aconteceu + o que fazer + tentar de novo | aviso leve (use `InfoBar`) |
| `LoadingState` | carregando com texto ("Lendo save…") | operações < 300 ms |
| `ChangeReviewList` | diff antes → depois de todas as mudanças pendentes | — (único lugar de revisão) |
| `ConfirmApplyDialog` | confirmar gravação, mostra backup | confirmações triviais |
| `Toast` | resultado de ação (sucesso, backup salvo em…) | erros que exigem ação |
| `Badge` | status curto (pendente, modificado, mod) | texto longo |

Cada componente: estilos via tokens, Light/Dark, textos por chave `.resx`,
estados `:pointerover/:pressed/:disabled/:focus-visible`, exemplo na Galeria.

## Padrões de tela
1. **Shell**: lateral compacta (ícone + rótulo curto em caixa alta: FRETES, FROTA, PERFIL, EMPRESA, MAPA);
   topo com perfil/save ativo, tema e idioma. Badge com nº de mudanças pendentes.
2. **Ciclo de módulo**: explorar (lista + busca) → selecionar → editar no
   `DetailPanel` → "Revisar mudanças (N)" → `ChangeReviewList` →
   `ConfirmApplyDialog` → `Toast` com caminho do backup.
3. **Nada grava sem revisão**: ações de módulo só alteram a sessão de edição.
4. Toda tela tem os 4 estados (carregando/vazio/erro/conteúdo).

## Galeria
`Views/Debug/GalleryView` (só em Debug): cada componente em todos os estados,
alternando Light/Dark e pt-BR/en. Screenshots: `toolkit/screenshots.sh`.
