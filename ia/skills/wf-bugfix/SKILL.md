---
name: wf-bugfix
description: Workflow de correção de bug — teste de regressão, correção, revisão e commit com bump patch. Use quando algo não funciona como deveria.
---

# Workflow: bugfix

| # | Quem | Faz | Saída / gate |
|---|------|-----|--------------|
| 1 | sessão | Coleta sintoma, passos, save envolvido (se houver). | descrição clara |
| 2 | `save-inspector` | Se envolve save: `toolkit/inspect/sii.sh format/show/roundtrip` no save. | evidência |
| 3 | `test-engineer` | Teste de regressão que **falha** reproduzindo o bug (usa `fixture-maker` se precisar). | ⛔ teste vermelho pelo motivo do bug |
| 4 | dono da área (`sii-engine`/`core-dev`/`ui-dev`) | Menor correção que faz o teste passar; sem refatorar junto. | — |
| 5 | `test-runner` | `toolkit/check/check.sh` | ⛔ tudo verde |
| 6 | `code-reviewer` | `toolkit/git/diff-summary.sh`, `toolkit/check/check-layers.sh`, `toolkit/check/check-rules.sh` | ⛔ sem BLOQUEANTE |
| 7 | `docs-keeper` | Só se o comportamento documentado mudou; sempre registra no `changelog.md`. | — |
| 8 | `git-ops` | `wf-commit`, bump **patch** (Y+1). | ⛔ aprovação do usuário |

## Regras
- Se não der para reproduzir em teste (ex.: só no jogo), registre o porquê
  no relatório e peça validação manual ao usuário antes do commit.
- Bug que exige mudança de contrato/arquitetura → pare e vá para `wf-feature`.
- Bug que pode ter corrompido saves → avise o usuário e documente como
  recuperar pelo backup `game.sii.bak-*`.
