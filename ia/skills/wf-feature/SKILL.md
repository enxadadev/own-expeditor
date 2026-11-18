---
name: wf-feature
description: Workflow de feature nova, do desenho ao commit com aprovação — arquiteto, contratos, testes, implementação, revisão, docs e commit. Use para funcionalidade ou mudança de comportamento que atravessa camadas.
---

# Workflow: feature

A sessão principal orquestra; cada passo chama **um** agente com contexto
mínimo (objetivo, caminhos, contrato, critério de pronto). Gates (⛔) param
o fluxo até passar.

| # | Quem | Faz | Saída / gate |
|---|------|-----|--------------|
| 0 | sessão | Confirma o escopo com o usuário em 1–3 frases. | escopo aceito |
| 1 | `chief-architect` | Plano: decisão, contratos C#, tarefas por agente, riscos, texto do ADR. | plano |
| 2 | sessão | Mostra o plano resumido ao usuário. | ⛔ aprovação do plano |
| 2b | `ux-designer` | Se a feature tem UI: `design/<tema>/` (flows, screens, strings, canvas). Sessão publica o canvas. | ⛔ usuário aprova; status → `aprovado` |
| 3 | `docs-keeper` | `toolkit/new-adr.sh "<titulo>" < adr.md` | ADR gravado |
| 4 | `save-inspector` | Só se depende do formato: `toolkit/sii.sh types/attrs/show` em `toolkit/samples.sh --text`. | trechos reais |
| 5 | `fixture-maker` | Só se precisar de dado real em teste. | fixtures |
| 6 | agente da camada | Cria **contratos** (stubs `NotImplementedException`). | `toolkit/check.sh` compila |
| 7 | `test-engineer` | Testes do contrato. | testes **falham** pelo motivo certo |
| 8 | `sii-engine` → `core-dev` → `ui-dev` | Implementação, de baixo para cima. Um agente por vez por área. | — |
| 9 | `test-runner` | `toolkit/check.sh` + `toolkit/check-layers.sh` | ⛔ verde; erro mecânico → `build-fixer`, senão volta ao 8 |
| 10 | `code-reviewer` | Revisa `toolkit/diff-summary.sh`. `ux-reviewer` também se mexeu na UI, sobre `toolkit/screenshots.sh`. | ⛔ sem BLOQUEANTE; ajustes voltam ao 8 (máx. 2 voltas, depois escala ao usuário) |
| 11 | `docs-keeper` | Atualiza docs + `changelog.md`; `toolkit/check-docs.sh`. | docs ok |
| 12 | — | Opcional: `wf-codex-review` para segunda opinião. | — |
| 13 | `git-ops` | `wf-commit` com bump **minor** (X+1) para feature complexa, **patch** para pequena. | ⛔ aprovação do usuário |

## Regras
- Se o arquiteto disser que é pequeno, pule 1–3 e vá direto ao `wf-bugfix`-like (6→13).
- Nenhum agente amplia escopo; "fora do meu escopo" → encaminhe ao dono.
- Não leia logs na sessão principal: use os resumos do `toolkit/`.
