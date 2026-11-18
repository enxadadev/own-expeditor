# Fretes — textos

Formato: `Chave | pt-BR | en`. Vira `Strings.resx` (en, neutro) e `Strings.pt-BR.resx`. Convenções da skill `ux-writing`: sem ponto final em botões, rótulos e títulos; `…` (um caractere); glossário do jogo (frete, carga, reboque, origem/destino). Plural: grupos `_Zero` (opcional) / `_One` / `_Other`, escolhidos por `ILocalizer.GetPlural(base, n)`; falta `_Zero` → usa `_Other`. Números pela cultura (`{0:N0}`).

**Nomes do jogo** (cidade, empresa, carga, reboque) **não** são chaves `.resx`: vêm da localização do jogo pelo Core (`IGameNames`, ADR 0006) e, sem a pasta do jogo, do id legível (`GameIds.Humanize`). Os nomes nos mockups são exemplos.

O `.resx` en usa apóstrofo reto `'`; o canvas usa `’` só por tipografia.

## Lista de cidades de saída (passo 1)

| Chave | pt-BR | en |
| --- | --- | --- |
| Jobs_List_Title | Cidades de saída | Departure cities |
| Jobs_List_SearchLabel | Buscar cidade de saída | Search departure city |
| Jobs_List_Search | Buscar cidade… | Search city… |
| Jobs_List_Count_One | 1 cidade de saída | 1 departure city |
| Jobs_List_Count_Other | {0} cidades de saída | {0} departure cities |
| Jobs_Companies_One | 1 empresa | 1 company |
| Jobs_Companies_Other | {0} empresas | {0} companies |
| Jobs_Slots_Zero | Sem vagas livres | No free slots |
| Jobs_Slots_One | 1 vaga livre | 1 free slot |
| Jobs_Slots_Other | {0} vagas livres | {0} free slots |
| Jobs_Offers_One | 1 oferta | 1 offer |
| Jobs_Offers_Other | {0} ofertas | {0} offers |
| Jobs_Pending_One | 1 pendente | 1 pending |
| Jobs_Pending_Other | {0} pendentes | {0} pending |
| Jobs_Empty_Title | Nenhuma cidade com vaga ou oferta | No city with a slot or offer |
| Jobs_Empty_Body | Uma cidade só aparece aqui se uma empresa dela tiver uma vaga livre ou uma oferta para substituir e enviar uma carga que o app já viu num frete deste save. | A city only shows up here if one of its companies has a free slot or an offer to replace and ships a cargo the app has already seen in a job in this save. |
| Jobs_Empty_Tip | Abra o mercado de fretes de algumas empresas no jogo, salve e abra o save de novo. | Open the freight market of a few companies in the game, save and open the save again. |
| Jobs_Search_NoResult_Title | Nenhuma cidade encontrada | No city found |
| Jobs_Search_NoResult_Body | Nada para “{0}”. Confira o nome ou limpe a busca. | Nothing for “{0}”. Check the name or clear the search. |
| Jobs_Search_Clear | Limpar busca | Clear search |

## Formulário: passos, campos e dicas

Os 5 passos, na ordem: cidade de saída, cidade de destino, empresa de saída, carga, empresa de chegada.

