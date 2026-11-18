using System;
using Avalonia.Threading;

namespace OwnExpeditor.App.Services;

/// <summary>Leva um trabalho para a thread da UI (eventos do Core chegam em threads de segundo plano).</summary>
public interface IUiDispatcher
{
    void Post(Action action);
}

public sealed class AvaloniaUiDispatcher : IUiDispatcher
{
    public void Post(Action action) => Dispatcher.UIThread.Post(action);
}

/// <summary>Executa na hora (amostras de design e testes).</summary>
public sealed class ImmediateUiDispatcher : IUiDispatcher
{
    public void Post(Action action) => action();
}
