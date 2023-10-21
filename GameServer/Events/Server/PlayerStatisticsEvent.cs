using System;
using System.Collections;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Server;

namespace Core.GS.Events;

public class PlayerStatisticsEvent
{
    private static bool HANDLERS_LOADED = false;

    // Load these when the first player logs in
    // Coded like this so they won't be loaded if the server uses custom statistics
    public static void CheckHandlers()
    {
        if (HANDLERS_LOADED == false)
        {
            HANDLERS_LOADED = true;
            GameEventMgr.AddHandler(GameLivingEvent.GainedRealmPoints, new CoreEventHandler(GainedRealmPointsCallback));
            GameEventMgr.AddHandler(GameLivingEvent.Dying, new CoreEventHandler(DyingCallback));
            GameEventMgr.AddHandler(GameLivingEvent.CastFinished, new CoreEventHandler(FinishCastSpellCallback));
            GameEventMgr.AddHandler(GameLivingEvent.HealthChanged, new CoreEventHandler(HealthChangedCallback));
        }
    }

    public static void GainedRealmPointsCallback(CoreEvent e, object sender, EventArgs args)
    {
        if (sender is not GamePlayer player || args is not GainedRealmPointsEventArgs gargs)
            return;

        if (player.Statistics is not PlayerStatistics stats)
            return;

        stats.TotalRP += (uint) gargs.RealmPoints;
    }

    public static void DyingCallback(CoreEvent e, object sender, EventArgs args)
    {
        if (sender is not GamePlayer dyingPlayer || args is not DyingEventArgs dargs)
            return;

        if (dargs.Killer is not GamePlayer killer)
            return;

        if (killer.Statistics is not PlayerStatistics killerStats || dyingPlayer.Statistics is not PlayerStatistics dyingPlayerStats)
            return;

        killerStats.Deathblows++;

        if (dyingPlayer.RealmPointsValue > 0)
        {
            killerStats.KillsThatHaveEarnedRPs++;
            killerStats.RealmPointsEarnedFromKills += RPsEarnedFromKill(killer, dyingPlayer);

            if (killer.Group != null)
            {
                foreach (GamePlayer member in killer.Group.GetPlayersInTheGroup())
                {
                    if (member != killer)
                    {
                        if (member.Statistics is PlayerStatistics memberStats)
                        {
                            memberStats.KillsThatHaveEarnedRPs++;
                            memberStats.RealmPointsEarnedFromKills += RPsEarnedFromKill(member, dyingPlayer);
                        }
                    }
                }
            }
        }

        dyingPlayerStats.Deaths++;
    }

    public static void FinishCastSpellCallback(CoreEvent e, object sender, EventArgs args)
    {
        if (sender is not GamePlayer caster || args is not CastingEventArgs fargs)
            return;

        if (fargs.SpellHandler.Spell.SpellType == ESpellType.Resurrect)
        {
            if (caster.Statistics is PlayerStatistics stats)
                stats.ResurrectionsPerformed++;
        }
    }

    public static void HealthChangedCallback(CoreEvent e, object sender, EventArgs args)
    {
        HealthChangedEventArgs hargs = args as HealthChangedEventArgs;

        if (hargs.ChangeType == EHealthChangeType.Spell)
        {
            if (hargs.ChangeSource is not GamePlayer player)
                return;

            if (player.Statistics is PlayerStatistics stats)
                stats.HitPointsHealed += (uint) hargs.ChangeAmount;
        }
    }

    public static uint RPsEarnedFromKill(GamePlayer killer, GamePlayer killedPlayer)
    {
        long noExpSeconds = ServerProperty.RP_WORTH_SECONDS;

        if (killedPlayer.DeathTime + noExpSeconds > killedPlayer.PlayedTime)
            return 0;

        float totalDmg = 0.0f;

        lock (killedPlayer.XPGainers.SyncRoot)
        {
            foreach (DictionaryEntry de in killedPlayer.XPGainers)
                totalDmg += (float) de.Value;

            foreach (DictionaryEntry de in killedPlayer.XPGainers)
            {
                GamePlayer key = de.Key as GamePlayer;

                if (killer == key)
                {
                    if (!killer.IsWithinRadius(killedPlayer, WorldMgr.MAX_EXPFORKILL_DISTANCE))
                        return 0;

                    double damagePercent = (float) de.Value / totalDmg;

                    if (!key.IsAlive)//Dead living gets 25% exp only
                        damagePercent *= 0.25;

                    int rpCap = key.RealmPointsValue * 2;
                    uint realmPoints = (uint) (killedPlayer.RealmPointsValue * damagePercent);
                    realmPoints = (uint) (realmPoints * (1.0 + 2.0 * (killedPlayer.RealmLevel - killer.RealmLevel) / 900.0));

                    if (killer.Group != null && killer.Group.MemberCount > 1)
                    {
                        int count = 0;

                        foreach (GamePlayer player in killer.Group.GetPlayersInTheGroup())
                        {
                            if (!player.IsWithinRadius(killedPlayer, WorldMgr.MAX_EXPFORKILL_DISTANCE))
                                continue;

                            count++;
                        }

                        realmPoints = (uint) (realmPoints * (1.0 + count * 0.125));
                    }

                    if (realmPoints > rpCap)
                        realmPoints = (uint) rpCap;

                    return realmPoints;
                }
                else
                    continue;
            }
        }

        return 0;
    }
}