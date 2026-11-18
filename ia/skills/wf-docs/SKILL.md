---
name: wf-docs
description: Atualização de docs com docs-keeper e check-docs.sh. Use quando pedirem documentação ou quando o código mudou sem docs.
---

# Workflow: docs

1. `docs-keeper` recebe: o que mudou (`toolkit/git/diff-summary.sh`) e quais docs tocar.
2. Ao terminar: `toolkit/check/check-docs.sh` (links, índice, contagem de TODOs); ADR novo: `toolkit/gen/new-adr.sh`; mudou `ia/`: `toolkit/check/check-agents.sh`.
3. `git-ops` → `wf-commit`, **sem bump**.
