# Nomes de reboque — telas

Componentes: só os do `design-system` nas §1–6 (aprovadas); a **§7 (ícone, proposto)** traz 1 componente **NOVO** (`TrailerIcon`). Tokens e Light/Dark como em [`../fretes/screens.md`](../fretes/screens.md). Mockups: [`canvas/Reboque.dc.html`](canvas/Reboque.dc.html), [`canvas/Exemplos.dc.html`](canvas/Exemplos.dc.html) e [`canvas/Icones.dc.html`](canvas/Icones.dc.html).

> **Status por parte:** §1–6 (nome e detalhes) **aprovadas**, sem mudança. §7 (ícone) **proposta**. Os wireframes das §2–4 abaixo mostram o desenho aprovado; o ícone entra por cima, como descrito na §7.3.

## 1. Regra do nome (contrato do texto)

Entrada: id da definição, ex. `trailer_def.scs.box.double_3_2.curtain` → sem o prefixo `trailer_def.`: `scs.box.double_3_2.curtain` → pontos: `dono=scs`, restante `[box, double_3_2, curtain]`.

| Parte | Como reconhecer | Vira |
| --- | --- | --- |
| Dono | 1º segmento (`scs`, `car_d`, `glass`, mod) | nunca aparece |
| Cadeia | segmento que casa `^(single\|singl\|double\|bdouble\|hct)(_…)?$` (`singl` é um erro de grafia que existe nos saves: vale como `single`) | `single` não aparece; `double`, `bdouble`, `hct` entram no nome: `Trailer_Name_WithChain` |
| Acabamento | qualquer segmento do restante (exceto a cadeia), **do último ao primeiro**, com chave `Trailer_Finish_<token>` | nome-base (vence a carroceria) |
| Carroceria | qualquer segmento do restante, **do primeiro ao último**, com chave `Trailer_Body_<token>` | nome-base se não houve acabamento |
| Fallback (mod) | nenhum token com chave | carroceria (2º segmento, se há cadeia depois dele; senão o 1º do restante; id sem ponto: o id inteiro) humanizado por `GameIds.Humanize` |
| Eixos | partes após a cadeia: leva-se, em ordem, as que casam `^\d[a-z]?$` (um dígito, letra opcional), no máximo 1 (single), 2 (double, bdouble) ou 3 (hct) | `3` → "3 eixos"; `3_2` → "3+2 eixos". **Tudo ou nada:** se o número esperado não casa (`41`), não mostra eixos |
| Comprimento | último segmento = `short`/`long`, ou parte `sh`/`ln` após a cadeia | "curto" / "longo" |
| Não são nomes | `steel`, `str_stl`, `flatbed_w`, `paint`, `short`, `long` | sem chave: caem na carroceria |

- Por que "último ao primeiro" no acabamento: ids curtos de mods e de DLC (`car_d.car_trans`, `glass.panel_trans.glass`) não têm carroceria nem cadeia; o acabamento é o último token que o vocabulário conhece (`glass` não está no vocabulário, `panel_trans` está).
- Detalhes = eixos e comprimento unidos por `Trailer_Details_Join` (" · "); vazio se nada for reconhecido. Nunca se inventa um valor.
- Busca de chave com `TryGet` (sem `!chave!`) e idioma ausente → neutro (en). Cultura de comparação: invariante, minúsculas.

## 2. Campo Reboque (tela Fretes) — superfície A

```
Cartão (4ª coluna do bloco de indicadores; ~176 px, texto ~152 px)
┌────────────────────┐
│ REBOQUE            │   caption (Jobs_Field_Trailer, caixa alta)
│ Baú refrigerado    │   Subtitle 16/600, TextColor, até 2 linhas, reticências
│ · Bitrem           │
│ 3+3 eixos · longo  │   bodysm, TextMutedColor, 1 linha; vazio = sem linha (altura mínima 16)
└────────────────────┘
Tooltip (hover/foco no nome):
  Copiado de um frete deste save com a mesma carga
  ID do reboque: scs.box.bdouble_3_3.dryvan        (mono 12, TextMutedColor)
```

