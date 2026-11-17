# 0001. Camadas com dependência só para baixo

- Status: aceita
- Data: 2026-11-17

## Contexto

O app tem interface gráfica, regras de negócio, acesso a arquivos e um parser
de um formato binário/criptografado. Sem limites claros, a UI tende a ler e
gravar o save direto, e a lógica de parsing fica espalhada.

## Decisão

O código é separado em projetos, com dependências apontando só para baixo:
`App → Core → Data/Sii → Domain`. `Domain` não tem dependências. A regra está em
`CLAUDE.md`, e os `ProjectReference` dos `.csproj` a seguem.

## Consequências

- O parser (`Sii`) pode ser testado sem a UI.
- Regras de negócio ficam em `Core`, e a UI não as reimplementa.
- Mudar uma camada inferior não obriga a mexer nas superiores, desde que a
  interface pública se mantenha.
- Há um custo: algumas operações passam por mais um projeto. Por exemplo, a UI
  precisa de um caso de uso no `Core` para gravar um save.
- Hoje `Core`, `Data` e `Domain` estão vazios, então a regra ainda não foi
  exercitada no código de produção. Ver [../architecture/layers.md](../architecture/layers.md).
