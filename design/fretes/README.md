# Fretes (módulo)

- **Status:** aprovado (2026-11-18)
- **Canvas:** https://claude.ai/artifact/DGcBW2CtBSZWnZXgzE7PhM; fonte em [`canvas/`](canvas/) (`canvas.json` + um `.dc.html` por tela, 1280×800).
- **Origem:** [ADR 0006](../../docs/decisions/0006-integracao-do-codigo-de-injecao-ao-shell.md) e [plano 0006](../../docs/architecture/plan-0006-integracao.md) tarefa 0.1.
  Dentro do shell aprovado ([`../shell/`](../shell/README.md)), na direção A+C ([`../direcao-visual/`](../direcao-visual/README.md)), com os controles de [`../controles/`](../controles/README.md).

## Objetivo do usuário
Montar um frete novo em poucos passos (cidade de saída, cidade de destino, empresa de saída com a escolha de usar uma vaga livre ou substituir uma oferta existente, carga, empresa de chegada), ver na hora a rota, a distância (que vem do app, nunca é digitada), o prazo e o reboque, e mandar o frete para a revisão. **Nada é gravado aqui**: "Adicionar à revisão" só enfileira um `ChangeSet` (`JobPlanner.Plan`); gravar é na página Revisar do shell.

## Telas e estados (um artboard por tela; a barra de prévia embaixo troca as variações)
| Canvas | Estado | Variações |
| --- | --- | --- |
| [`Escolher.dc.html`](canvas/Escolher.dc.html) | Fluxo completo, clicável | 1 · Saída, 2 · Destino (rota e distância já visíveis), 3 · Empresa, 4 · Carga, 5 · Chegada, Pronto, Adicionado (toast), Erro (cicla os 10 `Jobs_Error_*`) |
| [`ModoFrete.dc.html`](canvas/ModoFrete.dc.html) | **Onde entra o frete**: usar vaga livre ou substituir uma oferta existente | vaga livre, substituir (seletor da oferta aberto), pronto substituindo, empresa sem vaga livre (sugere substituir), empresa sem ofertas, lista de empresas |
| [`Cidades.dc.html`](canvas/Cidades.dc.html) | Cidades e dependências | seletor de destino (a cidade de saída aparece desativada), cidades iguais, par sem combinação, "só combinações possíveis", trocar saída / empresa / destino (limpa só o que deixou de valer) |
| [`DistanciaEstimada.dc.html`](canvas/DistanciaEstimada.dc.html) | Distância vinda do app: **conhecida** ou **estimada** (com explicação); destinos sem distância conhecida marcados e depois dos conhecidos | conhecida, estimada, lista de destinos, pronto com distância estimada |
| [`SemCatalogo.dc.html`](canvas/SemCatalogo.dc.html) | Sem a pasta de instalação do jogo: nomes por id, regras do save | aviso com link para Configurações; saída, rota, carga, pronto, sem combinação |
| [`SemOrigem.dc.html`](canvas/SemOrigem.dc.html) | Nenhuma cidade com vaga livre nem oferta | save sem vagas nem ofertas (estado ocupa o módulo), busca sem resultado, só ofertas (vagas em uso) |
| [`MudancaPendente.dc.html`](canvas/MudancaPendente.dc.html) | Já há mudança pendente no slot | pendente na cidade (Editar / Descartar), editando (vaga livre), editando (substituição), descartada (Desfazer), nada livre |
| [`SteamCloud.dc.html`](canvas/SteamCloud.dc.html) | Perfil com Steam Cloud | aviso no módulo (dispensável), pronto, dispensado; chip "Steam Cloud" na barra de sessão |

Todas as telas funcionam em Claro/Escuro e pt-BR/en (botões na barra de prévia, que não faz parte do app). Documentos: [flows.md](flows.md) · [screens.md](screens.md) · [strings.md](strings.md).

