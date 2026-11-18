# Own Expeditor

Editor de jobs (fretes) para Euro Truck Simulator 2 / American Truck Simulator,
inspirado no Virtual Speditor (forum.scssoft.com t=199114). Substitui job offers
existentes no save (`game.sii`) por origem, destino, carga e reboque escolhidos.

## Stack
- C# / .NET 8, UI em Avalonia (MVVM), testes com xUnit.
- Alvo principal: Windows (exe self-contained). Desenvolvimento em Linux.

## Camadas (dependências só apontam para baixo)
- `src/OwnExpeditor.App`     → UI Avalonia. Depende de Core.
- `src/OwnExpeditor.Core`    → casos de uso (ler save, listar opções, injetar job, backup).
- `src/OwnExpeditor.Data`    → perfis/saves no disco, cache de distâncias.
- `src/OwnExpeditor.Sii`     → parser/writer SII (texto e decrypt do formato binário/criptografado).
- `src/OwnExpeditor.Domain`  → entidades puras (Company, City, Cargo, Trailer, JobOffer). Sem dependências.

## Regras
- Nunca gravar um save sem antes criar backup ao lado (`game.sii.bak-<timestamp>`).
- Não ter listas fixas de cidades/cargas: tudo é lido do save (compatível com mods).
- O writer SII deve fazer round-trip: ler e gravar sem mudanças gera arquivo equivalente.
- Saves de exemplo ficam em `samples/` (não versionar saves pessoais grandes) e
  fixtures pequenos em `tests/**/Fixtures/`.
- Formato SII: documentar descobertas em `docs/sii-format.md` antes de codar o parser.
- Design antes de código: nenhuma tela é implementada sem desenho aprovado
  em `design/` (fluxos, telas, textos e canvas — ver `design/README.md`).
- Tema e idioma: a UI suporta tema Light/Dark (e Sistema) e idiomas pt-BR/en;
  nenhum texto visível fixo no código e nenhuma cor fixa (ver skill `i18n-theming`).

## Agentes
- Agentes e skills ficam em `ia/`; quem faz o quê e o fluxo estão em `ia/README.md`.

## Orquestração obrigatória (OBRIGATÓRIO em toda instrução)
- TODA instrução do usuário DEVE ser executada pelos agentes especializados
  do projeto (`ia/agents/`). A sessão principal nunca trabalha sozinha:
  ela só orquestra — escolhe o workflow, delega, repassa contexto e
  resultados e fala com o usuário.
- SEMPRE seguir um workflow já definido (skills `wf-*`): `wf-feature`,
  `wf-bugfix`, `wf-ui`, `wf-refactor`, `wf-sii-discovery`, `wf-docs`,
  `wf-review`, `wf-codex-review`, `wf-commit`, `wf-release`. Carregar a
  skill do workflow antes de começar.
- SEMPRE usar as skills do projeto (`ia/skills/`) indicadas pelo workflow
  e pelo agente.
- A sessão principal não edita código, testes, docs, design nem roda git
  diretamente: cada arquivo tem um agente dono (tabela em `ia/README.md`).
  Git sempre via `git-ops`; build/testes sempre via `test-runner`.
- Se nenhum workflow se encaixar, escolher o mais próximo e dizer ao
  usuário qual foi usado; se faltar um agente/workflow, propor criá-lo
  em vez de fazer o trabalho sozinho.

## Comandos
- `dotnet build`
- `dotnet test`
- `dotnet run --project src/OwnExpeditor.App`
