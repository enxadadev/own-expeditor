---
name: wf-codex-review
description: Revisão independente pelo Codex CLI (OpenAI) do diff atual, com triagem pelo code-reviewer. Use quando pedirem Codex review, segunda opinião ou antes de mudanças de alto risco (escrita de save ou formato SII).
---

# Workflow: Codex review

| # | Quem | Faz | Saída / gate |
|---|------|-----|--------------|
| 1 | sessão | `toolkit/check.sh` verde antes (não revise código quebrado). | ⛔ |
| 2 | sessão | `toolkit/codex-review.sh [base]` → `.reviews/codex-<data>.md` (ignorado pelo git). Sem Codex instalado: informe o comando de instalação e pare. | arquivo de review |
| 3 | `code-reviewer` | Lê o arquivo e **verifica cada achado no código**: confirmado / falso positivo, com motivo. | triagem |
| 4 | sessão | Mostra ao usuário só os confirmados, por severidade. | ⛔ usuário escolhe o que corrigir |
| 5 | dono da área | Corrige os escolhidos → `toolkit/check.sh`. | verde |

O diff enviado ao Codex passa por `toolkit/diff-summary.sh`, que exclui
`*.sii`; nunca envie saves ou dados pessoais.
