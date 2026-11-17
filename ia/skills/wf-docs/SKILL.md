---
name: wf-docs
description: Workflow para atualizar a documentação do Own Expeditor depois de mudanças — docs-keeper + validação automática. Use quando pedirem para documentar ou quando o código mudou sem docs.
---

# Workflow: docs

1. `docs-keeper` recebe: o que mudou (`toolkit/diff-summary.sh`) e quais docs tocar.
2. Ao terminar: `toolkit/check-docs.sh` (links, índice, contagem de TODOs).
3. `git-ops` → `wf-commit`, **sem bump**.
