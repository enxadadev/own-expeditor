using System.Windows.Input;

namespace OwnExpeditor.App.Modules.Jobs;

/// <summary>Prazo (24 h, 48 h, 72 h, 7 d).</summary>
public sealed record JobExpirationChoice(int Hours, string Label);

/// <summary>Uma mudança pendente da cidade: título, detalhe, Editar e Descartar (cartão "Mudanças pendentes nesta cidade").</summary>
public sealed record PendingJobViewModel(string Key, string Title, string Detail, string EditAria, string DiscardAria,
    ICommand EditCommand, ICommand DiscardCommand);

/// <summary>Para onde levar o foco depois de um passo (os 8 campos, o ⇄ e a ação).</summary>
public enum JobsFocusTarget
{
    FromCountry, FromCity, FromCompany, Slot, Swap, ToCountry, ToCity, ToCompany, Cargo, Add
}
