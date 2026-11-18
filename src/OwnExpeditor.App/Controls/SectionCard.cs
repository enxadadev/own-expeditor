using Avalonia;
using Avalonia.Controls;

namespace OwnExpeditor.App.Controls;

/// <summary>Agrupa campos relacionados: borda, sem sombra (design/controles §2.6). Template em Styles/Controls.axaml.</summary>
public class SectionCard : ContentControl
{
    public static readonly StyledProperty<string?> TitleProperty = AvaloniaProperty.Register<SectionCard, string?>(nameof(Title));
    public static readonly StyledProperty<string?> DescriptionProperty = AvaloniaProperty.Register<SectionCard, string?>(nameof(Description));
    public static readonly StyledProperty<object?> HeaderActionsProperty = AvaloniaProperty.Register<SectionCard, object?>(nameof(HeaderActions));

    public SectionCard()
    {
        Padding = new Thickness(16);
    }

    public string? Title { get => GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string? Description { get => GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }
    public object? HeaderActions { get => GetValue(HeaderActionsProperty); set => SetValue(HeaderActionsProperty, value); }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TitleProperty) Avalonia.Automation.AutomationProperties.SetName(this, Title);
    }
}