- **Mudanças em relação ao desenho aprovado de Fretes:** (1) nome em `Subtitle`, não `metric`; (2) `MaxLines=2` em vez de `trim`; (3) a linha de ajuda sai do cartão; os detalhes ocupam o lugar dela. Altura do cartão: no máximo +20 px em relação a hoje; o `Grid` de indicadores alinha o topo (`VerticalAlignment=Top`).
- **Componentes:** cartão `Border.tile` existente; `ToolTip.Tip` com 2 linhas (ajuda + id).
- **Estados:**

| Estado | Nome | Detalhes | Tooltip |
| --- | --- | --- | --- |
| Conteúdo | nome da regra | eixos · comprimento | ajuda + id |
| Cadeia dupla | "Sider · Rodotrem" (2 linhas se preciso) | "3+2 eixos" | idem |
| Mod desconhecido | token humanizado ("Cargo box · Rodotrem") | só o que casar | idem |
| Nome longo | 2 linhas e reticências; tooltip tem o texto inteiro | idem | idem |
| Sem reboque (frete sem modelo / carga ainda não escolhida) | `—` em `TextMutedColor` | vazio | sem tooltip |
| Carregando / erro | não há: é cálculo local; o cartão acompanha o estado da página (some com `ShowIndicators`) | | |

- **Foco / teclado:** o cartão não recebe foco (é leitura). O tooltip também abre com o foco no campo de carga; a ajuda vai para `AutomationProperties.HelpText`, para quem não usa mouse.
- **Nome acessível** do cartão: `Trailer_Automation_Details` ("Reboque: Baú refrigerado · Bitrem, 3+3 eixos"), sem detalhes `Trailer_Automation`, sem reboque `Trailer_Automation_None`.
- **Largura:** o nome nunca é cortado em 1 linha sem tooltip; passou de 2 linhas, reticências + tooltip completo.

## 3. Seletor de Carga: texto secundário — superfície B

```
┌ Carga ───────────────────────────┐
│ Máquinas                      ✓  │   primário: nome da carga
│ Sider · Rodotrem                 │   secundário (12 px, TextMuted): só o NOME
│ Vigas de concreto                │
│ Plataforma                       │
│ Peças especiais                  │   sem reboque: sem 2ª linha
└──────────────────────────────────┘
```

- `PickerOption(cargo, CargoName, TrailerNome)`. A busca do `SearchablePicker` **também casa o texto secundário** (digitar "baú" acha as cargas que usam baú), se o componente já faz; senão, não muda.
- Ordenação continua pelo nome da carga (não pelo reboque).
- Estados: normal, mod (token humanizado), sem reboque (linha ausente; linha com 1 só texto, altura da opção inalterada).
- Nome acessível da opção: `"{carga}, reboque {nome}"` (já é o que o leitor de tela lê como 2 textos; sem chave nova: usa `Jobs_Field_Trailer`).

## 4. Item de oferta (`RouteOfferItem`) e revisão — superfícies C e D

### C. 2ª linha do `RouteOfferItem`
```
Sófia ⋯ Belgrado                       394 km
Máquinas · Baú refrigerado                       ← CargoText · TrailerText (nome, sem detalhes)
```
- Hoje: `SecondLine = CargoText + " · " + TrailerText`; **não muda a lógica**, só o texto do reboque. Vazio → só a carga (já tratado).
- Truncamento: reticências no fim, o reboque cede primeiro; tooltip com a linha inteira.
- Cadeia dupla: "Eletrônicos · Sider · Rodotrem" (pergunta 5 do README).
- Nome acessível (`Offer_A11y`): `{Trailer}` passa a ser o nome com detalhes: "Baú refrigerado · Bitrem, 3+3 eixos".

### D. Linha do diff (`DiffList`/`ValueChange`)
```
REBOQUE   Prancha rebaixada (longo)   →   Baú refrigerado (3 eixos)
REBOQUE   —                           →   Sider · Rodotrem (3+2 eixos)       (vaga livre)
REBOQUE   Prancha baixa (4 eixos · curto) → Prancha baixa (4 eixos · longo)  (mesmo nome)
```
- Texto = `nome` + ` (detalhes)` (`Trailer_Details_Paren`) quando há detalhes; só o nome se não há.
- Antes: o reboque **da oferta que será substituída** (`trailer_definition` antigo); vaga livre: `—` (como hoje, `DiffValue.Empty`).
- Tooltip em cada lado: id do reboque (antes/depois).
- Estados do diff: igual ao de `review-apply` (carregando/vazio/erro/conteúdo); esta linha só muda o texto.

