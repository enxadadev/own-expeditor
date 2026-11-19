# Fretes País → Cidade — textos novos/alterados

Formato `Chave | pt-BR | en`. As demais chaves de [`../fretes/strings.md`](../fretes/strings.md) continuam (erros `Jobs_Error_*`, pendentes, Steam Cloud, sem pasta, distância, textos da revisão) salvo as listadas em "Alteradas" e "Removidas". Convenções da skill `ux-writing`: sem ponto final em rótulos e botões, `…` (um caractere), glossário do jogo. Plural: `_One` / `_Other` (e `_Zero` quando existe), por `ILocalizer.GetPlural`.

**País não é chave `.resx`**: o nome do país vem do jogo, traduzido, pelo `IGameNames` (kind `Country`), como cidade, empresa e carga; sem localização do jogo cai no id legível. Os nomes nos mockups ("Bulgária"/"Bulgaria", "Sérvia"/"Serbia"…) são só exemplo.

## Cartões e campos

| Chave | pt-BR | en |
| --- | --- | --- |
| Jobs_Side_From | Saída | Departure |
| Jobs_Side_To | Chegada | Arrival |
| Jobs_Field_Country | País | Country |
| Jobs_Field_City | Cidade | City |
| Jobs_Field_Company | Empresa | Company |
| Jobs_Field_Slot | Vaga | Slot |
| Jobs_Field_Aria | {0}, {1}: {2} | {0}, {1}: {2} |
| Jobs_Country_Placeholder | Escolha o país | Choose a country |
| Jobs_City_Placeholder | Escolha a cidade | Choose a city |
| Jobs_Company_Placeholder | Escolha a empresa | Choose a company |
| Jobs_Slot_Placeholder | Escolha a vaga | Choose a slot |
| Jobs_Cargo_Placeholder | Escolha a carga | Choose a cargo |
| Jobs_Single_Option | Única opção | Only option |
| Jobs_Swap | Trocar saída e chegada | Swap departure and arrival |

`Step_Cargo` ("Carga" / "Cargo") é o título do cartão ③ e o rótulo do campo de carga (já era `Jobs_Step_Cargo`; mantém o valor). `Jobs_Field_Aria`: {0} = lado (Saída/Chegada/Carga), {1} = campo, {2} = valor ou dica.

## Seletores

| Chave | pt-BR | en |
| --- | --- | --- |
| Jobs_Country_Search | Buscar país… | Search country… |
| Jobs_Slot_Search | Buscar vaga… | Search slot… |
| Jobs_Country_Cities_One | 1 cidade | 1 city |
| Jobs_Country_Cities_Other | {0} cidades | {0} cities |
| Jobs_Country_Other | Outros | Other |
| Jobs_Slot_Free | Vaga livre ({0}) | Free slot ({0}) |
| Jobs_Slot_Replace | Substituir: → {0} · {1} · {2} | Replace: → {0} · {1} · {2} |
| Jobs_Slot_Meta | {0} · expira em {1} | {0} · expires in {1} |

`Jobs_Slot_Replace`: {0} cidade de destino da oferta, {1} carga, {2} km (`Format_Km`). `Jobs_Slot_Meta`: {0} empresa · cidade de destino, {1} tempo restante (`Format_Hours`). `Jobs_Country_Other` agrupa cidades sem país (só quando parte das cidades não tem país).
Reaproveitadas de `fretes`: `Jobs_Picker_SearchCity/Company/Cargo`, `Jobs_Picker_Count_*`, `Jobs_Picker_NoItems`, `Jobs_Companies_*`, `Jobs_Slots_*`, `Jobs_Offers_*`, `Jobs_Pending_One/Other`, `Jobs_Distance_Estimated`, `Format_Km`, `Format_KmApprox`.

## Dicas de campo desabilitado

| Chave | pt-BR | en |
| --- | --- | --- |
| Jobs_Hint_ChooseCountry | Escolha o país primeiro | Choose the country first |
| Jobs_Hint_ChooseCity | Escolha a cidade primeiro | Choose the city first |
| Jobs_Hint_ChooseFromCompany | Escolha a empresa de saída primeiro | Choose the departure company first |
| Jobs_Hint_ChooseToCompany | Escolha a empresa de chegada primeiro | Choose the arrival company first |

## Barra de ação ("Falta escolher…")

| Chave | pt-BR | en |
| --- | --- | --- |
| Jobs_Missing_FromCountry | país de saída | departure country |
| Jobs_Missing_FromCity | cidade de saída | departure city |
| Jobs_Missing_FromCompany | empresa de saída | departure company |
| Jobs_Missing_Slot | vaga | slot |
| Jobs_Missing_ToCountry | país de chegada | arrival country |
| Jobs_Missing_ToCity | cidade de chegada | arrival city |
| Jobs_Missing_ToCompany | empresa de chegada | arrival company |
| Jobs_Missing_Cargo | carga | cargo |
| Jobs_Missing_GroupFrom | saída | departure |
| Jobs_Missing_GroupTo | chegada | arrival |

