---
name: wf-refactor
description: Workflow de refatoração sem mudar comportamento — testes verdes antes e depois, revisão e commit sem bump. Use para reorganizar, simplificar ou renomear.
---

# Workflow: refactor

| # | Quem | Faz | Saída / gate |
|---|------|-----|--------------|
| 1 | `test-runner` | `toolkit/check/check.sh` **antes**. | ⛔ verde (não refatore em cima de vermelho) |
| 2 | `test-engineer` | Se a área não tem cobertura, cria testes de caracterização primeiro. | cobertura |
| 3 | `chief-architect` | Só se muda fronteiras entre camadas/contratos públicos. | plano |
| 4 | dono da área | Refatora em passos pequenos; sem mudar comportamento nem testes existentes. | — |
| 5 | `test-runner` | `toolkit/check/check.sh` + `toolkit/check/check-layers.sh` | ⛔ verde, mesmos testes |
| 6 | `code-reviewer` | `toolkit/git/diff-summary.sh` + `toolkit/check/check-rules.sh`; confirma ausência de mudança de comportamento. | ⛔ sem BLOQUEANTE |
| 7 | `git-ops` | `wf-commit`, **sem bump** (confirmar com o usuário). | ⛔ aprovação |