## 5. Contrato exigido do código (para `chief-architect` / `ui-dev`)

1. **Quem monta o texto.** Os vocabulários são `.resx` (camada App); o diff nasce no Core (`DiffValue.GameName(GameNameKind.Trailer, id)`). Sugestão: o formatador (`TrailerNaming`, regra do §1) fica no App atrás de `ILocalizer` e é chamado por onde `GameName(Trailer)` é renderizado; o Core continua passando o **id da definição** e não texto. Retorno sugerido: `TrailerLabel(string Name, string Details, string Id)`.
2. **Definição, não variante.** Hoje `TrailerId(JobTemplate)` e o diff usam `trailer_variant` (`trailer.scs_flat_b`), que **não** tem acabamento nem cadeia. A regra exige `trailer_definition` (`JobTemplate.TrailerDefinition`). O "antes" do diff (`oldVariant`, `JobPlanner.cs:133`) também precisa trazer a definição da oferta antiga (`ExistingOffer`).
3. **`ILocalizer.TryGet(key, out text)`** para buscar `Trailer_Finish_*`/`Trailer_Body_*` sem cair em `!chave!`.
4. **Plural de eixos:** `Trailer_Axles_One`/`Trailer_Axles_Other` por `GetPlural`; o caso "3+3" é o `Other` com `{0}` = "3+3".
5. **Testes sugeridos:** os 13 ids de [`strings.md`](strings.md) §3 como tabela de teste, em pt-BR e en.

## 6. Ordem de foco e atalhos
Nada novo: o fluxo de Fretes não muda (Ctrl+R revisa, Ctrl+Enter confirma na revisão). Dentro do seletor de carga, as setas e o Enter são os do `SearchablePicker`. O ícone (§7) não recebe foco e não muda a ordem de Tab.

---

## 7. Ícone do reboque — PROPOSTO

Objetivo: reconhecer o tipo de reboque de relance (baú, sider, prancha, tanque…) ao percorrer a lista de cargas, as ofertas e a revisão. É **decoração**: o nome (§1) continua sendo a informação; o ícone só acelera a leitura. Mockups: [`canvas/Icones.dc.html`](canvas/Icones.dc.html) (catálogo Claro/Escuro), [`canvas/Reboque.dc.html`](canvas/Reboque.dc.html) (nas 4 superfícies), [`canvas/Exemplos.dc.html`](canvas/Exemplos.dc.html) (id → ícone).

### 7.1 Princípios (regras duras)
1. **Só decoração (ADR 0003).** O ícone é calculado depois que a lista de opções já existe. Não filtra, não ordena, não agrupa, não habilita/desabilita nada, não entra na busca do `SearchablePicker`. Reboque de mod desconhecido nunca some: usa o ícone **Genérico**.
2. **Sem arte do jogo.** 14 desenhos originais, monocromáticos, de contorno, vistos de lado; nada de logotipo, foto, textura ou silhueta copiada de modelo do jogo.
3. **Sem cor fixa.** Só pincéis do tema (`TextBrush`, `TextMutedBrush`, `AccentTextBrush`); Light/Dark trocam sozinhos. Sem cor por categoria (cor só no estado: selecionado).
4. **Invisível para leitor de tela.** `AutomationProperties.AccessibilityView="Raw"` no ícone e no selo; o nome acessível continua o do §2–4 (já diz "Baú refrigerado · Bitrem").
5. **Sem reboque = sem ícone** (e sem espaço reservado, exceto no seletor: §7.3 B). Reboque **desconhecido** = ícone Genérico. Os dois casos são diferentes de propósito.
6. **Mesma regra de token do nome** (§1): acabamento do último ao primeiro, depois carroceria do primeiro ao último. Assim nome e ícone não se contradizem.

### 7.2 Onde aparece e tamanhos

