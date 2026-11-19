# 0008. Cadeia de escolhas do frete no Core e país da cidade pelo IGameData

- Status: aceita
- Data: 2026-11-18

## Contexto

O módulo Fretes montava a escolha numa ordem plana: lista de cidades de saída à
esquerda, cidade de destino, empresa de saída, "Onde entra o frete", carga e
empresa de chegada. Com mods, a lista de cidades chega a centenas de itens, e a
ordem visual não batia com a ordem das dependências (ver
`design/fretes-pais-cidade/README.md`, "Pontos de confusão").

O desenho aprovado em `design/fretes-pais-cidade/` troca a ordem para
País → Cidade → Empresa (+ vaga, na saída) nos dois lados, depois a carga. As
regras são as mesmas de sempre: só aparece opção que combina, o que deixa de
valer é limpo, opção única é escolhida sozinha, a edição de uma pendência trava
a saída. Isso exige decidir onde ficam as regras de cadeia e de onde vem o país
de cada cidade.

Restrição herdada da ADR 0003: nenhuma lista fixa de países, cidades ou cargas.
O país tem que vir do jogo (`city.country` das definições de cidade e
`def/country/*.sui`), e o nome do país tem que seguir a mesma cadeia de nomes
das outras entidades.

## Decisão

1. **A cadeia de escolhas fica no Core.** `JobSelection` (estado da escolha) e
   `JobSelectionRules` (`Core/Modules/Jobs`) são funções puras sobre o
   `JobBoard`: `Revalidate` (limpa o que deixou de valer e escolhe opção única),
   `Swap` (⇄ troca país e cidade dos dois lados e limpa empresas, vaga e carga),
   `NextJob` (depois de "Adicionar": mantém saída e países/cidades de chegada,
   limpa vaga, empresa de chegada e carga), `Missing` (campos que faltam) e
   `ToRequest` (monta o `JobRequest`). A App só chama essas funções e mostra o
   resultado (`Cleared`, `AutoPicked`).
2. **O `JobBoard` responde por país.** Além das respostas por cidade, expõe
   `DepartureCountries()`, `DepartureCities(country)`, `ArrivalCountries(from)`,
   `ArrivalCities(from, country)`, `ArrivalCompanies(from, toCity)` e
   `Cargos(from, to)` (carga = o que a saída envia ∩ o que a chegada aceita).
   Convenção: país `null` = sem filtro; `OtherCountry` (`""`) = cidades sem país
   conhecido, mostradas no grupo "Outros".
3. **O país vem do `IGameData`.** `IGameData.CityCountry(city)` devolve o id do
   país (nulo se desconhecido). O nome vem de `GameNameKind.Country`, pela cadeia
   já usada por cidades e cargas: tradução do jogo → nome local da definição
   (`name`) → id legível (`GameIds.Humanize`). Nenhum país é fixo no código.
4. **Sem país no save ou sem pasta do jogo**, `JobBoard.HasCountries` é falso e os
   campos de país somem: a cadeia volta a ser cidade → empresa.
5. **Não muda:** `JobRequest`, `JobPlanner`, a `Key` `jobs:slot:<id>` e a escrita
   do save. A cadeia só decide quais valores vão para o `JobRequest`; a validação
   e a gravação continuam as mesmas.

## Consequências

- As regras de cadeia podem ser testadas sem a App (`JobSelectionRulesTests`,
  `JobBoardCountryTests`) e a tela fica sem lógica de negócio.
- `IGameData` ganhou um membro (`CityCountry`). Quem implementa a interface
  (`GameData`, `UnavailableGameData`) precisa implementá-lo; não há default.
- `GameNameKind` ganhou `Country`. A cadeia de nomes do `GameCatalog` e do
  `GameData` precisa ficar igual (`CountryName`).
- A ordem dos campos mudou: a empresa de chegada vem antes da carga, e a chegada
  depende da empresa de saída. Quem quiser "escolher a carga primeiro" perde esse
  caminho.
- A API nova substitui a anterior do `JobBoard` (`DepartureCities` recebe o
  país); não há duas versões da cadeia no código.

## Alternativas descartadas

- **Cadeia na ViewModel** (`JobsPageViewModel`): as regras ficariam sem teste
  automatizado sem a UI e seriam copiadas se outra tela precisasse delas.
- **Método `default` na interface** `IGameData` para `CityCountry`: esconderia
  implementações que esqueceram o país; a falta de país tem que ser explícita.
- **Manter a API antiga junto da nova** (dois caminhos de cadeia): duplicaria as
  regras e deixaria as duas divergirem.
