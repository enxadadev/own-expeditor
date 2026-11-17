# Desenvolvimento

## Pré-requisitos

- **.NET SDK 8** (testado com 8.0.134). Os projetos têm `TargetFramework`
  `net8.0`.
- Desenvolvimento no Linux funciona. O alvo principal é o Windows.
- Pacotes NuGet: o primeiro `dotnet build` precisa de acesso à internet para
  restaurar Avalonia 11.3.0, CommunityToolkit.Mvvm 8.4.2 e as dependências de
  teste (xUnit 2.4.2, Microsoft.NET.Test.Sdk 17.6.0, coverlet 6.0.0).

## Comandos

Rodar a partir da raiz do repositório:

```bash
dotnet build                                   # compila a solução inteira
dotnet test                                    # roda os testes de Core e Sii
dotnet run --project src/OwnExpeditor.App      # abre a aplicação
```

Resultado verificado com `dotnet build`: 0 avisos, 0 erros.

Resultado verificado com `dotnet test`: 17 testes passando. Desses, 1 é o
placeholder `UnitTest1` de `Core.Tests`, e 16 são de `Sii.Tests`
(7 em `SiiDocumentTests` e 9 casos de `RealSaveTests`).

### Sobre `RealSaveTests`

Esses 9 casos rodam contra saves reais. O teste procura uma pasta `samples/`
subindo a partir do diretório de saída do teste, então ele encontra a pasta
mesmo estando num worktree, desde que ela exista em algum diretório pai. A
pasta `samples/` está no `.gitignore` e não vem com o repositório. Sem ela,
`RealSaveTests` não executa nenhum caso e o resto da suíte passa.

Não coloque saves pessoais no repositório. Use `samples/` para isso.

## Publicar para Windows

Use o script de release, que roda `toolkit/check.sh` antes e só publica se os
testes passarem:

```bash
toolkit/release.sh
```

O script publica `OwnExpeditor.App` para `win-x64`, self-contained e em arquivo
único (`PublishSingleFile`), e grava em `publish/<versão>/`:

- `OwnExpeditor.App.exe`, o executável;
- `RELEASE-NOTES.md`, com a seção `## <versão>` copiada de
  [changelog.md](changelog.md). Se a seção não existir, o script avisa.

> TODO: confirmar. O `release.sh` ainda não foi executado num ambiente Windows.
> Até lá, o teste do `.exe` gerado fica pendente.

A pasta `publish/` está no `.gitignore`. Não versione o conteúdo dela.

## Toolkit

`toolkit/` reúne scripts que substituem sequências de comandos repetidas, tanto
por pessoas quanto pelos agentes em `ia/`. Os scripts devolvem saídas curtas
(uma linha de resumo, erros e avisos). Eles rodam a partir de qualquer
subpasta do repositório, porque resolvem a raiz pelo git.

A lista completa e o papel de cada script estão na seção Toolkit de
[ia/README.md](../ia/README.md). Abaixo, os casos de uso mais comuns.

### Build e testes

```bash
toolkit/check.sh                                  # build + testes, resumido
toolkit/check.sh --filter "FullyQualifiedName~Sii" # só um subconjunto dos testes
```

`check.sh` imprime `BUILD: ok (N avisos)` e
`TESTES: N passaram, N falharam, N pulados`. Em caso de falha, mostra as
primeiras linhas de erro, de falha e de exceção.

### Regras de camadas e docs

```bash
toolkit/check-layers.sh    # CAMADAS: ok, ou VIOLACAO: <projeto> -> <referência>
toolkit/check-docs.sh      # DOCS: ok, ou LINK QUEBRADO / FORA DO INDICE
```

`check-docs.sh` também conta os `TODO: confirmar` espalhados em `docs/`.

### Diff para revisão

```bash
toolkit/diff-summary.sh                           # contra a branch principal
toolkit/diff-summary.sh main -- src/OwnExpeditor.Sii   # base e caminhos específicos
```

Mostra o `stat` e o diff com 2 linhas de contexto, sem `*.Designer.cs`, `obj/`
e `*.sii`.

### Saves de exemplo e inspeção do formato

```bash
toolkit/samples.sh            # lista game.sii de samples/ com o formato de cada um
toolkit/samples.sh --text     # só os que o parser atual consegue ler
toolkit/sii.sh types  <save>            # contagem por tipo de bloco
toolkit/sii.sh list   <save> <tipo> 10  # ids de um tipo
toolkit/sii.sh show   <save> <id>       # um bloco inteiro
toolkit/sii.sh attrs  <save> <tipo>     # atributos de um tipo e quantos blocos têm cada um
toolkit/sii.sh roundtrip <save>         # parse + write reproduz o texto?
```

