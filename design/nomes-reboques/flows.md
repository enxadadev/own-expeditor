# Nomes de reboque — fluxos

O nome é calculado localmente a partir do id (sem I/O, sem pasta do jogo), então não há estado de carregamento próprio: o nome nasce com o reboque do frete.

## 1. Resolução do nome (regra única, usada por todas as superfícies)

```mermaid
flowchart TD
    A[id da definição do reboque] --> B{vazio / sem reboque?}
    B -- sim --> Z["nome vazio: campo e diff mostram —; seletor e item omitem"]
    B -- não --> C["remover prefixo trailer_def.; separar por ponto: dono, restante"]
    C --> D["achar o token de cadeia no restante: single, singl, double, bdouble, hct (com _eixos)"]
    D --> E{existe Trailer_Finish_token para algum token, do último ao primeiro?}
    E -- sim --> F[base = nome do acabamento]
    E -- não --> G{existe Trailer_Body_token, do primeiro ao último?}
    G -- sim --> H[base = nome da carroceria]
    G -- não --> I["base = token da carroceria humanizado (mod); sem pontos: o id inteiro humanizado"]
    F --> J
    H --> J
    I --> J{cadeia conhecida e diferente de single?}
    J -- sim --> K["nome = base · cadeia  (Trailer_Name_WithChain)"]
    J -- não --> L[nome = base]
    K --> M["detalhes: eixos (todos ou nenhum) + comprimento (short/long) ou vazio"]
    L --> M
```

## 2. Ciclo do módulo (onde o nome aparece)

```mermaid
flowchart LR
    X[Explorar: lista de cidades] --> P[Escolher carga]
    P -->|"seletor: Carga · nome do reboque"| E[Editar: cartão Reboque com nome, detalhes, tooltip]
    E --> AR[Adicionar à revisão]
    AR --> R["Revisar: linha Reboque antes → depois (nome + detalhes)"]
    R --> AP[Aplicar: confirmar e gravar]
    O["Lista de ofertas (RouteOfferItem)"] -.->|"2ª linha: carga · nome"| E
```

O reboque **não é escolhido pelo usuário**: vem do frete de modelo da carga (`Jobs_Trailer_Help`). Por isso o nome aparece já no seletor de carga (antes de escolher) e se confirma no cartão.

## 3. Caminhos de erro e exceções

```mermaid
flowchart TD
    S[Mostrar reboque] --> T{id conhecido pelo vocabulário?}
    T -- "acabamento ou carroceria" --> N[nome do vocabulário]
    T -- "nenhum token conhecido (mod)" --> U[token humanizado, ex.: Cargo box]
    T -- "id vazio / frete sem modelo de reboque" --> V["— no campo; sem texto no seletor; só a carga no item; — no diff"]
    N --> W{detalhes}
    U --> W
    W -- "eixos ou comprimento reconhecidos" --> X1[linha de detalhes]
    W -- "token estranho (41, 3e_40, 10)" --> X2["ignora só o token estranho; sem detalhes se nada sobrar"]
    S --> Y{chave .resx ausente no idioma?}
    Y -- sim --> Y1["cai para o vocabulário neutro (en); nunca exibe !chave!"]
```

Regras de segurança do texto: nenhuma exceção chega à tela; nenhum caminho exibe `!chave!`, `trailer_def.` ou o id cru fora do tooltip.

---

## 4. Ícone do reboque — PROPOSTO

Decoração calculada **depois** da lista de opções. Mesma ordem de tokens do nome (fluxo 1), tabela em [`screens.md`](screens.md) §7.5.

### 4.1 Escolha do ícone

```mermaid
flowchart TD
    A[TrailerDescriptor: Tokens e Chain] --> B{definição e variante vazias?}
    B -- sim --> Z["sem ícone e sem selo: A, C e D omitem; B mostra chip vazio"]
    B -- não --> C["tokens = segmentos após o dono, sem o da cadeia, em minúsculas"]
    C --> D{"algum token na tabela de acabamento? (do último ao primeiro)"}
    D -- sim --> F[ícone do acabamento]
    D -- não --> E{"algum token na tabela de carroceria? (do primeiro ao último)"}
    E -- sim --> G[ícone da carroceria]
    E -- não --> H["IconTrailerGeneric (mod ou valor desconhecido)"]
    F --> I
    G --> I
    H --> I{"cadeia double, bdouble ou hct e tamanho 20 ou 24 px?"}
    I -- sim --> J["selo ×2 (double, bdouble) ou ×3 (hct)"]
    I -- não --> K[sem selo]
```

### 4.2 Onde entra no ciclo (e o que ele NÃO faz)

```mermaid
flowchart LR
    L["Lista de opções construída (cargas, ofertas) pelo save"] --> M["Para cada linha: nome (§1) e ícone (§7.5)"]
    M --> N["Seletor de carga: chip + nome"]
    M --> O["Cartão Reboque: ícone + nome + detalhes"]
    M --> P["Item de oferta: ícone 16 + carga · nome"]
    M --> Q["Revisão: ícone 16 de cada lado do diff"]
    N -. "o ícone não volta para a lista" .-> L
```

O sentido é único (lista → ícone). Nenhum caminho usa o ícone para filtrar, ordenar, agrupar, habilitar ou buscar (ADR 0003); reboque de mod desconhecido segue na lista com o ícone Genérico.

### 4.3 Casos de borda
| Caso | Resultado |
| --- | --- |
| Id de mod sem token conhecido | Genérico (sem selo se `single`) |
| Mod com cadeia dupla e token de carroceria conhecido (`acme.box.double_2_2.x`) | ícone de baú + selo ×2 |
| Token de acabamento conflita com a carroceria (`box` + `curtain`) | acabamento vence (igual ao nome) |
| Cadeia `singl` (grafia errada) | trata como `single`: sem selo |
| `hct` sem eixos legíveis | selo ×3 mesmo assim (vem da cadeia, não dos eixos) |
| Reboque sem definição, só variante (`trailer.scs_flat_b`) | tenta os tokens da variante; sem nada conhecido, Genérico |
| Frete sem modelo de reboque | sem ícone |
| Ícone pedido por chave que não existe nos recursos | `TrailerIcon` não desenha nada (nunca lança) |