## Decisões
1. **Ordem dos passos (pedido do usuário):** 1) cidade de saída, 2) cidade de destino, 3) empresa de saída, 4) carga, 5) empresa de chegada. As opções de cada passo são **filtradas pelos anteriores**: a empresa de saída só aparece se tem vaga livre **ou** oferta para substituir e envia ao menos uma carga que alguma empresa da cidade de destino aceita; a carga é o que essa empresa envia e a cidade de destino aceita; a empresa de chegada é a da cidade de destino que aceita a carga. Nunca se monta uma combinação inválida.
2. **Modo do frete, no passo da empresa de saída:** depois de escolher a empresa, aparece **Onde entra o frete** com duas opções: **Usar vaga livre** (slot vazio, como o código atual; padrão quando há vaga) ou **Substituir uma oferta** (escolhe uma das ofertas atuais da empresa no seletor **Oferta a substituir**; a revisão mostra antes → depois). Cada opção mostra a contagem (`Usar vaga livre · 3`). Estados: **sem vaga livre** (a opção livre fica desativada, o modo vai para Substituir e a dica diz "Substitua uma das ofertas atuais"), **sem ofertas** (Substituir desativado, dica "Use a vaga livre") e, se não sobra nada (vagas e ofertas todas em uso por pendências), a cidade mostra "Sem vaga livre nem oferta" e só as pendentes. Oferta única é escolhida sozinha.
3. **O usuário nunca digita distância nem monta rota.** O campo de km foi removido. A distância vem do app: **conhecida** (tabela de distâncias extraída de `shortest_distance_km` de todas as ofertas dos saves do usuário, em cache) ou **estimada** (marcada "Estimada", com "≈" e a explicação "É uma estimativa do app: seus saves não trazem esta rota. O jogo recalcula a rota no GPS."). No seletor de destino, as cidades **sem distância conhecida** ficam **depois** das conhecidas e levam a etiqueta "Estimada". A distância estimada é gravada como `shortest_distance_km`; o jogo a corrige ao recalcular a rota.
4. **Rota assim que as duas cidades estão escolhidas**, com a distância (conhecida ou estimada) visível já nesse passo (mapa esquemático + `StatTile` Distância). O mapa continua esquemático até haver coordenadas de cidades (premissa de `direcao-visual`); o texto "Esquema da rota, sem posições reais" deixa isso claro.
5. **A lista da esquerda é o passo 1** (cidades de saída com vaga livre, oferta substituível ou pendência, com nº de empresas, vagas, ofertas e o selo de pendentes). A cidade de destino é um `SearchablePicker` (centenas de cidades); os passos 3–5 são `SearchablePicker`s em linha, cada um desabilitado até o anterior existir, com dica de por quê.
6. **Trocar uma escolha limpa só o que deixou de valer** (regra completa em `screens.md` §3): as cidades são independentes entre si; empresa de saída (com o modo e a oferta), carga e empresa de chegada ficam se continuam válidas, senão são limpas, e um aviso (`role=status`) diz o quê. Quando sobra uma só opção ela é escolhida sozinha ("Única opção, já escolhida").
7. **Cidades iguais não existem**: no seletor de destino a cidade de saída aparece desativada ("Já é a cidade de saída"); escolher na lista da esquerda a cidade que já é o destino limpa o destino com um aviso. Há um botão para **trocar saída e destino**.
8. **Par sem nenhuma combinação**: mensagem "Nenhuma combinação possível" com o motivo e duas ações (trocar destino, trocar saída). O seletor de destino marca "Sem combinação" nas cidades sem opções a partir da saída escolhida e tem a chave "Só combinações possíveis" para escondê-las.
9. **Sem a pasta do jogo**: aviso `Info` (não bloqueia) com "Abrir Configurações"; nomes legíveis pelo id (`GameIds.Humanize`: "Concr beams2", "Scs flat b") e só as regras de carga que o save revela (menos opções).
10. **Mudança pendente para o slot**: ao escolher uma cidade de saída com pendências, um cartão lista cada mudança (Editar / Descartar; a linha mostra "Vaga livre" ou "Substitui oferta"). **Editar = reenfileirar com a mesma `Key`** (substitui, sem operação nova): por isso, na edição, **a cidade de saída, a empresa de saída e a vaga/oferta ficam fixas** ("Fixa na edição"; para mudar, descarte e crie outra) e se pode mudar destino, carga, empresa de chegada e prazo (o seletor de destino mostra só cidades que a empresa fixa atende). O botão vira "Atualizar na revisão". Descartar é imediato com Desfazer (Toast 6 s).
11. **Limites aprovados pelo usuário:** prazos **24 h, 48 h, 72 h e 7 d** (padrão 72 h) e nome do novo save de **1 a 40 caracteres**.
12. **Steam Cloud**: aviso `Warning` dispensável no módulo e chip "Steam Cloud" fixo na barra de sessão; o detalhe de como gravar fica na confirmação (`design/review-apply/`).
13. **Sem atalho próprio**: Ctrl+R (revisar) e Ctrl+Enter (só na página Revisar) continuam do shell. Esc fecha o seletor aberto.
14. **Nomes de cidade, empresa, carga e reboque vêm do jogo, traduzidos** (ADR 0006): nos mockups, "Sófia"/"Sofia", "Belgrado"/"Belgrade" etc. são só exemplo.

