using Monq.Core.InstanceCoordination.Extensions;
using System.Diagnostics.Metrics;
using Xunit;

namespace Monq.Core.InstanceCoordination.Tests.Extensions;

public sealed class MetricExtensionsTests
{
    [Fact]
    public void CreateOwnedCandidatesGauge_CreatesExpectedObservableGauge()
    {
        using var meter = new Meter("InstanceCoordination.Tests");
        using var listener = new MeterListener();
        Instrument? observedInstrument = null;
        int? observedValue = null;
        listener.InstrumentPublished = (instrument, currentListener) =>
        {
            if (instrument.Meter == meter)
                currentListener.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<int>((instrument, value, _, _) =>
        {
            observedInstrument = instrument;
            observedValue = value;
        });
        listener.Start();

        var gauge = meter.CreateOwnedCandidatesGauge(() => 42);
        listener.RecordObservableInstruments();

        Assert.Same(gauge, observedInstrument);
        Assert.Equal(42, observedValue);
        Assert.Equal("candidates.owned", gauge.Name);
        Assert.Equal("{candidate}", gauge.Unit);
        Assert.Equal("Number of candidates owned by a service instance.", gauge.Description);
    }
}

