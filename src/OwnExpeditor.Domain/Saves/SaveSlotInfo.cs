namespace OwnExpeditor.Domain.Saves;

public sealed record SaveSlotInfo(ProfileInfo Profile, string Id, string DisplayName, string GameSiiPath,
                                  DateTime LastWriteUtc, long SizeBytes);
