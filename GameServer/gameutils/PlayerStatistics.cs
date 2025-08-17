using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using DOL.Database;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
    public class PlayerStatistics
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

        private readonly Lock _lock = new();

        private uint _totalRealmPointsEarned;
        private uint _realmPointsEarnedFromKills;
        private uint _killsThatHaveEarnedRealmPoints;
        private uint _deathblows;
        private uint _deaths;
        private uint _hitPointsHealed;
        private uint _realmPointsEarnedFromHitPointsHealed;
        private uint _resurrectionsPerformed;

        public uint TotalRealmPointsEarned => _totalRealmPointsEarned;
        public uint RealmPointsEarnedFromKills => _realmPointsEarnedFromKills;
        public uint KillsThatHaveEarnedRealmPoints => _killsThatHaveEarnedRealmPoints;
        public uint Deathblows => _deathblows;
        public uint Deaths => _deaths;
        public uint HitPointsHealed => _hitPointsHealed;
        public uint RealmPointsEarnedFromHitPointsHealed => _realmPointsEarnedFromHitPointsHealed;
        public uint ResurrectionsPerformed => _resurrectionsPerformed;

        public void AddToTotalRealmPointsEarned(uint realmPointsEarned)
        {
            lock (_lock)
            {
                _totalRealmPointsEarned += realmPointsEarned;
            }
        }

        public void AddToRealmPointsEarnedFromKills(uint realmPointsEarned)
        {
            lock (_lock)
            {
                _realmPointsEarnedFromKills += realmPointsEarned;
                _killsThatHaveEarnedRealmPoints++;
            }
        }

        public void AddToDeathblows()
        {
            lock (_lock)
            {
                _deathblows++;
            }
        }

        public void AddToDeaths()
        {
            lock (_lock)
            {
                _deaths++;
            }
        }

        public void AddToHitPointsHealed(uint hitPointsHealed)
        {
            lock (_lock)
            {
                _hitPointsHealed += hitPointsHealed;
            }
        }

        public void AddToRealmPointsEarnedFromHitPointsHealed(uint realmPointsEarned)
        {
            lock (_lock)
            {
                _realmPointsEarnedFromHitPointsHealed += realmPointsEarned;
            }
        }

        public void AddToResurrectionsPerformed()
        {
            lock (_lock)
            {
                _resurrectionsPerformed++;
            }
        }

        public GamePlayer Player { get; }
        public DateTime LoginTime { get; }

        public PlayerStatistics(GamePlayer player)
        {
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

            DbCoreCharacter[] chars = DOLDB<DbCoreCharacter>.SelectObjects(DB.Column("RealmPoints").IsGreaterThan(213881)).OrderByDescending(dc => dc.RealmPoints).Take(100).ToArray();

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

            foreach (GamePlayer otherPlayer in ClientService.Instance.GetNonGmPlayers())
            {
                if (!otherPlayer.IgnoreStatistics && otherPlayer.Statistics is PlayerStatistics stats)
                {
                    if (otherPlayer.RealmLevel > 31)
                    {
                        allStatsRp.Add(new StatToCount(otherPlayer.Name, stats.TotalRealmPointsEarned));
                        TimeSpan onlineTime = DateTime.Now.Subtract(stats.LoginTime);
                        allStatsLrp.Add(new StatToCount(otherPlayer.Name, (uint) Math.Round(RPsPerHour(stats.TotalRealmPointsEarned, onlineTime))));
                        allStatIrs.Add(new StatToCount(otherPlayer.Name, (uint) Math.Round(stats.TotalRealmPointsEarned / Math.Max(1.0, stats.Deaths))));
                    }

                    allStatsKill.Add(new StatToCount(otherPlayer.Name, stats.KillsThatHaveEarnedRealmPoints));
                    allStatDeath.Add(new StatToCount(otherPlayer.Name, stats.Deathblows));
                    allStatHeal.Add(new StatToCount(otherPlayer.Name, stats.HitPointsHealed));
                    allStatsRes.Add(new StatToCount(otherPlayer.Name, stats.ResurrectionsPerformed));
                    allStatsRpEarnedFromHeal.Add(new StatToCount(otherPlayer.Name, stats.RealmPointsEarnedFromHitPointsHealed));
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

            STATS_RP = string.Empty;
            STATS_LRP = string.Empty;
            STATS_KILL = string.Empty;
            STATS_DEATH = string.Empty;
            STATS_IRS = string.Empty;
            STATS_HEAL = string.Empty;
            STATS_RES = string.Empty;

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
            stringBuilder.Append($"Total RP: {TotalRealmPointsEarned}\n");
            stringBuilder.Append($"RP earned from kills: {RealmPointsEarnedFromKills}\n");
            stringBuilder.Append($"Kills that have earned RP: {KillsThatHaveEarnedRealmPoints}\n");
            stringBuilder.Append($"Deathblows: {Deathblows}\n");
            stringBuilder.Append($"Deaths: {Deaths}\n");
            stringBuilder.Append($"HP healed: {HitPointsHealed} and {RealmPointsEarnedFromHitPointsHealed} RP gained from this heal\n");
            stringBuilder.Append($"Resurrections performed: {ResurrectionsPerformed}\n");
            stringBuilder.Append(stringOnlineTime);
            stringBuilder.Append($"RP/hour: {RPsPerHour(TotalRealmPointsEarned, onlineTime)}\n");
            stringBuilder.Append($"Kills per death: {Divide(KillsThatHaveEarnedRealmPoints, Deaths)}\n");
            stringBuilder.Append($"RP per kill: {Divide(RealmPointsEarnedFromKills, KillsThatHaveEarnedRealmPoints)}\n");
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
                    GamePlayer otherPlayer = ClientService.Instance.GetPlayerByPartialName(playerName, out _);

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