## Componentes
Reaproveitados do catálogo: `SearchablePicker`, `RouteMap` (modo esquemático), `StatTile`, `SegmentedChoice` (prazo), `ErrorBanner` (Info, Warning, Error), `EmptyState`, `PendingBadge`, `SearchBox`, botões `accent`/`neutral`/`ghost`, `Toast`. **NOVOS** (detalhes em `screens.md` §2): `CityListItem`, `StepBadge`, `InfoTag` (etiquetas “Estimada” e “Sem combinação”). O campo de km (`UnitField`) deixou de existir.

## Contratos que o desenho exige (para o `chief-architect`)
O plano 0006 define `JobBoard.Origins()/CargosFor(origin)/Destinations(origin, cargo)`, que supõe a ordem origem → carga → destino. A ordem pedida, o modo do frete e a distância do app precisam de:
```csharp
public sealed record DistanceInfo(int Km, bool IsEstimated);
public sealed record ExistingOffer(string SlotId, CompanyLocation Target, string Cargo, int DistanceKm, TimeSpan ExpiresIn);
public enum SlotMode { FreeSlot, ReplaceOffer }

public sealed class JobBoard
{
    IReadOnlyList<string> Cities();                                            // destino: todas as cidades do save
    IReadOnlyList<CityOffer> DepartureCities();                                // cidades com vaga livre ou oferta substituível (+ nº de empresas, vagas, ofertas)
    IReadOnlyList<CompanyLocation> DepartureCompanies(string fromCity, string toCity);   // com vaga livre ou oferta e ≥1 carga aceita em toCity
    IReadOnlyList<string> CargosFor(CompanyLocation from, string toCity);      // envia e toCity aceita; com modelo de reboque conhecido
    IReadOnlyList<CompanyLocation> ArrivalCompanies(string toCity, string cargo);
    bool HasMatch(string fromCity, string toCity);                             // "Sem combinação" no seletor de destino
    int FreeSlots(CompanyLocation company);                                    // vagas livres (o App desconta as pendentes)
    IReadOnlyList<ExistingOffer> Offers(CompanyLocation company);              // ofertas atuais substituíveis (o App desconta as pendentes)
    DistanceInfo Distance(string fromCity, string toCity);                     // NUNCA nulo: tabela conhecida ou estimativa
    JobTemplate? Template(string cargo);                                       // reboque, unidades
}
public sealed record JobRequest(CompanyLocation Origin, CompanyLocation Target, string Cargo, int DistanceKm,
    SlotMode Mode, string? SlotId = null, int Urgency = 0, int ExpirationHours = 72);   // SlotId: a oferta a substituir, ou o slot já usado ao editar
```
- **Tabela de distâncias** (`Data`, cache): extrai `shortest_distance_km` de **todas** as ofertas dos saves do usuário (todos os perfis e saves) para pares de cidades; invalida por data dos arquivos; enquanto não está pronta, vale a do save aberto. **Estimativa** para os pares ausentes: método a definir pelo `chief-architect` (o mockup usa linha reta × 1,25 só para ilustrar).
- **Editar uma pendente** = `IEditSession.Stage` com a **mesma `Key`** (`jobs:slot:<id>`): substitui a anterior, sem `Conflict`, e sem chamada nova. Por isso a edição fixa empresa e slot (o `JobPlanner` recebe `SlotId`).
- `JobPlanner.Plan`: modo `ReplaceOffer` gera `SetAttribute` com `OldValue` = valores atuais da oferta (antes → depois na revisão); `FreeSlot` tem `OldValue` vazio.
- `Jobs_Error_*` (10 chaves, `strings.md`) são o `Reason` das violações de `JobPlanner` (`OfferMissing` é novo; `DistanceInvalid` saiu).

