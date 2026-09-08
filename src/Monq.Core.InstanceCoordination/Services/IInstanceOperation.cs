namespace Monq.Core.InstanceCoordination.Services;

/// <summary>
/// An additional application instance lifecycle operation.
/// </summary>
public interface IInstanceOperation
{
    /// <summary>
    /// Performs the operation when the instance registration is renewed.
    /// </summary>
    /// <returns><see langword="true"/> if the instance may continue running.</returns>
    Task<bool> Heartbeat();

    /// <summary>
    /// Performs the operation when the instance stops.
    /// </summary>
    Task Release();
}
