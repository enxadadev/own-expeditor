# Direção visual

- **Status:** aprovado — mistura A+C ("Cabine com mapa da rota"), escolhida em 2026-11-17.
- **Canvas:** https://claude.ai/artifact/EBn8WvYp5z8dgDmK8rNmQV (artboard *A+C · Cabine com mapa da rota*);
  fonte em [`canvas/CabineRota.dc.html`](canvas/CabineRota.dc.html).

## Alternativas avaliadas
| Artboard | Ideia | Resultado |
| --- | --- | --- |
| A · Cabine (`Main.dc.html`) | escuro, âmbar, números condensados grandes | base visual |
| B · Ferramenta limpa (`Ferramenta.dc.html`) | neutro, tabela densa, painel lateral | descartada |
| C · Logística / rota (`Rota.dc.html`) | rota como elemento central, cartões | mapa e cartões de rota |
| **A+C** (`CabineRota.dc.html`) | visual A + mapa da rota e cartões de C | **escolhida** |

## Características da direção A+C
- Tema escuro como padrão, tema claro quente (bege) com o mesmo âmbar escurecido.
- Destaque âmbar (`#F5A524` escuro / `#B86E00` claro) só para seleção, rota e ação principal.
- Tipografia: Barlow (UI) + Barlow Condensed (números, títulos em caixa alta).
- Números grandes tabulares para distância/tempo; rótulos em caixa alta pequenos.
- Navegação lateral compacta (ícone + rótulo curto); lista de ofertas à esquerda;
  mapa da rota no topo da área principal; cartões de indicadores; campos editáveis
  em cartões; "Revisar mudanças" sempre no canto inferior direito.

## Mapa da rota — premissa a validar
O mockup projeta cidades por latitude/longitude reais. O save **não** tem
coordenadas: elas vêm dos arquivos de definição do jogo (`def/city/*.sii` dentro
dos `.scs`) ou de mods. Antes de implementar, uma descoberta (`wf-sii-discovery`)
precisa confirmar de onde ler posições de cidades, inclusive de mods. Até lá,
o mapa é um esquema (origem → destino) sem fundo geográfico.
