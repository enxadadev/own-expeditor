using System;

namespace OwnExpeditor.App.Services;

/// <summary>Leva cada relatório de progresso para a thread da UI (o Core reporta de segundo plano).</summary>
public sealed class DispatchedProgress<T>(IUiDispatcher ui, Action<T> handler) : IProgress<T>
{
    public void Report(T value) => ui.Post(() => handler(value));
}
