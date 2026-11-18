---
name: wf-docs
description: Workflow de atualização de docs com docs-keeper e validação automática. Use quando pedirem documentação ou quando o código mudou sem docs.
---

# Workflow: docs

1. `docs-keeper` recebe: o que mudou (`toolkit/diff-summary.sh`) e quais docs tocar.
2. Ao terminar: `toolkit/check-docs.sh` (links, índice, contagem de TODOs).
3. `git-ops` → `wf-commit`, **sem bump**.
