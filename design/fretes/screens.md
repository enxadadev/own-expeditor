# Fretes — telas

Janela 1280×800 (mínimo 960×600). O módulo ocupa a área principal do shell (`design/shell/screens.md` §0): barra de sessão de 60 px em cima, rodapé de 36 px embaixo. Componentes do catálogo em `código`; o que não existe está marcado **NOVO**. Textos: [strings.md](strings.md). Mockups: [canvas/](canvas/). Dados de exemplo anonimizados; nomes de cidade/empresa/carga vêm do jogo (traduzidos) e, sem a pasta do jogo, do id legível.

## 1. Estrutura da tela

```
+- barra de sessão (shell) ---------------------------------------------------------+
| [Perfil ▾][Save ▾] ETS2 ● Jogo fechado [Steam Cloud]                  [Revisar (1)] |
+------------------------+--------------------------------------------------------+
| ① CIDADE DE SAÍDA      | [aviso Info: Sem a pasta do jogo ........ Abrir Config.] |  (condicional, 72 px)
| FRETES              10 | [aviso Warning: Steam Cloud ................ Entendi ]  |  (condicional, 72 px)
| [Buscar cidade…      ] | [faixa: Editando uma mudança pendente .. Cancelar]     |  (condicional, 56 px)
|                        | [① Cidade de saída ] [⇄] [② Cidade de destino ▾ ]      |  56 px
| ┌ Sófia ────────────┐ | +- mapa da rota (esquema) ---------------------------+ |  184 px (150 / 128 com avisos)
| │ 2 empresas · 4 vagas│ | | ROTA  Radus · Sófia → Tradeaux · Belgrado          | |
| └────────────────────┘ | |  ◯─────────╌╌╌╌─────────●            394 km         | |
| ┌ Belgrado  1 pendente┐ | | Esquema da rota, sem posições reais                | |
| │ 2 empresas · 4 vagas│ | +----------------------------------------------------+ |
| ┌ Istambul ...        ┐ | [Mudanças pendentes nesta cidade  Editar · Descartar]  |  (condicional)
| …                      | [DISTÂNCIA 394 km ][EXPIRA EM 24h 48h 72h 7d][UNIDADES 1 ][REBOQUE Rebaixado]|  110 px
|                        | [③ Empresa de saída ▾][④ Carga ▾][⑤ Empresa de chegada ▾] |  56 + dica 18
|                        | ↳ ONDE ENTRA O FRETE  [Usar vaga livre · 3][Substituir · 2]  [Oferta a substituir ▾] |  ~86 px (só com a empresa escolhida)
|                        | [Nenhuma combinação… | Sem vagas livres… | Erro | aviso]  |  (condicional)
|                        |----------------------------------------------------------|
|                        | Falta escolher: carga          [Adicionar à revisão]     |  barra fixa
+------------------------+--------------------------------------------------------+
| Nada é gravado sem revisão...                                      Ctrl+R Revisar |
```

- Coluna esquerda 320 px (lista de cidades de saída, é o passo ①). Coluna direita: o formulário rola na vertical; **a barra de ação fica fixa embaixo**.
- Ordem visual = ordem de Tab = ordem dos passos (ver §6). A linha **Onde entra o frete** pertence ao passo ③ e só aparece depois de escolher a empresa de saída.
- **A distância nunca é digitada**: o tile só mostra o valor (conhecido ou estimado).
- O mapa encolhe (184 → 150 → 128 px) quando há avisos, pendências ou aviso de dependência, para o formulário caber sem rolar; a chip "Rota" some abaixo de 184 px (os nomes já estão nos campos).
- **Estado "nenhuma cidade com vaga livre"**: a lista e o formulário somem e o `EmptyState` ocupa o módulo inteiro (§4.5).

## 2. Componentes

