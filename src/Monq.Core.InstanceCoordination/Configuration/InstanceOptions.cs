using System.Diagnostics.CodeAnalysis;

namespace Monq.Core.InstanceCoordination.Configuration;

/// <summary>
/// Application instance registration options.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class InstanceOptions
{
    /// <summary>
    /// The interval between application instance registration renewals.
    /// </summary>
    public TimeSpan HeartbeatInterval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// The application instance registration lifetime.
    /// </summary>
    public TimeSpan LeaseTtl { get; set; } = TimeSpan.FromSeconds(15);

    /// <summary>
    /// The application name.
    /// </summary>
    /// <remarks>Use a unique name for each application within the same infrastructure.</remarks>
    public string? AppName { get; set; }
}
