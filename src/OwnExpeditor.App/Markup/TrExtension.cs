using System;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.MarkupExtensions;
using OwnExpeditor.App.Services;

namespace OwnExpeditor.App.Markup;

/// <summary>
/// <c>{loc:Tr Chave}</c>: binding para o indexer do <see cref="Localizer"/>; atualiza ao trocar de idioma.
/// </summary>
public sealed class TrExtension(string key) : MarkupExtension
{
    public string Key { get; set; } = key;

    /// <summary>Caixa alta pela cultura atual (rótulos Caption).</summary>
    public bool Upper { get; set; }

    public override object ProvideValue(IServiceProvider serviceProvider) =>
        new ReflectionBindingExtension($"[{Key}]")
        {
            Mode = BindingMode.OneWay,
            Source = Localizer.Instance,
            Converter = Upper ? Controls.Converters.Upper : null,
        }
            .ProvideValue(serviceProvider);
}