| Parte | Componente | Observações |
| --- | --- | --- |
| Título + contagem | `PageHeader` (dentro da coluna da lista) | "FRETES" + nº de cidades de saída (`+tnum`, `Jobs_List_Count_*` como nome acessível); acima, "① Cidade de saída" |
| Busca | `SearchBox` | 44 px, debounce 250 ms; filtra por prefixo/sem acento no VM; mensagem de resultado anunciada |
| Lista | `OfferList` com **NOVO** `CityListItem` | `role=listbox`, virtualizada; seta ↑↓ move, Enter seleciona |
| Passo ② e ③–⑤ | `SearchablePicker` | campo 56 px; popup `max(campo, 360)` de largura, ≤ 360 de altura; o do destino tem a chave "Só combinações possíveis" (ver §3) |
| Chip de saída | `FieldRow`-like (botão 56 px) | só mostra a cidade escolhida; ativá-lo põe o foco na lista |
| Trocar | botão ghost 44 × 44 com ícone ⇄ | nome `Jobs_Swap`; desabilitado sem as duas cidades |
| Mapa | `RouteMap` (modo esquemático) | halo `RouteGlow` + rota `Accent`; origem vazada, destino cheio; chip "Rota" `Overlay`; distância `Hero` no canto |
| Indicadores | `StatTile` ×4 | Distância (conhecida, ou estimada com `InfoTag` "Estimada" e "≈"), Expira em (`SegmentedChoice` 24 h · 48 h · 72 h · 7 d), Unidades, Reboque |
| Modo | `SegmentedChoice` (2 opções, com contagem) | "Usar vaga livre · n" / "Substituir uma oferta · n"; opção sem itens fica desativada |
| Oferta a substituir | `SearchablePicker` | só no modo substituir; item: "→ empresa · cidade · carga" + "carga · km · tempo restante" |
| Numeração dos passos | **NOVO** `StepBadge` | círculo 16 px com o número do passo |
| Avisos | `ErrorBanner` | Info (sem pasta do jogo), Warning (Steam Cloud, `DismissCommand`), Error (falha ao adicionar) |
| Pendentes | cartão `SectionCard` com linhas + botões neutro/ghost-perigo | |
| Vazio | `EmptyState` (cheio e compacto) | |
| Ação | botão `accent` 48 px | "Adicionar à revisão" / "Atualizar na revisão" |
| Feedback | `Toast` | adicionado (com "Ver revisão"), atualizado, descartado (com "Desfazer") |

### Componentes NOVOS
| Nome | Para quê | Estados |
| --- | --- | --- |
| `CityListItem` | item da lista de cidades: nome 15/600, meta `BodySm` ("2 empresas · 4 vagas livres"), `PendingBadge` "n pendentes" à direita | normal, hover, selecionado (`Selection` + borda `Accent`, nome em `AccentText`), `:focus-visible` (anel interno), sem vaga livre (meta "Sem vagas livres", 80% de opacidade) |
| `StepBadge` | número do passo (1 a 5) ao lado do rótulo do campo | pendente (contorno `BorderStrong`, número `TextMuted`), feito (contorno e número `AccentText`) |
| `InfoTag` | etiqueta em pílula (contorno `BorderStrong`, texto 11) para "Estimada" e "Sem combinação": no tile de Distância, nas linhas do seletor de destino | normal; nunca só por cor (sempre texto) |

Os três entram em `design-system` e na Galeria quando implementados. (O `UnitField` do rascunho anterior foi removido: ninguém digita distância.)

## 3. Regras de opções e dependências

