# Telas do shell

Janela 1280×800 (mínimo 960×600, aprovado). Componentes do catálogo (`design-system`) em `código`; o que não existe
está marcado **NOVO**. Textos: [strings.md](strings.md) (todas as chaves abaixo). Mockups: [canvas/](canvas/).

## 0. Estrutura comum

```
+------+----------------------------------------------------------------------+
| logo | [Perfil ▾ Demo A] [Save ▾ Save 4] (ETS2) ● Jogo fechado   [Revisar (3)] |  <- SessionBar, 60 px
|      +----------------------------------------------------------------------+
| NAV  |                                                                      |
| 76px |                       ÁREA DO MÓDULO / TELA DE ESTADO                |  <- main
|      |                                                                      |
| FRET |                                                                      |
| FROT |                                                                      |
| PERF |                                                                      |
| EMPR |                                                                      |
| MAPA +----------------------------------------------------------------------+
| ---- | Nada é gravado sem revisão...             Ctrl+R Revisar  Ctrl+Enter |  <- rodapé, 36 px
| CONF |                                                                      |
+------+----------------------------------------------------------------------+
```

Componentes:
| Parte | Componente | Observações |
| --- | --- | --- |
| Janela | `AppShell` | Um só. Contém nav, `SessionBar`, `main` (conteúdo da página atual) e rodapé. Tema e idioma **não** ficam mais aqui (ver README, decisão 2). |
| Navegação | **NOVO** `NavItem` (dentro do `AppShell`) | 60×60 (área de clique ≥ 44), ícone 22 + rótulo curto em caixa alta (`Barlow Condensed` 12/600). Estados: normal, `:pointerover`, ativo (fundo `Surface`, barra âmbar de 3 px no topo, `aria-current=page`), `:focus-visible` (anel âmbar 2 px), desabilitado (opacidade 0,4, ToolTip `Nav_DisabledHint`). Fonte do rótulo: `ModuleDescriptor.TitleKey` em caixa alta por conversor; Configurações usa `Nav_Settings_Short` (cabe em 60 px). |
| Topo | **NOVO** `SessionBar` | Perfil, save, selo do jogo, estado do jogo, selo Revisar. Detalhes na seção 1. |
| Selos | `Badge` | Selo do jogo (ETS2/ATS) e tipo de save (Automático/Rápido/Manual). |
| Rodapé | texto + `kbd` | Nota de segurança sempre; dicas de atalho só com sessão. |

Ícones (traço 1,8, 24×24): Fretes = caixa; Frota = caminhão; Perfil = pessoa; Empresa = prédio; Mapa = mapa dobrado;
Configurações = ajustes (três barras com marcadores). Detalhe do desenho: `canvas/*.dc.html` (`ICONS`).

Responsividade (≥ 960 px): a nav não muda; abaixo de ~1100 px o texto do estado do jogo some (fica só o ponto,
com ToolTip); chips de perfil/save truncam com reticências e ToolTip com o nome inteiro (nomes de mods são longos).
Textos em inglês têm folga de ~30%.

## 1. Barra de sessão (`SessionBar`)

| Elemento | Conteúdo | Comportamento |
| --- | --- | --- |
| Botão Perfil | legenda `SessionBar_Profile` + nome do perfil + seta | abre o flyout de troca (`ProfileSavePicker`), ToolTip `SessionBar_ChangeHint` |
| Botão Save | legenda `SessionBar_Save` + nome do save + seta | idem |
| Selo do jogo | `Game_Ets2_Short` / `Game_Ats_Short` | só informação; nome acessível = nome completo do jogo |
| Estado do jogo | ponto + `SessionBar_GameClosed` / `SessionBar_GameRunning` | ponto verde (fechado) ou vermelho (aberto); ToolTip do aberto = `SessionBar_GameRunningHint`; consulta `IGameProcessMonitor` |
| Selo Revisar | `SessionBar_Review` ({0}) | ver abaixo |

