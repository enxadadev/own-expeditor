---
name: ux-writing
description: Guia de textos de interface pt-BR e en — tom, botões, erros, vazios, confirmações, glossário do jogo. Use ao escrever ou revisar texto visível ou entradas .resx.
---

# UX writing (pt-BR e en)

## Tom
Direto, amigável, sem jargão técnico do formato SII (`job_offer_data`,
`ScsC`, `BSII` nunca aparecem para o usuário). Frases curtas. Você/"you".

## Padrões
| Elemento | pt-BR | en |
| --- | --- | --- |
| Botão = verbo + objeto | `Injetar frete` | `Inject job` |
| Confirmação | `Substituir a oferta em Sofia? Um backup será criado.` | `Replace the offer in Sofia? A backup will be created.` |
| Sucesso | `Frete injetado. Backup salvo em …` | `Job injected. Backup saved to …` |
| Vazio | `Nenhuma oferta neste save. Abra o mercado de fretes no jogo, salve e tente de novo.` | `No offers in this save. Open the freight market in game, save and try again.` |
| Carregando | `Lendo save…` | `Reading save…` |

**Erro = o que aconteceu + o que fazer**, nunca só o código:
`Não foi possível gravar: o jogo está aberto. Feche o jogo e tente de novo.` /
`Couldn't save: the game is running. Close it and try again.`

## Regras
- Sem ponto final em botões, rótulos e títulos; com ponto em frases completas.
- Maiúscula só na primeira palavra (`Escolher destino`, `Choose destination`).
- Reticências `…` (um caractere) em ações em andamento e itens que abrem diálogo.
- Números/unidades pela cultura (skill `i18n-theming`).

## Glossário (siga a tradução oficial do jogo)
| pt-BR | en |
| --- | --- |
| frete / oferta de frete | job / job offer |
| carga | cargo |
| reboque | trailer |
| origem / destino | origin / destination |
| empresa | company |
| mercado de fretes | freight market |
| perfil | profile |
| save / salvamento | save |
| garagem | garage |
