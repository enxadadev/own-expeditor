using OwnExpeditor.Domain.Games;

namespace OwnExpeditor.Core.Games;

public enum GameFolderSource { Detected, Manual, NotFound }

public sealed record GameFolderStatus(GameKind Game, string? Path, GameFolderSource Source);
