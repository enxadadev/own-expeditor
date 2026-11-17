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

> TODO: confirmar. O projeto ainda não tem perfil de publicação nem
> configuração de `RuntimeIdentifier` ou `PublishSingleFile`. O comando abaixo
> é a forma padrão do .NET, não foi executado aqui e precisa ser testado.

```bash
dotnet publish src/OwnExpeditor.App -c Release -r win-x64 \
  --self-contained true -p:PublishSingleFile=true
```

A saída fica em `src/OwnExpeditor.App/bin/Release/net8.0/win-x64/publish/`.
Não versione essa pasta (a pasta `publish/` já está no `.gitignore`).

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
```

Os arquivos de `.claude/` são configuração do assistente. Ver
`ia/agents/` para a definição dos agentes.