| Passo | Lista | Regra |
| --- | --- | --- |
| ① Cidade de saída | cidades com ≥ 1 empresa com vaga livre ou oferta substituível, **ou** com mudança pendente | vaga livre = slot de oferta vazio; oferta = oferta atual da empresa; ambos descontam os já usados por pendências (`jobs:slot:<id>`); meta: "2 empresas · 4 vagas livres · 3 ofertas"; ordena pelo nome traduzido |
| ② Cidade de destino | todas as cidades do save, menos a de saída (desativada, no topo, "Já é a cidade de saída") | **ordem: distância conhecida primeiro, estimada depois** (nome dentro de cada grupo); km à direita ("394 km" ou "≈ 985 km"); etiqueta "Estimada" nas estimadas e "Sem combinação" (tem prioridade) quando não há empresa de saída que combine; chave "Só combinações possíveis" esconde essas |
| ③ Empresa de saída | empresas da cidade de saída com **vaga livre ou oferta substituível** que enviam ≥ 1 carga **aceita por alguma empresa da cidade de destino** (e com modelo de reboque conhecido) | secundário: "{cidade} · {n} vagas livres · {m} ofertas" (ou "Sem vagas livres · {m} ofertas") |
| ③ Onde entra o frete | modo **Usar vaga livre** (≥ 1 vaga livre) e/ou **Substituir uma oferta** (≥ 1 oferta); padrão = vaga livre se houver, senão substituir | contagens nos botões; opção sem itens desativada com dica |
| ③ Oferta a substituir | ofertas atuais da empresa, menos as que já têm pendência | só no modo substituir; uma só = escolhida sozinha |
| ④ Carga | o que a empresa de saída envia e a cidade de destino aceita | secundário: reboque |
| ⑤ Empresa de chegada | empresas da cidade de destino que aceitam a carga | |

**Distância** (não é um passo): vem do app. *Conhecida* = par presente na tabela de distâncias (extraída de `shortest_distance_km` de todas as ofertas dos saves do usuário, em cache); *estimada* = par ausente, valor aproximado marcado "Estimada".

Revalidação ao trocar uma escolha (fluxo 2 de `flows.md`): cada escolha posterior fica se continua válida; senão é limpa; se sobra uma única opção, ela é escolhida sozinha. A tela mostra um aviso (`role=status`):
- "Limpamos empresa de saída, carga: a nova escolha muda as opções." (o modo e a oferta acompanham a empresa) (`Jobs_Notice_Cleared`, nomes por `Jobs_Missing_*`)
- "Carga, Empresa de chegada: só restou uma opção, então ela foi escolhida." (`Jobs_Notice_Replaced`)
- "Limpamos a cidade de destino: ela não pode ser igual à cidade de saída." (`Jobs_Notice_SameCity`)
O aviso some na próxima escolha. Campos escolhidos sozinhos mostram "Única opção, já escolhida" embaixo.

## 4. Telas e estados

### 4.1 Escolher frete (`Escolher.dc.html`)
Conteúdo do §1. Estados da prévia e o que mostram:
| Estado | Mapa | Distância | Passos 3–5 | Botão |
| --- | --- | --- | --- | --- |
| Só a saída | nó de saída + nó de destino tracejado + "Escolha a cidade de destino" | "—" · "Escolha as duas cidades" | desabilitados ("Escolha as duas cidades primeiro") | desabilitado, "Falta escolher: cidade de destino" |
| Duas cidades | rota, chip "Rota" com as cidades, distância no canto | valor conhecido ou estimado (§4.2) | ③ habilitado ("2 opções"); ④ e ⑤ desabilitados | desabilitado, lista o que falta |
| Empresa / carga / chegada abertas | idem | idem | popup do campo (aberto para cima) | idem |
| Pronto | chip "Rota": "{empresa} · {cidade} → {empresa} · {cidade}" | idem | os três preenchidos | habilitado, "Pronto. Nada é gravado agora: o frete vai para a revisão." |
| Adicionado | idem | idem | ③–⑤ limpos; cidades ficam | desabilitado; Toast "Frete adicionado à revisão" (Ver revisão); selo "Revisar (n)" sobe; cartão de pendentes da cidade aparece |
| Erro ao adicionar | idem | idem | os preenchidos ficam | habilitado; `ErrorBanner` Error: `Error_Title_Edit` + `Jobs_Error_*` + código mono; "Fechar" |

Reboque e unidades aparecem assim que a carga é escolhida (copiados de um frete do save com a mesma carga: `JobBoard.Template`).

