---
name: wf-review
description: Revisão interna antes do commit: check.sh, check-layers.sh, check-rules.sh, diff-summary.sh, code-reviewer e ux-reviewer. Use quando pedirem revisão do trabalho atual ou como gate de outros workflows.
---

# Workflow: revisão interna

1. **Automático (sem tokens de modelo):**
   `toolkit/check/check.sh` → `check-layers.sh` → `check-rules.sh` →
   `check-i18n.sh` (se mexeu na UI) → `check-docs.sh` (se mexeu em docs), todos em `toolkit/check/`.
   Qualquer falha → corrigir antes de gastar revisão de modelo.
2. **`code-reviewer`** recebe só a saída de `toolkit/git/diff-summary.sh [base] [-- caminhos]`
   e os arquivos que ele mesmo pedir.
3. **`ux-reviewer`** se o diff toca `src/OwnExpeditor.App` (imagens de `toolkit/release/screenshots.sh`).
4. Sessão consolida: BLOQUEANTE → volta ao dono; IMPORTANTE → pergunta ao
   usuário se corrige agora; SUGESTÃO → lista no relatório.

Veredito final: **aprovado** só sem BLOQUEANTE e com tudo do passo 1 verde.
