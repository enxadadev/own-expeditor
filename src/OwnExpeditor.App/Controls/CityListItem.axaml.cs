using Avalonia;
using Avalonia.Controls;

namespace OwnExpeditor.App.Controls;

/// <summary>Item da lista de cidades de saída: nome, meta ("2 empresas · 4 vagas livres") e selo "n pendentes" (design/fretes §2).</summary>
public partial class CityListItem : UserControl
{
    public static readonly StyledProperty<string> TitleProperty = AvaloniaProperty.Register<CityListItem, string>(nameof(Title), "");
    public static readonly StyledProperty<string?> MetaProperty = AvaloniaProperty.Register<CityListItem, string?>(nameof(Meta));
    public static readonly StyledProperty<string?> PendingTextProperty = AvaloniaProperty.Register<CityListItem, string?>(nameof(PendingText));
    public static readonly StyledProperty<bool> IsDimmedProperty = AvaloniaProperty.Register<CityListItem, bool>(nameof(IsDimmed));

    public CityListItem() => InitializeComponent();

    public string Title { get => GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

    public string? Meta { get => GetValue(MetaProperty); set => SetValue(MetaProperty, value); }

    public string? PendingText { get => GetValue(PendingTextProperty); set => SetValue(PendingTextProperty, value); }

    /// <summary>Sem vaga livre: a meta fica a 80% de opacidade.</summary>
    public bool IsDimmed { get => GetValue(IsDimmedProperty); set => SetValue(IsDimmedProperty, value); }
}
