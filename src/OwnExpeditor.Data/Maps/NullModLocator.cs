using OwnExpeditor.Domain.Saves;

namespace OwnExpeditor.Data.Maps;

/// <summary>Fase 1a: sem suporte a mods.</summary>
public sealed class NullModLocator : IModLocator
{
    public IReadOnlyList<ModPackage> Active(ProfileInfo profile) => [];
}
