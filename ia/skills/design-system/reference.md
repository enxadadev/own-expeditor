# Design system — referência

Detalhe de tokens e componentes. Índice e regras em `SKILL.md`.

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
| `AccentColor` | `#F5A524` | `#B86E00` | preenchimento, borda, rota (não texto no claro) |
| `AccentTextColor` | `#F5A524` | `#8A5200` | texto em âmbar |
| `OnAccentColor` | `#1A1206` | `#1A1206` | texto sobre âmbar |
| `SelectionColor` | `#2A2414` | `#FBECD2` | item selecionado |
| `DangerColor` | `#F87171` | `#B42318` | erro, ação destrutiva |
| `SuccessColor` | `#4ADE80` | `#0F6B33` | sucesso |
| `BorderStrongColor` | `#66717D` | `#857E6F` | borda de campo e botão neutro (≥3:1) |
| `OnDangerColor` | `#1A0606` | `#FFFFFF` | texto sobre `Danger` |
Também: `SurfaceHover/Pressed`, `AccentHover/Pressed`, `DangerHover/Pressed`, `DangerBg`, `SuccessBg`, `Focus`, tokens do mapa, `RouteGlow`, `Overlay` — valores e os 48 pares de contraste em `design/controles/screens.md` §1. Cada cor tem o `…Brush` correspondente. Âmbar só para seleção, rota e a
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
| `AppShell` | lateral de módulos + `SessionBar` (perfil, save, jogo, selo Revisar); tema/idioma ficam em Configurações | — (um só) |
| `SessionBar`, `NavItem`, `ProfileSavePicker`, `SegmentedChoice`, `FolderField`, `StepList`, `CodeSnippet` | peças do shell e de Configurações | ver `design/shell/screens.md` |
| `PageHeader` | título do módulo, contagem, ações | cabeçalho de seção interna |
| `SearchablePicker` | escolher 1 entre milhares (cidade, empresa, carga, reboque) com busca/virtualização | listas < 10 itens (use `ComboBox`) |
| `OfferList` / `DataList` | lista virtualizada selecionável com colunas | grade editável |
| `DetailPanel` | painel lateral de edição do item selecionado | diálogos |
| `FieldRow` | rótulo + valor/controle, com erro inline | — |
| `StatTile` | número grande + rótulo (`+tnum`) | texto longo |
| `RouteMap` | mapa esquemático origem → destino (+ cidades se houver coordenadas) | navegação geral de mapa |
| `RouteOfferItem` | item de lista "Origem ⋯ Destino · km" + carga | tabelas genéricas |
| `EmptyState` | lista vazia com motivo e ação | erro |
| `ErrorBanner` (Inline/Block) | o que aconteceu + o que fazer + ação | aviso leve |
| `LoadingPanel` | carregando com etapas e progresso | operações < 300 ms |
| `SectionCard`, `ValueChange` | agrupar; um valor antes → depois | — |
| `DiffList` | diff antes → depois de todas as mudanças pendentes | — (único lugar de revisão) |
| `ConfirmApplyDialog` | confirmar gravação, mostra backup | confirmações triviais |
| `Toast` | feedback curto ("Desfazer" após descartar) | resultado de aplicar (é página) |
| `PendingBadge` | nº de mudanças pendentes | texto longo |
| Botões `accent` / `danger` / `neutral` / `ghost` | ação principal / gravar ou descartar (perigo) / secundária / terciária | — |

Cada componente: estilos via tokens, Light/Dark, textos por chave `.resx`,
estados `:pointerover/:pressed/:disabled/:focus-visible`, exemplo na Galeria.
