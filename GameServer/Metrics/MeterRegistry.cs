using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Reflection;
using DOL.Logging;
using DOL.GS.Metrics.Meters;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

namespace DOL.GS.Metrics;

public static class MeterRegistry
{
    private static readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

    /// <summary>
    /// Get all collectors and register them
    /// </summary>
    public static void RegisterMeterProviders()
    {
        log.Info("Configuring Meter Providers");

        MeterProviderBuilder builder = Sdk.CreateMeterProviderBuilder()
            .ConfigureResource(resource => resource.AddService("GameServer"))
            .AddRuntimeInstrumentation()
            .AddOtlpExporter((options, readerOptions) =>
            {
                options.Endpoint = GameServer.Instance.Configuration.OtlpEndpoint;
                readerOptions.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = GameServer.Instance.Configuration.MetricsExportInterval;
            });

        // Get all meters
        log.Info("Get all meters implementing IMeterProvider");
        List<IMeterProvider> meters = 
        [..
                Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.GetInterfaces().Contains(typeof(IMeterProvider)))
            .Select(t => (IMeterProvider)Activator.CreateInstance(t))
        ];


        foreach (IMeterProvider meter in meters)
        {
            log.Info($"Registering {meter.MeterName}");
            meter.Register(builder);
        }

        MeterProvider meterProvider = builder.Build();
    }
}