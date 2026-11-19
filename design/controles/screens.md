# Biblioteca de controles — tokens, controles e galeria

Status: rascunho. Mockups em [`canvas/Tokens.dc.html`](canvas/Tokens.dc.html) e [`canvas/Galeria.dc.html`](canvas/Galeria.dc.html). Textos em [`strings.md`](strings.md).

Convenções: tipos em C#/Avalonia (`string`, `bool`, `int?`, `ICommand?`, `object?`). Propriedades são `StyledProperty`; "bindável" = TwoWay por padrão quando indicado. Nenhum controle tem ViewModel, nem busca serviços, nem tem texto literal (ADR 0005, item 7). Dados de exemplo: ids sintéticos.

---

## 1. Tokens

Arquivos: `src/OwnExpeditor.App/Styles/Tokens.axaml` (espaços, raios, tipografia, tempos: `StaticResource`) e `Colors.axaml` (cores: `ThemeDictionaries` Light/Dark, consumidas com `DynamicResource`). Cada cor tem o `…Brush`.

### 1.1 Cores

Valores confirmados da skill `design-system` e **ajustes** (coluna Mudou). Dark é o padrão.

| Token | Dark | Light | Mudou | Uso |
| --- | --- | --- | --- | --- |
| `BgColor` | `#121417` | `#F4F2EE` | não | fundo da janela |
| `SideColor` | `#0C0E10` | `#EBE7DF` | não | navegação lateral, cabeçalho de grupo |
| `SurfaceColor` | `#1A1D21` | `#FFFFFF` | não | cartões, campos, lista, painéis |
| `SurfaceHoverColor` | `#22262B` | `#F6F3EC` | novo | hover de item/campo/botão neutro |
| `SurfacePressedColor` | `#2A2F35` | `#EDE8DD` | novo | pressed de item/botão neutro |
| `MapColor` | `#101316` | `#EFEBE3` | não | fundo do mapa |
| `LineColor` | `#2A2F35` | `#D9D3C7` | não | **só** divisores e bordas de cartão (decorativo, não precisa de contraste) |
| `BorderStrongColor` | `#66717D` | `#857E6F` | novo | borda de campo, botão neutro, chip (≥ 3:1) |
| `TextColor` | `#EEF0F2` | `#1B1D20` | não | texto principal |
| `TextMutedColor` | `#9AA3AD` | `#5F6670` | não | rótulos, secundário |
| `AccentColor` | `#F5A524` | `#B86E00` | não, **mas restrito** | preenchimento (botão), borda de seleção, rota no mapa, barra de progresso. **Nunca texto no claro** |
| `AccentHoverColor` | `#FFB84A` | `#C47A0A` | novo | hover do botão accent |
| `AccentPressedColor` | `#E8991A` | `#B86E00` | novo | pressed do botão accent (no claro igual ao normal; ver 1.3) |
| `AccentTextColor` | `#F5A524` | `#8A5200` | novo | texto, ícone e contador em acento (valor selecionado, badge, ênfase) |
| `OnAccentColor` | `#1A1206` | `#1A1206` | não | texto sobre preenchimento âmbar |
| `SelectionColor` | `#2A2414` | `#FBECD2` | não | item selecionado, fundo do badge, aviso (warning) |
| `DangerColor` | `#F87171` | `#B42318` | não | erro, ação destrutiva |
| `DangerHoverColor` | `#FA8A8A` | `#C42B1C` | novo | hover do botão danger |
| `DangerPressedColor` | `#E25C5C` | `#9A1B12` | novo | pressed do botão danger |
| `OnDangerColor` | `#1F0A0A` | `#FFFFFF` | novo | texto sobre preenchimento danger |
| `DangerBgColor` | `#39292C` | `#F8E9E8` | novo | fundo do `ErrorBanner` de erro |
| `SuccessColor` | `#4ADE80` | `#0F6B33` | **claro mudou** (era `#157F3B`: 4,12:1 sobre a lateral) | sucesso |
| `SuccessBgColor` | `#21382E` | `#E8F2EB` | novo | fundo de confirmação (toast/resultado) |
| `FocusColor` | `#EEF0F2` | `#1B1D20` | novo | anel de foco |
| `MapGridColor` | `#1A1F24` | `#E2DCCF` | novo | grade do mapa (decorativa) |
| `MapCityDotColor` | `#6B7783` | `#8F8675` | novo | ponto de cidade |
| `MapCityTextColor` | `#8A949E` | `#6A624F` | novo | nome de cidade no mapa |
| `RouteGlowColor` | `#2EF5A524` (alfa 18%) | `#29B86E00` (alfa 16%) | novo | halo da rota |
| `OverlayColor` | `#E0121417` (alfa 88%) | `#EBFFFFFF` (alfa 92%) | novo | chips sobre o mapa |

Regras de uso: âmbar só para seleção, rota, ação principal e ênfase de dado (nunca decoração). Cor nunca é o único sinal: erro tem ícone e título; selecionado tem borda, fundo e check; adicionado/removido no diff tem "(vazio)" e a ordem antes → depois.

### 1.2 Contraste verificado (WCAG 2.x, calculado numericamente)

Fórmula: luminância relativa sRGB, razão `(L1+0,05)/(L2+0,05)`. Texto ≥ 4,5:1; elementos gráficos e bordas de controle ≥ 3:1. O `Tokens.dc.html` recalcula todos os pares ao vivo. **Todos os pares abaixo passam nos dois temas.**

