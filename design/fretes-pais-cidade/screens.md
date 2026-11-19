# Fretes País → Cidade — telas

Janela 1280×800 (mínimo 960×600), dentro do shell aprovado (`design/shell/screens.md` §0: barra de sessão de 60 px, rodapé de 36 px). Substitui o layout de `design/fretes/screens.md` §1–§3 (lista de cidades à esquerda, chip de saída, três campos em linha e "Onde entra o frete"). O resto continua valendo: distância conhecida/estimada (§4.2), sem pasta do jogo (§4.3), sem origem (§4.5), pendentes (§4.6), Steam Cloud (§4.7), erros ao adicionar e teclado básico. Textos: [strings.md](strings.md). Mockups: [canvas/](canvas/).

## 1. Estrutura da tela (`Escolher.dc.html`)

```
+- barra de sessão (shell) -------------------------------------------------------------+
| [Perfil ▾][Save ▾] ETS2 ● Jogo fechado [Steam Cloud]*                   [Revisar (1)] |
+------------------------------------------------------------------------------------+
|        [aviso Info: sem pasta do jogo ................ Abrir Configurações]*         |
|        [aviso Warning: Steam Cloud ...................... Entendi]*                   |
|        [faixa: Editando uma mudança pendente ........... Cancelar edição]*            |
|        +-① SAÍDA ----------------------+  [⇄]  +-② CHEGADA ---------------------+    |
|        | País    [Bulgária           ▾]  |      | País    [Sérvia              ▾] |    |
|        | Cidade  [Sófia              ▾]  |      | Cidade  [Belgrado            ▾] |    |
|        | Empresa [Radus   Única opção ▾] |      | Empresa [Tradeaux            ▾] |    |
|        | Vaga    [Vaga livre (3)      ▾]*|      +---------------------------------+    |
|        +---------------------------------+                                            |
|        [Limpamos cidade de chegada, empresa de chegada, carga: …]*      (aviso)       |
|        [Mudanças pendentes nesta cidade ...... Editar · Descartar]*                    |
|        +-③ CARGA ----------------------------------------------------------------+   |
|        | [Maquinário                                                            ▾] |   |
|        | [EXPIRA EM  24 h 48 h 72 h 7 d] [UNIDADES 1] [REBOQUE Rebaixado]*         |   |
|        +---------------------------------------------------------------------------+   |
|        +- MAPA DA ROTA (último bloco, 300 px, largura total) ----------------------+   |
|        | [ROTA Radus · Sófia → Tradeaux · Belgrado]                                |   |
|        |            ◯━━━━━━━━━━╮                                                   |   |
|        |                       ╰━━━━━━━━━━━━●                      [394 km]        |   |
|        | Esquema da rota, sem posições reais                                       |   |
|        +---------------------------------------------------------------------------+   |
|------------------------------------------------------------------------------------|
|        Falta escolher: carga                                [Adicionar à revisão]     |  fixa
+------------------------------------------------------------------------------------+
| Nada é gravado sem revisão. Um backup é criado antes.                  Ctrl+R Revisar |
```
`*` = condicional (aviso Info só sem pasta do jogo; Steam Cloud só com o perfil; faixa só editando; Vaga só com 2+ opções; avisos de limpeza só depois de uma troca; pendentes só se a cidade de saída tem; detalhes só com carga).

- **Sem coluna da esquerda**: a página é um formulário centralizado, largura 960 (`max-width`), rola na vertical; **a barra de ação fica fixa embaixo**.
- Ordem visual = ordem de Tab = ordem da cadeia (§5). Dois cartões com os **mesmos campos**; o ⇄ fica entre eles (44 × 44).
- Alturas: campo 42 px (rótulo à esquerda, 76 px), cartão com 3 campos ~190 px (~238 px com Vaga), cartão de carga ~110 px (~190 px com detalhes), **mapa 300 px** (faixa recomendada 280–320). A página **rola**: os formulários ocupam a primeira tela e o mapa é visto rolando até o fim; a barra de ação continua fixa.
- **Mapa por último, e grande**: outra frente implementa o mapa real do jogo neste bloco; o desenho só reserva o espaço (largura total do formulário, 300 px) com o esquema atual como placeholder. A distância (conhecida ou "≈ estimada" com `InfoTag`) fica sobre o mapa, no canto inferior direito; o chip "Rota" (empresas e cidades) no canto superior esquerdo; "Esquema da rota, sem posições reais" no canto inferior esquerdo, **só como nota do esquema** (some quando o mapa real entrar).
- Abaixo de 1100 px de largura útil o rótulo passa para cima do campo e os dois cartões empilham (Saída em cima, ⇄ vira "Trocar" entre eles).

