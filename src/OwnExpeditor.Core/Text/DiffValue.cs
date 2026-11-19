using OwnExpeditor.Domain.Games;

namespace OwnExpeditor.Core.Text;

public abstract record DiffValue
{
    public sealed record Empty : DiffValue;
    public sealed record Raw(string Value) : DiffValue;
    public sealed record GameName(GameNameKind Kind, string Id) : DiffValue;
    public sealed record Trailer(string Definition, string? Variant) : DiffValue;
    public sealed record Number(decimal Value, string FormatKey) : DiffValue;
    public sealed record Money(long Amount, GameKind Game) : DiffValue;
    public sealed record Localized(TextRef Text) : DiffValue;
}
