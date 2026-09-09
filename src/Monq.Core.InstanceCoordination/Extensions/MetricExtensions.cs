using System.Diagnostics.Metrics;

namespace Monq.Core.InstanceCoordination.Extensions;

/// <summary>
/// Extension methods for candidate distribution metrics.
/// </summary>
public static class MetricExtensions
{
    /// <summary>
    /// Creates a gauge that records the number of candidates owned by a service instance.
    /// </summary>
    /// <param name="meter">The metrics meter.</param>
    /// <param name="observeValue">A callback that returns the current number of owned candidates.</param>
    /// <returns>The gauge for the number of owned candidates.</returns>
    public static ObservableGauge<int> CreateOwnedCandidatesGauge(
        this Meter meter,
        Func<int> observeValue)
        => meter.CreateObservableGauge(
            "candidates.owned",
            observeValue,
            "{candidate}",
            "Number of candidates owned by a service instance.");
}
