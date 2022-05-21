using System;
using System.Collections.Generic;
using System.Linq;
using DOL.Database;
using Microsoft.Extensions.Caching.Memory;

namespace DOL.GS.API;

public class Player
{
    private IMemoryCache _cache;

    public Player()
    {
        _cache = new MemoryCache(new MemoryCacheOptions());
    }
    
    #region Player Info

    public class PlayerInfo
    {
        public string Name { get; set; }
        public string Lastname { get; set; }
        public string Guild { get; set; }
        public string Realm { get; set; }
        public int RealmID { get; set; }
        public string Race { get; set; }
        public int RaceID { get; set; }
        public string Class { get; set; }
        public int ClassID { get; set; }
        public int Level { get; set; }
        public long RealmPoints { get; set; }
        public string RealmRank { get; set; }
        public int KillsAlbionPlayers { get; set; }
        public int KillsMidgardPlayers { get; set; }
        public int KillsHiberniaPlayers { get; set; }
        public int KillsAlbionDeathBlows { get; set; }
        public int KillsMidgardDeathBlows { get; set; }
        public int KillsHiberniaDeathBlows { get; set; }
        public int KillsAlbionSolo { get; set; }
        public int KillsMidgardSolo { get; set; }
        public int KillsHiberniaSolo { get; set; }
        public int pvpDeaths { get; set; }

        public PlayerInfo() { }

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
            
        }
    }

    public static string GetRR(int realmLevel)
    {
        int RR = realmLevel + 10;
        
        string realmRank = "";

        if (RR >= 100)
        { 
            realmRank = $"{RR.ToString().Substring(0, 2)}L{RR.ToString().Substring(2, 1)}";
        }
        else
        {
            realmRank = $"{RR.ToString().Substring(0, 1)}L{RR.ToString().Substring(1, 1)}";
        }
        
        return realmRank;
    }

    public bool GetDiscord(string accountName)
    {
        var account = DOLDB<Account>.SelectObject(DB.Column("Name").IsEqualTo(accountName));
        Console.WriteLine(account.DiscordID);
        return account?.DiscordID is not null;
    }
    
    public static string RealmIDtoString(int realm)
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
        string _playerInfoCacheKey = "api_player_info_" + playerName;

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
    public List<PlayerInfo> GetAllPlayers()
    {
        string _allPlayersCacheKey = "api_all_players";

        if (!_cache.TryGetValue(_allPlayersCacheKey, out List<PlayerInfo> allPlayers))
        {            
            var players = DOLDB<DOLCharacters>.SelectAllObjects();

            allPlayers = new List<PlayerInfo>(players.Count);

            allPlayers.AddRange(players.Select(x => new PlayerInfo(x)));            

            _cache.Set(_allPlayersCacheKey, allPlayers, DateTime.Now.AddMinutes(120));
        }

        return allPlayers;
    }
    
    public IList<PlayerInfo> GetPlayersByGuild(string guildName)
    {
        string _allPlayerByGuildCacheKey = "api_all_players_" + guildName;
        
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