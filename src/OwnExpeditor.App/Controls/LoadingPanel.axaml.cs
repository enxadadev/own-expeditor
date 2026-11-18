using System;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

namespace OwnExpeditor.App.Controls;

/// <summary>Carregando com texto, anel (indeterminado) ou barra (0–1). Só aparece após <see cref="ShowDelay"/> (design/controles §2.5).</summary>
public partial class LoadingPanel : UserControl
{
    public static readonly StyledProperty<string> TextProperty = AvaloniaProperty.Register<LoadingPanel, string>(nameof(Text), "");
    public static readonly StyledProperty<double?> ProgressProperty = AvaloniaProperty.Register<LoadingPanel, double?>(nameof(Progress));
    public static readonly StyledProperty<string?> DetailProperty = AvaloniaProperty.Register<LoadingPanel, string?>(nameof(Detail));
    public static readonly StyledProperty<ICommand?> CancelCommandProperty = AvaloniaProperty.Register<LoadingPanel, ICommand?>(nameof(CancelCommand));
    public static readonly StyledProperty<TimeSpan> ShowDelayProperty = AvaloniaProperty.Register<LoadingPanel, TimeSpan>(nameof(ShowDelay), TimeSpan.FromMilliseconds(300));
    public static readonly StyledProperty<bool> IsCompactProperty = AvaloniaProperty.Register<LoadingPanel, bool>(nameof(IsCompact));
    public static readonly DirectProperty<LoadingPanel, bool> IsIndeterminateProperty =
        AvaloniaProperty.RegisterDirect<LoadingPanel, bool>(nameof(IsIndeterminate), o => o.IsIndeterminate);
    public static readonly DirectProperty<LoadingPanel, double> ProgressValueProperty =
        AvaloniaProperty.RegisterDirect<LoadingPanel, double>(nameof(ProgressValue), o => o.ProgressValue);
    public static readonly DirectProperty<LoadingPanel, double> PanelOpacityProperty =
        AvaloniaProperty.RegisterDirect<LoadingPanel, double>(nameof(PanelOpacity), o => o.PanelOpacity);

    private double _panelOpacity;
    private DispatcherTimer? _timer;

    public LoadingPanel()
    {
        InitializeComponent();
        AttachedToVisualTree += (_, _) => StartDelay();
        DetachedFromVisualTree += (_, _) => _timer?.Stop();
    }

    public string Text { get => GetValue(TextProperty); set => SetValue(TextProperty, value); }

    /// <summary>0–1; <c>null</c> = indeterminado.</summary>
    public double? Progress { get => GetValue(ProgressProperty); set => SetValue(ProgressProperty, value); }

    /// <summary>Linha de apoio já formatada (ex.: "4.038 de 6.485 ofertas").</summary>
    public string? Detail { get => GetValue(DetailProperty); set => SetValue(DetailProperty, value); }
    public ICommand? CancelCommand { get => GetValue(CancelCommandProperty); set => SetValue(CancelCommandProperty, value); }
    public TimeSpan ShowDelay { get => GetValue(ShowDelayProperty); set => SetValue(ShowDelayProperty, value); }
    public bool IsCompact { get => GetValue(IsCompactProperty); set => SetValue(IsCompactProperty, value); }

    public bool IsIndeterminate => Progress is null;
    public double ProgressValue => Progress ?? 0;
    public double PanelOpacity => _panelOpacity;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ProgressProperty)
        {
            RaisePropertyChanged(IsIndeterminateProperty, !IsIndeterminate, IsIndeterminate);
            RaisePropertyChanged(ProgressValueProperty, 0, ProgressValue);
        }
    }

    private void StartDelay()
    {
        _timer?.Stop();
        if (ShowDelay <= TimeSpan.Zero) { SetOpacity(1); return; }
        SetOpacity(0);
        _timer = new DispatcherTimer { Interval = ShowDelay };
        _timer.Tick += (_, _) => { _timer?.Stop(); SetOpacity(1); };
        _timer.Start();
    }

    private void SetOpacity(double value)
    {
        var old = _panelOpacity;
        _panelOpacity = value;
        RaisePropertyChanged(PanelOpacityProperty, old, value);
    }
}
