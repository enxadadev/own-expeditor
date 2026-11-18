using System;
using Avalonia.Controls;
using Avalonia.Threading;
using OwnExpeditor.App.ViewModels;

namespace OwnExpeditor.App.Views;

/// <summary>Só aparece depois de 300 ms (cargas rápidas não piscam a tela) e leva o foco para Cancelar.</summary>
public partial class LoadingView : UserControl
{
    private DispatcherTimer? _timer;

    public LoadingView()
    {
        InitializeComponent();
        AttachedToVisualTree += (_, _) =>
        {
            var card = this.FindControl<Border>("Card")!;
            var delay = (DataContext as LoadingViewModel)?.ShowDelay ?? TimeSpan.FromMilliseconds(300);
            _timer?.Stop();
            if (delay <= TimeSpan.Zero)
                card.Opacity = 1;
            else
            {
                card.Opacity = 0;
                _timer = new DispatcherTimer { Interval = delay };
                _timer.Tick += (_, _) => { _timer?.Stop(); card.Opacity = 1; };
                _timer.Start();
            }
            this.FindControl<Button>("CancelButton")?.Focus();
        };
        DetachedFromVisualTree += (_, _) => _timer?.Stop();
    }
}
