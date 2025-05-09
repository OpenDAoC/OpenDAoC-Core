
using OpenTelemetry.Metrics;

namespace DOL.GS.Metrics.Meters;

public interface IMeterProvider
{
    /// <summary>
    /// The name of the meter
    /// </summary>
    string MeterName { get; }

    /// <summary>
    /// Register the meter with a MeterProviderBuilder
    /// </summary>
    /// <param name="meterProviderBuilder"></param>
    void Register(MeterProviderBuilder meterProviderBuilder);
}
