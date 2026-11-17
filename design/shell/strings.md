# Textos do shell (pt-BR / en)

Gerado a partir de uma única lista; vira `Strings.resx` (en) e `Strings.pt-BR.resx`. Regras: skill `ux-writing`.
Chaves `Canvas_*` existem só nos mockups. Nomes vindos do save (perfil, save, cidades) não são traduzidos.
Números/datas pela cultura (`{0:N1}`, `{1:N0}`); `Module_*` é exibido em caixa alta por conversor, não pelo texto.

## Navegação e shell

| Chave | pt-BR | en | Nota |
| --- | --- | --- | --- |
| `Nav_Label` | Módulos | Modules | Nome acessível da navegação lateral |
| `Module_jobs` | Fretes | Jobs | Título do módulo e rótulo curto da navegação (caixa alta por conversor) |
| `Module_fleet` | Frota | Fleet | Caminhões e reboques |
| `Module_profile` | Perfil | Profile | Dinheiro, XP, skills |
| `Module_company` | Empresa | Company | Garagens, motoristas |
| `Module_map` | Mapa | Map | Descobertas |
| `Nav_Settings` | Configurações | Settings | Título da página e nome acessível do botão |
| `Nav_Settings_Short` | Config. | Settings | Rótulo curto (cabe em 60 px, caixa alta) |
| `Nav_DisabledHint` | Abra um save para usar este módulo | Open a save to use this module | ToolTip dos módulos desabilitados |
| `Shell_SafetyNote` | Nada é gravado sem revisão. Um backup é criado antes. | Nothing is saved without review. A backup is created first. | Rodapé do shell, sempre visível |
| `Shell_HintReview` | Revisar | Review | Dica de atalho no rodapé (ao lado de Ctrl+R) |
| `Shell_HintApply` | Aplicar | Apply | Dica de atalho no rodapé (ao lado de Ctrl+Enter); só aparece na página Revisar |

## Barra de sessão

| Chave | pt-BR | en | Nota |
| --- | --- | --- | --- |
| `SessionBar_Label` | Sessão | Session | Nome acessível da barra superior |
| `SessionBar_NoSession` | Nenhum save aberto | No save open |  |
| `SessionBar_Profile` | Perfil | Profile | Legenda pequena do botão de perfil |
| `SessionBar_Save` | Save | Save | Legenda pequena do botão de save |
| `SessionBar_ChangeHint` | Trocar de save… | Change save… | ToolTip dos botões de perfil e save |
| `SessionBar_ProfileAutomation` | Perfil: {0}. Trocar de save | Profile: {0}. Change save | {0} = nome do perfil |
| `SessionBar_SaveAutomation` | Save: {0}. Trocar de save | Save: {0}. Change save | {0} = nome do save |
| `Game_Ets2` | Euro Truck Simulator 2 | Euro Truck Simulator 2 | Nome acessível/ToolTip do selo ETS2 |
| `Game_Ets2_Short` | ETS2 | ETS2 | Selo |
| `Game_Ats` | American Truck Simulator | American Truck Simulator | Nome acessível/ToolTip do selo ATS |
| `Game_Ats_Short` | ATS | ATS | Selo |
| `SessionBar_GameClosed` | Jogo fechado | Game closed | Estado do processo do jogo |
| `SessionBar_GameRunning` | Jogo aberto | Game running | Estado do processo do jogo |
| `SessionBar_GameRunningHint` | Feche o jogo antes de aplicar. | Close the game before applying. | ToolTip do estado Jogo aberto |
| `SessionBar_Review` | Revisar ({0}) | Review ({0}) | Selo/botão; {0} = nº de mudanças pendentes |
| `SessionBar_ReviewNone` | Sem mudanças | No changes | Selo desabilitado (0 pendências) |
| `SessionBar_ReviewHint` | Revisar mudanças (Ctrl+R) | Review changes (Ctrl+R) | ToolTip do selo |
| `SessionBar_ReviewAutomation` | Revisar mudanças: {0} | Review changes: {0} | {0} = Shell_Pending_One/Other |
| `SessionBar_ReviewAutomationNone` | Revisar mudanças: nenhuma pendente | Review changes: none pending | Selo desabilitado |
| `Shell_Pending_One` | 1 mudança pendente | 1 pending change | Plural singular |
| `Shell_Pending_Other` | {0} mudanças pendentes | {0} pending changes | Plural |

## Sem sessão (abrir save)

