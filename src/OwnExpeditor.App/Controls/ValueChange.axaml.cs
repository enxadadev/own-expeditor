using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using OwnExpeditor.App.Services;

namespace OwnExpeditor.App.Controls;

public enum ValueChangeLayout { Auto, Horizontal, Vertical }

/// <summary>Um valor antes → depois, sem tachado (design/controles §2.8). Vazio vira "(vazio)".</summary>
public partial class ValueChange : UserControl
{
    public static readonly StyledProperty<string?> BeforeProperty = AvaloniaProperty.Register<ValueChange, string?>(nameof(Before));
    public static readonly StyledProperty<string?> AfterProperty = AvaloniaProperty.Register<ValueChange, string?>(nameof(After));
    public static readonly StyledProperty<bool> IsNumericProperty = AvaloniaProperty.Register<ValueChange, bool>(nameof(IsNumeric));
    public static readonly StyledProperty<ValueChangeLayout> LayoutProperty = AvaloniaProperty.Register<ValueChange, ValueChangeLayout>(nameof(Layout));
    public static readonly StyledProperty<string?> LabelProperty = AvaloniaProperty.Register<ValueChange, string?>(nameof(Label));
    public static readonly StyledProperty<string?> BeforeIconProperty = AvaloniaProperty.Register<ValueChange, string?>(nameof(BeforeIcon));
    public static readonly StyledProperty<string?> AfterIconProperty = AvaloniaProperty.Register<ValueChange, string?>(nameof(AfterIcon));
    public static readonly DirectProperty<ValueChange, string> BeforeTextProperty =
        AvaloniaProperty.RegisterDirect<ValueChange, string>(nameof(BeforeText), o => o.BeforeText);
    public static readonly DirectProperty<ValueChange, string> AfterTextProperty =
        AvaloniaProperty.RegisterDirect<ValueChange, string>(nameof(AfterText), o => o.AfterText);
    public static readonly DirectProperty<ValueChange, bool> ShowHorizontalProperty =
        AvaloniaProperty.RegisterDirect<ValueChange, bool>(nameof(ShowHorizontal), o => o.ShowHorizontal);
    public static readonly DirectProperty<ValueChange, bool> ShowAutoProperty =
        AvaloniaProperty.RegisterDirect<ValueChange, bool>(nameof(ShowAuto), o => o.ShowAuto);
    public static readonly DirectProperty<ValueChange, bool> ShowVerticalProperty =
        AvaloniaProperty.RegisterDirect<ValueChange, bool>(nameof(ShowVertical), o => o.ShowVertical);

    public ValueChange()
    {
        InitializeComponent();
        Automation.Track(this, UpdateAutomation, BeforeProperty, AfterProperty, LabelProperty);
    }

    public string? Before { get => GetValue(BeforeProperty); set => SetValue(BeforeProperty, value); }
    public string? After { get => GetValue(AfterProperty); set => SetValue(AfterProperty, value); }
    /// <summary>Chaves <c>IconTrailer*</c> (16 px antes do texto de cada lado); nulo = sem ícone. Só decoração.</summary>
    public string? BeforeIcon { get => GetValue(BeforeIconProperty); set => SetValue(BeforeIconProperty, value); }
    public string? AfterIcon { get => GetValue(AfterIconProperty); set => SetValue(AfterIconProperty, value); }
    public bool IsNumeric { get => GetValue(IsNumericProperty); set => SetValue(IsNumericProperty, value); }
    public ValueChangeLayout Layout { get => GetValue(LayoutProperty); set => SetValue(LayoutProperty, value); }

    /// <summary>Só para o nome acessível ("{Label}: de X para Y").</summary>
    public string? Label { get => GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    public string BeforeText => string.IsNullOrEmpty(Before) ? Localizer.Instance["Diff_Value_Empty"] : Before;
    public string AfterText => string.IsNullOrEmpty(After) ? Localizer.Instance["Diff_Value_Empty"] : After;
    public static readonly DirectProperty<ValueChange, bool> BeforeEmptyProperty =
        AvaloniaProperty.RegisterDirect<ValueChange, bool>(nameof(BeforeEmpty), o => o.BeforeEmpty);
    public static readonly DirectProperty<ValueChange, bool> AfterEmptyProperty =
        AvaloniaProperty.RegisterDirect<ValueChange, bool>(nameof(AfterEmpty), o => o.AfterEmpty);

    public bool BeforeEmpty => string.IsNullOrEmpty(Before);
    public bool AfterEmpty => string.IsNullOrEmpty(After);
    public bool ShowHorizontal => Layout == ValueChangeLayout.Horizontal;
    public bool ShowAuto => Layout == ValueChangeLayout.Auto;
    public bool ShowVertical => Layout == ValueChangeLayout.Vertical;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == BeforeProperty)
        {
            RaisePropertyChanged(BeforeTextProperty, "", BeforeText);
            RaisePropertyChanged(BeforeEmptyProperty, !BeforeEmpty, BeforeEmpty);
        }
        else if (change.Property == AfterProperty)
        {
            RaisePropertyChanged(AfterTextProperty, "", AfterText);
            RaisePropertyChanged(AfterEmptyProperty, !AfterEmpty, AfterEmpty);
        }
        else if (change.Property == LayoutProperty)
        {
            RaisePropertyChanged(ShowHorizontalProperty, !ShowHorizontal, ShowHorizontal);
            RaisePropertyChanged(ShowAutoProperty, !ShowAuto, ShowAuto);
            RaisePropertyChanged(ShowVerticalProperty, !ShowVertical, ShowVertical);
        }
    }

    private void UpdateAutomation()
    {
        RaisePropertyChanged(BeforeTextProperty, "", BeforeText);
        RaisePropertyChanged(AfterTextProperty, "", AfterText);
        AutomationProperties.SetName(this, Localizer.Instance.Get("Diff_Change_A11y", Label ?? "", BeforeText, AfterText));
    }
}
