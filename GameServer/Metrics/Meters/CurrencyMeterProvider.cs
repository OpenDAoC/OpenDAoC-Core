using System;
using System.Diagnostics.Metrics;
using System.Reflection;
using DOL.Logging;
using OpenTelemetry.Metrics;

namespace DOL.GS.Metrics.Meters;

public class CurrencyMeterProvider : IMeterProvider
{
    private static readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);
    public string MeterName => "GameServer.CurrencyMetrics";

    public void Register(MeterProviderBuilder meterProviderBuilder)
    {
        meterProviderBuilder.AddMeter(MeterName);
        Meter meter = new(MeterName);

        var copperTotal = meter.CreateObservableGauge(
            "daoc.copper.total",
            TotalCopper,
            description: "Total amount of copper ingame"
        );
    }

    private static long TotalCopper()
    {
        try
        {
            long sum = 0;

            foreach (GamePlayer player in ClientService.Instance.GetPlayers<object>(IsValidPlayer))
                sum += player.GetCurrentMoney();

            return sum;

            static bool IsValidPlayer(GamePlayer player, object unused)
            {
                return (ePrivLevel) player.Client.Account.PrivLevel is ePrivLevel.Player;
            }
        }
        catch (Exception ex)
        {
            log.Error("MetricsCollector.CollectMetrics threw an exception", ex);
        }

        return 0;
    }
}