## 2. Componentes

| Parte | Componente | Observações |
| --- | --- | --- |
| Cartões Saída, Chegada e Carga | `SectionCard` + `StepBadge` (①②③) | `StepBadge` fica "feito" (âmbar) quando o cartão está completo |
| Campos País, Cidade, Empresa, Vaga, Carga | `SearchablePicker` (42 px, rótulo em linha) | um componente só para os 8 campos; ver §3 |
| Etiqueta "Única opção" / "Travado na edição" / "Estimada" | `InfoTag` | texto sempre, nunca só cor; "Travado" troca a seta por um cadeado |
| Trocar | botão ghost 44 × 44, ícone ⇄ | nome `Jobs_Swap`; desabilitado sem as duas cidades ou editando |
| Mapa da rota (último bloco) | `RouteMap` (modo esquemático hoje; mapa real depois), 300 px, largura total, + distância `Hero` + `InfoTag` Estimada | distância sobre o mapa; nota "Esquema da rota, sem posições reais" só no modo esquemático; antes das cidades: "Escolha as duas cidades" |
| Prazo | `SegmentedChoice` (24 h · 48 h · 72 h · 7 d) | dentro do tile Expira em |
| Unidades, Reboque | `StatTile` | só com carga; Distância deixou de ser tile (vai sobre o mapa) |
| Pendentes | `SectionCard` com linhas, botões neutro e ghost-perigo | como em `fretes` §4.6; logo abaixo dos cartões |
| Avisos | `ErrorBanner` (Info, Warning), `role=status` para limpeza | |
| Ação | botão `accent` 48 px | "Adicionar à revisão" / "Atualizar na revisão" |
| Feedback | `Toast` | adicionado (com "Ver revisão"), atualizado, descartado (com "Desfazer") |

Não há componente **NOVO** neste desenho. `StepBadge` e `InfoTag` já foram aprovados em `design/fretes/`. `CityListItem` e o chip de saída deixam de ser usados (a lista de cidades saiu); o `ModoFrete` (segmentado) também sai, trocado pelo campo Vaga.

## 3. Campos, opções e dependências

Todo campo é um `SearchablePicker` com busca, contagem ("8 opções") e popup de 360 px (até 7 linhas visíveis; mais que isso rola). Opção única é escolhida sozinha e mostra "Única opção"; sem opções o popup diz "Nada para escolher aqui.".

| Campo | Lista | Linha do popup | Ordem |
| --- | --- | --- | --- |
| ① País | países com ao menos uma cidade de saída (vaga livre, oferta ou pendência) | nome do país + "n cidades" à direita | nome na cultura atual |
| ① Cidade | cidades do país escolhido (ou todas, sem país) | nome; meta "n empresas · n vagas livres · n ofertas" (ou "Sem vagas livres"); selo âmbar "n pendente(s)" à direita | nome |
| ① Empresa | empresas da cidade com vaga livre ou oferta e que enviam ao menos uma carga aceita em outra cidade | nome; meta "n vagas livres · n ofertas" | nome |
| ① Vaga | "Vaga livre (n)" se houver, mais uma linha por oferta a substituir | "Substituir: → Belgrado · Maquinário · 394 km" e "Tradeaux · Belgrado · expira em 10 h" | vaga livre primeiro; ofertas na ordem do save |
| ② País | países com empresa que aceita ao menos uma carga enviada pela empresa de saída | nome + "n cidades" | nome |
| ② Cidade | cidades do país (≠ cidade de saída) com empresa que aceita | nome; meta "n empresas"; km à direita ("394 km" ou "≈ 985 km" + `InfoTag` Estimada) | conhecidas primeiro, estimadas depois; nome dentro de cada grupo |
| ② Empresa | empresas da cidade que aceitam ao menos uma carga da saída | nome; meta com as cargas em comum ("Vigas de concreto, Maquinário") | nome |
| ③ Carga | o que a empresa de saída envia **e** a de chegada aceita | nome; reboque na meta | nome |

