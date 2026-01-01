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

    private static List<Measurement<int>> OnlinePlayerInRvRZones()
    {
        try
        {
            List<GamePlayer> activePlayers = ClientService.Instance.GetPlayers<object>(IsPlayerInRvRZone);

            static bool IsPlayerInRvRZone(GamePlayer player, object unused)
            {
                return player.Client.ClientState is GameClient.eClientState.Playing &&
                    (ePrivLevel) player.Client.Account.PrivLevel is ePrivLevel.Player &&
                    (player.CurrentRegion.IsRvR || player.CurrentZone.IsRvR);
            }

            // Albion players in RvR
            Measurement<int> albionRvRPlayers = new(
                activePlayers.Count(p => p.Realm is eRealm.Albion),
                [new("realm", "Albion")]
            );

            // Hibernia players in RvR
            Measurement<int> hiberniaRvRPlayers = new(
                activePlayers.Count(p => p.Realm is eRealm.Hibernia),
                [new("realm", "Hibernia")]
            );

            // Midgard players in RvR
            Measurement<int> midgardRvRPlayers = new(
                activePlayers.Count(p => p.Realm is eRealm.Midgard),
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
