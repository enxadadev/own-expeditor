---
name: wf-docs
description: Atualização de docs com docs-keeper e check-docs.sh. Use quando pedirem documentação ou quando o código mudou sem docs.
---

# Workflow: docs

1. `docs-keeper` recebe: o que mudou (`ia/toolkit/git/diff-summary.sh`) e quais docs tocar.
2. Ao terminar: `ia/toolkit/check/check-docs.sh` (links, índice, contagem de TODOs); ADR novo: `ia/toolkit/gen/new-adr.sh`; mudou `ia/`: `ia/toolkit/check/check-agents.sh`.
3. `git-ops` → `wf-commit`, **sem bump**.
