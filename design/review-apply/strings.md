# Revisar e aplicar — textos

Formato: `Chave | pt-BR | en`. Vira `Strings.resx` (en, neutro) e
`Strings.pt-BR.resx`. Convenções da skill `ux-writing`: sem ponto final em
botões/rótulos/títulos, `…` (um caractere), nomes de cidade/empresa/carga vêm do
save e não são traduzidos pelo app (vêm da localização do jogo, ADR 0006). Plural: grupos `_Zero` (opcional) / `_One` / `_Other` (o `.resx` não tem
plural; `ILocalizer.GetPlural(base, n)` escolhe pela contagem; falta `_Zero` → usa `_Other`). Números e dinheiro pela cultura.

Chaves **de outro dono** aparecem marcadas; estão aqui só porque o mockup as usa.

## Contagens e formatos (compartilhados)
| Chave | pt-BR | en |
| --- | --- | --- |
| Count_Changes_Zero | Nenhuma mudança | No changes |
| Count_Changes_One | 1 mudança | 1 change |
| Count_Changes_Other | {0} mudanças | {0} changes |
| Count_Modules_Zero | Nenhum módulo | No modules |
| Count_Modules_One | 1 módulo | 1 module |
| Count_Modules_Other | {0} módulos | {0} modules |
| Format_Km | {0:N0} km | {0:N0} km |
| Format_KmApprox | ≈ {0:N0} km | ≈ {0:N0} km |
| Format_Money_Ets2 | {0:N0} € | €{0:N0} |
| Format_Money_Ats | US$ {0:N0} | ${0:N0} |
| Diff_Empty | vazio | empty |
| Common_Cancel | Cancelar | Cancel |
| Common_Close | Fechar | Close |

`Format_KmApprox` é a distância **estimada** pelo app (módulo Fretes): aparece na revisão com o “≈”. `Diff_Empty` é o nome acessível de um valor vazio; o texto visível é "—". Os `_Zero` são opcionais: `Review_Subtitle` pode usar "Nenhuma mudança" quando 0 (hoje a tela vazia usa `Review_Subtitle_None`).

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
O módulo Fretes ganhou desenho próprio: `Jobs_*` (inclusive `Jobs_Change_Title`, `Jobs_Field_*` e `Jobs_Error_*`) estão em [`design/fretes/strings.md`](../fretes/strings.md); aqui ficam só como referência.
| Chave | pt-BR | en | Dono |
| --- | --- | --- | --- |
| Shell_Review_Badge | Revisar ({0}) | Review ({0}) | UX-1 (shell) |
| Shell_Review_Badge_Aria | Revisar mudanças pendentes: {0} | Review pending changes: {0} | UX-1 |
| Module_Jobs_Title | Fretes | Jobs | UX-1 |
| Module_Profile_Title | Perfil | Profile | UX-1 |
| Jobs_Change_Title | Frete {0} → {1} | Job {0} → {1} | módulo Fretes (`JobsTextKeys`); definidas em [`design/fretes/strings.md`](../fretes/strings.md) |
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
| Confirm_IntroNew_One | 1 mudança será gravada em um novo save, criado a partir de {0}. | 1 change will be written to a new save, created from {0}. |
| Confirm_IntroNew_Other | {0} mudanças serão gravadas em um novo save, criado a partir de {1}. | {0} changes will be written to a new save, created from {1}. |
| Confirm_What_Title | O que será gravado | What will be written |
| Confirm_Dest_Title | Como gravar | How to write |
| Confirm_Dest_Default | Padrão | Default |
| Confirm_Dest_Overwrite | Sobrescrever este save | Overwrite this save |
| Confirm_Dest_Overwrite_Help | Grava no save aberto ({0}). Um backup é criado antes. | Writes to the open save ({0}). A backup is created first. |
| Confirm_Dest_New | Criar um novo save | Create a new save |
| Confirm_Dest_New_Help | Cria um save novo com as mudanças. O save original não é tocado. | Creates a new save with the changes. The original save isn't touched. |
| Confirm_Backup_Title | Onde fica o backup | Where the backup goes |
| Confirm_Backup_Note | A cópia do save original é criada nesta pasta antes de gravar. O nome usa a data e a hora da gravação. | A copy of the original save is created in this folder before writing. The name uses the date and time of the write. |
| Confirm_Backup_Pattern | game.sii.bak-AAAAMMDD-HHMMSS | game.sii.bak-YYYYMMDD-HHMMSS |
| Confirm_NewName_Label | Nome do novo save | New save name |
| Confirm_NewName_Default | {0} (editado) | {0} (edited) |
| Confirm_NewName_Hint | É o nome que aparece na lista de saves do jogo. | It's the name shown in the game's save list. |
| Confirm_NewName_Invalid | Digite um nome de 1 a 40 caracteres. | Enter a name of 1 to 40 characters. |
| Confirm_NewSlot_Title | Onde fica o novo save | Where the new save goes |
| Confirm_NewSlot_Next | (próximo slot livre) | (next free slot) |
| Confirm_NewSlot_Note | O app cria uma pasta nova no próximo slot livre do perfil {0}. O save original continua como está. | The app creates a new folder in the next free slot of profile {0}. The original save stays as it is. |
| Confirm_Cloud_Title | Este perfil usa Steam Cloud | This profile uses Steam Cloud |
| Confirm_Cloud_Overwrite | A nuvem pode restaurar o save antigo quando o jogo abrir. Se isso acontecer, feche o jogo e grave de novo. | The cloud may restore the old save when the game opens. If that happens, close the game and write again. |
| Confirm_Cloud_New | O jogo pode não mostrar saves criados fora dele. Se o novo save não aparecer, grave com “Sobrescrever este save”. | The game may not show saves created outside it. If the new save doesn't show up, write with “Overwrite this save”. |
| Confirm_Warn_Title | Feche o jogo antes de continuar | Close the game before continuing |
| Confirm_Warn_Body | Com o jogo aberto, ele pode sobrescrever o save ao salvar ou sair, e a gravação pode falhar. | With the game running, it may overwrite the save when it saves or exits, and writing may fail. |
| Confirm_Warn_Title_Running | O jogo está aberto | The game is running |
| Confirm_Warn_Body_Running | Feche o jogo antes de gravar. Ele pode sobrescrever o save ao salvar ou sair. | Close the game before writing. It may overwrite the save when it saves or exits. |
| Confirm_Undo_Note | Para desfazer depois, basta restaurar o backup. Mostramos como na próxima tela. | To undo later, just restore the backup. We show how on the next screen. |
| Confirm_Undo_Note_New | O save original não muda. Para descartar o novo save, basta apagá-lo. Mostramos como na próxima tela. | The original save doesn't change. To discard the new save, just delete it. We show how on the next screen. |
| Confirm_Confirm | Gravar no save | Write to save |
| Confirm_Confirm_New | Criar novo save | Create new save |
| Confirm_Applying | Gravando… | Writing… |
| Confirm_Applying_Hint | Criando o backup e gravando o save. Não feche o app. | Creating the backup and writing the save. Don't close the app. |
| Confirm_Applying_Hint_New | Criando o novo save e gravando as mudanças. Não feche o app. | Creating the new save and writing the changes. Don't close the app. |

