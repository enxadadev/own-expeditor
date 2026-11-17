# Shell do app (UX-1)

- **Status:** aprovado (2026-11-17)
- **Canvas:** https://claude.ai/artifact/YSdcaqn7AyAqvc6wVuBSYN; fonte em [`canvas/`](canvas/) (`canvas.json` + um `.dc.html` por estado)
- **Origem:** [ADR 0005](../../docs/decisions/0005-shell-modular-e-sessao-de-edicao.md) item 4 e
  [plano 0005](../../docs/architecture/plan-0005-shell.md) tarefa UX-1. Visual: [`../direcao-visual/`](../direcao-visual/README.md) (A+C).

## Objetivo do usuário
Abrir o perfil e o save que quer editar, saber sempre onde está (perfil, save, jogo) e quantas mudanças
estão pendentes, e chegar a qualquer módulo, à revisão e às configurações sem se perder. Se o save não
puder ser lido, entender o motivo e o que fazer.

## Telas e estados (um artboard por estado)
| Canvas | Estado | Variações na barra de prévia |
| --- | --- | --- |
| `SemSessao.dc.html` | Primeira abertura: escolher perfil e save; módulos desabilitados | primeira abertura, com último save, nenhum perfil; lista de saves clicável |
| `Carregando.dc.html` | Lendo o save, com progresso e etapas | 18%, 62%, 94% |
| `ErroInterpretacao.dc.html` | "Não foi possível entender o save": o arquivo foi lido, mas o conteúdo não foi entendido (jogo novo, mod incomum, arquivo danificado) | detalhes técnicos recolhíveis; Copiar detalhes |
| `ErroCorrompido.dc.html` | "Não foi possível ler este save": arquivo incompleto/ilegível (e, no mesmo layout, não encontrado / sem permissão) | corrompido, não encontrado, sem permissão; detalhes recolhíveis |
| `SessaoAberta.dc.html` | Sessão aberta com pendências; área do módulo é só um espaço reservado | 3 pendências, sem pendências, jogo aberto, página Revisar (selo ativo) |
| `Configuracoes.dc.html` | Tema (Claro/Escuro/Sistema) e idioma (pt-BR/en) | os próprios controles trocam o canvas; pasta de instalação por jogo (ETS2 detectada, ATS não encontrada; Escolher…/Usar detecção) |

Todas as telas têm, embaixo, uma barra tracejada de **prévia do canvas** (tema claro/escuro, idioma e variações).
Ela não faz parte do app. Documentos: [flows.md](flows.md), [screens.md](screens.md), [strings.md](strings.md).

## Decisões
1. **Navegação lateral de 76 px** com FRETES, FROTA, PERFIL, EMPRESA, MAPA e, no rodapé, CONFIG. Sem sessão os cinco
   módulos ficam desabilitados (com dica) e fora da ordem de Tab; Configurações sempre funciona.
2. **Tema e idioma saem do topo e da lateral** (como estavam em `CabineRota`) e vão para Configurações, conforme o
   ADR. O topo fica só com sessão: perfil, save, jogo, estado do jogo e o selo "Revisar (n)".
3. **"Revisar (n)" fica no topo, à direita**, e é o único ponto de entrada (ADR). Na direção visual aprovada o botão
   ficava no canto inferior direito de cada módulo; passa a ser do shell, para os módulos não repetirem. O rodapé do
   shell guarda só a nota de segurança e as dicas de atalho.
4. **Perfil e save são dois botões** que abrem o mesmo seletor (`ProfileSavePicker`) em um flyout. Com pendências,
   trocar de save pede confirmação destrutiva (texto em `strings.md`; visual no UX-2).
5. **Estado do jogo visível** (fechado/aberto): aplicar falha com `GameRunning`; melhor avisar antes. Revisar continua
   possível com o jogo aberto, só aplicar é que não.
6. **Erro ao abrir o save tem uma tela própria no lugar do módulo**, nunca um diálogo, e são **duas telas**:
   "Não foi possível entender o save" (o arquivo foi lido, o conteúdo não foi entendido: `SaveFormatUnsupported`) e
   "Não foi possível ler este save" (incompleto/ilegível, não encontrado, sem permissão: `SaveCorrupt` e afins). O app
   decodifica saves em texto e binários, então não há mais tela "formato binário" nem instrução de `g_save_format`.
   O texto nunca usa termos do formato (`BSII`, `SII`, `unit`). A tela "entender" oferece Copiar detalhes para reportar.
7. **Nada abre sozinho**: mesmo com último save conhecido, o app mostra "Continuar de onde parou" em vez de
   reabrir (saves grandes, jogo pode ter salvado de novo).
8. **Escolha de save sem pré-seleção**: "Abrir save" fica desabilitado até o usuário escolher.
9. **Atalhos**: Ctrl+R revisar (não Ctrl+S, que lembra "salvar"; nada é gravado ao revisar), Ctrl+Enter abre a confirmação de gravação (só na página Revisar), Ctrl+O trocar/abrir save, Ctrl+, configurações,
   Ctrl+1…5 módulos, Esc fecha/cancela.
