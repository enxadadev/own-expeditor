# Formato dos setores do mapa (ETS2/ATS)

Fase 0 da [ADR 0007](decisions/0007-mapa-do-jogo-com-rota-gps.md): descoberta
do formato antes de qualquer parser. Analisado em nov/2026 numa instalação real
do ETS2 com os 10 pacotes de DLC de mapa (`dlc_balkan_e`, `dlc_balkan_w`,
`dlc_balt`, `dlc_east`, `dlc_fr`, `dlc_greece`, `dlc_iberia`, `dlc_iceland`,
`dlc_it`, `dlc_north`) e `def.scs`. **Não havia `base.scs`** (o mapa base).
Tudo abaixo vale para esses pacotes; o que depende do `base.scs` está na seção 9.

Nada deste documento vem de código de produto: as medições foram feitas com
programas descartáveis, que usam o `HashFsArchive` e não ficam no repositório.
Dados extraídos do jogo (setores, coordenadas, listas de cidades) também não vão
para o repositório. Os exemplos aqui são só valores soltos.

Convenções: little-endian; `u8/u16/u32/u64` sem sinal, `i32` com sinal, `f32`
float de 32 bits. Offsets em hexadecimal.

## 1. Onde ficam os setores

- Dentro dos pacotes HashFS v2 (`.scs`), em `map/<nome-do-mapa>/`. Nos pacotes
  analisados o mapa se chama `europe` (`map/europe/`). Para o ATS espera-se
  `map/usa/` (sem amostra, não verificado).
- O índice dos mapas é `map/<nome>.mbd`, que **só existe no `base.scs`**. Aqui
  não pôde ser lido. O nome do mapa é o nome do arquivo sem a extensão. Os pacotes
  de DLC não têm `.mbd`: `List("map")` devolve só o diretório `europe`, sem
  arquivos. Para achar o mapa sem o `.mbd`, listar os subdiretórios de `map/`
  de cada pacote (é o que se fez aqui).
- Cada setor é um conjunto de 5 arquivos com o mesmo nome base:
  `sec<sx><sz>.{base,aux,data,desc,snd}`.
  - O nome tem sinal e 4 dígitos por eixo: `sec+0010+0006`, `sec-0001+0008`.
  - `sx` e `sz` são índices inteiros de uma grade de **4000 m**. O setor cobre
    `x ∈ [sx·4000, (sx+1)·4000)` e `z ∈ [sz·4000, (sz+1)·4000)`.
    Conferido: o nó de cada um dos 801 itens City cai no próprio setor.
- Pacotes com `map/` (todos com `features[]: map` no `*.manifest.sii`):

| Pacote | Tamanho do `.scs` | Setores | `relative_priority` |
|--------|-------------------|---------|---------------------|
| `dlc_balkan_e` | 191 MB | 70 | 69 |
| `dlc_balkan_w` | 438 MB | 42 | 86 |
| `dlc_balt` | 103 MB | 60 | 63 |
| `dlc_east` | 38 MB | 60 | 1 |
| `dlc_fr` | 95 MB | 76 | 47 |
| `dlc_greece` | 494 MB | 71 | 96 |
| `dlc_iberia` | 343 MB | 128 | 76 |
| `dlc_iceland` | 205 MB | 47 | 105 |
| `dlc_it` | 88 MB | 57 | 57 |
| `dlc_north` | 183 MB | 68 | 21 |
| `def.scs` | 28 MB | 0 | (sem `map/`) |

- Total: **679 setores** (3395 arquivos). Nenhum nome de setor se repete entre
  as DLCs, então não houve sobreposição para resolver. Sobreposição com o
  `base.scs` não pôde ser testada.
- `relative_priority` (do manifest) é candidato a ordem das camadas na fase 1b.
  Não foi validado.
- Tamanhos descompactados do conjunto (679 setores):

| Extensão | Total | Média por setor | Conteúdo |
|----------|-------|-----------------|----------|
| `.base` | 164,7 MB | 243 KB | itens do mapa e nós (o que interessa) |
| `.aux` | 1068,3 MB | 1,57 MB | itens visuais (terreno, prédios, modelos...) |
| `.data` | 293,0 MB | 432 KB | dados de renderização (não analisado) |
| `.snd` | 2,1 MB | 3 KB | sons (não analisado) |
| `.desc` | 27 KB | 40 bytes | caixa do setor (seção 3) |

  O maior `.base` tem 4,4 MB. O resto do mapa (`.aux`, `.data`) é 8 vezes maior
  e **não é necessário** para cidades, empresas, estradas e grafo.

## 2. Cabeçalho e versão

`.base`, `.aux` e `.data` começam com o mesmo cabeçalho de `0x14` bytes:

| Offset | Tipo | Campo | Valor visto |
|--------|------|-------|-------------|
| `0x00` | u32 | **versão do setor** | `907` (`0x38B`) nos 679 `.base`, `.aux` e `.data` |
| `0x04` | u32 | desconhecido, constante | `0x00750461` em todos os arquivos |
| `0x08` | u32 | desconhecido | `0` |
| `0x0C` | u32 | desconhecido, constante | `3` |
| `0x10` | u32 | **número de itens** | por setor |

- Os itens começam em `0x14` (seção 4). Depois dos itens vêm a tabela de nós e
  uma cauda (seção 5).
- A versão do setor é o número que decide o layout dos itens. O ts-map (seção
  10) lê versões de 825 a 907 e documenta mudanças em vários números (829, 831,
  834, 836, 846, 854, 855, 858, 875, 884, 891, 895, 896, 898, 900, 903, 906, 907).
  Este documento descreve **só a 907**.
- Os `.snd` têm cabeçalho igual mas versões menores (`0x387`, `0x385`) em alguns
  arquivos; alguns nem têm esse cabeçalho. Não são usados.
- `.desc`: ver seção 3.

## 3. Arquivo `.desc` (caixa do setor)

40 bytes, 10 `i32`:

| Índice | Campo | Observação |
|--------|-------|------------|
| 0 | versão | `3` |
| 1 | `minX` | metros × 256 |
| 2 | `minZ` | metros × 256 |
| 3 | `maxX` | metros × 256 |
| 4 | `maxZ` | metros × 256 |
| 5 a 9 | desconhecidos | `0`, dois pares de valores sem significado conhecido, `21` aparece |

A caixa é a extensão real dos itens do setor e passa um pouco dos 4000 m (ex.:
`minX = 15996` num setor que começa em 16000). Conferido nos 679: a caixa sempre
cruza a área do setor. Serve para saber os limites do mapa sem ler o `.base`.

## 4. Itens do `.base` (versão 907)

### Resposta à pergunta crítica da ADR

