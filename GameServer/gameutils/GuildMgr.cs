using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using DOL.Database;
using DOL.GS.PacketHandler;
using log4net;

namespace DOL.GS
{
    public sealed class GuildMgr
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public const long COST_RE_EMBLEM = 1000000; // 200 gold.

        private static readonly Lock _lock = new();
        private static readonly Dictionary<string, Guild> _nameToGuilds = new();
        private static readonly Dictionary<string, Guild> _idToGuild = new();
        private static readonly Dictionary<Guild, Dictionary<string, GuildMemberView>> _guildMemberViews = new();
        private static int _lastID;

        public static void AddPlayerToGuildMemberViews(GamePlayer player)
        {
            if (player?.Guild == null)
                return;

            lock (_lock)
            {
                if (!_guildMemberViews.TryGetValue(player.Guild, out Dictionary<string, GuildMemberView> value))
                    return;

                GuildMemberView member = new(player.InternalID,
                    player.Name,
                    player.Level.ToString(),
                    player.CharacterClass.ID.ToString(),
                    player.GuildRank.RankLevel.ToString(),
                    player.Group != null ? player.Group.MemberCount.ToString() : "1",
                    player.CurrentZone.Description,
                    player.GuildNote);
                value[player.InternalID] = member;
            }
        }

        public static void RemovePlayerFromGuildMemberViews(GamePlayer player)
        {
            if (player?.Guild == null)
                return;

            lock (_lock)
            {
                if (!_guildMemberViews.TryGetValue(player.Guild, out Dictionary<string, GuildMemberView> value))
                    return;

                value.Remove(player.InternalID);
            }
        }

        public static Dictionary<string, GuildMemberView> GetGuildMemberViews(Guild guild)
        {
            if (guild == null)
                return null;

            lock (_lock)
            {
                return !_guildMemberViews.TryGetValue(guild, out Dictionary<string, GuildMemberView> value) ?  null :  new(value);
            }
        }