| Par (primeiro plano / fundo) | Critério | Dark | Razão | Light | Razão |
| --- | --- | --- | --- | --- | --- |
| Text / Bg | texto ≥4.5 | #EEF0F2 / #121417 | 16.15 | #1B1D20 / #F4F2EE | 15.11 |
| Text / Side | texto ≥4.5 | #EEF0F2 / #0C0E10 | 16.93 | #1B1D20 / #EBE7DF | 13.70 |
| Text / Surface | texto ≥4.5 | #EEF0F2 / #1A1D21 | 14.80 | #1B1D20 / #FFFFFF | 16.89 |
| Text / SurfaceHover | texto ≥4.5 | #EEF0F2 / #22262B | 13.32 | #1B1D20 / #F6F3EC | 15.24 |
| Text / SurfacePressed | texto ≥4.5 | #EEF0F2 / #2A2F35 | 11.81 | #1B1D20 / #EDE8DD | 13.83 |
| Text / Selection | texto ≥4.5 | #EEF0F2 / #2A2414 | 13.51 | #1B1D20 / #FBECD2 | 14.50 |
| Text / Map | texto ≥4.5 | #EEF0F2 / #101316 | 16.31 | #1B1D20 / #EFEBE3 | 14.21 |
| Muted / Bg | texto ≥4.5 | #9AA3AD / #121417 | 7.22 | #5F6670 / #F4F2EE | 5.19 |
| Muted / Side | texto ≥4.5 | #9AA3AD / #0C0E10 | 7.57 | #5F6670 / #EBE7DF | 4.70 |
| Muted / Surface | texto ≥4.5 | #9AA3AD / #1A1D21 | 6.62 | #5F6670 / #FFFFFF | 5.80 |
| Muted / SurfaceHover | texto ≥4.5 | #9AA3AD / #22262B | 5.95 | #5F6670 / #F6F3EC | 5.23 |
| Muted / SurfacePressed | texto ≥4.5 | #9AA3AD / #2A2F35 | 5.28 | #5F6670 / #EDE8DD | 4.75 |
| Muted / Selection | texto ≥4.5 | #9AA3AD / #2A2414 | 6.04 | #5F6670 / #FBECD2 | 4.98 |
| Muted / Map | texto ≥4.5 | #9AA3AD / #101316 | 7.29 | #5F6670 / #EFEBE3 | 4.88 |
| AccentText / Bg | texto ≥4.5 | #F5A524 / #121417 | 9.04 | #8A5200 / #F4F2EE | 5.71 |
| AccentText / Side | texto ≥4.5 | #F5A524 / #0C0E10 | 9.48 | #8A5200 / #EBE7DF | 5.18 |
| AccentText / Surface | texto ≥4.5 | #F5A524 / #1A1D21 | 8.29 | #8A5200 / #FFFFFF | 6.39 |
| AccentText / Selection | texto ≥4.5 | #F5A524 / #2A2414 | 7.56 | #8A5200 / #FBECD2 | 5.48 |
| AccentText / Map | texto ≥4.5 | #F5A524 / #101316 | 9.13 | #8A5200 / #EFEBE3 | 5.37 |
| OnAccent / Accent | texto ≥4.5 | #1A1206 / #F5A524 | 9.08 | #1A1206 / #B86E00 | 4.65 |
| OnAccent / AccentHover | texto ≥4.5 | #1A1206 / #FFB84A | 10.77 | #1A1206 / #C47A0A | 5.41 |
| OnAccent / AccentPressed | texto ≥4.5 | #1A1206 / #E8991A | 7.95 | #1A1206 / #B86E00 | 4.65 |
| OnDanger / Danger | texto ≥4.5 | #1F0A0A / #F87171 | 6.86 | #FFFFFF / #B42318 | 6.57 |
| OnDanger / DangerHover | texto ≥4.5 | #1F0A0A / #FA8A8A | 8.20 | #FFFFFF / #C42B1C | 5.66 |
| OnDanger / DangerPressed | texto ≥4.5 | #1F0A0A / #E25C5C | 5.35 | #FFFFFF / #9A1B12 | 8.27 |
| Danger / Bg | texto ≥4.5 | #F87171 / #121417 | 6.67 | #B42318 / #F4F2EE | 5.88 |
| Danger / Surface | texto ≥4.5 | #F87171 / #1A1D21 | 6.11 | #B42318 / #FFFFFF | 6.57 |
| Danger / DangerBg | texto ≥4.5 | #F87171 / #39292C | 4.97 | #B42318 / #F8E9E8 | 5.58 |
| Text / DangerBg | texto ≥4.5 | #EEF0F2 / #39292C | 12.03 | #1B1D20 / #F8E9E8 | 14.33 |
| Muted / DangerBg | texto ≥4.5 | #9AA3AD / #39292C | 5.38 | #5F6670 / #F8E9E8 | 4.92 |
| Success / Bg | texto ≥4.5 | #4ADE80 / #121417 | 10.59 | #0F6B33 / #F4F2EE | 5.92 |
| Success / Surface | texto ≥4.5 | #4ADE80 / #1A1D21 | 9.71 | #0F6B33 / #FFFFFF | 6.62 |
| Success / SuccessBg | texto ≥4.5 | #4ADE80 / #21382E | 7.22 | #0F6B33 / #E8F2EB | 5.78 |
| Text / SuccessBg | texto ≥4.5 | #EEF0F2 / #21382E | 11.01 | #1B1D20 / #E8F2EB | 14.75 |
| Muted / SuccessBg | texto ≥4.5 | #9AA3AD / #21382E | 4.92 | #5F6670 / #E8F2EB | 5.06 |
| MapCityText / Map | texto ≥4.5 | #8A949E / #101316 | 6.04 | #6A624F / #EFEBE3 | 5.09 |
| Accent / Map | gráfico ≥3 | #F5A524 / #101316 | 9.13 | #B86E00 / #EFEBE3 | 3.35 |
| Accent / Bg | gráfico ≥3 | #F5A524 / #121417 | 9.04 | #B86E00 / #F4F2EE | 3.57 |
| Accent / Surface | gráfico ≥3 | #F5A524 / #1A1D21 | 8.29 | #B86E00 / #FFFFFF | 3.99 |
| BorderStrong / Bg | gráfico ≥3 | #66717D / #121417 | 3.71 | #857E6F / #F4F2EE | 3.60 |
| BorderStrong / Surface | gráfico ≥3 | #66717D / #1A1D21 | 3.40 | #857E6F / #FFFFFF | 4.03 |
| BorderStrong / Selection | gráfico ≥3 | #66717D / #2A2414 | 3.10 | #857E6F / #FBECD2 | 3.46 |
| Focus / Bg | gráfico ≥3 | #EEF0F2 / #121417 | 16.15 | #1B1D20 / #F4F2EE | 15.11 |
| Focus / Surface | gráfico ≥3 | #EEF0F2 / #1A1D21 | 14.80 | #1B1D20 / #FFFFFF | 16.89 |
| Focus / Side | gráfico ≥3 | #EEF0F2 / #0C0E10 | 16.93 | #1B1D20 / #EBE7DF | 13.70 |
| MapCityDot / Map | gráfico ≥3 | #6B7783 / #101316 | 4.08 | #8F8675 / #EFEBE3 | 3.03 |
| Danger / Surface | gráfico ≥3 | #F87171 / #1A1D21 | 6.11 | #B42318 / #FFFFFF | 6.57 |
| Success / Surface | gráfico ≥3 | #4ADE80 / #1A1D21 | 9.71 | #0F6B33 / #FFFFFF | 6.62 |

Pares que **falhavam** na tabela original da skill e como foram resolvidos:
| Par | Claro original | Correção |
| --- | --- | --- |
| `AccentColor` `#B86E00` como texto sobre Bg | 3,57:1 | texto usa `AccentTextColor` `#8A5200` (5,71:1) |
| `AccentColor` como texto sobre Side / Map / Selection | 3,23 / 3,35 / 3,42 | idem (5,18 / 5,37 / 5,48) |
| `SuccessColor` `#157F3B` sobre Side / Map / Selection | 4,12 / 4,28 / 4,36 | `#0F6B33` (≥ 5,37) |
| `LineColor` como borda de campo | 1,33–1,49 | campos usam `BorderStrongColor` (≥ 3,40) |
| `OnAccent` sobre acento escurecido (pressed claro) | 3,96 (`#A86400`) | pressed não escurece (ver 1.3) |

