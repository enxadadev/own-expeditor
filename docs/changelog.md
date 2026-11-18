# Changelog

A versão do produto fica em `Directory.Build.props` (`M.X.Y`). Quem a altera é
o agente `git-ops` (ver `ia/agents/git-ops.md`).

## 0.1.0 (em desenvolvimento)

Ainda sem release publicado. Estado do que existe no código:

- `OwnExpeditor.Sii`: decodificação do formato ScsC (AES-256-CBC e zlib) e
  leitura de texto `SiiNunit` (`SiiDecoder`).
- `OwnExpeditor.Sii`: leitura de saves binários `BSII` (`BsiiDecoder`), convertidos
  para texto antes do parser. Formato `3nK` ainda não é lido.
- `OwnExpeditor.Data`: leitor de pacotes `.scs` (HashFS v2, `HashFsArchive`),
  catálogo de empresas, cidades, cargas e reboques (`GameCatalog`) e nomes do
  jogo a partir de `locale.scs` (`Localization`).
- `OwnExpeditor.Data`: localizador de perfis e saves (`SaveLocator`).
- `OwnExpeditor.Core`: `SaveSession` (origens, cargas, destinos, distâncias e
  `Inject`) e `JobInjector`, que troca uma oferta existente no save.
- `OwnExpeditor.App`: tela única (`MainViewModel`) para escolher perfil e save,
  carregar as opções, adicionar fretes e gravar. A gravação cria um save novo;
  sobrescrever o save ainda não existe.
- `OwnExpeditor.Sii`: parser de texto para blocos e atributos, com gravação em
  texto que preserva a ordem (`SiiDocument`).
- `OwnExpeditor.Sii`: `Save` cria backup `game.sii.bak-<timestamp>` antes de
  gravar.
- Testes xUnit em `Sii.Tests` (7 casos inline e os casos de `samples/`) e um
  placeholder em `Core.Tests`.
- Projeto `Domain` criado, sem código de produção ainda.

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
- `OWNE-16`: desenhos do shell, da revisão e aplicação e dos controles em
  `design/` (shell, review-apply, controles).
- `OWNE-17`: merge de `feature/job-injection`. Traz a leitura de saves BSII, o
  leitor de `.scs`, o catálogo e os nomes do jogo, `SaveLocator`, `SaveSession` e
  `JobInjector`, e a tela única de injeção. Limitação: a gravação só cria um
  save novo, sem `.tmp` nem verificação; sobrescrever o save ainda não existe.
- (próximo commit) ADR 0006 e plano 0006: integração do código de injeção ao
  shell modular. Atualiza as ADRs 0002 e 0005, as skills `i18n-theming` e
  `design-system` e a documentação de arquitetura. Ainda sem código novo.

Os números de commit citados aqui vêm do histórico do git. Esta seção não é
gerada automaticamente.
