# Fretes — País → Cidade (simplificação)

- **Status:** aprovado (2026-11-18)
- **Canvas:** fonte em [`canvas/`](canvas/) (`canvas.json` + um `.dc.html` por tela, 1280×800); o link publicado entra aqui depois que a sessão principal publicar.
- **Substitui:** a ordem e o layout de [`../fretes/`](../fretes/README.md) §1–§2 (lista de cidades à esquerda, chip de saída, três campos em linha, "Onde entra o frete"). O resto — distância conhecida/estimada, pendentes, Steam Cloud, sem catálogo, sem origem, erros ao adicionar — continua valendo.
- **Pedido do usuário:** o fluxo atual é confuso; escolher primeiro o **país** e depois a **cidade**, na origem e no destino.

## Objetivo do usuário
Montar um frete escolhendo, em dois blocos iguais, de onde sai (país, cidade, empresa) e para onde vai (país, cidade, empresa), depois a carga, e mandar para a revisão. Nada é gravado aqui.

## Pontos de confusão do fluxo atual
| # | Confusão | Como o desenho novo resolve |
| --- | --- | --- |
| 1 | **Passo 1 duplicado**: a cidade de saída é escolhida numa lista à esquerda *e* aparece de novo como chip "① Cidade de saída" | A lista e o chip saem; a saída é um cartão com País → Cidade → Empresa |
| 2 | **Ordem visual ≠ ordem dos passos**: ①② no topo, depois mapa, pendentes e 4 indicadores, e só então ③④⑤ | Ordem de cima para baixo = ordem da cadeia: Saída, Chegada, Carga, detalhes, mapa (último), ação |
| 3 | **Origem e destino misturados**: empresa de saída é o passo 3, a de chegada o 5, com a carga no meio | Dois cartões com os **mesmos campos**; a carga vem depois da empresa de chegada |
| 4 | **"Onde entra o frete"** (vaga livre × substituir + seletor de oferta): dois controles técnicos para uma escolha que quase sempre nem existe | Um campo **Vaga**, só com 2+ opções: "Vaga livre (n)" e as ofertas a substituir |
| 5 | **Listas planas e longas** de cidades (centenas com mods) | País → Cidade: cada lista só tem as cidades do país |
| 6 | **Seletor de destino carregado**: chave "Só combinações possíveis", etiquetas "Sem combinação", cidade de saída desativada | Só aparecem opções que combinam; a chave e as etiquetas saem; a cidade de saída nem é opção |
| 7 | **Indicadores antes das escolhas**: Expira em, Unidades, Reboque aparecem antes da carga | Só aparecem com a carga escolhida |

## Telas e estados (um artboard por tela; a barra de prévia embaixo troca as variações)
| Canvas | Estado | Variações |
| --- | --- | --- |
| [`Escolher.dc.html`](canvas/Escolher.dc.html) | Fluxo completo, clicável: Saída (País → Cidade → Empresa [+ Vaga com 2+ opções]), Chegada (País → Cidade → Empresa), ⇄, ③ Carga, detalhes, mapa da rota com a distância (último bloco), barra "Falta escolher…" / "Adicionar à revisão" | Vazio · País escolhido · Cidade · Empresa · Chegada · Carga · Pronto · Adicionado (toast) |
| [`Seletores.dc.html`](canvas/Seletores.dc.html) | Os popups do `SearchablePicker` | País (saída, "n cidades", busca) · Cidade de saída (empresas · vagas · ofertas, selo pendente) · País (chegada) · Cidade de chegada (km conhecido, "≈ km" estimado) · Vaga (Vaga livre (n) + ofertas a substituir) · Única opção auto-escolhida · Campos desabilitados com dica |
| [`Estados.dc.html`](canvas/Estados.dc.html) | Estados especiais do formulário | Sem pasta do jogo (País oculto, aviso Info) · Editando pendente (saída travada) · Pendentes na cidade · Troca que limpa campos ("Limpamos…") · Troca com só uma opção · Steam Cloud |

Todas funcionam em Claro/Escuro e pt-BR/en (botões na barra de prévia, que não faz parte do app). Documentos: [flows.md](flows.md) · [screens.md](screens.md) · [strings.md](strings.md).

## Proposta (resumo)
Dois cartões lado a lado com os **mesmos campos**, e a carga embaixo:

| ① Saída | ② Chegada |
| --- | --- |
| País ▾ | País ▾ |
| Cidade ▾ | Cidade ▾ |
| Empresa ▾ | Empresa ▾ |
| Vaga ▾ (só com 2+ opções) | |

③ **Carga** ▾ → detalhes (Expira em · Unidades · Reboque) → **Adicionar à revisão**.

