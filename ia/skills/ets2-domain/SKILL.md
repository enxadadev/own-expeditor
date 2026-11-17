---
name: ets2-domain
description: Glossário do domínio ETS2/ATS como aparece no game.sii — job offers, empresas, cidades, cargas, reboques, tempo de jogo — com os campos reais levantados nos saves de exemplo. Use ao desenhar ou implementar leitura/injeção de jobs (chief-architect, core-dev, sii-engine, test-engineer, save-inspector).
---

# Domínio ETS2/ATS no save

Fatos levantados com `toolkit/sii.sh` num save real (texto, ~6.500 ofertas).
O que não foi confirmado está marcado **(?)** — confirme com
`save-inspector` antes de depender disso. Ao descobrir algo novo,
atualize esta skill e `docs/sii-format.md`.

## Blocos principais
| Tipo de bloco | Qtde (ex.) | O que é |
| --- | --- | --- |
| `company` | ~1.900 | uma empresa **em uma cidade**; id `company.volatile.<empresa>.<cidade>` |
| `job_offer_data` | ~6.500 | uma oferta de frete; referenciada por `company.job_offer[i]` |
| `economy` | 1 | estado global; tem `game_time`, `bank`, `player`, `time_zone` |
| `job_info` | centenas | (?) jobs aceitos/em andamento |
| `garage` | centenas | garagens por cidade |

## company
```
company : company.volatile.radus_ru.sofia {
 permanent_data: company.permanent.radus_ru   ← definição da empresa (vem dos defs do jogo/mods)
 job_offer: 4                                  ← tamanho do array
 job_offer[0]: _nameless.2d8.21c9.d5d8         ← ids de blocos job_offer_data
 cargo_offer_seeds: 10 / cargo_offer_seeds[i]  ← (?) sementes do gerador de ofertas
 discovered, state, reserved_trailer_slot, delivered_trailer, delivered_pos
}
```
Cidade e empresa saem do **id** (`<empresa>.<cidade>` após `company.volatile.`).
Nada de listas fixas: tudo vem destes ids (mods acrescentam empresas/cidades).

## job_offer_data (origem = a company que referencia a oferta)
| Atributo | Exemplo | Significado |
| --- | --- | --- |
| `target` | `"lognstick.istanbul"` | destino: `<empresa>.<cidade>` (string entre aspas) |
| `expiration_time` | `11649` | expira quando `economy.game_time` passar disso (minutos de jogo) (?) unidade |
| `urgency` | `0` | (?) 0 = padrão; maiores = mais urgente |
| `shortest_distance_km` | `569` | distância rodoviária em km |
| `ferry_time` / `ferry_price` | `0` | tempo/preço de balsa no trajeto |
| `cargo` | `cargo.concr_beams2` | carga (`cargo.<nome>`) |
| `company_truck` | `mercedes_actros_2014_4x2_c` | caminhão da empresa (frete rápido) |
| `trailer_variant` | `trailer.scs_flat_b` | variante do reboque |
| `trailer_definition` | `trailer_def.scs.flatbed.single_3.flatbed_w` | definição do reboque |
| `units_count` | `2` | quantidade de unidades da carga |
| `fill_ratio` | `1` | (?) fração de ocupação do reboque |
| `trailer_place` | `0` | (?) vaga do reboque na empresa |

Em `economy` de exemplo: `game_time: 10915` e ofertas com `expiration_time`
~11.500–11.650 → ofertas válidas por algumas centenas de minutos de jogo.

## Injetar um job (regras de negócio)
- Substitui-se uma oferta **existente** (`job_offer_data`) — não se criam
  blocos novos (decisão do projeto, como no Virtual Speditor).
- Origem = company dona da oferta; destino = `target` de outra company existente.
- Carga e reboque precisam ser compatíveis (?) — a compatibilidade vem dos
  defs do jogo, não do save; documente a regra quando for implementada.
- `shortest_distance_km` precisa ser recalculado/estimado para o novo destino
  (cache de distâncias em Data).
- `expiration_time` deve ficar no futuro em relação a `economy.game_time`.