| Chave | pt-BR | en |
| --- | --- | --- |
| Jobs_Form_Label | Novo frete | New job |
| Jobs_Step_FromCity | Cidade de saída | Departure city |
| Jobs_Step_ToCity | Cidade de destino | Destination city |
| Jobs_Step_FromCompany | Empresa de saída | Departure company |
| Jobs_Step_Cargo | Carga | Cargo |
| Jobs_Step_ToCompany | Empresa de chegada | Arrival company |
| Jobs_Step_Choose | Escolher… | Choose… |
| Jobs_FromCity_Placeholder | Escolha na lista | Choose from the list |
| Jobs_ToCity_Placeholder | Escolher cidade… | Choose city… |
| Jobs_Chip_FromHint | Escolha a cidade de saída na lista à esquerda | Choose the departure city in the list on the left |
| Jobs_Chip_FromAria | Cidade de saída: {0}. Mude na lista | Departure city: {0}. Change it in the list |
| Jobs_Chip_ToAria | Cidade de destino: {0}. Mudar | Destination city: {0}. Change |
| Jobs_Swap | Trocar saída e destino | Swap departure and destination |
| Jobs_Hint_NeedCities | Escolha as duas cidades primeiro | Choose both cities first |
| Jobs_Hint_NeedFromCompany | Escolha a empresa de saída primeiro | Choose the departure company first |
| Jobs_Hint_NeedCargo | Escolha a carga primeiro | Choose the cargo first |
| Jobs_Hint_OnlyOption | Única opção, já escolhida | Only option, already chosen |
| Jobs_Hint_Options_One | 1 opção | 1 option |
| Jobs_Hint_Options_Other | {0} opções | {0} options |
| Jobs_Hint_Locked | Fixa na edição | Locked while editing |
| Jobs_Mode_Title | Onde entra o frete | Where the job goes |
| Jobs_Mode_Free | Usar vaga livre | Use a free slot |
| Jobs_Mode_Replace | Substituir uma oferta | Replace an offer |
| Jobs_Mode_Help | Substituir troca uma oferta atual da empresa: a revisão mostra antes → depois. | Replacing swaps one of the company's current offers: the review shows before → after. |
| Jobs_Mode_NoFree | Sem vaga livre nesta empresa. Substitua uma das ofertas atuais. | No free slot in this company. Replace one of the current offers. |
| Jobs_Mode_NoOffers | Esta empresa não tem ofertas para substituir. Use a vaga livre. | This company has no offers to replace. Use the free slot. |
| Jobs_Mode_Locked | Fixo na edição: para mudar, descarte esta mudança e crie outra. | Locked while editing: to change it, discard this change and create another. |
| Jobs_Step_Offer | Oferta a substituir | Offer to replace |
| Jobs_Offer_Meta | {0} · {1} · {2} | {0} · {1} · {2} |
| Jobs_Offer_Expires | expira em {0} | expires in {0} |

## Seletor (SearchablePicker)

| Chave | pt-BR | en |
| --- | --- | --- |
| Jobs_Picker_SearchCity | Buscar cidade… | Search city… |
| Jobs_Picker_SearchCompany | Buscar empresa… | Search company… |
| Jobs_Picker_SearchCargo | Buscar carga… | Search cargo… |
| Jobs_Picker_SearchOffer | Buscar oferta… | Search offer… |
| Jobs_Picker_Count_One | 1 opção | 1 option |
| Jobs_Picker_Count_Other | {0} opções | {0} options |
| Jobs_Picker_OnlyPossible | Só combinações possíveis | Only possible matches |
| Jobs_Picker_SameCity | Já é a cidade de saída | Already the departure city |
| Jobs_Picker_NoMatch | Sem combinação | No match |
| Jobs_Picker_NoItems | Nada para escolher aqui. | Nothing to choose here. |

## Mapa da rota

| Chave | pt-BR | en |
| --- | --- | --- |
| Jobs_Map_Route | Rota | Route |
| Jobs_Map_Schematic | Esquema da rota, sem posições reais | Route sketch, without real positions |
| Jobs_Map_Label | Rota de {0} até {1} | Route from {0} to {1} |
| Jobs_Map_Empty_Aria | Mapa da rota: escolha as cidades | Route map: choose the cities |
| Jobs_Map_PickFrom | Escolha a cidade de saída na lista | Choose the departure city in the list |
| Jobs_Map_PickTo | Escolha a cidade de destino | Choose the destination city |

## Indicadores (StatTile)

| Chave | pt-BR | en |
| --- | --- | --- |
| Jobs_Field_Distance | Distância | Distance |
| Jobs_Distance_FromSave | Conhecida: vem de ofertas dos seus saves | Known: from offers in your saves |
| Jobs_Distance_Estimated | Estimada | Estimated |
| Jobs_Distance_EstimatedShort | estimada | estimated |
| Jobs_Distance_Estimated_Help | É uma estimativa do app: seus saves não trazem esta rota. O jogo recalcula a rota no GPS. | This is the app's estimate: your saves don't have this route. The game recalculates the route on the GPS. |
| Jobs_Distance_Pending | Escolha as duas cidades | Choose both cities |
| Jobs_Field_Expiration | Expira em | Expires in |
| Jobs_Expiration_Help | Tempo de jogo, a partir do horário do save. | Game time, counted from the save's clock. |
| Format_Hours | {0} h | {0} h |
| Format_Days | {0} d | {0} d |
| Format_KmApprox | ≈ {0:N0} km | ≈ {0:N0} km |
| Jobs_Field_Units | Unidades | Units |
| Jobs_Units_Help | Definidas pelo reboque | Set by the trailer |
| Jobs_Field_Trailer | Reboque | Trailer |
| Jobs_Trailer_Help | Copiado de um frete deste save com a mesma carga | Copied from a job in this save with the same cargo |

