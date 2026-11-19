using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using OwnExpeditor.Core;
using OwnExpeditor.Core.Games;
using OwnExpeditor.Core.Text;
using OwnExpeditor.Domain.Games;
using OwnExpeditor.Domain.Settings;

namespace OwnExpeditor.App.Services;

/// <summary>Textos da UI por chave do .resx, com troca de idioma em runtime (skill i18n-theming).</summary>
public interface ILocalizer : INotifyPropertyChanged
{
    /// <summary>Cultura atual do app (usada em todos os formatos de número e data).</summary>
    CultureInfo Culture { get; }

    /// <summary>Texto da chave; chave inexistente devolve <c>!Chave!</c> (visível, nunca lança).</summary>
    string this[string key] { get; }

    /// <summary>Busca sem o marcador <c>!Chave!</c>: false se a chave não existe.</summary>
    bool TryGet(string key, [NotNullWhen(true)] out string? value);

    string Get(string key, params object?[] args);

    /// <summary>
    /// Escolhe <c>base_Zero</c> (só se existir e count = 0), <c>base_One</c> (count = 1) ou <c>base_Other</c>.
    /// O primeiro argumento do formato é sempre o count; <paramref name="args"/> vêm depois.
    /// </summary>
    string GetPlural(string baseKey, long count, params object?[] args);

    string Format(TextRef text);

    /// <summary>Valor de diff por tipo; nomes do jogo vêm de <paramref name="names"/> (sem ele, id legível).</summary>
    string Format(DiffValue value, IGameNames? names = null);

    /// <summary>Texto de <c>Error_&lt;Code&gt;</c>.</summary>
    string FormatError(CoreError code);

    /// <summary>Dinheiro: moeda fixa por jogo (EUR no ETS2, USD no ATS), número pela cultura do app.</summary>
    string FormatMoney(long amount, GameKind game);

    void SetLanguage(Language language);

    event EventHandler? CultureChanged;
}
