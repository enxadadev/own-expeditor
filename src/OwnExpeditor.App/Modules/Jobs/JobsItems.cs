using System;
using System.Windows.Input;
using OwnExpeditor.Core.Modules.Jobs;

namespace OwnExpeditor.App.Modules.Jobs;

/// <summary>Uma cidade de saída na lista (passo 1). A meta ("2 empresas · 4 vagas livres") é calculada só quando a linha aparece.</summary>
public sealed class CityItemViewModel(string id, string name, int pending, string pendingText, Func<(string Text, bool Dimmed)> meta)
{
    private (string Text, bool Dimmed)? _meta;

    public string Id { get; } = id;

    public string Name { get; } = name;

    public int Pending { get; } = pending;

    /// <summary>"1 pendente" / "n pendentes"; vazio sem pendências.</summary>
    public string PendingText { get; } = pendingText;

    public string Meta => (_meta ??= meta()).Text;

    /// <summary>Sem vaga livre: a meta fica esmaecida.</summary>
    public bool IsDimmed => (_meta ??= meta()).Dimmed;
}

/// <summary>Onde entra o frete (Usar vaga livre · n / Substituir uma oferta · n); a opção sem itens fica desativada.</summary>
public sealed record JobModeChoice(JobSlotMode Mode, string Label, bool IsEnabled);

/// <summary>Prazo (24 h, 48 h, 72 h, 7 d).</summary>
public sealed record JobExpirationChoice(int Hours, string Label);

/// <summary>Uma mudança pendente da cidade: título, detalhe, Editar e Descartar (cartão "Mudanças pendentes nesta cidade").</summary>
public sealed record PendingJobViewModel(string Key, string Title, string Detail, string EditAria, string DiscardAria,
    ICommand EditCommand, ICommand DiscardCommand);

/// <summary>Para onde levar o foco depois de um passo.</summary>
public enum JobsFocusTarget { CityList, ToCity, FromCompany, Cargo, ToCompany, Mode, Offer, Add }