**Os itens não têm tamanho próprio.** Não há prefixo de tamanho nem tabela de
offsets: cada item começa com `u32 tipo` e o tamanho só se calcula conhecendo o
layout do tipo (parte fixa mais contadores que dão partes variáveis). Para
alcançar o 2º item é preciso medir o 1º; logo, **para achar as cidades o leitor
precisa saber medir todos os tipos que aparecem no `.base`**. Um tipo
desconhecido, ou um tipo conhecido com layout diferente, desalinha o setor
inteiro.

Isso é viável e foi validado: no `.base` da versão 907 aparecem **só 23 tipos**
(tabela abaixo). Com as regras da tabela, **os 679 `.base` fecham exatamente no
fim do arquivo** (itens + tabela de nós + cauda), com 348.373 itens e 488.167
nós. Esse fechamento é um teste de integridade forte: qualquer erro de tamanho
desalinharia os itens seguintes e o fim não bateria. Ele serve de verificação em
runtime (seção 8).

Os `.aux` têm mais tipos (ex.: `9` Mover, `11` NoWeather, `40` Compound), que
**não** estão neste documento. Como o `.base` basta, o `.aux` não deve ser lido.

### Cabeçalho comum do item (`0x34` bytes)

| Offset | Tipo | Campo |
|--------|------|-------|
| `0x00` | u32 | tipo do item |
| `0x04` | u64 | uid do item |
| `0x0C` | f32 | x (metros). É o **canto mínimo da caixa** do item; ver City |
| `0x10` | f32 | y (altura) |
| `0x14` | f32 | z (metros), idem |
| `0x18` | 28 bytes | desconhecido (parecem limites da caixa; não usados) |
| `0x34` | 5 bytes | **flags** (início dos campos específicos do tipo) |

Nas fórmulas abaixo `F` = offset do início dos flags = `início do item + 0x34`,
e o tamanho do item é `fim − início`. Dos 5 bytes de flags, o byte 0 bit 0 marca
"oculto" em City e Prefab (segundo o ts-map), o byte 1 é o DLC guard.

### Tabela de tipos (versão 907)

Tamanho medido nos 679 setores (mín / máx em bytes). "Fixo" = sem contador.

| Tipo | Nome (ts-map) | Quantidade | Tamanho | Regra (a partir de `F`) |
|------|---------------|-----------|---------|--------------------------|
| 1 | Terrain | 8.623 | 415 a 181.985 | `i32 n` em `F+5+0xEE`; `n·0x14` de esferas de vegetação; 2 blocos *Quad*; `0x20` |
| 2 | Building | 658 | 105 a 397 | `i32 n` em `F+5+0x2C`; `4·n` |
| 3 | Road | 151.097 | **269** (fixo) | fixo `0x34+0xD9` |
| 4 | Prefab | 45.934 | 123 a 1103 | ver seção 6.3 |
| 5 | Model | 2.150 | 121 a 153 | `i32 n` em `F+5+0x18`; `8·n`; `0x24` |
| 6 | Company | 1.202 | 105 a 429 | ver seção 6.2 |
| 7 | Service | 2.505 | 77 a 85 | `i32 n` em `F+5+0x10`; `8·n` |
| 8 | CutPlane | 9.683 | 77 a 229 | `i32 n` em `F+5`; `8·n` |
| 12 | **City** | 801 | **81** (fixo) | ver seção 6.1 |
| 18 | MapOverlay | 1.851 | **73** (fixo) | fixo `0x34+0x15` |
| 19 | Ferry | 61 | **93** (fixo) | fixo `0x34+0x29` |
| 22 | Garage | 285 | 89 a 97 | `i32 n` em `F+5+0x1C`; `8·n` |
| 34 | Trigger | 2.055 | 93 a 449 | variável (ações com textos); ver abaixo |
| 35 | FuelPump | 143 | 77 | `i32 n` em `F+5+0x10`; `8·n` |
| 36 | RoadSide (sign) | 86.981 | 98 a 657 | variável (placas com textos); ver abaixo |
| 37 | BusStop | 198 | **81** (fixo) | fixo `0x34+0x1D` |
| 38 | TrafficRule | 3.194 | 101 a 469 | `i32 tags` em `F+5`; `8·tags`; `i32 nós`; `8·nós`; `0x0C` |
| 39 | BezierPatch | 137 | 340 a 4180 | `i32 n` em `F+5+0xF1`; `n·0x14`; 1 bloco *Quad* |
| 41 | Trajectory | 20.010 | 89 a 565 | `i32 nós` em `F+5`; `8·nós`; `8`; `i32 regras`; `0x1C·regras`; `i32 pontos`; `0x10·pontos`; `i32 tags`; `8·tags` |
| 42 | MapArea | 5.234 | 89 a 537 | `i32 n` em `F+5`; `8·n`; `4` |
| 44 | Curve | 2.309 | 197 a 1501 | variável (máscara de subcurvas); ver abaixo |
| 46 | Cutscene | 401 | 177 a 1702 | variável (ações com textos); ver abaixo |
| 48 | VisibilityArea | 2.861 | 77 a 3669 | `i32 n` em `F+5+0x10`; `8·n` |

Notas para os tipos longos (todas as regras abaixo foram validadas pelo
fechamento no fim do arquivo):

- **Quad**: `u16 nMat`; `0x0A·nMat`; `u16 nCor`; `4·nCor`; 4 bytes (tamanho);
  `i32 nArm`; `4·nArm`; `i32 nDesloc`; `0x10·nDesloc`; `i32 nNormais`;
  `0x10·nNormais`.
- **Trigger** (`b` = `F+5`, depois dos 5 bytes de flags): `i32 tags` em `b`; `8·tags`;
  `i32 nós`; `8·nós`; `i32 ações`; para cada ação: `u64 uid` e `i32 sobrescritas`.
  Se `sobrescritas < 0`, a ação termina ali (12 bytes). Senão: `4·sobrescritas`,
  `i32 parâmetros`, por parâmetro `i32 len` + `4` + `len` bytes, e depois, **ainda
  dentro da ação**, `i32 tagsAlvo`, `8·tagsAlvo` e `8`. No fim do item, se
  `nós == 1`, mais `4`. (Conferido: o mínimo de 93 bytes é `nós = 1` com uma ação
  negativa. Uma versão anterior deste texto punha `tagsAlvo` depois do laço de
  ações e desalinhava 194 setores.)