| Chave | pt-BR | en | Nota |
| --- | --- | --- | --- |
| `Welcome_Title` | Abra um save | Open a save | Título (caixa alta por conversor) |
| `Welcome_Subtitle` | Escolha o perfil e o save que você quer editar. Nada é alterado até você aplicar. | Choose the profile and save you want to edit. Nothing changes until you apply. |  |
| `Welcome_Continue` | Continuar de onde parou | Pick up where you left off | Legenda do cartão do último save |
| `Welcome_LastSave` | {0} · {1} · {2} | {0} · {1} · {2} | {0} perfil, {1} save, {2} data (cultura atual) |
| `Welcome_ReopenLast` | Abrir de novo | Open again | Botão do cartão do último save |
| `Welcome_Profiles` | Perfis | Profiles | Título da lista e nome acessível |
| `Welcome_ProfileSaveCount` | {0} saves | {0} saves | Contagem por perfil |
| `Welcome_SavesOf` | Saves de {0} | Saves of {0} | {0} = nome do perfil |
| `Welcome_Open` | Abrir save | Open save | Ação principal; desabilitada sem save escolhido |
| `Welcome_Browse` | Escolher pasta… | Choose folder… | Procurar perfis em outra pasta |
| `Welcome_PickHint` | Escolha um save para continuar | Choose a save to continue | Dica ao lado do botão, sem seleção |
| `Welcome_PickedHint` | {0} · {1} | {0} · {1} | {0} perfil, {1} save escolhido |
| `Welcome_NoProfilesTitle` | Nenhum perfil encontrado | No profiles found | Estado vazio |
| `Welcome_NoProfilesBody` | Procuramos em Documentos, nas pastas do Euro Truck Simulator 2 e do American Truck Simulator. Se o seu jogo está em outro lugar, escolha a pasta manualmente. | We looked in Documents, in the Euro Truck Simulator 2 and American Truck Simulator folders. If your game is somewhere else, choose the folder manually. |  |
| `Welcome_Searching` | Procurando perfis… | Looking for profiles… | Estado carregando da lista |
| `Welcome_NoSavesTitle` | Nenhum save neste perfil | No saves in this profile | Lista de saves vazia |
| `Welcome_NoSavesBody` | Abra o jogo, salve uma vez e volte aqui. | Open the game, save once and come back here. |  |
| `SaveKind_Auto` | Automático | Autosave | Selo do tipo de save |
| `SaveKind_Quick` | Rápido | Quicksave | Selo do tipo de save |
| `SaveKind_Manual` | Manual | Manual | Selo do tipo de save |

## Carregando save

| Chave | pt-BR | en | Nota |
| --- | --- | --- | --- |
| `Loading_Title` | Lendo save… | Reading save… |  |
| `Loading_Stage_Read` | Lendo o arquivo | Reading the file | LoadProgress.StageKey |
| `Loading_Stage_Parse` | Interpretando o save | Interpreting the save | LoadProgress.StageKey |
| `Loading_Stage_Index` | Organizando fretes e empresas | Organizing jobs and companies | LoadProgress.StageKey |
| `Loading_Hint` | Saves grandes podem levar alguns segundos. | Large saves can take a few seconds. |  |
| `Loading_Cancel` | Cancelar | Cancel | Volta para Abra um save |
| `Loading_Automation` | Lendo save, {0}% | Reading save, {0}% | Nome acessível da barra de progresso |
| `Loading_Size` | {0:N1} MB | {0:N1} MB | Tamanho do arquivo, pela cultura |

## Erro ao abrir save