| # | Superfície | Tamanho do ícone | Contêiner | Cor | Selo ×N |
| --- | --- | --- | --- | --- | --- |
| A | Cartão **Reboque** (tela Fretes) | 24 px | nenhum; canto direito da linha do rótulo | `TextBrush` | sim |
| B | Linha do seletor de **Carga** (`SearchablePicker`) | 20 px | chip 32×32, `RadiusSm`, fundo `SurfaceHoverBrush`, à esquerda do texto | `TextMutedBrush`; linha selecionada: `AccentTextBrush` e chip sem fundo | sim |
| C | 2ª linha do `RouteOfferItem` | 16 px | nenhum; antes do texto, gap 6 | `TextMutedBrush`; item selecionado: `AccentTextBrush` | não |
| D | Linha **Reboque** do diff (`ValueChange`) | 16 px | nenhum; antes do texto de cada lado, gap 6 | antes: `TextMutedBrush`; depois: `TextBrush` | não |

- **Traço** (px reais, `StrokeThickness`; precedente: `IconNav*` usa 1,8 a 22 px): 24 px → 1,8; 20 px → 1,6; 16 px → 1,4. `StrokeLineCap`/`StrokeJoin` = Round. Sem `Fill`.
- **Por que 16 px sem selo:** a cadeia já está no texto ("· Rodotrem") nas superfícies C e D, e um selo de 18×14 px não fica legível ao lado de 16 px.
- Os tamanhos 16/20/24 são os únicos; nenhum outro valor solto.

### 7.3 Composição com o nome de 2 linhas

**A. Cartão (coluna de ~176 px, texto ~152 px).** O ícone fica na **linha do rótulo**, à direita, para não tirar largura do nome (que já usa 2 linhas). A linha ganha altura mínima 24 px (+10 px no cartão; cabe nos "+20 px" já previstos em §2).
```
┌────────────────────┐
│ REBOQUE        [▭]24│   caption à esquerda; ícone 24 px à direita (selo ×2 no canto inferior direito do ícone)
│ Baú refrigerado     │   Subtitle 16/600, até 2 linhas
│ · Bitrem            │
│ 3+3 eixos · longo   │   bodysm muted
└────────────────────┘
```
- Sem reboque: nada na linha do rótulo (o ícone é omitido, não vira espaço em branco além do mínimo de 24 px; o `Grid` alinha o topo).
- O tooltip do nome (ajuda + id) continua só no nome; o ícone tem `IsHitTestVisible="False"` para não roubar o hover.

**B. Linha do seletor de carga.** Chip à esquerda, depois as 2 linhas de texto (carga + nome do reboque). A altura mínima 44 px da opção não muda (chip 32 + 6 + 6 de padding).
```
┌ Carga ─────────────────────────────────┐
│ [▭]32  Máquinas                      ✓ │   chip 32: ícone 20 px; selo ×N sobre o canto do chip
│        Sider · Rodotrem                │
│ [▭]32  Plataforma …                    │
│ [  ]32 Peças especiais                 │   sem reboque: o chip fica VAZIO (reserva o espaço) para as linhas alinharem
└────────────────────────────────────────┘
```
- **Exceção ao "sem espaço reservado":** no seletor o chip vazio (sem fundo, sem ícone) mantém o alinhamento vertical do texto entre as linhas. Em `PickerOption` o ícone é um campo genérico (`LeadingIcon`), então qualquer outro seletor pode usá-lo depois.
- A busca continua casando só o texto (primário e secundário); **nunca** o nome do ícone.
- Largura: o texto perde 32 + 8 px; já trunca com reticências e tooltip.

**C. `RouteOfferItem`.** O ícone abre a 2ª linha, antes de "Carga · Reboque". Alinhar os ícones numa coluna à esquerda facilita varrer a lista.
```
Sófia ⋯ Belgrado                          394 km
[▭]16 Máquinas · Baú refrigerado
```
- Sem reboque: a linha começa sem ícone (aceito: é raro; evita um recuo permanente sob os nomes das cidades).
- Truncamento: o texto trunca; o ícone nunca encolhe (`Width/Height` fixos, `Shrink` 0).

**D. Diff da revisão.** Ícone de 16 px antes do texto de **cada** lado; trocas de categoria (prancha → baú) ficam evidentes sem ler.
```
REBOQUE   [▭] Prancha rebaixada (longo)   →   [▭] Baú refrigerado (3 eixos)
REBOQUE   —                               →   [▭] Sider · Rodotrem (3+2 eixos)
```
- Lado "—" (vaga livre / sem antes): sem ícone. Altura da linha inalterada.
- Requer dois campos opcionais novos em `IDiffLine`/`ValueChange` (`BeforeIcon`, `AfterIcon`: chave do recurso ou nulo). O Core **não muda**: `DiffValue.Trailer` já leva a definição; o App calcula a chave. Pergunta 8 do README (o diff é opcional; A–C valem sozinhos).

