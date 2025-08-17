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

    /// <summary>
    /// Online Players in PvE Zones
    /// </summary>
    /// <returns></returns>
    private static List<Measurement<int>> OnlinePlayerInPvEZones()
    {
        try
        {
            List<GameClient> activePlayers = ClientService.Instance.GetClients().Where(IsPlayerInPvEZone).ToList();

            static bool IsPlayerInPvEZone(GameClient client)
            {
                return client.ClientState is GameClient.eClientState.Playing &&
                    (ePrivLevel) client.Account.PrivLevel is ePrivLevel.Player &&
                    !client.Player.CurrentRegion.IsRvR &&
                    !client.Player.CurrentZone.IsRvR;
            }

            // Albion players in PvE
            Measurement<int> albionPvEPlayers = new(
                activePlayers.Count(c => c.Player.Realm == eRealm.Albion),
                [new("realm", "Albion")]
            );

            // Hibernia players in PvE
            Measurement<int> hiberniaPvEPlayers = new(
                activePlayers.Count(c => c.Player.Realm == eRealm.Hibernia),
                [new("realm", "Hibernia")]
            );

            // Midgard players in PvE
            Measurement<int> midgardPvEPlayers = new(
                activePlayers.Count(c => c.Player.Realm == eRealm.Midgard),
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