---
name: xunit-patterns
description: Padrões de teste xUnit — nomes, AAA, fixtures, saves reais opcionais, arquivos temporários, round-trip, testes de ViewModel. Use ao escrever ou revisar testes.
---

# xUnit patterns

- **Nome**: `Metodo_Cenario_Resultado` (`Save_ExistingFile_CreatesBackupFirst`).
- **AAA** com linhas em branco separando; um comportamento por teste;
  sem `if`/laços no teste (use `[Theory]` + `[InlineData]`/`[MemberData]`).
- **Fixtures**: `tests/<Proj>.Tests/Fixtures/*.sii`, copiados para a saída
  (`<None Include="Fixtures\**" CopyToOutputDirectory="PreserveNewest" />`);
  carregue por `Path.Combine(AppContext.BaseDirectory, "Fixtures", nome)`.
- **Saves reais**: só via `samples/` (fora do git); quando ausente, o teste
  não roda (padrão de `RealSaveTests`: `MemberData` vazio). Nunca falhar
  por falta de `samples/`.
- **Arquivos temporários**:
  ```csharp
  public sealed class TempDir : IDisposable
  {
      public string Path { get; } = Directory.CreateTempSubdirectory("ownexp-").FullName;
      public void Dispose() => Directory.Delete(Path, recursive: true);
  }
  ```
- **Round-trip**: `Assert.Equal(original, Parse(original).ToText())` — compare
  strings inteiras; em falha, reporte a primeira linha diferente.
- **Erros**: `Assert.Throws<InvalidDataException>(...)` e confira trecho da mensagem.
- **Async**: testes `async Task`, nunca `.Result`.
- **ViewModels**: dependências por interface com fakes simples escritos à
  mão (sem framework de mock até precisar); verifique propriedades e
  `CanExecute`, não a View.
- Rodar: `toolkit/check/check.sh --filter <Classe>`.
