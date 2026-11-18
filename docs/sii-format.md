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

O `SiiDecoder` lê `BSII` (ver seção 8) e `3nK` (XOR; só para arquivos de
definição do jogo). Erros: ver "Erros e índice" na seção 7.

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
 urgency: 0                             # 0 padrão, 1 importante, 2 urgente (TODO: confirmar)
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

### Glossário dos demais atributos

Os atributos acima são os que o app usa hoje. O glossário completo, com as
dúvidas marcadas, está em
[ia/skills/ets2-domain/SKILL.md](../ia/skills/ets2-domain/SKILL.md). Em
`company`, falta listar `reserved_trailer_slot`, `delivered_trailer` e
`delivered_pos`, que aparecem no save de exemplo.

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
O app já lê esses arquivos para validar as escolhas (ver seção 8).

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
- Se `info.sii` precisa ser atualizado junto.
- Se o jogo regenera as ofertas ao carregar quando `expiration_time` é menor
  que `game_time`.
- **Traduções não verificadas com o arquivo real (pendente para o PC Windows).**
  O `locale.scs` não está em `dataset/scs`, então as cargas aparecem com nome
  derivado do id ("Used plast"). No Windows: rodar o app apontando para a pasta
  do jogo e conferir se a barra de status mostra "Textos no idioma pt_br" e se
  cargas/cidades saem traduzidas; se não, gerar
  `tar -a -cf ets2_locale.zip locale.scs` na pasta do jogo e usar como fixture.
- ~~Se o jogo carrega um `game.sii` em texto puro gravado pelo app~~ —
  validado no jogo (seção 7; [ADR 0002](decisions/0002-gravar-save-em-texto.md)).

## 7. Como o código lê e grava (`src/OwnExpeditor.Sii`)

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

### Erros e índice (`SiiFormatException`)

Todo conteúdo inválido lança `SiiFormatException` (deriva de
`FormatException`), com `Kind` (`SiiErrorKind`), `Line` (texto, 1-based) ou
`Offset` (binário, em bytes) quando se aplicam. Nunca há documento parcial.
Erros de E/S (arquivo ausente, sem permissão) continuam sendo `IOException`.

| `Kind` | Situações | Local |
|---|---|---|
| `Empty` | Entrada com 0 bytes (decoder); texto sem nenhuma linha não vazia (`Parse`) | |
| `UnknownFormat` | Início sem `SiiNunit`, `ScsC`, `BSII` nem `3nK` | `Offset` 0 |
| `Truncated` | ScsC com menos de 56 bytes; 3nK com menos de 6; BSII sem os 8 bytes do cabeçalho ou que acaba no meio de um valor; bloco de texto sem `}` | `Offset` / `Line` do bloco aberto |
| `DecryptFailed` | AES falha (cifrado que não é múltiplo de 16 bytes) | |
| `DecompressFailed` | zlib inválido depois de decifrar (chave certa, dados ruins) | |
| `SizeMismatch` | Tamanho descomprimido diferente do cabeçalho ScsC (inclui cabeçalho maior que 2 GB) | |
| `UnsupportedVersion` | BSII com versão fora de 1–3 | `Offset` 4 |
| `UnknownValueType` | Tipo de valor de campo BSII não tratado | `Offset` |
| `Syntax` | Texto: cabeçalho diferente de `SiiNunit`, linha de bloco ou atributo inválido (`Line`). BSII: estrutura desconhecida ou duplicada, token inválido | `Line` / `Offset` quando disponível |

`IsUnreadable` é `true` para `Empty`, `Truncated`, `DecryptFailed`,
`DecompressFailed` e `SizeMismatch` (o arquivo está danificado; no Core vira
`SaveCorrupt`) e `false` para `UnknownFormat`, `UnsupportedVersion`,
`UnknownValueType` e `Syntax` (o arquivo está inteiro, mas não o entendemos;
no Core vira `SaveParseFailed`).

`3nK` é decodificado hoje (XOR), mas só para arquivos de definição do jogo;
um `game.sii` em 3nK não é esperado. Não há erro específico: cabeçalho curto é
`Truncated`.

**Índice por tipo**: `SiiDocument` mantém um índice `tipo → blocos` montado no
`Parse`. `OfType(tipo)` devolve `IReadOnlyList<SiiUnit>` na ordem do arquivo
(lista vazia se não há o tipo), sem varrer `Units`. O índice reflete apenas
blocos presentes no `Parse`; `Units` é público e não é observado.
`Parse(texto, IProgress<double>? progress)` reporta 0..1 pela posição no
texto.