- **RoadSide** (placa): `i8 placas` em `b+0x20`; `0x18·placas`; `i32 len` do modelo
  de sobrescrita. Se `len ≠ 0`: `4` bytes, `len` bytes (nome do modelo),
  `i32 nSobre` e, para cada uma, `8` + `u8 flags` + (`2` se bit 0) + (`8` se
  bit 1). Depois (com ou sem sobrescrita) `i32 nSinais`; por sinal: `0x0C`,
  `i32 nAtrib` e, por atributo, `i16 tipo` + `4`, mais um valor que depende do tipo
  (`5`: `i32 len` + `4` + `len`; `6`: `8`; `1`: `1`; outros: `4`). Os 8 bytes que o
  texto anterior colocava depois de `len` não existem: são só `4`, e só quando
  `len ≠ 0`.
- **Curve**: `i32 máscara` em `b+0x24`; as subcurvas começam em `b+0x28` (logo
  depois da máscara); para cada bit ligado da máscara: `0x4C`, `i32 n`, `4·n`,
  `0x14`. Conferido: o erro era contar 4 bytes extras entre a máscara e a primeira
  subcurva (a 1ª subcurva caía certa por compensação; as seguintes não).
- **Cutscene**: `i32 tags` em `F+5`; `8·tags`; `u64 nó`; `i32 ações`; por ação:
  `i32 nNum`, `4·nNum`, `i32 nTxt`, por texto `i32 len` + `4` + `len`; `i32 nAlvo`,
  `8·nAlvo`, `8`.

Esses tamanhos vieram do estudo do ts-map (seção 10) e foram reimplementados e
conferidos de forma independente contra os dados reais. O ts-map não decodifica
todos os campos (muitos blocos são só "pular"); onde o campo é só tamanho, a
semântica não foi investigada.

## 5. Tabela de nós e cauda

Depois do último item:

| Parte | Layout |
|-------|--------|
| Contagem | `i32 nós` |
| Cada nó (`0x38` bytes) | ver abaixo |
| Cauda | `i32 filhos` + `8·filhos` bytes de uids (versão ≥ 891) |

Nó:

| Offset | Tipo | Campo |
|--------|------|-------|
| `0x00` | u64 | uid do nó |
| `0x08` | i32 | x, em 1/256 m (dividir por 256 dá metros) |
| `0x0C` | i32 | y (altura), 1/256 m |
| `0x10` | i32 | z, 1/256 m |
| `0x14` | f32[4] | rotação (quaternion w, x, y, z); a guinada é `2·atan2(y, w)` |
| `0x24` | u64 | uid de um item ligado ao nó (0 = nenhum); "ligação A" |
| `0x2C` | u64 | uid de um item ligado ao nó; "ligação B" |
| `0x34` | u32 | flags do nó (ex.: `0x3000001` aparece nos nós de City) |

- Conferido: `x` e `z` vêm de `i32/256` (um `f32` direto daria posição errada).
  A guinada pela fórmula acima reproduz a caixa dos 801 City com erro de no
  máximo 0,23 m.
- Os nós de um setor guardam as posições dos itens que **começam** nele, mas um
  item pode apontar para um nó de **outro setor**: dos 1.202 Company, 11 apontam
  para nó fora do próprio setor. Todos os uids de nó de Road (302.194
  referências), Prefab (129.629) e Company (1.202) existem na união dos nós das
  DLCs. Para qualquer coisa além de City, o leitor precisa de uma tabela global
  de nós (uid → posição).
- `ligação A/B`: 225.689 e 470.466 dos 488.167 nós apontam para um item existente
  (Road, Prefab etc.). A semântica exata (frente/trás) não foi investigada.

## 6. Itens que interessam

### 6.1 City (tipo 12), tamanho fixo `0x51`

| Offset | Tipo | Campo |
|--------|------|-------|
| `0x00..0x33` | | cabeçalho comum |
| `F+0x00` | 5 bytes | flags; byte 0 bit 0 = "oculto" (ver abaixo) |
| `F+0x05` | u64 | **token da cidade** |
| `F+0x0D` | f32 | largura da área (m) |
| `F+0x11` | f32 | altura da área (m) |
| `F+0x15` | u64 | **uid do nó** (centro da área) |

- **Token**: base 38, até 12 caracteres `[0-9a-z_]`, primeiro caractere no
  dígito menos significativo (mesmo esquema do BSII; ver `sii-format.md`). O
  token **não** é o nome do arquivo em `def/city`: é o id dentro do arquivo
  (`city_data : city.<token>`). Exemplos em que diferem: arquivo
  `veliko_tarnovo.*.sui` tem id `veli_tarnovo`; `gdynia` tem `gdyne`. Chave de
  ligação com o save: o mesmo token que o `game.sii` usa (`city.<token>`).
  Conferido: **262 de 262** tokens achados nos setores existem como
  `city_data` em `def/city/*.sui` (`def.scs` + DLCs, 356 cidades ao todo).
- **Posição da cidade = posição do nó** (`F+0x15`), convertida de 1/256 m. O nó
  é o **centro** da área. O `x/z` do cabeçalho do item é o canto mínimo da caixa
  alinhada aos eixos do retângulo `largura × altura` girado pela guinada do nó:
  `x_item = x_nó − (L/2·|cos θ| + A/2·|sen θ|)` e `z_item = z_nó − (L/2·|sen θ| +
  A/2·|cos θ|)`. Conferido nos 801, com erro até 0,23 m. Usar o `x/z` do
  cabeçalho como centro erra até 1,3 km nas áreas giradas.
- **Várias áreas por cidade**: 801 itens para 262 tokens (média 3; uma cidade
  chegou a 10). Cada item é uma área (centro, bairros). Em **250 dos 262
  tokens há exatamente um item com o bit "oculto" desligado**: é a âncora que o
  ts-map usa para o nome da cidade, e a escolha natural para o centro. Nos 12
  restantes (cerca de 5% dos tokens; a lista não é publicada) todos os itens têm o bit ligado
  (o item principal provavelmente está no `base.scs`). Regra proposta: item
  visível; se não houver, a área de maior `largura × altura`.
- O nó do City sempre está no mesmo setor do item (801 de 801).

### 6.2 Company (tipo 6), versão ≥ 903

| Offset | Tipo | Campo |
|--------|------|-------|
| `F+0x00` | 5 bytes | flags (byte 1 = DLC guard) |
| `F+0x05` | u64 | token da **cidade** (`linked_city_name`) |
| `F+0x0D` | u64 | uid do **prefab** da empresa |
| `F+0x15` | u64 | **token da empresa** |
| `F+0x1D` | u64 | uid do **nó** da empresa |
| `F+0x25` | i32 | `n` nós extras |
| `F+0x29` | `n × 12` | cada um: `u64 uid` + `u32 flag` |

- Tamanho: `0x34 + 0x29 + 12·n`.
- Conferido: os **241** tokens de empresa achados existem em `def/company/`
  (`def.scs` + DLCs, 266 pastas) e os **259** tokens de cidade das empresas
  existem como id de `def/city`. Cada par (empresa, cidade) aparece uma vez
  (1.202 itens).
