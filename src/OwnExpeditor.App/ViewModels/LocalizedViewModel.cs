using System;
using OwnExpeditor.App.Services;

namespace OwnExpeditor.App.ViewModels;

/// <summary>
/// Base dos ViewModels com textos: ao trocar de idioma todas as propriedades são relidas
/// (<c>PropertyChanged("")</c>). Quem tem vida curta (diálogos) deve ser descartado.
/// </summary>
public abstract class LocalizedViewModel : ViewModelBase, IDisposable
{
    private bool _disposed;

    protected LocalizedViewModel(ILocalizer loc)
    {
        Loc = loc;
        loc.CultureChanged += OnCultureChangedCore;
    }

    protected ILocalizer Loc { get; }

    protected virtual void OnCultureChanged() => OnPropertyChanged(string.Empty);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Loc.CultureChanged -= OnCultureChangedCore;
        OnDisposed();
    }

    protected virtual void OnDisposed() { }

    private void OnCultureChangedCore(object? sender, EventArgs e) => OnCultureChanged();
}
