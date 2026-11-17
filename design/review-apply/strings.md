# Revisar e aplicar — textos

Formato: `Chave | pt-BR | en`. Vira `Strings.resx` (en, neutro) e
`Strings.pt-BR.resx`. Convenções da skill `ux-writing`: sem ponto final em
botões/rótulos/títulos, `…` (um caractere), nomes de cidade/empresa/carga vêm do
save e não são traduzidos. Plural: pares `_One` / `_Other` (o `.resx` não tem
plural; a App escolhe pela contagem). Números e dinheiro pela cultura.

Chaves **de outro dono** aparecem marcadas; estão aqui só porque o mockup as usa.

## Contagens e formatos (compartilhados)
| Chave | pt-BR | en |
| --- | --- | --- |
| Count_Changes_One | 1 mudança | 1 change |
| Count_Changes_Other | {0} mudanças | {0} changes |
| Count_Modules_One | 1 módulo | 1 module |
| Count_Modules_Other | {0} módulos | {0} modules |
| Format_Km | {0:N0} km | {0:N0} km |
| Format_Money_Ets2 | {0:N0} € | €{0:N0} |
| Format_Money_Ats | US$ {0:N0} | ${0:N0} |
| Diff_Empty | vazio | empty |
| Common_Cancel | Cancelar | Cancel |
| Common_Close | Fechar | Close |

`Diff_Empty` é o nome acessível de um valor vazio; o texto visível é "—".

## Tela Revisar (`Review_*`)
| Chave | pt-BR | en |
| --- | --- | --- |
| Review_Title | Revisar mudanças | Review changes |
| Review_Subtitle | {0} em {1} | {0} in {1} |
| Review_Subtitle_None | Nenhuma mudança pendente | No pending changes |
| Review_List_Label | Mudanças pendentes | Pending changes |
| Review_Col_Field | Campo | Field |
| Review_Col_Before | Antes | Before |
| Review_Col_After | Depois | After |
| Review_Line_Aria | {0}: antes {1}, depois {2} | {0}: before {1}, after {2} |
| Review_Discard | Descartar | Discard |
| Review_DiscardItem_Aria | Descartar mudança: {0} | Discard change: {0} |
| Review_DiscardModule | Descartar módulo | Discard module |
| Review_DiscardModule_Aria | Descartar todas as mudanças de {0} | Discard all changes in {0} |
| Review_DiscardAll | Descartar tudo | Discard all |
| Review_Summary | Resumo | Summary |
| Review_Stat_Changes | Mudanças | Changes |
| Review_Stat_Modules | Módulos | Modules |
| Review_Safety_Title | Antes de gravar | Before writing |
| Review_Safety_Backup | Um backup do save é criado ao lado dele. | A backup of the save is created next to it. |
| Review_Safety_Once | Todos os módulos são gravados de uma vez. | All modules are written at once. |
| Review_Safety_CloseGame | Feche o jogo antes de aplicar. | Close the game before applying. |
| Review_Back | Continuar editando | Keep editing |
| Review_Apply | Aplicar mudanças | Apply changes |
| Review_Apply_Hint | Nada é gravado sem sua confirmação. | Nothing is written without your confirmation. |
| Review_Apply_Disabled_Hint | Aplicar fica disponível quando houver mudanças. | Apply becomes available when there are changes. |
| Toast_Discarded | Mudança descartada. | Change discarded. |
| Toast_DiscardedMany | Mudanças descartadas. | Changes discarded. |
| Toast_Undo | Desfazer | Undo |
| Discard_All_Title | Descartar todas as mudanças? | Discard all changes? |
| Discard_All_Message | Todas as mudanças pendentes serão perdidas. O save não foi alterado. | All pending changes will be lost. Your save has not been changed. |
| Discard_All_Confirm | Descartar tudo | Discard all |

`Review_Subtitle` recebe duas frases de contagem (`Count_Changes_*`,
`Count_Modules_*`): "3 mudanças em 2 módulos" / "3 changes in 2 modules".

