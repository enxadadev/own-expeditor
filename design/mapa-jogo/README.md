# Mapa do jogo (módulo Fretes)

- **Status:** aprovado (2026-11-18).
- **Canvas:** pendente; os mockups `.dc.html` serão feitos depois que a direção (fases) for aprovada.
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