- **Vaga**: só aparece com 2+ opções (vaga livre conta uma; cada oferta substituível conta uma, já descontadas as pendências). Com 1 opção ela é usada sem aparecer. Linha "Vaga livre (n)" é a padrão quando existe; sem vaga livre o campo fica vazio. Na edição fica travada.
- **Campo desabilitado** mostra a dica no lugar do valor ("Escolha o país primeiro", "Escolha a cidade primeiro", "Escolha a empresa de saída primeiro", "Escolha a empresa de chegada primeiro"), com opacidade 0,55 e `aria-disabled`.
- **Sem país** (sem pasta do jogo): País①/② somem (`display none`), Cidade lista tudo e os campos seguintes se habilitam pela cidade.
- A distância é **conhecida** ou **estimada** como em `design/fretes/` §4.2 e nunca digitada; a estimativa aparece em "≈ n km" com a etiqueta "Estimada" no seletor e sobre o mapa.
- Nomes (país, cidade, empresa, carga, reboque) vêm do jogo, traduzidos (`IGameNames`; país com kind `Country`); sem pasta do jogo, país não aparece e os demais usam o id legível.

## 4. Telas e estados

### 4.1 Escolher frete (`Escolher.dc.html`)
| Estado da prévia | O que mostra | Botão |
| --- | --- | --- |
| Vazio | ① País habilitado, Cidade e Empresa desabilitados com dica; ② tudo desabilitado ("Escolha a empresa de saída primeiro"); mapa (no fim da página) "Escolha as duas cidades" | desabilitado, "Falta escolher: saída, chegada, carga" |
| País escolhido | Cidade habilitada ("Escolha a cidade"), Empresa desabilitada ("Escolha a cidade primeiro") | desabilitado, "Falta escolher: cidade de saída, empresa de saída, …" (agrupa em "saída, chegada, carga" quando passa de 3) |
| Cidade | Empresa habilitada ("Escolha a empresa") | idem |
| Empresa | aparece Vaga com "Vaga livre (3)"; ② País habilitado | desabilitado, lista o que falta |
| Chegada | ② País e Cidade escolhidos; o mapa (último bloco) mostra a rota e a distância; Empresa de chegada pendente | desabilitado |
| Carga · Pronto | ② completo; Carga escolhida; detalhes (Expira em · Unidades · Reboque) | habilitado, "Pronto. Nada é gravado agora: o frete vai para a revisão." |
| Adicionado | Toast "Frete adicionado à revisão." (Ver revisão); selo "Revisar (1)"; cartão de pendentes; Vaga, Empresa de chegada e Carga limpas; países, cidades e empresa de saída ficam | desabilitado |

### 4.2 Seletores (`Seletores.dc.html`)
Variações: **País (saída)** e **País (chegada)** (lista com "n cidades"), **Cidade de saída** (meta empresas · vagas · ofertas, selo de pendente, "Sem vagas livres"), **Cidade de chegada** (distância conhecida primeiro; "≈ n km" com "Estimada" depois), **Vaga** (Vaga livre (n) + ofertas a substituir), **Única opção** (país com uma cidade: Cidade e Empresa já escolhidas com a etiqueta; Vaga sem padrão aberta) e **Campos desabilitados** (dica no lugar do valor).

### 4.3 Estados (`Estados.dc.html`)
| Estado | O que muda |
| --- | --- |
| Sem pasta do jogo | aviso Info (72 px) "Pasta do Euro Truck Simulator 2 não encontrada"; País somem; nomes por id ("Beograd", "Rock eat"); menos empresas e cargas (só regras do save) |
| Editando pendente | faixa "Editando uma mudança pendente"; País, Cidade, Empresa e Vaga de saída travados (cadeado, "Travado na edição", ⇄ desabilitado); chegada e carga editáveis; botão "Atualizar na revisão" |
| Pendentes na cidade | cartão "Mudanças pendentes nesta cidade" (até 2 linhas) com Editar e Descartar; formulário livre para outro frete; Vaga já desconta o que as pendências usam |
| Troca que limpa | aviso "Limpamos cidade de chegada, empresa de chegada, carga: a nova escolha muda as opções." |
| Troca: só uma opção | aviso "Carga: só restou uma opção, então ela foi escolhida." |
| Steam Cloud | chip "Steam Cloud" na barra de sessão e aviso Warning "Este perfil usa Steam Cloud" com "Entendi" |