Selo "Revisar (n)":
| Situação | Visual | Nome acessível |
| --- | --- | --- |
| n > 0 | botão âmbar cheio (`OnAccent`), números `+tnum` | `SessionBar_ReviewAutomation` com `Shell_Pending_One/Other` |
| n = 0 | contorno cinza, texto `SessionBar_ReviewNone`, `aria-disabled`, fora do Tab | `SessionBar_ReviewAutomationNone` |
| na página Revisar | borda e fundo âmbar suaves ("ativo"), `aria-current=page`; Ctrl+R não faz nada | idem |
| sem sessão | oculto | — |
| Ctrl+R | com n > 0 abre a revisão (UX-2); com n = 0 não faz nada | — |

Estados da barra: sem sessão (texto `SessionBar_NoSession`, sem botões), carregando (perfil/save/jogo, sem selo),
erro (perfil e save com borda `Danger`), aberta, aberta com jogo aberto.
Não é o lugar de tema/idioma/ajuda.

## 2. Sem sessão — "Abra um save" (`SemSessao.dc.html`)

```
 NAV (desabilitada)   Nenhum save aberto
                      ABRA UM SAVE
                      Escolha o perfil e o save...
                      +- Continuar de onde parou ------------------ [Abrir de novo] -+   (só se LastSavePath)
                      | Demo A · Save 4 · 17/11/2026 14:32                            |
                      +----------------------------------------------------------------+
                      PERFIS                       SAVES DE DEMO A
                      [ETS2 Demo A      5 saves]*  [Save 4              Manual   ]
                      [ETS2 Demo B      3 saves]   [Autosave            Automático]
                      [ATS  Demo ATS com nome...]  [Quicksave           Rápido   ]
                      ...                          ...
                      dica: Escolha um save...           [Escolher pasta…] [Abrir save]
```

- Componentes: **NOVO** `ProfileSavePicker` (duas listas `role=listbox`), `Badge`, `EmptyState`, botões padrão.
- `ProfileSavePicker` — props: perfis (`ProfileInfo`), saves do perfil (`SaveSlotInfo`), perfil e save selecionados.
  Itens: perfil = selo do jogo + nome + "{n} saves"; save = nome + data/hora (cultura) + selo do tipo. Seleção âmbar
  (`Selection`/`Accent`). Nome longo trunca com ToolTip. Eventos: `ProfileSelected`, `SaveSelected`, `SaveActivated`
  (Enter ou duplo clique = abrir). Estados: carregando perfis/saves (`LoadingState` pequeno), vazio, erro, conteúdo.
- Estados da tela:
  | Estado | O que aparece |
  | --- | --- |
  | Primeira abertura | listas; primeiro perfil já selecionado; nenhum save escolhido; **Abrir save** desabilitado; dica `Welcome_PickHint` |
  | Com último save | acima, cartão `Welcome_Continue` com **Abrir de novo** (ação principal âmbar); listas abaixo |
  | Save escolhido | dica vira "{perfil} · {save}"; **Abrir save** âmbar |
  | Nenhum perfil | `EmptyState` com `Welcome_NoProfilesTitle/Body` e **Escolher pasta…** como ação principal; sem listas |
  | Perfil sem saves | coluna de saves mostra `Welcome_NoSavesTitle/Body` (`EmptyState` compacto) |
  | Carregando perfis | `LoadingState` com `Welcome_Searching` no lugar das listas |
- Ordem de Tab: (1) Configurações — os módulos estão fora do Tab; (2) botão Abrir de novo, se houver; (3) lista de
  perfis (um Tab, setas movem); (4) lista de saves (idem); (5) Escolher pasta…; (6) Abrir save (se habilitado).
  Foco inicial: lista de perfis (ou **Abrir de novo** se existir).
- Atalhos: Enter na lista de saves abre; Ctrl+O foca a lista de perfis; Setas ↑↓ nas listas; Esc nada.

## 3. Carregando (`Carregando.dc.html`)

