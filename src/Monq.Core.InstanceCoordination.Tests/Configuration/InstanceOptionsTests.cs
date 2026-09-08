using Monq.Core.InstanceCoordination.Configuration;
using Xunit;

namespace Monq.Core.InstanceCoordination.Tests.Configuration;

public sealed class InstanceOptionsTests
{
    [Fact]
    public void Defaults_AreSuitableForLeaseRenewal()
    {
        var options = new InstanceOptions();

        Assert.Equal(TimeSpan.FromSeconds(5), options.HeartbeatInterval);
        Assert.Equal(TimeSpan.FromSeconds(15), options.LeaseTtl);
        Assert.Null(options.AppName);
        Assert.True(options.HeartbeatInterval < options.LeaseTtl);
    }
}

