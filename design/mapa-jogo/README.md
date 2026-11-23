# Mapa do jogo (módulo Fretes)

- **Status:** Fase 1 implementada; canvas pendente (2026-11-18).
- **Canvas:** pendente. Os estados da Fase 1 ainda não têm canvas (ver exceção abaixo); os mockups `.dc.html` das fases seguintes serão feitos depois da direção aprovada.
- **Origem:** [ADR 0007](../../docs/decisions/0007-mapa-do-jogo-com-rota-gps.md) (aceita); substitui o mapa esquemático de [`../controles/`](../controles/README.md) §2.14 dentro de [`../fretes/`](../fretes/README.md).

## Objetivo do usuário
Ver, ao montar um frete, **onde** ficam a cidade de saída e a de destino no mapa
de verdade do jogo (com os mods que ele usa) e, na fase final, **por onde** o
caminhão vai, como o GPS do jogo mostra.

## Entrega em fases (cada fase é útil sozinha)
| Fase | O que o usuário vê | Fonte dos dados |
| --- | --- | --- |
| 0 (hoje) | Duas bolinhas e uma curva (esquemático) | nada |
| 1 | Cidades nas posições reais, cidades de fundo, linha reta saída → destino | itens City dos setores do mapa (instalação + mods ativos) |
| 2 | + malha de estradas ao fundo, zoom e arrastar | itens Road/Node dos setores |
| 3 | + rota de GPS desenhada sobre as estradas, distância calculada | grafo de estradas + prefabs |

Sem a pasta do jogo, com versão de mapa não suportada ou enquanto o índice
ainda está sendo montado, o mapa volta ao esquemático (Fase 0) — o frete nunca
fica bloqueado pelo mapa.

## Decisões de UX
- O mapa continua sendo uma **ajuda visual**; nenhum passo do frete depende dele.
- Primeira montagem do índice: aviso discreto sobre o mapa ("Preparando o mapa do jogo…") com progresso; o resto da tela funciona.
- Fase 1 desenha a linha reta **tracejada** para não parecer rota real; a rota sólida só aparece na Fase 3.
- Zoom/arrastar (Fase 2+): roda do mouse, arrastar com botão esquerdo, `+`/`-`/`0` no teclado com foco no mapa, botão "Centralizar rota". Ao trocar saída/destino, o mapa enquadra os dois pontos.
- Cores só por tokens (`MapRoadColor`, `MapRoadMajorColor` novos; demais de `controles` §1.1). Âmbar só na rota e nas duas pontas.
- Nenhum asset do jogo (texturas, ícones) é mostrado: estradas e cidades são desenhadas pelo app a partir das geometrias.

Documentos: [flows.md](flows.md) · [screens.md](screens.md) · [strings.md](strings.md).

## Exceção de processo: Fase 1a antes do canvas
- O texto dos desenhos (`flows.md`, `screens.md`, `strings.md`) foi aprovado pelo usuário.
- Com isso, o usuário autorizou a implementação da Fase 1a antes do canvas. A exceção vale só para a Fase 1a e só para o que o texto aprovado cobre.
- **Pendência:** o canvas dos estados da Fase 1 (com par de cidades, sem pasta do jogo, erro, vazio e carregando) fica para o `ux-designer` antes da Fase 2. Até lá, os estados seguem o texto aprovado em `screens.md` e `strings.md`.

## Referência visual (2026-11-18)
O usuário quer o mapa igual ao de seleção de frete do jogo: fundo escuro, contornos e nomes de países, pontos de cidade, bandeiras de saída/chegada e zoom/arrastar. A rota usa a **cor de destaque do app (âmbar/mostarda, `AccentColor` + `RouteGlowColor`)**, não o vermelho do jogo. Isto substitui o foco em malha de estradas das fases 2/3 do ADR 0007; a investigação de texturas de UI decide como desenhar os países.
