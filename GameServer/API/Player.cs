using System;
using System.Collections.Generic;
using System.Text.Json;
using DOL.Database;
using Microsoft.Extensions.Caching.Memory;

namespace DOL.GS.API;

internal class Player
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
    }
    public string GetPlayerCount()
    {
        if (!_cache.TryGetValue(_playerCountCacheKey, out PlayerCount playerCount))
        {
            int clients = WorldMgr.GetAllPlayingClientsCount();
            int AlbPlayers = WorldMgr.GetClientsOfRealmCount(eRealm.Albion);
            int MidPlayers = WorldMgr.GetClientsOfRealmCount(eRealm.Midgard);
            int HibPlayers = WorldMgr.GetClientsOfRealmCount(eRealm.Hibernia);

            playerCount = new PlayerCount
            {
                Albion = AlbPlayers,
                Midgard = MidPlayers,
                Hibernia = HibPlayers,
                Total = clients
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
        public string Class { get; set; }
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
    }
    
    public string GetPlayerInfo(string playerName)
    {
        string _playerInfoCacheKey = "api_player_info_" + playerName;

        if (!_cache.TryGetValue(_playerInfoCacheKey, out PlayerInfo playerInfo))
        {
            var player = DOLDB<DOLCharacters>.SelectObject(DB.Column("Name").IsEqualTo(playerName));
            
            if (player == null)
                return "Player not found";
            
            playerInfo = new PlayerInfo()
            {
                Name = player.Name,
                Lastname = player.LastName,
                Guild = player.GuildID,
                Realm = player.Realm.ToString(),
                Class = player.Class.ToString(),
                Level = player.Level,
                RealmPoints = player.RealmPoints,
                RealmRank = player.RealmLevel,
                KillsAlbionPlayers = player.KillsAlbionPlayers,
                KillsMidgardPlayers = player.KillsMidgardPlayers,
                KillsHiberniaPlayers = player.KillsHiberniaPlayers,
                KillsAlbionDeathBlows = player.KillsAlbionDeathBlows,
                KillsMidgardDeathBlows = player.KillsMidgardDeathBlows,
                KillsHiberniaDeathBlows = player.KillsHiberniaDeathBlows,
                KillsAlbionSolo = player.KillsAlbionSolo,
                KillsMidgardSolo = player.KillsMidgardSolo,
                KillsHiberniaSolo = player.KillsHiberniaSolo
            };
        }
        
        _cache.Set(_playerInfoCacheKey, playerInfo, DateTime.Now.AddMinutes(1));
        
        var options = new JsonSerializerOptions()
        {
            WriteIndented = true
        };
        
        string jsonString = JsonSerializer.Serialize(playerInfo,options);
        return jsonString;
    }

    #endregion
}