using Avalonia;
using System;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Extensions.DependencyInjection;
using OwnExpeditor.App.Shell;
using Avalonia.Markup.Xaml;
using OwnExpeditor.App.ViewModels;
using OwnExpeditor.App.Views;

namespace OwnExpeditor.App;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = CreateShell();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static Window CreateShell()
    {
        AppComposition.RegisterViews();
        var services = AppComposition.Build();
        AppComposition.StartBackgroundServices(services);
        var shell = services.GetRequiredService<ShellViewModel>();
        var window = new ShellWindow { DataContext = shell };
        // Tema e idioma salvos entram assim que a janela abre (primeira execução: idioma do sistema).
        window.Opened += async (_, _) => await shell.InitializeAsync();
        window.Closed += (_, _) => (services as IDisposable)?.Dispose();
        return window;
    }
}