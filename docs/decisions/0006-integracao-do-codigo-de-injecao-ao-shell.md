# 0006. Integração do código de injeção ao shell modular

- Status: aceita
- Data: 2026-11-17

## Contexto

O merge OWNE-17 trouxe código que funciona no jogo: leitura de saves binários
(`BsiiDecoder`), leitor de pacotes `.scs` (`HashFsArchive`), catálogo de
empresas e cargas (`GameCatalog`) com textos traduzidos do jogo
(`Localization`), localizador de perfis (`SaveLocator`), `SaveSession` e
`JobInjector` no Core e uma tela única (`MainViewModel`). Esse código foi
escrito antes do ADR 0005: a App usa Data e Sii direto, o `JobInjector` altera
o documento em memória e a gravação cria um slot novo com `SiiDocument.Save`,
sem `.tmp` nem verificação.

Os desenhos aprovados (`design/shell`, `design/review-apply`,
`design/controles`) deixaram perguntas para a arquitetura: códigos de erro ao
abrir, pasta de instalação do jogo, abrir pasta no sistema, etapas de carga,
monitor do jogo, plural, moeda e atalhos.

## Decisão

1. **Reaproveitar, não reescrever.**
   - `BsiiDecoder`, `SiiDecoder`, `HashFsArchive`, `GameCatalog` e
     `Localization` ficam onde estão.
   - `SaveLocator` vira a implementação de `IProfileLocator` e devolve os tipos
     do Domain (`ProfileInfo` com `GameKind` e `IsSteamCloud`).
   - `SaveSession` vira `JobBoard`, índice só de leitura sobre `ISaveView` no
     módulo Jobs do Core.
   - `JobInjector` vira `JobPlanner`, que devolve um `ChangeSet` de patches
     `SetAttribute` (uma `Key` por slot de oferta). `JobInjector.Inject` sobre
     `SiiDocument` fica como adaptador para a CLI e os testes.
   - `MainViewModel` continua até o módulo Fretes do shell ficar pronto, usando
     só Core e Domain por uma fachada transitória (`Core/Legacy/LegacyJobs`).
2. **Nomes do jogo pelo Core.** O Core expõe `IGameNames`/`IGameData`
   (catálogo + textos) por `IGameDataService`; a App não conhece a Data.
   `DiffValue.GameName` leva o tipo do nome (cidade, empresa, carga, reboque,
   caminhão) e é resolvido na hora de exibir. O idioma dos nomes segue o do app
   (pt-BR → `pt_br`; en → `en_us`, `en_gb`); fallback: tradução → nome local →
   id legível. Os nomes vêm da localização do próprio jogo, não dos `.resx`.
3. **Erros ao abrir.** O Sii lança `SiiFormatException` com `SiiErrorKind`.
   Vazio, truncado, falha de decifragem/descompressão, tamanho divergente →
   `SaveCorrupt` ("não foi possível ler"). Formato desconhecido, versão nova,
   tipo de valor desconhecido, sintaxe → `SaveParseFailed` (renomeado de
   `SaveFormatUnsupported`; "não foi possível entender"). Novo `InvalidEdit`
   com motivo `TextRef` para regras de módulo. `CoreException` leva etapa e
   linha para "Copiar detalhes".
4. **Pasta de instalação por jogo.** `AppSettings.GameFolders` guarda a escolha
   manual (nulo = detecção). A Data detecta (registro da Steam,
   `libraryfolders.vdf` em Windows e Linux, Flatpak, pastas padrão) e valida
   (`base.scs`, `def.scs` e o executável do jogo certo). Proton muda só a pasta
   de documentos, que o localizador de perfis também procura. Sem pasta, a
   sessão abre com nomes legíveis e as regras de carga tiradas do save.
5. **Destino da gravação escolhido pelo usuário** na confirmação:
   - **Sobrescrever este save** (padrão): backup `game.sii.bak-<timestamp>`,
     `.tmp`, verificação e substituição (ADR 0005).
   - **Criar um novo save**: copia a pasta do slot para um slot novo livre no
     mesmo perfil, grava o `game.sii` alterado ali (mesmas etapas `.tmp` +
     verificação) e ajusta o nome em `info.sii`; o original não é tocado.
     Em perfil com Steam Cloud o jogo pode não mostrar slots criados fora
     dele: a opção aparece com aviso.
   - Ambos passam pelo mesmo `ApplyAsync(ApplyTarget)` e pela Data
     (`ISaveFileStore`); nenhum outro caminho de escrita existe.
6. **Serviços de plataforma na App:** `IShellLauncher` (abrir pasta e
   selecionar arquivo), `IFolderPicker` e `IClipboardService`.
7. **Carga em três etapas:** `Loading_Stage_Read`, `Loading_Stage_Parse` e
   `Loading_Stage_Index`, com o tamanho do arquivo; o catálogo do jogo é
   carregado com cache na etapa Index.
8. **Jogo aberto:** polling a cada 2 s, só com sessão aberta, como aviso; a
   checagem que vale continua dentro de `ApplyAsync`.
9. **Plural e moeda.** Chaves `_Zero` (opcional), `_One` e `_Other` via
   `ILocalizer.GetPlural`. Dinheiro com `Format_Money_<Jogo>` e número pela
   cultura do app; o save guarda EUR no ETS2 e USD no ATS.
10. **Atalhos:** Ctrl+R abre Revisar; Ctrl+Enter, só na página Revisar, abre a
    confirmação e nunca grava direto. Ctrl+S não é usado.
11. **Migração em fases.** O shell nasce atrás de `--shell` e vira o padrão
    quando o módulo Fretes estiver pronto; os testes existentes ficam verdes em
    cada fase.

Alternativas descartadas:
- Reescrever a injeção dentro do shell: joga fora código validado no jogo.
- A App ler `GameCatalog` direto: viola as camadas (ADR 0001/0005).
- Uma tela só para os erros de abrir: ações e causas diferentes.
- Só um destino de gravação: sobrescrever exige confiança total; só slot novo
  falha com Steam Cloud. O usuário escolhe.
- Ler `.scs` em cada abertura sem cache: custo repetido.

## Consequências

- Um só caminho de escrita (`ApplyAsync` → `ISaveFileStore`), com dois
  destinos, testado contra a skill `save-safety`.
- Validar no jogo: sobrescrever um save binário, criar slot novo, e perfil com
  Steam Cloud.
- Empresas de mods dependem das regras tiradas do save até os pacotes de
  `mod/` serem lidos.
- Ajustar ADR 0002 (BSII suportado), a skill `i18n-theming` (nomes do jogo vêm
  da localização dele), a skill `design-system` e `design/review-apply`
  (escolha de destino na confirmação, `Error_SaveParseFailed`,
  `Error_InvalidEdit`).
- Plano de implementação em `docs/architecture/plan-0006-integracao.md`.
