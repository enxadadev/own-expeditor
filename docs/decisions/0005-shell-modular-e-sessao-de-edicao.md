# 0005. Shell modular e sessão de edição

- Status: proposta
- Data: 2026-11-17

## Contexto

O Own Expeditor vai deixar de ser só um editor de fretes e virar um conjunto de
módulos que editam o save do ETS2/ATS: Fretes primeiro, depois Caminhões e
Reboques, Perfil (dinheiro, XP, skills), Empresa (garagens, motoristas) e Mapa
(descobertas). Todos seguem o mesmo ciclo: escolher perfil e save, explorar,
editar, revisar as mudanças (antes → depois), aplicar com backup e ver a
confirmação.

Sem um desenho comum, cada módulo leria o save por conta própria, gravaria
separado (vários backups, risco de um sobrescrever o outro) e repetiria telas
de revisão e confirmação. Hoje a única gravação é `SiiDocument.Save`, que faz
backup mas escreve direto no arquivo final, sem `.tmp` nem verificação, o que
não cumpre a skill `save-safety`.

A ADR 0004 diz que a Data implementa uma interface "definida no Core". Isso
não é possível: o Core referencia a Data, e o contrário criaria um ciclo.

## Decisão

1. **Sessão de edição no Core** (`IEditSession`, `IEditSessionHost`).
   - Carrega o save uma vez e guarda os bytes originais e o documento lido,
     que nunca é alterado.
   - Os módulos não mexem no documento: enfileiram `ChangeSet`s, cada um com
     patches (`SetAttribute(unidade, atributo, valorAntigo, valorNovo)`) e
     linhas de diff já com significado (`DiffLine` com `TextRef`/`DiffValue`).
   - Leitura por `ISaveView`: `View` mostra o original com os pendentes por
     cima; `Original` mostra o save como foi lido.
   - Um `ChangeSet` com a mesma `Key` substitui o anterior. Um patch em
     atributo já alterado por outra `Key` é recusado (`Conflict`). Um patch
     cujo `valorAntigo` não bate com o valor atual é recusado (`StalePatch`).
   - É possível descartar uma mudança, um módulo ou tudo.
   - **Aplicar é atômico:** confere se o jogo está aberto e se o arquivo não
     mudou no disco (tamanho, data e SHA-256); faz o parse de uma cópia nova
     a partir dos bytes; aplica todos os patches; gera o texto e entrega à
     Data, que faz backup `game.sii.bak-<yyyyMMdd-HHmmss>`, grava
     `game.sii.tmp` com `Flush(true)`, verifica e só então substitui. Uma
     gravação cobre todos os módulos. Se qualquer passo falhar, o original
     fica intacto. Depois de gravar, a cópia vira a nova base.
   - `SiiDocument.Save` fica obsoleto. A única gravação de save é
     Core (`IEditSession.ApplyAsync`) → Data (`ISaveFileStore.WriteAtomicAsync`).

2. **Interfaces da Data ficam na Data** (`ISaveFileStore`, `IProfileLocator`,
   `IGameProcessMonitor`, `ISettingsStore`), com implementação interna. O Core
   as consome e expõe à App os próprios serviços (`IProfileService`,
   `IAppSettingsService`). Isto corrige o trecho correspondente da ADR 0004.

3. **O Core devolve códigos, a App traduz.** Erros são `CoreException` com
   `CoreError`. Textos são `TextRef` (chave + argumentos), e valores do diff
   são `DiffValue`: valor do save sem tradução, nome legível do id, número
   com formato da cultura, dinheiro ou texto traduzível. Cada módulo do Core
   lista as chaves que emite em `XxxTextKeys`, e um teste confere que elas
   existem nos dois `.resx`.

4. **Shell da UI.** Navegação lateral compacta com os módulos e, embaixo,
   Configurações. A barra superior tem perfil, save, jogo e o selo
   "Revisar (n)". Revisar, aplicar (com confirmação de ação destrutiva) e a
   tela de resultado, com o caminho do backup, são do shell e servem a todos
   os módulos. Os módulos só exploram e editam. Tema (Light/Dark/Sistema) e
   idioma (pt-BR/en) mudam em runtime e ficam salvos em `settings.json`.
   Visual conforme a direção aprovada em `design/direcao-visual/`.

5. **Registro explícito dos módulos.**
   - Core: `AddXxxModule()` em `Core/Modules/Xxx/`, chamado por
     `AddOwnExpeditorCore()`.
   - App: `AddModule<TPage, TView>(id, chaveDoTitulo, ícone, ordem)`, que
     registra a página, o `ModuleDescriptor` e a ligação no `ViewRegistry`.
   - O `ViewLocator` deixa de usar reflection.
   - Um módulo novo fica em `Domain/<Mod>`, `Core/Modules/<Mod>`,
     `App/Modules/<Mod>` e, só se precisar de disco além do save,
     `Data/<Mod>`. O `Sii` continua genérico.

