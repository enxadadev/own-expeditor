---
name: wf-review
description: Workflow de revisão interna das mudanças do Own Expeditor antes do commit — checagens automáticas do toolkit, code-reviewer e ux-reviewer. Use quando pedirem revisão do trabalho atual ou como gate dos outros workflows.
---

# Workflow: revisão interna

1. **Automático (sem tokens de modelo):**
   `toolkit/check.sh`, `toolkit/check-layers.sh`, `toolkit/check-docs.sh`.
   Qualquer falha → corrigir antes de gastar revisão de modelo.
2. **`code-reviewer`** recebe só a saída de `toolkit/diff-summary.sh [base] [-- caminhos]`
   e os arquivos que ele mesmo pedir.
3. **`ux-reviewer`** se o diff toca `src/OwnExpeditor.App`.
4. Sessão consolida: BLOQUEANTE → volta ao dono; IMPORTANTE → pergunta ao
   usuário se corrige agora; SUGESTÃO → lista no relatório.

Veredito final: **aprovado** só sem BLOQUEANTE e com tudo do passo 1 verde.