- Posição simples: posição do nó (`F+0x1D`, tabela global). Mediana de 0,8 km do
  centro da cidade, p90 2,6 km, máximo 7,9 km. O `x/z` do cabeçalho do item
  difere do nó em mediana 70 m e no máximo 596 m, então pode servir de
  contingência quando o nó está em outro setor. A posição exata do ícone usa o
  ponto de spawn `CompanyPos` do `.ppd` do prefab (fase 3b, não necessária para
  o mapa).
- Antes da versão 903 a ordem dos campos era outra (token da empresa antes do
  prefab). Não coberto aqui.

### 6.3 Prefab (tipo 4), versão ≥ 855

| Offset | Tipo | Campo |
|--------|------|-------|
| `F+0x00` | 5 bytes | flags (byte 0 bit 5 = secreto; byte 1 = DLC guard; byte 2 bit 1 = oculto) |
| `F+0x05` | u64 | token do modelo de prefab (`prefab.<id>` em `def/world/prefab*.sii`) |
| `F+0x0D` | u64 | variante/aparência |
| `F+0x15` | i32 | `p` peças adicionais, depois `8·p` bytes |
| | i32 | `n` nós, depois `n × u64` **uids dos nós ligados** |
| | i32 | `c` itens conectados, depois `8·c` bytes |
| | u64 + u8 | uid auxiliar; `origem` (índice do nó de origem) |
| | 1 byte | padding |
| | `n × 12` | aparência de cada nó |
| | 8 bytes | padding |

- Média de 2,82 nós por prefab; **todas as 129.629 referências resolvem** na
  tabela global de nós. Um prefab liga seus nós entre si de formas que só o
  `.ppd` (`prefab2/**/*.ppd`, versão `0x19` no que foi aberto) descreve: sem ele
  vale a aproximação "ligação total entre os nós" da fase 3a.

### 6.4 Road (tipo 3), versão ≥ 895, fixo `0x10D`

| Offset | Tipo | Campo |
|--------|------|-------|
| `F+0x00` | 9 bytes | flags (byte 2 bit 0 = secreto; byte 3 bit 1 = oculto; byte 6 = DLC guard) |
| `F+0x09` | u64 | token do **road look** (`road_look : road.<token>` em `def/world/road_look*.sii`) |
| `F+0x11` | `0xB4` bytes | parâmetros (não mapeados) |
| `F+0xC5` | u64 | uid do **nó inicial** |
| `F+0xCD` | u64 | uid do **nó final** |
| `F+0xD5` | 4 bytes | desconhecido |

- **Todos os 151.097 Road têm os dois nós na tabela global.** A estrada é uma
  curva entre os dois nós: o ts-map desenha uma spline de Hermite com tangente de
  módulo igual à distância entre os nós, na direção da rotação de cada nó
  (os `0xB4` bytes não são necessários para a geometria).
- A **classe** da estrada vem do road look. Das 264 looks usadas, 232
  resolvem em `def/world/road_look*.sii` (`def.scs` + DLCs; 334 definições no
  total). As 32 restantes (ex.: `blkw1`, `blkwhw1`) provavelmente estão no
  `base.scs`. O look declara `lanes_left[]`/`lanes_right[]` com nomes como
  `traffic_lane.road.motorway`, `.expressway`, `.local`, `traffic_lane.rail.*`
  e `traffic_lane.no_vehicles`: a classe (autoestrada/principal/local/trilho)
  pode sair daí. Questão da fase 2.
- Balsas: item Ferry (tipo 19, fixo `0x5D`): token do porto em `F+0x05`, nó em
  `F+0x15`. A ligação entre portos vem de `def/ferry/*.sui`. Fase 3.

## 7. Medições (instalação real, 10 DLCs, sem `base.scs`)

Máquina de desenvolvimento (Linux, SSD NVMe), .NET 8 Release, uma thread. O
programa abre os 10 pacotes, lista `map/europe`, lê **só os `.base`** (via
`HashFsArchive.Read`, que aloca o buffer comprimido e o descomprimido), mede
todos os itens, cola cidades e empresas e busca os nós das cidades.

| Medida | Valor |
|--------|-------|
| Setores lidos / com erro | 679 / 0 |
| Bytes de `.base` descompactados | 164,7 MB |
| Itens / nós | 348.373 / 488.167 |
| Cidades (itens) / tokens distintos | 801 / 262 |
| Empresas | 1.202 (241 tokens) |
| **Tempo total, só cidades e empresas** | **0,52 a 0,60 s** (cache frio: 0,60 s; quente: 0,52 s) |
| Memória (RSS de pico) | ~81 MB |
| Alocação total (churn) | ~257 MB |

- A leitura e a descompressão do zlib são ~90% do tempo (0,52 s medidos só para
  isso); a medição dos itens é desprezível. O cache frio foi obtido com `posix_fadvise(DONTNEED)` nos `.scs`,
  que pode não ser frio de verdade em disco rápido. Em disco lento (HD) espera-se
  mais.
- Para extrair e deixar em memória o grafo completo (nós de todos os setores num
  dicionário, com formatação de texto por nó) o mesmo programa levou 1,2 a 1,4
  s. Não é o caminho final, mas mostra que o grafo cabe em ~220 MB de RSS sem
  otimizar.
- Para comparar: ler também `.aux` (1068 MB) e `.data` (293 MB) levaria ~5 s no
  total (`.aux` 3,7 s). **Não ler nada além do `.base`.**
- `HashFsArchive.Read` aloca dois buffers por arquivo (comprimido e
  descomprimido), por isso o churn de ~257 MB para 165 MB lidos. Para a fase 1a
  vale um método que leia para buffers reaproveitados (`ArrayPool`).

### Coordenadas: sanidade

As coordenadas são x/z em metros do mundo (nó de City). O eixo z cresce para o
**sul**, então o mapa na tela usa `(x, −z)` (como diz a ADR).

Os valores de cidades individuais não vão para o repositório. Ficam só as
ordens de grandeza e a relação entre regiões:

- As cidades ficam a até ~100 km da origem em cada eixo (|x| e |z| abaixo de
  100.000 m, o limite da seção 8).
- O sul do mapa (Itália) fica bem abaixo da Escandinávia (z maior); a península
  ibérica fica a oeste, a Turquia no extremo leste e a Islândia no noroeste.
- Escala: a distância em linha reta entre centros × 19 (escala da ADR) ficou
  dentro de ~15% da distância real em pares de cidades conhecidas (pior caso:
  travessia curta de mar), coerente com a escala de ~1:19 da estrada. As
  coordenadas fazem sentido.

