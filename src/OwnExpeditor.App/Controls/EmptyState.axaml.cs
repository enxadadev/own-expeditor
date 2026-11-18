using System.Windows.Input;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Media;

namespace OwnExpeditor.App.Controls;

/// <summary>Lista ou área sem conteúdo, com motivo e, quando possível, ação (design/controles §2.3).</summary>
public partial class EmptyState : UserControl
{
    public static readonly StyledProperty<Geometry?> IconProperty = AvaloniaProperty.Register<EmptyState, Geometry?>(nameof(Icon));
    public static readonly StyledProperty<string> TitleProperty = AvaloniaProperty.Register<EmptyState, string>(nameof(Title), "");
    public static readonly StyledProperty<string?> MessageProperty = AvaloniaProperty.Register<EmptyState, string?>(nameof(Message));
    public static readonly StyledProperty<string?> ActionTextProperty = AvaloniaProperty.Register<EmptyState, string?>(nameof(ActionText));
    public static readonly StyledProperty<ICommand?> ActionCommandProperty = AvaloniaProperty.Register<EmptyState, ICommand?>(nameof(ActionCommand));
    public static readonly StyledProperty<bool> IsCompactProperty = AvaloniaProperty.Register<EmptyState, bool>(nameof(IsCompact));

    public static readonly DirectProperty<EmptyState, bool> ShowIconProperty =
        AvaloniaProperty.RegisterDirect<EmptyState, bool>(nameof(ShowIcon), o => o.ShowIcon);
    public static readonly DirectProperty<EmptyState, Thickness> BodyMarginProperty =
        AvaloniaProperty.RegisterDirect<EmptyState, Thickness>(nameof(BodyMargin), o => o.BodyMargin);

    public EmptyState()
    {
        InitializeComponent();
        AutomationProperties.SetAutomationId(this, nameof(EmptyState));
    }

    public Geometry? Icon { get => GetValue(IconProperty); set => SetValue(IconProperty, value); }
    public string Title { get => GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string? Message { get => GetValue(MessageProperty); set => SetValue(MessageProperty, value); }
    public string? ActionText { get => GetValue(ActionTextProperty); set => SetValue(ActionTextProperty, value); }
    public ICommand? ActionCommand { get => GetValue(ActionCommandProperty); set => SetValue(ActionCommandProperty, value); }

    /// <summary>Compacto: sem ícone e com menos folga, para dentro de cartão ou popup.</summary>
    public bool IsCompact { get => GetValue(IsCompactProperty); set => SetValue(IsCompactProperty, value); }

    public bool ShowIcon => !IsCompact && Icon is not null;
    public Thickness BodyMargin => new(IsCompact ? 12 : 24);

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsCompactProperty || change.Property == IconProperty)
        {
            RaisePropertyChanged(ShowIconProperty, !ShowIcon, ShowIcon);
            RaisePropertyChanged(BodyMarginProperty, default, BodyMargin);
        }
        else if (change.Property == TitleProperty)
        {
            AutomationProperties.SetName(this, Title);
        }
    }
}
