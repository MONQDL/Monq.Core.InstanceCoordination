using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Monq.Core.InstanceCoordination.Configuration;
using Monq.Core.InstanceCoordination.Services;

namespace Monq.Core.InstanceCoordination.DependencyInjection;

/// <summary>
/// Extension methods for registering application instance services.
/// </summary>
public static class InstancesServiceCollectionExtensions
{
    /// <summary>
    /// Adds application instance registration and work distribution services.
    /// </summary>
    /// <param name="services">The application service collection.</param>
    /// <returns>The application service collection.</returns>
    public static IServiceCollection AddInstances(this IServiceCollection services)
        => AddInstances(services, _ => { });

    /// <summary>
    /// Adds application instance registration and work distribution services.
    /// </summary>
    /// <param name="services">The application service collection.</param>
    /// <param name="setupAction">An <see cref="Action{InstanceOptions}"/> used to configure <see cref="InstanceOptions"/>.</param>
    /// <returns>The application service collection.</returns>
    public static IServiceCollection AddInstances(
        this IServiceCollection services,
        Action<InstanceOptions> setupAction)
    {
        services.AddOptions<InstanceOptions>()
            .BindConfiguration(nameof(InstanceOptions))
            .Validate(x => x.HeartbeatInterval > TimeSpan.Zero)
            .Validate(x => x.LeaseTtl > TimeSpan.Zero)
            .ValidateOnStart();
        services.TryAddSingleton<InstanceService>();
        services.AddHostedService<InstanceHostedService>();
        services.Configure(setupAction);

        return services;
    }
}