### Chaves de outros donos usadas no desenho (exemplos)
| Chave | pt-BR | en | Dono |
| --- | --- | --- | --- |
| Shell_Review_Badge | Revisar ({0}) | Review ({0}) | UX-1 (shell) |
| Shell_Review_Badge_Aria | Revisar mudanças pendentes: {0} | Review pending changes: {0} | UX-1 |
| Module_Jobs_Title | Fretes | Jobs | UX-1 |
| Module_Profile_Title | Perfil | Profile | UX-1 |
| Jobs_Change_Title | Frete {0} → {1} | Job {0} → {1} | módulo Fretes (`JobsTextKeys`) |
| Jobs_Field_Destination | Destino | Destination | Fretes |
| Jobs_Field_Cargo | Carga | Cargo | Fretes |
| Jobs_Field_Distance | Distância | Distance | Fretes |
| Jobs_Field_Trailer | Reboque | Trailer | Fretes |
| Profile_Change_Money_Title | Dinheiro do perfil | Profile money | módulo Perfil (futuro, só exemplo) |
| Profile_Field_Money | Dinheiro | Money | Perfil (futuro) |

## Confirmação (`Confirm_*`)
| Chave | pt-BR | en |
| --- | --- | --- |
| Confirm_Title | Gravar mudanças no save? | Write changes to the save? |
| Confirm_Intro_One | 1 mudança será gravada em {0}. | 1 change will be written to {0}. |
| Confirm_Intro_Other | {0} mudanças serão gravadas em {1}. | {0} changes will be written to {1}. |
| Confirm_What_Title | O que será gravado | What will be written |
| Confirm_Backup_Title | Onde fica o backup | Where the backup goes |
| Confirm_Backup_Note | A cópia do save original é criada nesta pasta antes de gravar. O nome usa a data e a hora da gravação. | A copy of the original save is created in this folder before writing. The name uses the date and time of the write. |
| Confirm_Backup_Pattern | game.sii.bak-AAAAMMDD-HHMMSS | game.sii.bak-YYYYMMDD-HHMMSS |
| Confirm_Warn_Title | Feche o jogo antes de continuar | Close the game before continuing |
| Confirm_Warn_Body | Com o jogo aberto, ele pode sobrescrever o save ao salvar ou sair, e a gravação pode falhar. | With the game running, it may overwrite the save when it saves or exits, and writing may fail. |
| Confirm_Warn_Title_Running | O jogo está aberto | The game is running |
| Confirm_Warn_Body_Running | Feche o jogo antes de gravar. Ele pode sobrescrever o save ao salvar ou sair. | Close the game before writing. It may overwrite the save when it saves or exits. |
| Confirm_Undo_Note | Para desfazer depois, basta restaurar o backup. Mostramos como na próxima tela. | To undo later, just restore the backup. We show how on the next screen. |
| Confirm_Confirm | Gravar no save | Write to save |
| Confirm_Applying | Gravando… | Writing… |
| Confirm_Applying_Hint | Criando o backup e gravando o save. Não feche o app. | Creating the backup and writing the save. Don't close the app. |

`Confirm_Warn_*_Running` só quando `IGameProcessMonitor.IsRunning` for verdadeiro
ao abrir o diálogo (a checagem que vale continua sendo a do `ApplyAsync`).

## Resultado (`Result_*`)
| Chave | pt-BR | en |
| --- | --- | --- |
| Result_Title | Mudanças gravadas | Changes written |
| Result_Written_One | 1 mudança gravada no save. | 1 change written to the save. |
| Result_Written_Other | {0} mudanças gravadas no save. | {0} changes written to the save. |
| Result_Backup_Title | Backup do save original | Backup of the original save |
| Result_Backup_Help | Guardamos uma cópia do save como ele estava antes. Ela fica na mesma pasta. | We kept a copy of the save as it was before. It sits in the same folder. |
| Result_OpenFolder | Abrir pasta | Open folder |
| Result_OpenFolder_Failed | Não foi possível abrir a pasta. Ela está em: {0} | Couldn't open the folder. It is at: {0} |
| Result_CopyPath | Copiar caminho | Copy path |
| Result_PathCopied | Caminho copiado | Path copied |
| Result_Now_Title | Agora no jogo | Now in the game |
| Result_Now_Body | Abra o jogo e carregue este save para ver as mudanças. | Open the game and load this save to see the changes. |
| Result_TextNote | O Own Expeditor grava o save em texto simples. Se o jogo não carregar este save, restaure o backup. | Own Expeditor writes the save as plain text. If the game doesn't load this save, restore the backup. |
| Result_Restore_Title | Como restaurar o backup | How to restore the backup |
| Result_Restore_Step1 | Feche o jogo. | Close the game. |
| Result_Restore_Step2 | Na pasta do save, renomeie game.sii para game.sii.old. | In the save folder, rename game.sii to game.sii.old. |
| Result_Restore_Step3 | Renomeie o backup (game.sii.bak-…) para game.sii. | Rename the backup (game.sii.bak-…) to game.sii. |
| Result_Keep | Continuar editando | Keep editing |