## Ação, avisos de dependência e pendências

| Chave | pt-BR | en |
| --- | --- | --- |
| Jobs_Add | Adicionar à revisão | Add to review |
| Jobs_Update | Atualizar na revisão | Update in review |
| Jobs_Missing | Falta escolher: {0} | Still to choose: {0} |
| Jobs_Missing_FromCity | cidade de saída | departure city |
| Jobs_Missing_ToCity | cidade de destino | destination city |
| Jobs_Missing_FromCompany | empresa de saída | departure company |
| Jobs_Missing_Cargo | carga | cargo |
| Jobs_Missing_Offer | oferta a substituir | offer to replace |
| Jobs_Missing_ToCompany | empresa de chegada | arrival company |
| Jobs_Ready | Pronto. Nada é gravado agora: o frete vai para a revisão. | Ready. Nothing is written now: the job goes to review. |
| Jobs_Added_Toast | Frete adicionado à revisão. | Job added to review. |
| Jobs_Added_View | Ver revisão | View review |
| Jobs_Updated_Toast | Mudança atualizada na revisão. | Change updated in review. |
| Jobs_Notice_Cleared | Limpamos {0}: a nova escolha muda as opções. | Cleared {0}: the new choice changes the options. |
| Jobs_Notice_Replaced | {0}: só restou uma opção, então ela foi escolhida. | {0}: only one option was left, so it was chosen. |
| Jobs_Notice_SameCity | Limpamos a cidade de destino: ela não pode ser igual à cidade de saída. | Cleared the destination city: it can't be the same as the departure city. |
| Jobs_Notice_Close | Fechar aviso | Dismiss notice |
| Jobs_Pending_Title | Mudanças pendentes nesta cidade | Pending changes in this city |
| Jobs_Pending_Edit | Editar | Edit |
| Jobs_Pending_EditAria | Editar mudança: {0} | Edit change: {0} |
| Jobs_Pending_Mode_Free | Vaga livre | Free slot |
| Jobs_Pending_Mode_Replace | Substitui oferta | Replaces offer |
| Jobs_Editing_Title | Editando uma mudança pendente | Editing a pending change |
| Jobs_Editing_Body | A empresa e a vaga ficam as mesmas. Ao atualizar, ela substitui a anterior na revisão. | The company and slot stay the same. When you update, it replaces the previous one in review. |
| Jobs_CancelEdit | Cancelar edição | Cancel editing |

## Estados vazios

| Chave | pt-BR | en |
| --- | --- | --- |
| Jobs_NoMatch_Title | Nenhuma combinação possível | No possible match |
| Jobs_NoMatch_Body | Nenhuma empresa de {0} com vaga livre envia uma carga que alguma empresa de {1} aceite. Troque uma das cidades. | No company in {0} with a free slot ships a cargo that a company in {1} accepts. Change one of the cities. |
| Jobs_NoMatch_CatalogNote | Sem a pasta do jogo, valem só as regras de carga vistas no save. | Without the game folder, only the cargo rules seen in the save apply. |
| Jobs_NoMatch_ChangeTo | Trocar cidade de destino | Change destination city |
| Jobs_NoMatch_ChangeFrom | Trocar cidade de saída | Change departure city |
| Jobs_NoSlots_Title | Sem vaga livre nem oferta em {0} | No free slot or offer in {0} |
| Jobs_NoSlots_Body | Todas as vagas e ofertas de {0} já têm uma mudança pendente. Edite ou descarte uma delas para liberar. | Every slot and offer in {0} already has a pending change. Edit or discard one of them to free it. |

## Sem a pasta do jogo e Steam Cloud