```
 Demo A | Save 4 | ETS2
             +----------------------------------------+
             | LENDO SAVE…                  86,4 MB   |
             | Demo A · Save 4                        |
             | [██████████████░░░░░░░░░]              |
             | Interpretando o save               62% |
             | (✓) Lendo o arquivo                    |
             | ( ) Interpretando o save               |
             | ( ) Organizando fretes e empresas      |
             | Saves grandes podem levar...  [Cancelar]|
             +----------------------------------------+
```
- Componente: `LoadingState` **com variante de progresso** (barra determinada, texto da etapa, percentual `+tnum`,
  lista de etapas com ✓/atual/pendente, tamanho do arquivo, **Cancelar**). Alimentado por `LoadProgress(Percent, StageKey)`.
  Sem percentual conhecido (`Percent < 0`): barra indeterminada e sem lista.
- Menos de ~300 ms: não mostrar a tela (o skill `avalonia-polish` pede para evitar piscada); mostrar só depois de 300 ms.
- Módulos desabilitados; selo oculto. Cancelar volta para Sem sessão.
- Acessibilidade: seção `role=status` com `aria-live=polite` (anuncia a mudança de etapa, não cada percentual);
  barra `role=progressbar` com nome `Loading_Automation` e `aria-valuenow`.
- Ordem de Tab: Configurações → Cancelar. Foco inicial: Cancelar. Esc = Cancelar.

## 4. Erro ao abrir o save

O app lê saves em texto e binários, então "formato não suportado" não existe mais como tela. Há **duas** telas de erro,
com causas e ações diferentes:

| | Não foi possível entender o save (4.1) | Não foi possível ler este save (4.2) |
| --- | --- | --- |
| Canvas | `ErroInterpretacao.dc.html` | `ErroCorrompido.dc.html` |
| `CoreError` | `SaveParseFailed` (formato/conteúdo não reconhecido) | `SaveCorrupt`; também `SaveNotFound`, `AccessDenied` e outros |
| O que aconteceu | o arquivo **foi lido**, mas o conteúdo não foi entendido | o arquivo **não pôde ser lido por inteiro** (incompleto, ilegível, sumiu, sem permissão) |
| Causas mostradas | versão nova do jogo, mod incomum, arquivo danificado | só a causa do caso (corrompido: jogo fechou ao salvar; não encontrado: movido/apagado; permissão: outro programa ou pasta protegida) |
| Ação principal | Escolher outro save | Escolher outro save |
| Extras | Abrir pasta do save, **Copiar detalhes** (para reportar), detalhes técnicos recolhidos | detalhes recolhidos só no corrompido (código e linha) |
| Quando "Tentar de novo" ajuda | depois de atualizar o app, ou de salvar de novo no jogo | depois de o jogo terminar de salvar ou de resolver a permissão |

### 4.1 Não foi possível entender o save (`ErroInterpretacao.dc.html`) — `CoreError.SaveParseFailed`
```
 Demo A | Save 4 | ETS2   (bordas vermelhas)
  [⚠] NÃO FOI POSSÍVEL ENTENDER O SAVE
      O arquivo foi lido, mas o Own Expeditor não entendeu o que está dentro dele. Seu save não foi alterado.
  +- O QUE PODE TER CAUSADO ISSO ---------------------------------------+
  | • Uma versão do jogo mais nova do que a que o app conhece.         |
  | • Um mod que muda o jeito como o save é gravado.                   |
  | • Um arquivo danificado.                                           |
  +--------------------------------------------------------------------+
  • Tente outro save do mesmo perfil, como o salvamento automático mais recente.
  • Se continuar acontecendo, copie os detalhes técnicos e envie ao relatar o problema.
  > Detalhes técnicos   (recolhido: Código, Jogo, Perfil, Save, Tamanho, Etapa, App)
  [Escolher outro save] [Tentar de novo]        Abrir pasta do save  [Copiar detalhes]
```
- Componentes: `ErrorState` **estendido** (ícone, título, texto, conteúdo livre, ações) e **NOVO** `DetailsDisclosure`
  (botão com seta + bloco mono recolhido, `aria-expanded`); o mesmo disclosure é usado em 4.2.