## 8. Como ler: estratégia e regras de integridade (para a fase 1a)

1. Achar os pacotes com `map/` (seção 1) e listar `map/<nome>/*.base`.
2. Para cada `.base`: ler o arquivo todo (máx. 4,4 MB), checar `versão == 907` e
   ler `u32 itens` em `0x10`.
3. Percorrer os itens a partir de `0x14` medindo cada um (seção 4). Guardar só
   City (12) e Company (6).
4. Chegar à tabela de nós (`i32 n` + `n·0x38`) e à cauda; **o fim tem de bater
   exatamente com o tamanho do arquivo**. Para cidades basta achar, na tabela, os
   uids dos nós das City do setor. Para empresas, um dicionário global (ou o
   `x/z` do cabeçalho como contingência).
5. Escolher o centro de cada cidade pela regra do item 6.1.

Quando considerar o setor ilegível (conta para o limite de 5% da ADR):

- versão diferente de 907;
- tipo de item desconhecido;
- tamanho de item que passa do fim do arquivo (ou negativo);
- tabela de nós ou cauda que não fecha exatamente no fim;
- City com nó fora da tabela do próprio setor.

Validações de sanidade do índice (a ADR pede que posição errada nunca seja
desenhada em silêncio): token de City decodificado só com `[0-9a-z_]`; nó dentro
do setor de origem; ao menos centenas de cidades; coordenadas dentro da caixa
esperada (aqui, |x| < 100.000 e |z| < 120.000).

### Implementação (fase 1a, `src/OwnExpeditor.Data/Maps`)

Medida com o `MapSource` real sobre os 10 pacotes de DLC + `def.scs` (sem `base.scs`;
Linux, SSD, .NET 8 Release, uma thread):

| Medida | Valor |
|--------|-------|
| Setores lidos / ilegíveis | 679 / 0 (status `Ready`) |
| Cidades (tokens) / empresas / países | 262 / 1.202 / 36 |
| Cidades com país, população e 8 deslocamentos de rótulo | 262 de 262 |
| Tempo da leitura completa (sem cache) | ~0,65 s |
| Alocação total | ~60 MB (era ~257 MB com `HashFsArchive.Read`) |
| Tamanho do índice em cache | ~58 KB |
| Segunda abertura (cache) | poucos ms |

- `SectorParser` mede os 23 tipos pelas regras da seção 4; `MapSectorReader` lista
  `map/*/*.base` (sem `.mbd`), lê um setor por vez e guarda só: a melhor área de
  cada token de City, as Company e uma tabela global de nós (16 bytes por nó); a
  posição das empresas vem dessa tabela, com o `x/z` do item como contingência.
- A posição da City sai da tabela de nós **do próprio setor** (se o nó não estiver
  lá, o setor é ilegível). `MapCompany.NodeId` fica `null`: o uid é de 64 bits e o
  contrato guarda `int?` (fase 3 decide).
- País e cidade de `def/country/*.sui` e `def/city/*.sui` (nome nativo, `pos`,
  `country`, `population`, `map_x_offsets[]`/`map_y_offsets[]` por nível de zoom)
  são lidos pelo mesmo sistema de arquivos em camadas. A chave da cidade é o id do
  bloco (`city_data : city.<id>`), não o nome do arquivo.
- `HashFsArchive.OpenPooled` devolve o arquivo num buffer do `ArrayPool` (um buffer
  comprimido e um descomprimido, devolvidos ao descartar) e o leitor usa
  `TryGetBuffer`, sem cópia.
- Camadas (`ScsFileSystem`): `base*.scs`, `def.scs`, `dlc_*.scs`, prioridade
  crescente nessa ordem (e ordinal dentro do grupo). A prioridade por
  `relative_priority` do manifest e os mods ficam para a fase 1b; hoje os mods só
  entram na chave do cache e voltam em `SkippedMods`.
- Cache (`MapIndexCache`): arquivo `<jogo>-<hash da chave>.idx`, magic `OEMI` +
  versão de esquema + chave completa + dados + marcador final; gravado em `.tmp` e
  movido; ficam os 3 mais recentes por jogo. Só resultados `Ready` são gravados
  (o status parcial não faz parte do arquivo). Chave: jogo, esquema do cache,
  versão do setor (907), detalhe e impressão (nome/tamanho/data) de `base*.scs`,
  `def.scs`, `dlc_*.scs` e dos mods, em ordem.
- Sem setores encontrados o leitor devolve `Failed`; pacote que não é HashFS v2 vira
  `UnsupportedVersion`; pasta do jogo ausente, `NoGameFolder`.

## 9. O que NÃO foi possível verificar (sem `base.scs`)

- Conteúdo do `.mbd` (só se sabe pelo ts-map que o nome do arquivo dá o nome do
  mapa).
- Versão de setor dos `base*.scs` (esperada 907 por serem do mesmo jogo, mas só
  a cópia da instalação confirma).
- Como o `base.scs` e as DLCs se sobrepõem (há setores iguais nos dois?). Se
  houver, a prioridade (`relative_priority`?) decide qual vale.
- Os 12 tokens de cidade sem item visível, as 32 road looks fora das DLCs e as
  cidades do jogo base (ex.: Paris, Milão, Berlim) só vão aparecer com o
  `base.scs`. No conjunto lido há 94 ids de `def/city` (de 356) sem item City nos setores;
  provavelmente são do base (ex.: `berlin`, `amsterdam`, `bern`, `aberdeen`).
- ATS: nenhum pacote foi analisado. `map/usa/` e o mesmo formato são hipótese.
- Mods (`map/<outro-nome>/`, zip, HashFS v1): fase 1b.
- Mapa de visão geral (textura) e ícones de mapa: não estão em `def.scs` nem nas DLCs (seção 12).

## 10. Referência e licença

- ts-map (github.com/dariowouters/ts-map), commit `ae8b4c0` (10/11/2026):
  licença **MIT**, © 2017 Dario Wouters. Usado só para entender o formato (tipos,
  ordem dos campos, versões). Nenhum código foi copiado: os tamanhos foram
  reescritos e conferidos contra os dados reais, e o fechamento no fim do arquivo
  detectou o único erro de transcrição (a regra de "sobrescrita negativa" do
  Trigger).
- HashFS v2: github.com/Wilps93/scs_tools (MIT), já citado em `HashFsArchive`.
- O ts-map só lê `.base` e dá suporte a versões até 907 (nesta data); ele não
  tem tratamento para itens `.aux`.

## 11. Riscos para a fase 1a

- **Atualização do jogo**: a versão do setor sobe e algum item muda de tamanho.
  O fechamento no fim do arquivo e a contagem de setores ilegíveis protegem; a
  tabela de tipos passa a ser por versão (um bloco de regras por versão
  suportada).