## Decisões
1. **País → Cidade → Empresa** nos dois lados. O país vem do save + catálogo do jogo (`city.country` das definições de cidade); nada fixo no código. Nome do país pelo `IGameNames` (kind `Country`), **não** por `.resx`, com fallback para o id legível.
2. **Só aparecem opções que combinam.** Países de saída = países com alguma cidade de saída (vaga livre, oferta ou pendente). Países de chegada = países com alguma empresa que aceita uma carga que a empresa de saída envia. Nunca se monta combinação inválida: a chave "Só combinações possíveis" e as etiquetas "Sem combinação" saem.
3. **Nova ordem de dependência:** a empresa de chegada vem antes da carga. A empresa de chegada lista quem aceita **ao menos uma** carga enviada pela empresa de saída; a carga lista o que a saída envia **e** a chegada aceita. A chegada só abre depois da empresa de saída (as opções dependem do que ela envia).
4. **Vaga num campo só, e só quando preciso:** "Vaga" (`SearchablePicker`) com "Vaga livre (3)" + cada oferta substituível ("Substituir: → Belgrado · Maquinário · 394 km"). Vaga livre é o padrão se houver; se só houver ofertas, o campo aparece vazio; com uma só opção, nem aparece.
5. **Escolha única = escolhida sozinha** em todos os campos, com a etiqueta "Única opção" (também ao abrir o módulo).
6. **Lista da esquerda sai.** O cartão "Mudanças pendentes nesta cidade" fica logo abaixo dos cartões de Saída e Chegada.
7. **Mapa por último e grande:** o mapa da rota é o **último bloco da página**, depois de Carga e detalhes e logo acima da barra fixa, com 300 px de altura e largura total (a página rola). Reserva o lugar do mapa real do jogo, que outra frente implementa; hoje é o esquema, com a nota "Esquema da rota, sem posições reais" só como placeholder. A distância (conhecida ou "≈ estimada" com a etiqueta) fica sobre o mapa. O botão ⇄ troca país e cidade dos dois lados; empresas, vaga e carga são limpas.
8. **A cidade de saída nunca é opção de chegada**: o aviso "Limpamos a cidade de destino…" deixa de existir.
9. **Sem pasta do jogo** (sem país conhecido): os campos País somem e as cidades vêm numa lista só, com o aviso Info de sempre. Cidades sem país, quando só parte tem país, vão no grupo "Outros".
10. **Edição de pendente:** a saída (País, Cidade, Empresa, Vaga) fica travada com cadeado e "Travado na edição"; chegada e carga editáveis. Editar = reenfileirar com a mesma `Key`, como em `fretes`.
11. **Depois de "Adicionar":** ficam países, cidades e empresa de saída; limpam Vaga (volta a "Vaga livre" se ainda houver), empresa de chegada e Carga, para encadear fretes entre as mesmas cidades.
12. **Sem componente novo:** só `SearchablePicker`, `SectionCard`, `StepBadge`, `InfoTag`, `RouteMap`, `SegmentedChoice`, `StatTile`, `ErrorBanner`, `Toast`. `CityListItem` e o segmentado "Onde entra o frete" saem do catálogo previsto.

## Contratos que o desenho exige (para o `chief-architect`)
Em relação a `fretes` (`JobBoard`): países e cidades agrupados, chegada dependente da empresa de saída.
```csharp
IReadOnlyList<CountryInfo> DepartureCountries();                         // país + nº de cidades de saída
IReadOnlyList<CityOffer> DepartureCities(string? country);               // vaga livre/oferta/pendência; null = sem país
IReadOnlyList<CompanyLocation> DepartureCompanies(string fromCity);      // com vaga livre ou oferta e ≥1 carga aceita em outra cidade
IReadOnlyList<CountryInfo> ArrivalCountries(CompanyLocation from);       // países com empresa que aceita carga de `from`
IReadOnlyList<string> ArrivalCities(CompanyLocation from, string? country);   // nunca a cidade de `from`
IReadOnlyList<CompanyLocation> ArrivalCompanies(CompanyLocation from, string toCity);
IReadOnlyList<string> Cargos(CompanyLocation from, CompanyLocation to);  // envia ∩ aceita
```
`DistanceInfo`, `ExistingOffer`, `SlotMode`, `JobRequest` e a tabela de distâncias de `fretes` continuam. A contagem de vagas e ofertas desconta as pendentes (App, pelas `Key`s de `IEditSession.Pending`).

## Perguntas em aberto
1. **Grupo "Outros"** para cidades sem país (mods): vale a complexidade ou basta esconder o campo País quando alguma cidade não tem país?
2. **Chegada antes da carga**: o usuário perde a possibilidade de "escolher a carga e ver quem recebe". Aceitável, já que a empresa de saída restringe as cargas?
3. **Manter a chegada quando a empresa de saída é trocada**: o desenho mantém País, Cidade e Empresa de chegada e revalida quando a empresa de saída volta. Prefere limpar tudo?
4. **Vaga com 2+ opções**: o padrão "Vaga livre" some quando só há ofertas. Prefere escolher a primeira oferta automaticamente?
5. **Prazo e unidade de `expiration_time`**: seguem como em `fretes` (24 h, 48 h, 72 h, 7 d; unidade ainda por confirmar).
6. **Largura mínima**: abaixo de ~1100 px os cartões empilham; nos 960 px mínimos o formulário rola.
