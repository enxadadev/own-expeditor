using OwnExpeditor.Core.Text;

namespace OwnExpeditor.Core;

public sealed class CoreException(CoreError code, string? detail = null, Exception? inner = null)
    : Exception(detail ?? code.ToString(), inner)
{
    public CoreError Code { get; } = code;

    /// <summary>Motivo localizável (ex.: <c>Jobs_Error_*</c> em <see cref="CoreError.InvalidEdit"/>).</summary>
    public TextRef? Reason { get; init; }

    /// <summary>Etapa de carga em que falhou (<c>LoadStages</c>).</summary>
    public string? StageKey { get; init; }

    public int? Line { get; init; }
}