        public static Guild CreateGuild(eRealm realm, string guildName, GamePlayer creator = null)
        {
            if (string.IsNullOrEmpty(guildName))
                return null;

            lock (_lock)
            {
                if (DoesGuildExist(guildName))
                {
                    creator?.Out.SendMessage($"{guildName} already exists. Please choose a different name.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return null;
                }

                DbGuild dbGuild = new()
                {
                    GuildName = guildName,
                    GuildID = Guid.NewGuid().ToString(),
                    Realm = (byte) realm
                };

                Guild guild = new(dbGuild);

                if (!guild.AddToDatabase())
                {
                    creator?.Out.SendMessage("Database error, unable to add a new guild.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return null;
                }

                AddGuild(guild);
                CreateRanks(guild);

                if (log.IsDebugEnabled)
                    log.Debug($"Create guild; guild name=\"{guildName}\" Realm={GlobalConstants.RealmToName(guild.Realm)}");

                return guild;

                static void CreateRanks(Guild guild)
                {
                    DbGuildRank rank;

                    for (int i = 0; i < 10; i++)
                    {
                        rank = CreateRank(guild, i);
                        GameServer.Database.AddObject(rank);
                        guild.Ranks[i] = rank;
                    }
                }
            }
        }

        public static bool DeleteGuild(string guildName)
        {
            if (string.IsNullOrEmpty(guildName))
                return false;

            lock (_lock)
            {
                Guild guild = GetGuildByName(guildName);

                if (guild == null)
                    return false;

                IList<DbGuild> dbGuilds = DOLDB<DbGuild>.SelectObjects(DB.Column("GuildID").IsEqualTo(guild.GuildID));

                foreach (DbGuild dbGuild in dbGuilds)
                {
                    foreach (DbCoreCharacter character in DOLDB<DbCoreCharacter>.SelectObjects(DB.Column("GuildID").IsEqualTo(dbGuild.GuildID)))
                        character.GuildID = string.Empty;
                }

                GameServer.Database.DeleteObject(dbGuilds);
                IList<DbGuildRank> ranks = DOLDB<DbGuildRank>.SelectObjects(DB.Column("GuildID").IsEqualTo(guild.GuildID));
                GameServer.Database.DeleteObject(ranks);

                foreach (GamePlayer onlineMember in guild.GetListOfOnlineMembers())
                {
                    onlineMember.Guild = null;
                    onlineMember.GuildID = string.Empty;
                    onlineMember.GuildName = string.Empty;
                    onlineMember.GuildRank = null;
                }

                RemoveGuild(guild);
            }

            return true;
        }

        public static bool RenameGuild(string oldName, string newName)
        {
            if (string.IsNullOrEmpty(oldName) || string.IsNullOrEmpty(newName))
                return false;

            lock (_lock)
            {
                Guild guild = GetGuildByName(oldName);

                if (guild == null)
                    return false;

                if (DoesGuildExist(newName))
                    return false;

                guild.Name = newName;
                _nameToGuilds.Remove(oldName);
                _nameToGuilds.Add(newName, guild);

                foreach (GamePlayer player in guild.GetListOfOnlineMembers())
                    player.GuildName = newName;
            }

            return true;
        }

        public static bool DoesGuildExist(string guildName)
        {
            if (string.IsNullOrEmpty(guildName))
                return false;

            lock (_lock)
            {
                return _nameToGuilds.ContainsKey(guildName);
            }
        }

        public static void ChangeGuildEmblem(GamePlayer player, int oldEmblem, int newEmblem)
        {
            if (player?.Guild == null)
                return;

            lock (_lock)
            {
                foreach (Guild guild in _nameToGuilds.Values)
                {
                    if (guild.Emblem == newEmblem)
                    {
                        player.Out.SendMessage("This emblem is already in use by another guild, please choose another one.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                        return;
                    }
                }

                player.Guild.Emblem = newEmblem;

                if (oldEmblem != 0)
                {
                    player.RemoveMoney(COST_RE_EMBLEM, null);
                    InventoryLogging.LogInventoryAction(player, $"(GUILD;{player.GuildName})", eInventoryActionType.Other, COST_RE_EMBLEM);
                    IList<DbInventoryItem> items = DOLDB<DbInventoryItem>.SelectObjects(DB.Column("Emblem").IsEqualTo(oldEmblem));

                    foreach (DbInventoryItem item in items)
                        item.Emblem = newEmblem;

                    GameServer.Database.SaveObject(items);

                    if (player.Guild.GuildOwnsHouse && player.Guild.GuildHouseNumber > 0)
                    {
                        Housing.House guildHouse = Housing.HouseMgr.GetHouse(player.Guild.GuildHouseNumber);

                        if (guildHouse != null)
                        {
                            guildHouse.Emblem = player.Guild.Emblem;
                            guildHouse.SaveIntoDatabase();
                            guildHouse.SendUpdate();
                        }
                    }
                }
            }
        }

        public static Guild GetGuildByName(string guildName)
        {
            if (string.IsNullOrEmpty(guildName))
                return null;

            lock (_lock)
            {
                if (_nameToGuilds.TryGetValue(guildName, out Guild guild))
                    return guild;
            }

            return null;
        }

        public static Guild GetGuildByGuildID(string guildId)
        {
            if (string.IsNullOrEmpty(guildId))
                return null;

            lock (_lock)
            {
                if (_idToGuild.TryGetValue(guildId, out Guild guild))
                    return guild;
            }

            return null;
        }

        public static bool LoadAllGuilds()
        {
            lock (_lock)
            {
                _nameToGuilds.Clear();
                _lastID = 0;
                LoadGuilds();
                LoadAlliances();
            }

            return true;

            static void LoadGuilds()
            {
                IList<DbGuild> dbGuilds = GameServer.Database.SelectAllObjects<DbGuild>();

                foreach (DbGuild dbGuild in dbGuilds)
                {
                    Guild guild = new(dbGuild);

                    if (dbGuild.Ranks == null ||
                        dbGuild.Ranks.Length < 10 ||
                        dbGuild.Ranks[0] == null ||
                        dbGuild.Ranks[1] == null ||
                        dbGuild.Ranks[2] == null ||
                        dbGuild.Ranks[3] == null ||
                        dbGuild.Ranks[4] == null ||
                        dbGuild.Ranks[5] == null ||
                        dbGuild.Ranks[6] == null ||
                        dbGuild.Ranks[7] == null ||
                        dbGuild.Ranks[8] == null ||
                        dbGuild.Ranks[9] == null)
                    {
                        if (log.IsErrorEnabled)
                            log.ErrorFormat($"GuildMgr: Ranks missing for {guild.Name}, creating new ones!");

                        RepairRanks(guild);

                        // Reload the guild to fix the relations.
                        guild = new Guild(DOLDB<DbGuild>.SelectObjects(DB.Column("GuildID").IsEqualTo(dbGuild.GuildID)).FirstOrDefault());
                    }

                    AddGuild(guild);

                    static void RepairRanks(Guild guild)
                    {
                        DbGuildRank rank;
                        guild.Ranks ??= new DbGuildRank[10];

                        for (int i = 0; i < 10; i++)
                        {
                            bool foundRank = false;

                            foreach (DbGuildRank dbRank in guild.Ranks)
                            {
                                if (dbRank == null)
                                    break;

                                if (dbRank.RankLevel == i)
                                {
                                    foundRank = true;
                                    break;
                                }
                            }

                            if (foundRank == false)
                            {
                                rank = CreateRank(guild, i);
                                rank.Title = rank.Title.Replace("Rank", "Repaired Rank");
                                GameServer.Database.AddObject(rank);
                            }
                        }
                    }
                }
            }

            static void LoadAlliances()
            {
                IList<DbGuildAlliance> dbAlliances = GameServer.Database.SelectAllObjects<DbGuildAlliance>();

                foreach (DbGuildAlliance dbAlliance in dbAlliances)
                {
                    Alliance alliance = new();
                    alliance.LoadFromDatabase(dbAlliance);

                    if (dbAlliance != null && dbAlliance.DBguilds != null)
                    {
                        foreach (DbGuild dbGuild in dbAlliance.DBguilds)
                        {
                            Guild guild = GetGuildByName(dbGuild.GuildName);
                            alliance.Guilds.Add(guild);
                            guild.alliance = alliance;
                        }
                    }
                }
            }
        }

        public static int SaveAllGuilds()
        {
            int count = 0;

            try
            {
                lock (_lock)
                {
                    foreach (Guild guild in _nameToGuilds.Values)
                    {
                        guild.SaveIntoDatabase();
                        count++;
                    }
                }
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.Error("Error saving guilds.", e);
            }

            return count;
        }

        public static List<Guild> GetGuilds()
        {
            return _nameToGuilds.Values.ToList();
        }

        private static void AddGuild(Guild guild)
        {
            _nameToGuilds.Add(guild.Name, guild);
            _idToGuild.Add(guild.GuildID, guild);
            guild.ID = ++_lastID;

            IList<DbCoreCharacter> characters = DOLDB<DbCoreCharacter>.SelectObjects(DB.Column("GuildID").IsEqualTo(guild.GuildID));
            Dictionary<string, GuildMemberView> guildMemberViews = new(characters.Count);

            foreach (DbCoreCharacter character in characters)
            {
                GuildMemberView member = new(character.ObjectId,
                    character.Name,
                    character.Level.ToString(),
                    character.Class.ToString(),
                    character.GuildRank.ToString(),
                    "0",
                    character.LastPlayed.ToShortDateString(),
                    character.GuildNote);

                guildMemberViews.Add(character.ObjectId, member);
            }

            _guildMemberViews.Add(guild, guildMemberViews);
        }

        private static void RemoveGuild(Guild guild)
        {
            guild.ClearOnlineMemberList();
            _nameToGuilds.Remove(guild.Name);
            _idToGuild.Remove(guild.GuildID);
            _guildMemberViews.Remove(guild);
        }

        private static DbGuildRank CreateRank(Guild guild, int rankLevel)
        {
            DbGuildRank rank = new()
            {
                GcHear = true,
                GuildID = guild.GuildID,
                RankLevel = (byte) rankLevel,
                Title = $"Rank {rankLevel}",
            };

            if (rankLevel >= 9)
                return rank;

            rank.GcSpeak = true;
            rank.View = true;

            if (rankLevel >= 8)
                return rank;

            rank.Emblem = true;

            if (rankLevel >= 7)
                return rank;

            rank.AcHear = true;

            if (rankLevel >= 6)
                return rank;

            rank.AcSpeak = true;

            if (rankLevel >= 5)
                return rank;

            rank.OcHear = true;

            if (rankLevel >= 4)
                return rank;

            rank.OcSpeak = true;

            if (rankLevel >= 3)
                return rank;

            rank.Invite = true;
            rank.Promote = true;

            if (rankLevel >= 2)
                return rank;

            rank.Release = true;
            rank.Upgrade = true;
            rank.Claim = true;

            if (rankLevel >= 1)
                return rank;

            rank.Remove = true;
            rank.Alli = true;
            rank.Dues = true;
            rank.Withdraw = true;
            rank.Title = "Guildmaster";
            rank.Buff = true;
            return rank;
        }

        public class GuildMemberView
        {
            public string InternalID { get; }
            public string Name { get; }
            public string Level { get; set; }
            public string ClassID { get; set; }
            public string Rank { get; set; }
            public string GroupSize { get; set; } = "0";
            public string ZoneOrOnline { get; set; }
            public string Note { get; set; } = string.Empty;

            public string this[eSocialWindowSortColumn i] => i switch
            {
                eSocialWindowSortColumn.NAME => Name,
                eSocialWindowSortColumn.CLASS_ID => ClassID,
                eSocialWindowSortColumn.GROUP => GroupSize,
                eSocialWindowSortColumn.LEVEL => Level,
                eSocialWindowSortColumn.NOTE => Note,
                eSocialWindowSortColumn.RANK => Rank,
                eSocialWindowSortColumn.ZONE_OR_ONLINE => ZoneOrOnline,
                _ => string.Empty,
            };

            public GuildMemberView(string internalID, string name, string level, string classID, string rank, string group, string zoneOrOnline, string note)
            {
                InternalID = internalID;
                Name = name;
                Level = level;
                ClassID = classID;
                Rank = rank;
                GroupSize = group;
                ZoneOrOnline = zoneOrOnline;
                Note = note;
            }

            public GuildMemberView(GamePlayer player)
            {
                InternalID = player.InternalID;
                Name = player.Name;
                Level = player.Level.ToString();
                ClassID = player.CharacterClass.ID.ToString();
                Rank = player.GuildRank.RankLevel.ToString();
                GroupSize = player.Group == null ? "1" : "2";
                ZoneOrOnline = player.CurrentZone.ToString();
                Note = player.GuildNote;
            }

            public string ToString(int position, int guildPop)
            {
                // This is used to send the correct information to the client social window.
                return $"E,{position},{guildPop},{Name},{Level},{ClassID},{Rank},{GroupSize},\"{ZoneOrOnline}\",\"{Note}\"";
            }

            public void UpdateMember(GamePlayer player)
            {
                Level = player.Level.ToString();
                ClassID = player.CharacterClass.ID.ToString();
                Rank = player.GuildRank.RankLevel.ToString();
                GroupSize = player.Group == null ? "1" : "2";
                Note = player.GuildNote;
                ZoneOrOnline = player.CurrentZone.Description;
            }

            public enum eSocialWindowSort : int
            {
                NAME_DESC = -1,
                NAME_ASC = 1,
                LEVEL_DESC = -2,
                LEVEL_ASC = 2,
                CLASS_DESC = -3,
                CLASS_ASC = 3,
                RANK_DESC = -4,
                RANK_ASC = 4,
                GROUP_DESC = -5,
                GROUP_ASC = 5,
                ZONE_OR_ONLINE_DESC = 6,
                ZONE_OR_ONLINE_ASC = -6,
                NOTE_DESC = 7,
                NOTE_ASC = -7
            }

            public enum eSocialWindowSortColumn : int
            {
                NAME = 0,
                LEVEL = 1,
                CLASS_ID = 2,
                RANK = 3,
                GROUP = 4,
                ZONE_OR_ONLINE = 5,
                NOTE = 6
            }
        }
    }
}
