---
name: dotnet-standards
description: Convenções C#/.NET 8 do Own Expeditor — nullable, records, async com cancelamento, exceções, Span em parsing, nomes, organização de arquivos. Use ao escrever ou corrigir código C# (core-dev, sii-engine, test-engineer, build-fixer, code-reviewer).
---

# .NET standards

- **Nullable** habilitado; sem `!` para silenciar. Valide entradas públicas
  com `ArgumentNullException.ThrowIfNull` / `ArgumentException.ThrowIfNullOrEmpty`.
- **Domain**: `sealed record` imutáveis; coleções expostas como
  `IReadOnlyList<T>`. Sem I/O, sem dependências.
- **Async**: métodos de I/O são `async Task<T> XxxAsync(..., CancellationToken ct = default)`;
  repasse o `ct`; sem `async void` (exceto event handlers); sem `.Result`.
  Em bibliotecas (Core/Data/Sii): `ConfigureAwait(false)`.
- **Exceções**: específicas (`InvalidDataException`, `NotSupportedException`,
  `IOException`), mensagem com contexto (arquivo, linha, offset). Nunca
  `catch (Exception)` vazio; capture só o que trata.
- **Resultados de negócio** esperados (ex.: "save travado") viram tipos
  de resultado/código de erro, não exceção.
- **Parsing/perf**: `ReadOnlySpan<char>`/`<byte>` para varrer; evite
  `Substring`/`Split` em laço quente; `StringBuilder` com capacidade.
- **Nomes**: PascalCase público, `_camelCase` campos, `Async` no fim de
  métodos assíncronos, um tipo público por arquivo (nome = arquivo).
- **Namespaces**: file-scoped, iguais à pasta (`OwnExpeditor.Core.Jobs`).
- **Primary constructors** para injeção simples; `required` para init obrigatório.
- **Sem estáticos com estado**; dependências por construtor (DI).
- Sem `#pragma warning disable` sem comentário explicando.