Até 3 itens faltando a barra lista os campos ("Falta escolher: cidade de saída, empresa de saída, vaga"); com mais, agrupa em `Jobs_Missing_GroupFrom`, `Jobs_Missing_GroupTo` e `Jobs_Missing_Cargo` ("Falta escolher: saída, chegada, carga"). Os mesmos nomes entram nos avisos `Jobs_Notice_Cleared` e `Jobs_Notice_Replaced` (o segundo com a primeira letra maiúscula).

## Alteradas

| Chave | pt-BR | en | Mudança |
| --- | --- | --- | --- |
| Jobs_Hint_Locked | Travado na edição | Locked while editing | era "Fixa na edição" (agora serve para país, cidade, empresa e vaga) |
| Jobs_Editing_Body | A saída (cidade, empresa e vaga) fica a mesma. Ao atualizar, ela substitui a anterior na revisão. | The departure (city, company and slot) stays the same. When you update, it replaces the previous one in review. | era "A empresa e a vaga ficam as mesmas…" |
| Jobs_NoCatalog_Body | Sem ela não há o campo País: as cidades vêm numa lista só, com os nomes como estão no save e só as regras de carga que ele revela. Pode haver menos opções. | Without it there is no Country field: cities come in one list, with the names as in the save and only the cargo rules it reveals. There may be fewer options. | acrescenta o efeito no campo País |
| Jobs_Notice_Cleared | Limpamos {0}: a nova escolha muda as opções. | Cleared {0}: the new choice changes the options. | mesma chave; {0} agora lista campos novos ("cidade de chegada, empresa de chegada, carga") |
| Jobs_Notice_Replaced | {0}: só restou uma opção, então ela foi escolhida. | {0}: only one option was left, so it was chosen. | mesma chave; {0} idem, com maiúscula inicial |
| Jobs_Swap | Trocar saída e chegada | Swap departure and arrival | era "…saída e destino" (o lado agora se chama Chegada) |
| Jobs_Map_Empty_Aria | Mapa da rota: escolha as duas cidades | Route map: choose both cities | era "…escolha as cidades" |
| Jobs_Map_Empty | Escolha as duas cidades | Choose both cities | nova: título do mapa vazio (substitui `Jobs_Map_PickFrom`/`PickTo`) |

## Revisão de UI (novas)

| Chave | pt-BR | en | Uso |
| --- | --- | --- | --- |
| Jobs_Swap_Short | Trocar | Swap | rótulo do botão entre os cartões empilhados (largura < 1100 px) |
| Jobs_Missing_NoSlot | Esta empresa não tem vaga nem oferta disponível | This company has no slot or offer available | barra de ação, saída escolhida sem opção de vaga |
| Jobs_Loading | Atualizando as opções de frete… | Updating the job options… | estado Carregando do módulo |
| Jobs_LoadError_Title | Não foi possível carregar os fretes | Couldn't load the jobs | estado Erro do módulo (ação: `Common_Retry`) |
| Jobs_LoadError_Body | Seu save não foi alterado. Tente de novo ou volte e escolha outro save. | Your save was not changed. Try again or go back and choose another save. | estado Erro do módulo |

## Removidas

`Jobs_List_*` (lista de cidades de saída), `Jobs_Search_*` (busca da lista), `Jobs_Step_FromCity`, `Jobs_Step_ToCity`, `Jobs_Step_FromCompany`, `Jobs_Step_ToCompany`, `Jobs_FromCity_Placeholder`, `Jobs_ToCity_Placeholder`, `Jobs_Chip_*`, `Jobs_Hint_NeedCities`, `Jobs_Hint_NeedFromCompany`, `Jobs_Hint_NeedCargo`, `Jobs_Hint_OnlyOption` (vira `Jobs_Single_Option`), `Jobs_Hint_Options_*`, `Jobs_Mode_*` (modo vaga livre/substituir), `Jobs_Step_Offer`, `Jobs_Offer_Meta`, `Jobs_Offer_Expires`, `Jobs_Picker_OnlyPossible`, `Jobs_Picker_SameCity`, `Jobs_Picker_NoMatch`, `Jobs_Map_PickFrom`, `Jobs_Map_PickTo`, `Jobs_NoMatch_*` (nenhuma combinação deixa de existir: só opções válidas aparecem), `Jobs_Notice_SameCity`, `Jobs_Missing_Offer`, `Jobs_Distance_FromSave`, `Jobs_Expiration_Help`, `Jobs_Units_Help`, `Jobs_Trailer_Help`.

**Continuam em uso** (não remover): `Jobs_Empty_*` (estado vazio do módulo, `fretes` §4.5), `Jobs_Distance_Pending` (mapa da rota), `Jobs_Distance_Estimated*`, `Jobs_Map_Route`, `Jobs_Map_Schematic`, `Jobs_Map_Label`, `Jobs_Map_Empty_Aria` (o texto de `Jobs_Map_Empty_Aria` passa a "Mapa da rota: escolha as duas cidades"). Ao implementar, o `ui-dev` confere a paridade dos `.resx` pelo script de i18n (skill `i18n-theming`).

## Observações

- Nenhum termo do formato SII aparece. "Vaga" e "oferta" são os termos do usuário (`ets2-domain`).
- Os textos do mockup `Escolher.dc.html` usam exatamente estas chaves; a barra de prévia (Claro/Escuro, Português/English, nomes das variações) **não** faz parte do app.
- Sem pasta do jogo não há campo País, então não há chave de país por id legível.
