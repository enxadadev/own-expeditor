using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using OwnExpeditor.App.Services;

namespace OwnExpeditor.App.Controls;

/// <summary>
/// Campo de busca com debounce (design/controles §2.11): <see cref="Text"/> muda a cada tecla,
/// <see cref="Query"/> só depois de <see cref="DebounceMs"/>. Enter publica já; Esc limpa.
/// A filtragem é do ViewModel.
/// </summary>
public partial class SearchBox : UserControl
{
    public static readonly StyledProperty<string> TextProperty = AvaloniaProperty.Register<SearchBox, string>(nameof(Text), "", defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);
    public static readonly StyledProperty<string> QueryProperty = AvaloniaProperty.Register<SearchBox, string>(nameof(Query), "", defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);
    public static readonly StyledProperty<int> DebounceMsProperty = AvaloniaProperty.Register<SearchBox, int>(nameof(DebounceMs), 250);
    public static readonly StyledProperty<int> MinLengthProperty = AvaloniaProperty.Register<SearchBox, int>(nameof(MinLength));
    public static readonly StyledProperty<string> WatermarkProperty = AvaloniaProperty.Register<SearchBox, string>(nameof(Watermark), "");
    public static readonly StyledProperty<bool> IsBusyProperty = AvaloniaProperty.Register<SearchBox, bool>(nameof(IsBusy));
    public static readonly StyledProperty<int?> ResultCountProperty = AvaloniaProperty.Register<SearchBox, int?>(nameof(ResultCount));
    public static readonly DirectProperty<SearchBox, bool> HasTextProperty =
        AvaloniaProperty.RegisterDirect<SearchBox, bool>(nameof(HasText), o => o.HasText);

    private readonly DispatcherTimer _timer = new();

    public SearchBox()
    {
        InitializeComponent();
        _timer.Tick += (_, _) => Publish();
        Automation.Track(this, UpdateAutomation, ResultCountProperty);
        DetachedFromVisualTree += (_, _) => _timer.Stop();
    }

    public string Text { get => GetValue(TextProperty); set => SetValue(TextProperty, value); }

    /// <summary>Valor com debounce: o que o ViewModel usa para filtrar.</summary>
    public string Query { get => GetValue(QueryProperty); set => SetValue(QueryProperty, value); }
    public int DebounceMs { get => GetValue(DebounceMsProperty); set => SetValue(DebounceMsProperty, value); }
    public int MinLength { get => GetValue(MinLengthProperty); set => SetValue(MinLengthProperty, value); }
    public string Watermark { get => GetValue(WatermarkProperty); set => SetValue(WatermarkProperty, value); }
    public bool IsBusy { get => GetValue(IsBusyProperty); set => SetValue(IsBusyProperty, value); }

    /// <summary>Alimenta o anúncio "N resultados" para leitores de tela.</summary>
    public int? ResultCount { get => GetValue(ResultCountProperty); set => SetValue(ResultCountProperty, value); }

    public bool HasText => !string.IsNullOrEmpty(Text);

    public bool FocusInput() => Input.Focus();

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property != TextProperty) return;
        RaisePropertyChanged(HasTextProperty, !HasText, HasText);
        _timer.Stop();
        if (DebounceMs <= 0) { Publish(); return; }
        _timer.Interval = TimeSpan.FromMilliseconds(DebounceMs);
        _timer.Start();
    }

    private void Publish()
    {
        _timer.Stop();
        var trimmed = Text.Trim();
        var next = trimmed.Length < MinLength ? "" : trimmed;
        if (next != Query) Query = next;
    }

    private void OnClear(object? sender, RoutedEventArgs e)
    {
        Text = "";
        Publish();
        Input.Focus();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) { Publish(); e.Handled = true; }
        else if (e.Key == Key.Escape && HasText) { Text = ""; Publish(); e.Handled = true; }
    }

    private void UpdateAutomation()
    {
        Avalonia.Automation.AutomationProperties.SetLiveSetting(this, Avalonia.Automation.AutomationLiveSetting.Polite);
        Avalonia.Automation.AutomationProperties.SetName(this, ResultCount is { } n
            ? Localizer.Instance.GetPlural("Search_Results", n)
            : Watermark);
    }
}
