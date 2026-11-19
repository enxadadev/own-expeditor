# Nomes de reboque (Fretes)

- **Status:**
  - **Nome e detalhes do reboque** (§1–6 de `screens.md`, decisões 1–7 abaixo): **aprovado** (2026-11-18), como proposto (termos e perguntas 1–7 aceitos com os padrões do desenho). **Sem mudança.**
  - **Ícone do reboque** (§7 de `screens.md`, decisões 8–14 e perguntas 8–14 abaixo): **aprovado** (2026-11-18), com as escolhas recomendadas do desenho (ícones conferidos pelo usuário em imagem).
- **Canvas:** fonte em [`canvas/`](canvas/) (`canvas.json` + 3 `.dc.html`, 1280×800). Link do canvas publicado: *a preencher pela sessão principal* (republicar: entrou o `Icones.dc.html` e os outros dois ganharam ícones).
- **Pertence a:** módulo Fretes ([`../fretes/`](../fretes/README.md), aprovado). Esta pasta **altera** só o texto do reboque e (proposto) acrescenta o ícone; o resto da tela não muda (exceto o item 2 de "Decisões" e o ícone no cartão).
- **Direção:** A+C ([`../direcao-visual/`](../direcao-visual/README.md)), controles de [`../controles/`](../controles/README.md). Nomes: nenhum componente novo. Ícone: **1 componente NOVO** (`TrailerIcon`, `screens.md` §7.7).

## Objetivo do usuário
Ler o reboque do frete como uma pessoa ("Baú refrigerado · Bitrem", "3+3 eixos") e não como um id do jogo ("Scs flat b"), em qualquer lugar da tela de Fretes e da revisão, em pt-BR e en, e mesmo com reboques de mods.

## Onde aparece (4 superfícies, um só texto)
| # | Superfície | Mostra | Hoje |
| --- | --- | --- | --- |
| A | Campo **Reboque** da tela Fretes (`JobsView.axaml:163-165`) | nome, detalhes, tooltip com id | `Scs flat b` + "Copiado de um frete…" |
| B | Texto secundário do seletor de **Carga** (`JobsPageViewModel.Flow.cs:311`) | nome | `Scs flat b` |
| C | 2ª linha do `RouteOfferItem` (`CargoText · TrailerText`) | nome | `Carga · Scs flat b` |
| D | Linha **Reboque** do diff na revisão (`JobPlanner` → `DiffValue.GameName`) | nome + detalhes entre parênteses | `Scs flat b → Scs flat b` |

## Decisões
1. **Nome derivado do id da definição** (`trailer_def.<dono>.<carroceria>.<cadeia>_<eixos>.<acabamento>`), por vocabulário `.resx`, não por lista de reboques: serve a mods e não depende da pasta do jogo. Busca `Trailer_Finish_<acabamento>` → `Trailer_Body_<carroceria>` → token humanizado (nunca `!chave!`). Regra completa em [`screens.md`](screens.md) §1.
2. **Nome curto, detalhe à parte.** O nome diz *que reboque é* (e a cadeia, se não for simples: "Baú refrigerado · Bitrem"). Eixos e comprimento vão numa linha de detalhes ("3+3 eixos · longo"), só no campo da tela Fretes e na revisão. Seletor e item de oferta mostram só o nome (pouco espaço).
3. **Campo da tela Fretes** (única mudança visual): o nome deixa de usar o estilo `metric` (22 px, para números) e passa a `Subtitle` 16 px com até **2 linhas**, porque a coluna tem ~152 px de texto e "Baú refrigerado · Bitrem" não cabe em 22 px. A linha de ajuda "Copiado de um frete deste save com a mesma carga" **sai do cartão e vai para o tooltip** (junto com o id) e para `AutomationProperties.HelpText`; no lugar entram os detalhes. Pergunta 1.
4. **Id cru só no tooltip** ("ID do reboque: scs.box.single_3.reefer"). Não é termo do formato SII (é o id de um reboque, útil a quem usa mods).
5. **Sem reboque conhecido:** campo "—", sem detalhes, sem tooltip; seletor de carga sem texto secundário; item de oferta mostra só a carga; diff "—".
6. **Nome igual, detalhes diferentes (diff):** o diff mostra sempre os detalhes entre parênteses, então "Prancha baixa (4 eixos · curto) → Prancha baixa (4 eixos · longo)" não parece "nada mudou".
7. **Termos pt-BR:** `double` = **Rodotrem**, `bdouble` = **Bitrem**, `hct` = **HCT** (sigla mantida). Escolha minha, **não verificada** com a tradução oficial do jogo: ver Perguntas.

