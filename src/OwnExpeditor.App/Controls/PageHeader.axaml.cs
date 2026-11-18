using System;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;

namespace OwnExpeditor.App.Controls;

/// <summary>Título do módulo, eyebrow (perfil/save), contagem e ações (design/controles §2.2).</summary>
public partial class PageHeader : UserControl
{
    public static readonly StyledProperty<string?> EyebrowProperty = AvaloniaProperty.Register<PageHeader, string?>(nameof(Eyebrow));
    public static readonly StyledProperty<string> TitleProperty = AvaloniaProperty.Register<PageHeader, string>(nameof(Title), "");
    public static readonly StyledProperty<string?> CountProperty = AvaloniaProperty.Register<PageHeader, string?>(nameof(Count));
    public static readonly StyledProperty<string?> CountLabelProperty = AvaloniaProperty.Register<PageHeader, string?>(nameof(CountLabel));
    public static readonly StyledProperty<object?> ActionsProperty = AvaloniaProperty.Register<PageHeader, object?>(nameof(Actions));
    public static readonly DirectProperty<PageHeader, string> TitleUpperProperty =
        AvaloniaProperty.RegisterDirect<PageHeader, string>(nameof(TitleUpper), o => o.TitleUpper);

    public PageHeader()
    {
        InitializeComponent();
    }

    public string? Eyebrow { get => GetValue(EyebrowProperty); set => SetValue(EyebrowProperty, value); }
    public string Title { get => GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string? Count { get => GetValue(CountProperty); set => SetValue(CountProperty, value); }
    public string? CountLabel { get => GetValue(CountLabelProperty); set => SetValue(CountLabelProperty, value); }
    public object? Actions { get => GetValue(ActionsProperty); set => SetValue(ActionsProperty, value); }

    /// <summary>Título em caixa alta (estilo do design); o texto original vai no ToolTip.</summary>
    public string TitleUpper => Title.ToUpper(System.Globalization.CultureInfo.CurrentCulture);

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TitleProperty) RaisePropertyChanged(TitleUpperProperty, "", TitleUpper);
    }
}