### 7.4 Cadeia dupla (`double`, `bdouble`, `hct`)
- **Um ícone só** (o da carroceria/acabamento), mais um **selo "×N"** no canto inferior direito do ícone, só em 20 e 24 px (A e B). N = reboques da cadeia: `double`/`bdouble` = 2, `hct` = 3; `single`/desconhecida = sem selo.
- Selo: pílula de 14 px de altura, mín. 18 px de largura, texto 10 px SemiBold `+tnum`, fundo `SurfaceBrush`, borda 1 px `BorderStrongBrush`, `RadiusPill`; sobrepõe o ícone em 7 px à direita e 5 px abaixo (deixa respiro para o texto vizinho: o cartão e o chip têm padding de 12 e 6 px).
- O texto do selo vem de `Trailer_Badge` ("×{0}"), formatado com a cultura (`+tnum`).
- Por que selo e não ícone duplo: duas carrocerias em 24 px viram ruído; o selo é legível e o nome ("Rodotrem", "Bitrem") reforça.
- Selo é decorativo (Raw), como o ícone.

### 7.5 Regra token → ícone

Entrada: `TrailerDescriptor.Tokens` (minúsculas, sem o dono e sem o segmento da cadeia; `short`/`long`/`steel`/`flatbed_w` etc. simplesmente não estão na tabela). **Não** usar `Body`/`Finish` direto: são só posições estruturais (em `glass.panel_trans.glass`, `Finish` = `glass`, e o token que importa é `panel_trans`). Usa-se `Chain` para o selo.

Algoritmo (idêntico ao do nome):
1. **Passo acabamento:** do **último ao primeiro** token, o primeiro que estiver na coluna "Acabamento" vence.
2. **Passo carroceria:** senão, do **primeiro ao último**, o primeiro que estiver na coluna "Carroceria" vence.
3. Senão: **Genérico**.
4. Definição e variante vazias: **sem ícone** (não é Genérico).

| Ícone (chave do recurso) | Tokens de acabamento (passo 1) | Tokens de carroceria (passo 2) | Observação |
| --- | --- | --- | --- |
| `IconTrailerBox` (baú/furgão) | `dryvan`, `dryvan_m`, `insulated` | `box` | |
| `IconTrailerCurtain` (sider) | `curtain` | | |
| `IconTrailerReefer` (refrigerado) | `reefer` | | baú com floco de neve |
| `IconTrailerFlatbed` (plataforma) | `brick` | `flatbed` | `brick` = inferência (pergunta 9) |
| `IconTrailerLowbed` (prancha baixa/rebaixada) | | `lowbed`, `lowloader` | um desenho para os dois |
| `IconTrailerContainer` (contêiner/gooseneck) | `container` | `gooseneck` | |
| `IconTrailerDumper` (basculante) | | `dumper` | |
| `IconTrailerLog` (toras) | | `log` | |
| `IconTrailerTank` (tanque) | | `foodtank` | outros tanques: pergunta 9 |
| `IconTrailerSilo` (silo) | | `silo` | |
| `IconTrailerLivestock` (animais) | | `livestock` | |
| `IconTrailerCarTransporter` (cegonha) | `car_trans`, `van_trans`, `truck_trans` | | |
| `IconTrailerPanel` (painéis/vidro) | `panel_trans`, `gls_trailer`, `glass` | | `glass` só existe no ícone (o nome não o usa) |
| `IconTrailerGeneric` (genérico) | qualquer outro | qualquer outro | chassi vazio; mods e valores desconhecidos |

Exemplos (os 13 ids de [`strings.md`](strings.md) §3; o mesmo conjunto vira tabela de teste):

