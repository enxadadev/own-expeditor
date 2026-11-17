# Textos dos controles (pt-BR / en)

Status: rascunho. Vira `Strings.resx` (en) e `Strings.pt-BR.resx`, mesmas chaves nos dois. Convenção `Area_Elemento` (skill `i18n-theming`), tom e glossário da skill `ux-writing` (sem ponto final em rótulos/botões/títulos; `…` num caractere só em ações em andamento e campos que abrem lista; maiúscula só na primeira palavra).

Regras gerais:
- **Plural**: chaves `_Zero`, `_One`, `_Other`, escolhidas por `ILocalizer.GetPlural(chave, n)` (0 → Zero, 1 → One, demais → Other; igual nos dois idiomas). Pergunta em aberto no README.
- **Números**: nunca montar à mão. `Format_Km` = `{0:N0} km` (a cultura decide `1.234` ou `1,234`).
- Textos de **amostra** da galeria (nomes de cidade, carga, mods) não são chaves: vêm das amostras sintéticas.
- Os controles **não** têm texto literal. Textos de conteúdo (título, mensagem) chegam por propriedade; as chaves abaixo são os textos fixos dos controles, os nomes acessíveis e os textos padrão.
- "Folga en" = comprimento do en em relação ao pt-BR; reservar 30% de largura extra mesmo assim.

## Comuns
| Chave | pt-BR | en |
| --- | --- | --- |
| `Common_Retry` | Tentar de novo | Try again |
| `Common_Cancel` | Cancelar | Cancel |
| `Common_Dismiss` | Dispensar | Dismiss |
| `Common_Clear` | Limpar | Clear |
| `Common_Details` | Detalhes | Details |
| `Common_Edit` | Editar | Edit |
| `Common_Refresh` | Atualizar | Refresh |
| `Common_Unknown` | Sem valor | No value |
| `Format_Km` | {0:N0} km | {0:N0} km |

## PageHeader
| Chave | pt-BR | en |
| --- | --- | --- |
| `PageHeader_Count_A11y` | Total: {0} | Total: {0} |

## EmptyState
| Chave | pt-BR | en |
| --- | --- | --- |
| `Empty_Default_Title` | Nada por aqui | Nothing here yet |
| `Empty_NoResults_Title` | Nada encontrado | Nothing found |
| `Empty_NoResults_Message` | Nenhum resultado para “{0}”. Tente outro termo. | No results for “{0}”. Try another term. |
| `Empty_NoResults_Action` | Limpar busca | Clear search |

(O texto de "sem ofertas" é do módulo Fretes: `Jobs_Empty_Title` / `Jobs_Empty_Message`, em `design/fretes/`. Exemplo da galeria: "Nenhuma oferta neste save" / "Abra o mercado de fretes no jogo, salve e tente de novo." / "Recarregar save".)

## ErrorBanner
| Chave | pt-BR | en |
| --- | --- | --- |
| `Banner_Severity_Error` | Erro | Error |
| `Banner_Severity_Warning` | Aviso | Warning |
| `Banner_Severity_Info` | Informação | Information |
| `Banner_Name_A11y` | {0}: {1} | {0}: {1} |
| `Banner_Details_Show` | Mostrar detalhes | Show details |
| `Banner_Details_Hide` | Ocultar detalhes | Hide details |
| `Banner_Dismiss_A11y` | Dispensar aviso | Dismiss message |

(Títulos e mensagens de erro: `Error_<Code>_Title` / `Error_<Code>_Message`, em UX-2. Exemplos da galeria: "Não foi possível gravar" + "O jogo está aberto. Feche o jogo e tente de novo." / "Couldn’t save" + "The game is running. Close it and try again.")

## LoadingPanel
| Chave | pt-BR | en |
| --- | --- | --- |
| `Loading_Default` | Carregando… | Loading… |
| `Loading_Cancelling` | Cancelando… | Cancelling… |
| `Loading_Map` | Carregando mapa… | Loading map… |
| `Loading_Progress_A11y` | {0}% concluído | {0}% complete |

(Exemplo: `Session_Reading` = "Lendo save…" / "Reading save…", do shell.)

## StatTile
| Chave | pt-BR | en |
| --- | --- | --- |
| `Stat_A11y` | {0}: {1} | {0}: {1} |
| `Stat_Unknown_A11y` | {0}: sem valor | {0}: no value |

## ValueChange e DiffList
| Chave | pt-BR | en |
| --- | --- | --- |
| `Diff_Value_Empty` | (vazio) | (empty) |
| `Diff_Change_A11y` | {0}: de {1} para {2} | {0}: from {1} to {2} |
| `Diff_Discard` | Descartar | Discard |
| `Diff_Discard_A11y` | Descartar mudança: {0} | Discard change: {0} |
| `Diff_Empty_Title` | Nenhuma mudança pendente | No pending changes |
| `Diff_Empty_Message` | Edite algo em um módulo e volte aqui para revisar. | Edit something in a module and come back here to review. |
| `Diff_Group_Lines_Zero` | Sem linhas | No lines |
| `Diff_Group_Lines_One` | {0} linha | {0} line |
| `Diff_Group_Lines_Other` | {0} linhas | {0} lines |

