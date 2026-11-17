# Changelog

A versão do produto fica em `Directory.Build.props` (`M.X.Y`). Quem a altera é
o agente `git-ops` (ver `ia/agents/git-ops.md`).

## 0.1.0 (em desenvolvimento)

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
- `OWNE-2`: skill `make-interfaces-feel-better`.
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

Os números de commit citados aqui vêm do histórico do git. Esta seção não é
gerada automaticamente.