`Confirm_Warn_*_Running` só quando `IGameStatusMonitor.IsRunning` for verdadeiro
ao abrir o diálogo (a checagem que vale continua sendo a do `ApplyAsync`).
`Confirm_Cloud_*` só quando `ProfileInfo.IsSteamCloud`. `Confirm_NewName_Default` recebe o nome do save aberto; `Confirm_NewName_Invalid` vale para nome vazio (só espaços) ou com mais de 40 caracteres (limite aprovado pelo usuário). `Confirm_Dest_Overwrite_Help` recebe o nome do save aberto.

## Resultado (`Result_*`)
| Chave | pt-BR | en |
| --- | --- | --- |
| Result_Title | Mudanças gravadas | Changes written |
| Result_Title_New | Novo save criado | New save created |
| Result_Written_One | 1 mudança gravada no save. | 1 change written to the save. |
| Result_Written_Other | {0} mudanças gravadas no save. | {0} changes written to the save. |
| Result_WrittenNew_One | 1 mudança gravada no novo save “{0}”. | 1 change written to the new save “{0}”. |
| Result_WrittenNew_Other | {0} mudanças gravadas no novo save “{1}”. | {0} changes written to the new save “{1}”. |
| Result_Slot_Title | Save gravado | Saved to |
| Result_Slot_Title_New | Novo save | New save |
| Result_Slot_Name | {0} · slot {1} | {0} · slot {1} |
| Result_Slot_Help | O save aberto foi atualizado. | The open save was updated. |
| Result_Slot_Help_New | Criado ao lado do original, que não foi alterado. | Created next to the original, which wasn't changed. |
| Result_Backup_Title | Backup do save original | Backup of the original save |
| Result_Backup_Help | Guardamos uma cópia do save como ele estava antes. Ela fica na mesma pasta. | We kept a copy of the save as it was before. It sits in the same folder. |
| Result_OpenFolder | Abrir pasta | Open folder |
| Result_OpenFolder_Failed | Não foi possível abrir a pasta. Ela está em: {0} | Couldn't open the folder. It is at: {0} |
| Result_CopyPath | Copiar caminho | Copy path |
| Result_PathCopied | Caminho copiado | Path copied |
| Result_Now_Title | Agora no jogo | Now in the game |
| Result_Now_Body | Abra o jogo e carregue este save para ver as mudanças. | Open the game and load this save to see the changes. |
| Result_Now_Body_New | Abra o jogo e carregue “{0}” para ver as mudanças. | Open the game and load “{0}” to see the changes. |
| Result_Session_New | A edição continua no novo save. | Editing continues in the new save. |
| Result_TextNote | O Own Expeditor grava o save em texto simples. Se o jogo não carregar este save, restaure o backup. | Own Expeditor writes the save as plain text. If the game doesn't load this save, restore the backup. |
| Result_TextNote_New | O Own Expeditor grava o save em texto simples. Se o jogo não carregar o novo save, apague-o: o original continua intacto. | Own Expeditor writes the save as plain text. If the game doesn't load the new save, delete it: the original stays intact. |
| Result_Cloud_Overwrite | Se o jogo trouxer o save antigo de volta (Steam Cloud), feche o jogo e grave de novo. | If the game brings the old save back (Steam Cloud), close the game and write again. |
| Result_Cloud_New | Se o novo save não aparecer no jogo (Steam Cloud), grave de novo com “Sobrescrever este save”. | If the new save doesn't show up in the game (Steam Cloud), write again with “Overwrite this save”. |
| Result_Restore_Title | Como restaurar o backup | How to restore the backup |
| Result_Restore_Step1 | Feche o jogo. | Close the game. |
| Result_Restore_Step2 | Na pasta do save, renomeie game.sii para game.sii.old. | In the save folder, rename game.sii to game.sii.old. |
| Result_Restore_Step3 | Renomeie o backup (game.sii.bak-…) para game.sii. | Rename the backup (game.sii.bak-…) to game.sii. |
| Result_Discard_Title | Como descartar o novo save | How to discard the new save |
| Result_Discard_Step1 | Feche o jogo. | Close the game. |
| Result_Discard_Step2 | Apague a pasta do novo save (o caminho está acima). | Delete the new save's folder (the path is above). |
| Result_Keep | Continuar editando | Keep editing |

