namespace OwnExpeditor.App.ViewModels;

/// <summary>O que um módulo pode pedir ao shell: abrir Revisar, Configurações ou o seletor de save.</summary>
public interface IShellNavigation
{
    void OpenReview();

    void OpenSettings();

    void ChooseSave();
}
