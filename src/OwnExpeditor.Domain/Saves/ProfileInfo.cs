using OwnExpeditor.Domain.Games;

namespace OwnExpeditor.Domain.Saves;

/// <summary>Perfil do jogador. <c>Id</c> é o nome da pasta (hex do nome); <c>DisplayName</c> é o nome decodificado.</summary>
public sealed record ProfileInfo(GameKind Game, string Id, string DisplayName, string DirectoryPath, bool IsSteamCloud);
