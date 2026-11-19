# Nomes de reboque — textos

Formato: `Chave | pt-BR | en`. Vira `Strings.resx` (en, neutro) e `Strings.pt-BR.resx`. Convenções da skill `ux-writing`: sem ponto final em rótulos; maiúscula só na primeira palavra; números pela cultura. Plural por grupos `_One`/`_Other` (`ILocalizer.GetPlural`).

Chaves **fora** do vocabulário (mods): sem chave, mostra-se o token humanizado. Nunca `!chave!`.

## 1. Vocabulário de nome

### Carroceria (`Trailer_Body_<token>`)
| Chave | pt-BR | en | Dúvida / nota |
| --- | --- | --- | --- |
| Trailer_Body_box | Baú | Box trailer | |
| Trailer_Body_flatbed | Plataforma | Flatbed | pt: também "Prancha"; "Plataforma" evita confundir com lowbed |
| Trailer_Body_lowbed | Prancha rebaixada | Lowbed | pt: lowbed e lowloader são quase iguais; confirmar a diferença no jogo |
| Trailer_Body_lowloader | Prancha baixa | Low loader | idem |
| Trailer_Body_gooseneck | Gooseneck | Gooseneck | pt: sem termo corrente; "pescoço de ganso" é pouco usado |
| Trailer_Body_dumper | Basculante | Dump trailer | |
| Trailer_Body_log | Transporte de toras | Log trailer | |
| Trailer_Body_foodtank | Tanque alimentício | Food tanker | |
| Trailer_Body_silo | Silo | Silo trailer | |
| Trailer_Body_livestock | Transporte de animais | Livestock trailer | |

### Acabamento (`Trailer_Finish_<token>`; vence a carroceria)
| Chave | pt-BR | en | Dúvida / nota |
| --- | --- | --- | --- |
| Trailer_Finish_dryvan | Baú seco | Dry van | |
| Trailer_Finish_dryvan_m | Baú seco | Dry van | `_m` desconhecido; tratado como `dryvan` (pergunta 4) |
| Trailer_Finish_curtain | Sider | Curtainsider | pt: "Sider" é o termo do setor; "Lona" é a alternativa |
| Trailer_Finish_insulated | Baú isolado | Insulated box | |
| Trailer_Finish_reefer | Baú refrigerado | Reefer | alternativa pt: "Baú frigorífico" (mantive o exemplo pedido) |
| Trailer_Finish_brick | Transporte de tijolos | Brick carrier | |
| Trailer_Finish_container | Porta-contêiner | Container chassis | |
| Trailer_Finish_car_trans | Transporte de carros | Car transporter | alternativa pt: "Cegonha" (termo de caminhoneiro) |
| Trailer_Finish_van_trans | Transporte de vans | Van transporter | |
| Trailer_Finish_truck_trans | Transporte de caminhões | Truck transporter | |
| Trailer_Finish_panel_trans | Transporte de painéis | Panel transporter | |
| Trailer_Finish_gls_trailer | Transporte de vidro | Glass transporter | `gls` lido como *glass* (inferência) |

### Cadeia (`Trailer_Chain_<token>`; `single` não tem chave: não aparece)
| Chave | pt-BR | en | Dúvida / nota |
| --- | --- | --- | --- |
| Trailer_Chain_double | Rodotrem | Double | **Escolha:** cavalo + 2 semirreboques com dolly (A-train). Não verificado com a tradução oficial |
| Trailer_Chain_bdouble | Bitrem | B-double | **Escolha:** 2 semirreboques engatados em sequência; é o equivalente brasileiro direto |
| Trailer_Chain_hct | HCT | HCT | Sigla mantida (alta capacidade). "Treminhão" é o termo BR de caminhão + 2 reboques, mas não é o mesmo arranjo com certeza |

## 2. Composição

