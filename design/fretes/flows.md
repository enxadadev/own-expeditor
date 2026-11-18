# Fretes — fluxos

Parte do ciclo explorar → **editar** → revisar → aplicar. O módulo cuida de explorar e editar; a revisão e a gravação são do shell (`design/review-apply/`). Nomes entre crases: contratos do plano 0006 (`JobBoard`, `JobRequest`, `JobPlanner`, `ChangeSet`, `IEditSession`).

## 1. Fluxo principal: do primeiro passo à revisão

```mermaid
flowchart TD
    A([Módulo Fretes com sessão aberta]) --> B{Há cidade com vaga livre ou oferta?}
    B -- não --> B1[Estado: Nenhuma cidade com vaga ou oferta<br/>Escolher outro save]
    B -- sim --> C[Passo 1: escolher a cidade de saída<br/>lista à esquerda]
    C --> D[Passo 2: escolher a cidade de destino<br/>seletor com busca]
    D --> E{Cidades iguais?}
    E -- sim --> E1[Cidade de saída aparece desativada no seletor<br/>ou o destino é limpo com aviso]
    E1 --> D
    E -- não --> F[Rota no mapa + distância<br/>do app: conhecida ou estimada]
    F --> I{Alguma empresa de saída combina?<br/>vaga livre ou oferta + carga aceita no destino}
    I -- não --> I1[Nenhuma combinação possível<br/>trocar destino ou saída]
    I1 --> C
    I -- sim --> J[Passo 3: empresa de saída<br/>única opção = escolhida sozinha]
    J --> J2{Onde entra o frete?}
    J2 -- Usar vaga livre --> K
    J2 -- Substituir uma oferta --> J3[Escolher a oferta a substituir<br/>seletor com as ofertas atuais]
    J3 --> K[Passo 4: carga<br/>reboque e unidades aparecem]
    K --> L[Passo 5: empresa de chegada]
    L --> M{Tudo escolhido?}
    M -- não --> M1[Botão desabilitado<br/>Falta escolher: ...]
    M -- sim --> N[Adicionar à revisão]
    N --> O[JobPlanner.Plan → ChangeSet<br/>IEditSession.Stage]
    O -- ok --> P[Toast Frete adicionado à revisão<br/>selo Revisar n+1<br/>empresa, modo, carga e chegada limpas; cidades ficam]
    O -- CoreException InvalidEdit Jobs_Error_* --> Q[ErrorBanner no módulo<br/>nada foi enfileirado]
    Q --> J
    P --> C2([Outro frete, ou Ctrl+R → Revisar])
```

Notas:
- Cada passo só mostra opções que combinam com os anteriores; os erros `Jobs_Error_*` são a rede de segurança (a sessão mudou, outra mudança pendente tomou a vaga ou a oferta).
- Os passos 1 e 2 podem ser feitos em qualquer ordem; 3 a 5 são sempre depois de 1 e 2, na ordem.
- **O usuário nunca digita a distância**: ela vem do app (§3). Não há passo nem campo para isso.
- Nada é gravado no módulo; "Adicionar" só altera a sessão de edição (pendentes) e o selo da barra.

## 1b. Onde entra o frete (vaga livre ou substituir)

```mermaid
flowchart TD
    A([Empresa de saída escolhida]) --> B{Vagas livres e ofertas da empresa<br/>descontando as pendentes}
    B -- "vagas > 0 e ofertas > 0" --> C[Modo Usar vaga livre selecionado<br/>as duas opções habilitadas]
    B -- "vagas = 0, ofertas > 0" --> D[Usar vaga livre desativado<br/>modo Substituir selecionado<br/>dica: Substitua uma das ofertas atuais]
    B -- "vagas > 0, ofertas = 0" --> E[Substituir desativado<br/>dica: Esta empresa não tem ofertas para substituir]
    B -- "vagas = 0 e ofertas = 0" --> F[A empresa nem aparece na lista<br/>cidade: Sem vaga livre nem oferta, só pendentes]
    C --> G{Usuário troca para Substituir?}
    G -- sim --> H
    D --> H[Seletor Oferta a substituir<br/>destino, carga, km, tempo restante]
    H --> I{Só uma oferta?}
    I -- sim --> I1[Escolhida sozinha]
    I -- não --> I2[Usuário escolhe]
    I1 --> J([Segue para a carga])
    I2 --> J
    C --> J
    E --> J
```

