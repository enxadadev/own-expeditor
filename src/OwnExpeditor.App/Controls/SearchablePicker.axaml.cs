using Avalonia.VisualTree;
using System.Collections;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using OwnExpeditor.App.Services;

namespace OwnExpeditor.App.Controls;

/// <summary>
/// Escolhe uma opção entre milhares, com busca e lista virtualizada (design/controles §2.12).
/// <see cref="Items"/> já vem filtrado pelo ViewModel a partir de <see cref="Query"/>.
/// </summary>
public partial class SearchablePicker : UserControl
{
    public static readonly StyledProperty<string> LabelProperty = AvaloniaProperty.Register<SearchablePicker, string>(nameof(Label), "");
    public static readonly StyledProperty<string> PlaceholderProperty = AvaloniaProperty.Register<SearchablePicker, string>(nameof(Placeholder), "");
    public static readonly StyledProperty<IEnumerable?> ItemsProperty = AvaloniaProperty.Register<SearchablePicker, IEnumerable?>(nameof(Items));
    public static readonly StyledProperty<PickerOption?> SelectedItemProperty = AvaloniaProperty.Register<SearchablePicker, PickerOption?>(nameof(SelectedItem), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);
    public static readonly StyledProperty<string> QueryProperty = AvaloniaProperty.Register<SearchablePicker, string>(nameof(Query), "", defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);
    public static readonly StyledProperty<int?> TotalCountProperty = AvaloniaProperty.Register<SearchablePicker, int?>(nameof(TotalCount));
    public static readonly StyledProperty<bool> IsLoadingProperty = AvaloniaProperty.Register<SearchablePicker, bool>(nameof(IsLoading));
    public static readonly StyledProperty<bool> AllowClearProperty = AvaloniaProperty.Register<SearchablePicker, bool>(nameof(AllowClear));
    public static readonly StyledProperty<bool> IsRequiredProperty = AvaloniaProperty.Register<SearchablePicker, bool>(nameof(IsRequired));
    public static readonly StyledProperty<string?> ErrorTextProperty = AvaloniaProperty.Register<SearchablePicker, string?>(nameof(ErrorText));
    public static readonly StyledProperty<string?> EmptyTextProperty = AvaloniaProperty.Register<SearchablePicker, string?>(nameof(EmptyText));
    public static readonly StyledProperty<bool> IsDropDownOpenProperty = AvaloniaProperty.Register<SearchablePicker, bool>(nameof(IsDropDownOpen), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<int> StepNumberProperty = AvaloniaProperty.Register<SearchablePicker, int>(nameof(StepNumber));
    public static readonly StyledProperty<bool> IsStepDoneProperty = AvaloniaProperty.Register<SearchablePicker, bool>(nameof(IsStepDone));
    public static readonly StyledProperty<string?> SearchWatermarkProperty = AvaloniaProperty.Register<SearchablePicker, string?>(nameof(SearchWatermark));
    public static readonly StyledProperty<string?> ToggleTextProperty = AvaloniaProperty.Register<SearchablePicker, string?>(nameof(ToggleText));
    public static readonly StyledProperty<bool> ToggleCheckedProperty = AvaloniaProperty.Register<SearchablePicker, bool>(nameof(ToggleChecked), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly DirectProperty<SearchablePicker, string> LabelUpperProperty = AvaloniaProperty.RegisterDirect<SearchablePicker, string>(nameof(LabelUpper), o => o.LabelUpper);
    public static readonly DirectProperty<SearchablePicker, string> DisplayTextProperty = AvaloniaProperty.RegisterDirect<SearchablePicker, string>(nameof(DisplayText), o => o.DisplayText);
    public static readonly DirectProperty<SearchablePicker, string> AccessibleNameProperty = AvaloniaProperty.RegisterDirect<SearchablePicker, string>(nameof(AccessibleName), o => o.AccessibleName);
    public static readonly DirectProperty<SearchablePicker, string?> CountTextProperty = AvaloniaProperty.RegisterDirect<SearchablePicker, string?>(nameof(CountText), o => o.CountText);
    public static readonly DirectProperty<SearchablePicker, string> EmptyTitleProperty = AvaloniaProperty.RegisterDirect<SearchablePicker, string>(nameof(EmptyTitle), o => o.EmptyTitle);
    public static readonly DirectProperty<SearchablePicker, bool> ShowClearProperty = AvaloniaProperty.RegisterDirect<SearchablePicker, bool>(nameof(ShowClear), o => o.ShowClear);
    public static readonly DirectProperty<SearchablePicker, bool> ShowListProperty = AvaloniaProperty.RegisterDirect<SearchablePicker, bool>(nameof(ShowList), o => o.ShowList);
    public static readonly DirectProperty<SearchablePicker, bool> ShowEmptyProperty = AvaloniaProperty.RegisterDirect<SearchablePicker, bool>(nameof(ShowEmpty), o => o.ShowEmpty);
    public static readonly DirectProperty<SearchablePicker, Thickness> ClearGapProperty = AvaloniaProperty.RegisterDirect<SearchablePicker, Thickness>(nameof(ClearGap), o => o.ClearGap);
    public static readonly DirectProperty<SearchablePicker, double> PopupWidthProperty = AvaloniaProperty.RegisterDirect<SearchablePicker, double>(nameof(PopupWidth), o => o.PopupWidth);

    private double _popupWidth = 360;

    public SearchablePicker()
    {
        InitializeComponent();
        PopupRoot.AddHandler(KeyDownEvent, OnPopupKeyDown, RoutingStrategies.Tunnel);
        Automation.Track(this, Refresh, LabelProperty, SelectedItemProperty, IsRequiredProperty, TotalCountProperty, ItemsProperty, QueryProperty);
    }

    public string Label { get => GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
    public string Placeholder { get => GetValue(PlaceholderProperty); set => SetValue(PlaceholderProperty, value); }
    public IEnumerable? Items { get => GetValue(ItemsProperty); set => SetValue(ItemsProperty, value); }
    public PickerOption? SelectedItem { get => GetValue(SelectedItemProperty); set => SetValue(SelectedItemProperty, value); }
    public string Query { get => GetValue(QueryProperty); set => SetValue(QueryProperty, value); }
    public int? TotalCount { get => GetValue(TotalCountProperty); set => SetValue(TotalCountProperty, value); }
    public bool IsLoading { get => GetValue(IsLoadingProperty); set => SetValue(IsLoadingProperty, value); }
    public bool AllowClear { get => GetValue(AllowClearProperty); set => SetValue(AllowClearProperty, value); }
    public bool IsRequired { get => GetValue(IsRequiredProperty); set => SetValue(IsRequiredProperty, value); }
    public string? ErrorText { get => GetValue(ErrorTextProperty); set => SetValue(ErrorTextProperty, value); }
    public string? EmptyText { get => GetValue(EmptyTextProperty); set => SetValue(EmptyTextProperty, value); }
    /// <summary>Chave opcional no alto do popup (ex.: "Só combinações possíveis"); o filtro em si é do ViewModel.</summary>
    /// <summary>Número do passo ao lado do rótulo (0 = sem número); <see cref="IsStepDone"/> pinta de âmbar.</summary>
    public int StepNumber { get => GetValue(StepNumberProperty); set => SetValue(StepNumberProperty, value); }
    public bool IsStepDone { get => GetValue(IsStepDoneProperty); set => SetValue(IsStepDoneProperty, value); }
    /// <summary>Texto da busca no popup; sem valor, o padrão do app.</summary>
    public string? SearchWatermark { get => GetValue(SearchWatermarkProperty); set => SetValue(SearchWatermarkProperty, value); }
    public string? ToggleText { get => GetValue(ToggleTextProperty); set => SetValue(ToggleTextProperty, value); }
    public bool ToggleChecked { get => GetValue(ToggleCheckedProperty); set => SetValue(ToggleCheckedProperty, value); }
    public bool IsDropDownOpen { get => GetValue(IsDropDownOpenProperty); set => SetValue(IsDropDownOpenProperty, value); }

    public string LabelUpper => Label.ToUpper(System.Globalization.CultureInfo.CurrentCulture);
    public string DisplayText => SelectedItem?.Primary ?? Placeholder;
    public string AccessibleName
    {
        get
        {
            var loc = Localizer.Instance;
            var name = loc.Get("Picker_Name_A11y", Label, SelectedItem?.Primary ?? loc["Picker_None"]);
            return IsRequired ? loc.Get("Picker_Required_A11y", name) : name;
        }
    }
    public string? CountText => TotalCount is { } total && ItemCount is { } n ? Localizer.Instance.Get("Picker_CountOf", n, total) : null;
    public string EmptyTitle => EmptyText ?? Localizer.Instance[string.IsNullOrEmpty(Query) ? "Picker_Empty" : "Picker_NoResults"];
    public bool ShowClear => AllowClear && SelectedItem is not null;
    public bool ShowList => !IsLoading && ItemCount > 0;
    public bool ShowEmpty => !IsLoading && ItemCount == 0;
    public Thickness ClearGap => new(0, 0, ShowClear ? 40 : 0, 0);
    public double PopupWidth => _popupWidth;

    private int? ItemCount => Items?.Cast<object>().Count();

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SearchWatermarkProperty && SearchWatermark is { } watermark) Search.Watermark = watermark;
        if (change.Property == SelectedItemProperty || change.Property == LabelProperty || change.Property == PlaceholderProperty
            || change.Property == TotalCountProperty || change.Property == ItemsProperty || change.Property == IsLoadingProperty
            || change.Property == AllowClearProperty || change.Property == QueryProperty || change.Property == EmptyTextProperty
            || change.Property == ErrorTextProperty || change.Property == IsRequiredProperty)
            Refresh();
    }

    private void Refresh()
    {
        RaisePropertyChanged(LabelUpperProperty, "", LabelUpper);
        RaisePropertyChanged(DisplayTextProperty, "", DisplayText);
        RaisePropertyChanged(AccessibleNameProperty, "", AccessibleName);
        RaisePropertyChanged(CountTextProperty, "", CountText);
        RaisePropertyChanged(EmptyTitleProperty, "", EmptyTitle);
        RaisePropertyChanged(ShowClearProperty, !ShowClear, ShowClear);
        RaisePropertyChanged(ClearGapProperty, default, ClearGap);
        RaisePropertyChanged(ShowListProperty, !ShowList, ShowList);
        RaisePropertyChanged(ShowEmptyProperty, !ShowEmpty, ShowEmpty);
        FieldButton.Classes.Set("selected", SelectedItem is not null);
        FieldButton.Classes.Set("error", !string.IsNullOrEmpty(ErrorText));
        ValueText.Classes.Set("placeholder", SelectedItem is null);
    }

    private void OnToggle(object? sender, RoutedEventArgs e) => IsDropDownOpen = !IsDropDownOpen;

    private void OnClear(object? sender, RoutedEventArgs e) => SelectedItem = null;

    private void OnOpened(object? sender, System.EventArgs e)
    {
        _popupWidth = System.Math.Clamp(System.Math.Max(Bounds.Width, 360), 360, 480);
        RaisePropertyChanged(PopupWidthProperty, 0, _popupWidth);
        OptionList.SelectedItem = SelectedItem is not null && Items?.Cast<object>().Contains(SelectedItem) == true ? SelectedItem : null;
        Search.Focus();
    }

    private void OnFieldKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Down && e.KeyModifiers.HasFlag(KeyModifiers.Alt)) { IsDropDownOpen = true; e.Handled = true; }
    }

    private void OnListPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if ((e.Source as Visual)?.GetSelfAndVisualAncestors().OfType<ListBoxItem>().FirstOrDefault() is { } item
            && OptionList.ItemFromContainer(item) is PickerOption { IsEnabled: true } option)
            Commit(option);
    }

    /// <summary>A escolha só vale com Enter ou clique: as setas apenas movem o realce.</summary>
    private void Commit(PickerOption option)
    {
        SelectedItem = option;
        Close();
    }

    private bool IsEnabledAt(int index) => Items?.Cast<object>().ElementAtOrDefault(index) is PickerOption { IsEnabled: true };

    /// <summary>↑/↓/PageUp/PageDown/Home/End movem o realce mesmo com o foco na busca; Enter fecha; Esc devolve o foco ao campo.</summary>
    private void OnPopupKeyDown(object? sender, KeyEventArgs e)
    {
        var count = ItemCount ?? 0;
        var index = OptionList.SelectedIndex;
        int? next = e.Key switch
        {
            Key.Down => index + 1,
            Key.Up => index - 1,
            Key.PageDown => index + 8,
            Key.PageUp => index - 8,
            Key.Home => 0,
            Key.End => count - 1,
            _ => null,
        };
        if (next is { } n && count > 0)
        {
            // linhas desativadas ficam fora das setas
            var step = n >= index ? 1 : -1;
            var target = System.Math.Clamp(n, 0, count - 1);
            while (target >= 0 && target < count && !IsEnabledAt(target)) target += step;
            if (target >= 0 && target < count)
            {
                OptionList.SelectedIndex = target;
                OptionList.ScrollIntoView(target);
            }
            e.Handled = true;
        }
        else if (e.Key == Key.Enter && OptionList.SelectedItem is PickerOption { IsEnabled: true } chosen) { Commit(chosen); e.Handled = true; }
        else if (e.Key == Key.Escape) { Close(); e.Handled = true; }
        else if (e.Key == Key.Tab) { IsDropDownOpen = false; }
    }

    /// <summary>Põe o foco no campo (próximo passo incompleto), sem abrir o popup.</summary>
    public void FocusField() => FieldButton.Focus();

    private void Close()
    {
        IsDropDownOpen = false;
        FieldButton.Focus();
    }
}
