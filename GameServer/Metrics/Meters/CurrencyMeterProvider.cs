using System;
using System.Diagnostics.Metrics;
using System.Linq;
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

    /// <summary>
    /// Get the current ingame money
    /// </summary>
    /// <returns>The total amount of money in copper</returns>
    private static long TotalCopper()
    {
        try
        {
            return ClientService.GetClients().Where(IsPlayerActive).Select(GetPlayerMoney).Sum();

            static bool IsPlayerActive(GameClient client)
            {
                return client.ClientState is GameClient.eClientState.Playing && (ePrivLevel) client.Account.PrivLevel is ePrivLevel.Player;
            }

            static long GetPlayerMoney(GameClient client)
            {
                return client.Player.GetCurrentMoney();
            }
        }
        catch (Exception ex)
        {
            log.Error("MetricsCollector.CollectMetrics threw an exception", ex);
        }

        return 0;
    }
}