namespace OwnExpeditor.App.Controls;

/// <summary>Item do <see cref="SearchablePicker"/>. Textos já traduzidos; <see cref="Key"/> identifica o dado. <see cref="Trailing"/> é um texto curto alinhado à direita (ex.: distância). <see cref="LeadingIcon"/> (chave <c>IconTrailer*</c>, decorativa; fora da busca) vai num chip à esquerda com selo <see cref="LeadingBadge"/> (×N, se &gt; 1); <see cref="ReserveLeading"/> mantém o chip vazio quando não há ícone, para alinhar as linhas.</summary>
public sealed record PickerOption(object Key, string Primary, string? Secondary = null, string? Tag = null, bool IsEnabled = true, string? Trailing = null,
    string? LeadingIcon = null, int LeadingBadge = 1, bool ReserveLeading = false);