## Perguntas em aberto
1. **API do `JobBoard` por cidade, com modo e distância** (bloco acima): é isso? A contagem de vagas e ofertas desconta as pendentes dentro do board, ou o `JobsPageViewModel` desconta usando as `Key`s de `IEditSession.Pending`? O desenho assume que o App desconta, que cada "Adicionar" em vaga livre usa o próximo slot livre da empresa e que "substituir" usa o `SlotId` escolhido.
2. ~~"Slot livre" × "substituir"~~ resolvido pelo modo: **vaga livre** (`target ""`, "Antes" = "—" na revisão) ou **substituir** (antes → depois, como no mockup de `Revisar`). Alinhar a skill `ets2-domain` e o `CLAUDE.md`, que só falam em substituir.
3. **Tabela de distâncias**: onde e quando extrair (ler todos os saves de todos os perfis é caro para saves grandes; sugestão: em segundo plano, com cache por arquivo e data, e a do save aberto primeiro). Como **estimar** os pares ausentes (grafo de distâncias conhecidas, coordenadas de `def/city` quando houver, outra)? O texto "Estimada" e o "≈" já cobrem a incerteza.
4. **Balsa e urgência**: o `JobInjector` grava `ferry_time/price = 0` e `urgency` 0. Rotas com balsa (Calais → Londres) ficam com balsa zerada, e a distância conhecida de uma oferta com balsa pode incluir o trecho de balsa. Expor "Balsa" e "Urgência" na v1? Hoje não aparecem.
5. **Prazo**: a unidade de `expiration_time` não está confirmada (risco 8 do plano); os quatro valores foram aprovados.
6. **Steam Cloud**: confirmar que `ProfileInfo.IsSteamCloud` é confiável o bastante para mostrar o aviso; o aviso do módulo é dispensado só nesta sessão (não persiste).
7. **Depois de "Adicionar"** o desenho mantém as duas cidades e limpa empresa, modo/oferta, carga e chegada (para encadear fretes entre as mesmas cidades). Aceito?
8. **Escolha automática de opção única** (empresa, oferta, carga, chegada): aceito ou o usuário prefere clicar sempre?
9. **Rota do mapa** sem coordenadas: aceitável como esquema, ou vale esperar a descoberta de `def/city` (ADR 0005)?
10. **Lista longa**: virtualização e busca no VM. O seletor de destino calcula "Sem combinação" e a distância (conhecida ou estimada) só para as linhas visíveis; com "Só combinações possíveis" ligado, para todas. Custo a validar com um save de mods grande.
11. **Substituir** troca todos os campos da oferta (destino, carga, reboque, unidades, prazo, distância). O desenho não oferece "manter o reboque da oferta antiga"; correto?