| Id da definição | Ícone | Selo | Por quê |
| --- | --- | --- | --- |
| `scs.box.single_3.reefer` | Reefer | | acabamento `reefer` |
| `scs.box.double_3_2.curtain` | Curtain | ×2 | acabamento `curtain` vence `box` |
| `scs.box.bdouble_3_3.dryvan` | Box | ×2 | acabamento `dryvan` |
| `scs.lowbed.single_41_ln.long` | Lowbed | | `long` não é acabamento; carroceria `lowbed` |
| `scs.lowloader.single_4_sh.short` | Lowbed | | carroceria `lowloader` |
| `scs.gooseneck.singl_3e_40.container` | Container | | acabamento `container` |
| `scs.dumper.single_3.steel` | Dumper | | `steel` ignorado; carroceria |
| `scs.silo.single_3_10.silo` | Silo | | carroceria |
| `scs.flatbed.single_3.flatbed_w` | Flatbed | | `flatbed_w` ignorado; carroceria `flatbed` |
| `car_d.car_trans` | CarTransporter | | acabamento |
| `glass.panel_trans.glass` | Panel | | do último ao primeiro: `glass` (alias) já vence |
| `acme_mods.cargo_box.double_2_2.frozen` *(mod)* | Generic | ×2 | nenhum token conhecido |
| `my_trailer` *(mod)* | Generic | | idem |

### 7.6 Como os ícones são guardados

Reaproveita o sistema que já existe: **`src/OwnExpeditor.App/Styles/Icons.axaml`** (`StreamGeometry`, 24×24) e o conversor `ShellConverters.Icon` (chave string → geometria dos recursos), usado hoje pelos `IconNav*`. Os 14 novos entram **no mesmo arquivo**, em um bloco com comentário, **de contorno** (usar `Stroke`, sem `Fill`), como os `IconNav*`.

