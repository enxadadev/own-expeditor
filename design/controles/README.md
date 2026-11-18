# Biblioteca de controles e galeria

- **Status:** aprovado (2026-11-17)
- **Tarefa:** UX-3 de [`docs/architecture/plan-0005-shell.md`](../../docs/architecture/plan-0005-shell.md); ADR 0005, decisão 6 (controles em `src/OwnExpeditor.App/Controls`, galeria só em Debug).
- **Direção visual:** A+C "Cabine com mapa da rota" ([`../direcao-visual/`](../direcao-visual/README.md)).
- **Canvas:** https://claude.ai/artifact/AHbpXwyEujo1i82j8AZEED . Fonte em [`canvas/`](canvas/): `Tokens.dc.html` (paleta, contraste calculado ao vivo, tipografia, espaços, raios, foco) e `Galeria.dc.html` (todos os controles em estados, Light × Dark lado a lado, alternância de idioma, textos longos e áreas de clique).

## Objetivo
Dar ao `ui-dev` (tarefa A3) tudo o que precisa para implementar tokens e controles sem decidir nada de visual: propriedades, estados, área de clique, acessibilidade e comportamento com textos longos, em pt-BR e en. Cada tela do shell e dos módulos só monta estes controles.

## Arquivos
| Arquivo | Conteúdo |
| --- | --- |
| [`screens.md`](screens.md) | tokens (cores, contraste verificado, tipografia, espaços, raios, foco, movimento), regras comuns, um bloco por controle, layout da galeria |
| [`strings.md`](strings.md) | `Chave \| pt-BR \| en` dos textos dos controles e da galeria (vira os `.resx`) |
| [`canvas/Tokens.dc.html`](canvas/Tokens.dc.html) | quadro de tokens |
| [`canvas/Galeria.dc.html`](canvas/Galeria.dc.html) | quadro da galeria |
| [`canvas/canvas.json`](canvas/canvas.json) | posições dos quadros |

Não há `flows.md`: controles não têm fluxo próprio (os fluxos ficam em `design/shell/` e `design/review-apply/`).

## Controles
PageHeader, EmptyState, ErrorBanner, LoadingPanel, SectionCard, StatTile, ValueChange, DiffList, PendingBadge, SearchBox, SearchablePicker, RouteOfferItem, RouteMap e estilos de botão `accent` / `danger` / `neutral`.

## Decisões
1. **Tokens da skill `design-system` confirmados, com 5 ajustes** (todos por contraste medido; veja `screens.md` §1.2):
   - novo `AccentTextColor`: o âmbar do tema claro (`#B86E00`) tem só 3,57:1 sobre o fundo e 3,23:1 sobre a lateral, então **não pode ser cor de texto**; texto/ícone de acento usa `AccentTextColor` (`#8A5200` no claro, igual ao âmbar no escuro). `AccentColor` fica para preenchimento, borda de seleção e rota.
   - `SuccessColor` claro `#157F3B` → `#0F6B33` (falhava 4,5:1 sobre lateral, mapa e seleção).
   - novos `BorderStrongColor` (bordas de campo/botão neutro ≥ 3:1; `LineColor` fica só como divisor), `FocusColor`, estados `*HoverColor`/`*PressedColor`, `OnDangerColor`, `DangerBgColor`, `SuccessBgColor`, tokens do mapa.
   - `Caption` sobe de 11–12 para **12 px** fixo.
   - `Space1 = 2`, `Space8 = 32`, `RadiusPill = 999` adicionados.
2. **Botão accent claro** mantém `#B86E00` com texto `#1A1206` (4,65:1). Hover clareia (`#C47A0A`); pressed não escurece (escurecer derrubaria o contraste): usa o mesmo preenchimento, `scale(0.98)` do Fluent e borda `AccentTextColor`.
3. **Foco visível** = anel de 2 px `FocusColor` (cor do texto) com 2 px de folga; em itens de lista o anel é interno. Vale ≥ 13:1 nos dois temas.
4. **Nomes alinhados à skill:** `ErrorState` → **`ErrorBanner`** (variantes Inline e Block), `LoadingState` → **`LoadingPanel`**, `ChangeReviewList` → **`DiffList`**, `Badge` → **`PendingBadge`**, `OfferList`/`DataList` permanecem para as telas. A skill `design-system` precisa refletir isso (fora do meu escopo de escrita).
5. **Controles sem ViewModel**: recebem valores já formatados pela cultura (`DistanceText`, `Value`) e textos já traduzidos; quem chama usa `ILocalizer`. Exceção: textos fixos do próprio controle (rótulo do botão limpar, "Detalhes") usam chaves `Control_*`/`Picker_*`.
6. **Galeria**: cada controle em Light e Dark lado a lado (cada célula força `RequestedThemeVariant`), alternância pt-BR/en, "textos longos" e "áreas de clique". Estados hover/pressed/focus são forçados na galeria por pseudo-classes, não dependem do mouse.

## Perguntas em aberto
1. **Atalho de busca** (`Ctrl+F`/`Ctrl+K` para focar o `SearchBox`) pertence ao shell (UX-1). Confirmar com `design/shell/`.
2. **Plural**: `.resx` não tem plural. Proposta: chaves `_Zero/_One/_Other` escolhidas por `ILocalizer.GetPlural(key, n)` (0 → Zero, 1 → One, resto → Other, igual em pt-BR e en). Aprovar.
3. **Strings da galeria** (`Gallery_*`): ficar num `Gallery.resx` excluído do Release junto com `Gallery/**`? O teste de paridade cobre os dois.
4. **Mapa geográfico**: depende da descoberta de coordenadas de cidades (ADR 0005, consequências). O controle já prevê os dois modos (esquemático e geográfico); o geográfico só liga quando houver coordenadas.
5. **Movimento reduzido**: Avalonia não expõe a preferência do SO de forma simples. Proposta: animações ≤ 200 ms só em opacidade/transform; o spinner é a única animação contínua. Sem opção própria na v1.
6. **`ValueChange` para valores monetários** vem como texto já formatado (`DiffValue.Money`); confirmar se o símbolo da moeda do ATS (US$) entra pela cultura ou pelo `GameKind`.
7. **Skeleton** no `LoadingPanel`: ficou de fora da v1 (só spinner/barra); reavaliar se a lista de ofertas demorar.