## Vazio (`Empty_*`)
| Chave | pt-BR | en |
| --- | --- | --- |
| Empty_Title | Nada para revisar | Nothing to review |
| Empty_Message | Você ainda não fez mudanças neste save. Escolha um módulo, edite o que quiser e volte aqui para revisar antes de gravar. | You haven't made any changes to this save yet. Pick a module, edit what you like and come back here to review before writing. |
| Empty_Go | Ir para {0} | Go to {0} |
| Empty_Tip | Dica: Ctrl+R abre esta tela a qualquer momento. | Tip: Ctrl+R opens this screen any time. |
| Review_AllDiscarded | Você descartou todas as mudanças. O save não foi alterado. | You discarded every change. Your save was not changed. |

`Empty_Go` recebe o título do primeiro módulo habilitado (`Module_*_Title`).

## Erros — chrome (`Error_*` de apoio e `Action_*`)
| Chave | pt-BR | en |
| --- | --- | --- |
| Error_Title_Open | Não foi possível abrir o save | Couldn't open the save |
| Error_Title_Apply | Não foi possível gravar | Couldn't write the save |
| Error_Title_Edit | Não foi possível adicionar a mudança | Couldn't add the change |
| Error_NothingWritten | Nada foi gravado. Seu save continua como estava. | Nothing was written. Your save is as it was. |
| Error_PendingKept | Suas mudanças pendentes continuam aqui. | Your pending changes are still here. |
| Error_PendingLost | As mudanças pendentes serão descartadas. | Pending changes will be discarded. |
| Error_Details | Detalhes | Details |
| Action_Retry | Tentar de novo | Try again |
| Action_RetryGameClosed | Já fechei, tentar de novo | I closed it, try again |
| Action_BackToReview | Voltar à revisão | Back to review |
| Action_OpenReview | Abrir revisão | Open review |
| Action_ReopenSave | Abrir o save de novo | Open the save again |
| Action_ChooseSave | Escolher outro save | Choose another save |
| Action_OpenFolder | Abrir pasta | Open folder |

`Error_Details` rotula o código (ex.: `GameRunning`) e o detalhe técnico
(`CoreException.Message`) em fonte mono, para relatar problemas. Não é traduzido.

## Erros — uma mensagem por `CoreError` (`Error_<Code>`)
Cada mensagem diz **o que aconteceu + o que fazer**. `ILocalizer.FormatError(code)`
lê `Error_<Code>`. O teste de paridade garante as 18 chaves nos dois `.resx`.

