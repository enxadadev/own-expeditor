using System.Globalization;
using Avalonia.Data.Converters;
using OwnExpeditor.App.Services;

namespace OwnExpeditor.App.Controls;

/// <summary>Conversores só de formatação visual para os templates dos controles.</summary>
public static class Converters
{
    /// <summary>Caixa alta pela cultura atual (rótulos Caption/Title do design).</summary>
    public static readonly FuncValueConverter<string?, string?> Upper = new(s => s?.ToUpper(CultureInfo.CurrentCulture));

    /// <summary>Item desabilitado a 40 % (convenção de estados).</summary>
    public static readonly FuncValueConverter<bool, double> DisabledOpacity = new(enabled => enabled ? 1.0 : 0.4);

    /// <summary>Nome acessível do botão Descartar de um grupo ("Descartar mudança: {Title}").</summary>
    public static readonly FuncValueConverter<string?, string?> DiscardName =
        new(title => Localizer.Instance.Get("Diff_Discard_A11y", title ?? ""));
}
