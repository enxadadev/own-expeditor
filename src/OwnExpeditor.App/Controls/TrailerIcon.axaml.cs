using System;
using Avalonia;
using Avalonia.Controls;
using OwnExpeditor.App.Services;

namespace OwnExpeditor.App.Controls;

/// <summary>
/// Ícone decorativo de reboque (design/nomes-reboques §7.7): geometria <c>IconTrailer*</c> por chave, traço por tamanho (16/20/24),
/// cor do tema (<c>Foreground</c>, padrão TextMutedBrush) e selo ×N só a partir de 20 px. Invisível para leitor de tela.
/// </summary>
public partial class TrailerIcon : UserControl
{
    public static readonly StyledProperty<string?> IconKeyProperty = AvaloniaProperty.Register<TrailerIcon, string?>(nameof(IconKey));
    public static readonly StyledProperty<int> UnitsProperty = AvaloniaProperty.Register<TrailerIcon, int>(nameof(Units), 1);
    public static readonly StyledProperty<double> IconSizeProperty = AvaloniaProperty.Register<TrailerIcon, double>(nameof(IconSize), 16);
    public static readonly StyledProperty<bool> HasChipProperty = AvaloniaProperty.Register<TrailerIcon, bool>(nameof(HasChip));
    public static readonly DirectProperty<TrailerIcon, double> HostSizeProperty = AvaloniaProperty.RegisterDirect<TrailerIcon, double>(nameof(HostSize), o => o.HostSize);
    public static readonly DirectProperty<TrailerIcon, double> StrokeWidthProperty = AvaloniaProperty.RegisterDirect<TrailerIcon, double>(nameof(StrokeWidth), o => o.StrokeWidth);
    public static readonly DirectProperty<TrailerIcon, bool> ShowBadgeProperty = AvaloniaProperty.RegisterDirect<TrailerIcon, bool>(nameof(ShowBadge), o => o.ShowBadge);
    public static readonly DirectProperty<TrailerIcon, bool> ShowChipBackgroundProperty = AvaloniaProperty.RegisterDirect<TrailerIcon, bool>(nameof(ShowChipBackground), o => o.ShowChipBackground);
    public static readonly DirectProperty<TrailerIcon, string> BadgeTextProperty = AvaloniaProperty.RegisterDirect<TrailerIcon, string>(nameof(BadgeText), o => o.BadgeText);

    public TrailerIcon()
    {
        InitializeComponent();
        AttachedToVisualTree += (_, _) => { _attached = true; Refresh(); };
        DetachedFromVisualTree += (_, _) => { _attached = false; SyncSubscription(); };
    }

    /// <summary>Chave <c>IconTrailer*</c>; nulo não desenha (e, sem chip, o controle ocupa 0 × 0).</summary>
    public string? IconKey { get => GetValue(IconKeyProperty); set => SetValue(IconKeyProperty, value); }

    /// <summary>Reboques da cadeia; &gt; 1 mostra o selo ×N em 20/24 px.</summary>
    public int Units { get => GetValue(UnitsProperty); set => SetValue(UnitsProperty, value); }

    /// <summary>16, 20 ou 24.</summary>
    public double IconSize { get => GetValue(IconSizeProperty); set => SetValue(IconSizeProperty, value); }

    /// <summary>Chip 32 × 32 (seletor): sem chave, o chip fica vazio e só reserva o espaço.</summary>
    public bool HasChip { get => GetValue(HasChipProperty); set => SetValue(HasChipProperty, value); }

    public double HostSize => HasChip ? 32 : string.IsNullOrEmpty(IconKey) ? 0 : IconSize;
    public bool ShowBadge => !string.IsNullOrEmpty(IconKey) && Units > 1 && IconSize >= 20;
    public bool ShowChipBackground => HasChip && !string.IsNullOrEmpty(IconKey);
    public string BadgeText => ShowBadge ? Localizer.Instance.Get("Trailer_Badge", Units) : "";

    /// <summary>Traço em px reais (24 → 1,8; 20 → 1,6; 16 → 1,4), compensado pela escala do Viewbox (tamanho / 24).</summary>
    public double StrokeWidth => (IconSize switch { >= 24 => 1.8, >= 20 => 1.6, _ => 1.4 }) * 24 / IconSize;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IconKeyProperty || change.Property == HasChipProperty || change.Property == UnitsProperty || change.Property == IconSizeProperty)
        {
            Refresh();
        }
    }

    private void OnCultureChanged(object? sender, EventArgs e)
    {
        if (ShowBadge) RaisePropertyChanged(BadgeTextProperty, "", BadgeText);
    }

    private bool _hadBadge;
    private bool _attached;
    private bool _subscribed;

    /// <summary>Só escuta a troca de idioma enquanto há selo visível na árvore.</summary>
    private void SyncSubscription()
    {
        var want = _attached && ShowBadge;
        if (want == _subscribed) return;
        _subscribed = want;
        if (want) Localizer.Instance.CultureChanged += OnCultureChanged;
        else Localizer.Instance.CultureChanged -= OnCultureChanged;
    }

    private void Refresh()
    {
        var showBadge = ShowBadge;
        RaisePropertyChanged(HostSizeProperty, 0d, HostSize);
        RaisePropertyChanged(StrokeWidthProperty, 0d, StrokeWidth);
        RaisePropertyChanged(ShowChipBackgroundProperty, !ShowChipBackground, ShowChipBackground);
        if (showBadge || _hadBadge)
        {
            RaisePropertyChanged(ShowBadgeProperty, !showBadge, showBadge);
            RaisePropertyChanged(BadgeTextProperty, "", BadgeText);
        }
        _hadBadge = showBadge;
        SyncSubscription();
    }
}
