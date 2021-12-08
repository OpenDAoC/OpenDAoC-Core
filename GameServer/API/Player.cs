using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using DOL.Database;
using Microsoft.Extensions.Caching.Memory;

namespace DOL.GS.API;

public class Player
{
    private const string _playerCountCacheKey = "api_player_count";
    private IMemoryCache _cache;

    public Player()
    {
        _cache = new MemoryCache(new MemoryCacheOptions());
    }
    
    #region Player Count
    public class PlayerCount
    {
        public int Albion {get; set;}
        public int Midgard {get; set;}
        public int Hibernia {get; set;}
        public int Total {get; set;}
        public string Timestamp {get; set;}
    }
    public string GetPlayerCount()
    {
        if (!_cache.TryGetValue(_playerCountCacheKey, out PlayerCount playerCount))
        {
            int clients = WorldMgr.GetAllPlayingClientsCount();
            int albPlayers = WorldMgr.GetClientsOfRealmCount(eRealm.Albion);
            int midPlayers = WorldMgr.GetClientsOfRealmCount(eRealm.Midgard);
            int hibPlayers = WorldMgr.GetClientsOfRealmCount(eRealm.Hibernia);
            DateTime now = DateTime.Now;

            playerCount = new PlayerCount
            {
                Albion = albPlayers,
                Midgard = midPlayers,
                Hibernia = hibPlayers,
                Total = clients,
                Timestamp = now.ToString("dd-MM-yyyy hh:mm tt")
            };

            _cache.Set(_playerCountCacheKey, playerCount, DateTime.Now.AddMinutes(1));
        }

        var options = new JsonSerializerOptions()
        {
            WriteIndented = true
        };
        
        string jsonString = JsonSerializer.Serialize(playerCount,options);
        return jsonString;
    }
    #endregion
    
    #region Player Info

    public class PlayerInfo
    {
        public string Name { get; set; }
        public string Lastname { get; set; }
        public string Guild { get; set; }
        public string Realm { get; set; }
        public int RealmID { get; set; }
        public string ClassName { get; set; }
        public int ClassID { get; set; }
        public int Level { get; set; }
        public long RealmPoints { get; set; }
        public int RealmRank { get; set; }
        public int KillsAlbionPlayers { get; set; }
        public int KillsMidgardPlayers { get; set; }
        public int KillsHiberniaPlayers { get; set; }
        public int KillsAlbionDeathBlows { get; set; }
        public int KillsMidgardDeathBlows { get; set; }
        public int KillsHiberniaDeathBlows { get; set; }
        public int KillsAlbionSolo { get; set; }
        public int KillsMidgardSolo { get; set; }
        public int KillsHiberniaSolo { get; set; }

        public PlayerInfo() { }

        public PlayerInfo(DOLCharacters player)
        {
            if (player == null)
                return;

            Name = player.Name;
            Lastname = player.LastName;
            Guild = GuildMgr.GetGuildByGuildID(player.GuildID)?.Name;
            RealmID = player.Realm;
            Realm = RealmIDtoString(player.Realm);
            ClassID = player.Class;
            ClassName = ScriptMgr.FindCharacterClass(player.Class).Name;
            Level = player.Level;
            RealmPoints = player.RealmPoints;
            RealmRank = player.RealmLevel;
            KillsAlbionPlayers = player.KillsAlbionPlayers;
            KillsMidgardPlayers = player.KillsMidgardPlayers;
            KillsHiberniaPlayers = player.KillsHiberniaPlayers;
            KillsAlbionDeathBlows = player.KillsAlbionDeathBlows;
            KillsMidgardDeathBlows = player.KillsMidgardDeathBlows;
            KillsHiberniaDeathBlows = player.KillsHiberniaDeathBlows;
            KillsAlbionSolo = player.KillsAlbionSolo;
            KillsMidgardSolo = player.KillsMidgardSolo;
            KillsHiberniaSolo = player.KillsHiberniaSolo;
        }
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