using Avalonia.Input;
using OwnExpeditor.App.ViewModels;

namespace OwnExpeditor.App.Shell;

/// <summary>
/// Atalhos do shell (design/shell, seção 7). Ctrl+R abre a revisão; Ctrl+Enter só abre a confirmação (nunca grava) e só
/// na página Revisar; Ctrl+O, Ctrl+, e Ctrl+1…5; Esc fecha/volta. Com um diálogo aberto só o Esc vale.
/// </summary>
public static class ShellShortcuts
{
    /// <summary>Executa o atalho se houver e a ação estiver habilitada; devolve se tratou a tecla.</summary>
    public static bool TryExecute(ShellViewModel vm, Key key, KeyModifiers modifiers)
    {
        if (key == Key.Escape && modifiers == KeyModifiers.None) return vm.TryBack();
        if (modifiers != KeyModifiers.Control || vm.Dialog is not null) return false;

        switch (key)
        {
            case Key.R: return Run(vm.ReviewCommand);
            case Key.Enter: return Run(vm.RequestApplyCommand);
            case Key.O: return Run(vm.OpenSaveCommand);
            case Key.OemComma: return Run(vm.OpenSettingsCommand);
        }

        var number = key switch
        {
            >= Key.D1 and <= Key.D5 => key - Key.D0,
            >= Key.NumPad1 and <= Key.NumPad5 => key - Key.NumPad0,
            _ => 0,
        };
        return number > 0 && vm.HasSession && Run(vm.NavigateCommand, number);
    }

    private static bool Run(System.Windows.Input.ICommand command, object? parameter = null)
    {
        if (!command.CanExecute(parameter)) return false;
        command.Execute(parameter);
        return true;
    }
}
