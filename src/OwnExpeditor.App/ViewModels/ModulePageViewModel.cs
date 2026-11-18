using System;
using System.Threading;
using System.Threading.Tasks;
using OwnExpeditor.App.Services;
using OwnExpeditor.Core.Session;

namespace OwnExpeditor.App.ViewModels;

public enum PageState { Idle, Loading, Empty, Error, Ready }

/// <summary>
/// Página de um módulo. O shell chama <see cref="AttachAsync"/> quando uma sessão abre e <see cref="Detach"/> quando
/// ela fecha; o módulo enfileira mudanças com <c>Session.Stage</c> e nunca grava.
/// </summary>
public abstract class ModulePageViewModel(ILocalizer loc) : LocalizedViewModel(loc)
{
    private PageState _state;

    public abstract string ModuleId { get; }

    public PageState State
    {
        get => _state;
        protected set => SetProperty(ref _state, value);
    }

    protected IEditSession? Session { get; private set; }

    /// <summary>Navegação do shell, ligada por ele ao criar a página (nula em testes que não montam o shell).</summary>
    public IShellNavigation? Navigation { get; set; }

    public async Task AttachAsync(IEditSession session, CancellationToken ct)
    {
        Session = session;
        await OnSessionOpenedAsync(session, ct);
    }

    public void Detach()
    {
        Session = null;
        OnSessionClosed();
    }

    protected virtual Task OnSessionOpenedAsync(IEditSession session, CancellationToken ct) => Task.CompletedTask;

    protected virtual void OnSessionClosed() { }
}

/// <summary>Espaço reservado de um módulo que ainda não existe (design/shell: a área do módulo é só um espaço).</summary>
public sealed class ModulePlaceholderViewModel(ILocalizer loc, string moduleId, string titleKey) : ModulePageViewModel(loc)
{
    public override string ModuleId => moduleId;

    public string Title => Loc[titleKey];
}