- **Tipo novo no `.base`**: hoje só 23 tipos; um tipo novo derruba o setor
  inteiro (não dá para pular). Mesmo tratamento.
- **Posição em setor vizinho**: Company pode apontar para nó de outro setor; no
  1a use o `x/z` do cabeçalho ou uma tabela global de nós (custa mais memória).
- **Mods e `base.scs`**: não testados aqui (seção 9). Rodar o mesmo programa de
  medição numa instalação com `base.scs` antes de fechar a fase 1a.
- **Desempenho**: ~0,6 s e ~80 MB para o conjunto lido; com o `base.scs`
  (provavelmente 2 a 3 vezes mais setores) e mods grandes (ProMods) pode passar
  de alguns segundos. O cache da ADR cobre; a leitura deve ser em segundo plano,
  cancelável e sem `.aux`.

## 12. Mapa de visão geral (textura de UI)

Pergunta: o mapa da tela de seleção de frete do jogo (fundo escuro, contornos e
nomes de países, pontos de cidade, rota com bandeiras, zoom e arrastar) pode ser
reproduzido lendo a **textura/arte de UI** da instalação, em vez de desenhar
estradas (ADR 0007)? Mesmas regras do resto do documento: medido com programas
descartáveis sobre `HashFsArchive.List/Walk`, sem código de produto e sem copiar
dados do jogo para o repositório. Mesma amostra da seção 1 (`def.scs` + 10 DLCs,
**sem `base.scs`**).

**Resultado curto:** nenhum dos 11 pacotes contém a textura do mapa de visão
geral, nem texturas de contorno de países, nem bandeiras de país. Os pacotes
só têm ícones de UI específicos de DLC. O que se confirmou é (a) como o jogo
parametriza esse mapa (`def/map_data.sii`), (b) de onde vêm nomes e posições
de países e cidades, e (c) que **toda textura do HashFS v2 usa GDeflate**,
formato que o `HashFsArchive` atual não lê.

### 12.1 O que existe nos pacotes

Os 11 pacotes têm 117.404 caminhos (fora os setores de `map/`). Todos os
diretórios de topo: `automat`, `custom`, `def`, `map`, `material`, `matlib`,
`model`, `model2`, `prefab2`, `road_template`, `umatlib`, `unit`, `vehicle`.
**Não existe `ui/`, `material/ui/map*`, `.mbd` nem arquivo `.dds` na listagem.**
Em `material/` só há 95 arquivos; os de UI são (por pacote, `.mat` + `.tobj`):

| Caminho | Qtd | Tamanho | O que é |
|---------|-----|---------|---------|
| `material/ui/dlc/dlc_<dlc>.{mat,tobj}` | 10 DLCs | 350x130, BGRX8 sRGB | logo da DLC |
| `material/ui/company/small/<token>.{mat,tobj}` | 10 (só `dlc_balkan_e`) | 128x32, BGRA8 sRGB, 1 mip | logo pequeno de empresa (o ícone que o mapa usa) |
| `material/ui/cargo_icons/<carga>.{mat,tobj}` | 9 (só `dlc_greece`) | 122x96, BGRA8 sRGB | ícone de carga |
| `material/ui/accessory`, `material/ui/packages` | 2 + 2 (`dlc_fr`) | | pintura e pacote da DLC |

- Um `.mat` de UI é texto SII curto (zlib): `effect : "ui.rfx" { texture :
  "texture" { source : "<nome>.tobj" u_address : clamp v_address : clamp
  mip_filter : none } }`. O `.mat` só nomeia a textura; não tem origem, escala
  nem coordenadas de mundo.
- Os ícones **de mapa** (`border_ico`, `toll_ico`, `weigh_ico`, `quarry`,
  logos de cidade de `def/world/overlay.sii`) são só **nomes**; as texturas
  não estão nestes pacotes (presumivelmente no `base.scs`).
- Nenhum pacote analisado tem `def/ui*.sii` apontando para uma textura de mapa:
  `def/ui.sii` só tem `ref_res_x/y` (1440x900). A arte de UI do jogo (o
  layout das telas) não está em `def.scs` nem nas DLCs.

### 12.2 Texturas no HashFS v2 (vale para qualquer textura, inclusive a do mapa)

- **Não há `.dds` no pacote.** A entrada com extensão `.tobj` **é** a textura:
  no v2 ela tem 3 partes de metadados (`tipo 1` imagem, `tipo 2` sampler,
  `tipo 4` dados). Dos 6.049 `.tobj` dos 11 pacotes, **todos** têm exatamente
  essas 3 partes e compressão `0x30`. (No `HashFsArchive`, `compression =
  byte3 & 0xF0` do descritor de dados; arquivos normais são `0x00` ou `0x10`
  = zlib.) Um `.dds` e um `.tobj` v1 teriam de ser **reconstruídos** a partir
  disso.
- Para as 6.049 texturas: 769 MB comprimidos, 1.791 MB descomprimidos. A maior
  tem 5,6 MB descomprimida (2048x2048, 12 mips). Texturas maiores que 16 MB
  não apareceram aqui: o tamanho descomprimido do descritor é de 24 bits
  (`usize`), então uma textura grande de mapa precisaria de outro arranjo
  (não verificado).
- **Compressão `0x30` = GDeflate** (DirectStorage). Os primeiros bytes do
  fluxo são `04 FB nn nn`: `id=4`, `~id=0xFB`, `u16 nTiles`, depois um `u32` de
  flags e uma tabela de `nTiles` offsets `u32`. Conferido: `nTiles =
  ceil(usize / 65536)` nos casos medidos (199.544 B -> 4 tiles; 352.784 B -> 6).
  `HashFsArchive.Read` hoje lança `NotSupportedException("Compressão 0x30")`
  para todos eles, o que já está dito no resumo da classe.
- **Metadado de imagem** (parte `tipo 1`, 8 bytes, little-endian):

| Bytes | Campo | Conferência |
|-------|-------|-------------|
| `0..1` (u16) | largura − 1 | 349 -> 350 |
| `2..3` (u16) | altura − 1 | 129 -> 130 |
| `4..7` (u32) bits `0..3` | número de mips − 1 | `0x…5B0` = 1 mip; `0x…4E9` = 10 mips (512x256) |
| `4..7` bits `4..11` | formato **DXGI** | `0x5B` = 91, `0x5D` = 93, `0x4E` = 78 |
| `4..7` bits `12+` | alinhamentos/faces (`0x0980` no alto, constante) | não decodificado |

  Formatos dos 6.049 `.tobj`: BC1 sRGB (72) 1.877; BC3 (77) 1.108; BC3 sRGB
  (78) 1.046; BC5 (83) 762; BC4 (80) 621; BC1 (71) 603; BGRA8 sRGB (91) 21;
  BGRX8 sRGB (93) 10; BC2 (75) 1. **Sem BC7 nesta amostra.**