6. **Biblioteca de controles e galeria.** Controles reutilizáveis em
   `src/OwnExpeditor.App/Controls`, sem ViewModel, com tokens em `Styles/`
   (skill `design-system`). Uma galeria de componentes aparece só em Debug e
   fica fora da compilação Release.

7. **Telas renderizáveis sem save real.**
   - Toda View tem construtor sem parâmetros e não busca serviços.
   - Bindings compilados, sem texto literal e só com cores `DynamicResource`.
   - O construtor do ViewModel não faz I/O.
   - Toda página fornece amostras dos estados carregando, vazio, erro e
     pronto (`IDesignSampleProvider`). Elas usam
     `EditSessionFactory.FromText` com SII sintético, só com ids fictícios.
   - Screenshots de cada amostra em Light/Dark × pt-BR/en (`.screenshots/`)
     para revisão visual; falha se houver erro de binding.

8. **Camadas.** A App pode referenciar Domain (records puros), mas não
   Sii nem Data. O csproj da App usa `DisableTransitiveProjectReferences`, e
   `toolkit/check-layers.sh` rejeita `using` de Sii/Data na App.

9. **Design antes de código.** Cada tela tem desenho aprovado em
   `design/<tema>/` (fluxos, telas, textos e canvas) antes da implementação.

Alternativas descartadas:
- Módulos alteram o `SiiDocument` direto e o diff sai de uma comparação: o
  diff fica sem significado, descartar exige reverter, e um módulo afeta o
  outro sem controle.
- Reverter o documento em memória se a gravação falhar: fica complexo quando
  houver criação de blocos. Reparsear os bytes é simples e sempre correto.
- Descobrir módulos por reflection ou MEF: ordem não determinística,
  incompatível com trimming do exe self-contained, e não há plugins externos
  (mods do jogo não são código).
- Gravação separada por módulo: vários backups por sessão e risco de um
  módulo gravar sobre a mudança de outro.

## Consequências

- Todas as regras de `save-safety` ficam num só caminho de escrita, testado
  uma vez, que vale para todos os módulos.
- Um módulo novo não precisa de tela de revisão, confirmação nem backup
  próprios. Basta montar `ChangeSet`s.
- Mudanças de vários módulos são aplicadas juntas, com um único backup.
- Se o arquivo mudar no disco (por exemplo, o jogo salvou), aplicar falha com
  `SaveChangedOnDisk` e o usuário precisa reabrir o save. As mudanças
  pendentes se perdem.
- Hoje só existe o patch `SetAttribute`. Módulos que criam ou removem blocos
  ou mudam arrays (Caminhões, Empresa) vão precisar de novos tipos de patch,
  decididos no ADR de cada módulo, com verificação de contagem de blocos.
- Durante a aplicação, o uso de memória fica perto do dobro do documento.
- Toda tela nova precisa de desenho em `design/` e de amostras de design.
- O risco da ADR 0002 (o jogo carregar o save em texto puro) passa a valer
  para todos os módulos. A primeira aplicação real precisa ser testada no jogo.
- O mapa da rota depende de coordenadas de cidades que não estão no save
  (vêm dos arquivos de definição do jogo/mods); exige descoberta própria.
- A ADR 0004 continua valendo, com o ajuste do item 2.

## Atualização (2026-11-17)

Esta ADR continua em proposta. A integração do código já existente ao shell
(leitura BSII, `.scs`, catálogo, `SaveSession` e `JobInjector`) está na
[ADR 0006](0006-integracao-do-codigo-de-injecao-ao-shell.md), com o plano em
[../architecture/plan-0006-integracao.md](../architecture/plan-0006-integracao.md).
Pontos que a ADR 0006 altera nesta ADR:

- **Destino da gravação.** `IEditSession.ApplyAsync` recebe um `ApplyTarget`:
  **Sobrescrever este save** (padrão, com backup, `.tmp` e verificação) ou
  **Criar um novo save** (copia a pasta do slot para um slot livre; o original
  não é tocado). Os dois passam pelo mesmo `ApplyAsync` e pela Data.
- **Códigos de erro.** `SaveFormatUnsupported` foi renomeado para
  `SaveParseFailed`. `SaveCorrupt` e `InvalidEdit` foram acrescentados ao
  `CoreError`. Erros de leitura do Sii são `SiiFormatException` com
  `SiiErrorKind`.
- **Estado do código.** A escrita atual (`SaveLocator.WriteAsNewSave`) só cria
  slot novo, sem `.tmp` nem verificação. `ApplyAsync` e `ISaveFileStore` ainda
  não existem no código.