## 8. BSII (binário)

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

## 9. Definições do jogo (.scs)

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


## 10. Distância entre cidades (descoberta de 18/11/2026)

Medições com o dataset real (`dataset/scs`: `def.scs` + 10 DLCs; **sem `base.scs`**)
e os 30+ saves do perfil de exemplo (`save/1..4`, `autosave*`).

**Coordenadas de cidades nos `.scs`: não existem em `def/`.**
- `def/city/<cidade>.sui` (80 cidades no `def.scs`) só traz `city_name`,
  `city_name_localized`, `country`, `population`, `map_x_offsets[]`/`map_y_offsets[]`
  (deslocamentos de **rótulo** por nível de zoom, não posição) e placas.
- `def/company/<empresa>/editor/<cidade>.sii` (`company_def`) só traz `city` e
  `prefab` (nome do prefab, sem posição).
- As posições existem apenas no mapa: `map/europe/sec±XXXX±YYYY.{base,desc,aux,data,snd}`
  (setores binários em grade; só as DLCs trazem os seus aqui, o mapa base está
  em `base.scs`, ausente no dataset). Extrair a posição de uma cidade exige ler
  itens `city` e nós dos `.base` de todos os setores: engenharia reversa do formato
  do mapa, fora do escopo desta etapa. **Decisão: não usar coordenadas; estimar a partir dos pares conhecidos.**

**Tabela conhecida.** `shortest_distance_km` de cada oferta com `target` e
distância > 0 dá o par (cidade da empresa dona, cidade do `target`). Os pares são
tratados como não direcionais (chave ordenada); o primeiro valor visto vence. No
perfil de exemplo: `save/3` sozinho → 386 cidades e 4.122 pares únicos
(5,6% dos pares possíveis); todos os saves do perfil juntos → 12.072 pares
(16,2%, grafo conexo). Agregar todos os saves do usuário triplica a cobertura.

**Estimativa (grafo).** Para um par ausente: menor caminho (Dijkstra) pelo
grafo de pares conhecidos × fator de calibração. Validação *leave-one-out*
(remove a aresta e estima, para todos os pares conhecidos), razão real/caminho:

| Base conhecida | Mediana real/caminho | Erro médio absoluto (fator = mediana) |
|----------------|----------------------|----------------------------------------|
| todos os saves (12.072 pares) | 1,00 a 1,04 | ≈ 16% (≤ 2 saltos) a ≈ 22% (qualquer) |
| só `save/3` (4.122 pares) | 0,83 a 0,91 | ≈ 26% a 28% |
| só `save/1` (580 pares) | 0,54 a 0,56 | ≈ 25% a 28% |

Validação *holdout* (20% dos pares conhecidos escondidos e estimados; fator calibrado
sem eles), todos os saves: erro médio absoluto 24%, viés +8%; por faixa do valor real:
< 300 km superestima ≈ +90% (vizinhos não têm caminho curto sem a aresta direta),
300–600 km ±15%, 600–1000 km ±11%, ≥ 1000 km subestima ≈ −15% a −24%. Só `save/3`:
erro médio 52% (as faixas curtas pesam). Alternativa testada (limites de desigualdade
triangular por vizinhos comuns) ficou na mesma faixa (22–25%) e foi descartada.
Os pares conhecidos não são amostra aleatória dos pares do mapa; o erro real dos
pares ausentes (em geral mais longos) pode diferir.

O fator depende da densidade do grafo (quanto mais esparso, mais o caminho
passa por cidades-polo e excede a rota direta), por isso o app **calibra em
tempo de execução**: mediana de real/caminho (leave-one-out) numa amostra
determinística (até 300 pares) da própria tabela, limitada a [0,3; 1,2].
Sem caminho (cidade sem nenhum par conhecido) → mediana dos pares conhecidos;
sem nenhum par → 500 km. Sempre marcada `Estimated`. Erro típico ≈ 25%: serve
para exibição; o jogo recalcula a rota ao dirigir (se recalcula
`shortest_distance_km`/pagamento ao carregar ou aceitar: **a validar no jogo**).

**Cache (Data).** `IDistanceCache` guarda a tabela agregada por jogo em
`<SettingsDirectory>/distances-<ets2|ats>.json` e as impressões digitais
(caminho, tamanho, data) dos saves já lidos, para só reler o que mudou. Cresce a
cada save aberto e por varredura dos saves de todos os perfis.
