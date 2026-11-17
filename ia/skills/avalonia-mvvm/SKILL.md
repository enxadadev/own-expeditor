---
name: avalonia-mvvm
description: Padrões de código para a UI Avalonia 11 + CommunityToolkit.Mvvm do Own Expeditor — compiled bindings, ViewModels, comandos assíncronos, Dispatcher, DI, navegação, listas grandes, design-time. Use ao escrever ou revisar Views/ViewModels (ui-dev, code-reviewer).
---

# Avalonia MVVM

## Bindings
- Compiled bindings: `x:DataType="vm:XxxViewModel"` em toda View/DataTemplate
  e `<AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>`
  no `OwnExpeditor.App.csproj` (**ainda não ativado** — ativar na primeira
  tarefa de UI). Erro de binding vira erro de build.
- Nada de `ElementName` para lógica; nada de lógica em converters além de
  formatação visual.

## ViewModel
```csharp
public partial class JobEditorViewModel(IJobService jobs, ILocalizer loc) : ViewModelBase
{
    [ObservableProperty] private CompanyItem? _origin;
    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(InjectCommand))] private CargoItem? _cargo;

    [RelayCommand(CanExecute = nameof(CanInject), IncludeCancelCommand = true)]
    private async Task InjectAsync(CancellationToken ct) { ... await jobs.InjectAsync(..., ct); }
    private bool CanInject() => Origin is not null && Cargo is not null;
}
```
- ViewModel não conhece `Window`, `Control` nem tipos do Sii/Data: fala com
  interfaces do Core.
- Estados como propriedades: `IsBusy`, `ErrorMessage`, `IsEmpty`.
- Textos para o usuário **sempre** via `ILocalizer`/recursos (skill `i18n-theming`).
- Diálogos/arquivos via interface (`IDialogService`, `IFilePicker`) implementada
  na App — testável.

## Threads
- Trabalho pesado (`Task.Run`/async do Core) fora da UI; ao voltar, atualize
  propriedades no `Dispatcher.UIThread` se não estiver nele.
- Nunca `.Result`/`.Wait()`.

## DI e navegação
- `Microsoft.Extensions.DependencyInjection` montado em `App.axaml.cs`;
  Views resolvidas pelo `ViewLocator` (convenção `XxxViewModel` → `XxxView`).
- Navegação = propriedade `CurrentPage` (ViewModel) num `ContentControl`.

## Listas
- Milhares de itens (ofertas, empresas): `ListBox`/`ItemsRepeater` com
  virtualização (padrão `VirtualizingStackPanel`), filtro no ViewModel
  com debounce, nunca recriar a coleção a cada tecla.

## Design-time
- `Design.DataContext` com um ViewModel de exemplo para o previewer; dados
  fictícios, nunca de save real.
