# Mapa do jogo — telas e estados

Mesmo lugar e tamanho do `RouteMap` atual em `JobsView` (painel direito do módulo Fretes). Chips de rota e distância continuam como hoje (`OverlayColor`).

```
┌──────────────────────────────────────────────┐
│ [Lyon → Milano · Fase 3]          [⌖] [+][-] │  chips/botões sobre o mapa
│      ·Dijon        ╲╲ estradas (MapRoad*)    │
│   ●Lyon ━━━━━━━━━━━━━━┓  rota âmbar + halo   │
│      ·Grenoble        ┗━━━━━━━●Milano        │
│                         ·Torino              │
│ [Preparando o mapa do jogo… 42%]   [812 km]  │
└──────────────────────────────────────────────┘
```

## Estados
| Estado | Desenho | Texto |
| --- | --- | --- |
| Sem pasta do jogo | esquemático atual | `Map_Hint_NoGame` |
| Montando índice | esquemático + nota no rodapé (sem barra: o índice não expõe progresso) | `Map_Building` |
| Erro / versão não suportada | esquemático + ícone de aviso | `Map_Error_Version` / `Map_Error_Read` |
| Geográfico sem rota | cidades de fundo (dots + nomes que não colidem) | estado vazio atual do módulo |
| Fase 1 com par | pontas âmbar, linha reta **tracejada, sem halo** (halo só na rota sólida) | `Map_StraightLine` (tooltip do segmento e do chip) e `Map_StraightLine_Short` no chip de distância |
| Fase 3 com par | rota sólida âmbar com halo sobre as estradas | chip de distância com "calculada" |
| Rota não encontrada | linha tracejada + aviso pequeno | `Map_Error_NoRoute` |

## Níveis de detalhe (Fase 2+)
- Afastado: só estradas principais (`MapRoadMajorColor`) e capitais/cidades com mais empresas.
- Médio: todas as estradas, todas as cidades com nome se couber.
- Perto: estradas com largura proporcional, nomes de todas as cidades.

## Teclado e acessibilidade
- v1 (fase 1): o mapa **não é focável** (exceção documentada: não há ação nem zoom; o conteúdo vai no nome acessível e na nota). Fase 2: mapa focável (Tab) com foco visível; `+`/`-` zoom, setas movem, `0` centraliza a rota.
- Nome acessível continua `Route_A11y` (origem, destino, distância); o mapa é decorativo para leitores de tela.
- Movimento: zoom animado ≤150 ms; respeita "reduzir animações".
