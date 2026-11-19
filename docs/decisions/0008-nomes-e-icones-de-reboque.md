# 0008. Nomes legíveis e ícones de reboque a partir do id

- Status: aceita
- Data: 2026-11-18
- Relacionadas: 0003, 0004, 0006
- Desenho: [`design/nomes-reboques/`](../../design/nomes-reboques/README.md)

## Contexto

- O `job_offer_data` do save só traz `trailer_variant` (ex.: `trailer.scs_flat_b`) e `trailer_definition` (ex.: `trailer_def.scs.flatbed.single_3.flatbed_w`). Nenhum dos dois traz marca, eixos, comprimento nem localização.
- Até aqui o app mostrava o reboque com `GameIds.Humanize`, que transforma o id em texto do tipo `Scs flat b`. Esse texto não diz que reboque é, e na revisão a troca de reboque parecia não mudar nada quando só o comprimento mudava.
- O id da definição tem a estrutura `trailer_def.<dono>.<carroceria>.<cadeia>_<eixos>[_sh|_ln].<acabamento>` (ver [sii-format.md](../sii-format.md), seção "Ids de reboque"). Mods seguem a mesma convenção na maioria dos casos, mas nem sempre usam o vocabulário conhecido.
- O jogo tem tradução dos nomes em `locale.scs`, mas ela depende da pasta do jogo instalada e não cobre mods instalados fora dela.

## Decisão

1. **Leitura do id no Domain.** `Domain.Games.TrailerDescriptor.Parse(definition, variant)` separa dono, carroceria, cadeia, grupos de eixos, comprimento e tokens. É conservador: só reconhece o que casa com o padrão, deixa o resto como token e **nunca lança**. Entrada vazia ou estranha vira um descritor sem cadeia, eixos nem comprimento.
2. **Core expõe o descritor.** `IGameNames.Trailer(definition, variant)` tem implementação padrão que chama `TrailerDescriptor.Parse`. `DiffValue.Trailer(definition, variant)` leva o reboque para a revisão como id, sem texto.
3. **App monta o texto.** `TrailerText` (App) procura, nesta ordem:
   - `Trailer_Finish_<token>`, do último token para o primeiro;
   - `Trailer_Body_<token>`, do primeiro para o último;
   - `GameIds.Humanize`, como último recurso.

   A cadeia entra no nome só quando não é simples (`Trailer_Chain_double`, `_bdouble`, `_hct`). Eixos e comprimento vão numa linha de detalhes separada. O tooltip mostra o id cru.
4. **Chaves `Trailer_*` no `.resx` são vocabulário de categorias.** Isso é uma exceção delimitada à [ADR 0006](0006-integracao-do-codigo-de-injecao-ao-shell.md): essas chaves só dão texto a tokens já reconhecidos. Nunca filtram nem geram opções. A lista de opções continua vindo do save e do catálogo.
5. **Ícone decorativo.** `TrailerIcons.Resolve(descritor)` escolhe a chave `IconTrailer*` pelos mesmos tokens do nome. Desconhecido ou de mod cai no ícone genérico. Cadeias duplas ganham um selo `×2`, e `hct` ganha `×3`. Os desenhos são vetoriais e originais, sem arte do jogo. O controle `TrailerIcon` os desenha.

Alternativas consideradas:

- **Ler `trailer defs` e `locale.scs` do jogo** (fase 2). Dependeria da pasta do jogo e não cobriria mods. Se existir, terá precedência atrás de `IGameNames.Trailer`, sem mudar as chamadas.
- **Dicionário id → nome fixo.** Seria uma lista fixa de reboques, o que viola a [ADR 0003](0003-sem-listas-fixas.md).
- **Montar o texto no Core.** Obrigaria o Core a conhecer idioma e textos, o que quebra a [ADR 0004](0004-suporte-a-tema-e-idioma.md) e a regra de camadas.

## Consequências

- A gravação do save não muda. Nada disso altera o `ChangeSet` nem o `SavePatch`.
- Um token novo pede só duas coisas: as chaves `Trailer_Finish_<t>` ou `Trailer_Body_<t>` nos `.resx` (pt-BR e en) e uma linha na tabela de ícones de `TrailerIcons`. Sem nova tela nem nova regra.
- `ILocalizer` ganha `TryGet(key, out value)`, que busca uma chave sem devolver `!chave!`. Isso permite cair para o próximo passo da cadeia sem mostrar o marcador de erro.
- Reboque de mod desconhecido continua na lista, com nome derivado do id e ícone genérico.
- Os tokens com significado incerto (ex.: `dryvan_m`, `3e`, `41`) são tratados conforme o desenho e precisam de confirmação no jogo antes de virarem regra fixa.