- **Copiar detalhes** copia o texto de `OpenError_Parse_DetailsText` mesmo com o disclosure recolhido; o botão vira
  "Copiado" por ~2 s (região viva `aria-live=polite`). Os detalhes nunca incluem conteúdo do save; só código, jogo, perfil,
  save, tamanho, etapa em que falhou e versão do app. Os valores `Etapa` usam os textos de `Loading_Stage_*`.
- **Abrir pasta do save** abre a pasta do arquivo no sistema (serviço novo, `IShellLauncher`; ver pergunta 1 do README).
  Sem o serviço, o botão não aparece.
- Ação principal: **Escolher outro save** (reler o mesmo arquivo raramente muda o resultado); secundária: **Tentar de novo**.
- Foco inicial: o título (foco programático; o leitor de tela lê o erro). Tab: Configurações → Detalhes técnicos →
  Escolher outro save → Tentar de novo → Abrir pasta do save → Copiar detalhes.
- Região `role=alert`. Nenhum termo do formato do arquivo aparece.

### 4.2 Não foi possível ler este save (`ErroCorrompido.dc.html`) — `CoreError.SaveCorrupt`
```
  [⚠] NÃO FOI POSSÍVEL LER ESTE SAVE
      O arquivo não pôde ser lido por inteiro: parece incompleto ou ilegível... Seu save não foi alterado.
      • Tente o salvamento automático mais recente do mesmo perfil.
      • Ou abra o jogo, carregue o perfil e salve de novo.
      > Detalhes técnicos (recolhido)   Código: SaveCorrupt · perto da linha 18.204
  [Escolher outro save] [Tentar de novo]
```
- Mesmo `ErrorState`; **Detalhes técnicos** é o mesmo `DetailsDisclosure` (`aria-expanded`), recolhido por padrão, com o código
  do erro e a linha (número pela cultura). Nunca mostrar texto do arquivo.
- Ação principal: **Escolher outro save**; secundária: **Tentar de novo**.
- O mesmo layout serve para `SaveNotFound` (`OpenError_NotFound_*`) e `AccessDenied` (`OpenError_AccessDenied_*`),
  sem as dicas, e para qualquer outro `CoreError` ao abrir (`OpenError_Unknown_*`).
- Ordem de Tab: Configurações → Detalhes → Escolher outro save → Tentar de novo. Foco inicial: título.
- Sem **Copiar detalhes** nem **Abrir pasta do save** (a ação do usuário é escolher outro arquivo); se o `core-dev` distinguir
  `SaveCorrupt` de falha de leitura, os dois botões também cabem aqui (pergunta 2 do README).

## 5. Sessão aberta (`SessaoAberta.dc.html`)

```
 [Demo A ▾][Save 4 ▾] (ETS2) ● Jogo fechado                     [Revisar (3)]
 FRETES    6.485   |  (área do módulo: lista | mapa/painel — design/fretes)
 [cartões ...]
```
- A área do módulo é só espaço reservado: o conteúdo é de `design/fretes` (e dos demais módulos).
  O shell fornece `PageHeader` (título do módulo em caixa alta + contagem) e o conteúdo da `ModulePageViewModel`.
- Estados do módulo (carregando/vazio/erro/conteúdo) são do módulo; o shell não os mascara.
- Variações do canvas: com 3 pendências (selo âmbar), sem pendências (selo desabilitado), jogo aberto (ponto vermelho).
- Ordem de Tab (esquerda→direita, cima→baixo): (1) navegação de módulos — **um** Tab, setas ↑↓ movem, Enter/Espaço ativam;
  (2) Configurações; (3) botão Perfil; (4) botão Save; (5) selo Revisar (se habilitado); (6) conteúdo do módulo na ordem
  definida por ele; o rodapé não tem controles. Shift+Tab inverte.
- Landmarks: `navigation` "Módulos", `banner`/região "Sessão", `main` com o nome do módulo.

### 5.1 Páginas Revisar e Resultado (do shell, desenhadas em `design/review-apply/`)
- Abrem na área principal quando o usuário usa o selo ou Ctrl+R (Revisar) e depois de aplicar com sucesso (Resultado).
  Nenhum módulo fica ativo na lateral (proposta; pergunta 6); a nav continua habilitada e clicar em um módulo sai da página.
