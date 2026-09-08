using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Monq.Core.InstanceCoordination.Configuration;
using Monq.Core.InstanceCoordination.DependencyInjection;
using Monq.Core.InstanceCoordination.Services;
using Xunit;

namespace Monq.Core.InstanceCoordination.Tests.DependencyInjection;

public sealed class InstancesServiceCollectionExtensionsTests
{
    [Fact]
    public void AddInstances_RegistersInstanceServices()
    {
        var services = CreateServices();

        services.AddInstances();

        var instanceService = Assert.Single(services, x => x.ServiceType == typeof(InstanceService));
        Assert.Equal(ServiceLifetime.Singleton, instanceService.Lifetime);
        Assert.Contains(services, x =>
            x.ServiceType == typeof(IHostedService)
            && x.ImplementationType == typeof(InstanceHostedService));
    }

    [Fact]
    public void AddInstances_AppliesSetupAction()
    {
        var services = CreateServices();
        services.AddInstances(options =>
        {
            options.AppName = "test-app";
            options.HeartbeatInterval = TimeSpan.FromSeconds(2);
            options.LeaseTtl = TimeSpan.FromSeconds(10);
        });

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<InstanceOptions>>().Value;

        Assert.Equal("test-app", options.AppName);
        Assert.Equal(TimeSpan.FromSeconds(2), options.HeartbeatInterval);
        Assert.Equal(TimeSpan.FromSeconds(10), options.LeaseTtl);
    }

    [Fact]
    public void AddInstances_RejectsNonPositiveHeartbeatInterval()
    {
        var services = CreateServices();
        services.AddInstances(options => options.HeartbeatInterval = TimeSpan.Zero);

        using var provider = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IOptions<InstanceOptions>>().Value);
    }

    [Fact]
    public void AddInstances_RejectsNonPositiveLeaseTtl()
    {
        var services = CreateServices();
        services.AddInstances(options => options.LeaseTtl = TimeSpan.Zero);

        using var provider = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IOptions<InstanceOptions>>().Value);
    }

    static ServiceCollection CreateServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        return services;
    }
}