| Chave | pt-BR | en | Nota |
| --- | --- | --- | --- |
| `OpenError_Parse_Title` | Não foi possível entender o save | Couldn't understand the save | CoreError.SaveFormatUnsupported: o arquivo foi lido, mas o conteúdo não foi entendido |
| `OpenError_Parse_Body` | O arquivo foi lido, mas o Own Expeditor não entendeu o que está dentro dele. Seu save não foi alterado. | The file was read, but Own Expeditor couldn't make sense of what's inside. Your save was not changed. |  |
| `OpenError_Parse_CausesTitle` | O que pode ter causado isso | What may have caused this |  |
| `OpenError_Parse_Cause1` | Uma versão do jogo mais nova do que a que o app conhece. | A game version newer than the one the app knows. |  |
| `OpenError_Parse_Cause2` | Um mod que muda o jeito como o save é gravado. | A mod that changes how the save is written. |  |
| `OpenError_Parse_Cause3` | Um arquivo danificado. | A damaged file. |  |
| `OpenError_Parse_Tip1` | Tente outro save do mesmo perfil, como o salvamento automático mais recente. | Try another save from the same profile, such as the most recent autosave. |  |
| `OpenError_Parse_Tip2` | Se continuar acontecendo, copie os detalhes técnicos e envie ao relatar o problema. | If it keeps happening, copy the technical details and send them when you report the problem. |  |
| `OpenError_Parse_DetailsText` | Código: {0}
Jogo: {1}
Perfil: {2}
Save: {3}
Tamanho: {4}
Etapa: {5}
App: {6} | Code: {0}
Game: {1}
Profile: {2}
Save: {3}
Size: {4}
Step: {5}
App: {6} | Texto copiado por Copiar detalhes; {0} código, {1} jogo, {2} perfil, {3} save, {4} tamanho, {5} etapa em que falhou, {6} versão do app. Nunca inclui conteúdo do save |
| `OpenError_CopyDetails` | Copiar detalhes | Copy details | Copia os detalhes técnicos mesmo com o disclosure recolhido |
| `OpenError_Copied` | Copiado | Copied | Mesmo botão, depois de copiar (~2 s) |
| `OpenError_OpenSaveFolder` | Abrir pasta do save | Open save folder | Exige IShellLauncher (ver README, perguntas) |
| `OpenError_ChooseAnother` | Escolher outro save | Choose another save | Volta para Abra um save |
| `OpenError_Retry` | Tentar de novo | Try again | Relê o mesmo save |
| `OpenError_Corrupt_Title` | Não foi possível ler este save | Couldn't read this save | CoreError.SaveCorrupt: o arquivo não pôde ser lido (diferente de Não foi possível entender, em que o arquivo foi lido) |
| `OpenError_Corrupt_Body` | O arquivo não pôde ser lido por inteiro: parece incompleto ou ilegível, como se o jogo tivesse fechado durante o salvamento. Seu save não foi alterado. | The file couldn't be read in full: it looks incomplete or unreadable, as if the game closed while saving. Your save was not changed. |  |
| `OpenError_Corrupt_Tip1` | Tente o salvamento automático mais recente do mesmo perfil. | Try the most recent autosave of the same profile. |  |
| `OpenError_Corrupt_Tip2` | Ou abra o jogo, carregue o perfil e salve de novo. | Or open the game, load the profile and save again. |  |
| `OpenError_Details` | Detalhes técnicos | Technical details | Disclosure recolhido por padrão |
| `OpenError_DetailsText` | Código: {0} · perto da linha {1:N0} | Code: {0} · near line {1:N0} | {0} = nome do CoreError; {1} = linha |
| `OpenError_DetailsCode` | Código: {0} | Code: {0} | Quando não há linha (não encontrado, sem permissão) |
| `OpenError_NotFound_Title` | Save não encontrado | Save not found | CoreError.SaveNotFound (mesmo layout do corrompido) |
| `OpenError_NotFound_Body` | O arquivo não existe mais. Ele pode ter sido movido ou apagado. | The file no longer exists. It may have been moved or deleted. |  |
| `OpenError_AccessDenied_Title` | Sem permissão para ler o save | No permission to read the save | CoreError.AccessDenied (mesmo layout do corrompido) |
| `OpenError_AccessDenied_Body` | Verifique se outro programa está usando o arquivo ou se a pasta está protegida e tente de novo. | Check whether another program is using the file or the folder is protected, then try again. |  |
| `OpenError_Unknown_Title` | Não foi possível abrir o save | Couldn't open the save | Qualquer outro CoreError |
| `OpenError_Unknown_Body` | Algo inesperado aconteceu. Seu save não foi alterado. Tente de novo ou escolha outro save. | Something unexpected happened. Your save was not changed. Try again or choose another save. |  |
| `Error_SaveFormatUnsupported` | Não foi possível entender este save: o formato não foi reconhecido. Tente outro save do mesmo perfil ou, se continuar, relate o problema. | Couldn't understand this save: the format wasn't recognized. Try another save from the same profile or, if it keeps happening, report the problem. | Mensagem curta por ILocalizer.FormatError (toast/diálogo); mesma da tela Não foi possível entender o save |
| `Error_SaveCorrupt` | Não foi possível ler este save: o arquivo parece incompleto ou ilegível. | Couldn't read this save: the file looks incomplete or unreadable. | Idem |
| `Error_SaveNotFound` | Não foi possível abrir o save: o arquivo não existe mais. | Couldn't open the save: the file no longer exists. | Idem |
| `Error_AccessDenied` | Não foi possível acessar o arquivo. Verifique as permissões da pasta e tente de novo. | Couldn't access the file. Check the folder permissions and try again. | Compartilhada com UX-2 (gravação) |

## Trocar de save e sair com pendências

| Chave | pt-BR | en | Nota |
| --- | --- | --- | --- |
| `Shell_SwitchSave_Title` | Trocar de save? | Switch save? | Diálogo destrutivo (UX-2 define o visual) |
| `Shell_SwitchSave_Message` | Trocar de save vai descartar {0}. | Switching saves will discard {0}. | {0} = Shell_Pending_One/Other |
| `Shell_SwitchSave_Confirm` | Descartar e trocar | Discard and switch |  |
| `Shell_Exit_Title` | Sair sem aplicar? | Exit without applying? |  |
| `Shell_Exit_Message` | Sair vai descartar {0}. | Exiting will discard {0}. |  |
| `Shell_Exit_Confirm` | Descartar e sair | Discard and exit |  |
| `Shell_KeepEditing` | Continuar editando | Keep editing | Botão seguro (padrão) dos dois diálogos |

