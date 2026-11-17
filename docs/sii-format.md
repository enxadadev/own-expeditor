# Formato do save (game.sii)

Analisado com um save real do ETS2 (perfil local, slot 4, nov/2026). Os valores
de exemplo vêm desse save.

## 1. Camadas do arquivo

O `game.sii` pode chegar em três formatos. Os 4 primeiros bytes dizem qual:

| Assinatura | Formato |
|------------|---------|
| `ScsC` | criptografado (padrão do jogo; foi o caso deste save) |
| `BSII` | binário (o jogo usa quando `g_save_format "0"`/padrão, dentro do ScsC) |
| `SiiNunit` | texto puro (`g_save_format "2"`) |
| `3nK` (0x014B6E33) | texto ofuscado com XOR (usado em alguns arquivos do jogo) |

O `SiiDecoder` ainda não lê `BSII` nem `3nK`: ambos lançam
`NotSupportedException` (ver seção 6).

### ScsC (criptografado)

Cabeçalho de 56 bytes, little-endian:

| Offset | Tamanho | Campo |
|--------|---------|-------|
| 0 | 4 | assinatura `ScsC` |
| 4 | 32 | HMAC (não é necessário para ler) |
| 36 | 16 | IV do AES |
| 52 | 4 | tamanho descompactado |

Depois do cabeçalho: **AES-256-CBC** com chave fixa e o IV do cabeçalho, e em
seguida **zlib**. O resultado é `SiiNunit` (texto) ou `BSII` (binário).

Chave AES (pública, a mesma usada pelo SII_Decrypt; referência:
github.com/liam-dong/SII-Decrypt-cpp, `src/core/sii_format.cpp`, MIT):

```
2a 5f cb 17 91 d2 2f b6 02 45 b3 d8 36 9e d0 b2
c2 73 71 56 3f bf 1f 3c 9e df 6b 11 82 5a 5d 0a
```

Verificado: o slot 4 (815 KB) virou 8,1 MB de texto `SiiNunit`, e o tamanho
bateu com o do cabeçalho.

O jogo aceita carregar o save em texto puro, então gravamos em `SiiNunit` sem
precisar recriptografar. (A confirmar no jogo.)

## 2. Estrutura do texto

```
SiiNunit
{
<tipo> : <id> {
 <atributo>: <valor>
 <array>: <n>
 <array>[0]: <valor>
}
...
}
```

- IDs nomeados: `company.volatile.<empresa>.<cidade>`.
- IDs anônimos: `_nameless.2da.b339.b508` (hex separado por pontos).
- `null` = referência vazia; `nil` = valor numérico vazio; `&3f000000` = float
  em hex (IEEE 754).

Contagem de blocos no save de exemplo: 1941 `company`, 6485 `job_offer_data`,
2390 `line_offer_data`, 9020 `economy_event`.

## 3. Campos que importam para injetar jobs

### Tempo do jogo

No bloco `economy`: `game_time: 10915` (minutos de jogo).

### company

```
company : company.volatile.wilnet_trans.lemans {
 permanent_data: company.permanent.wilnet_trans
 job_offer: 7
 job_offer[0]: _nameless.2da.b339.b508
 ...
 cargo_offer_seeds: 10
 cargo_offer_seeds[0]: 11050
 ...
 discovered: false
 state: 0
}
```

- A origem do job é a própria empresa: `<empresa>.<cidade>` vem do ID.
- `job_offer[n]` aponta para blocos `job_offer_data`.

### job_offer_data

Job ativo:
```
job_offer_data : _nameless.2d8.21c9.d5d8 {
 target: "lognstick.istanbul"          # destino: "<empresa>.<cidade>"
 expiration_time: 11649                 # game_time em que a oferta expira
 urgency: 0                             # 0 padrão, 1 importante, 2 urgente
 shortest_distance_km: 569              # usado para pagamento e tempo
 ferry_time: 0
 ferry_price: 0
 cargo: cargo.concr_beams2
 company_truck: mercedes_actros_2014_4x2_c   # caminhão do frete rápido
 trailer_variant: trailer.scs_flat_b
 trailer_definition: trailer_def.scs.flatbed.single_3.flatbed_w
 units_count: 2
 fill_ratio: 1
 trailer_place: 0
}
```

Slot vazio (1406 no save de exemplo): `target: ""`, `cargo: null`,
`expiration_time: nil`. Esses slots são os candidatos naturais para injetar
jobs sem apagar ofertas existentes.

## 4. De onde vêm as listas (sem listas fixas)

| Lista | Fonte no save |
|-------|---------------|
| Empresas/cidades de origem | IDs `company.volatile.*` |
| Destinos | os mesmos IDs, no formato `target` |
| Cargas | valores `cargo:` em uso (301 distintas no exemplo) |
| Reboques | pares `trailer_variant`/`trailer_definition` em uso (54 definições) |
| Distância | `shortest_distance_km` de jobs já existentes com o mesmo par origem/destino |

Limitação: só aparecem cargas e reboques que o jogo já sorteou em alguma
oferta. A lista completa está nos arquivos `def/` do jogo (`.scs`, HashFS v2).
Ler esses arquivos fica para uma fase posterior.

## 5. Em aberto

- Se o jogo valida a combinação carga + reboque + empresa (ex.: carga que a
  empresa de origem não produz).
- Como calcular a distância quando o par origem/destino não existe no save.
- Se `info.sii` precisa ser atualizado junto.
- Se o jogo regenera as ofertas ao carregar quando `expiration_time` é menor
  que `game_time`.
- Se o jogo carrega um `game.sii` em texto puro gravado pelo app (ver
  [ADR 0002](decisions/0002-gravar-save-em-texto.md)).

## 6. Como o código lê e grava (`src/OwnExpeditor.Sii`)

Esta seção registra o comportamento do parser e do writer atuais. Ela não
descreve o jogo.

- **Detecção**: `DetectFormat` reconhece `SiiNunit`, `SiiNunit` precedido de
  BOM UTF-8, `ScsC`, `BSII` e `3nK`. Qualquer outro início vira "não
  reconhecido".
- **ScsC**: `Decrypt` exige pelo menos 56 bytes e usa o IV no offset 36 e o
  tamanho no offset 52. O HMAC (offsets 4 a 35) não é verificado. A saída
  descomprimida precisa ter exatamente o tamanho do cabeçalho.
- **Texto**: o parser ignora linhas em branco e faz `Trim()` em cada linha. Por
  isso espaços no início ou no fim de um valor se perdem.
- **Atributo**: é separado pelo primeiro `": "`. Uma linha `nome:` sem valor
  vira atributo com valor vazio. Ao gravar, ele sai como `nome: ` (com espaço).
  TODO: confirmar se o jogo gera linhas `nome:` sem espaço.
- **Cabeçalho de bloco**: precisa ser `tipo : id {`, com espaço antes e depois
  do `:`. TODO: confirmar se o jogo gera blocos com outra formatação.
- **Escrita**: `ToText` usa CRLF e UTF-8 sem BOM. A ordem de blocos e
  atributos é preservada.
- **Round-trip**: `RealSaveTests` compara `ToText()` com o texto original dos
  saves de `samples/` e passa para todos os saves que estão lá. Os saves não
  vêm com o repositório.
