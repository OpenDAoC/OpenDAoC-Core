using OpenTelemetry.Metrics;

namespace DOL.GS.Metrics.Meters;

public interface IMeterProvider
{
    string MeterName { get; }

    void Register(MeterProviderBuilder meterProviderBuilder);
}
