---
name: wf-commit
description: Workflow de commit do Own Expeditor com o agente git-ops — preparo por script, proposta, aprovação do usuário, commit validado. Use sempre que for commitar.
---

# Workflow: commit

1. **`git-ops`** roda `toolkit/precommit.sh` (branch, próximo OWNE, versão,
   arquivos, alertas) e `toolkit/check.sh`.
2. Classifica: patch / minor / major / sem bump; confere `toolkit/bump-version.sh <tipo> --dry-run`.
3. **Proposta ao usuário**: arquivos, versão `antiga → nova`, mensagem completa.
4. ⛔ **Aguarda aprovação explícita.**
5. Após o "sim": `toolkit/bump-version.sh <tipo>` (se houver), escreve a
   mensagem num arquivo temporário e roda
   `toolkit/commit.sh <msg> <arquivos...>` (valida formato, OWNE único e
   ausência de menções proibidas).
6. Relata hash, branch, versão; push só se pedido.

Reescrita de histórico (amend, renumeração) segue a seção própria do
`git-ops` e também exige aprovação.
