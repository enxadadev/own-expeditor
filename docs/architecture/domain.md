# Domínio

> TODO: confirmar. O projeto `src/OwnExpeditor.Domain` ainda não tem nenhum
> arquivo `.cs`. Este documento descreve o modelo **planejado**, derivado do
> `CLAUDE.md` e da análise do save em [../sii-format.md](../sii-format.md). Os
> nomes e campos podem mudar quando forem codificados.

O `Domain` deve conter entidades puras, sem dependências de outros projetos
(ver [layers.md](layers.md)).

## Entidades planejadas

| Entidade | Papel | Origem no save (planejada) |
|----------|-------|----------------------------|
| `Company` | Empresa de origem de um job | Bloco `company : company.volatile.<empresa>.<cidade>`; a origem é o próprio ID |
| `City` | Cidade de origem ou destino | Parte `<cidade>` do ID e do campo `target` (`"<empresa>.<cidade>"`) |
| `Cargo` | Carga do job | Valor `cargo:` (ex.: `cargo.concr_beams2`) |
| `Trailer` | Reboque: variante e definição | Pares `trailer_variant` e `trailer_definition` |
| `JobOffer` | Oferta de job editável | Bloco `job_offer_data`, referenciado por `job_offer[n]` da empresa |

Campos de `JobOffer` que a análise do save identificou como relevantes: `target`,
`expiration_time`, `urgency`, `shortest_distance_km`, `cargo`,
`company_truck`, `trailer_variant`, `trailer_definition`, `units_count`,
`fill_ratio` e `trailer_place`.

## Regras já definidas

- Nenhuma lista de cidades, cargas ou reboques é fixa. Tudo vem do save, para
  funcionar com mods ([ADR 0003](../decisions/0003-sem-listas-fixas.md)).
- Slot vazio de oferta é `target: ""` e `cargo: null`. Esses slots são onde a
  injeção pode acontecer sem apagar ofertas existentes (ver
  [sii-format.md](../sii-format.md), seção 3).

## Em aberto

- Se o modelo é imutável ou se `JobOffer` é mutável como `SiiUnit` hoje.
- Se `Company` e `City` são entidades separadas ou só valores derivados do ID.
- Como representar a distância quando o par origem/destino não existe no save
  (já listado em `sii-format.md`, seção 5).
