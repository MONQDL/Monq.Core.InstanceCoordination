# Monq.Core.InstanceCoordination

Application instance coordination and distributed workload ownership for .NET.

The library registers application instances in Redis using renewable leases and distributes work between active instances with rendezvous hashing. It is intended for services where every work item must be handled by exactly one active application instance without a central coordinator.

## Installing

```powershell
Install-Package Monq.Core.InstanceCoordination
```

The library requires `Monq.Core.Redis`. Register a Redis client before starting the host.

## How it works

Each running process registers a lease identified by its machine name and process ID. A background service renews the lease periodically, removes expired registrations, and refreshes the local list of active instances.

`InstanceService.IsOwner(key)` applies rendezvous hashing to the key and active instance list. This provides deterministic ownership and minimizes redistribution when instances are added or removed.

## Registration

Add the Redis client and instance coordination services in `Program.cs`:

```csharp
using Monq.Core.InstanceCoordination.DependencyInjection;

builder.Services.AddRedisClient(builder.Configuration.GetSection("Redis"));
builder.Services.AddInstances(options =>
{
    options.AppName = "thresholds";
    options.HeartbeatInterval = TimeSpan.FromSeconds(5);
    options.LeaseTtl = TimeSpan.FromSeconds(15);
});
```

`AddInstances()` also binds options from the `InstanceOptions` configuration section. Values supplied through the setup action override configuration values.

```json
{
  "InstanceOptions": {
    "AppName": "thresholds",
    "HeartbeatInterval": "00:00:05",
    "LeaseTtl": "00:00:15"
  }
}
```

### Options

| Option | Default | Description |
|---|---:|---|
| `AppName` | `null` | Distinguishes applications sharing the same Redis infrastructure. Use a unique value for each application. |
| `HeartbeatInterval` | 5 seconds | Interval between lease renewals. Must be greater than zero. |
| `LeaseTtl` | 15 seconds | Time after which an instance is considered unavailable without a successful renewal. Must be greater than zero. |

`HeartbeatInterval` should be shorter than `LeaseTtl` to allow the lease to be renewed before it expires.

## Distributing work

Inject `InstanceService` and process only the items owned by the current instance:

```csharp
using Monq.Core.InstanceCoordination.Services;

public sealed class CandidateProcessor(InstanceService instances)
{
    public async Task Process(IEnumerable<Candidate> candidates)
    {
        foreach (var candidate in candidates)
        {
            if (!instances.IsOwner(candidate.Id.ToString()))
                continue;

            await ProcessCandidate(candidate);
        }
    }

    static Task ProcessCandidate(Candidate candidate)
    {
        // Process the candidate on its owning application instance.
        return Task.CompletedTask;
    }
}
```

Use a stable, unique key for each work item. Passing the same key to every instance guarantees that all instances calculate the same owner from the same active instance list.

### Detecting ownership changes

`OwnershipVersion` is incremented whenever the active instance set changes. A service can use it to rebuild a local ownership-dependent cache only when necessary:

```csharp
long ownershipVersion = -1;

if (ownershipVersion != instances.OwnershipVersion)
{
    ownedCandidates = candidates
        .Where(candidate => instances.IsOwner(candidate.Id.ToString()))
        .ToArray();

    ownershipVersion = instances.OwnershipVersion;
}
```

`InstanceService.LeaseToken` exposes the identifier of the current application instance.

## Instance lifecycle operations

Implement `IInstanceOperation` to attach an additional resource to the instance lease lifecycle. Operations run after the main instance heartbeat and are released in reverse registration order when the host stops.

```csharp
using Monq.Core.InstanceCoordination.Services;

public sealed class PartitionLeaseOperation : IInstanceOperation
{
    public Task<bool> Heartbeat()
    {
        // Renew an additional resource owned by this instance.
        return Task.FromResult(true);
    }

    public Task Release()
    {
        // Release the resource during graceful shutdown.
        return Task.CompletedTask;
    }
}
```

Register one or more operations as singletons:

```csharp
builder.Services.AddSingleton<IInstanceOperation, PartitionLeaseOperation>();
```

Returning `false` from `Heartbeat()` rejects the instance heartbeat and stops normal background execution. Exceptions are logged so that transient failures can be retried on the next heartbeat.

## Metrics

The library provides a helper for creating the `candidates.owned` observable gauge:

```csharp
using System.Diagnostics.Metrics;
using Monq.Core.InstanceCoordination.Extensions;

var meter = new Meter("Thresholds");
var ownedCandidates = Array.Empty<Candidate>();

meter.CreateOwnedCandidatesGauge(() => ownedCandidates.Length);
```

| Instrument | Type | Unit | Description |
|---|---|---|---|
| `candidates.owned` | Observable gauge | `{candidate}` | Number of candidates owned by a service instance. |

## Target frameworks

- .NET 8
- .NET 9
- .NET 10

The package is trimming-compatible and validated with the Native AOT analyzer.
