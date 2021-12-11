using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using DOL.Database;
using Microsoft.Extensions.Caching.Memory;

namespace DOL.GS.API;

public class Stats
{
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

    public class ServerUptime
    {
        public long Seconds { get; set; }
        public long Minutes { get; set; }
        public long Hours { get; set; }
        public long Days { get; set; }
        public string Uptime { get; set; }
    }

    public ServerUptime GetUptime(DateTime startupTime)
    {
        string _uptimeCacheKey = "api_player_count";

        if (!_cache.TryGetValue(_uptimeCacheKey, out ServerUptime serverUptime))
        {
            var uptime = DateTime.Now.Subtract(startupTime);

            // ServerStats Uptime = new ServerStats();
        
            double sec = uptime.TotalSeconds;
            long min = Convert.ToInt64(sec) / 60;
            long hours = min / 60;
            long days = hours / 24;

            serverUptime = new ServerUptime
            {
                Seconds = Convert.ToInt64(sec % 60),
                Minutes = min % 60,
                Hours = hours % 24,
                Days = days,
                Uptime = string.Format("{0}d {1}h {2}m {3:00}s", days, hours % 24, min % 60, sec % 60)
            };
            
            _cache.Set(_uptimeCacheKey, serverUptime, DateTime.Now.AddSeconds(30));
        }
        
        return serverUptime;
    }
    public string GetPlayerCount()
    {
        string _playerCountCacheKey = "api_player_count";
        
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
    public IList<Player.PlayerInfo> GetTopRP()
    {
        string _topRPKey = "api_top_rp";
        
        var _player = new Player();
        
        if(!_cache.TryGetValue(_topRPKey, out IList<Player.PlayerInfo> topRP))
        {
            topRP = new List<Player.PlayerInfo>();

            Dictionary<string,long> topRpPlayers = DOLDB<DOLCharacters>.SelectObjects(DB.Column("RealmPoints").IsLessThan(7000000)).OrderByDescending(x => x.RealmPoints).Take(10).ToDictionary(x => x.Name, x => x.RealmPoints);
            
            foreach (var player in topRpPlayers)
            {
                var thisPlayer = _player.GetPlayerInfo(player.Key);
                topRP.Add(thisPlayer);
            }
            
            _cache.Set(_topRPKey, topRP, DateTime.Now.AddMinutes(60));
        }
        return topRP;
    }
    #endregion
}