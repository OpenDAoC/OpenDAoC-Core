using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
    public class PlayerStatistics
    {
        private const string USAGE = "Options: /stats [rp | lrp | kills | deathblows | irs | heal | rez | player <name|target>]";
        private const int TIME_BETWEEN_UPDATES = 60000;
        private const int TOP_COUNT = 20;

        private static long _nextUpdateTime;
        private static string _statsRp;
        private static string _statsLrp;
        private static string _statsKills;
        private static string _statsDeathblows;
        private static string _statsIrs;
        private static string _statsHeal;
        private static string _statsResurrect;
        private static readonly Lock _globalStatsLock = new();

        private GamePlayer _player;
        private DateTime _loginTime;
        public uint TotalRealmPointsEarned { get; private set; }
        public uint RealmPointsEarnedFromKills { get; private set; }
        public uint KillsThatHaveEarnedRealmPoints { get; private set; }
        public uint Deathblows { get; private set; }
        public uint Deaths { get; private set; }
        public uint HitPointsHealed { get; private set; }
        public uint RealmPointsEarnedFromHitPointsHealed { get; private set; }
        public uint ResurrectionsPerformed { get; private set; }
        private readonly Lock _playerStatsLock = new();

        public PlayerStatistics(GamePlayer player)
        {
            _player = player;
            _loginTime = DateTime.Now;
        }

        public void AddToTotalRealmPointsEarned(uint realmPointsEarned)
        {
            lock (_playerStatsLock)
            {
                TotalRealmPointsEarned += realmPointsEarned;
            }
        }

        public void AddToRealmPointsEarnedFromKills(uint realmPointsEarned)
        {
            lock (_playerStatsLock)
            {
                RealmPointsEarnedFromKills += realmPointsEarned;
                KillsThatHaveEarnedRealmPoints++;
            }
        }

        public void AddToDeathblows()
        {
            lock (_playerStatsLock)
            {
                Deathblows++;
            }
        }

        public void AddToDeaths()
        {
            lock (_playerStatsLock)
            {
                Deaths++;
            }
        }

        public void AddToHitPointsHealed(uint hitPointsHealed)
        {
            lock (_playerStatsLock)
            {
                HitPointsHealed += hitPointsHealed;
            }
        }

        public void AddToRealmPointsEarnedFromHitPointsHealed(uint realmPointsEarned)
        {
            lock (_playerStatsLock)
            {
                RealmPointsEarnedFromHitPointsHealed += realmPointsEarned;
            }
        }

        public void AddToResurrectionsPerformed()
        {
            lock (_playerStatsLock)
            {
                ResurrectionsPerformed++;
            }
        }

        public static void CreateServerStats(GameClient client)
        {
            if (GameLoop.GameLoopTime < _nextUpdateTime)
                return;

            lock (_globalStatsLock)
            {
                if (GameLoop.GameLoopTime < _nextUpdateTime)
                    return;

                List<StatEntry> topRp = new(TOP_COUNT);
                List<StatEntry> topLrp = new(TOP_COUNT);
                List<StatEntry> topKill = new(TOP_COUNT);
                List<StatEntry> topDeath = new(TOP_COUNT);
                List<StatEntry> topIrs = new(TOP_COUNT);
                List<StatEntry> topHeal = new(TOP_COUNT);
                List<StatEntry> topRes = new(TOP_COUNT);

                DateTime now = DateTime.Now;

                foreach (GamePlayer otherPlayer in ClientService.Instance.GetNonGmPlayers())
                {
                    if (otherPlayer.IgnoreStatistics || otherPlayer.IsAnonymous || otherPlayer.Statistics is not PlayerStatistics stats)
                        continue;

                    TryInsertTopStat(topRp, otherPlayer.Name, stats.TotalRealmPointsEarned);

                    uint rphs = (uint) Math.Round(RPsPerHour(stats.TotalRealmPointsEarned, now.Subtract(stats._loginTime)));
                    TryInsertTopStat(topLrp, otherPlayer.Name, rphs);

                    uint irs = (uint) Math.Round(stats.TotalRealmPointsEarned / Math.Max(1.0, stats.Deaths));
                    TryInsertTopStat(topIrs, otherPlayer.Name, irs);

                    TryInsertTopStat(topKill, otherPlayer.Name, stats.KillsThatHaveEarnedRealmPoints);
                    TryInsertTopStat(topDeath, otherPlayer.Name, stats.Deathblows);
                    TryInsertTopStat(topHeal, otherPlayer.Name, stats.HitPointsHealed, stats.RealmPointsEarnedFromHitPointsHealed);
                    TryInsertTopStat(topRes, otherPlayer.Name, stats.ResurrectionsPerformed);
                }

                StringBuilder sb = new(256);

                _statsRp = BuildStatString(sb, topRp, "RP");
                _statsLrp = BuildStatString(sb, topLrp, "RP/hour");
                _statsKills = BuildStatString(sb, topKill, "kills");
                _statsDeathblows = BuildStatString(sb, topDeath, "deathblows");
                _statsIrs = BuildStatString(sb, topIrs, "RP/death");
                _statsResurrect = BuildStatString(sb, topRes, "res");

                // Heal requires custom formatting due to the secondary value.
                sb.Clear();

                for (int i = 0; i < topHeal.Count; i++)
                {
                    sb.Append(i + 1)
                        .Append(". ")
                        .Append(topHeal[i].Name)
                        .Append(" with ")
                        .Append(topHeal[i].Value)
                        .Append(" HP and ")
                        .Append(topHeal[i].SecondaryValue)
                        .Append(" RP gained from heal\n");
                }

                _statsHeal = sb.ToString();

                _nextUpdateTime = GameLoop.GameLoopTime + TIME_BETWEEN_UPDATES;
            }
        }

        private static void TryInsertTopStat(List<StatEntry> list, string name, uint value, uint secondaryValue = 0)
        {
            if (value == 0 || (list.Count == TOP_COUNT && value <= list[^1].Value))
                return;

            int index = 0;

            while (index < list.Count && list[index].Value >= value)
                index++;

            list.Insert(index, new(name, value, secondaryValue));

            if (list.Count > TOP_COUNT)
                list.RemoveAt(TOP_COUNT);
        }

        private static string BuildStatString(StringBuilder sb, List<StatEntry> list, string suffix)
        {
            sb.Clear();

            for (int i = 0; i < list.Count; i++)
                sb.Append(i + 1).Append(". ").Append(list[i].Name).Append(" with ").Append(list[i].Value).Append(' ').Append(suffix).Append('\n');

            return sb.ToString();
        }

        public virtual string GetStatisticsMessage()
        {
            TimeSpan onlineTime = DateTime.Now.Subtract(_loginTime);

            StringBuilder sb = new();

            sb.Append(USAGE).Append('\n');
            sb.Append("Statistics for ").Append(_player.Name).Append(" this session:\n");
            sb.Append("Total RP: ").Append(TotalRealmPointsEarned).Append('\n');
            sb.Append("RP earned from kills: ").Append(RealmPointsEarnedFromKills).Append('\n');
            sb.Append("Kills that have earned RP: ").Append(KillsThatHaveEarnedRealmPoints).Append('\n');
            // Live shows solo kills here.
            sb.Append("Deathblows: ").Append(Deathblows).Append('\n');
            sb.Append("Deaths: ").Append(Deaths).Append('\n');
            sb.Append("HP healed: ").Append(HitPointsHealed).Append(" and ").Append(RealmPointsEarnedFromHitPointsHealed).Append(" RP gained from heal\n");
            sb.Append("Resurrections performed: ").Append(ResurrectionsPerformed).Append('\n');

            sb.Append("Online time: ");

            if (onlineTime.Days > 0)
                sb.Append(onlineTime.Days).Append(" days, ");

            sb.Append(onlineTime.Hours).Append(" hours, ")
                .Append(onlineTime.Minutes)
                .Append(" minutes, ")
                .Append(onlineTime.Seconds).Append(" seconds\n");

            sb.Append("RP/hour: ").Append(RPsPerHour(TotalRealmPointsEarned, onlineTime)).Append('\n');
            sb.Append("Kills per death: ").Append(Divide(KillsThatHaveEarnedRealmPoints, Deaths)).Append('\n');
            sb.Append("RP per kill: ").Append(Divide(RealmPointsEarnedFromKills, KillsThatHaveEarnedRealmPoints)).Append('\n');
            sb.Append("\"I Remain Standing...\": ").Append(Divide(RealmPointsEarnedFromKills, Deaths)).Append('\n');

            return sb.ToString();
        }

        public virtual void DisplayServerStatistics(GameClient client, string command, string playerName)
        {
            CreateServerStats(client);

            if (string.Equals(command, "rp", StringComparison.OrdinalIgnoreCase))
                client.Player.Out.SendMessage($"Top 20 for Realm Points\n{_statsRp}", eChatType.CT_System, eChatLoc.CL_SystemWindow);
            else if (string.Equals(command, "lrp", StringComparison.OrdinalIgnoreCase))
                client.Player.Out.SendMessage($"Top 20 for RP / Hour\n{_statsLrp}", eChatType.CT_System, eChatLoc.CL_SystemWindow);
            else if (string.Equals(command, "kills", StringComparison.OrdinalIgnoreCase))
                client.Player.Out.SendMessage($"Top 20 Killers\n{_statsKills}", eChatType.CT_System, eChatLoc.CL_SystemWindow);
            else if (string.Equals(command, "deathblows", StringComparison.OrdinalIgnoreCase))
                client.Player.Out.SendMessage($"Top 20 Deathblows\n{_statsDeathblows}", eChatType.CT_System, eChatLoc.CL_SystemWindow);
            else if (string.Equals(command, "irs", StringComparison.OrdinalIgnoreCase))
                client.Player.Out.SendMessage($"Top 20 \"I Remain Standing\"\n{_statsIrs}", eChatType.CT_System, eChatLoc.CL_SystemWindow);
            else if (string.Equals(command, "heal", StringComparison.OrdinalIgnoreCase))
                client.Player.Out.SendMessage($"Top 20 Healers\n{_statsHeal}", eChatType.CT_System, eChatLoc.CL_SystemWindow);
            else if (string.Equals(command, "rez", StringComparison.OrdinalIgnoreCase))
                client.Player.Out.SendMessage($"Top 20 Resurrectors\n{_statsResurrect}", eChatType.CT_System, eChatLoc.CL_SystemWindow);
            else if (string.Equals(command, "player", StringComparison.OrdinalIgnoreCase))
            {
                GamePlayer otherPlayer = ClientService.Instance.GetPlayerByPartialName(playerName, out _);

                if (otherPlayer == null || otherPlayer.IsAnonymous)
                {
                    client.Player.Out.SendMessage($"No player with name {playerName} found!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return;
                }

                if (otherPlayer.IgnoreStatistics)
                {
                    client.Player.Out.SendMessage($"{otherPlayer.Name} doesn't want you to view his stats.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return;
                }

                client.Player.Out.SendMessage(otherPlayer.Statistics.GetStatisticsMessage(), eChatType.CT_System, eChatLoc.CL_SystemWindow);
            }
            else
                client.Player.Out.SendMessage(USAGE, eChatType.CT_System, eChatLoc.CL_SystemWindow);
        }

        public static uint Divide(uint dividend, uint divisor)
        {
            return divisor == 0 ? dividend : dividend == 0 ? 0 : dividend / divisor;
        }

        public static double RPsPerHour(uint realmPoints, TimeSpan time)
        {
            return realmPoints == 0 || time.TotalHours <= 0 ? 0.0 : realmPoints / time.TotalHours;
        }

        private readonly struct StatEntry
        {
            public readonly string Name;
            public readonly uint Value;
            public readonly uint SecondaryValue;

            public StatEntry(string name, uint value, uint secondaryValue = 0)
            {
                Name = name;
                Value = value;
                SecondaryValue = secondaryValue;
            }
        }
    }
}
