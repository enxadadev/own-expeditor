using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OwnExpeditor.App.Services;
using OwnExpeditor.App.Shell;

namespace OwnExpeditor.App.ViewModels;

/// <summary>Um item da navegação lateral (módulo ou Configurações). Sem sessão, os módulos ficam desabilitados.</summary>
public sealed partial class NavItemViewModel : LocalizedViewModel
{
    private readonly string _titleKey;
    private readonly string? _shortKey;

    public NavItemViewModel(ILocalizer loc, string id, string titleKey, string iconKey, bool requiresSession, string? shortKey = null)
        : base(loc)
    {
        Id = id;
        IconKey = iconKey;
        RequiresSession = requiresSession;
        _titleKey = titleKey;
        _shortKey = shortKey;
    }

    public static NavItemViewModel For(ILocalizer loc, ModuleDescriptor d) => new(loc, d.Id, d.TitleKey, d.IconKey, d.RequiresSession);

    public string Id { get; }

    public string IconKey { get; }

    public bool RequiresSession { get; }

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(ToolTip))] private bool _isEnabled = true;
    [ObservableProperty] private bool _isActive;

    public string Title => Loc[_titleKey];

    /// <summary>Rótulo curto em caixa alta (cabe em 60 px).</summary>
    public string ShortTitle => Loc[_shortKey ?? _titleKey].ToUpper(Loc.Culture);

    public string ToolTip => IsEnabled ? Title : Loc["Nav_DisabledHint"];

    public IRelayCommand? Command { get; set; }
}