Isentos de contraste: texto desabilitado (opacidade 0,4), grade e ponto de fundo do mapa decorativos (o ponto de cidade ainda tem ≥ 3:1), divisores `LineColor`.

### 1.3 Estados dos botões (cores)

| Classe | Normal | Hover | Pressed | Texto | Borda |
| --- | --- | --- | --- | --- | --- |
| `accent` | `Accent` | `AccentHover` | `AccentPressed` + `scale(0.98)` + borda `AccentText` | `OnAccent` | transparente |
| `danger` | `Danger` | `DangerHover` | `DangerPressed` + `scale(0.98)` | `OnDanger` | transparente |
| `neutral` (padrão) | `Surface` | `SurfaceHover` | `SurfacePressed` + `scale(0.98)` | `Text` | `BorderStrong` 1 px |

No tema claro, escurecer o âmbar no pressed derrubaria o contraste do texto (3,96:1 com `#A86400`), por isso pressed claro reaproveita o preenchimento e sinaliza por escala e borda. `scale(0.98)` já vem do Fluent: não somar outro (skill `avalonia-polish`).

### 1.4 Tipografia

| Token | Valor | Uso |
| --- | --- | --- |
| `FontUi` | Barlow (embutida), pesos 400/500/600 | corpo e controles |
| `FontDisplay` | Barlow Condensed (embutida), pesos 500/600/700 | números, títulos em caixa alta |

| Estilo | Fonte / peso | Tamanho / altura de linha | Notas |
| --- | --- | --- | --- |
| `Caption` | Ui 500 | **12 / 16** | caixa alta, `LetterSpacing` 0,08 em, `TextMuted` |
| `BodySm` | Ui 400 | 13 / 18 | linha secundária, ajuda |
| `Body` | Ui 400 e 600 | 14 / 20 | padrão |
| `Subtitle` | Ui 600 | 16 / 22 | título de cartão |
| `Metric` | Display 600 | 22 / 26 | `+tnum` |
| `Title` | Display 700 | 26 / 30 | caixa alta, `LetterSpacing` 0,03 em |
| `Hero` | Display 700 | 34 / 40 | `+tnum`, cor `AccentText` (km no mapa) |

Números sempre `FontFeatures="+tnum"`, formatados pela cultura atual (pt-BR `1.234 km`, en `1,234 km`) via chave `Format_Km` (`"{0:N0} km"`).

### 1.5 Espaço, raio, movimento

| Grupo | Tokens |
| --- | --- |
| Espaço | `Space1 2`, `Space2 4`, `Space3 8`, `Space4 12`, `Space5 16`, `Space6 20`, `Space7 24`, `Space8 32` |
| Raio | `RadiusSm 8` (campos, chips), `RadiusMd 10` (botões), `RadiusLg 12` (cartões), `RadiusXl 16` (mapa), `RadiusPill 999` (badge) |
| Elevação | borda `LineColor`; `ShadowPopup` (`0 8 24 0 #80000000` escuro / `#24000000` claro + `0 1 2 0 #33000000`) só em flyouts e diálogos |
| Movimento | `DurFast 150 ms`, `DurBase 200 ms`, `EaseOut` (`CubicEaseOut`); só `Opacity`, `RenderTransform`, pincéis; nunca `Width/Height/Margin` |

Raio concêntrico: externo = interno + padding (ex.: popup `RadiusLg` 12 com item `RadiusSm` 8 e padding 4).

### 1.6 Foco, área de clique, desabilitado (valem para todos os controles)

- **Foco visível** (`:focus-visible`, só por teclado): anel de 2 px `FocusColor` + 2 px de folga na cor do fundo. Implementação sugerida: `Border#FocusRing` sobreposto (`Margin=-4`, `BorderThickness=2`, `CornerRadius` = raio + 4, `IsHitTestVisible=false`). Em item de lista virtualizada e em qualquer controle cortado por contêiner, o anel é **interno** (`Margin=0`, 2 px dentro da borda).
- **Área de clique**: mínimo 40 × 40; padrão 44; ação principal 48; item de lista ≥ 64 de altura; ícone de 16 px dentro de alvo de 40. Alvos vizinhos separados por ≥ `Space2` (4). Fundo `Transparent` (não `null`) para o hit-test pegar a área toda.
- **Desabilitado**: `Opacity 0,4`, sem hover/pressed, `Cursor` padrão. O motivo aparece em `ToolTip` e/ou texto de apoio (ex.: "Revisar" sem mudanças: "Nada para revisar").
- **Hover/pressed**: troca de pincel em 150 ms (`BrushTransition` em `ContentPresenter#PART_ContentPresenter`, ref. `avalonia-polish`).
- **Texto longo**: rótulos curtos `NoWrap` + `TextTrimming=CharacterEllipsis` + `ToolTip.Tip` com o texto inteiro; mensagens `Wrap` com `MaxWidth`; caminhos em mono sem quebrar no meio de palavra. Nomes de mods e cidades chegam longos; en costuma ser ~30% mais curto em verbos e mais longo em substantivos compostos: reservar folga de 30%.
- **Acessibilidade**: `AutomationProperties.Name` em todo controle só com ícone; `HeadingLevel` em títulos; `LiveSetting` em contadores e estados assíncronos; ordem de Tab = ordem visual; contraste dos dois temas (seção 1.2). Textos de Name vêm de chaves `.resx`.
- **Cultura**: todo número recebido já formatado; trocar idioma atualiza sem reiniciar (`CultureChangedMessage` → `OnPropertyChanged("")`).

---

## 2. Controles

Cada bloco: para quê, propriedades, estados, área de clique, acessibilidade, textos longos/en, amostras (`DesignSample`) e chaves. Imagens: Galeria.

### 2.1 Botões `accent` / `danger` / `neutral`

Não é um controle novo: **classes de estilo** do `Button` (arquivo `Styles/Buttons.axaml`).

| Classe | Quando | Exemplos |
| --- | --- | --- |
| `accent` | **uma** ação principal por tela | Revisar mudanças, Aplicar mudanças (no diálogo não destrutivo) |
| `danger` | ação destrutiva ou irreversível | Gravar no save (confirmação), Descartar tudo |
| `neutral` (padrão, sem classe) | demais ações | Cancelar, Editar, Tentar de novo |
| `icon` (combina com qualquer) | só ícone | Atualizar, Fechar |