## Ícone do reboque (PROPOSTO)

**Objetivo:** reconhecer o tipo de reboque de relance. Decisões (detalhe em `screens.md` §7):

8. **14 ícones vetoriais originais**, de contorno e monocromáticos, vistos de lado (baú, sider, refrigerado, plataforma, prancha, contêiner/gooseneck, basculante, toras, tanque, silo, animais, cegonha, painéis/vidro, genérico). Sem arte nem silhueta do jogo.
9. **Só decoração (ADR 0003):** nunca filtra, ordena, agrupa nem entra na busca; reboque de mod desconhecido segue na lista com o ícone **Genérico**. Decorativo para leitor de tela (`Raw`); o nome já diz o reboque.
10. **Escolhido pelos mesmos tokens do nome** (`TrailerDescriptor.Tokens`: acabamento do último ao primeiro, depois carroceria do primeiro ao último; senão Genérico). `Body`/`Finish` sozinhos não bastam (ex.: `glass.panel_trans.glass`). Tabela completa em `screens.md` §7.5.
11. **Onde e tamanho:** cartão Reboque **24 px** (canto direito da linha do rótulo, para não roubar largura do nome de 2 linhas); seletor de carga **20 px** em chip 32×32; item de oferta e diff **16 px** inline antes do texto.
12. **Cadeia dupla:** **um ícone + selo "×2"** (`double`, `bdouble`) ou "×3" (`hct`), só em 20/24 px; em 16 px o texto "· Rodotrem" já diz.
13. **Armazenamento:** os 14 `StreamGeometry` entram em `Styles/Icons.axaml` (24×24, contorno, como `IconNav*`), resolvidos pelo `ShellConverters.Icon` já existente; cor por pincéis do tema (`TextMutedBrush`, `TextBrush`, `AccentTextBrush`), nada fixo.
14. **Sem reboque = sem ícone; reboque desconhecido = Genérico.** No seletor, uma opção sem reboque mantém um chip vazio para alinhar o texto.

## Telas e estados
| Canvas | Conteúdo | Variações (barra de prévia) |
| --- | --- | --- |
| [`Reboque.dc.html`](canvas/Reboque.dc.html) | As 4 superfícies lado a lado, Claro/Escuro e pt-BR/en, **agora com o ícone proposto** | Baú refrigerado (normal) · Sider · Rodotrem (cadeia dupla) · Prancha longo (detalhe de comprimento) · Mod desconhecido (ícone Genérico + ×2) · Nome longo (2 linhas / reticências) · Sem reboque (sem ícone) |
| [`Exemplos.dc.html`](canvas/Exemplos.dc.html) | Tabela id → **ícone**, nome e detalhes em pt-BR e en (13 ids) | tema, idioma |
| [`Icones.dc.html`](canvas/Icones.dc.html) (novo, proposto) | Catálogo dos 14 ícones em **Escuro e Claro lado a lado**, a 36 px e nos tamanhos reais 24/20/16, mais variações de uso (cartão, selo ×2/×3, chip do seletor, selecionado, suave, desabilitado, sem reboque, mod desconhecido) | idioma |

Documentos: [flows.md](flows.md) · [screens.md](screens.md) · [strings.md](strings.md).

## Componentes
Nomes (aprovado): todos reaproveitados: `FieldRow`/tile do cartão de indicadores, `SearchablePicker`, `RouteOfferItem`, `DiffList`/`ValueChange`. Contrato novo de código (não de componente) em `screens.md` §5.

