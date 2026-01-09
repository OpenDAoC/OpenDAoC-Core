using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
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

    private static int OnlineTotalPlayerCount()
    {
        try
        {
            return ClientService.Instance.GetPlayers().Count;
        }
        catch (Exception e)
        {
            log.Error("MetricsCollector.CollectMetrics threw an exception", e);
        }

        return 0;
    }

    private static List<Measurement<int>> OnlineClassesPerRealm()
    {
        try
        {
            List<GamePlayer> players = ClientService.Instance.GetPlayers();

            if (players.Count == 0)
                return [];

            Dictionary<eCharacterClass, int> counts = new();

            foreach (var player in players)
            {
                var cls = (eCharacterClass)player.CharacterClass.ID;
                counts[cls] = counts.GetValueOrDefault(cls) + 1;
            }

            List<Measurement<int>> classes = new(classIds.Length);

            foreach (eCharacterClass characterClass in classIds)
            {
                counts.TryGetValue(characterClass, out int classCount);

                classes.Add(new Measurement<int>(
                    classCount,
                    new("realm", GetRealmForCharacterClass(characterClass)),
                    new("class", characterClass.ToString())
                ));
            }

            return classes;
        }
        catch (Exception e)
        {
            log.Error("MetricsCollector.CollectMetrics threw an exception", e);
            return [];
        }
    }

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
