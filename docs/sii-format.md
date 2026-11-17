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

## 5. Validado no jogo (17/11/2026, ETS2 1.5x, Windows)

- **O jogo carrega `game.sii` e `info.sii` em texto puro.** Não é preciso
  recriptografar.
- **Job injetado num slot vazio aparece no mercado de fretes** (kaarfor.budapest
  → dfh.berlin, `cargo.used_plast`, 847 km).
- **Um job foi descartado sem aviso, e a causa não é conhecida.**
  tradeaux.budapest → dfh.berlin com `cargo.plumb_suppl` não apareceu. A
  hipótese de que a DFH não aceitava a carga **está errada**: `def/company/dfh/in/plumb_suppl.sii`
  existe e `tradeaux/out/plumb_suppl.sii` também. Diferenças em relação ao teste
  que funcionou: outra empresa de origem, reboque `scs_dry_s` e
  `company_truck: daf_4x2_a`. Ainda a investigar.
- **Steam Cloud:** em perfis sincronizados, o jogo pede a lista de arquivos à
  Steam. Pastas de save criadas à mão em `Steam\userdata\...\remote` são ignoradas.
  O app deve detectar isso e orientar o usuário a desativar o Steam Cloud do perfil,
  ou gravar sobre um slot existente.
- Uma pasta de save nova chamada `5` não apareceu e `6` apareceu; a causa não foi
  investigada. Possível sobra da sincronização da Steam.

## 6. Em aberto

- Se o jogo valida reboque e caminhão (`company_truck`) contra a carga.
- Como calcular a distância quando o par origem/destino não existe no save.
## 8. Definições do jogo (.scs)

Leitor em `src/OwnExpeditor.Data/Scs/` (HashFS v2 + CityHash64, spec do
scs_tools, MIT). O app lê `def.scs` e `dlc_*.scs` da instalação do usuário em
tempo de execução, sem distribuir arquivos do jogo. O catálogo completo
(def + 10 DLCs de mapa) carrega em ~70 ms.

| Caminho no pacote | Conteúdo |
|-------------------|----------|
| `def/company/<empresa>/in/<carga>.sii` | cargas que a empresa recebe |
| `def/company/<empresa>/out/<carga>.sii` | cargas que a empresa envia |
| `def/company/<empresa>/editor/<cidade>.sii` | cidades onde a empresa existe (`city:`) |
| `def/cargo/<carga>.sui` | `cargo_data`: nome (`@@chave@@`), `body_types[]`, `group[]`, `unit_reward_per_km`, massa, volume |
| `def/cargo.sii` | `@include` de todos os `.sui` |
| `def/company/<empresa>.sui` | `name: "Kaarfor"` (nome de exibição, sem tradução) |
| `def/city/<cidade>.sui` | `city_data : city.x`: `city_name` (nome local), `city_name_localized: "@@x@@"`, `country` |
| `locale/<idioma>/local*.sii` (em `locale.scs`) | `localization_db`: pares `key[]`/`val[]`; `\xNN` são bytes UTF-8; pode ter `@include` e vir em 3nK |

Nomes na tela: tradução (`@@chave@@` no idioma de `config.cfg` → `g_lang_init`,
senão o idioma do sistema, senão en_us/en_gb) → nome local → id legível.
O `locale.scs` ainda não foi testado com o arquivo real.

As DLCs só trazem os arquivos delas; o catálogo junta todos os pacotes.
Ainda não lido: mods (`.scs`/`.zip` na pasta `mod`), HashFS v1 e o vínculo
reboque ↔ `body_types`.

## 7. BSII (binário)

Implementado em `src/OwnExpeditor.Sii/BsiiDecoder.cs`, portado de
SII-Decrypt-cpp (MIT). Resumo do formato:

- Cabeçalho: `BSII` + versão u32 (1–3). Depois, uma sequência de blocos que
  começam com um u32:
  - `0` → definição de estrutura: u8 válido (0 = fim do arquivo), u32 id,
    string nome, e campos (u32 tipo + string nome) até tipo `0`. O tipo `0x37`
    (ordinal string) traz uma tabela valor → texto.
  - `≠0` → bloco de dados da estrutura com esse id: um ID e os valores dos
    campos, na ordem da estrutura.
- Strings: u32 tamanho + bytes UTF-8. Tudo little-endian.
- IDs: u8 tamanho (0 = `null`, 0xFF = `_nameless` + u64) e N tokens u64.
- Tokens: até 12 caracteres `[0-9a-z_]` em base 38, primeiro caractere no
  dígito menos significativo.
- Placement (`0x19`, v2+): 8 floats; o 4º carrega um deslocamento de setor
  somado a x e z.

Ao gerar texto, seguimos o jogo: `nil` para inteiros com valor máximo, floats
não inteiros como `&hex`, e strings sem aspas quando só têm `[A-Za-z0-9_]`.

Validação: os 25 saves do exemplo (23 BSII) decodificam; os nomes dos campos
e a forma dos valores batem com um save que o próprio jogo gravou em texto.
Validado no jogo: o slot 3 (BSII) convertido para texto e com job injetado
carregou normalmente, e o frete apareceu.