```xml
<!-- Ícones de reboque (24 x 24, de contorno: Stroke 1,4 a 1,8 conforme o tamanho, sem Fill). Decorativos (AccessibilityView="Raw"). Fonte: design/nomes-reboques/canvas/Icones.dc.html -->
<StreamGeometry x:Key="IconTrailerGeneric">M3 12.5h18M3 16h18M8 12.5V16M13 12.5V16M5 16v3.4M14.1 19.2a1.4 1.4 0 1 0 2.8 0 1.4 1.4 0 1 0-2.8 0M18.1 19.2a1.4 1.4 0 1 0 2.8 0 1.4 1.4 0 1 0-2.8 0</StreamGeometry>
<StreamGeometry x:Key="IconTrailerBox">M3 5.5h18V16H3zM17.5 5.5V16M5 16v3.4M14.1 19.2a1.4 1.4 0 1 0 2.8 0 1.4 1.4 0 1 0-2.8 0M18.1 19.2a1.4 1.4 0 1 0 2.8 0 1.4 1.4 0 1 0-2.8 0</StreamGeometry>
<StreamGeometry x:Key="IconTrailerCurtain">M3 5.5h18V16H3zM3 8.5h18M7.5 8.5V16M12 8.5V16M16.5 8.5V16M5 16v3.4M14.1 19.2a1.4 1.4 0 1 0 2.8 0 1.4 1.4 0 1 0-2.8 0M18.1 19.2a1.4 1.4 0 1 0 2.8 0 1.4 1.4 0 1 0-2.8 0</StreamGeometry>
<StreamGeometry x:Key="IconTrailerReefer">M3 5.5h18V16H3zM12 7.5v6.5M9.2 9.1l5.6 3.2M14.8 9.1l-5.6 3.2M5 16v3.4M14.1 19.2a1.4 1.4 0 1 0 2.8 0 1.4 1.4 0 1 0-2.8 0M18.1 19.2a1.4 1.4 0 1 0 2.8 0 1.4 1.4 0 1 0-2.8 0</StreamGeometry>
<StreamGeometry x:Key="IconTrailerFlatbed">M3 14h18v2H3zM3 7.5V14M7 8.5h11V14H7zM5 16v3.4M14.1 19.2a1.4 1.4 0 1 0 2.8 0 1.4 1.4 0 1 0-2.8 0M18.1 19.2a1.4 1.4 0 1 0 2.8 0 1.4 1.4 0 1 0-2.8 0</StreamGeometry>
<StreamGeometry x:Key="IconTrailerLowbed">M3 9.5h5.5l1.3 3.7H21V16H3zM14.1 19.2a1.4 1.4 0 1 0 2.8 0 1.4 1.4 0 1 0-2.8 0M18.1 19.2a1.4 1.4 0 1 0 2.8 0 1.4 1.4 0 1 0-2.8 0</StreamGeometry>
<StreamGeometry x:Key="IconTrailerContainer">M6 5.5h15V14H6zM9.5 5.5V14M13.5 5.5V14M17.5 5.5V14M3 16h18M3 11.5V16M14.1 19.2a1.4 1.4 0 1 0 2.8 0 1.4 1.4 0 1 0-2.8 0M18.1 19.2a1.4 1.4 0 1 0 2.8 0 1.4 1.4 0 1 0-2.8 0</StreamGeometry>
<StreamGeometry x:Key="IconTrailerDumper">M3 8h13l5 8H3zM6 8l2-3 2.3 2.2 2.4-3L15 8M14.1 19.2a1.4 1.4 0 1 0 2.8 0 1.4 1.4 0 1 0-2.8 0M18.1 19.2a1.4 1.4 0 1 0 2.8 0 1.4 1.4 0 1 0-2.8 0</StreamGeometry>
<StreamGeometry x:Key="IconTrailerLog">M3 16h18M4.5 6v10M19.5 6v10M8 8.5h8a1.5 1.5 0 0 1 0 3H8a1.5 1.5 0 0 1 0-3zM8 11.5h8a1.5 1.5 0 0 1 0 3H8a1.5 1.5 0 0 1 0-3zM14.1 19.2a1.4 1.4 0 1 0 2.8 0 1.4 1.4 0 1 0-2.8 0M18.1 19.2a1.4 1.4 0 1 0 2.8 0 1.4 1.4 0 1 0-2.8 0</StreamGeometry>
<StreamGeometry x:Key="IconTrailerTank">M7.5 5h9a4.5 4.5 0 0 1 0 9h-9a4.5 4.5 0 0 1 0-9zM11 5V3.5h2V5M8 14v2M16 14v2M3 16h18M14.1 19.2a1.4 1.4 0 1 0 2.8 0 1.4 1.4 0 1 0-2.8 0M18.1 19.2a1.4 1.4 0 1 0 2.8 0 1.4 1.4 0 1 0-2.8 0</StreamGeometry>
<StreamGeometry x:Key="IconTrailerSilo">M7 16V9.5a5 5 0 0 1 10 0V16M7 12.5h10M3 16h18M14.1 19.2a1.4 1.4 0 1 0 2.8 0 1.4 1.4 0 1 0-2.8 0M18.1 19.2a1.4 1.4 0 1 0 2.8 0 1.4 1.4 0 1 0-2.8 0</StreamGeometry>
<StreamGeometry x:Key="IconTrailerLivestock">M3 5.5h18V16H3zM6 8.5h4M14 8.5h4M6 12.5h4M14 12.5h4M5 16v3.4M14.1 19.2a1.4 1.4 0 1 0 2.8 0 1.4 1.4 0 1 0-2.8 0M18.1 19.2a1.4 1.4 0 1 0 2.8 0 1.4 1.4 0 1 0-2.8 0</StreamGeometry>
<StreamGeometry x:Key="IconTrailerCarTransporter">M3 16h18M4.5 10.5h15M4.5 5h15M4.5 5v11M19.5 5v11M7 16l.9-3.2h4.2l.9 3.2M10 10.5l.9-3.2h4.2l.9 3.2M14.1 19.2a1.4 1.4 0 1 0 2.8 0 1.4 1.4 0 1 0-2.8 0M18.1 19.2a1.4 1.4 0 1 0 2.8 0 1.4 1.4 0 1 0-2.8 0</StreamGeometry>
<StreamGeometry x:Key="IconTrailerPanel">M3 16h18M5.5 15 8 7h4l-2.5 8zM12.5 15 15 7h4l-2.5 8zM14.1 19.2a1.4 1.4 0 1 0 2.8 0 1.4 1.4 0 1 0-2.8 0M18.1 19.2a1.4 1.4 0 1 0 2.8 0 1.4 1.4 0 1 0-2.8 0</StreamGeometry>
```
- Nomenclatura: `IconTrailer<Categoria>`; a chave do recurso é o que o resolvedor devolve (sem enum de ícone separado).
- As geometrias do canvas são exatamente estas (no canvas são montadas por concatenação: corpo + trem de rolagem). O desenho foi feito sem ver o app rodando: **verificar visualmente** em 16/20/24 px e nos dois temas (principalmente selo ×N e os detalhes finos do floco de neve e das nervuras a 16 px; a silhueta deve continuar distinta).
- Se o `ui-dev` achar que o traço não escala com `Stretch="Uniform"` (Avalonia aplica o traço em px, após a escala), usar os valores de §7.2; senão ajustar.

