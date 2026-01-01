using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DOL.GS.Metrics.Meters;
using DOL.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

namespace DOL.GS.Metrics;

public static class MeterRegistry
{
    private static readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

    public static void RegisterMeterProviders()
    {
        if (log.IsInfoEnabled)
            log.Info("Configuring Meter Providers");

        MeterProviderBuilder builder = Sdk.CreateMeterProviderBuilder()
            .ConfigureResource(resource => resource.AddService("GameServer"))
            .AddRuntimeInstrumentation()
            .AddOtlpExporter((options, readerOptions) =>
            {
                options.Endpoint = GameServer.Instance.Configuration.OtlpEndpoint;
                readerOptions.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = GameServer.Instance.Configuration.MetricsExportInterval;
            });

        if (log.IsInfoEnabled)
            log.Info("Get all meters implementing IMeterProvider");

        List<IMeterProvider> meters =
        [
            ..Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.GetInterfaces().Contains(typeof(IMeterProvider)))
            .Select(t => (IMeterProvider)Activator.CreateInstance(t))
        ];

        foreach (IMeterProvider meter in meters)
        {
            if (log.IsInfoEnabled)
                log.Info($"Registering {meter.MeterName}");

            meter.Register(builder);
        }

        MeterProvider meterProvider = builder.Build();
    }
}