- Selo da barra em estado "ativo" na página Revisar (canvas `SessaoAberta`, variação "Página Revisar").
- Rodapé: aparece também a dica Ctrl+Enter (abre a confirmação), só nesta página.
- **Aplicar** (na revisão e no diálogo de confirmação) e os confirmadores destrutivos do shell usam `Button.danger`
  (vermelho, `OnDangerColor`); âmbar fica para o selo, seleção e progresso. Foco inicial dos diálogos destrutivos em Cancelar / "Continuar editando".
- Trocar de save ou sair com pendências usa o mesmo diálogo destrutivo (`Shell_SwitchSave_*`, `Shell_Exit_*`).
- Esc volta ao módulo anterior.

## 6. Configurações (`Configuracoes.dc.html`)

```
 CONFIGURAÇÕES
 +- APARÊNCIA ---------------------------------------------------------+
 | Tema     Sistema segue o tema do seu computador.   [Claro|Escuro|Sistema] |
 | -----------------------------------------------------------------   |
 | Idioma   Muda na hora, sem reiniciar.        [Português (Brasil)|English] |
 | ✓ Preferência salva                                                 |
 +---------------------------------------------------------------------+
 Suas mudanças pendentes não são afetadas.
 +- JOGOS · PASTA DE INSTALAÇÃO DO JOGO --------------------------------+
 | Usada para ler os dados do jogo (como o mapa)...                    |
 | Euro Truck Simulator 2   ● Detectada automaticamente                |
 | [C:\...\Euro Truck Simulator 2 ]  [Escolher…] [Abrir pasta]          |
 | American Truck Simulator ● Não encontrada                           |
 | [—                              ]  [Escolher…]                      |
 | Não encontramos o American Truck Simulator. Escolha a pasta...      |
 +---------------------------------------------------------------------+
 +- SOBRE ------------------------------+
 | OWN EXPEDITOR  Versão 0.1.0          |
 | Projeto da comunidade, sem ligação...|
 +--------------------------------------+
```
- Componentes: **NOVO** `SegmentedChoice` (grupo de opções exclusivas, `role=radiogroup`; cada opção `role=radio`;
  Tab entra uma vez, ←/→ movem e já aplicam, Espaço/Enter confirmam; opção marcada = âmbar cheio; altura 40, área ≥ 40),
  `FieldRow`-like para rótulo + ajuda + controle, `Badge` não usado.
- Troca imediata, sem botão Salvar. Mostra "✓ Preferência salva" (região viva `role=status`, texto some após ~3 s).
  Falha ao gravar `settings.json`: mensagem inline `Settings_SaveFailed` no lugar do "salvo" (cor `Danger`).
- Pendências não são afetadas; a nota só aparece quando há sessão com pendências.
- Valores de idioma são endônimos (iguais nos dois idiomas) e não seguem a língua atual.
- **Pasta de instalação do jogo (por jogo)**: componente **NOVO** `FolderField` (caminho em mono truncado no meio com ToolTip
  do caminho inteiro, selo de status, botões). Estados por linha:
  | Estado | Status (ponto) | Botões | Texto extra |
  | --- | --- | --- | --- |
  | Detectada | `Settings_GameFolder_Auto` (verde) | Escolher…, Abrir pasta | — |
  | Escolhida por você | `Settings_GameFolder_Manual` (verde) | Escolher…, Usar detecção automática, Abrir pasta | — |
  | Não encontrada | `Settings_GameFolder_NotFound` (vermelho) | Escolher… | `Settings_GameFolder_NotFoundHelp` (consequência: sem mapa) |
  | Pasta inválida (ao escolher) | mantém o estado anterior | idem | erro inline `Settings_GameFolder_Invalid` |
  Escolher… abre o seletor de pasta do sistema; a pasta é validada antes de salvar. "Usar detecção automática" volta a procurar.
  Cada linha é um grupo com nome "Pasta de instalação de {jogo}: {caminho}". Nenhum jogo instalado não bloqueia o app:
  só deixa o mapa indisponível (módulo Mapa). O canvas permite testar Escolher…/Usar detecção (ATS começa "não encontrada").