| Chave | pt-BR | en |
| --- | --- | --- |
| Jobs_NoCatalog_Title | Pasta do {0} não encontrada | {0} folder not found |
| Jobs_NoCatalog_Body | Mostramos os nomes como estão no save e só as regras de carga que ele revela. Pode haver menos opções. | We show names as they appear in the save and only the cargo rules it reveals. There may be fewer options. |
| Jobs_NoCatalog_Action | Abrir Configurações | Open Settings |
| Jobs_Cloud_Chip | Steam Cloud | Steam Cloud |
| Jobs_Cloud_Title | Este perfil usa Steam Cloud | This profile uses Steam Cloud |
| Jobs_Cloud_Body | A nuvem pode trazer de volta um save antigo ou esconder saves novos. Ao aplicar, você escolhe como gravar. | The cloud may bring back an older save or hide new ones. When you apply, you choose how to write. |
| Jobs_Cloud_Dismiss | Entendi | Got it |

## Erros ao adicionar (`InvalidEdit`, `Reason = Jobs_Error_*`)

Mostrados no `ErrorBanner` do módulo, com o título `Error_Title_Edit`. O Core lança `CoreException(InvalidEdit)` com a chave em `Reason`; a mensagem diz o que aconteceu e o que fazer.

| Chave | pt-BR | en |
| --- | --- | --- |
| Jobs_Error_NoFreeSlot | Esta empresa não tem mais vaga livre: outra mudança pendente já usa a vaga. Escolha uma oferta para substituir, edite ou descarte essa mudança, ou escolha outra empresa de saída. | This company has no free slot left: another pending change already uses it. Choose an offer to replace, edit or discard that change, or choose another departure company. |
| Jobs_Error_OfferMissing | A oferta que você ia substituir não existe mais ou já tem outra mudança pendente. Escolha outra oferta ou use uma vaga livre. | The offer you were going to replace is gone or already has another pending change. Choose another offer or use a free slot. |
| Jobs_Error_CargoNotShipped | A empresa de saída não envia esta carga. Escolha outra carga ou outra empresa de saída. | The departure company doesn't ship this cargo. Choose another cargo or another departure company. |
| Jobs_Error_CargoNotAccepted | A empresa de chegada não recebe esta carga. Escolha outra empresa de chegada ou outra carga. | The arrival company doesn't accept this cargo. Choose another arrival company or another cargo. |
| Jobs_Error_NoTrailer | Este save não tem nenhum frete com esta carga para copiar o reboque. Escolha outra carga. | This save has no job with this cargo to copy the trailer from. Choose another cargo. |
| Jobs_Error_OriginMissing | A empresa de saída não existe mais neste save. Escolha outra empresa de saída. | The departure company is no longer in this save. Choose another departure company. |
| Jobs_Error_TargetMissing | A empresa de chegada não existe neste save. Escolha outra empresa de chegada ou outra cidade de destino. | The arrival company isn't in this save. Choose another arrival company or another destination city. |
| Jobs_Error_SameCity | A cidade de saída e a de destino são iguais. Escolha outra cidade de destino. | The departure and destination cities are the same. Choose another destination city. |
| Jobs_Error_ExpirationInvalid | O prazo escolhido não é válido. Escolha 24 h, 48 h, 72 h ou 7 d. | The chosen time limit isn't valid. Choose 24 h, 48 h, 72 h or 7 d. |
| Jobs_Error_NoGameTime | Não encontramos a data do jogo neste save, então não dá para calcular quando o frete expira. Abra o save de novo; se continuar, escolha outro save. | We couldn't find the game date in this save, so we can't work out when the job expires. Open the save again; if it keeps happening, choose another save. |

## Aparece na revisão (`ChangeSet` de Fretes)

Os textos que `JobPlanner` põe no `ChangeSet` e que a tela Revisar mostra (ver `design/review-apply/`). Os nomes de cidade, empresa, carga e reboque vêm como `DiffValue.GameName` e são traduzidos pela localização do jogo.

| Chave | pt-BR | en |
| --- | --- | --- |
| Jobs_Change_Title | Frete {0} → {1} | Job {0} → {1} |
| Jobs_Field_Origin | Origem | Origin |
| Jobs_Field_Destination | Destino | Destination |
| Jobs_Field_Cargo | Carga | Cargo |

(`Jobs_Field_Distance`, `Jobs_Field_Trailer`, `Jobs_Field_Expiration` e `Jobs_Field_Units` já estão em “Indicadores” e são as mesmas chaves.)

## Chaves de outros donos usadas no mockup

Só para o desenho; o dono é quem as define.

