# Own Expeditor: documentação

O Own Expeditor é um editor de jobs (fretes) para Euro Truck Simulator 2 e
American Truck Simulator. Ele troca ofertas de frete que já existem no save por
origem, destino, carga e reboque escolhidos. O projeto é inspirado no Virtual
Speditor.

Estado atual: a biblioteca de leitura e gravação do save (`src/OwnExpeditor.Sii`)
está implementada e testada. A interface e os casos de uso de injeção de jobs
ainda não existem.

## Onde achar cada coisa

| Documento | Para que serve |
|-----------|----------------|
| [usage.md](usage.md) | Guia do usuário: como abrir o app, backups, restauração e problemas comuns |
| [development.md](development.md) | Pré-requisitos, comandos de build, teste e execução, publicação para Windows, toolkit e convenções |
| [ai-workflow.md](ai-workflow.md) | Como os agentes de IA, as skills (workflows `wf-*` e skills de apoio) e o toolkit se organizam (resumo; a fonte está em `ia/`) |
| [sii-format.md](sii-format.md) | Formato do `game.sii`: camadas ScsC, BSII e SiiNunit, campos de job e comportamento do parser |
| [architecture/overview.md](architecture/overview.md) | Visão geral, diagrama de camadas e fluxo principal de leitura, injeção e escrita |
| [architecture/layers.md](architecture/layers.md) | Responsabilidade de cada projeto em `src/` e a regra de dependência |
| [architecture/domain.md](architecture/domain.md) | Entidades planejadas (Company, City, Cargo, Trailer, JobOffer) |
| [architecture/data-flow.md](architecture/data-flow.md) | Sequência de leitura, escrita e backup, com caveats do parser |
| [decisions/0001-camadas-com-dependencia-para-baixo.md](decisions/0001-camadas-com-dependencia-para-baixo.md) | ADR: camadas com dependência só para baixo |
| [decisions/0002-gravar-save-em-texto.md](decisions/0002-gravar-save-em-texto.md) | ADR: gravar o save em texto SiiNunit, sem recriptografar |
| [decisions/0003-sem-listas-fixas.md](decisions/0003-sem-listas-fixas.md) | ADR: nenhuma lista fixa de cidades, cargas ou reboques |
| [decisions/0004-suporte-a-tema-e-idioma.md](decisions/0004-suporte-a-tema-e-idioma.md) | ADR: tema Light/Dark/Sistema e idiomas pt-BR/en, com preferências em JSON |
| [changelog.md](changelog.md) | Mudanças por versão (`Directory.Build.props`) |

Fora de `docs/`:

- `CLAUDE.md` (na raiz): instruções do projeto para o Claude Code, incluindo as
  regras de backup e de round-trip do save.
- `ia/agents/` e `ia/skills/`: agentes e skills de apoio ao desenvolvimento
  (resumidos em [ai-workflow.md](ai-workflow.md)).
- `toolkit/`: scripts de apoio ao desenvolvimento (resumidos em
  [development.md](development.md#toolkit)).

## Como começar

- Quer rodar ou contribuir: leia [development.md](development.md).
- Quer usar o app: leia [usage.md](usage.md).
- Quer entender o save: leia [sii-format.md](sii-format.md).
- Quer entender o código: leia [architecture/overview.md](architecture/overview.md).
