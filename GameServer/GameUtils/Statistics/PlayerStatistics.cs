using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DOL.Database;
using DOL.Events;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
    public class PlayerStatistics : IPlayerStatistics
    {
        private const int TIME_BETWEEN_UPDATES = 60000; // 1 minute
        private static bool HAS_BEEN_RUN = false;
        private static long LAST_UPDATED_TIME = 0;
        private static long TIME_TO_CHANGE = 0;
        private static List<string> TOP_LIST = new();
        private static string STATS_RP;
        private static string STATS_LRP;
        private static string STATS_KILL;
        private static string STATS_DEATH;
        private static string STATS_IRS;
        private static string STATS_HEAL;
        private static string STATS_RES;

        public class StatToCount
        {
            public string name;
            public uint count;

            public StatToCount(string name, uint count)
            {
                this.name = name;
                this.count = count;
            }
        }

        public GamePlayer Player { get; set; } = null;
        public uint TotalRP { get; set; } = 0;
        public uint RealmPointsEarnedFromKills { get; set; } = 0;
        public ushort KillsThatHaveEarnedRPs { get; set; } = 0;
        public ushort Deathblows { get; set; } = 0;
        public ushort Deaths { get; set; } = 0;
        public uint HitPointsHealed { get; set; } = 0;
        public uint RPEarnedFromHitPointsHealed { get; set; } = 0;
        public ushort ResurrectionsPerformed { get; set; } = 0;
        public DateTime LoginTime { get; private set; }

        public PlayerStatistics(GamePlayer player)
        {
            GameEvents.PlayerStatisticsEvent.CheckHandlers();
            Player = player;
            LoginTime = DateTime.Now;
        }

        public static void CreateServerStats(GameClient client)
        {
            GamePlayer player = client.Player;

            if (LAST_UPDATED_TIME == 0)
                LAST_UPDATED_TIME = player.CurrentRegion.Time;

            TIME_TO_CHANGE = LAST_UPDATED_TIME + TIME_BETWEEN_UPDATES;

            if (player.CurrentRegion.Time < TIME_TO_CHANGE && HAS_BEEN_RUN)
                return;

            DbCoreCharacter[] chars = DOLDB<DbCoreCharacter>.SelectObjects(DB.Column("RealmPoints").IsGreatherThan(213881)).OrderByDescending(dc => dc.RealmPoints).Take(100).ToArray();

            // assuming we can get at least 20 players
            if (TOP_LIST.Count > 0)
                TOP_LIST.Clear();

            int count = 1;

            foreach (DbCoreCharacter chr in chars)
            {
                if (chr.IgnoreStatistics == false)
                {
                    DbAccount account = GameServer.Database.FindObjectByKey<DbAccount>(chr.AccountName);

                    if (account != null && account.PrivLevel == 1)
                    {
                        TOP_LIST.Add($"\n{count} - [{chr.Name}] with {string.Format("{0:0,0}", chr.RealmPoints)} RP - [{(chr.RealmLevel + 10) / 10}L{(chr.RealmLevel + 10) % 10}]");

                        if (++count > 20)
                            break;
                    }
                }
            }

            if (count == 1)
                TOP_LIST.Add("None found!");

            List<StatToCount> allStatsRp = new();
            List<StatToCount> allStatsLrp = new();
            List<StatToCount> allStatsKill = new();
            List<StatToCount> allStatDeath = new();
            List<StatToCount> allStatIrs = new();
            List<StatToCount> allStatHeal = new();
            List<StatToCount> allStatsRes = new();
            List<StatToCount> allStatsRpEarnedFromHeal = new();

            foreach (GamePlayer otherPlayer in ClientService.GetNonGmPlayers())
            {
                if (!otherPlayer.IgnoreStatistics && otherPlayer.Statistics is PlayerStatistics stats)
                {
                    if (otherPlayer.RealmLevel > 31)
                    {
                        allStatsRp.Add(new StatToCount(otherPlayer.Name, stats.TotalRP));
                        TimeSpan onlineTime = DateTime.Now.Subtract(stats.LoginTime);
                        allStatsLrp.Add(new StatToCount(otherPlayer.Name, (uint) Math.Round(RPsPerHour(stats.TotalRP, onlineTime))));
                        allStatIrs.Add(new StatToCount(otherPlayer.Name, (uint) Math.Round(stats.TotalRP / Math.Max(1.0, stats.Deaths))));
                    }

                    allStatsKill.Add(new StatToCount(otherPlayer.Name, stats.KillsThatHaveEarnedRPs));
                    allStatDeath.Add(new StatToCount(otherPlayer.Name, stats.Deathblows));
                    allStatHeal.Add(new StatToCount(otherPlayer.Name, stats.HitPointsHealed));
                    allStatsRes.Add(new StatToCount(otherPlayer.Name, stats.ResurrectionsPerformed));
                    allStatsRpEarnedFromHeal.Add(new StatToCount(otherPlayer.Name, stats.RPEarnedFromHitPointsHealed));
                }
            }

            allStatsRp.Sort((ctc1, ctc2) => ctc1.count.CompareTo(ctc2.count));
            allStatsRp.Reverse();
            allStatsLrp.Sort((ctc1, ctc2) => ctc1.count.CompareTo(ctc2.count));
            allStatsLrp.Reverse();
            allStatsKill.Sort((ctc1, ctc2) => ctc1.count.CompareTo(ctc2.count));
            allStatsKill.Reverse();
            allStatDeath.Sort((ctc1, ctc2) => ctc1.count.CompareTo(ctc2.count));
            allStatDeath.Reverse();
            allStatIrs.Sort((ctc1, ctc2) => ctc1.count.CompareTo(ctc2.count));
            allStatIrs.Reverse();
            allStatHeal.Sort((ctc1, ctc2) => ctc1.count.CompareTo(ctc2.count));
            allStatHeal.Reverse();
            allStatsRes.Sort((ctc1, ctc2) => ctc1.count.CompareTo(ctc2.count));
            allStatsRes.Reverse();
            allStatsRpEarnedFromHeal.Sort((ctc1, ctc2) => ctc1.count.CompareTo(ctc2.count));
            allStatsRpEarnedFromHeal.Reverse();

            STATS_RP = "";
            STATS_LRP = "";
            STATS_KILL = "";
            STATS_DEATH = "";
            STATS_IRS = "";
            STATS_HEAL = "";
            STATS_RES = "";

            for (int c = 0; c < allStatsRp.Count; c++)
            {
                if (c > 19 || allStatsRp[c].count < 1)
                    break;

                STATS_RP += $"{c + 1}. {allStatsRp[c].name} with {allStatsRp[c].count} RP\n";
            }

            for (int c = 0; c < allStatsLrp.Count; c++)
            {
                if (c > 19 || allStatsLrp[c].count < 1)
                    break;

                STATS_LRP += $"{c + 1}. {allStatsLrp[c].name} with {allStatsLrp[c].count} RP/hour\n";
            }

            for (int c = 0; c < allStatsKill.Count; c++)
            {
                if (c > 19 || allStatsKill[c].count < 1)
                    break;

                STATS_KILL += $"{c + 1}. {allStatsKill[c].name} with {allStatsKill[c].count} kills\n";
            }

            for (int c = 0; c < allStatDeath.Count; c++)
            {
                if (c > 19 || allStatDeath[c].count < 1)
                    break;

                STATS_DEATH += $"{c + 1}. {allStatDeath[c].name} with {allStatDeath[c].count} deathblows\n";
            }

            for (int c = 0; c < allStatIrs.Count; c++)
            {
                if (c > 19 || allStatIrs[c].count < 1)
                    break;

                STATS_IRS += $"{c + 1}. {allStatIrs[c].name} with {allStatIrs[c].count} RP/death\n";
            }

            for (int c = 0; c < allStatHeal.Count; c++)
            {
                if (c > 19 || allStatHeal[c].count < 1)
                    break;

                STATS_HEAL += $"{c + 1}. {allStatHeal[c].name} with {allStatHeal[c].count} HP and {allStatsRpEarnedFromHeal[c].count} RP gained from heal\n";
            }

            for (int c = 0; c < allStatsRes.Count; c++)
            {
                if (c > 19 || allStatsRes[c].count < 1)
                    break;

                STATS_RES += $"{c + 1}. {allStatsRes[c].name} with {allStatsRes[c].count} res\n";
            }

            LAST_UPDATED_TIME = player.CurrentRegion.Time;
            HAS_BEEN_RUN = true;
        }

        public virtual string GetStatisticsMessage()
        {
            TimeSpan onlineTime = DateTime.Now.Subtract(LoginTime);
            string stringOnlineTime;

            if (onlineTime.Days < 1)
                stringOnlineTime = $"Online time: {onlineTime.Hours} hours, {onlineTime.Minutes} minutes, {onlineTime.Seconds} seconds\n";
            else
                stringOnlineTime = $"Online time: {onlineTime.Days} days, {onlineTime.Hours} hours, {onlineTime.Minutes} minutes, {onlineTime.Seconds} seconds\n";

            StringBuilder stringBuilder = new();
            stringBuilder.Append("Options: /stats [top | rp | kills | deathblows | irs | heal | rez | player <name|target>]\n");
            stringBuilder.Append($"Statistics for {Player.Name} this Session:\n");
            stringBuilder.Append($"Total RP: {TotalRP}\n");
            stringBuilder.Append($"RP earned from kills: {RealmPointsEarnedFromKills}\n");
            stringBuilder.Append($"Kills that have earned RP: {KillsThatHaveEarnedRPs}\n");
            stringBuilder.Append($"Deathblows: {Deathblows}\n");
            stringBuilder.Append($"Deaths: {Deaths}\n");
            stringBuilder.Append($"HP healed: {HitPointsHealed} and {RPEarnedFromHitPointsHealed} RP gained from this heal\n");
            stringBuilder.Append($"Resurrections performed: {ResurrectionsPerformed}\n");
            stringBuilder.Append(stringOnlineTime);
            stringBuilder.Append($"RP/hour: {RPsPerHour(TotalRP, onlineTime)}\n");
            stringBuilder.Append($"Kills per death: {Divide(KillsThatHaveEarnedRPs, Deaths)}\n");
            stringBuilder.Append($"RP per kill: {Divide(RealmPointsEarnedFromKills, KillsThatHaveEarnedRPs)}\n");
            stringBuilder.Append($"\"I Remain Standing...\": {Divide(RealmPointsEarnedFromKills, Deaths)}\n");
            return stringBuilder.ToString();
        }

        public virtual void DisplayServerStatistics(GameClient client, string command, string playerName)
        {
            CreateServerStats(client);

            switch (command)
            {
                case "top":
                {
                    client.Out.SendCustomTextWindow("Top 20 Players", TOP_LIST);
                    break;
                }
                case "rp":
                {
                    client.Player.Out.SendMessage($"Top 20 for Realm Points\n{STATS_RP}", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    break;
                }
                case "lrp":
                {
                    client.Player.Out.SendMessage($"Top 20 for RP / Hour\n{STATS_LRP}", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    break;
                }
                case "kills":
                {
                    client.Player.Out.SendMessage($"Top 20 Killers\n{STATS_KILL}", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    break;
                }
                case "deathblows":
                {
                    client.Player.Out.SendMessage($"Top 20 Deathblows\n{STATS_DEATH}", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    break;
                }
                case "irs":
                {
                    client.Player.Out.SendMessage($"Top 20 \"I Remain Standing\"\n{STATS_IRS}", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    break;
                }
                case "heal":
                {
                    client.Player.Out.SendMessage($"Top 20 Healers\n{STATS_HEAL}", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    break;
                }
                case "rez":
                {
                    client.Player.Out.SendMessage($"Top 20 Resurrectors\n{STATS_RES}", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    break;
                }
                case "player":
                {
                    GamePlayer otherPlayer = ClientService.GetPlayerByPartialName(playerName, out _);

                    if (otherPlayer == null)
                    {
                        client.Player.Out.SendMessage($"No player with name {playerName} found!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                        return;
                    }

                    if (otherPlayer.StatsAnonFlag)
                    {
                        client.Player.Out.SendMessage($"{playerName} doesn't want you to view his stats.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                        return;
                    }

                    client.Player.Out.SendMessage(otherPlayer.Statistics.GetStatisticsMessage(), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    break;
                }
                default:
                {
                    client.Player.Out.SendMessage("Options: /stats [ top | rp | kills | deathblows | irs | heal | rez | player <name|target> ]", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    break;
                }
            }
        }

        public static uint Divide(uint dividend, uint divisor)
        {
            return divisor == 0 ? dividend : dividend == 0 ? 0 : dividend / divisor;
        }

        public static float RPsPerHour(uint realmPoints, TimeSpan time)
        {
            if (realmPoints == 0)
                return 0f;

            float days = time.Days;
            float hours = time.Hours;
            float minutes = time.Minutes;
            float seconds = time.Seconds;
            return realmPoints / (days * 24 + hours + minutes / 60 + seconds / (60 * 60));
        }
    }
}

namespace DOL.GS.GameEvents
{
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
                GameEventMgr.AddHandler(GameLivingEvent.GainedRealmPoints, new DOLEventHandler(GainedRealmPointsCallback));
                GameEventMgr.AddHandler(GameLivingEvent.Dying, new DOLEventHandler(DyingCallback));
                GameEventMgr.AddHandler(GameLivingEvent.CastFinished, new DOLEventHandler(FinishCastSpellCallback));
                GameEventMgr.AddHandler(GameLivingEvent.HealthChanged, new DOLEventHandler(HealthChangedCallback));
            }
        }

        public static void GainedRealmPointsCallback(DOLEvent e, object sender, EventArgs args)
        {
            if (sender is not GamePlayer player || args is not GainedRealmPointsEventArgs gargs)
                return;

            if (player.Statistics is not PlayerStatistics stats)
                return;

            stats.TotalRP += (uint) gargs.RealmPoints;
        }

        public static void DyingCallback(DOLEvent e, object sender, EventArgs args)
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

        public static void FinishCastSpellCallback(DOLEvent e, object sender, EventArgs args)
        {
            if (sender is not GamePlayer caster || args is not CastingEventArgs fargs)
                return;

            if (fargs.SpellHandler.Spell.SpellType == eSpellType.Resurrect)
            {
                if (caster.Statistics is PlayerStatistics stats)
                    stats.ResurrectionsPerformed++;
            }
        }

        public static void HealthChangedCallback(DOLEvent e, object sender, EventArgs args)
        {
            HealthChangedEventArgs hargs = args as HealthChangedEventArgs;

            if (hargs.ChangeType == eHealthChangeType.Spell)
            {
                if (hargs.ChangeSource is not GamePlayer player)
                    return;

                if (player.Statistics is PlayerStatistics stats)
                    stats.HitPointsHealed += (uint) hargs.ChangeAmount;
            }
        }

        public static uint RPsEarnedFromKill(GamePlayer killer, GamePlayer killedPlayer)
        {
            long noExpSeconds = ServerProperties.Properties.RP_WORTH_SECONDS;

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
}