### 4.2 Distância conhecida e estimada (`DistanciaEstimada.dc.html`)
O usuário **nunca digita a distância nem monta a rota**. O app a fornece assim que as duas cidades estão escolhidas:
```
 DISTÂNCIA                          DISTÂNCIA
 394 km                             ≈ 1.330 km  [Estimada]
 Conhecida: vem de ofertas          É uma estimativa do app: seus saves não trazem
 dos seus saves                     esta rota. O jogo recalcula a rota no GPS.
```
| Estado | Tile Distância | Mapa (canto) | Seletor de destino |
| --- | --- | --- | --- |
| Conhecida | "394 km" + "Conhecida: vem de ofertas dos seus saves" | "394 km" | linha com "394 km", sem etiqueta |
| Estimada | "≈ 1.330 km" + etiqueta **Estimada** + explicação curta ("É uma estimativa do app: seus saves não trazem esta rota. O jogo recalcula a rota no GPS.") | "≈ 1.330 km · estimada" | linha com "≈ 985 km" e etiqueta "Estimada", **depois** das conhecidas |
| Sem as duas cidades | "—" + "Escolha as duas cidades" | — | — |
- A estimativa não é um erro nem bloqueia nada: o botão não depende da distância. "≈" e a etiqueta garantem que ninguém a confunda com um valor do save.
- A distância estimada é gravada em `shortest_distance_km`; na revisão aparece como "≈ 1.330 km" (`Format_KmApprox`).
- Seletor de destino: as cidades sem distância conhecida são **marcadas** (etiqueta) e **ordenadas depois** das conhecidas; a cidade de saída fica sempre no topo, desativada. "Sem combinação" tem prioridade sobre "Estimada" na etiqueta (o "≈" continua).
- A tabela de distâncias fica na `Data` (cache): extrai `shortest_distance_km` de todas as ofertas dos saves do usuário; até estar pronta, vale a do save aberto (mais pares estimados). Formata pela cultura ("1.330" / "1,330").

### 4.3 Sem a pasta do jogo (`SemCatalogo.dc.html`)
Aviso `ErrorBanner` Info no topo do formulário (72 px): "Pasta do Euro Truck Simulator 2 não encontrada" + "Mostramos os nomes como estão no save e só as regras de carga que ele revela. Pode haver menos opções." + botão **Abrir Configurações** (vai para a seção "Jogos" de `design/shell/screens.md` §6; volta ao módulo com Esc/Ctrl+1). Não bloqueia nada.
- Nomes por id legível: "Beograd", "Concr beams2", "Scs flat b"; a lista ordena por esse texto.
- Menos opções: só entram empresas e cargas que o save revela (`JobBoard` = catálogo ∪ regras do save; sem catálogo, só as regras do save). O exemplo mostra uma empresa de saída a menos e duas cargas em vez de cinco.
- "Sem combinação" fica mais comum; a mensagem acrescenta "Sem a pasta do jogo, valem só as regras de carga vistas no save." (`Jobs_NoMatch_CatalogNote`).
- Ao escolher a pasta nas Configurações, o módulo revalida as escolhas (nomes e regras novos) sem perder as pendências.

### 4.4 Cidades e dependências (`Cidades.dc.html`)
- **Seletor de destino**: popup 384 px, busca, "14 opções", chave "Só combinações possíveis", linhas de 48 px: nome, km à direita ("394 km" conhecido; "≈ 985 km" estimado, com etiqueta "Estimada"), etiqueta "Sem combinação"; conhecidas antes das estimadas. A cidade de saída vem primeiro, desativada (`aria-disabled`, "Já é a cidade de saída").
- **Cidades iguais**: ao escolher na lista a cidade que já é o destino, o destino é limpo e aparece o aviso `Jobs_Notice_SameCity`. No seletor não há como escolhê-la.
- **Sem combinação**: cartão tracejado "Nenhuma combinação possível" + "Nenhuma empresa de {saída} com vaga livre envia uma carga que alguma empresa de {destino} aceite. Troque uma das cidades." + [Trocar cidade de destino] (abre o seletor já com "Só combinações possíveis" ligado) e [Trocar cidade de saída] (foco na lista). Os passos ③–⑤ somem; o mapa e a distância continuam.
- **Trocar saída / empresa / destino**: mostram o aviso de limpeza (§3) com o que mudou.