- **Layout descomprimido.** Em BGRA8 com 1 mip, as linhas têm passo alinhado a
  **256 bytes** e a última linha não tem preenchimento: `(h−1)·ceil256(w·4) +
  w·4`. Conferido nos 3 tamanhos (128x32 -> 16.384; 122x96 -> 49.128;
  350x130 -> 199.544). Para BC com cadeia de mips a soma não fechou (512x256
  BC3, 10 mips: medido 177.168 B; o passo de 256 B por mip dá 176.640), então
  o layout dos mips **não está decifrado**. É preciso resolvê-lo antes de
  decodificar texturas com mips (a spec do `scs_tools`, já citada, deve
  cobrir).

### 12.3 Parâmetros do mapa de UI: `def/map_data.sii` (em `def.scs`)

É o único arquivo que descreve o mapa de visão geral. **Não há textura ali**:
ele só dá cores, zooms e limites. Pistas importantes:

- **O mapa é desenhado em tempo de execução** com os dados dos setores, com
  cores por tipo de tela: comentários do arquivo falam em "Job market map
  undiscovered route" (`job_road_color: 0x50353535`, cinza escuro **translúcido**),
  "Main map" (`world_road_color: 0xFF707070`), "Advisor map" e "Fleet
  manager map". O aspecto "escuro" da tela de fretes é, portanto, fundo +
  estrada quase invisível. Cores `0xAARRGGBB`.
- **8 níveis de zoom (0..7).** 0-1 = mapa 3D, 2-3 = minimapa, **4-7 = mapa
  do mundo** (4 detalhe de cidade, 5 mais próximo, 6 médio, 7 mundo todo).
  `zoom_uplift[0..7]`: 50, 220, 900, 4000, 1000, 5000, 18500, 45000 (unidade
  não confirmada, provavelmente altura da câmera em metros). Por nível
  existem tamanho do jogador, fator de largura de estrada
  (`road_size_factor`: 1, 1, 1.5, 3, 1, 1.5, 4.5, 8), `full_detail`,
  `icon_size`, `city_icon_size`, `company_icon_size` (empresa some nos
  níveis 2 e 3) e `map_items_flags` (máscara dos itens desenhados, ex.:
  `0xC481402` no nível 7 = destino, cidades, reboque e veículos).
- **Limites de câmera, nível 4**: min `(-93000, -140000)`, max `(77000, 86000)`
  em metros de mundo (x, z). **Conferido**: a caixa das 801 City medidas
  (seção 7: x −92.710..77.442; z −112.499..85.680) cabe nesse retângulo, e o eixo z
  positivo para baixo (sul) bate com o "(right, down)" do comentário. Servem
  como extensão do fundo sem ler setores (folga de ~28 km ao norte).
  Nível 7: min `(-82000, -122000)`, max `(51000, 69000)` (câmera mais
  restrita porque a janela é maior).
- `ui_map_center: (-13500.0, -4950.0)` ("in editor coordinates") e
  `ui_map_size: (5308.2, 5200.0)` ("map texture in real km"),
  `ui_map_camera_min/max` = os limites do nível 7. Indicam que **existe** uma
  textura de mapa cuja extensão é dada em km reais, mas **não se conseguiu
  amarrar** esses valores ao mundo em metros: 5308 km reais / 5200 km reais
  não bate com uma única escala sobre 133 km x 191 km de jogo (razões 25 e
  37 m/km), e o centro z (−4950) está longe do centro da câmera (−26500).
  Hipótese: a textura está em coordenadas de editor/real, não em metros de
  mundo. Sem a textura, a relação entre pixel e metro **não é verificável**.
- `ui_map_always_draw_city: true`; tamanho do pino de cidade por população:
  `city_pin_75_pop: 0`, `city_pin_100_pop: 200000`, `city_pin_150_pop:
  8000000` (75/100/150 % de tamanho).
- Cores de áreas (`map_area_color[]`: Road, Light, Dark, Green) e do trajeto
  de navegação (`navigation_color 0xFF07077C`, `navigation_current_color
  0xFF0C42DF`), contorno `0xFF000000`. A cor da rota **vermelha** da tela de
  fretes não está nesse arquivo (é arte/UI).

### 12.4 Países, cidades e ícones

- **Países**: `def/country/<nome>.sui` (36 em `def.scs`; as DLCs só acrescentam
  `traffic.<dlc>.sii`). Campos: `country_id`, `name` (nome nativo, ex.
  "Italia"), `name_localized: "@@italy@@"` (chave do `locale.scs`, ausente
  nesta amostra; a Data já lê `locale.scs`), `country_code`,
  `iso_country_code`, **`pos: (x, 0, z)`**, combustível, fuso e limites de
  massa. `pos` está em **metros de mundo** (mesmo sistema do nó das City;
  ex.: Itália `(-4210, 0, 31600)`, Suécia `(15500, 0, -51000)`, Estônia
  `(48000, 0, -47500)`, e cada um cai na região certa em relação às
  cidades da seção 7). É um **ponto de âncora do rótulo** do país (um ponto
  por país). **Não há polígono, contorno nem fronteira** em nenhum
  `def/country*`. O arquivo antigo `custom/country.sii` é só compatibilidade.
- **Cidades**: além do que a seção 6.1 já usa, cada `def/city/*.sui` tem
  `country: <id do país>`, `population` (um arquivo sem) e **8 deslocamentos
  de rótulo**, `map_x_offsets[]` e `map_y_offsets[]` (um valor por nível de
  zoom 0..7; inteiros pequenos como `0`, `-3`, `-44`; unidade provavelmente
  pixels, não confirmada). Os 147 `.sui` de cidade lidos (def + 3 DLCs) têm
  os dois vetores. Isso dá nome, pino por população e posição do texto por
  zoom, sem textura.
- **Bandeiras de saída/chegada**: o que se descreve (bandeira de partida e de
  chegada na rota) é ícone de UI/mapa; não há arquivo equivalente aqui.
  Pode-se desenhar como vetor próprio (sem asset). **Bandeiras de país** não
  existem como textura nos pacotes (só como modelos de bandeira 3D de
  `model2/mover/flags/`, que não servem à UI).

### 12.5 O que dá para confirmar só com as DLCs e o que exige o `base.scs`

Confirmado aqui: tudo da seção 12.1 a 12.4 (inclusive a ausência da textura
nestes pacotes), o formato de textura do HashFS v2, os parâmetros de zoom e
cores, e as posições dos países.

