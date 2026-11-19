using System.Collections;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using OwnExpeditor.App.Services;

namespace OwnExpeditor.App.Controls;

public interface IDiffLine
{
    string Label { get; }
    string? Before { get; }
    string? After { get; }
    bool IsNumeric { get; }

    /// <summary>Chaves <c>IconTrailer*</c> de cada lado (só a linha Reboque); nulo = sem ícone. Só decoração.</summary>
    string? BeforeIcon => null;
    string? AfterIcon => null;
}

/// <summary>Um grupo = um ChangeSet (módulo, título, linhas, descartar). Textos já traduzidos/formatados.</summary>
public interface IDiffGroup
{
    string ModuleName { get; }
    string Title { get; }
    System.Collections.Generic.IReadOnlyList<IDiffLine> Lines { get; }
    ICommand? DiscardCommand { get; }
}

/// <summary>Valor simples para amostras e VMs que não precisam de mais.</summary>
public sealed record DiffLine(string Label, string? Before, string? After, bool IsNumeric = false, string? BeforeIcon = null, string? AfterIcon = null) : IDiffLine;

public sealed record DiffGroup(string ModuleName, string Title, System.Collections.Generic.IReadOnlyList<IDiffLine> Lines, ICommand? DiscardCommand = null) : IDiffGroup;

/// <summary>Lista agrupada das mudanças pendentes (design/controles §2.9).</summary>
public partial class DiffList : UserControl
{
    public static readonly StyledProperty<IEnumerable?> GroupsProperty = AvaloniaProperty.Register<DiffList, IEnumerable?>(nameof(Groups));
    public static readonly StyledProperty<object?> EmptyContentProperty = AvaloniaProperty.Register<DiffList, object?>(nameof(EmptyContent));
    public static readonly StyledProperty<bool> IsDiscardEnabledProperty = AvaloniaProperty.Register<DiffList, bool>(nameof(IsDiscardEnabled), true);
    public static readonly DirectProperty<DiffList, bool> IsEmptyProperty =
        AvaloniaProperty.RegisterDirect<DiffList, bool>(nameof(IsEmpty), o => o.IsEmpty);

    public DiffList()
    {
        InitializeComponent();
    }

    public IEnumerable? Groups { get => GetValue(GroupsProperty); set => SetValue(GroupsProperty, value); }

    /// <summary>Mostrado sem grupos; o padrão é um EmptyState "Nenhuma mudança pendente".</summary>
    public object? EmptyContent { get => GetValue(EmptyContentProperty); set => SetValue(EmptyContentProperty, value); }
    public bool IsDiscardEnabled { get => GetValue(IsDiscardEnabledProperty); set => SetValue(IsDiscardEnabledProperty, value); }

    public static readonly DirectProperty<DiffList, bool> ShowCustomEmptyProperty =
        AvaloniaProperty.RegisterDirect<DiffList, bool>(nameof(ShowCustomEmpty), o => o.ShowCustomEmpty);
    public static readonly DirectProperty<DiffList, bool> ShowDefaultEmptyProperty =
        AvaloniaProperty.RegisterDirect<DiffList, bool>(nameof(ShowDefaultEmpty), o => o.ShowDefaultEmpty);

    public bool ShowCustomEmpty => IsEmpty && EmptyContent is not null;
    public bool ShowDefaultEmpty => IsEmpty && EmptyContent is null;

    public bool IsEmpty => Groups is null || !Groups.Cast<object>().Any();

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == GroupsProperty || change.Property == EmptyContentProperty)
        {
            RaisePropertyChanged(IsEmptyProperty, !IsEmpty, IsEmpty);
            RaisePropertyChanged(ShowCustomEmptyProperty, !ShowCustomEmpty, ShowCustomEmpty);
            RaisePropertyChanged(ShowDefaultEmptyProperty, !ShowDefaultEmpty, ShowDefaultEmpty);
        }
    }
}
