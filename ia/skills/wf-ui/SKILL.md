---
name: wf-ui
description: Workflow para criar ou polir telas Avalonia do Own Expeditor — ui-dev com a skill make-interfaces-feel-better, revisão do ux-reviewer e commit. Use para ajustes visuais, novas telas sem regra de negócio nova, ou "a UI está estranha".
---

# Workflow: UI

| # | Quem | Faz | Saída / gate |
|---|------|-----|--------------|
| 1 | sessão | Define tela(s) e objetivo; se precisar de dado/caso de uso novo do Core, use `wf-feature`. | escopo |
| 2 | `ux-reviewer` | Se é polimento: tabela antes/depois do estado atual. | lista de mudanças |
| 3 | `ui-dev` | Implementa (skill `make-interfaces-feel-better`). | — |
| 4 | `test-runner` | `toolkit/check.sh` | ⛔ verde |
| 5 | `ux-reviewer` | Revisa o resultado. | ⛔ sem itens de alto impacto (máx. 2 voltas) |
| 6 | usuário | Itens "verificar visualmente": `dotnet run --project src/OwnExpeditor.App`. | ⛔ ok visual |
| 7 | `git-ops` | `wf-commit`, bump **patch**. | ⛔ aprovação |