### 4.5 Nenhuma cidade com vaga ou oferta (`SemOrigem.dc.html`)
| Estado | Tela |
| --- | --- |
| Save sem vagas nem ofertas | lista e formulário somem; `EmptyState` cheio no módulo: ícone, "Nenhuma cidade com vaga ou oferta", "Uma cidade só aparece aqui se uma empresa dela tiver uma vaga livre ou uma oferta para substituir e enviar uma carga que o app já viu num frete deste save.", dica "Abra o mercado de fretes de algumas empresas no jogo, salve e abra o save de novo.", ação **Escolher outro save** |
| Busca sem resultado | só a lista mostra `EmptyState` compacto: "Nenhuma cidade encontrada" / "Nada para “{termo}”. Confira o nome ou limpe a busca." / **Limpar busca**; o formulário continua |
| Só ofertas (vagas em uso) | a cidade fica na lista com "Sem vagas livres · n ofertas"; o formulário funciona no modo **Substituir** (a opção livre fica desativada). Só quando **nem ofertas** sobram é que aparece "Sem vaga livre nem oferta" (§4.6) |

### 4.6 Mudança pendente para o slot (`MudancaPendente.dc.html`)
Ao escolher uma cidade de saída com pendências aparece o cartão **Mudanças pendentes nesta cidade** (até 2 linhas visíveis; a revisão lista tudo):
```
 MUDANÇAS PENDENTES NESTA CIDADE
 Radus · Sófia → Tradeaux · Belgrado                       [Editar] [Descartar]
 Maquinário · 394 km · 72 h · Vaga livre            (ou "· Substitui oferta")
```
| Estado | O que acontece |
| --- | --- |
| Pendente na cidade | cartão + formulário vazio para outro frete (próxima vaga livre ou outra oferta); o selo da cidade na lista mostra "1 pendente" |
| Editando (vaga livre) | **Editar = reenfileirar com a mesma `Key`** (substitui a anterior, sem operação nova). A linha editada fica realçada e o cartão de pendentes some para dar lugar ao formulário; faixa "Editando uma mudança pendente / A empresa e a vaga ficam as mesmas. Ao atualizar, ela substitui a anterior na revisão." + [Cancelar edição]; os 5 passos vêm preenchidos; botão "Atualizar na revisão". **Ficam fixos** (desativados, "Fixa na edição"): a lista de cidades de saída, o chip de saída, trocar saída/destino, a empresa de saída e o modo (e a oferta). **Editáveis**: cidade de destino (só cidades que a empresa fixa atende), carga, empresa de chegada e prazo. Para mudar o que está fixo: descartar e criar outra |
| Editando (substituição) | igual; a oferta a substituir também fica fixa (mostra a oferta) |
| Descartada | Toast "Mudança descartada." + **Desfazer** (6 s, `role=status`); Desfazer re-enfileira o `ChangeSet` guardado |
| Nada livre | cidade na lista com "Sem vagas livres" e "n pendentes"; sem vaga livre **nem oferta**, os indicadores e os passos ③–⑤ somem; cartão tracejado "Sem vaga livre nem oferta em {cidade}" + "Todas as vagas e ofertas de {cidade} já têm uma mudança pendente. Edite ou descarte uma delas para liberar."; o botão fica desabilitado |
A edição de uma pendente de **outra** cidade pede escolher a cidade dela na lista (o cartão só mostra as da cidade escolhida; a revisão é o lugar de ver todas).

### 4.7 Perfil com Steam Cloud (`SteamCloud.dc.html`)
- Chip "Steam Cloud" (ícone de nuvem) na barra de sessão, sempre visível com a sessão aberta, `ToolTip` = título do aviso, nome acessível "Este perfil usa Steam Cloud".
- Aviso `ErrorBanner` Warning (`Selection` + borda `AccentText`, ícone ▲) no topo do formulário: "Este perfil usa Steam Cloud" + "A nuvem pode trazer de volta um save antigo ou esconder saves novos. Ao aplicar, você escolhe como gravar." + [Entendi] (`DismissCommand`). Dispensado fica só nesta sessão; o chip continua.
- Não muda nada no fluxo de montar o frete.