- A página pode rolar na vertical (mín. 960×600).
- Esta página substitui o módulo atual na área principal; voltar = clicar em outro módulo, Ctrl+1…5 ou Esc.
- Ordem de Tab: navegação → Configurações → Perfil → Save → Revisar → grupo Tema → grupo Idioma → por jogo: Escolher… → Usar detecção automática → Abrir pasta.
- Estados: não há carregando/vazio/erro de dados; só erro de gravação (acima). Funciona sem sessão.

## 7. Atalhos

| Atalho | Ação | Quando |
| --- | --- | --- |
| Ctrl+R | Abrir Revisar mudanças (página do shell) | sessão com pendências; não grava nada. Ctrl+S não é usado |
| Ctrl+Enter | Abre a confirmação de gravação (nunca grava direto) | só na página Revisar |
| Ctrl+O | Trocar/abrir save | sempre (sem sessão foca a lista de perfis; com sessão abre o flyout) |
| Ctrl+, | Configurações | sempre |
| Ctrl+1 … Ctrl+5 | Fretes, Frota, Perfil, Empresa, Mapa | só com sessão |
| Esc | Cancelar carregamento; fechar flyout/Revisar/Configurações | contexto |

Mostrados no ToolTip dos controles e no rodapé (Ctrl+R sempre; Ctrl+Enter só na página Revisar). Nenhum atalho conflita com campos de texto
(Ctrl+Enter só vale na tela Revisar, que não tem campo de várias linhas).

## 8. Nomes acessíveis (`AutomationProperties.Name`)

| Controle | Nome | Chave |
| --- | --- | --- |
| Navegação | Módulos | `Nav_Label` |
| Item de módulo | Fretes / Frota / Perfil / Empresa / Mapa (o rótulo visual em caixa alta é só estilo) | `Module_*` |
| Configurações | Configurações | `Nav_Settings` |
| Barra de sessão | Sessão | `SessionBar_Label` |
| Botão Perfil | Perfil: Demo A. Trocar de save | `SessionBar_ProfileAutomation` |
| Botão Save | Save: Save 4. Trocar de save | `SessionBar_SaveAutomation` |
| Selo do jogo | Euro Truck Simulator 2 | `Game_Ets2` |
| Selo Revisar | Revisar mudanças: 3 mudanças pendentes (ou "nenhuma pendente", desabilitado) | `SessionBar_ReviewAutomation*` |
| Listas Perfis/Saves | Perfis / Saves de Demo A (`listbox`; item selecionado `aria-selected`) | `Welcome_Profiles`, `Welcome_SavesOf` |
| Progresso | Lendo save, 62% | `Loading_Automation` |
| Erro | região `alert`; título com foco | `OpenError_*_Title` |
| Copiar detalhes | Copiar detalhes / Copiado (`aria-live=polite`) | `OpenError_CopyDetails`, `OpenError_Copied` |
| Abrir pasta do save | Abrir pasta do save | `OpenError_OpenSaveFolder` |
| Detalhes técnicos | Detalhes técnicos, `aria-expanded` | `OpenError_Details` |
| Tema / Idioma | grupos `radiogroup` com rótulo; opções `radio` | `Settings_Theme`, `Settings_Language` |
| Pasta do jogo | grupo "Pasta de instalação de Euro Truck Simulator 2: {caminho}" | `Settings_GameFolder_Automation` |

Contraste: texto secundário (`TextMuted`) ≥ 4,5:1 nos dois temas; âmbar só em seleção, ação principal e progresso;
vermelho/verde nunca sozinhos (sempre com texto: "Jogo aberto/fechado", "Preferência salva").

## 9. Light/Dark e pt-BR/en

Todos os artboards funcionam nos dois temas e idiomas (use a barra de prévia ou a página Configurações). Em inglês os
rótulos da nav (JOBS, FLEET, PROFILE, COMPANY, MAP, SETTINGS) cabem nos 60 px; "Português (Brasil)" é o texto mais
longo dos controles segmentados e define a largura do grupo de idioma.
