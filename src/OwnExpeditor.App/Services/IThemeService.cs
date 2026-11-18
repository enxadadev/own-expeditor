using System;
using OwnExpeditor.Domain.Settings;

namespace OwnExpeditor.App.Services;

/// <summary>Tema do app: Claro, Escuro ou Sistema; troca em runtime, sem reiniciar.</summary>
public interface IThemeService
{
    ThemePreference Current { get; }

    void Apply(ThemePreference theme);

    event EventHandler? Changed;
}