### 4.8 Onde entra o frete (`ModoFrete.dc.html`)
Linha abaixo dos três campos, ligada ao passo ③ (só aparece com a empresa escolhida):
```
 ↳ ONDE ENTRA O FRETE                              OFERTA A SUBSTITUIR   (só no modo substituir)
 [ Usar vaga livre · 3 ] [ Substituir uma oferta · 2 ]  [→ LogNStick · Istambul · Vigas ▾]
 Substituir troca uma oferta atual…                     2 ofertas
```
| Estado | Botões | Dica | Seletor de oferta |
| --- | --- | --- | --- |
| Vaga livre (padrão) | livre selecionado (âmbar cheio), substituir disponível | "Substituir troca uma oferta atual da empresa: a revisão mostra antes → depois." | oculto |
| Substituir | substituir selecionado | idem | visível; popup com as ofertas ("→ empresa · cidade · carga", "carga · km · tempo restante"); "n ofertas" ou "Única opção, já escolhida" |
| Sem vaga livre | livre **desativado** ("· 0"), substituir selecionado sozinho | "Sem vaga livre nesta empresa. Substitua uma das ofertas atuais." | visível |
| Sem ofertas | substituir **desativado** ("· 0"), livre selecionado | "Esta empresa não tem ofertas para substituir. Use a vaga livre." | oculto |
| Editando | os dois desativados (o selecionado fica realçado) | "Fixo na edição: para mudar, descarte esta mudança e crie outra." | fixo |
- Falta a oferta: o botão fica desabilitado com "Falta escolher: oferta a substituir, …".
- Trocar a empresa de saída volta o modo ao padrão da nova empresa e limpa a oferta.
- Na revisão, o `ChangeSet` de "substituir" mostra **antes → depois** dos campos da oferta (destino, carga, distância, reboque, prazo); o de vaga livre mostra "—" no antes.
- Acessibilidade: `radiogroup` "Onde entra o frete"; opção desativada = `aria-disabled` + a dica como descrição; o seletor segue o padrão de `SearchablePicker`.

## 5. Estados por região (os quatro)
| Região | Carregando | Vazio | Erro | Conteúdo |
| --- | --- | --- | --- | --- |
| Lista de cidades | `LoadingPanel` compacto "Atualizando as opções de frete…" só ao refazer o `JobBoard` (pasta do jogo trocada); a carga normal está na carga do save | `EmptyState` (nenhuma cidade / busca sem resultado) | — (o board nunca derruba a sessão) | lista |
| Seletor de destino | `LoadingPanel` compacto no popup | "Nada para escolher aqui." / "Nada encontrado para “{termo}”" | — | linhas |
| Passos ③–⑤ | — | desabilitados com dica; "Nenhuma combinação possível" | `ErrorBanner` Error (`Jobs_Error_*`) | preenchidos |
| Mapa | — | "Escolha a cidade de saída na lista" / "Escolha a cidade de destino" | — | rota |

## 6. Teclado e foco

Ordem de Tab (um Tab por grupo): navegação do shell → Perfil → Save → Revisar → **busca** → **lista de cidades** (setas movem, Enter escolhe) → chip de saída (ativar = foco na lista) → trocar → **cidade de destino** → cartão de pendentes (Editar, Descartar por linha) → **prazo** (`radiogroup`, setas movem; a distância não é campo) → **empresa de saída** → **carga** → **empresa de chegada** → **onde entra o frete** (`radiogroup`) → **oferta a substituir** (se substituir) → aviso (Fechar) → **Adicionar à revisão**.
- Foco inicial do módulo: a lista (ou a busca se houver texto). Depois de escolher em um seletor o foco volta ao campo; depois de completar um passo, o foco vai para o campo do **próximo passo incompleto** (sem abrir o popup).
- Seletores: `Enter`/`Espaço`/`Alt+↓` abrem; digitar abre e busca; `↑`/`↓` movem o realce mesmo com o foco na busca; `Enter` escolhe e fecha; `Esc` fecha e devolve o foco ao campo (nunca cancela a edição nem sai do módulo); `Tab` fecha sem escolher.
- Sem atalho próprio no módulo; Ctrl+R, Ctrl+O, Ctrl+, e Ctrl+1…5 continuam do shell. Nenhum atalho dispara "Adicionar à revisão" (evita confundir com Ctrl+Enter da revisão).
- Linhas desativadas do seletor (cidade de saída) e controles fixos da edição ficam fora das setas e do Tab.

