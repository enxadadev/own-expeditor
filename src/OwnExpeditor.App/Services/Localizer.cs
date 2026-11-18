using System;
using System.ComponentModel;
using System.Globalization;
using System.Resources;
using OwnExpeditor.Core;
using OwnExpeditor.Core.Games;
using OwnExpeditor.Core.Text;
using OwnExpeditor.Domain.Games;
using OwnExpeditor.Domain.Settings;

namespace OwnExpeditor.App.Services;

/// <summary>
/// Localizador sobre <c>Resources/Strings*.resx</c> (e <c>Gallery*.resx</c> em Debug). Views usam o indexer
/// (<c>{loc:Tr Chave}</c>); ao trocar de idioma dispara <c>PropertyChanged("Item[]")</c> e tudo se atualiza.
/// </summary>
public sealed class Localizer : ILocalizer
{
    private static readonly Lazy<Localizer> Shared = new(() => new Localizer(DetectLanguage(CultureInfo.CurrentUICulture), applyToProcess: true));

    private readonly ResourceManager[] _bundles;
    private readonly bool _applyToProcess;

    /// <summary>Instância do app (aplica a cultura ao processo; os testes criam a sua).</summary>
    public static Localizer Instance => Shared.Value;

    public Localizer(Language initial, bool applyToProcess = false)
    {
        var asm = typeof(Localizer).Assembly;
        _bundles =
        [
            new ResourceManager("OwnExpeditor.App.Resources.Strings", asm),
            new ResourceManager("OwnExpeditor.App.Resources.Gallery", asm), // ausente em Release
        ];
        _applyToProcess = applyToProcess;
        Language = initial;
        Culture = ToCulture(initial);
        Apply();
    }

    public Language Language { get; private set; }

    public CultureInfo Culture { get; private set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public event EventHandler? CultureChanged;

    public string this[string key] => Lookup(key) ?? $"!{key}!";

    /// <summary>pt-* → pt-BR; qualquer outra → en (padrão da primeira execução).</summary>
    public static Language DetectLanguage(CultureInfo culture) =>
        culture.TwoLetterISOLanguageName == "pt" ? Language.PtBr : Language.En;

    public static CultureInfo ToCulture(Language language) =>
        CultureInfo.GetCultureInfo(language == Language.PtBr ? "pt-BR" : "en");

    public string Get(string key, params object?[] args)
    {
        var text = this[key];
        return args.Length == 0 ? text : string.Format(Culture, text, args);
    }

    public string GetPlural(string baseKey, long count, params object?[] args)
    {
        var suffix = count == 0 && Lookup(baseKey + "_Zero") is not null ? "_Zero" : count == 1 ? "_One" : "_Other";
        return Get(baseKey + suffix, [count, .. args]);
    }

    public string Format(TextRef text) =>
        text.Count is { } count ? GetPlural(text.Key, count, [.. text.Args]) : Get(text.Key, [.. text.Args]);

    public string Format(DiffValue value, IGameNames? names = null) => value switch
    {
        DiffValue.Empty => Get("Diff_Value_Empty"),
        DiffValue.Raw r => r.Value,
        DiffValue.GameName g => names?.Get(g.Kind, g.Id) ?? GameIds.Humanize(g.Id),
        DiffValue.Number n => Get(n.FormatKey, n.Value),
        DiffValue.Money m => FormatMoney(m.Amount, m.Game),
        DiffValue.Localized l => Format(l.Text),
        _ => value.ToString() ?? string.Empty,
    };

    public string FormatError(CoreError code) => Get($"Error_{code}");

    public string FormatMoney(long amount, GameKind game) => Get($"Format_Money_{game}", amount);

    public void SetLanguage(Language language)
    {
        if (language == Language && Culture.Name == ToCulture(language).Name) return;
        Language = language;
        Culture = ToCulture(language);
        Apply();
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Culture)));
        CultureChanged?.Invoke(this, EventArgs.Empty);
    }

    private void Apply()
    {
        if (!_applyToProcess) return;
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = Culture;
        CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = Culture;
    }

    private string? Lookup(string key)
    {
        foreach (var bundle in _bundles)
        {
            try
            {
                if (bundle.GetString(key, Culture) is { } text) return text;
            }
            catch (MissingManifestResourceException)
            {
                // bundle fora do build (Gallery em Release): segue para o próximo
            }
        }
        return null;
    }
}
