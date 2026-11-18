namespace OwnExpeditor.Core.Session;

public abstract record SavePatch(string UnitId);

public sealed record SetAttribute(string UnitId, string Attribute, string OldValue, string NewValue) : SavePatch(UnitId);
