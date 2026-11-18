using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using OwnExpeditor.App.Services;

namespace OwnExpeditor.App.Controls;

public enum StatEmphasis { None, Accent, Danger, Success }

/// <summary>Número grande + rótulo (design/controles §2.7). Valor já formatado pela cultura.</summary>
public partial class StatTile : UserControl
{
    public static readonly StyledProperty<string> LabelProperty = AvaloniaProperty.Register<StatTile, string>(nameof(Label), "");
    public static readonly StyledProperty<string?> ValueProperty = AvaloniaProperty.Register<StatTile, string?>(nameof(Value));
    public static readonly StyledProperty<StatEmphasis> EmphasisProperty = AvaloniaProperty.Register<StatTile, StatEmphasis>(nameof(Emphasis));
    public static readonly StyledProperty<bool> IsLoadingProperty = AvaloniaProperty.Register<StatTile, bool>(nameof(IsLoading));
    public static readonly StyledProperty<string?> HintProperty = AvaloniaProperty.Register<StatTile, string?>(nameof(Hint));
    public static readonly DirectProperty<StatTile, string> DisplayValueProperty =
        AvaloniaProperty.RegisterDirect<StatTile, string>(nameof(DisplayValue), o => o.DisplayValue);
    public static readonly DirectProperty<StatTile, string> LabelUpperProperty =
        AvaloniaProperty.RegisterDirect<StatTile, string>(nameof(LabelUpper), o => o.LabelUpper);

    public StatTile()
    {
        InitializeComponent();
        ApplyEmphasis();
        Automation.Track(this, UpdateAutomation, LabelProperty, ValueProperty);
    }

    public string Label { get => GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
    public string? Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    public StatEmphasis Emphasis { get => GetValue(EmphasisProperty); set => SetValue(EmphasisProperty, value); }
    public bool IsLoading { get => GetValue(IsLoadingProperty); set => SetValue(IsLoadingProperty, value); }
    public string? Hint { get => GetValue(HintProperty); set => SetValue(HintProperty, value); }

    public string DisplayValue => string.IsNullOrEmpty(Value) ? "—" : Value;
    public string LabelUpper => Label.ToUpper(CultureInfo.CurrentCulture);

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ValueProperty)
        {
            RaisePropertyChanged(DisplayValueProperty, "", DisplayValue);
            ApplyEmphasis();
        }
        else if (change.Property == LabelProperty) RaisePropertyChanged(LabelUpperProperty, "", LabelUpper);
        else if (change.Property == EmphasisProperty) ApplyEmphasis();
    }

    private void ApplyEmphasis()
    {
        ValueText.Classes.Set("accent", Emphasis == StatEmphasis.Accent);
        ValueText.Classes.Set("danger", Emphasis == StatEmphasis.Danger);
        ValueText.Classes.Set("success", Emphasis == StatEmphasis.Success);
        ValueText.Classes.Set("unknown", string.IsNullOrEmpty(Value));
    }

    private void UpdateAutomation() => AutomationProperties.SetName(this, string.IsNullOrEmpty(Value)
        ? Localizer.Instance.Get("Stat_Unknown_A11y", Label)
        : Localizer.Instance.Get("Stat_A11y", Label, Value));
}