**Exige `base.scs`** (e provavelmente `base_share`/`core`/`locale`, que o
dataset também não tem):

- A textura do mapa do mundo, ou o que a substitui: formato, resolução (uma
  ou várias peças), nome do arquivo e relação com `ui_map_center/size`.
- Texturas dos ícones de mapa (`overlay.sii`), pinos de cidade e bandeiras
  de rota.
- Se o contorno dos países vem de textura ou de dados (nenhum dado de
  contorno existe em `def.scs` ou nas DLCs).
- Qual é a ordem/identidade do mapa base e se o `.mbd` cita a textura.
- Se a textura cobre as DLCs (cada DLC amplia o mapa; a textura do jogo base
  poderia não cobrir Ibéria, Itália etc.) e o que o jogo faz com mods.

**ATS**: nada foi analisado. `country` no ATS é "estado" (campo e arquivos
diferentes) e a mesma pergunta se repete para `base.scs` do ATS.

### 12.6 Decodificar a textura em runtime

Só vale se a textura do mapa existir. Cadeia necessária e custo:

1. **GDeflate (CPU).** Não existe em .NET nem no `System.IO.Compression`.
   Tem de ser implementado em C# ou chamado por biblioteca nativa
   (a referência é o código do DirectStorage da Microsoft e o
   `libdeflate-gdeflate`; **licenças a conferir** antes de qualquer porte). O
   decodificador é o item mais arriscado: ~300 a 500 linhas, tiles de 64 KB
   com fluxo Huffman entrelaçado em 32 faixas. Dá para validar com as 6.049
   texturas aqui (tamanho final, `nTiles`, imagem sensata nos logos). Estimativa
   (não medida): dezenas a poucas centenas de MB/s por thread; nada que
   incomode para algumas texturas.
2. **Montar a imagem**: layout dos mips (12.2) e escolher o mip certo; só o mip
   0 interessa para um fundo estático.
3. **BC1/2/3/4/5 para BGRA8**: simples e sem dependência (~60 a 150 linhas;
   por bloco 4x4). BC7 (não visto nesta amostra, pode aparecer no `base`) tem
   ~250 linhas. Estimativa: 4096x4096 (16 M pixels) em ~0,2 a 0,6 s numa
   thread, paralelizável por faixas de blocos.
4. **Avalonia/Skia não leem isso.** O `Bitmap` decodifica PNG/JPEG/BMP/WebP/GIF;
   não há DDS nem BCn. Só dá para entregar pixels já em BGRA8/RGBA8
   (`WriteableBitmap`). O app precisa do decodificador próprio.
5. **Memória**: BGRA8 sem mips = 16 MB (2048²), 64 MB (4096²), 256 MB (8192²).
   Vale reduzir (mip) para a tela e guardar em cache **PNG derivado no perfil
   local** (`LocalApplicationData`), não o DDS.
6. **Risco**: GDeflate, layout de mips e limite de 24 bits do tamanho são
   três pontos que só se resolvem com a textura real do `base.scs`.

### 12.7 Alternativa vetorial (se a textura não for utilizável)

Tudo vem da instalação, só leitura, sem texturas:

- **Fundo**: cor sólida escura; extensão pelo `camera_limits` do nível 4
  (12.3) ou pela caixa das City.
- **Cidades**: setores (seção 6.1) + `population` e `map_*_offsets` do
  `def/city`. Pino e rótulo por zoom, como o jogo.
- **Nomes de países**: `def/country/*.sui` (`pos`, `name`/locale). Posição
  de rótulo exata e gratuita.
- **Estradas esmaecidas**: fase 2 da ADR, com `job_road_color` (`0x50353535`).
- **Contorno de países**: **não há dado** nos pacotes. Aproximações viáveis:
  (a) atribuir cada cidade (`country` do `.sui`) e cada empresa a um país e
  desenhar o casco/área suavizada (alpha shape) de cidades + estradas
  de cada país, com preenchimento tênue; (b) máscara de terra pelos setores
  existentes (`.desc`, 40 bytes cada, grade de 4 km; custa quase nada);
  (c) pontos de fronteira pelos prefabs de `border_crossing` (593 arquivos em
  `prefab2/border_crossing`, 115 em `model2/border_crossing`) e os overlays `border_ico`.
  Nenhuma das três reproduz a fronteira real; (a) + (b) dão uma silhueta
  aceitável, não "igual ao jogo".
- **Bandeiras** de saída/chegada, setas e botões de zoom: vetor próprio do
  app (Design System).

### 12.8 Direitos (SCS)

Mesma decisão da ADR 0007: **só leitura em runtime** da instalação do usuário.
Nada do jogo (texturas, PNG exportados, tabelas de contorno, posições
extraídas) vai para o repositório, os testes, o instalador ou a release. Cache
derivado (índice, PNG reduzido) só em `LocalApplicationData/OwnExpeditor/cache/`
do usuário, descartável, recriado se a instalação mudar (mesma chave da ADR).
Textura do jogo exibida na tela do app é reprodução de arte da SCS: ainda que
só local, a **revisão dos termos de uso da SCS** da ADR passa a incluir isso;
o desenho vetorial não tem esse risco. As fixtures dos testes continuam
sintéticas (para GDeflate/BCn, blocos pequenos montados à mão).

### 12.9 Recomendação e o que falta decidir

- **Recomendação: vetor.** Fundo escuro + rótulos de país (`pos`) + pinos de
  cidade (população e offsets por zoom) + estradas esmaecidas + rota e
  bandeiras como vetor próprio. Reproduz o aspecto do mapa de fretes com
  dados que **já estão nestes pacotes e são verificáveis**, acompanha DLC e
  mods (o que a ADR já exigia), não depende de GDeflate e não carrega arte
  da SCS na tela. A diferença para o jogo será o **contorno dos países**.
- **Textura continua possível**, mas hoje é indecidível: a textura (se existir)
  só está no `base.scs`, e antes de decodificá-la é preciso GDeflate + layout
  de mips + extensão em metros (`ui_map_*` sem amarração confirmada). Também não
  cobriria mods como ProMods nem DLCs lançadas depois.
- **Falta para decidir** (todos exigem o `base.scs`, e o do ATS para o ATS):
  1. Listar `base.scs` (e o que mais exista fora de `def`/`map`) e achar a
     textura do mapa de UI e dos ícones; anotar tamanho, formato, peças.
  2. Medir se o jogo desenha contorno a partir de textura ou de dado.
  3. Fixar a relação entre `ui_map_center/size` e metros de mundo.
  4. Se (1) e (2) apontarem textura, medir o GDeflate (implementação em C#
     validada com as texturas desta amostra, e tempo real).
