using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OwnExpeditor.Data.Distances;
using OwnExpeditor.Data.Saves;

namespace OwnExpeditor.Core.Modules.Jobs;

public static class JobsServiceCollectionExtensions
{
    /// <summary>Registra o módulo Fretes (precisa de <c>AddOwnExpeditorCore</c>, que traz a Data). Chamar de novo não duplica.</summary>
    public static IServiceCollection AddJobsModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton<IJobBoardFactory>(sp => new JobBoardFactory(
            sp.GetRequiredService<IDistanceCache>(), sp.GetRequiredService<IProfileLocator>()));
        return services;
    }
}
