using System;
using System.Collections.Generic;
using System.Linq;
using DOL.Database;
using Microsoft.Extensions.Caching.Memory;

namespace DOL.GS.API;

public class Player
{
    private readonly IMemoryCache _cache;

    public Player()
    {
        _cache = new MemoryCache(new MemoryCacheOptions());
    }

    #region Player Info

    public class PlayerInfo
    {
        public PlayerInfo()
        {
        }

        public PlayerInfo(DOLCharacters player)
        {
            if (player == null)
                return;

            var DBRace = DOLDB<Race>.SelectObject(DB.Column("ID").IsEqualTo(player.Race));

            Name = player.Name;
            Lastname = player.LastName;
            Guild = GuildMgr.GetGuildByGuildID(player.GuildID)?.Name;
            Realm = RealmIDtoString(player.Realm);
            RealmID = player.Realm;
            Race = DBRace.Name;
            RaceID = player.Race;
            Class = ScriptMgr.FindCharacterClass(player.Class).Name;
            ClassID = player.Class;
            Level = player.Level;
            RealmPoints = player.RealmPoints;
            RealmRank = GetRR(player.RealmLevel);
            KillsAlbionPlayers = player.KillsAlbionPlayers;
            KillsMidgardPlayers = player.KillsMidgardPlayers;
            KillsHiberniaPlayers = player.KillsHiberniaPlayers;
            KillsAlbionDeathBlows = player.KillsAlbionDeathBlows;
            KillsMidgardDeathBlows = player.KillsMidgardDeathBlows;
            KillsHiberniaDeathBlows = player.KillsHiberniaDeathBlows;
            KillsAlbionSolo = player.KillsAlbionSolo;
            KillsMidgardSolo = player.KillsMidgardSolo;
            KillsHiberniaSolo = player.KillsHiberniaSolo;
            pvpDeaths = player.DeathsPvP;
            PrimaryTradeSkill = player.CraftingPrimarySkill;
            TradeSkills = player.SerializedCraftingSkills;
        }

        public string Name { get; }
        public string Lastname { get; }
        public string Guild { get; }
        public string Realm { get; }
        public int RealmID { get; }
        public string Race { get; }
        public int RaceID { get; }
        public string Class { get; }
        public int ClassID { get; }
        public int Level { get; }
        public long RealmPoints { get; }
        public string RealmRank { get; }
        public int KillsAlbionPlayers { get; }
        public int KillsMidgardPlayers { get; }
        public int KillsHiberniaPlayers { get; }
        public int KillsAlbionDeathBlows { get; }
        public int KillsMidgardDeathBlows { get; }
        public int KillsHiberniaDeathBlows { get; }
        public int KillsAlbionSolo { get; }
        public int KillsMidgardSolo { get; }
        public int KillsHiberniaSolo { get; }
        public int pvpDeaths { get; }
        public int PrimaryTradeSkill { get; }
        public string TradeSkills { get; }
    }

    public class PlayerSpec
    {
        public PlayerSpec()
        {
        }
    
        public PlayerSpec(DOLCharacters player)
        {
            if (player == null)
                return;
            var specs = new Dictionary<string,int>();
            var realmAbilities = new Dictionary<string,int>();

            player.SerializedSpecs.Split(';').ToList().ForEach(x =>
            {
                var spec = x.Split('|');
                if (spec.Length == 2)
                {
                    specs.Add(spec[0], int.Parse(spec[1]));
                }
            });
            
            player.SerializedRealmAbilities.Split(';').ToList().ForEach(x =>
            {
                var spec = x.Split('|');
                if (spec.Length == 2)
                {
                    realmAbilities.Add(spec[0], int.Parse(spec[1]));
                }
            });
            
            Name = player.Name;
            Level = player.Level;
            Class = ScriptMgr.FindCharacterClass(player.Class).Name;
            Specializations = specs;
            RealmAbilities = realmAbilities;
        }
        
        public string Name { get; }
        public int Level { get; }
        public string Class { get; }
        public Dictionary<string, int> Specializations { get; }
        public Dictionary<string, int> RealmAbilities { get; }
    }

    private static string GetRR(int realmLevel)
    {
        var RR = realmLevel + 10;

        var realmRank = "";

        if (RR >= 100)
            realmRank = $"{RR.ToString().Substring(0, 2)}L{RR.ToString().Substring(2, 1)}";
        else
            realmRank = $"{RR.ToString().Substring(0, 1)}L{RR.ToString().Substring(1, 1)}";

        return realmRank;
    }

    public static bool GetDiscord(string accountName)
    {
        var account = DOLDB<Account>.SelectObject(DB.Column("Name").IsEqualTo(accountName));
        Console.WriteLine(account.DiscordID);
        return account.DiscordID is not (null or "");
    }

    private static string RealmIDtoString(int realm)
    {
        switch (realm)
        {
            case 0: return "None";
            case 1: return "Albion";
            case 2: return "Midgard";
            case 3: return "Hibernia";
            default: return "None";
        }
    }

    public PlayerInfo GetPlayerInfo(string playerName)
    {
        var _playerInfoCacheKey = "api_player_info_" + playerName;

        if (!_cache.TryGetValue(_playerInfoCacheKey, out PlayerInfo playerInfo))
        {
            var player = DOLDB<DOLCharacters>.SelectObject(DB.Column("Name").IsEqualTo(playerName));

            if (player == null)
                return null;

            playerInfo = new PlayerInfo(player);

            _cache.Set(_playerInfoCacheKey, playerInfo, DateTime.Now.AddMinutes(1));
        }

        return playerInfo;
    }
    
    public PlayerSpec GetPlayerSpec(string playerName)
    {
        var player = DOLDB<DOLCharacters>.SelectObject(DB.Column("Name").IsEqualTo(playerName));

        if (player == null)
            return null;

        var specs = new PlayerSpec(player);

        return specs;
    }

    public List<PlayerInfo> GetAllPlayers()
    {
        var _allPlayersCacheKey = "api_all_players";

        if (!_cache.TryGetValue(_allPlayersCacheKey, out List<PlayerInfo> allPlayers))
        {
            var dayLimit = DateTime.Now.Subtract(TimeSpan.FromDays(31));
            
            var players = GameServer.Database.SelectObjects<DOLCharacters>(DB.Column("LastPlayed").IsGreatherThan(dayLimit));

            allPlayers = new List<PlayerInfo>(players.Count);

            allPlayers.AddRange(players.Select(x => new PlayerInfo(x)));

            _cache.Set(_allPlayersCacheKey, allPlayers, DateTime.Now.AddMinutes(120));
        }

        return allPlayers;
    }

    public IList<PlayerInfo> GetPlayersByGuild(string guildName)
    {
        var _allPlayerByGuildCacheKey = "api_all_players_" + guildName;

        if (guildName == null)
            return null;

        var guild = GuildMgr.GetGuildByName(guildName);
        if (guild == null)
            return null;

        var guildId = guild.GuildID;

        if (!_cache.TryGetValue(_allPlayerByGuildCacheKey, out IList<PlayerInfo> allPlayers))
        {
            allPlayers = new List<PlayerInfo>();
            var players = DOLDB<DOLCharacters>.SelectObjects(DB.Column("GuildID").IsEqualTo(guildId));

            foreach (var player in players)
            {
                var thisPlayer = GetPlayerInfo(player.Name);
                if (thisPlayer == null)
                    continue;
                allPlayers.Add(thisPlayer);
            }

            _cache.Set(_allPlayerByGuildCacheKey, allPlayers, DateTime.Now.AddMinutes(120));
        }

        return allPlayers;
    }

    #endregion
}