| Propriedade | Tipo | Padrão | Notas |
| --- | --- | --- | --- |
| `Content` | `object` | — | texto em chave `.resx`; verbo + objeto, sem ponto final |
| `Command` / `CommandParameter` | `ICommand?` / `object?` | — | |
| `IsEnabled` | `bool` | `true` | desabilitado explica por `ToolTip` |
| `Classes` | `accent` \| `danger` \| `icon` | neutral | |
| `Tag` (opcional) | `object?` | — | conteúdo extra (ex.: `PendingBadge` dentro do botão Revisar) |

Tamanhos: padrão `MinHeight 44`, compacto (`Classes="compact"`, dentro de cartões e cabeçalho de grupo) `MinHeight 40`, primário `Classes="large"` `MinHeight 48`. `MinWidth` = altura. Padding horizontal 18 (compacto 14). Raio `RadiusMd`. Texto Ui 600 14, `LetterSpacing` 0,03 em. Ícone de 16 px + gap 8 quando houver.

Estados: normal, hover, pressed, `:focus-visible`, disabled (cores em 1.3). Dado: `IsBusy` (opcional, `ProgressRing` 14 px no lugar do ícone, botão continua desabilitado) usado em "Aplicando…".
Área de clique: 40/44/48 conforme tamanho; ícone 40 × 40 (`icon` compact) ou 44 × 44.
Acessibilidade: `Name` = texto do conteúdo; botão `icon` exige `AutomationProperties.Name`; `AccessKey` não usado. Atalhos ficam no shell (Ctrl+S revisar, Ctrl+Enter aplicar) e aparecem no `ToolTip`.
Texto longo: não quebra; `TextTrimming` + `ToolTip`; botão nunca abaixo de `MinWidth`. En geralmente mais curto ("Revisar mudanças" → "Review changes").
Amostras: `Buttons/{accent,danger,neutral}/{normal,hover,pressed,focus,disabled}`, `Buttons/Icon`, `Buttons/LongText`.

### 2.2 PageHeader

Título do módulo, eyebrow (perfil/save), contagem e ações. Não usar para cabeçalho de seção interna (use `SectionCard`).

```
PERFIL 1 · SAVE 4                                     6.485   [ Revisar mudanças (3) ]
FRETES
```

| Propriedade | Tipo | Padrão | Notas |
| --- | --- | --- | --- |
| `Eyebrow` | `string?` | `null` | `Caption`; oculto se vazio |
| `Title` | `string` | `""` | `Title` condensed caixa alta, `HeadingLevel 1` |
| `Count` | `string?` | `null` | `Metric`, `TextMuted`, alinhado à direita, `+tnum`; texto já formatado |
| `CountLabel` | `string?` | `null` | nome acessível da contagem ("6.485 ofertas"); sem ele lê só o número |
| `Actions` | `object?` (conteúdo) | `null` | à direita da contagem; geralmente botão accent |

Estados: normal; sem eyebrow; sem contagem; sem ações; carregando (`Count` = `null`, mantém altura, sem "0"); texto longo.
Área de clique: o cabeçalho não é clicável; as ações seguem 2.1.
Acessibilidade: `Title` com `AutomationProperties.HeadingLevel=Level1`; `Count` `LiveSetting=Polite` ao mudar (busca filtrando).
Texto longo: `Eyebrow` e `Title` `NoWrap` + reticências (`Title` com ToolTip); `Count` e `Actions` nunca encolhem (`MinWidth` pelo conteúdo). Altura fixa 56 (eyebrow 16 + título 30 + folga).
Amostras: `PageHeader/Default`, `/WithActions`, `/LongText`, `/Loading`.

### 2.3 EmptyState

Lista ou área sem conteúdo, com **motivo** e, quando possível, ação. Não usar para erro (`ErrorBanner`).

```
        [ícone 48]
   Nenhuma oferta neste save
   Abra o mercado de fretes no jogo,
   salve e tente de novo.
        [ Recarregar save ]
```

| Propriedade | Tipo | Padrão | Notas |
| --- | --- | --- | --- |
| `Icon` | `Geometry?` | `null` | 24 px, em quadrado 48 `SelectionColor`, ícone `AccentText`; decorativo (`IsAccessible=false`) |
| `Title` | `string` | — | 16/22, peso 600, centralizado |
| `Message` | `string?` | `null` | `BodySm` `TextMuted`, centralizado, `MaxWidth 280`, quebra |
| `ActionText` | `string?` | `null` | botão neutro; some se `null` |
| `ActionCommand` | `ICommand?` | `null` | |
| `IsCompact` | `bool` | `false` | compacto: sem ícone, padding 12, para dentro de cartão/popup |

Estados: com e sem ação; busca sem resultado (mensagem cita o termo: `Nenhum resultado para “{0}”`, ação "Limpar busca"); compacto.
Área de clique: o botão 44 px; o resto não é interativo.
Acessibilidade: o contêiner é `Group` com `Name = Title`; ao surgir depois de uma busca, `LiveSetting=Polite` anuncia título + mensagem. O botão recebe foco só por Tab (não rouba foco).
Texto longo: `Title` quebra em até 2 linhas e depois reticências; `Message` até 4 linhas; termo de busca longo é truncado a 40 caracteres com `…` na mensagem.
Amostras: `EmptyState/NoOffers`, `/NoResults`, `/Compact`.

### 2.4 ErrorBanner

Faixa que diz **o que aconteceu + o que fazer**. Substitui `ErrorState` da skill. Nunca mostra código nem termo do formato SII; detalhe técnico (caminho, linha) só em "Detalhes".

```
(x) Não foi possível gravar                          [ Tentar de novo ]  x
    O jogo está aberto. Feche o jogo e tente de novo.
```

| Propriedade | Tipo | Padrão | Notas |
| --- | --- | --- | --- |
| `Severity` | `BannerSeverity` (`Error`, `Warning`, `Info`) | `Error` | cor, ícone e `LiveSetting` |
| `Layout` | `BannerLayout` (`Inline`, `Block`) | `Inline` | `Block` ocupa a página (centralizado, ícone 48) quando o conteúdo não pode ser mostrado |
| `Title` | `string` | — | peso 600 |
| `Message` | `string` | — | `BodySm`, quebra, sem `MaxWidth` no Inline |
| `Details` | `string?` | `null` | mono 12, `TextMuted`, selecionável; colapsado com link "Detalhes" |
| `ActionText` / `ActionCommand` | `string?` / `ICommand?` | `null` | botão neutro compacto à direita; no Block abaixo do texto |
| `DismissCommand` | `ICommand?` | `null` | se definido, mostra "×" 40 × 40 |

