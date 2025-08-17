
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Reflection;
using DOL.Logging;
using OpenTelemetry.Metrics;

namespace DOL.GS.Metrics.Meters;

public class PlayerMeterProvider : IMeterProvider
{
    private static readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);
    private static readonly eCharacterClass[] classIds = Enum.GetValues<eCharacterClass>();

    public string MeterName => "GameServer.PlayerMetrics";

    // Register the PlayerMetrics
    public void Register(MeterProviderBuilder meterProviderBuilder)
    {
        meterProviderBuilder.AddMeter(MeterName);
        Meter meter = new(MeterName);

        var playerCountTotal = meter.CreateObservableGauge(
            "daoc.online.player.count.total",
            OnlineTotalPlayerCount,
            description: "Total number of players online"
        );

        var playerCount = meter.CreateObservableGauge(
            "daoc.online.player.count.by.class",
            OnlineClassesPerRealm,
            description: "Number of classes per realm online"
        );
    }

    /// <summary>
    /// Calculate the current online player
    /// </summary>
    /// <returns></returns>
    private static int OnlineTotalPlayerCount()
    {
        try
        {
            return ClientService.Instance.GetClients().Count(client => client.ClientState == GameClient.eClientState.Playing);
        }
        catch (Exception e)
        {
            log.Error("MetricsCollector.CollectMetrics threw an exception", e);
        }

        return 0;
    }

    /// <summary>
    /// Get classes per realm online
    /// </summary>
    /// <returns></returns>
    private static List<Measurement<int>> OnlineClassesPerRealm()
    {
        try
        {
            List<Measurement<int>> classes = [];
            List<GameClient> clients = [.. ClientService.Instance.GetClients().Where(client => client.ClientState == GameClient.eClientState.Playing)];

            if (clients.Count == 0)
            {
                return classes;
            }

            foreach (eCharacterClass characterClass in classIds)
            {
                // Get current players by class id
                var classCount = clients.Count(c => c.Player.CharacterClass.ID == (int)characterClass);
                string realmName = GetRealmForCharacterClass(characterClass);

                Measurement<int> measurement = new(
                    classCount,
                    new("realm", realmName),
                    new("class", characterClass.ToString())
                );

                classes.Add(measurement);
            }

            return classes;
        }
        catch (Exception e)
        {
            log.Error("MetricsCollector.CollectMetrics threw an exception", e);
        }

        return [];
    }

    /// <summary>
    /// Get the realm name for a class
    /// </summary>
    /// <param name="characterClass"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    private static string GetRealmForCharacterClass(eCharacterClass characterClass)
    {
        switch (characterClass)
        {
            // Albion
            case eCharacterClass.Acolyte:
            case eCharacterClass.Disciple:
            case eCharacterClass.Fighter:
            case eCharacterClass.AlbionRogue:
            case eCharacterClass.Mage:
            case eCharacterClass.Elementalist:

            case eCharacterClass.Armsman:
            case eCharacterClass.Cabalist:
            case eCharacterClass.Cleric:
            case eCharacterClass.Friar:
            case eCharacterClass.Heretic:
            case eCharacterClass.Infiltrator:
            case eCharacterClass.Mercenary:
            case eCharacterClass.Minstrel:
            case eCharacterClass.Necromancer:
            case eCharacterClass.Paladin:
            case eCharacterClass.Reaver:
            case eCharacterClass.Scout:
            case eCharacterClass.Sorcerer:
            case eCharacterClass.Theurgist:
            case eCharacterClass.Wizard:
            case eCharacterClass.MaulerAlb:
                return "Albion";

            // Hibernia
            case eCharacterClass.Guardian:
            case eCharacterClass.Magician:
            case eCharacterClass.Naturalist:
            case eCharacterClass.Stalker:
            case eCharacterClass.Forester:

            case eCharacterClass.Animist:
            case eCharacterClass.Bainshee:
            case eCharacterClass.Bard:
            case eCharacterClass.Blademaster:
            case eCharacterClass.Champion:
            case eCharacterClass.Druid:
            case eCharacterClass.Eldritch:
            case eCharacterClass.Enchanter:
            case eCharacterClass.Hero:
            case eCharacterClass.Mentalist:
            case eCharacterClass.Nightshade:
            case eCharacterClass.Ranger:
            case eCharacterClass.Valewalker:
            case eCharacterClass.Vampiir:
            case eCharacterClass.Warden:
            case eCharacterClass.MaulerHib:
                return "Hibernia";

            // Midgard
            case eCharacterClass.Mystic:
            case eCharacterClass.Seer:
            case eCharacterClass.Viking:
            case eCharacterClass.MidgardRogue:

            case eCharacterClass.Berserker:
            case eCharacterClass.Bonedancer:
            case eCharacterClass.Healer:
            case eCharacterClass.Hunter:
            case eCharacterClass.Runemaster:
            case eCharacterClass.Savage:
            case eCharacterClass.Shadowblade:
            case eCharacterClass.Shaman:
            case eCharacterClass.Skald:
            case eCharacterClass.Spiritmaster:
            case eCharacterClass.Thane:
            case eCharacterClass.Valkyrie:
            case eCharacterClass.Warlock:
            case eCharacterClass.Warrior:
            case eCharacterClass.MaulerMid:
                return "Midgard";
            default:
                return "Unknown";
        }
    }
}