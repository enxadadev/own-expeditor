using System;
using Microsoft.Extensions.DependencyInjection;
using OwnExpeditor.App.ViewModels;

namespace OwnExpeditor.App.Shell;

/// <summary>Um módulo na navegação lateral. <c>CreatePage</c> monta a página do módulo (uma vez por abertura do app).</summary>
public sealed record ModuleDescriptor(string Id, string TitleKey, string IconKey, int Order,
    Func<IServiceProvider, ModulePageViewModel> CreatePage, bool RequiresSession = true);

public static class ModuleRegistration
{
    /// <summary>Registra um módulo (o módulo Fretes entra assim na F7).</summary>
    public static IServiceCollection AddModule(this IServiceCollection services, string id, string titleKey,
        string iconKey, int order, Func<IServiceProvider, ModulePageViewModel> createPage, bool requiresSession = true) =>
        services.AddSingleton(new ModuleDescriptor(id, titleKey, iconKey, order, createPage, requiresSession));
}