`Result_Slot_Name` recebe o nome exibido do save (`SaveSlotInfo.DisplayName`) e o id do slot (`SaveSlotInfo.Id`). `Result_Written_*` e `Result_WrittenNew_*` usam `ChangeSetCount`. `Result_Cloud_*` só com `IsSteamCloud`. O cartão de backup (`Result_Backup_*`) só aparece quando `ApplyResult.BackupPath` não é nulo (sobrescrita).

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
lê `Error_<Code>`. O teste de paridade garante as **20** chaves nos dois `.resx`. Para `InvalidEdit`, se `CoreException.Reason` existir (chave do módulo, ex.: `Jobs_Error_NoFreeSlot`), a tela mostra o texto do `Reason` no lugar de `Error_InvalidEdit`.

| Chave | pt-BR | en |
| --- | --- | --- |
| Error_SaveNotFound | Não encontramos o arquivo do save. Ele pode ter sido movido, renomeado ou apagado. Volte à lista de saves e escolha outro. | We couldn't find the save file. It may have been moved, renamed or deleted. Go back to the save list and pick another one. |
| Error_SaveParseFailed | Não foi possível entender este save: o conteúdo não foi reconhecido. Pode ser de uma versão do jogo mais nova, de um mod incomum ou um arquivo danificado. Tente outro save do mesmo perfil ou, se continuar, relate o problema. | Couldn't understand this save: its content wasn't recognized. It may come from a newer game version, an unusual mod or a damaged file. Try another save from the same profile or, if it keeps happening, report the problem. |
| Error_SaveCorrupt | O arquivo do save parece danificado e não foi possível lê-lo. Escolha outro save ou restaure um backup (game.sii.bak-…). | The save file looks damaged and couldn't be read. Pick another save or restore a backup (game.sii.bak-…). |
| Error_NoSession | Nenhum save está aberto. Escolha um perfil e um save para começar. | No save is open. Choose a profile and a save to get started. |
| Error_SessionBusy | O save ainda está sendo lido ou gravado. Espere alguns segundos e tente de novo. | The save is still being read or written. Wait a few seconds and try again. |
| Error_ReadOnlySession | Este save está aberto só para visualização, então não dá para gravar. Abra o save de novo pelo seletor de saves. | This save is open for viewing only, so it can't be written. Open the save again from the save picker. |
| Error_Conflict | Esta mudança altera algo que outra mudança pendente já alterou. Abra a revisão, descarte uma das duas e refaça a edição. | This change edits something another pending change already edited. Open the review, discard one of the two and redo the edit. |
| Error_StalePatch | Os dados desta mudança ficaram desatualizados. Volte à revisão, descarte a mudança e refaça a edição. Se continuar, abra o save de novo. | The data behind this change is out of date. Go back to the review, discard the change and redo the edit. If it keeps happening, open the save again. |
| Error_UnknownUnit | Não encontramos no save o item que você quis alterar. Ele pode ter sido removido. Volte à revisão, descarte a mudança e escolha o item de novo. | We couldn't find the item you wanted to change in the save. It may have been removed. Go back to the review, discard the change and pick the item again. |
| Error_InvalidEdit | Esta mudança não é válida para este save. Descarte-a, ajuste a escolha e tente de novo. | This change isn't valid for this save. Discard it, adjust your choice and try again. |
| Error_UnknownAttribute | Este save não tem o campo que o app tentou alterar. Isso costuma acontecer com saves de mods ou de outra versão do jogo. Descarte a mudança, tente outro item ou atualize o app. | This save doesn't have the field the app tried to change. This usually happens with saves from mods or another game version. Discard the change, try another item or update the app. |
| Error_NothingToApply | Não há mudanças para gravar. Faça uma edição em um módulo e volte para revisar. | There are no changes to write. Make an edit in a module and come back to review. |
| Error_GameRunning | O jogo está aberto, e ele sobrescreveria o save ao salvar ou sair. Feche o jogo e tente de novo. | The game is running, and it would overwrite the save when it saves or exits. Close the game and try again. |
| Error_SaveChangedOnDisk | O arquivo do save mudou depois que você o abriu (por exemplo, o jogo salvou). Para não perder progresso, o app não gravou. Abra o save de novo e refaça as mudanças. | The save file changed after you opened it (for example, the game saved). To avoid losing progress, the app didn't write. Open the save again and redo your changes. |
| Error_BackupExists | Já existe um backup com este mesmo horário, e o app não sobrescreve backups. Espere um segundo e tente de novo. | A backup with this same time already exists, and the app never overwrites backups. Wait a second and try again. |
| Error_BackupFailed | Não foi possível criar o backup, então o app não gravou. Verifique o espaço livre e as permissões da pasta do save e tente de novo. | The backup couldn't be created, so the app didn't write. Check the free space and permissions of the save folder and try again. |
| Error_WriteFailed | Não foi possível gravar o arquivo. Veja se outro programa (como antivírus ou backup na nuvem) está usando a pasta do save e tente de novo. | The file couldn't be written. Check whether another program (such as antivirus or cloud backup) is using the save folder and try again. |
| Error_VerifyFailed | O arquivo gravado não passou na verificação, então o save original foi mantido. Tente de novo; se repetir, abra o save de novo e refaça as mudanças. | The written file didn't pass verification, so the original save was kept. Try again; if it repeats, open the save again and redo your changes. |
| Error_SlotUnavailable | Não foi possível criar um novo save: o perfil não tem slot livre ou a pasta não pôde ser criada. Volte à revisão e grave com “Sobrescrever este save”, ou apague um save antigo no jogo. | Couldn't create a new save: the profile has no free slot or the folder couldn't be created. Go back to the review and write with “Overwrite this save”, or delete an old save in the game. |
| Error_AccessDenied | O sistema negou o acesso à pasta do save. Veja se a pasta não é somente leitura nem está protegida (por exemplo, por antivírus ou nuvem) e tente de novo. | The system denied access to the save folder. Check that the folder isn't read-only or protected (for example by antivirus or cloud sync) and try again. |

Observações:
- Nos textos do `.resx` use o apóstrofo reto `'` do inglês como no resto do app; o canvas usa `’` só por tipografia.
- `Error_SaveParseFailed` **substitui** `Error_SaveFormatUnsupported` (ADR 0006): o arquivo foi lido, mas o conteúdo não foi entendido (o app decodifica saves em texto e binários; não há instrução de `g_save_format`). A tela "Não foi possível entender o save" de `design/shell/` usa as chaves `OpenError_Parse_*`; `Error_SaveParseFailed` é o texto de uma linha quando o erro aparece fora dela.
- `Error_InvalidEdit` é o texto genérico de reserva; os motivos específicos de cada módulo ficam em chaves do módulo (Fretes: `Jobs_Error_*`).
- `Error_SlotUnavailable`: só no destino "Criar um novo save".
- Nenhum termo do formato SII (unidade, atributo, patch, bloco) aparece para o usuário.
