# 0003. Sem listas fixas de cidades, cargas e reboques

- Status: aceita
- Data: 2026-11-17

## Contexto

Os jogos aceitam mods que adicionam cidades, empresas, cargas e reboques. Uma
lista fixa no código ficaria desatualizada assim que um mod fosse instalado, e
o app passaria a oferecer opções que o jogo não reconhece, ou deixaria de
oferecer opções que existem.

## Decisão

Nenhuma lista de origem, destino, carga ou reboque é codificada. Tudo é lido
do próprio save (IDs `company.volatile.*`, valores `cargo:`, pares
`trailer_variant` e `trailer_definition`). A regra está no `CLAUDE.md`, e a
origem de cada lista está em [../sii-format.md](../sii-format.md), seção 4.

## Consequências

- O app funciona com mods sem atualização, desde que o save tenha as opções.
- A lista de opções fica limitada ao que o jogo já sorteou em alguma oferta do
  save. Cargas e reboques que nunca apareceram ficam de fora. A leitura dos
  arquivos `def/` do jogo é uma fase posterior.
- Não se deve documentar nem implementar nomes fixos de cidades ou cargas, nem
  como exemplo de uso.
