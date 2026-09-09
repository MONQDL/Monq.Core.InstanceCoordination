using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monq.Core.InstanceCoordination.Configuration;

namespace Monq.Core.InstanceCoordination.Services;

/// <summary>
/// A background service that maintains the application instance registration.
/// </summary>
sealed class InstanceHostedService : BackgroundService
{
    readonly InstanceService _instances;
    readonly IInstanceOperation[] _operations;
    readonly ILogger<InstanceHostedService> _logger;
    readonly TimeSpan _heartbeatInterval;

    /// <summary>
    /// Initializes the application instance registration background service.
    /// </summary>
    public InstanceHostedService(
        InstanceService instances,
        IEnumerable<IInstanceOperation> operations,
        ILogger<InstanceHostedService> logger,
        IOptions<InstanceOptions> options)
    {
        _instances = instances;
        _operations = [.. operations];
        _logger = logger;
        _heartbeatInterval = options.Value.HeartbeatInterval;
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Heartbeat(stoppingToken);
            await Task.Delay(_heartbeatInterval, stoppingToken);
        }
    }

    async Task Heartbeat(CancellationToken stoppingToken)
    {
        await Heartbeat(_instances.Heartbeat, stoppingToken);

        foreach (var operation in _operations)
            await Heartbeat(operation.Heartbeat, stoppingToken);
    }

    async Task Heartbeat(Func<Task<bool>> heartbeat, CancellationToken stoppingToken)
    {
        try
        {
            if (await heartbeat())
                return;
        }
        catch (Exception e) when (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogError(e, "Error while updating instance heartbeat.");
            return;
        }

        _logger.LogCritical("Instance heartbeat was rejected. Application cannot continue.");
        throw new InvalidOperationException("Instance heartbeat was rejected.");
    }

    /// <inheritdoc/>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);

        foreach (var operation in _operations.Reverse())
            await Release(operation.Release, cancellationToken);

        await Release(_instances.Release, cancellationToken);
    }

    async Task Release(Func<Task> release, CancellationToken cancellationToken)
    {
        try
        {
            await release();
        }
        catch (Exception e) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(e, "Error while releasing instance resource.");
        }
    }
}