Cores: Error = `DangerBg` + borda `Danger` + ícone `Danger`; Warning = `Selection` + borda `AccentText` + ícone `AccentText`; Info = `Surface` + borda `BorderStrong` + ícone `TextMuted`. Texto sempre `Text` (≥ 12:1). Ícones distintos por severidade (x em círculo, triângulo, i em círculo).
Estados: normal; com/sem ação; com/sem dispensar; com detalhes (colapsado/expandido); `Block`; ação em andamento (botão `IsBusy`).
Área de clique: ação 40 px de altura, dispensar 40 × 40, link Detalhes 40 px de altura.
Acessibilidade: `AutomationProperties.LiveSetting` `Assertive` para Error e `Polite` para Warning/Info; `Name` = `"{severidade}: {Title}"`; ao aparecer por falha de ação do usuário, o foco vai para a ação (ou fica onde está se não houver). Esc dispensa quando há `DismissCommand`.
Texto longo: `Title` até 2 linhas; `Message` quebra livre; `Details` quebra em qualquer caractere (caminhos). Ação nunca quebra (`NoWrap`, reticências) e em largura < 480 vai para baixo do texto.
Amostras: `ErrorBanner/{Error,Warning,Info,WithDetails,Block}`.

### 2.4b Mapeamento de erros
O Core devolve `CoreError`; a App mostra `ErrorBanner` com `Title`/`Message` por chaves `Error_<Code>_Title`/`Error_<Code>_Message` (definidas em UX-2). Este controle não conhece códigos.

### 2.5 LoadingPanel

Carregando com texto. Substitui `LoadingState`. Só aparece depois de 300 ms (evita piscar).

| Propriedade | Tipo | Padrão | Notas |
| --- | --- | --- | --- |
| `Text` | `string` | — | 15/20 peso 600, ex.: "Lendo save…" |
| `Progress` | `double?` | `null` | 0–1; `null` = indeterminado |
| `Detail` | `string?` | `null` | `BodySm` `tnum`, ex.: "4.038 de 6.485 ofertas" (já formatado) |
| `CancelCommand` | `ICommand?` | `null` | botão neutro "Cancelar" |
| `ShowDelay` | `TimeSpan` | `300 ms` | tempo até aparecer |
| `IsCompact` | `bool` | `false` | para dentro de cartão/botão |

Visual: indeterminado = anel de 28 px (`Accent` sobre `Line`, 900 ms linear); determinado = barra de 6 px de altura, largura máx. 240, `Accent` sobre `Line`, raio 3; transição do valor 200 ms.
Estados: indeterminado; determinado; com cancelar; cancelando (botão desabilitado, texto "Cancelando…").
Área de clique: cancelar 44.
Acessibilidade: `role status`; `LiveSetting=Polite` no `Text`; a barra é `ProgressBar` com `Name = Text` e `Value` em %; o anúncio de progresso só a cada 10%.
Texto longo: `Text` até 2 linhas centralizadas; `Detail` 1 linha com reticências.
Amostras: `LoadingPanel/{Indeterminate,Progress,Cancelable}`.

### 2.6 SectionCard

Agrupa campos relacionados. Borda, sem sombra.

| Propriedade | Tipo | Padrão | Notas |
| --- | --- | --- | --- |
| `Title` | `string?` | `null` | `Subtitle` 16/22, `HeadingLevel 2` |
| `Description` | `string?` | `null` | `BodySm` `TextMuted`, quebra |
| `HeaderActions` | `object?` | `null` | à direita do título (botão compacto) |
| `Content` | `object?` | — | corpo; `Spacing` 8 entre filhos |
| `Padding` | `Thickness` | `16` | |

Visual: `Surface`, borda `Line` 1 px, `RadiusLg`. Estados: normal; sem título (só conteúdo); com ações; conteúdo vazio (use `EmptyState` compacto dentro).
Área de clique: o cartão não é clicável.
Acessibilidade: `Group` com `Name = Title`; ordem de Tab segue o conteúdo.
Texto longo: `Title` 1 linha + reticências + ToolTip; `Description` quebra; o cartão cresce em altura, nunca corta.
Amostras: `SectionCard/{Default,WithActions,LongTitle,Empty}`.

### 2.7 StatTile

Número grande + rótulo. Não usar para texto longo.

| Propriedade | Tipo | Padrão | Notas |
| --- | --- | --- | --- |
| `Label` | `string` | — | `Caption` caixa alta |
| `Value` | `string?` | `null` | `Metric` 22 `+tnum`, já formatado; `null` → "—" em `TextMuted` |
| `Emphasis` | `StatEmphasis` (`None`, `Accent`, `Danger`, `Success`) | `None` | cor do valor: `Text`, `AccentText`, `Danger`, `Success` |
| `IsLoading` | `bool` | `false` | barra-esqueleto 70% × 22 `Line` no lugar do valor |
| `Hint` | `string?` | `null` | `ToolTip` explicando o valor |

Visual: `Surface`, borda `Line`, `RadiusLg`, padding 12 × 14, mínimo 96 de largura × 64 de altura.
Estados: normal; ênfase; desconhecido ("—"); carregando; texto longo.
Área de clique: não interativo; não é parada de Tab.
Acessibilidade: `Name` composto `"{Label}: {Value}"` (`Stat_A11y`); desconhecido lê "{Label}: sem valor".
Texto longo: `Value` 1 linha + reticências + ToolTip com o valor inteiro; `Label` 1 linha + reticências. Grade de 4 colunas colapsa para 2 abaixo de 560 px.
Amostras: `StatTile/{Normal,Accent,Unknown,Loading,LongValue}`.

### 2.8 ValueChange (antes → depois)

Mostra uma alteração de valor. Núcleo do `DiffList`.

| Propriedade | Tipo | Padrão | Notas |
| --- | --- | --- | --- |
| `Before` | `string?` | `null` | `null`/vazio → "(vazio)" itálico `TextMuted` |
| `After` | `string?` | `null` | idem |
| `IsNumeric` | `bool` | `false` | `+tnum`; alinha à direita em colunas |
| `Layout` | `ValueChangeLayout` (`Auto`, `Horizontal`, `Vertical`) | `Auto` | `Auto` vira vertical se não couber em 1 linha |
| `Label` | `string?` | `null` | só para o nome acessível |

Visual horizontal: `Antes` (`TextMuted`, peso 400) + seta `→` 18 × 12 (`TextMuted`) + `Depois` (`Text`, peso 600). Vertical: antes, seta girada 90°, depois, alinhados à esquerda. **Sem tachado** (prejudica leitura); a distinção é ordem + peso + seta, não só cor.
Estados: alterado; adicionado (`Before` vazio); removido (`After` vazio); numérico; vertical; texto longo.
Área de clique: não interativo (texto selecionável por cópia).
Acessibilidade: o conjunto é um único elemento `Name = "{Label}: de {Before} para {After}"` (`Diff_Change_A11y`); a seta é decorativa (`IsAccessible=false`).
Texto longo: horizontal usa reticências em cada lado com `ToolTip`; no `Auto`, vertical quebra em até 3 linhas por valor, depois reticências.
Amostras: `ValueChange/{Number,Text,Added,Removed,Vertical}`.

