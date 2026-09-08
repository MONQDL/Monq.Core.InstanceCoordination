using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Monq.Core.InstanceCoordination.Configuration;
using Monq.Core.InstanceCoordination.Helpers;
using Monq.Core.Redis.RedisClient;

namespace Monq.Core.InstanceCoordination.Services;

/// <summary>
/// Registers an application instance and distributes work among active instances.
/// </summary>
public sealed class InstanceService : RedisClientBase
{
    static readonly string _id = $"{Environment.MachineName}:{Environment.ProcessId}";

    readonly TimeSpan _leaseTtl;
    readonly string _instancesKey;
    string[] _activeInstances = [_id];
    long _ownershipVersion;
    bool _released;

    /// <summary>
    /// The current application instance lease token.
    /// </summary>
    public static string LeaseToken => _id;

    /// <summary>
    /// The version of the active application instance set.
    /// </summary>
    public long OwnershipVersion => Volatile.Read(ref _ownershipVersion);

    /// <summary>
    /// Initializes the application instance registration and work distribution service.
    /// </summary>
    public InstanceService(
        IRedisConnectionFactory connectionFactory,
        IHostEnvironment env,
        IConfiguration configuration,
        IOptions<InstanceOptions> options)
        : base(connectionFactory, env, configuration)
    {
        _leaseTtl = options.Value.LeaseTtl;
        _instancesKey = string.IsNullOrEmpty(options.Value.AppName)
            ? $"{KeyPrefix}:Instances"
            : $"{KeyPrefix}:{options.Value.AppName}:Instances";
    }

    /// <summary>
    /// Determines whether the current instance owns the specified key.
    /// </summary>
    public bool IsOwner(string key)
        => RendezvousHashHelper.IsOwner(
            key,
            _id,
            Volatile.Read(ref _activeInstances));

    /// <summary>
    /// Renews the instance registration and refreshes the active instance set.
    /// </summary>
    public async Task<bool> Heartbeat()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var expiresAt = now + (long)_leaseTtl.TotalMilliseconds;

        await Db.SortedSetAddAsync(_instancesKey, _id, expiresAt);
        await Db.SortedSetRemoveRangeByScoreAsync(
            _instancesKey,
            double.NegativeInfinity,
            now);
        await Db.KeyExpireAsync(_instancesKey, _leaseTtl + _leaseTtl);

        var instances = await Db.SortedSetRangeByRankAsync(_instancesKey);
        var activeInstances = instances
            .Select(x => x.ToString())
            .Order(StringComparer.Ordinal)
            .ToArray();
        if (activeInstances.SequenceEqual(Volatile.Read(ref _activeInstances)))
            return true;

        Volatile.Write(ref _activeInstances, activeInstances);
        Interlocked.Increment(ref _ownershipVersion);
        return true;
    }

    /// <summary>
    /// Removes the current application instance registration.
    /// </summary>
    public async Task Release()
    {
        if (_released)
            return;

        await Db.SortedSetRemoveAsync(_instancesKey, _id);
        _released = true;
    }
}
