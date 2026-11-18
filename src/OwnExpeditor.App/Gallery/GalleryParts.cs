using Avalonia;
using Avalonia.Controls;

namespace OwnExpeditor.App.Gallery;

/// <summary>Um exemplo com legenda do estado embaixo.</summary>
public class GalleryCell : ContentControl
{
    public static readonly StyledProperty<string?> CaptionProperty = AvaloniaProperty.Register<GalleryCell, string?>(nameof(Caption));

    public string? Caption { get => GetValue(CaptionProperty); set => SetValue(CaptionProperty, value); }
}

/// <summary>Seção de um controle: título (nome da classe), descrição e a grade de exemplos.</summary>
public class GallerySection : ContentControl
{
    public static readonly StyledProperty<string?> TitleProperty = AvaloniaProperty.Register<GallerySection, string?>(nameof(Title));
    public static readonly StyledProperty<string?> DescriptionProperty = AvaloniaProperty.Register<GallerySection, string?>(nameof(Description));

    public string? Title { get => GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string? Description { get => GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }
}
