using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using OwnExpeditor.App.Controls;

namespace OwnExpeditor.App.Modules.Jobs;

/// <summary>
/// Estado de um <c>SearchablePicker</c> do módulo: opções, busca sem acento (feita aqui, não na View), seleção, dica e
/// bloqueio. <see cref="Badge"/> é a etiqueta ("Única opção" / "Travado na edição"). A seleção do usuário vira o evento <see cref="Chosen"/>; mudar <see cref="Selected"/> por código não dispara nada.
/// </summary>
public sealed partial class JobPickerViewModel : ObservableObject
{
    private IReadOnlyList<PickerOption> _all = [];
    private PickerOption? _selected;
    private bool _syncing;

    [ObservableProperty] private string _label = "";
    [ObservableProperty] private string _placeholder = "";
    [ObservableProperty] private string? _searchWatermark;
    [ObservableProperty] private string? _hint;
    [ObservableProperty] private bool _isEnabled = true;
    [ObservableProperty] private bool _isOpen;
    [ObservableProperty] private IReadOnlyList<PickerOption> _items = [];
    [ObservableProperty] private string _query = "";
    [ObservableProperty] private bool _isLocked;
    [ObservableProperty] private bool _isSingleOption;
    [ObservableProperty] private string? _badge;
    [ObservableProperty] private string _automationName = "";

    /// <summary>O usuário escolheu uma opção (Enter ou clique); nunca vem nula nem desativada.</summary>
    public event EventHandler<PickerOption>? Chosen;

    public IReadOnlyList<PickerOption> AllOptions => _all;

    public int Count => _all.Count(o => o.IsEnabled);

    public string? EmptyText { get; set; }

    public string? EffectiveEmptyText => string.IsNullOrEmpty(Query) ? EmptyText : null;

    /// <summary>A opção marcada. O controle devolve <c>null</c> quando o filtro a esconde: isso nunca é uma escolha.</summary>
    public PickerOption? Selected
    {
        get => _selected;
        set
        {
            if (value is null || value.Equals(_selected)) return;
            if (!value.IsEnabled) return;
            _selected = value;
            OnPropertyChanged();
            if (!_syncing) Chosen?.Invoke(this, value);
        }
    }

    partial void OnQueryChanged(string value)
    {
        ApplyFilter();
        OnPropertyChanged(nameof(EffectiveEmptyText));
    }

    /// <summary>Troca as opções e a seleção (por <c>Key</c>) sem disparar <see cref="Chosen"/>.</summary>
    public void Set(IReadOnlyList<PickerOption> options, object? selectedKey)
    {
        _syncing = true;
        try
        {
            _all = options;
            var match = selectedKey is null ? null : options.FirstOrDefault(o => Equals(o.Key, selectedKey));
            _selected = match;
            OnPropertyChanged(nameof(Selected));
            OnPropertyChanged(nameof(AllOptions));
            OnPropertyChanged(nameof(Count));
            ApplyFilter();
        }
        finally
        {
            _syncing = false;
        }
    }

    private void ApplyFilter()
    {
        var query = Normalize(Query);
        Items = query.Length == 0
            ? _all.ToList()
            : _all.Where(o => Normalize(o.Primary).Contains(query, StringComparison.Ordinal)
                || (o.Secondary is { } s && Normalize(s).Contains(query, StringComparison.Ordinal))).ToList();
    }

    /// <summary>Minúsculas e sem acentos: "Sófia" casa com "sofia".</summary>
    public static string Normalize(string? text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        var sb = new StringBuilder(text.Length);
        foreach (var ch in text.Normalize(NormalizationForm.FormD))
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark) sb.Append(char.ToLowerInvariant(ch));
        return sb.ToString();
    }
}