- Vaga livre = slot de oferta vazio (o `ChangeSet` tem `Antes` vazio, "—"). Substituir = o slot da oferta escolhida (o `ChangeSet` tem `Antes` com os valores atuais; a revisão mostra antes → depois).
- Cada pendência usa um slot (uma `Key` por slot): um slot já usado por outra pendência não aparece como vaga nem como oferta.

## 2. Trocar uma escolha anterior

Regra: **cada escolha posterior fica se continua válida; senão é limpa** (e, se sobrar uma única opção, é escolhida sozinha). As cidades não limpam uma à outra, exceto quando ficariam iguais.

```mermaid
flowchart TD
    S([Usuário troca uma escolha]) --> T{Qual?}
    T -- cidade de saída --> A1[Destino: mantém, exceto se igual à nova saída<br/>→ limpa e avisa]
    T -- cidade de destino --> A2[Saída: mantém]
    T -- empresa de saída --> A3[Carga e chegada: revalidam]
    T -- carga --> A4[Chegada: revalida]
    T -- chegada --> A5[Nada depende dela]
    A1 --> V
    A2 --> V
    A3 --> V
    A4 --> V
    V[Revalidar em cadeia:<br/>empresa de saída → carga → chegada] --> W{Ainda válida?}
    W -- sim --> X[Mantém]
    W -- não --> Y[Limpa]
    Y --> Z{Sobrou só 1 opção?}
    Z -- sim --> Z1[Escolhe sozinha<br/>Única opção, já escolhida]
    Z -- não --> Z2[Fica vazia para escolher]
    X --> R[Aviso role=status:<br/>Limpamos ... / ... só restou uma opção]
    Z1 --> R
    Z2 --> R
    A5 --> R2[Sem aviso]
```

| Troca | Mantém | Revalida e pode limpar |
| --- | --- | --- |
| Cidade de saída | cidade de destino (se diferente) | empresa de saída (com modo e oferta), carga, empresa de chegada |
| Cidade de destino | cidade de saída | empresa de saída (com modo e oferta), carga, empresa de chegada |
| Empresa de saída | cidades | modo (volta ao padrão da nova empresa) e oferta, carga, empresa de chegada |
| Carga | cidades, empresa de saída, modo e oferta | empresa de chegada |
| Empresa de chegada | tudo | — |
| Trocar saída e destino (botão) | — | limpa os três (as regras são direcionais) |

A distância é sempre a do par de cidades (conhecida ou estimada) e se recalcula ao mudar uma delas; não há valor a limpar.

## 3. Estados do par de cidades e da distância

```mermaid
stateDiagram-v2
    [*] --> SemSaida
    SemSaida --> SoSaida: escolhe a cidade de saída
    SoSaida --> Rota: escolhe a cidade de destino
    Rota --> SemCombinacao: nenhuma empresa combina
    Rota --> Montando: há combinação
    SemCombinacao --> Rota: troca uma das cidades
    Montando --> Pronto: carga, chegada (e oferta, se substituir) escolhidas
    Pronto --> Montando: troca empresa, carga ou chegada
    Pronto --> SoSaida: destino limpo (cidade igual à saída)
    Rota --> SoSaida: destino limpo
    state Rota {
        [*] --> DistanciaConhecida: par na tabela de distâncias
        [*] --> DistanciaEstimada: par ausente
    }
```

Como o app obtém a distância (nunca é digitada):

```mermaid
flowchart LR
    A[Par de cidades] --> B{Está na tabela de distâncias conhecidas?}
    B -- sim --> C[Conhecida: km do save<br/>texto: vem de ofertas dos seus saves]
    B -- não --> D[Estimada: ≈ km + etiqueta Estimada<br/>texto: é uma estimativa; o jogo recalcula a rota no GPS]
    E[Saves do usuário<br/>shortest_distance_km de todas as ofertas] --> F[Tabela em cache na Data<br/>atualizada em segundo plano]
    F --> B
    C --> G[JobRequest.DistanceKm]
    D --> G
```

