# Agentes e skills do Own Expeditor

Definições em `ia/agents/` e `ia/skills/`; `.claude/agents` e `.claude/skills`
têm links simbólicos para elas. Agentes e skills novos só aparecem em
sessões iniciadas depois de criados.

A sessão principal é a **orquestradora**: subagentes não chamam outros
subagentes. Ela escolhe o agente, passa o **contexto mínimo** (caminhos,
contrato, critério de pronto) e repassa o resultado ao próximo.

## Quem faz o quê

| Agente | Modelo / esforço | Dono de | Escreve? |
| --- | --- | --- | --- |
| chief-architect | Opus 5.5 / high | desenho, contratos, plano, texto do ADR | não |
| sii-engine | Sonnet 5.5 / high | `src/OwnExpeditor.Sii`, conteúdo de `docs/sii-format.md` | sim |
| core-dev | Sonnet 5.5 / medium | `src/OwnExpeditor.{Domain,Core,Data}` | sim |
| ui-dev | Sonnet 5.5 / medium | `src/OwnExpeditor.App` | sim |
| test-engineer | Sonnet 5.5 / medium | `tests/**` (menos `Fixtures/`) | sim |
| ux-designer | Sonnet 5.5 / high | `design/**` (fluxos, telas, textos, canvas) | só `design/` |
| code-reviewer | Sonnet 5.5 / high | revisão de correção e regras | não |
| docs-keeper | Haiku 5.5 / medium | `docs/` (ADRs, changelog, forma do sii-format) | sim |
| fixture-maker | Haiku 5.5 / medium | `tests/**/Fixtures/` | sim |
| ux-reviewer | Haiku 5.5 / medium | revisão visual pelo XAML | não |
| build-fixer | Haiku 5.5 / medium | correções mecânicas de compilação | sim (mínimo) |
| save-inspector | Haiku 5.5 / low | consultas aos saves de exemplo | não |
| test-runner | Haiku 5.5 / low | rodar build/testes e resumir falhas | não |
| release-builder | Haiku 5.5 / low | `publish/` (exe + notas) | só `publish/` |
| git-ops | Haiku 5.5 / low | versão, commit (com aprovação do usuário) | sim |

Cada arquivo tem um único dono. Dois agentes que escrevem nunca rodam em
paralelo sobre a mesma área.

## Fluxo de uma feature (detalhe em `wf-feature`)

1. `chief-architect` → plano, contratos, tarefas, texto do ADR.
2. `docs-keeper` grava o ADR.
2b. `ux-designer` → `design/<tema>/`; sessão publica o canvas; usuário aprova.
3. Agente da camada cria os **contratos** (stubs).
4. `save-inspector` / `fixture-maker` → dados reais mínimos, se necessário.
5. `test-engineer` → testes que falham.
6. `sii-engine` / `core-dev` / `ui-dev` → implementação.
7. `test-runner` (e `build-fixer` se o erro for mecânico) até ficar verde.
8. `code-reviewer` (+ `ux-reviewer` se mexeu na UI) → ajustes voltam ao passo 6.
9. `docs-keeper` → docs e changelog.
10. `git-ops` → proposta de commit; só commita com aprovação do usuário.

Mudança pequena (bug isolado, ajuste visual) pode pular 1–2 e começar no 5.

## Regras para a orquestradora

- **Obrigatório:** toda instrução do usuário passa por um workflow `wf-*`
  e é executada pelos agentes donos da área. A orquestradora nunca faz o
  trabalho sozinha (ver "Orquestração obrigatória" no `CLAUDE.md`).

- Prefira o agente mais barato que resolve: logs e status via `test-runner`,
  nunca lendo a saída inteira na sessão principal.
- Tarefa para Haiku: um objetivo, caminhos explícitos, formato de saída.
- Se um agente reporta "fora do meu escopo", encaminhe ao dono — não
  amplie o escopo dele.
- `chief-architect` só para decisões estruturais; não para dúvidas de
  implementação.

## Workflows (skills `wf-*`)

| Workflow | Quando |
| --- | --- |
| `wf-feature` | funcionalidade nova / mudança entre camadas |
| `wf-bugfix` | algo quebrado: teste de regressão → correção |
| `wf-ui` | tela nova ou polimento visual |
| `wf-refactor` | reorganizar sem mudar comportamento |
| `wf-sii-discovery` | entender/suportar parte do formato do save |
| `wf-review` | revisão interna (toolkit + code/ux-reviewer) |
| `wf-codex-review` | segunda opinião pelo Codex CLI + triagem |
| `wf-docs` | atualizar documentação |
| `wf-commit` | commit com aprovação (git-ops) |
| `wf-release` | exe Windows + notas |

## Toolkit (`toolkit/`) — trabalho repetitivo sem gastar tokens

| Script | Faz |
| --- | --- |
| `check.sh [--filter X]` | build + testes, saída resumida |
| `check-layers.sh` | regra de camadas pelos ProjectReference/usings |
| `check-docs.sh` | links e índice de `docs/`, conta TODOs |
| `diff-summary.sh [base] [-- caminhos]` | diff compacto para revisores |
| `samples.sh [--text]` | saves de exemplo e formato de cada um |
| `sii.sh <cmd> <save>` | format/formats/decode/types/list/show/attrs/roundtrip (SiiTool) |
| `next-owne.sh` | próximo número OWNE |
| `bump-version.sh patch\|minor\|major [--dry-run]` | versão M.X.Y |
| `precommit.sh` | tudo que o git-ops precisa para propor um commit |
| `lint-commit-msg.sh <msg>` | valida formato/OWNE/menções proibidas |
| `commit.sh <msg> <arquivos>` | valida e commita (após aprovação) |
| `install-hooks.sh` | instala hook commit-msg |
| `new-adr.sh "<titulo>" < texto` | próximo ADR numerado |
| `codex-review.sh [base]` | revisão pelo Codex CLI em `.reviews/` |
| `release.sh` | publish win-x64 + notas |
| `screenshots.sh [View]` | PNGs das Views em Light/Dark × pt-BR/en (Avalonia.Headless) |

Regra: se um agente repete a mesma sequência de comandos, vira script aqui.

## Skills

| Skill | Conteúdo | Usada por |
| --- | --- | --- |
| design-system | tokens, componentes, padrões de tela | ux-designer, ui-dev, ux-reviewer |
| avalonia-polish | polimento visual/interação (Fluent) | ui-dev, ux-reviewer, ux-designer |
| avalonia-mvvm | padrões de View/ViewModel | ui-dev, code-reviewer |
| i18n-theming | Light/Dark, pt-BR/en, preferências | ui-dev, ux-reviewer, chief-architect |
| ux-writing | textos de interface pt-BR/en, glossário | ui-dev, ux-reviewer, docs-keeper |
| save-safety | regras de escrita do game.sii | sii-engine, core-dev, code-reviewer, chief-architect |
| ets2-domain | campos reais do save (jobs, empresas…) | chief-architect, core-dev, save-inspector, fixture-maker |
| dotnet-standards | convenções C# | core-dev, sii-engine, test-engineer, build-fixer, code-reviewer |
| xunit-patterns | padrões de teste | test-engineer, fixture-maker |
| large-save-performance | desempenho com saves grandes | sii-engine |
| wf-* | workflows | sessão principal |