`toolkit/sii.sh` compila a ferramenta `toolkit/SiiTool` na primeira execução e
de novo sempre que `src/OwnExpeditor.Sii` ou o próprio `SiiTool` mudam. Rode
sem argumentos para ver a ajuda.

Limitação atual: saves no formato **BSII** (binário) não são suportados. O
`samples.sh` marca cada save como `Text` (utilizável) ou `Binary` (BSII, ainda
não suportado). Os comandos do `sii.sh` só funcionam nos `Text`. O `samples.sh`
procura `samples/` na raiz do worktree e, em seguida, na do repositório
principal. Os caminhos que ele imprime são locais: não os copie para a
documentação.

Para detalhes do formato, ver [sii-format.md](sii-format.md).

### Versão e numeração

```bash
toolkit/next-owne.sh                  # próximo número OWNE livre, ex.: OWNE-11
toolkit/bump-version.sh patch --dry-run   # mostra 0.1.0 -> 0.1.1 sem gravar
toolkit/bump-version.sh minor             # altera Directory.Build.props
```

`bump-version.sh` aceita `patch`, `minor` ou `major`. `--dry-run` vai como
segundo argumento. `next-owne.sh` consulta `git fetch --all` antes de ler o
histórico de todas as branches.

### Commits

```bash
toolkit/precommit.sh                       # branch, próximo OWNE, versão, arquivos e alertas
toolkit/lint-commit-msg.sh msg.txt         # valida título, linha em branco e OWNE repetido
toolkit/install-hooks.sh                   # instala o hook commit-msg (vale para os worktrees)
toolkit/commit.sh msg.txt src/a.cs src/b.cs  # valida e commita só os arquivos listados
```

`precommit.sh` alerta quando a branch atual é a principal, quando há arquivos
proibidos (`.sii`, `bin/`, `obj/`, `samples/`, `publish/`) e quando a versão
mudou sem entrada no changelog.

`commit.sh` só deve ser usado depois da aprovação explícita do usuário. Ele
não aceita `git add -A`: a lista de arquivos é obrigatória. Ele valida a
mensagem antes e commita com `--no-verify`, porque a validação já foi feita.

Regras da mensagem: título `OWNE-N: Resumo` com até 72 caracteres, sem ponto
final, linha 2 em branco, corpo não vazio, e sem menção a Claude, Anthropic
ou co-autoria.

### ADRs

```bash
toolkit/new-adr.sh "usar-cache-de-distancias" < texto-do-adr.md
```

Cria `docs/decisions/NNNN-usar-cache-de-distancias.md` com o próximo número
livre e imprime o caminho. O texto vem da entrada padrão. Se a entrada estiver
vazia, o script recusa e não cria o arquivo. Ver [decisions/](decisions/).

### Revisão pelo Codex

```bash
toolkit/codex-review.sh            # diff contra a branch principal
toolkit/codex-review.sh origin/main
```

Precisa do Codex CLI instalado e autenticado (`npm i -g @openai/codex` e
`codex login`). O relatório vai para `.reviews/codex-<data>.md`, pasta que está
no `.gitignore`. O script imprime quantos achados `BLOQUEANTE` e `IMPORTANTE`
o relatório tem.

## Convenções

## Convenções

- Nullable e `ImplicitUsings` estão ligados em todos os projetos.
- A versão do produto fica em `Directory.Build.props` (`M.X.Y`). Quem altera
  essa versão é o agente `git-ops`, em `ia/agents/git-ops.md`. Ver
  [changelog.md](changelog.md).
- Commits seguem `OWNE-N: Resumo`, com número incremental único. Ver
  `ia/agents/git-ops.md`.
- Testes de unidade usam strings inline, como em
  `tests/OwnExpeditor.Sii.Tests/SiiDocumentTests.cs`.
- Fixtures pequenos vão em `tests/**/Fixtures/`. Essa pasta ainda não existe.
- Saves de exemplo ficam em `samples/`, fora do git. Ver a seção acima.
- Não há listas fixas de cidades ou cargas no código
  ([ADR 0003](decisions/0003-sem-listas-fixas.md)).
- Antes de mudar o formato de leitura ou escrita do save, documente a descoberta
  em [sii-format.md](sii-format.md). Esse requisito vem do `CLAUDE.md`.

## Estrutura do repositório

```
CLAUDE.md                 instruções do projeto para o Claude Code
Directory.Build.props     versão do produto
OwnExpeditor.sln          solução
src/                      código de produção (ver architecture/layers.md)
tests/                    testes xUnit
docs/                     esta documentação
ia/                       agentes e skills de apoio ao desenvolvimento
toolkit/                  scripts de build, checagem, commit e inspeção de saves
```

Os arquivos de `.claude/` são configuração do assistente. Ver
`ia/agents/` para a definição dos agentes.
