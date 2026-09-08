# Monq.Core.InstanceCoordination

Application instance coordination and distributed workload ownership for .NET.

The library registers application instances in Redis using renewable leases and
distributes work between active instances with rendezvous hashing.

## Usage

```csharp
services.AddInstances(options =>
{
    options.AppName = "thresholds";
    options.LeaseTtl = TimeSpan.FromSeconds(15);
});
```

Inject `InstanceService` and call `IsOwner(key)` to determine whether the current
application instance owns a work item. `OwnershipVersion` changes whenever the
set of active instances changes.
