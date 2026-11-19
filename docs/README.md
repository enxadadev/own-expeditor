# Own Expeditor: documentação

O Own Expeditor é um editor de jobs (fretes) para Euro Truck Simulator 2 e
American Truck Simulator. Ele troca ofertas de frete que já existem no save por
origem, destino, carga e reboque escolhidos. O projeto é inspirado no Virtual
Speditor.

Estado atual: o app abre sempre o shell modular. Ele lê perfis e saves,
mostra a lista de fretes (módulo Fretes), monta mudanças numa revisão e grava
com backup e verificação, sobrescrevendo o save ou criando um novo. A
integração planejada na ADR 0006 está concluída (fases F0 a F7 do plano 0006).
Falta o teste no jogo (item 7.5 do plano 0006).

## Onde achar cada coisa

| Documento | Para que serve |
|-----------|----------------|
| [usage.md](usage.md) | Guia do usuário: como abrir o app, backups, restauração e problemas comuns |
| [development.md](development.md) | Pré-requisitos, comandos de build, teste e execução, publicação para Windows, toolkit e convenções |
| [ai-workflow.md](ai-workflow.md) | Como os agentes de IA, as skills (workflows `wf-*` e skills de apoio) e o toolkit se organizam (resumo; a fonte está em `ia/`) |
| [sii-format.md](sii-format.md) | Formato do `game.sii`: camadas ScsC, BSII e SiiNunit, campos de job e comportamento do parser |
| [architecture/overview.md](architecture/overview.md) | Visão geral, diagrama de camadas e fluxo principal de leitura, injeção e escrita |
| [architecture/layers.md](architecture/layers.md) | Responsabilidade de cada projeto em `src/` e a regra de dependência |
| [architecture/domain.md](architecture/domain.md) | Tipos do `Domain` (jogo, perfil, save, ajustes) e onde ficam as entidades do jogo |
| [architecture/data-flow.md](architecture/data-flow.md) | Sequência de abertura do save, planejamento, revisão, gravação (sobrescrever e novo save), backup e caveats do parser |
| [decisions/0001-camadas-com-dependencia-para-baixo.md](decisions/0001-camadas-com-dependencia-para-baixo.md) | ADR: camadas com dependência só para baixo |
| [decisions/0002-gravar-save-em-texto.md](decisions/0002-gravar-save-em-texto.md) | ADR: gravar o save em texto SiiNunit, sem recriptografar |
| [decisions/0003-sem-listas-fixas.md](decisions/0003-sem-listas-fixas.md) | ADR: nenhuma lista fixa de cidades, cargas ou reboques |
| [decisions/0004-suporte-a-tema-e-idioma.md](decisions/0004-suporte-a-tema-e-idioma.md) | ADR: tema Light/Dark/Sistema e idiomas pt-BR/en, com preferências em JSON |
| [decisions/0005-shell-modular-e-sessao-de-edicao.md](decisions/0005-shell-modular-e-sessao-de-edicao.md) | ADR (proposta): shell modular, sessão de edição no Core e escrita atômica do save |
| [architecture/plan-0005-shell.md](architecture/plan-0005-shell.md) | Plano de implementação da ADR 0005: contratos, tarefas por agente e riscos |
| [decisions/0006-integracao-do-codigo-de-injecao-ao-shell.md](decisions/0006-integracao-do-codigo-de-injecao-ao-shell.md) | ADR: integração do código de injeção (BSII, `.scs`, `SaveSession`, `JobInjector`) ao shell; destino da gravação; erros tipados |
| [architecture/plan-0006-integracao.md](architecture/plan-0006-integracao.md) | Plano de implementação da ADR 0006: reaproveitamento, contratos, fases F0 a F7 (concluídas) e riscos |
| [decisions/0007-cadeia-de-escolhas-do-frete-no-core-e-pais-da-cidade-pelo-igamedata.md](decisions/0007-cadeia-de-escolhas-do-frete-no-core-e-pais-da-cidade-pelo-igamedata.md) | ADR: cadeia País → Cidade → Empresa → Carga no Core (`JobSelectionRules`); país da cidade pelo `IGameData` |
| [changelog.md](changelog.md) | Mudanças por versão (`Directory.Build.props`) |

Fora de `docs/`:

- `CLAUDE.md` (na raiz): instruções do projeto para o Claude Code, incluindo as
  regras de backup e de round-trip do save.
- `design/`: fluxos, telas, textos e canvas das interfaces. Nenhuma tela é
  implementada sem desenho aprovado aqui (ver [development.md](development.md#design)).
- `ia/agents/` e `ia/skills/`: agentes e skills de apoio ao desenvolvimento
  (resumidos em [ai-workflow.md](ai-workflow.md)).
- `toolkit/`: scripts de apoio ao desenvolvimento (resumidos em
  [development.md](development.md#toolkit)).

## Como começar

- Quer rodar ou contribuir: leia [development.md](development.md).
- Quer usar o app: leia [usage.md](usage.md).
- Quer entender o save: leia [sii-format.md](sii-format.md).
- Quer entender o código: leia [architecture/overview.md](architecture/overview.md).
