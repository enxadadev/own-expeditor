using OwnExpeditor.Domain.Games;

namespace OwnExpeditor.Data.Maps;

public sealed record MapRequest(GameKind Game, string InstallDir, IReadOnlyList<ModPackage> Mods, MapDetail Detail);
