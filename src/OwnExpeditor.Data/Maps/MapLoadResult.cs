using OwnExpeditor.Domain.Maps;

namespace OwnExpeditor.Data.Maps;

public sealed record MapLoadResult(MapStatus Status, MapIndex? Index, IReadOnlyList<string> SkippedMods);
