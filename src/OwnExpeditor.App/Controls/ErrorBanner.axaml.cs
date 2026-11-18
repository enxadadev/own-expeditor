using System.Windows.Input;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using OwnExpeditor.App.Services;

namespace OwnExpeditor.App.Controls;

public enum BannerSeverity { Error, Warning, Info }

public enum BannerLayout { Inline, Block }

/// <summary>Faixa "o que aconteceu + o que fazer" (design/controles §2.4). Não conhece códigos de erro.</summary>
public partial class ErrorBanner : UserControl
{
    public static readonly StyledProperty<BannerSeverity> SeverityProperty = AvaloniaProperty.Register<ErrorBanner, BannerSeverity>(nameof(Severity));
    public static readonly StyledProperty<BannerLayout> LayoutProperty = AvaloniaProperty.Register<ErrorBanner, BannerLayout>(nameof(Layout));
    public static readonly StyledProperty<string> TitleProperty = AvaloniaProperty.Register<ErrorBanner, string>(nameof(Title), "");
    public static readonly StyledProperty<string> MessageProperty = AvaloniaProperty.Register<ErrorBanner, string>(nameof(Message), "");
    public static readonly StyledProperty<string?> DetailsProperty = AvaloniaProperty.Register<ErrorBanner, string?>(nameof(Details));
    public static readonly StyledProperty<string?> ActionTextProperty = AvaloniaProperty.Register<ErrorBanner, string?>(nameof(ActionText));
    public static readonly StyledProperty<ICommand?> ActionCommandProperty = AvaloniaProperty.Register<ErrorBanner, ICommand?>(nameof(ActionCommand));
    public static readonly StyledProperty<ICommand?> DismissCommandProperty = AvaloniaProperty.Register<ErrorBanner, ICommand?>(nameof(DismissCommand));
    public static readonly StyledProperty<bool> IsDetailsOpenProperty = AvaloniaProperty.Register<ErrorBanner, bool>(nameof(IsDetailsOpen));
    public static readonly DirectProperty<ErrorBanner, bool> HasDetailsProperty =
        AvaloniaProperty.RegisterDirect<ErrorBanner, bool>(nameof(HasDetails), o => o.HasDetails);
    public static readonly DirectProperty<ErrorBanner, string> DetailsToggleTextProperty =
        AvaloniaProperty.RegisterDirect<ErrorBanner, string>(nameof(DetailsToggleText), o => o.DetailsToggleText);

    public ErrorBanner()
    {
        InitializeComponent();
        ApplySeverity();
        Automation.Track(this, UpdateAutomation, SeverityProperty, TitleProperty, IsDetailsOpenProperty);
    }

    public BannerSeverity Severity { get => GetValue(SeverityProperty); set => SetValue(SeverityProperty, value); }
    public BannerLayout Layout { get => GetValue(LayoutProperty); set => SetValue(LayoutProperty, value); }
    public string Title { get => GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string Message { get => GetValue(MessageProperty); set => SetValue(MessageProperty, value); }

    /// <summary>Detalhe técnico (caminho, linha); colapsado atrás de "Mostrar detalhes".</summary>
    public string? Details { get => GetValue(DetailsProperty); set => SetValue(DetailsProperty, value); }
    public string? ActionText { get => GetValue(ActionTextProperty); set => SetValue(ActionTextProperty, value); }
    public ICommand? ActionCommand { get => GetValue(ActionCommandProperty); set => SetValue(ActionCommandProperty, value); }
    public ICommand? DismissCommand { get => GetValue(DismissCommandProperty); set => SetValue(DismissCommandProperty, value); }
    public bool IsDetailsOpen { get => GetValue(IsDetailsOpenProperty); set => SetValue(IsDetailsOpenProperty, value); }

    public bool HasDetails => !string.IsNullOrEmpty(Details);

    public string DetailsToggleText => Localizer.Instance[IsDetailsOpen ? "Banner_Details_Hide" : "Banner_Details_Show"];

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SeverityProperty) ApplySeverity();
        else if (change.Property == DetailsProperty) RaisePropertyChanged(HasDetailsProperty, !HasDetails, HasDetails);
        else if (change.Property == IsDetailsOpenProperty) RaisePropertyChanged(DetailsToggleTextProperty, "", DetailsToggleText);
        else if (change.Property == LayoutProperty) ApplyLayout();
    }

    protected override void OnKeyDown(Avalonia.Input.KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Avalonia.Input.Key.Escape && DismissCommand?.CanExecute(null) == true)
        {
            DismissCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void OnToggleDetails(object? sender, RoutedEventArgs e) => IsDetailsOpen = !IsDetailsOpen;

    private void ApplySeverity()
    {
        foreach (var name in new[] { "error", "warning", "info" })
        {
            Frame.Classes.Set(name, false);
            SeverityIcon.Classes.Set(name, false);
        }
        var cls = Severity.ToString().ToLowerInvariant();
        Frame.Classes.Set(cls, true);
        SeverityIcon.Classes.Set(cls, true);
        AutomationProperties.SetLiveSetting(this, Severity == BannerSeverity.Error ? AutomationLiveSetting.Assertive : AutomationLiveSetting.Polite);
    }

    private void ApplyLayout()
    {
        var block = Layout == BannerLayout.Block;
        SeverityIcon.Width = SeverityIcon.Height = block ? 48 : 20;
        SeverityIcon.VerticalAlignment = block ? VerticalAlignment.Center : VerticalAlignment.Top;
        Frame.Padding = block ? new Thickness(32, 28) : new Thickness(12, 10);
        Frame.HorizontalAlignment = block ? HorizontalAlignment.Center : HorizontalAlignment.Stretch;
        Frame.VerticalAlignment = block ? VerticalAlignment.Center : VerticalAlignment.Top;
        Frame.MaxWidth = block ? 560 : double.PositiveInfinity;
    }

    private void UpdateAutomation() =>
        AutomationProperties.SetName(this, Localizer.Instance.Get("Banner_Name_A11y", Localizer.Instance[$"Banner_Severity_{Severity}"], Title));
}