## Configurações

| Chave | pt-BR | en | Nota |
| --- | --- | --- | --- |
| `Settings_Title` | Configurações | Settings | Título da página (caixa alta por conversor) |
| `Settings_Appearance` | Aparência | Appearance |  |
| `Settings_Theme` | Tema | Theme |  |
| `Settings_Theme_Help` | Sistema segue o tema do seu computador. | System follows your computer's theme. |  |
| `Settings_Theme_Light` | Claro | Light |  |
| `Settings_Theme_Dark` | Escuro | Dark |  |
| `Settings_Theme_System` | Sistema | System |  |
| `Settings_Language` | Idioma | Language |  |
| `Settings_Language_Help` | Muda na hora, sem reiniciar. | Changes right away, no restart. |  |
| `Settings_Language_PtBr` | Português (Brasil) | Português (Brasil) | Endônimo: igual nos dois idiomas |
| `Settings_Language_En` | English | English | Endônimo: igual nos dois idiomas |
| `Settings_Games` | Jogos | Games | Título da seção |
| `Settings_GameFolder` | Pasta de instalação do jogo | Game installation folder | Rótulo da seção; uma linha por jogo (ETS2, ATS) |
| `Settings_GameFolder_Help` | Usada para ler os dados do jogo (como o mapa) direto dos arquivos instalados. | Used to read game data (such as the map) straight from the installed files. |  |
| `Settings_GameFolder_Auto` | Detectada automaticamente | Detected automatically | Selo de status |
| `Settings_GameFolder_Manual` | Escolhida por você | Chosen by you | Selo de status |
| `Settings_GameFolder_NotFound` | Não encontrada | Not found | Selo de status |
| `Settings_GameFolder_NotFoundHelp` | Não encontramos o {0}. Escolha a pasta de instalação. Sem ela, o mapa não fica disponível. | We couldn't find {0}. Choose the installation folder. Without it, the map isn't available. | {0} = nome do jogo |
| `Settings_GameFolder_Invalid` | Esta pasta não parece ser a instalação de {0}. Escolha a pasta onde o jogo está instalado. | This folder doesn't look like the installation of {0}. Choose the folder where the game is installed. | Erro inline ao validar a pasta escolhida; a escolha anterior é mantida |
| `Settings_GameFolder_Choose` | Escolher… | Choose… | Abre o seletor de pasta |
| `Settings_GameFolder_Reset` | Usar detecção automática | Use automatic detection | Só quando a pasta foi escolhida manualmente |
| `Settings_GameFolder_Open` | Abrir pasta | Open folder | Só quando há pasta |
| `Settings_GameFolder_Automation` | Pasta de instalação de {0}: {1} | Installation folder of {0}: {1} | {0} = jogo, {1} = caminho ou 'não encontrada' |
| `Settings_Saved` | Preferência salva | Preference saved | Região viva (status), aparece depois de cada mudança |
| `Settings_SaveFailed` | Não foi possível salvar a preferência. Ela vale só até você fechar o app. | Couldn't save the preference. It applies only until you close the app. | Erro inline (CoreError WriteFailed/AccessDenied em settings.json); tema/idioma continuam trocados na sessão |
| `Settings_Note` | Suas mudanças pendentes não são afetadas. | Your pending changes are not affected. | Mostrado quando há sessão com pendências |
| `Settings_About` | Sobre | About |  |
| `Settings_Version` | Versão {0} | Version {0} |  |
| `Settings_Disclaimer` | Projeto da comunidade, sem ligação com a SCS Software. | Community project, not affiliated with SCS Software. |  |

## Somente nos mockups (não vão para os .resx)

| Chave | pt-BR | en | Nota |
| --- | --- | --- | --- |
| `Canvas_ReviewPage` | Página Revisar: o conteúdo é desenhado em design/review-apply | Review page: content is designed in design/review-apply |  |
| `Canvas_Preview` | Prévia do canvas (fora do app) | Canvas preview (not part of the app) |  |
| `Canvas_ToLight` | Tema claro | Light theme |  |
| `Canvas_ToDark` | Tema escuro | Dark theme |  |
| `Canvas_ToEn` | English | English |  |
| `Canvas_ToPt` | Português | Português |  |
| `Canvas_ModuleArea` | Área do módulo: o conteúdo é desenhado em design/fretes | Module area: content is designed in design/fretes |  |
