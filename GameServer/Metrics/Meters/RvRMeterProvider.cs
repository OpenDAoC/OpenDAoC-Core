using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Reflection;
using DOL.Logging;
using OpenTelemetry.Metrics;

namespace DOL.GS.Metrics.Meters;

public class RvRMeterProvider : IMeterProvider
{
    private static readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);
    public string MeterName => "GameServer.RvRMetrics";

    public void Register(MeterProviderBuilder meterProviderBuilder)
    {
        meterProviderBuilder.AddMeter(MeterName);
        Meter meter = new(MeterName);

        var playerInRvPZones = meter.CreateObservableGauge(
            "daoc.online.player.count.rvr.zones",
            OnlinePlayerInRvRZones,
            description: "Total number of players online in RvP zones"
        );
    }

    /// <summary>
    /// Get active players in RvR zones
    /// </summary>
    /// <returns></returns>
    private static List<Measurement<int>> OnlinePlayerInRvRZones()
    {
        try
        {
            List<GameClient> activePlayers = ClientService.GetClients().Where(IsPlayerInRvRZone).ToList();

            static bool IsPlayerInRvRZone(GameClient client)
            {
                return client.ClientState is GameClient.eClientState.Playing &&
                    (ePrivLevel) client.Account.PrivLevel is ePrivLevel.Player &&
                    (client.Player.CurrentRegion.IsRvR || client.Player.CurrentZone.IsRvR);
            }

            // Albion players in RvR
            Measurement<int> albionRvRPlayers = new(
                activePlayers.Count(c => c.Player.Realm == eRealm.Albion),
                [new("realm", "Albion")]
            );

            // Hibernia players in RvR
            Measurement<int> hiberniaRvRPlayers = new(
                activePlayers.Count(c => c.Player.Realm == eRealm.Hibernia),
                [new("realm", "Hibernia")]
            );

            // Midgard players in RvR
            Measurement<int> midgardRvRPlayers = new(
                activePlayers.Count(c => c.Player.Realm == eRealm.Midgard),
                [new("realm", "Midgard")]
            );

            return 
            [
                albionRvRPlayers,
                hiberniaRvRPlayers,
                midgardRvRPlayers
            ];
        } 
        catch (Exception e)
        {
            log.Error("MetricsCollector.CollectMetrics threw an exception", e);
        }

        return [];
    }
}