# Domínio

O projeto `src/OwnExpeditor.Domain` tem só records e enums puros, sem I/O e sem
dependências. Ele guarda os conceitos que mais de uma camada usa: jogo, perfil,
save e ajustes.

## Tipos atuais

| Pasta | Tipo | Papel |
|-------|------|-------|
| `Games` | `GameKind` | `Ets2` ou `Ats` |
| `Games` | `GameIds` | Converte um identificador do jogo (ex.: `cargo.concr_beams2`) em nome legível (`Humanize`) |
| `Games` | `GameNameKind` | Categoria de nome do jogo (empresa, cidade, carga, reboque, caminhão, país) |
| `Games` | `GameFolderCheck` | Resultado da validação de uma pasta de instalação |
| `Games` | `CityPair` | Par de cidades não direcionado, usado pelo cache de distâncias |
| `Saves` | `ProfileInfo` | Perfil: jogo, id, nome, pasta e se usa Steam Cloud |
| `Saves` | `SaveSlotInfo` | Slot de save: perfil, id da pasta, nome, caminho do `game.sii`, data e tamanho |
| `Saves` | `ProfileNames` | Converte o nome da pasta do perfil (hex) em nome legível |
| `Settings` | `AppSettings` | Tema, idioma, último save aberto e pastas do jogo |
| `Settings` | `GameFolders` | Pastas de instalação escolhidas pelo usuário (`null` = detectar) |
| `Settings` | `ThemePreference` | `System`, `Light` ou `Dark` |
| `Settings` | `Language` | `PtBr` ou `En` |

## Entidades do jogo (fora do Domain)

Empresas, cidades, cargas e reboques são lidos do jogo e do save, e não têm
records próprios no `Domain`:

- `GameCatalog` (`Data`): definições lidas dos pacotes `.scs` (empresas, cidades,
  países, cargas que cada empresa envia e recebe, nomes).
- `CountryDef` (`Data`): país lido de `def/country/<pais>.sui`. A cidade guarda
  só o id do país (`CityDef.Country`); o nome vem do catálogo.
- `JobBoard` e `CompanyLocation`, `JobTemplate`, `RouteDistance` (`Core/Modules/Jobs`):
  o índice de leitura que a tela usa e a distância conhecida ou estimada.
  `CountryInfo` e `DepartureCity` são as respostas de país e de cidade de saída.
- `JobSelection` e `JobSelectionResult` (`Core/Modules/Jobs`): a escolha em
  andamento na tela (país, cidade, empresa, vaga, carga, nos dois lados) e o
  resultado de revalidar, com os campos limpos e os preenchidos sozinhos.
  `JobSelectionRules` faz as contas sobre eles (ver [ADR 0007](../decisions/0007-cadeia-de-escolhas-do-frete-no-core-e-pais-da-cidade-pelo-igamedata.md)).
- `JobRequest`, `ChangeSet` e `SavePatch` (`Core`): o pedido de frete e a mudança
  que vai para a revisão.

A oferta de frete (`job_offer_data`) continua sendo um bloco do save, lido e
alterado por `SiiUnit` (`Sii`). Os campos relevantes para o frete estão em
[../sii-format.md](../sii-format.md), seção 3.

## Regras

- Nenhuma lista de cidades, cargas ou reboques é fixa. Tudo vem do save e do
  catálogo do jogo, para funcionar com mods
  ([ADR 0003](../decisions/0003-sem-listas-fixas.md)).
- País é lido do jogo, nunca fixo. `IGameData.CityCountry(city)` devolve o id do
  país, ou nulo se o jogo não diz. O nome segue a cadeia de `GameNameKind.Country`:
  tradução do jogo → nome local da definição → id legível. Cidade sem país vai
  para o grupo "Outros" (`JobBoard.OtherCountry`, id vazio). Sem países
  conhecidos (`JobBoard.HasCountries` falso), a cadeia é só cidade → empresa.
- Slot vazio de oferta é `target: ""` e `cargo: null`. Esses slots são onde um
  frete novo entra sem apagar ofertas existentes (ver
  [../sii-format.md](../sii-format.md), seção 3).
- Distância entre cidades não vem do save de forma completa: usa-se a tabela
  conhecida (`shortest_distance_km`) e, quando falta, uma estimativa marcada como
  tal (ver [../sii-format.md](../sii-format.md), seção 10).