| Chave | pt-BR | en | Dono |
| --- | --- | --- | --- |
| Nav_Label | Módulos | Modules | shell |
| Module_jobs | Fretes | Jobs | shell |
| Module_fleet | Frota | Fleet | shell |
| Module_profile | Perfil | Profile | shell |
| Module_company | Empresa | Company | shell |
| Module_map | Mapa | Map | shell |
| Nav_Settings | Configurações | Settings | shell |
| Nav_Settings_Short | Config. | Settings | shell |
| SessionBar_Label | Sessão | Session | shell |
| SessionBar_Profile | Perfil | Profile | shell |
| SessionBar_Save | Save | Save | shell |
| SessionBar_ChangeHint | Trocar de save… | Change save… | shell |
| SessionBar_ProfileAutomation | Perfil: {0}. Trocar de save | Profile: {0}. Change save | shell |
| SessionBar_SaveAutomation | Save: {0}. Trocar de save | Save: {0}. Change save | shell |
| Game_Ets2 | Euro Truck Simulator 2 | Euro Truck Simulator 2 | shell |
| Game_Ets2_Short | ETS2 | ETS2 | shell |
| SessionBar_GameClosed | Jogo fechado | Game closed | shell |
| SessionBar_Review | Revisar ({0}) | Review ({0}) | shell |
| SessionBar_ReviewNone | Sem mudanças | No changes | shell |
| SessionBar_ReviewHint | Revisar mudanças (Ctrl+R) | Review changes (Ctrl+R) | shell |
| SessionBar_ReviewAutomation | Revisar mudanças: {0} | Review changes: {0} | shell |
| SessionBar_ReviewAutomationNone | Revisar mudanças: nenhuma pendente | Review changes: none pending | shell |
| Shell_Pending_One | 1 mudança pendente | 1 pending change | shell |
| Shell_Pending_Other | {0} mudanças pendentes | {0} pending changes | shell |
| Shell_SafetyNote | Nada é gravado sem revisão. Um backup é criado antes. | Nothing is saved without review. A backup is created first. | shell |
| Shell_HintReview | Revisar | Review | shell |
| Review_Discard | Descartar | Discard | review-apply |
| Review_DiscardItem_Aria | Descartar mudança: {0} | Discard change: {0} | review-apply |
| Toast_Discarded | Mudança descartada. | Change discarded. | review-apply |
| Toast_Undo | Desfazer | Undo | review-apply |
| Error_Title_Edit | Não foi possível adicionar a mudança | Couldn't add the change | review-apply |
| Common_Close | Fechar | Close | review-apply |
| Action_ChooseSave | Escolher outro save | Choose another save | review-apply |
| Format_Km | {0:N0} km | {0:N0} km | review-apply |

## Observações

- `Jobs_Error_*` são o `Reason` (`TextRef`) de `CoreException(InvalidEdit)` lançada pelo `JobPlanner`. O módulo mostra a mensagem no `ErrorBanner` com o título `Error_Title_Edit`; se `Reason` vier vazio, mostra `Error_InvalidEdit` (ver `design/review-apply/strings.md`). Teste de paridade: as 10 chaves nos dois `.resx` (`NoFreeSlot`, `OfferMissing`, `CargoNotShipped`, `CargoNotAccepted`, `NoTrailer`, `OriginMissing`, `TargetMissing`, `SameCity`, `ExpirationInvalid`, `NoGameTime`).
- `Jobs_Hint_OnlyOption` aparece sob o campo quando só havia uma opção e o app já a escolheu; `Jobs_Notice_*` ficam numa região `role=status` (anunciada por leitor de tela).
- A distância nunca é digitada: não há chaves de campo de km. `Jobs_Distance_Estimated` (etiqueta), `Jobs_Distance_EstimatedShort` (“km · estimada” no mapa) e `Format_KmApprox` (“≈ {0:N0} km”, também usada na revisão) marcam a estimativa.
- Limites aprovados: prazos 24 h, 48 h, 72 h e 7 d (`Format_Hours`/`Format_Days`); nome do novo save de 1 a 40 caracteres (`Confirm_NewName_Invalid`, em `design/review-apply/strings.md`).
- `Jobs_Mode_*` e `Jobs_Step_Offer` são o passo “Onde entra o frete”; as contagens `Jobs_Slots_*`/`Jobs_Offers_*` aparecem nos botões, na lista e no seletor de empresas.
- Nenhum termo do formato SII (unidade, bloco, atributo, `job_offer_data`) aparece no texto.
