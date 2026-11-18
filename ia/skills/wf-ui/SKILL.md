---
name: wf-ui
description: Workflow para criar ou polir telas Avalonia — ui-dev com avalonia-polish, revisão do ux-reviewer e commit. Use para ajustes visuais, telas sem regra de negócio nova ou "a UI está estranha".
---

# Workflow: UI

| # | Quem | Faz | Saída / gate |
|---|------|-----|--------------|
| 1 | sessão | Define tela(s) e objetivo; se precisar de dado/caso de uso novo do Core, use `wf-feature`. | escopo |
| 1b | `ux-designer` | Atualiza/cria `design/<tema>/` (flows, screens, strings, canvas). Sessão publica o canvas. | ⛔ usuário aprova; status → `aprovado` |
| 2 | `ux-reviewer` | Se é polimento: tabela antes/depois do estado atual (`toolkit/release/screenshots.sh`). | lista de mudanças |
| 3 | `ui-dev` | Implementa (skill `avalonia-polish`). | — |
| 4 | `test-runner` | `toolkit/check/check.sh`, `toolkit/check/check-i18n.sh`, `toolkit/check/check-rules.sh` | ⛔ verde |
| 5 | `ux-reviewer` | Revisa o resultado nas imagens de `toolkit/release/screenshots.sh` (Light/Dark × pt-BR/en). | ⛔ sem itens de alto impacto (máx. 2 voltas) |
| 6 | usuário | Itens "verificar visualmente": `dotnet run --project src/OwnExpeditor.App`. | ⛔ ok visual |
| 7 | `git-ops` | `wf-commit`, bump **patch**. | ⛔ aprovação |