### 4.4 Estados por região (os quatro)
| Região | Carregando | Vazio | Erro | Conteúdo |
| --- | --- | --- | --- | --- |
| Campos | o `JobBoard` é montado na abertura do save; ao refazer (pasta do jogo trocada) um `LoadingPanel` compacto "Atualizando as opções de frete…" cobre os cartões | popup "Nada para escolher aqui."; campo desabilitado com dica | `ErrorBanner` de erro ao adicionar (`Jobs_Error_*`), campos ficam | valores |
| Mapa da rota | o mapa real, quando entrar, mostra seu próprio carregamento | "Escolha as duas cidades" | — | rota + km |
| Pendentes | — | cartão não aparece | — | linhas |

## 5. Teclado e foco

Ordem de Tab (um Tab por campo): navegação do shell → Perfil → Save → Revisar → **País①** → **Cidade①** → **Empresa①** → **Vaga**\* → **⇄** → **País②** → **Cidade②** → **Empresa②** → pendentes (Editar, Descartar por linha)\* → **Carga** → **prazo** (`radiogroup`, setas) → aviso (Fechar)\* → **Adicionar à revisão**.
- Foco inicial: o primeiro campo vazio e habilitado. Ao completar um campo, o foco vai para o **próximo campo incompleto**, sem abrir o popup.
- Seletores: `Enter`/`Espaço`/`Alt+↓` abrem; digitar abre e busca; `↑`/`↓` movem o realce; `Enter` escolhe e fecha; `Esc` fecha e devolve o foco (nunca cancela a edição nem sai do módulo); `Tab` fecha sem escolher.
- Sem atalho próprio; Ctrl+R, Ctrl+O, Ctrl+, e Ctrl+1…5 continuam do shell. Nenhum atalho dispara "Adicionar à revisão".
- Campos travados e desabilitados ficam fora do Tab e das setas.

## 6. Nomes acessíveis

| Controle | Nome | Chave |
| --- | --- | --- |
| Cartão | grupo "Saída" / "Chegada" / "Carga" | `Jobs_Side_From`, `Jobs_Side_To`, `Step_Cargo` |
| Campo | "Saída, País: Bulgária" / "Chegada, Cidade: Escolha a cidade" (`aria-haspopup=listbox`, `aria-expanded`; `aria-disabled` com a dica como valor) | `Jobs_Field_Aria` + `Jobs_Field_*` |
| Etiqueta | "Única opção", "Travado na edição" lidas junto ao valor | `Jobs_Single_Option`, `Jobs_Hint_Locked` |
| Trocar | Trocar saída e chegada | `Jobs_Swap` |
| Rota | "Rota de Sófia até Belgrado" (`img`) / "Mapa da rota: escolha as duas cidades" | `Jobs_Map_Label`, `Jobs_Map_Empty_Aria` |
| Prazo | grupo "Expira em" com 4 `radio` | `Jobs_Field_Expiration` |
| Avisos de limpeza | `role=status`, anunciado | `Jobs_Notice_*` |
| Adicionar | desabilitado → `aria-disabled` + "Falta escolher: …" por `aria-describedby` | `Jobs_Add`, `Jobs_Update`, `Jobs_Missing` |

Contraste: `TextMuted` ≥ 4,5:1 nos dois temas; âmbar só em seleção, rota e ação principal; etiquetas nunca só por cor.

## 7. Light/Dark, pt-BR/en, textos longos

- Os três canvases funcionam nos dois temas e idiomas (barra de prévia). Em inglês, "Departure"/"Arrival" são mais longos que "Saída"/"Chegada" mas cabem; em pt-BR a mensagem do aviso de pasta do jogo e as dicas são as mais longas (a dica de campo desabilitado ocupa uma linha com reticências).
- Países, cidades e empresas de mod: reticências com `ToolTip` do nome inteiro (campo, popup, chip "Rota" até 250 px).
- Números (km, unidades, contagens) em `FontDisplay` `+tnum`, pela cultura ("1.250 km" / "1,250 km").
- Listas longas (centenas de cidades com mods): o popup é virtualizado; a contagem e a distância só são calculadas para as linhas visíveis.