### 2.9 DiffList

Lista agrupada de mudanças pendentes (única tela de revisão, no shell). Substitui `ChangeReviewList`.

```
FRETES  Radus · Sofia → LogNStick · Istanbul                  [ Descartar ]
  Destino      LogNStick · Istanbul  →  Tradeaux · Praha
  Distância    569 km  →  612 km
  Carga        Vigas de concreto  →  Maquinário
PERFIL  Dinheiro e experiência                                [ Descartar ]
  Dinheiro     € 120.450  →  € 250.000
```

| Propriedade | Tipo | Padrão | Notas |
| --- | --- | --- | --- |
| `Groups` | `IEnumerable<IDiffGroup>` | vazio | grupo = um `ChangeSet`: `ModuleName`, `Title`, `Lines`, `DiscardCommand` |
| `IDiffLine` | `Label`, `Before`, `After`, `IsNumeric` | — | já traduzidos/formatados pelo `ILocalizer` (`DiffValue`) |
| `EmptyContent` | `object?` | `EmptyState` padrão | mostrado sem grupos |
| `IsDiscardEnabled` | `bool` | `true` | falso durante a aplicação |

Visual: cartão por grupo; cabeçalho (`Side`) com módulo (`Caption`), título (`Subtitle`) e botão "Descartar" neutro compacto; linhas `Surface` com coluna de rótulo de 130–160 px (`BodySm` `TextMuted`) e `ValueChange`; divisor `Line`. Linha em hover: `SurfaceHover`. Virtualizada (`ItemsRepeater`/`VirtualizingStackPanel`); até algumas centenas de linhas.
Estados: com grupos; vazio (`EmptyState` "Nenhuma mudança pendente"); descartando (botão do grupo `IsBusy`); aplicando (todos os "Descartar" desabilitados); grupo com muitas linhas (rolagem da página, não do grupo).
Área de clique: "Descartar" 40 px (compacto). Linhas não clicáveis.
Acessibilidade: cada grupo é `Group` com `Name = Title`; "Descartar" tem `Name = "Descartar mudança: {Title}"`; Tab percorre só os botões de descartar; leitor de tela lê cada linha como "Rótulo: de X para Y". Após descartar, o foco vai para o próximo botão (ou para o cabeçalho da lista se for o último).
Texto longo: título de grupo 1 linha + reticências + ToolTip; rótulo 1 linha + reticências; valores seguem `ValueChange` (vertical `Auto`). Largura mínima 480; abaixo disso a coluna de rótulo vai para cima do valor.
Amostras: `DiffList/{Many,Single,Empty,Discarding}`.

### 2.10 PendingBadge

Selo de pendência. Três variantes.

| Propriedade | Tipo | Padrão | Notas |
| --- | --- | --- | --- |
| `Variant` | `BadgeVariant` (`Count`, `Dot`, `Tag`) | `Count` | |
| `Count` | `int` | `0` | `Count`: oculto quando 0 (salvo `ShowZero`); 100+ vira "99+" |
| `Text` | `string?` | `null` | `Tag`: texto curto ("Modificado") |
| `ShowZero` | `bool` | `false` | |
| `AccessibleText` | `string?` | derivado | nome acessível; `Count` usa `Pending_Count_*` |

Visual: pílula `RadiusPill`, altura 20, `MinWidth 20`, padding 6, fundo `Selection`, borda 1 px `AccentText`, texto `AccentText` (Display 700 13 `+tnum`; `Tag` Ui 500 11). `Dot` 10 px `Accent` com margem 5 (alvo visual, não clicável). Dentro de botão accent: fundo `OnAccent` e texto `Accent`/`#FFD9A0`.
Estados: normal; mudança de contagem (anima `scale` 1 → 1,15 → 1, 150 ms, só depois de carregado); 0 oculto; estouro.
Área de clique: não interativo; quando dentro de "Revisar (n)", o alvo é o botão (48).
Acessibilidade: `Name` = "3 mudanças pendentes" (plural); `LiveSetting=Polite` ao mudar; `Dot` e `Tag` têm `Name` "Modificado"; não depende só da cor (borda + texto/forma).
Texto longo: `Tag` máx. 14 caracteres; excedente com reticências + ToolTip. Em en, "Modified" é mais curto que "Modificado".
Amostras: `PendingBadge/{Count,TwoDigits,Overflow,Tag,Dot,Zero,InButton}`.

### 2.11 SearchBox (com debounce)

| Propriedade | Tipo | Padrão | Notas |
| --- | --- | --- | --- |
| `Text` | `string` | `""` | TwoWay, atualiza a cada tecla (o campo mostra) |
| `Query` | `string` | `""` | TwoWay; **valor com debounce**: o que o ViewModel usa para filtrar |
| `DebounceMs` | `int` | `250` | 0 = sem debounce |
| `MinLength` | `int` | `0` | abaixo disso `Query` fica `""` |
| `Watermark` | `string` | — | chave; ex.: "Cidade, empresa ou carga…" |
| `IsBusy` | `bool` | `false` | spinner de 14 px antes do "×" |
| `ResultCount` | `int?` | `null` | alimenta o anúncio "N resultados" |
| `ClearCommand` | interno | — | "×" e `Esc` |

Comportamento: `Query` = `Text` aparado, 250 ms depois da última tecla (cada tecla reinicia o contador); `Enter` publica imediatamente e cancela o contador; `Esc` com texto limpa e publica na hora, sem texto deixa o evento subir; texto igual ao `Query` não republica. Sem filtragem na UI: o VM filtra (listas de milhares, buscar por prefixo/diacrítico é do VM).
Visual: altura 44, `Surface`, borda `BorderStrong` 1 px, `RadiusMd`, lupa 16 px `TextMuted` à esquerda, "×" 36 × 36 (alvo 40 com margem) à direita só com texto.
Estados: vazio; hover (`SurfaceHover`); foco (`:focus-visible` anel); com texto + "×"; ocupado; desabilitado (0,4).
Área de clique: campo inteiro 44; "×" 40 × 40.
Acessibilidade: `Name` = `Watermark` (ou `Label`); "×" `Name` "Limpar busca"; `ResultCount` muda → `LiveSetting=Polite` "12 resultados" / "Nenhum resultado" (debounce de 500 ms para não falar a cada tecla); `Role` search.
Texto longo: o texto digitado rola horizontalmente (nunca quebra); `Watermark` com reticências.
Amostras: `SearchBox/{Empty,Focus,Filled,Busy,Disabled,Hover}`.

### 2.12 SearchablePicker

Escolher **uma** entre milhares (cidade, empresa, carga, reboque), com busca e lista virtualizada. Menos de 10 itens: `ComboBox`.