- Seletor de destino: cidades de distância conhecida primeiro (com "394 km"), depois as de distância estimada (com a etiqueta "Estimada" e "≈ 985 km"); a cidade de saída fica no topo, desativada; "Sem combinação" tem prioridade sobre "Estimada" na etiqueta.
- Mapa e `StatTile` mostram "≈" e "km · estimada" quando estimada; a revisão mostra a distância com o mesmo "≈" (`Format_KmApprox`).

## 4. Mudança pendente para o slot (editar / descartar)

```mermaid
flowchart TD
    A([Escolhe uma cidade de saída com pendências]) --> B[Cartão Mudanças pendentes nesta cidade<br/>uma linha por ChangeSet: vaga livre ou substitui oferta]
    B --> C{Ação}
    C -- Editar --> D[Carrega os 5 passos preenchidos<br/>faixa Editando uma mudança pendente<br/>cidade de saída, empresa e vaga/oferta FIXAS]
    D --> E{Usuário}
    E -- Atualizar na revisão --> F[IEditSession.Stage com a MESMA Key<br/>substitui a anterior, sem operação nova]
    F --> G[Toast Mudança atualizada na revisão]
    E -- Cancelar edição --> H[Volta ao formulário vazio<br/>a pendente continua]
    C -- Descartar --> I[IEditSession.Discard<br/>Toast Mudança descartada + Desfazer 6 s]
    I -- Desfazer --> J[Re-Stage do ChangeSet guardado]
    B --> K{Há vaga livre ou oferta na cidade?}
    K -- sim --> L[Pode adicionar outro frete<br/>usa a próxima vaga livre, ou outra oferta]
    K -- não --> M[Sem vaga livre nem oferta em cidade<br/>edite ou descarte uma pendente]
```

- Editar pode mudar **destino, carga, empresa de chegada e prazo**. O seletor de destino, na edição, mostra só as cidades que a empresa fixa atende. Para trocar a empresa, o modo ou a oferta/vaga, descarte a pendente e crie outra (a `Key` é por slot; reenfileirar com a mesma `Key` é o que substitui).
- A cidade continua na lista enquanto tiver vaga, oferta ou pendência; sem nada disso, sai da lista.

## 5. Estados do módulo

```mermaid
flowchart TD
    A([Entrou no módulo]) --> B{Tem JobBoard?}
    B -- indexando --> B1[LoadingPanel Atualizando as opções de frete…<br/>só após trocar a pasta do jogo; o índice normal vem da carga do save]
    B -- pronto --> C{Cidades com vaga livre, oferta ou pendência?}
    C -- nenhuma --> D[Estado vazio ocupa o módulo<br/>Escolher outro save]
    C -- algumas --> E{Pasta do jogo?}
    E -- sem pasta --> F[Aviso Info Sem a pasta do jogo<br/>nomes por id, regras do save]
    E -- com pasta --> G[Formulário]
    F --> G
    G --> H{Perfil com Steam Cloud?}
    H -- sim --> I[Aviso Warning dispensável + chip na barra]
    H -- não --> J[Sem aviso]
    I --> K([Montar frete: fluxo 1])
    J --> K
    G -- busca sem resultado --> L[Lista: Nenhuma cidade encontrada<br/>Limpar busca]
```

- **Trocar a pasta do jogo com sessão aberta** (Configurações): `IGameDataService.Changed` refaz o `JobBoard` (a tabela de distâncias não depende da pasta); as pendências ficam e o formulário revalida (fluxo 2) com as novas regras e nomes. Nomes e idioma mudam sem reler o save.
- **Trocar o idioma do app**: nomes do jogo são trocados na hora; as escolhas ficam.
- **Falha ao enfileirar**: `CoreException(InvalidEdit)` com `Jobs_Error_*` (ou `Conflict`/`StalePatch` do `Stage`) → `ErrorBanner` no módulo; as escolhas ficam para o usuário corrigir.

## 6. Steam Cloud e gravação

```mermaid
flowchart LR
    A[Perfil com IsSteamCloud] --> B[Módulo: aviso dispensável + chip na barra]
    A --> C[Confirmar gravação: aviso por destino]
    C --> D[Sobrescrever: a nuvem pode restaurar o save antigo]
    C --> E[Criar novo save: o jogo pode não mostrar o slot novo]
```

O módulo só informa; a escolha do destino da gravação é na confirmação (`design/review-apply/`).
