using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Reflection;
using DOL.Logging;
using OpenTelemetry.Metrics;

namespace DOL.GS.Metrics.Meters
{
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
                List<Measurement<int>> pveZones = [];
                List<GameClient> clients = [.. ClientService.GetClients().Where(client => client.ClientState == GameClient.eClientState.Playing && !client.Player.CurrentRegion.IsRvR)];

                // Albion players in PvE
                Measurement<int> albionPvEPlayers = new(
                    clients.Count(c => c.Player.Realm == eRealm.Albion),
                    [new("realm", "Albion")]
                );

                // Hibernia players in PvE
                Measurement<int> hiberniaPvEPlayers = new(
                    clients.Count(c => c.Player.Realm == eRealm.Hibernia),
                    [new("realm", "Hibernia")]
                );

                // Midgard players in PvE
                Measurement<int> midgardPvEPlayers = new(
                    clients.Count(c => c.Player.Realm == eRealm.Midgard),
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
}