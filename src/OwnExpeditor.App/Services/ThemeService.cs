using System;
using Avalonia;
using Avalonia.Styling;
using OwnExpeditor.Domain.Settings;

namespace OwnExpeditor.App.Services;

/// <summary>Onde o tema é aplicado (o <c>Application</c>); separado para testar sem Avalonia rodando.</summary>
public interface IThemeHost
{
    ThemeVariant RequestedThemeVariant { get; set; }
}

public sealed class ApplicationThemeHost : IThemeHost
{
    public ThemeVariant RequestedThemeVariant
    {
        get => Application.Current?.RequestedThemeVariant ?? ThemeVariant.Default;
        set { if (Application.Current is { } app) app.RequestedThemeVariant = value; }
    }
}

public sealed class ThemeService(IThemeHost host) : IThemeService
{
    public ThemePreference Current { get; private set; } = ThemePreference.System;

    public event EventHandler? Changed;

    public void Apply(ThemePreference theme)
    {
        host.RequestedThemeVariant = theme switch
        {
            ThemePreference.Light => ThemeVariant.Light,
            ThemePreference.Dark => ThemeVariant.Dark,
            _ => ThemeVariant.Default,
        };
        if (theme == Current) return;
        Current = theme;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
