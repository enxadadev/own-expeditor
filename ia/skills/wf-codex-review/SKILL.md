---
name: wf-codex-review
description: Revisão independente pelo Codex CLI (codex-review.sh) do diff atual, com triagem pelo code-reviewer. Use para Codex review, segunda opinião ou mudanças de alto risco (save, formato SII).
---

# Workflow: Codex review

| # | Quem | Faz | Saída / gate |
|---|------|-----|--------------|
| 1 | sessão | `ia/toolkit/check/check.sh` verde antes (não revise código quebrado). | ⛔ |
| 2 | sessão | `ia/toolkit/release/codex-review.sh [base]` → `.reviews/codex-<data>.md` (ignorado pelo git). Sem Codex instalado: informe o comando de instalação e pare. | arquivo de review |
| 3 | `code-reviewer` | Lê o arquivo e **verifica cada achado no código**: confirmado / falso positivo, com motivo. | triagem |
| 4 | sessão | Mostra ao usuário só os confirmados, por severidade. | ⛔ usuário escolhe o que corrigir |
| 5 | dono da área | Corrige os escolhidos → `ia/toolkit/check/check.sh`. | verde |

O diff enviado ao Codex passa por `ia/toolkit/git/diff-summary.sh`, que exclui
`*.sii`; nunca envie saves ou dados pessoais.