| Chave | pt-BR | en |
| --- | --- | --- |
| Error_SaveNotFound | Não encontramos o arquivo do save. Ele pode ter sido movido, renomeado ou apagado. Volte à lista de saves e escolha outro. | We couldn't find the save file. It may have been moved, renamed or deleted. Go back to the save list and pick another one. |
| Error_SaveFormatUnsupported | Não foi possível entender este save: o formato não foi reconhecido. Tente outro save do mesmo perfil ou, se continuar, relate o problema. | Couldn't understand this save: the format wasn't recognized. Try another save from the same profile or, if it keeps happening, report the problem. |
| Error_SaveCorrupt | O arquivo do save parece danificado e não foi possível lê-lo. Escolha outro save ou restaure um backup (game.sii.bak-…). | The save file looks damaged and couldn't be read. Pick another save or restore a backup (game.sii.bak-…). |
| Error_NoSession | Nenhum save está aberto. Escolha um perfil e um save para começar. | No save is open. Choose a profile and a save to get started. |
| Error_SessionBusy | O save ainda está sendo lido ou gravado. Espere alguns segundos e tente de novo. | The save is still being read or written. Wait a few seconds and try again. |
| Error_ReadOnlySession | Este save está aberto só para visualização, então não dá para gravar. Abra o save de novo pelo seletor de saves. | This save is open for viewing only, so it can't be written. Open the save again from the save picker. |
| Error_Conflict | Esta mudança altera algo que outra mudança pendente já alterou. Abra a revisão, descarte uma das duas e refaça a edição. | This change edits something another pending change already edited. Open the review, discard one of the two and redo the edit. |
| Error_StalePatch | Os dados desta mudança ficaram desatualizados. Volte à revisão, descarte a mudança e refaça a edição. Se continuar, abra o save de novo. | The data behind this change is out of date. Go back to the review, discard the change and redo the edit. If it keeps happening, open the save again. |
| Error_UnknownUnit | Não encontramos no save o item que você quis alterar. Ele pode ter sido removido. Volte à revisão, descarte a mudança e escolha o item de novo. | We couldn't find the item you wanted to change in the save. It may have been removed. Go back to the review, discard the change and pick the item again. |
| Error_UnknownAttribute | Este save não tem o campo que o app tentou alterar. Isso costuma acontecer com saves de mods ou de outra versão do jogo. Descarte a mudança, tente outro item ou atualize o app. | This save doesn't have the field the app tried to change. This usually happens with saves from mods or another game version. Discard the change, try another item or update the app. |
| Error_NothingToApply | Não há mudanças para gravar. Faça uma edição em um módulo e volte para revisar. | There are no changes to write. Make an edit in a module and come back to review. |
| Error_GameRunning | O jogo está aberto, e ele sobrescreveria o save ao salvar ou sair. Feche o jogo e tente de novo. | The game is running, and it would overwrite the save when it saves or exits. Close the game and try again. |
| Error_SaveChangedOnDisk | O arquivo do save mudou depois que você o abriu (por exemplo, o jogo salvou). Para não perder progresso, o app não gravou. Abra o save de novo e refaça as mudanças. | The save file changed after you opened it (for example, the game saved). To avoid losing progress, the app didn't write. Open the save again and redo your changes. |
| Error_BackupExists | Já existe um backup com este mesmo horário, e o app não sobrescreve backups. Espere um segundo e tente de novo. | A backup with this same time already exists, and the app never overwrites backups. Wait a second and try again. |
| Error_BackupFailed | Não foi possível criar o backup, então o app não gravou. Verifique o espaço livre e as permissões da pasta do save e tente de novo. | The backup couldn't be created, so the app didn't write. Check the free space and permissions of the save folder and try again. |
| Error_WriteFailed | Não foi possível gravar o arquivo. Veja se outro programa (como antivírus ou backup na nuvem) está usando a pasta do save e tente de novo. | The file couldn't be written. Check whether another program (such as antivirus or cloud backup) is using the save folder and try again. |
| Error_VerifyFailed | O arquivo gravado não passou na verificação, então o save original foi mantido. Tente de novo; se repetir, abra o save de novo e refaça as mudanças. | The written file didn't pass verification, so the original save was kept. Try again; if it repeats, open the save again and redo your changes. |
| Error_AccessDenied | O sistema negou o acesso à pasta do save. Veja se a pasta não é somente leitura nem está protegida (por exemplo, por antivírus ou nuvem) e tente de novo. | The system denied access to the save folder. Check that the folder isn't read-only or protected (for example by antivirus or cloud sync) and try again. |

Observações:
- Nos textos do `.resx` use o apóstrofo reto `'` do inglês como no resto do app; o canvas usa `’` só por tipografia.
- `Error_SaveFormatUnsupported` agora significa "formato de save não reconhecido" (o app decodifica saves em texto e binários; não há mais instrução de `g_save_format`). A mesma mensagem aparece na tela "Não foi possível entender o save" de `design/shell/`.
- Nenhum termo do formato SII (unidade, atributo, patch, bloco) aparece para o usuário.