10. **Revisar e Resultado são páginas do shell** (alinhado a `design/review-apply/`): abrem na área principal, sem módulo
    ativo na lateral; na página Revisar o selo do topo fica "ativo" (borda e fundo âmbar suave, `aria-current=page`).
    O canvas `SessaoAberta` tem a variação "Página Revisar" para mostrar isso. Os botões **Aplicar** (revisão e diálogo
    de confirmação) e os confirmadores destrutivos do shell ("Descartar e trocar", "Descartar e sair") usam
    `Button.danger` (token `OnDangerColor`, definido no UX-2); o selo Revisar continua âmbar porque só abre a revisão.
11. **Janela mínima 960×600** (aprovado).
12. **Falha ao abrir um save, com outro já aberto, deixa o app sem sessão** (aprovado); mantém o fluxo 2.
13. **Pasta de instalação do jogo, por jogo, em Configurações** (seção "Jogos", linha ETS2 e linha ATS): o app localiza
    a instalação sozinho; o usuário pode escolher a pasta manualmente e voltar à detecção. Serve para, no futuro, ler os
    arquivos atualizados do jogo e desenhar o mapa. "Abrir pasta" na tela de erro passou a abrir a pasta **do save** (ver o ajuste abaixo).

## Componentes NOVOS (entram em `design-system` e na Galeria quando implementados)
`SessionBar`, `NavItem`, `ProfileSavePicker`, `SegmentedChoice`, `DetailsDisclosure`, `FolderField` (caminho em mono + status + Escolher…/Usar detecção automática/Abrir pasta); extensões de
`LoadingState` (progresso + etapas) e `ErrorState` (conteúdo livre, detalhes recolhíveis). Detalhes em [screens.md](screens.md).

## Ajuste aprovado: o app decodifica saves binários
A tela "save binário não suportado" e toda instrução de `g_save_format "2"` foram removidas. Entrou
`ErroInterpretacao.dc.html` ("Não foi possível entender o save"). `ErroCorrompido` continua, para arquivo ilegível,
não encontrado e sem permissão. Diferença entre as duas: ver [screens.md](screens.md) seção 4.

## Respondido pelo usuário (2026-11-17)
1. Atalho de revisar: **Ctrl+R** (não Ctrl+S). `design/review-apply/` ainda cita Ctrl+S e precisa ser ajustado.
2. Falha ao abrir com outro save aberto: **fica sem save**.
3. Abrir pasta aprovado (hoje como "Abrir pasta do save" no erro de interpretação); Configurações ganha a pasta de instalação por jogo (decisão 13).
4. Tamanho mínimo da janela **960×600**.

## Perguntas em aberto
0. **Nome do código de erro:** `CoreError.SaveFormatUnsupported` agora significa "arquivo lido, conteúdo não entendido"; o nome
   sugere "não suportado". Renomear (ex.: `SaveParseFailed`) e confirmar se o Core separa isto de `SaveCorrupt` (arquivo
   ilegível). Se não separar, as duas telas de erro viram uma. Levar ao `chief-architect`.
1. **Serviço `IShellLauncher`** (abrir pasta no sistema) na App ou no Core? Atendem "Abrir pasta do save" (erro), "Abrir pasta" da
   instalação e o "Abrir pasta" do Resultado (UX-2).
2. **Pasta de instalação do jogo precisa de contrato novo:** `AppSettings` hoje só tem tema, idioma e `LastSavePath`;
   faltam o caminho manual por jogo (`GameKind` → caminho ou nulo) e um localizador da instalação na Data (Steam, registro,
   pastas padrão), com validação ("parece mesmo ser o ETS2/ATS?") que devolve `Settings_GameFolder_Invalid`. Levar ao `chief-architect`.
3. **Detecção automática em Linux/Proton e Steam em outro disco**: fica só a escolha manual quando falha? (Depende da
   descoberta `wf-sii-discovery` sobre de onde ler `.scs` e mods.)
4. **Etapas de carga:** o Core precisa emitir `LoadProgress.StageKey` ∈ `Loading_Stage_Read`, `Loading_Stage_Parse`,
   `Loading_Stage_Index` (ou outra lista; os textos acompanham). Confirmar com o `chief-architect`.
5. **Flyout de troca de save** e **diálogos de descarte** não foram desenhados em detalhe (reaproveitam `ProfileSavePicker`
   e o diálogo destrutivo do UX-2; botão de confirmar em `Button.danger`).
6. **Lateral nas páginas Revisar/Resultado:** proposta é nenhum módulo ativo; confirmar com o UX-2 (os mockups de
   `review-apply` são simplificados).
7. **Skill `design-system`** diz que o `AppShell` tem tema e idioma no topo e que o resultado é um Toast; ambos mudam
   (este desenho e o UX-2). Atualizar a skill quando aprovado (fora do escopo do `ux-designer`).
8. **Monitor do jogo:** `IGameProcessMonitor.IsRunning` por polling (sugestão: a cada 2 s, só com sessão aberta). Confirmar o custo com o `core-dev`.
