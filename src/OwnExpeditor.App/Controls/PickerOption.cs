namespace OwnExpeditor.App.Controls;

/// <summary>Item do <see cref="SearchablePicker"/>. Textos já traduzidos; <see cref="Key"/> identifica o dado. <see cref="Trailing"/> é um texto curto alinhado à direita (ex.: distância).</summary>
public sealed record PickerOption(object Key, string Primary, string? Secondary = null, string? Tag = null, bool IsEnabled = true, string? Trailing = null);
