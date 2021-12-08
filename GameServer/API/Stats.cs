using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using DOL.Database;
using Microsoft.Extensions.Caching.Memory;

namespace DOL.GS.API;

public class Stats
{
    private const string _playerCountCacheKey = "api_player_count";
    private IMemoryCache _cache;

    public Stats()
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
}