Ícone (proposto): **NOVO `TrailerIcon`** — desenha o ícone por chave (`IconKey`), tamanho (16/20/24), selo `Units` e chip opcional; estados: oculto, normal, com selo, selecionado, desabilitado, chip vazio. Também estende, com campos opcionais: `PickerOption` (`LeadingIcon`, `LeadingBadge`), `RouteOfferItem` (`TrailerIconKey`) e `IDiffLine`/`ValueChange` (`BeforeIcon`, `AfterIcon`). Entra no catálogo do `design-system` e na Galeria no mesmo PR.

## Perguntas em aberto
1. **Cartão Reboque sem a ajuda fixa** (decisão 3): aceita mover "Copiado de um frete deste save com a mesma carga" para o tooltip? Alternativa: manter a ajuda e deixar os detalhes só no tooltip (cartão mais alto, 4 linhas).
2. **Termos de cadeia em pt-BR** (`double`, `bdouble`, `hct`): confirmar com a tradução do jogo/uso dos jogadores. Em português do Brasil, *bitrem* = 2 semirreboques engatados em sequência (casa com B-double); *rodotrem* = cavalo com 2 semirreboques e dolly (casa com "double"); *treminhão* = caminhão com 2 reboques (poderia ser `hct`, mas deixei "HCT" porque o jogo usa a sigla e não achei equivalente certo).
3. **Vocabulário pt-BR a validar** (ver `strings.md`, coluna de dúvidas): "Sider", "Gooseneck", "Prancha rebaixada" × "Prancha baixa", "Baú seco", "Transporte de carros" (alternativa: "Cegonha").
4. **Tokens com significado incerto** (`dryvan_m`, `3e`, `41`, `40`, `10` em `silo.single_3_10`): tratados como segue em `screens.md` §1 (`dryvan_m` = `dryvan`; `3e` = 3 eixos; números que não são um dígito sozinho são ignorados). Confirmar com o `save-inspector`/`ets2-domain`.
5. **Item de oferta com cadeia:** "Máquinas · Sider · Rodotrem" tem dois "·" com papéis diferentes (carga/reboque e reboque/cadeia). Aceito (cadeia dupla é rara e a linha trunca com reticências) ou usar outro separador só aí?
6. **Prioridade do catálogo do jogo:** se o catálogo trouxer um nome de reboque localizado, ele vence a regra (hoje parece que não traz)? O desenho assume que **não**: a regra vale sempre.
7. **Id no tooltip:** ok mostrar o id do reboque (ex.: `scs.box.single_3.reefer`)? Ajuda a achar o reboque de um mod; é o único "id cru" visível.

### Perguntas do ícone (propostas, aguardando resposta)
8. **Ícone na revisão (superfície D):** incluir (recomendado: a troca de categoria salta aos olhos) ou deixar só A–C? Custa 2 campos opcionais em `IDiffLine`/`ValueChange`; o Core não muda.
9. **Tokens além dos já conhecidos:** `brick` mapeado para plataforma (inferência), `glass` como alias de painéis, e só `foodtank` para tanque. Existem outros corpos de tanque/cimento/combustível nos saves reais e em ATS (ex.: `fueltank`, `chemtank`, `gastank`)? Confirmar com o `save-inspector`; cada um novo é uma linha na tabela §7.5, sem novo desenho (cai no Genérico enquanto isso).
10. **Prancha rebaixada e baixa:** um só desenho (`lowbed` e `lowloader`), ok? Se o jogo as trata como coisas distintas, dá para separar.
11. **Selo "×N" vs. ícone duplo:** o selo (decisão 12) é preferível a desenhar duas carrocerias; ok? `hct` = ×3 é pelo número de grupos de eixos; confirmar.
12. **Ícone do cartão no canto direito** (e não à esquerda do nome): decisão para não encolher o nome de 2 linhas; a alternativa (à esquerda, 32 px) deixa ~112 px de texto. Ok?
13. **Item de oferta:** ícone no começo da 2ª linha (coluna de ícones à esquerda) e **sem** recuo reservado quando falta reboque (a linha fica à esquerda). Ok, ou prefere reservar o espaço sempre?
14. **Verificação visual:** os traços foram desenhados sem renderizar o app. Pede-se checar a 16 px (silhuetas distintas?) e o traço em px com `Stretch="Uniform"` antes de aprovar o resultado final no código.
