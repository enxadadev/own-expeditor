using OwnExpeditor.Core;
using OwnExpeditor.Core.Session;

namespace OwnExpeditor.App.ViewModels;

public enum ApplyOutcomeKind { Cancelled, Applied, ReopenSave, ChooseSave }

/// <summary>Como terminou o diálogo de confirmação.</summary>
public sealed record ApplyOutcome(ApplyOutcomeKind Kind, ApplyResult? Result = null, ApplyTarget? Target = null);

/// <summary>Ações possíveis de um erro ao aplicar (design/review-apply, tabela "Erros por CoreError").</summary>
public enum ErrorAction { Retry, RetryGameClosed, BackToReview, OpenReview, ReopenSave, ChooseSave, OpenFolder, Close }

public enum PendingNote { None, Kept, Lost }

public sealed record ApplyErrorPlan(ErrorAction Primary, ErrorAction? Secondary, PendingNote Pending);

/// <summary>Tabela de erros ao aplicar: primária, secundária e o que acontece com as pendências.</summary>
public static class ApplyErrorMap
{
    public static ApplyErrorPlan For(CoreError code) => code switch
    {
        CoreError.SaveNotFound => new(ErrorAction.ChooseSave, null, PendingNote.None),
        CoreError.NoSession => new(ErrorAction.ChooseSave, null, PendingNote.None),
        CoreError.SessionBusy => new(ErrorAction.Retry, ErrorAction.BackToReview, PendingNote.Kept),
        CoreError.ReadOnlySession => new(ErrorAction.Close, null, PendingNote.Kept),
        CoreError.Conflict => new(ErrorAction.OpenReview, ErrorAction.Close, PendingNote.Kept),
        CoreError.InvalidEdit => new(ErrorAction.BackToReview, null, PendingNote.Kept),
        CoreError.StalePatch or CoreError.UnknownUnit or CoreError.UnknownAttribute
            => new(ErrorAction.BackToReview, ErrorAction.ReopenSave, PendingNote.Kept),
        CoreError.NothingToApply => new(ErrorAction.Close, null, PendingNote.None),
        CoreError.GameRunning => new(ErrorAction.RetryGameClosed, ErrorAction.BackToReview, PendingNote.Kept),
        CoreError.SaveChangedOnDisk => new(ErrorAction.ReopenSave, null, PendingNote.Lost),
        CoreError.BackupExists => new(ErrorAction.Retry, null, PendingNote.Kept),
        CoreError.BackupFailed or CoreError.WriteFailed or CoreError.VerifyFailed or CoreError.SlotUnavailable
            => new(ErrorAction.Retry, ErrorAction.BackToReview, PendingNote.Kept),
        CoreError.AccessDenied => new(ErrorAction.Retry, ErrorAction.OpenFolder, PendingNote.Kept),
        _ => new(ErrorAction.Close, null, PendingNote.None),
    };
}
