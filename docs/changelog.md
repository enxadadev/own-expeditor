# Changelog

A versão do produto fica em `Directory.Build.props` (`M.X.Y`). Quem a altera é
o agente `git-ops` (ver `ia/agents/git-ops.md`).

## 0.2.0 (em desenvolvimento)

Primeira versão com o app de fretes utilizável. Substitui a tela única da 0.1.0
por um shell com módulos. Ainda sem release publicado; a versão em
`Directory.Build.props` será alterada pelo `git-ops` ao fechar a versão.

### Para o usuário

- **Abertura por perfil e save.** A tela inicial lista perfis e saves
  encontrados em Documentos, `~/.local/share` e Proton, com **Continuar de onde
  parou**. Saves grandes mostram o progresso e podem ser cancelados.
- **Módulo Fretes.** Escolha cidade de saída, cidade de destino, empresa de
  saída, carga e empresa de chegada. As opções vêm do save e das regras de carga
  do jogo, sem lista fixa. O frete vai para a revisão; nada é gravado ainda.
- **Vaga livre ou oferta a substituir.** Uma empresa pode receber o frete numa
  vaga livre ou trocando uma oferta atual (antes → depois na revisão).
- **Prazo de 24 h, 48 h, 72 h ou 7 dias** de tempo de jogo.
- **Distância conhecida ou estimada.** A distância vem das ofertas dos seus
  saves. Quando não há, o app estima e marca como estimada. A tabela conhecida
  fica num cache por jogo (`distances-ets2.json` e `distances-ats.json`) e cresce
  a cada save aberto.
- **Revisão única.** Ctrl+R abre a revisão de todas as mudanças pendentes, com
  descartar por mudança, por módulo ou tudo. Ctrl+Enter abre a confirmação.
- **Sobrescrever o save ou criar um novo.** A confirmação pergunta o destino:
  **Sobrescrever este save** (padrão) ou **Criar um novo save** (o original não é
  tocado). O resultado mostra o backup ou o novo save e os passos para desfazer.
- **Backup e gravação segura.** Antes de sobrescrever, cria
  `game.sii.bak-AAAAMMDD-HHMMSS` ao lado. Grava em `game.sii.tmp`, confere o
  conteúdo gravado e só então troca o arquivo. Se algo falhar, o save original
  fica como estava.
- **Jogo aberto.** O app recusa gravar com o jogo aberto e mostra a barra de
  sessão com o estado do jogo.
- **Configurações.** Tema Claro, Escuro ou Sistema; idioma pt-BR ou English,
  trocado na hora; pasta de instalação do jogo escolhida à mão quando a detecção
  automática não achar.
- **Erros claros.** Erros de leitura e de gravação dizem o que aconteceu e o que
  fazer.
- **Steam Cloud.** Perfis do Steam Cloud mostram aviso antes de gravar.
- **Botão Início no logo.** A cabeça do mascote, no topo da barra lateral, volta
  à seleção de save de qualquer tela. A sessão aberta e as alterações pendentes
  são mantidas. Corrige: depois de sair da seleção de save não havia como voltar.
- **Novo ícone.** A janela e o executável usam a cabeça do mascote.
- **Hover limpo na barra lateral.** Ao passar o mouse, pressionar ou focar um
  item, não aparece mais uma caixa quadrada atrás do ícone; pressionar encolhe
  levemente o item, visível nos temas claro e escuro.

### Mudanças técnicas

- O app sempre abre o shell modular. A opção `--shell` e a tela única anterior
  foram removidas.
- Módulo `Core/Modules/Jobs` (`JobBoard`, `JobPlanner`, `DistanceTable`) e
  `App/Modules/Jobs`. `SaveSession` e `JobInjector` foram substituídos por eles.
- `Core/Legacy` foi removido. `SiiDocument.Save` e `SaveLocator.WriteAsNewSave`
  também: a única escrita de save é `SaveFileStore` (`Data`).
- Erros de leitura do save são tipados: `SiiFormatException` com `SiiErrorKind`
  (`Sii`) e `CoreError` no `Core`.
- `Data/Distances`: cache de distâncias por jogo.
- Projetos de teste novos: `tests/OwnExpeditor.Data.Tests` e
  `tests/OwnExpeditor.App.Tests`. Total: 764 testes passando.
- Textos em `.resx` (pt-BR e en), gerados por `toolkit/gen/gen-resx.py`.
- A CLI `tools/OwnExpeditor.Cli` perdeu o comando `inject`. Restaram `decode`,
  `scs-ls`, `scs-cat` e `catalog`.

### Limitações conhecidas

- Ainda não confirmado no jogo: sobrescrita de save BSII, criação de novo save e
  o comportamento com Steam Cloud (plano 0006, fase F7, item 7.5).
  > TODO: confirmar no jogo antes de publicar.
- Distância estimada tem erro típico de cerca de 25%. O jogo recalcula a rota ao
  dirigir; se ele recalcula o pagamento ao aceitar o frete, ainda é preciso
  confirmar (ver [sii-format.md](sii-format.md), seção 10).
- A unidade de `expiration_time` ainda não foi confirmada no jogo: o app grava o
  prazo como minutos de tempo de jogo.
- Não há módulos além de Fretes: Frota, Perfil, Empresa e Mapa são espaços
  reservados.
- O release para Windows (`toolkit/release/release.sh`) ainda não foi testado num
  ambiente Windows.

## 0.1.0 (não publicado)

Versão anterior à 0.2.0; nunca foi publicada. Estado do que existia no código
nessa versão (histórico; o que vale hoje está em [architecture/](architecture/)):

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
