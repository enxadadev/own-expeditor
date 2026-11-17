# Changelog

A versão do produto fica em `Directory.Build.props` (`M.X.Y`). Quem a altera é
o agente `git-ops` (ver `ia/agents/git-ops.md`).

## 0.2.0 (em desenvolvimento)

Injeção de fretes (`feature/job-injection`):

- `OwnExpeditor.Sii`: leitura de BSII (binário) e 3nK, além de ScsC e texto.
- `OwnExpeditor.Data`: leitor de pacotes `.scs` (HashFS v2) e catálogo do
  jogo (empresas, cidades, cargas) lido da instalação do usuário.
- `OwnExpeditor.Data`: nomes traduzidos via `locale.scs` no idioma do jogo,
  com fallback para nome local e id legível. Ainda não verificado com o
  arquivo real (pendência em `docs/sii-format.md`, seção 6).
- `OwnExpeditor.Core`: `JobInjector` e `SaveSession` (origens com vaga,
  cargas, destinos, distância aprendida do save) e gravação como save novo.
- `OwnExpeditor.App`: primeira interface (pastas, save, frete, gravar).
- Validado no jogo: save em texto carrega e o frete injetado aparece.

## 0.1.0

Ainda sem release publicado. Estado do que existe no código:

- `OwnExpeditor.Sii`: decodificação do formato ScsC (AES-256-CBC e zlib) e
  leitura de texto `SiiNunit` (`SiiDecoder`).
- `OwnExpeditor.Sii`: parser de texto para blocos e atributos, com gravação em
  texto que preserva a ordem (`SiiDocument`).
- `OwnExpeditor.Sii`: `Save` cria backup `game.sii.bak-<timestamp>` antes de
  gravar.
- Testes xUnit em `Sii.Tests` (7 casos inline e os casos de `samples/`) e um
  placeholder em `Core.Tests`.
- Esqueleto da aplicação Avalonia (`OwnExpeditor.App`), ainda com o template.
- Projetos `Core`, `Data` e `Domain` criados, sem código de produção.

Histórico de commits relevante para o estado atual:

- `OWNE-1`: commit inicial.
- `OWNE-2`: skill `make-interfaces-feel-better` (renomeada depois para
  `avalonia-polish`).
- `OWNE-3`: agente `git-ops` e versionamento do projeto.
- `OWNE-4`: agentes e skills movidos para `ia/`.
- `OWNE-5`, `OWNE-6`: regras de numeração e de histórico do git.
- `OWNE-7`: agente `docs-keeper`.
- `OWNE-8`: documentação inicial em `docs/`.
- `OWNE-9`: `toolkit/`, com scripts para build e testes resumidos, checagem de
  camadas e de docs, diff compacto, numeração OWNE, versão, commit com
  validação de mensagem, ADRs, revisão pelo Codex e release Windows. Inclui a
  ferramenta `toolkit/SiiTool` para inspecionar saves sem ler o arquivo inteiro.
  Limitação: saves BSII ainda não são suportados.
- `OWNE-10`: agentes de código (`chief-architect`, `core-dev`, `sii-engine`,
  `ui-dev`, `test-engineer`, revisores, `build-fixer`, `save-inspector`,
  `fixture-maker`, `test-runner`, `release-builder`, `git-ops`), skills de
  workflow `wf-*` e a orquestração documentada em `ia/README.md`.
- `OWNE-12`: skills de domínio, UI, testes e regra de tema/idioma:
  skills `save-safety`, `ets2-domain`, `avalonia-mvvm`, `i18n-theming`,
  `ux-writing`, `dotnet-standards`, `xunit-patterns` e `large-save-performance`;
  `make-interfaces-feel-better` renomeada para `avalonia-polish`; agentes
  listam as skills que carregam; regra de tema e idioma no `CLAUDE.md` e
  ADR 0004.
- `OWNE-13`: documentação das skills, do requisito de tema/idioma e ADR 0004.
- `OWNE-14`: design antes de código. `design/` versionado com a direção visual
  aprovada (A+C, "Cabine com mapa da rota"); agente `ux-designer`; skill
  `design-system`; `toolkit/screenshots.sh` para PNGs das Views em Light/Dark ×
  pt-BR/en; regra "design antes de código" no `CLAUDE.md`.
- `OWNE-15`: ADR 0005 (shell modular e sessão de edição, em proposta), plano de
  implementação `plan-0005-shell.md` e atualização da documentação de
  arquitetura, workflow e desenvolvimento.

Os números de commit citados aqui vêm do histórico do git. Esta seção não é
gerada automaticamente.