## 7. Nomes acessíveis
| Controle | Nome | Chave |
| --- | --- | --- |
| Módulo | Fretes (`main`) | `Module_jobs` |
| Lista | Cidades de saída (`listbox`; item `aria-selected`) | `Jobs_List_Title` |
| Contagem | "10 cidades de saída" | `Jobs_List_Count_*` |
| Chip de saída | "Cidade de saída: Sófia. Mude na lista" | `Jobs_Chip_FromAria` |
| Trocar | Trocar saída e destino | `Jobs_Swap` |
| Cidade de destino | "Cidade de destino: Belgrado. Mudar" / "Escolher cidade…" (`aria-haspopup=listbox`, `aria-expanded`) | `Jobs_Chip_ToAria` |
| Passos 3–5 | "Empresa de saída: Radus · Sófia" / "…: Escolher…" (`aria-disabled` quando bloqueado; a dica sob o campo é a descrição) | `Jobs_Step_*` |
| Mapa | "Rota de Sófia até Belgrado" (`img`) / "Mapa da rota: escolha as cidades" | `Jobs_Map_Label`, `Jobs_Map_Empty_Aria` |
| Distância | StatTile "Distância: 394 km. Conhecida" / "Distância: aproximadamente 1.330 km. Estimada" (a etiqueta e o "≈" entram no nome) | `Jobs_Field_Distance`, `Jobs_Distance_Estimated` |
| Onde entra o frete | `radiogroup` "Onde entra o frete": "Usar vaga livre · 3" / "Substituir uma oferta · 2" (`aria-disabled` + dica) | `Jobs_Mode_*` |
| Oferta a substituir | `SearchablePicker` "Oferta a substituir: → LogNStick · Istambul · Vigas de concreto" | `Jobs_Step_Offer` |
| Prazo | grupo "Expira em" com 4 `radio` ("24 h"…"7 d") | `Jobs_Field_Expiration` |
| Pendentes | grupo "Mudanças pendentes nesta cidade"; botões "Editar mudança: {título}" / "Descartar mudança: {título}" | `Jobs_Pending_EditAria`, `Review_DiscardItem_Aria` |
| Avisos | `role=status` (limpeza, nuvem, editando); erro `role=alert` | `Jobs_Notice_*`, `Error_Title_Edit` |
| Adicionar | botão; desabilitado → `aria-disabled` + texto "Falta escolher: …" ligado por `aria-describedby`; na edição "Atualizar na revisão" | `Jobs_Add`, `Jobs_Update`, `Jobs_Missing` |
Contraste: `TextMuted` ≥ 4,5:1 nos dois temas; âmbar só em seleção, rota e ação principal; "Estimada", "Sem combinação", pendente e erro nunca só por cor (texto e ícone junto). Campos desabilitados usam opacidade 0,5 com a dica em texto (não dependem da cor).

## 8. Light/Dark, pt-BR/en, textos longos
- Todos os canvases funcionam nos dois temas e idiomas (barra de prévia). Em inglês, "Departure company" / "Arrival company" são os rótulos mais longos dos campos de 270 px e cabem; em pt-BR as mensagens são as mais longas.
- Nomes de mods: cidade e empresa com reticências e `ToolTip` com o nome inteiro (lista, campos, popup, chip "Rota" até 62% da largura); reboque em uma linha com reticências; listas com centenas de itens virtualizam.
- Números (km, unidades, contagens) em `FontDisplay` `+tnum`, pela cultura ("1.250 km" / "1,250 km").
- Janela mínima 960 px: a coluna da lista fica em 320; o formulário rola; os indicadores passam de 4 para 2 colunas abaixo de 1100 px; os três campos viram uma coluna abaixo de 900 px de largura útil.
