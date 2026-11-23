namespace OwnExpeditor.Domain.Maps;

/// <summary>País (def/country): âncora do rótulo em metros do mundo.</summary>
public sealed record MapCountry(string Token, string? Name, MapPos Pos);