| Chave | pt-BR | en | Uso |
| --- | --- | --- | --- |
| Trailer_Name_WithChain | {0} · {1} | {0} · {1} | nome-base · cadeia: "Baú refrigerado · Bitrem" |
| Trailer_Axles_One | 1 eixo | 1 axle | eixo único (raro) |
| Trailer_Axles_Other | {0} eixos | {0} axles | contagem simples (plural geral) |
| Trailer_Axles_Groups | {0} eixos | {0} axles | grupos, `{0}` = "3+2" ou "3+3" (texto) |
| Trailer_Length_short | curto | short | comprimento do reboque |
| Trailer_Length_long | longo | long | |
| Trailer_Details_Join | {0} · {1} | {0} · {1} | eixos · comprimento |
| Trailer_Details_Paren | {0} ({1}) | {0} ({1}) | diff: nome (detalhes) |
| Trailer_IdTip | ID do reboque: {0} | Trailer ID: {0} | 2ª linha do tooltip; `{0}` sem o prefixo `trailer_def.` |
| Trailer_Automation | Reboque: {0} | Trailer: {0} | nome acessível do cartão, sem detalhes |
| Trailer_Automation_Details | Reboque: {0}, {1} | Trailer: {0}, {1} | com detalhes |
| Trailer_Automation_None | Reboque: nenhum | Trailer: none | sem reboque |

## 2b. Ícone (PROPOSTO)

O ícone não tem texto próprio (decorativo, oculto para leitor de tela). Só o selo da cadeia dupla precisa de uma chave, para não haver "×" fixo no XAML.

| Chave | pt-BR | en | Uso |
| --- | --- | --- | --- |
| Trailer_Badge | ×{0} | ×{0} | selo sobre o ícone em 20/24 px; `{0}` = 2 (`double`, `bdouble`) ou 3 (`hct`); formatado pela cultura |

Sem chave de nome acessível para o ícone: o nome acessível do cartão/opção/diff (`Trailer_Automation*`) já descreve o reboque. Sem tooltip no ícone.

Mantidas como estão: `Jobs_Field_Trailer` (Reboque / Trailer) e `Jobs_Trailer_Help` (vai para o tooltip e `HelpText`). `Jobs_Trailer_Help` perde a exibição fixa no cartão (pergunta 1 do README).

## 3. Exemplos (id → nome e detalhes)

Calculados pela regra de [`screens.md`](screens.md) §1; são a tabela de teste sugerida. "—" = detalhes vazios.

| Id da definição | pt-BR nome | pt-BR detalhes | en name | en details | Origem |
| --- | --- | --- | --- | --- | --- |
| scs.box.single_3.reefer | Baú refrigerado | 3 eixos | Reefer | 3 axles | acabamento |
| scs.box.double_3_2.curtain | Sider · Rodotrem | 3+2 eixos | Curtainsider · Double | 3+2 axles | acabamento + cadeia |
| scs.box.bdouble_3_3.dryvan | Baú seco · Bitrem | 3+3 eixos | Dry van · B-double | 3+3 axles | acabamento + cadeia |
| scs.lowbed.single_41_ln.long | Prancha rebaixada | longo | Lowbed | long | carroceria (`long` não é nome; `41` não é eixo) |
| scs.lowloader.single_4_sh.short | Prancha baixa | 4 eixos · curto | Low loader | 4 axles · short | carroceria |
| scs.gooseneck.singl_3e_40.container | Porta-contêiner | 3 eixos | Container chassis | 3 axles | acabamento (`singl` = single; `3e` = 3; `40` ignorado) |
| scs.dumper.single_3.steel | Basculante | 3 eixos | Dump trailer | 3 axles | carroceria (`steel` não é nome) |
| scs.silo.single_3_10.silo | Silo | 3 eixos | Silo trailer | 3 axles | carroceria (`10` ignorado: single usa 1 número) |
| scs.flatbed.single_3.flatbed_w | Plataforma | 3 eixos | Flatbed | 3 axles | carroceria (`flatbed_w` não é nome) |
| car_d.car_trans | Transporte de carros | — | Car transporter | — | acabamento (id curto: sem cadeia nem eixos) |
| glass.panel_trans.glass | Transporte de painéis | — | Panel transporter | — | acabamento (`glass` não está no vocabulário; `panel_trans` está) |
| acme_mods.cargo_box.double_2_2.frozen *(inventado, mod)* | Cargo box · Rodotrem | 2+2 eixos | Cargo box · Double | 2+2 axles | token humanizado + cadeia |
| my_trailer *(inventado, mod)* | My trailer | — | My trailer | — | token humanizado (id inteiro) |

Observação: o fallback humanizado **não é traduzido** (vem do mod), nos dois idiomas ("Cargo box" em pt-BR também); a cadeia e os detalhes continuam traduzidos.
