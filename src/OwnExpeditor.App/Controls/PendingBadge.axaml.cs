using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using OwnExpeditor.App.Services;

namespace OwnExpeditor.App.Controls;

public enum BadgeVariant { Count, Dot, Tag }

/// <summary>Selo de pendência: contagem, ponto ou etiqueta (design/controles §2.10).</summary>
public partial class PendingBadge : UserControl
{
    public static readonly StyledProperty<BadgeVariant> VariantProperty = AvaloniaProperty.Register<PendingBadge, BadgeVariant>(nameof(Variant));
    public static readonly StyledProperty<int> CountProperty = AvaloniaProperty.Register<PendingBadge, int>(nameof(Count));
    public static readonly StyledProperty<string?> TextProperty = AvaloniaProperty.Register<PendingBadge, string?>(nameof(Text));
    public static readonly StyledProperty<bool> ShowZeroProperty = AvaloniaProperty.Register<PendingBadge, bool>(nameof(ShowZero));
    public static readonly StyledProperty<string?> AccessibleTextProperty = AvaloniaProperty.Register<PendingBadge, string?>(nameof(AccessibleText));
    public static readonly DirectProperty<PendingBadge, bool> IsShownProperty =
        AvaloniaProperty.RegisterDirect<PendingBadge, bool>(nameof(IsShown), o => o.IsShown);
    public static readonly DirectProperty<PendingBadge, bool> IsDotProperty =
        AvaloniaProperty.RegisterDirect<PendingBadge, bool>(nameof(IsDot), o => o.IsDot);
    public static readonly DirectProperty<PendingBadge, string> DisplayTextProperty =
        AvaloniaProperty.RegisterDirect<PendingBadge, string>(nameof(DisplayText), o => o.DisplayText);

    public PendingBadge()
    {
        InitializeComponent();
        Automation.Track(this, UpdateAutomation, VariantProperty, CountProperty, TextProperty, AccessibleTextProperty);
    }

    public BadgeVariant Variant { get => GetValue(VariantProperty); set => SetValue(VariantProperty, value); }
    public int Count { get => GetValue(CountProperty); set => SetValue(CountProperty, value); }
    public string? Text { get => GetValue(TextProperty); set => SetValue(TextProperty, value); }
    public bool ShowZero { get => GetValue(ShowZeroProperty); set => SetValue(ShowZeroProperty, value); }
    public string? AccessibleText { get => GetValue(AccessibleTextProperty); set => SetValue(AccessibleTextProperty, value); }

    public bool IsDot => Variant == BadgeVariant.Dot;
    public bool IsShown => Variant != BadgeVariant.Count || Count > 0 || ShowZero;

    /// <summary>"3", "99+" (100 ou mais) ou o texto da etiqueta.</summary>
    public string DisplayText => Variant switch
    {
        BadgeVariant.Tag => Text ?? "",
        _ => Count >= 100 ? Localizer.Instance.Get("Pending_Overflow", 99) : Count.ToString(Localizer.Instance.Culture),
    };

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == VariantProperty || change.Property == CountProperty || change.Property == ShowZeroProperty || change.Property == TextProperty)
        {
            RaisePropertyChanged(IsShownProperty, !IsShown, IsShown);
            RaisePropertyChanged(IsDotProperty, !IsDot, IsDot);
            RaisePropertyChanged(DisplayTextProperty, "", DisplayText);
        }
    }

    private void UpdateAutomation()
    {
        AutomationProperties.SetLiveSetting(this, AutomationLiveSetting.Polite);
        AutomationProperties.SetName(this, AccessibleText ?? Variant switch
        {
            BadgeVariant.Count when Count >= 100 => Localizer.Instance.Get("Pending_Overflow_A11y", 99),
            BadgeVariant.Count => Localizer.Instance.GetPlural("Pending_Count", Count),
            _ => Text ?? Localizer.Instance["Pending_Modified"],
        });
        RaisePropertyChanged(DisplayTextProperty, "", DisplayText);
    }
}
