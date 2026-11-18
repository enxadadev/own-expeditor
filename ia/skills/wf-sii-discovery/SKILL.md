---
name: wf-sii-discovery
description: Workflow para investigar e documentar parte do formato do save antes de codar — samples, sii-format.md, fixture e testes. Use para blocos ou atributos novos, ou suporte a BSII/3nK.
---

# Workflow: descoberta do formato SII

Regra do projeto: **documentar antes de codar**.

| # | Quem | Faz | Saída / gate |
|---|------|-----|--------------|
| 1 | `save-inspector` | `toolkit/samples.sh --text`, `toolkit/sii.sh types/attrs/list/show` — responde a pergunta com trechos mínimos anonimizados. | evidência |
| 2 | `sii-engine` | Escreve a descoberta em `docs/sii-format.md` (o quê, exemplos, dúvidas). | ⛔ doc antes do código |
| 3 | `fixture-maker` | Fixture mínimo do caso. | fixture |
| 4 | `test-engineer` | Testes de parse + round-trip com o fixture. | vermelho |
| 5 | `sii-engine` | Implementa. | — |
| 6 | `test-runner` | `toolkit/check.sh`; `toolkit/sii.sh roundtrip` em todos de `toolkit/samples.sh --text`. | ⛔ verde e round-trip ok |
| 7 | `code-reviewer` | Revisão. | ⛔ |
| 8 | `git-ops` | `wf-commit` (patch; minor se habilita formato novo, ex. BSII). | ⛔ aprovação |

Se a pergunta é só exploratória, pare no passo 2 e commite só a doc.
