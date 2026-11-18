using OwnExpeditor.App.Services;

namespace OwnExpeditor.App.ViewModels;

/// <summary>Opção de um grupo exclusivo (SegmentedChoice); o rótulo acompanha o idioma.</summary>
public sealed class ChoiceItemViewModel(ILocalizer loc, object value, string labelKey) : LocalizedViewModel(loc)
{
    public object Value { get; } = value;

    public string Label => Loc[labelKey];
}
