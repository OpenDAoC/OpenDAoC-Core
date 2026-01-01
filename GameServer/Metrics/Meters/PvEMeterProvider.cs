using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Reflection;
using DOL.Logging;
using OpenTelemetry.Metrics;

namespace DOL.GS.Metrics.Meters;

public class PvEMeterProvider : IMeterProvider
{
    private static readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);
    public string MeterName => "GameServer.PvEMetrics";

    public void Register(MeterProviderBuilder meterProviderBuilder)
    {
        meterProviderBuilder.AddMeter(MeterName);
        Meter meter = new(MeterName);

        var playerInPvEZones = meter.CreateObservableGauge(
            "daoc.online.player.count.pve.zones",
            OnlinePlayerInPvEZones,
            description: "Total number of players online in PvE zones"
        );
    }

    private static List<Measurement<int>> OnlinePlayerInPvEZones()
    {
        try
        {
            List<GamePlayer> activePlayers = ClientService.Instance.GetPlayers<object>(IsPlayerInPvEZone);

            static bool IsPlayerInPvEZone(GamePlayer player, object unused)
            {
                return player.Client.ClientState is GameClient.eClientState.Playing &&
                    (ePrivLevel) player.Client.Account.PrivLevel is ePrivLevel.Player &&
                    !player.CurrentRegion.IsRvR &&
                    !player.CurrentZone.IsRvR;
            }

            // Albion players in PvE
            Measurement<int> albionPvEPlayers = new(
                activePlayers.Count(p => p.Realm is eRealm.Albion),
                [new("realm", "Albion")]
            );

            // Hibernia players in PvE
            Measurement<int> hiberniaPvEPlayers = new(
                activePlayers.Count(p => p.Realm is eRealm.Hibernia),
                [new("realm", "Hibernia")]
            );

            // Midgard players in PvE
            Measurement<int> midgardPvEPlayers = new(
                activePlayers.Count(p => p.Realm is eRealm.Midgard),
                [new("realm", "Midgard")]
            );

            return
            [
                albionPvEPlayers,
                hiberniaPvEPlayers,
                midgardPvEPlayers
            ];
        }
        catch (Exception e)
        {
            log.Error("MetricsCollector.CollectMetrics threw an exception", e);
        }

        return [];
    }
}