## PendingBadge
| Chave | pt-BR | en |
| --- | --- | --- |
| `Pending_Count_Zero` | Nenhuma mudança pendente | No pending changes |
| `Pending_Count_One` | {0} mudança pendente | {0} pending change |
| `Pending_Count_Other` | {0} mudanças pendentes | {0} pending changes |
| `Pending_Overflow` | {0}+ | {0}+ |
| `Pending_Overflow_A11y` | mais de {0} mudanças pendentes | more than {0} pending changes |
| `Pending_Modified` | Modificado | Modified |

## SearchBox
| Chave | pt-BR | en |
| --- | --- | --- |
| `Search_Placeholder_Default` | Buscar… | Search… |
| `Search_Clear_A11y` | Limpar busca | Clear search |
| `Search_Busy` | Buscando… | Searching… |
| `Search_Results_Zero` | Nenhum resultado | No results |
| `Search_Results_One` | {0} resultado | {0} result |
| `Search_Results_Other` | {0} resultados | {0} results |

(Placeholders por módulo: `Jobs_Search_Placeholder` = "Cidade, empresa ou carga…" / "City, company or cargo…".)

## SearchablePicker
| Chave | pt-BR | en |
| --- | --- | --- |
| `Picker_None` | Nenhum selecionado | None selected |
| `Picker_Name_A11y` | {0}: {1} | {0}: {1} |
| `Picker_Required_A11y` | {0} (obrigatório) | {0} (required) |
| `Picker_Required_Error` | Escolha uma opção | Choose an option |
| `Picker_Open_A11y` | Abrir lista: {0} | Open list: {0} |
| `Picker_Clear_A11y` | Limpar seleção | Clear selection |
| `Picker_CountOf` | {0} de {1} | {0} of {1} |
| `Picker_Loading` | Carregando opções… | Loading options… |
| `Picker_NoResults` | Nenhuma opção encontrada | No options found |
| `Picker_Empty` | Nenhuma opção disponível | No options available |
| `Picker_Tag_Mod` | Mod | Mod |

(Rótulos e placeholders por campo: `Jobs_Origin_Label` = Origem / Origin, `Jobs_Origin_Placeholder` = "Escolher origem…" / "Choose origin…", etc., em `design/fretes/`.)

## RouteOfferItem
| Chave | pt-BR | en |
| --- | --- | --- |
| `Offer_A11y` | {0} para {1}, {2}, {3} | {0} to {1}, {2}, {3} |
| `Offer_Modified` | Modificada | Modified |
| `Offer_Expired` | Expirada | Expired |
| `Offer_Tooltip` | {0} → {1} | {0} → {1} |

(`{0}`/`{1}` = "Empresa · Cidade". Em `Offer_A11y`, `{2}` = distância formatada, `{3}` = "carga, reboque"; "modificada"/"expirada" são acrescentados com os dois textos acima.)

## RouteMap
| Chave | pt-BR | en |
| --- | --- | --- |
| `Route_Label` | Rota | Route |
| `Route_A11y` | Mapa da rota de {0} até {1}, {2} | Route map from {0} to {1}, {2} |
| `Route_A11y_Empty` | Mapa da rota: nenhuma oferta escolhida | Route map: no offer picked |
| `Route_Empty_Title` | Escolha uma oferta | Pick an offer |
| `Route_Empty_Message` | A rota aparece aqui. | The route appears here. |
| `Route_SameCity` | Origem e destino na mesma cidade | Origin and destination in the same city |
| `Route_Origin` | Origem | Origin |
| `Route_Destination` | Destino | Destination |

## Galeria (Debug)
Ficam em `Gallery.resx` (excluído do Release; pergunta em aberto no README). A galeria é ferramenta de desenvolvimento, mas segue a regra "sem texto literal".

| Chave | pt-BR | en |
| --- | --- | --- |
| `Gallery_Title` | Galeria de controles | Control gallery |
| `Gallery_Theme_Both` | Lado a lado | Side by side |
| `Gallery_Theme_Light` | Claro | Light |
| `Gallery_Theme_Dark` | Escuro | Dark |
| `Gallery_LongTexts` | Textos longos | Long texts |
| `Gallery_HitAreas` | Áreas de clique | Hit areas |
| `Gallery_Language` | Idioma | Language |
| `Gallery_State_Normal` | normal | normal |
| `Gallery_State_Hover` | hover | hover |
| `Gallery_State_Pressed` | pressed | pressed |
| `Gallery_State_Focus` | focus-visible | focus-visible |
| `Gallery_State_Disabled` | disabled | disabled |
| `Gallery_State_Selected` | selecionado | selected |
| `Gallery_State_Empty` | vazio | empty |
| `Gallery_State_Loading` | carregando | loading |
| `Gallery_State_Error` | erro | error |
| `Gallery_State_LongText` | texto longo | long text |