| Propriedade | Tipo | Padrão | Notas |
| --- | --- | --- | --- |
| `Label` | `string` | — | rótulo `Caption` no campo ("Origem") |
| `Placeholder` | `string` | — | "Escolher origem…" |
| `Items` | `IEnumerable<PickerOption>` | vazio | **já filtrado** pelo VM; virtualizado |
| `SelectedItem` | `PickerOption?` | `null` | TwoWay |
| `Query` | `string` | `""` | TwoWay; SearchBox interno com debounce (2.11) |
| `TotalCount` | `int?` | `null` | mostra "3 de 1.842" |
| `IsLoading` | `bool` | `false` | lista mostra `LoadingPanel` compacto |
| `AllowClear` | `bool` | `false` | "×" no campo fechado |
| `IsRequired` / `ErrorText` | `bool` / `string?` | `false` / `null` | borda e texto `Danger` abaixo |
| `EmptyText` | `string?` | padrão | `EmptyState` compacto sem resultados |
| `IsDropDownOpen` | `bool` | `false` | TwoWay |

`PickerOption`: `Key` (`object`), `Primary` (`string`), `Secondary` (`string?`), `Tag` (`string?`, ex.: "Mod"), `IsEnabled` (`bool`).
Campo fechado: altura mínima 56 (como os campos do mockup A+C), `Surface`, borda `BorderStrong`, `RadiusMd`; rótulo `Caption` em cima, valor 14/600 embaixo (ou placeholder `TextMuted`), chevron 16 px à direita. Selecionado: borda `AccentText`. Popup: largura = max(campo, 360), altura máx. 360, `Surface`, borda `Line`, `ShadowPopup`, raio 12, padding 6; no topo o `SearchBox` (foco automático) e a linha de contagem (`Caption` sem caixa alta); abaixo, a lista com itens de 44 de altura, `RadiusSm`.
Item: `Primary` 14/600 (1 linha) e `Secondary` `BodySm` `TextMuted` (1 linha), `Tag` pílula `BorderStrong` 11; selecionado: `Selection` + borda `AccentText` + "✓" `AccentText` + `Primary` em `AccentText`; hover/realce de teclado `SurfaceHover`.
Estados: fechado vazio; fechado selecionado; fechado com erro; foco; desabilitado; aberto com resultados; aberto carregando; aberto sem resultados; aberto vazio (sem itens).
Teclado: `Enter`/`Espaço`/`Alt+↓` abre; digitar abre e já busca; `↑`/`↓` movem o realce **mesmo com o foco na busca**; `PageUp/PageDown` pulam 8; `Home/End` primeiro/último; `Enter` seleciona e fecha; `Esc` fecha e devolve o foco ao campo; `Tab` fecha sem selecionar.
Área de clique: campo fechado 56 × largura toda; item 44; "×" 40.
Acessibilidade: campo `Name` = "{Label}: {valor ou 'Nenhum selecionado'}"; `AutomationProperties.ExpandCollapseState`; lista `Name` = `Label`, itens com `SelectionItem`; mudança de `TotalCount`/resultados anunciada (`Polite`, debounce 500 ms); `IsRequired` → `Name` inclui "obrigatório".
Texto longo: `Primary` e `Secondary` com reticências e `ToolTip` com os dois textos; campo fechado com reticências; o popup nunca passa de 480 px de largura.
Amostras: `Picker/{ClosedEmpty,ClosedSelected,Error,Focus,Disabled,Open,OpenLoading,OpenNoResults,LongNames}`.

### 2.13 RouteOfferItem

Item de lista "Origem ⋯ Destino · km" + carga e reboque. Vai dentro de `ListBoxItem` (a lista cuida de seleção e setas).

| Propriedade | Tipo | Padrão | Notas |
| --- | --- | --- | --- |
| `Origin` / `Destination` | `string` | — | nome legível da cidade (o VM deriva do id) |
| `OriginCompany` / `DestinationCompany` | `string?` | `null` | só no `ToolTip` e no nome acessível |
| `DistanceText` | `string` | — | já formatado ("569 km"); `Metric` 19 condensed `+tnum`, à direita |
| `CargoText` | `string` | — | |
| `TrailerText` | `string?` | `null` | "Carga · Reboque" na 2ª linha |
| `IsSelected` | `bool` | `false` | pseudoclasse `:selected` (vem do `ListBoxItem`) |
| `IsModified` | `bool` | `false` | `PendingBadge` Tag "Modificado" |
| `StatusText` | `string?` | `null` | etiqueta de dado ("Expirada"); item a 70% de opacidade |

Visual: altura mín. 64, padding 10 × 14, `RadiusLg`, 2 linhas. Linha 1: origem 14/600, `⋯` tracejado 22 px `TextMuted`, destino 14/600, km à direita; linha 2: carga · reboque `BodySm` `TextMuted` + etiqueta à direita. Origem e destino têm `MaxWidth 38%` cada.
Estados: normal (`Surface`, borda `Line`); hover (`SurfaceHover`); pressed (`SurfacePressed`); selecionado (`Selection`, borda `Accent`, km em `AccentText`); `:focus-visible` (anel **interno** 2 px); modificado; expirada; texto longo; desabilitado (oferta de outra fonte, 0,4).
Área de clique: o card inteiro, 100% da largura × ≥ 64.
Acessibilidade: `Name` = "{Origin} ({OriginCompany}) para {Destination} ({DestinationCompany}), {Distance}, {Cargo}, {Trailer}" (`Offer_A11y`); `ListBoxItem` expõe selecionado; modificado e expirada entram no nome ("modificada", "expirada"); `Enter` seleciona (lista).
Texto longo: cidades com reticências (cada uma até 38%), linha 2 com reticências; `ToolTip` completo "Empresa · Cidade → Empresa · Cidade". Abaixo de 280 px de largura a seta some. Em en as palavras da linha 2 costumam ser mais curtas ("Vigas de concreto" → "Concrete beams").
Amostras: `RouteOffer/{Normal,Hover,Selected,Modified,Expired,LongText,Focus}`.

### 2.14 RouteMap

Mapa esquemático origem → destino, e geográfico quando houver coordenadas (premissa em `design/direcao-visual/README.md`).

| Propriedade | Tipo | Padrão | Notas |
| --- | --- | --- | --- |
| `Origin` / `Destination` | `MapPoint?` | `null` | `MapPoint`: `Label` (`string`), `Longitude`/`Latitude` (`double?`) |
| `Cities` | `IEnumerable<MapPoint>?` | `null` | pontos de fundo; só no geográfico |
| `DistanceText` | `string?` | `null` | `Hero` 34 `AccentText` + "km" no canto inferior direito |
| `RouteLabel` | `string?` | `null` | chip superior esquerdo ("Radus · Sofia → LogNStick · Istanbul") |
| `Mode` | `RouteMapMode` (`Auto`, `Schematic`, `Geographic`) | `Auto` | `Auto` = geográfico se origem e destino têm coordenadas |
| `IsLoading` | `bool` | `false` | `LoadingPanel` compacto ("Carregando mapa…") |
| `MinHeight` | `double` | `220` | padrão 340 no módulo Fretes |