### 7.7 Componente NOVO: `TrailerIcon`

Para quê: desenhar o ícone (geometria por chave), com traço por tamanho, cor do tema e selo ×N, nas 4 superfícies, sem repetir XAML. Entra no catálogo do `design-system` e na Galeria no mesmo PR (regra 1).

| Propriedade | Tipo | Padrão | Nota |
| --- | --- | --- | --- |
| `IconKey` | `string?` | nulo | chave `IconTrailer*`; nulo = não desenha nada (e `IsVisible` falso) |
| `Units` | `int` | 1 | > 1 e tamanho ≥ 20 mostra o selo `Trailer_Badge` |
| `IconSize` | `double` | 16 | só 16, 20 ou 24 |
| `HasChip` | `bool` | falso | chip 32×32 com fundo `SurfaceHoverBrush` (seletor) |
| `Foreground` | `IBrush` | `TextMutedBrush` | herda cor do pai quando selecionado (`AccentTextBrush`) |

- **Estados:** sem ícone (oculto) · normal · com selo · selecionado (cor de destaque) · desabilitado (herda o `Opacity` do pai, 0,4 no seletor) · chip vazio (só no seletor, quando a opção não tem reboque).
- **Acessibilidade:** `AutomationProperties.AccessibilityView="Raw"`, não focável, `IsHitTestVisible="False"`.
- **Galeria (Debug):** seção `TrailerIcon` com os 14 ícones × 3 tamanhos × (normal, selo ×2, ×3, selecionado, desabilitado, chip), alternando Light/Dark e pt-BR/en (igual ao `Icones.dc.html`).
- Não há alternativa nos componentes existentes: `EmptyState`, `InfoTag` e `StepBadge` têm outro propósito.

### 7.8 Estados, tema e idioma
| Caso | Comportamento |
| --- | --- |
| Reboque conhecido | ícone da tabela §7.5 |
| Reboque de mod (tokens desconhecidos) | Genérico; mesmo tamanho e cor; nome humanizado como hoje |
| Sem reboque (frete sem modelo / carga não escolhida) | sem ícone (A, C, D); chip vazio (B) |
| Seleção / foco / hover da linha | só a cor muda (selecionado); hover e foco são da linha, não do ícone |
| Carregando / erro | não há: cálculo local, acompanha o estado da página |
| Light / Dark | pincéis do tema; contraste do ícone ≥ 3:1 em `SurfaceBrush` e `SelectionBrush` (os pincéis de texto já passam em ambos) |
| pt-BR / en | ícone sem texto; só o selo usa `Trailer_Badge` ("×2" nos dois idiomas) |

### 7.9 Contrato exigido do código (para `chief-architect` / `ui-dev`)
1. `TrailerIcons.Resolve(TrailerDescriptor) → TrailerIconInfo(string Key, int Units)?` em `App/Services/` (ao lado de `TrailerText`): puro, sem `ILocalizer`, sem I/O; devolve `null` só se não houver definição nem variante. Tabelas de §7.5 como dois `Dictionary<string,string>` (acabamento e carroceria); comparação minúscula invariante.
2. `JobsPageViewModel`: `TrailerIconKey`, `TrailerUnits`; `PickerOption`: `LeadingIcon`/`LeadingBadge` (opcionais; o `SearchablePicker` não os usa na busca); `RouteOfferItem`: `TrailerIconKey`; `IDiffLine`/`ValueChange`: `BeforeIcon`/`AfterIcon` (se a pergunta 8 for aceita).
3. O ícone **nunca** entra em `Where`/`OrderBy`/chave de agrupamento. Teste de regressão: a lista de opções (cargas, ofertas) é idêntica com o resolvedor trocado por um que sempre devolve nulo.
4. Testes: os 13 ids de §7.5 → chave e selo; id vazio → `null`; tokens aleatórios → `IconTrailerGeneric`; toda chave devolvida existe em `Icons.axaml`; os 14 recursos existem.
5. `check-i18n.sh`: `Trailer_Badge` no `.resx` (nenhum "×" fixo no XAML).
