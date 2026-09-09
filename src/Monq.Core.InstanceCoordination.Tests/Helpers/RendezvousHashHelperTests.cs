using Monq.Core.InstanceCoordination.Helpers;
using Xunit;

namespace Monq.Core.InstanceCoordination.Tests.Helpers;

public sealed class RendezvousHashHelperTests
{
    static readonly string[] _instances = ["instance-1", "instance-2", "instance-3"];

    [Fact]
    public void IsOwner_SelectsExactlyOneOwner()
    {
        string[] instances = ["instance-1", "instance-2", "instance-3"];

        var owners = instances.Count(instance =>
            RendezvousHashHelper.IsOwner("work-item", instance, instances));

        Assert.Equal(1, owners);
    }

    [Fact]
    public void IsOwner_DoesNotDependOnInstanceOrder()
    {
        string[] instances = ["instance-1", "instance-2", "instance-3"];
        var owner = instances.Single(instance =>
            RendezvousHashHelper.IsOwner("work-item", instance, instances));
        Array.Reverse(instances);

        Assert.True(RendezvousHashHelper.IsOwner("work-item", owner, instances));
    }

    [Fact]
    public void IsOwner_ReturnsFalseForEmptyInstanceSet()
        => Assert.False(RendezvousHashHelper.IsOwner("work-item", "instance-1", []));

    [Theory]
    [InlineData("work-item", "instance-1")]
    [InlineData("candidate-42", "instance-3")]
    [InlineData("tenant:100", "instance-1")]
    public void IsOwner_MatchesStableTestVectors(string key, string expectedOwner)
        => Assert.Equal(expectedOwner, GetOwner(key, _instances));

    [Fact]
    public void IsOwner_WithSingleInstance_AlwaysSelectsThatInstance()
        => Assert.True(RendezvousHashHelper.IsOwner("work-item", "instance-1", ["instance-1"]));

    [Fact]
    public void IsOwner_ReturnsFalseForInstanceOutsideCandidateSet()
        => Assert.False(RendezvousHashHelper.IsOwner("work-item", "unknown", _instances));

    [Fact]
    public void AddingInstance_MovesKeysOnlyToAddedInstance()
    {
        string[] expandedInstances = [.. _instances, "instance-4"];

        foreach (var key in Enumerable.Range(0, 1_000).Select(x => $"work-item-{x}"))
        {
            var originalOwner = GetOwner(key, _instances);
            var expandedOwner = GetOwner(key, expandedInstances);

            Assert.True(expandedOwner == originalOwner || expandedOwner == "instance-4");
        }
    }

    [Fact]
    public void RemovingInstance_KeepsKeysOwnedByRemainingInstances()
    {
        var remainingInstances = _instances.Where(x => x != "instance-2").ToArray();

        foreach (var key in Enumerable.Range(0, 1_000).Select(x => $"work-item-{x}"))
        {
            var originalOwner = GetOwner(key, _instances);
            if (originalOwner == "instance-2")
                continue;

            Assert.Equal(originalOwner, GetOwner(key, remainingInstances));
        }
    }

    static string GetOwner(string key, IReadOnlyCollection<string> instances)
        => instances.Single(instance => RendezvousHashHelper.IsOwner(key, instance, instances));
}