Visual: contêiner `Map`, borda `Line`, `RadiusXl`, `ClipToBounds`; grade `MapGrid` (decorativa); rota = curva quadrática: halo `RouteGlow` 12 px + linha `Accent` 4 px; origem = círculo vazado (raio 9, contorno `Accent` 4); destino = círculo cheio (raio 10); rótulos dos nós 15/600 `Text`; cidades de fundo ponto 3 px `MapCityDot` + nome 13 `MapCityText`. Chips sobre o mapa: `Overlay`, borda `Line`, `RadiusMd`.
Estados: esquemático; geográfico; vazio ("Escolha uma oferta", no centro); carregando; origem = destino ("Origem e destino na mesma cidade": um nó só); sem distância (esconde o bloco km).
Área de clique: não interativo na v1 (sem zoom/arrasto); nós têm `ToolTip` com o nome completo.
Acessibilidade: o mapa é uma `Image`/`Group` com `Name` = "Mapa da rota de {A} até {B}, {distância}" (`Route_A11y`); cidades de fundo e grade são decorativas e não expostas; vazio → `Name` "Mapa da rota: nenhuma oferta escolhida". Contraste: rota 3,35:1 (claro) / 9,13:1 (escuro) sobre o mapa; rótulos ≥ 5,09:1.
Texto longo: chip da rota com reticências (máx. 62% da largura) e `ToolTip`; rótulos dos nós quebram em 2 linhas de até 160 px; sobreposição de rótulos: o do destino fica acima do nó, o da origem abaixo.
Amostras: `RouteMap/{Schematic,Geographic,Empty,Loading,SameCity,LongLabels}`.

---

## 3. Galeria (Debug)

`Views/Debug/GalleryView` (só em Debug; `Gallery/**` excluído do Release, ADR 0005). Mockup: [`canvas/Galeria.dc.html`](canvas/Galeria.dc.html).

### 3.1 Layout

```
┌─ Galeria de controles ── [Lado a lado | Claro | Escuro] [Textos longos] [Áreas de clique] [PT|EN] ┐
│ ┌─────────── LIGHT ──────────┐  ┌─────────── DARK ───────────┐                                  │
│ │ BOTÕES                      │  │ BOTÕES                      │   uma seção por controle,       │
│ │  [accent: normal hover …]   │  │  [accent: normal hover …]   │   na ordem: Botões, PageHeader, │
│ │ PAGEHEADER …                │  │ PAGEHEADER …                │   SearchBox, SearchablePicker,  │
│ │ …                           │  │ …                           │   RouteOfferItem, RouteMap,     │
│ └─────────────────────────────┘  └─────────────────────────────┘   StatTile, SectionCard,        │
│                                                                    ValueChange, DiffList,          │
│                                                                    PendingBadge, EmptyState,       │
│                                                                    ErrorBanner, LoadingPanel       │
└────────────────────────────────────────────────────────────────────────────────────────────────┘
```
- **Duas colunas iguais** (Light à esquerda, Dark à direita), cada uma um `Border` com `RequestedThemeVariant` fixo e fundo `Bg`; assim `DynamicResource` resolve para o tema da coluna independente do tema do app. Conteúdo idêntico nas duas.
- **Barra superior**: segmentado Lado a lado / Claro / Escuro (mostra uma coluna só em largura total), "Textos longos" (troca amostras por nomes de mod/cidade longos), "Áreas de clique" (contorno tracejado em todo alvo interativo, para checar 40/44 px), alternância PT/EN (troca `Localizer.Language` em runtime, afeta as duas colunas).
- **Cada seção**: título do controle (nome da classe), uma linha de descrição, os estados em grade com legenda `Caption` embaixo de cada exemplo (nome do estado). Estados de ponteiro e foco são **forçados** por helper anexado `Gallery.ForceState="Hover|Pressed|Focus|Disabled"` que liga as pseudo-classes (`:pointerover`, `:pressed`, `:focus-visible`), pois não dá para segurar o mouse numa captura.
- **Tamanho**: 1280 de largura, altura livre (rolagem vertical); cerca de 5.400 px com as duas colunas. Coluna ≈ 600 px úteis, o que também testa larguras estreitas (itens de lista a 292 px).
- **Navegação**: índice fixo à esquerda em largura ≥ 1400 (âncora por controle); foco em cada seção por `Ctrl+↓`/`↑`.
- **Screenshots** (`ia/toolkit/release/screenshots.sh`): amostras `Gallery/<Controle>/<Light|Dark>/<pt-BR|en>`; falha se houver erro de binding. Cada controle da lista acima tem `DesignSample` (A3, A6).
- **Dados**: sintéticos (`Radus`, `LogNStick`, `Sofia`, `Istanbul`; mods fictícios "Balkans Expansion"); nada de marcas ou arte da SCS.

### 3.2 O que a galeria precisa mostrar por controle
| Controle | Estados mínimos |
| --- | --- |
| Botões | 3 classes × normal/hover/pressed/focus/disabled; `icon` 40 e 44; texto longo |
| PageHeader | padrão com ação; texto longo; sem contagem |
| SearchBox | vazio, foco, preenchido + ×, ocupado, desabilitado, hover |
| SearchablePicker | fechado vazio/selecionado/erro/foco/desabilitado; aberto com resultados (item selecionado, realçado, com tag Mod, nome longo) |
| RouteOfferItem | normal, hover, selecionado, modificado, expirada, texto longo + foco |
| RouteMap | esquemático, geográfico, vazio |
| StatTile | normal, ênfase, desconhecido, texto longo, carregando |
| SectionCard | com ações, título longo + vazio |
| ValueChange | número, texto, adicionado, removido, vertical |
| DiffList | 2 grupos, linha em hover |
| PendingBadge | 3, 12, 99+, etiqueta, ponto, 0 (oculto), dentro do botão |
| EmptyState | sem ofertas, sem resultados |
| ErrorBanner | erro com ação e ×, aviso, info, erro com detalhes |
| LoadingPanel | indeterminado, com progresso e cancelar |

### 3.3 Contrato com o `ui-dev` (A3/A6)
1. Um `DesignSample` por controle e estado (nomes na coluna "Amostras" de cada bloco).
2. Galeria sem lógica de negócio; `GalleryViewModel` só monta amostras estáticas.
3. Cada controle novo entra na galeria e neste documento no mesmo PR (regra da skill `design-system`).
4. Verificação: Light/Dark × pt-BR/en, sem erro de binding, contraste conforme 1.2.
