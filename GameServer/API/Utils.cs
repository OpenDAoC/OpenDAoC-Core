using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using DOL.Database;
using DOL.GS.ServerProperties;
using Microsoft.Extensions.Caching.Memory;

namespace DOL.GS.API;

public class Utils
{
    private readonly IMemoryCache _cache;

    public Utils()
    {
        _cache = new MemoryCache(new MemoryCacheOptions());
    }

    #region Discord

    public string IsDiscordRequired()
    {
        var _discordRequiredKey = "api_discord_required";

        if (!_cache.TryGetValue(_discordRequiredKey, out bool discordRequired))
        {
            discordRequired = Properties.FORCE_DISCORD_LINK;
            _cache.Set(_discordRequiredKey, discordRequired, DateTime.Now.AddMinutes(1));
        }


        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };
        var jsonString = JsonSerializer.Serialize(discordRequired, options);
        return jsonString;
    }

    #endregion

    #region Player Count

    private class PlayerCount
    {
        public int Albion { get; set; }
        public int Midgard { get; set; }
        public int Hibernia { get; set; }
        public int Total { get; set; }
        public string Timestamp { get; set; }
    }

    public class ClientStatus
    {
        public uint PrivLevel { get; set; }
        public int State { get; set; }
        public bool IsTester { get; set; }
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
        var _uptimeCacheKey = "api_player_count";

        if (!_cache.TryGetValue(_uptimeCacheKey, out ServerUptime serverUptime))
        {
            var uptime = DateTime.Now.Subtract(startupTime);

            // ServerStats Uptime = new ServerStats();

            var sec = uptime.TotalSeconds;
            var min = Convert.ToInt64(sec) / 60;
            var hours = min / 60;
            var days = hours / 24;

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
        var _playerCountCacheKey = "api_player_count";

        if (!_cache.TryGetValue(_playerCountCacheKey, out PlayerCount playerCount))
        {
            var clients = WorldMgr.GetAllPlayingClientsCount();
            var albPlayers = WorldMgr.GetClientsOfRealmCount(eRealm.Albion);
            var midPlayers = WorldMgr.GetClientsOfRealmCount(eRealm.Midgard);
            var hibPlayers = WorldMgr.GetClientsOfRealmCount(eRealm.Hibernia);
            var now = DateTime.Now;

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

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        var jsonString = JsonSerializer.Serialize(playerCount, options);
        return jsonString;
    }

    public IList<Player.PlayerInfo> GetTopRP()
    {
        var _topRPKey = "api_top_rp";

        var _player = new Player();

        if (!_cache.TryGetValue(_topRPKey, out IList<Player.PlayerInfo> topRP))
        {
            topRP = new List<Player.PlayerInfo>();

            var topRpPlayers = DOLDB<DOLCharacters>.SelectObjects(DB.Column("RealmPoints").IsLessThan(7000000)).OrderByDescending(x => x.RealmPoints).Take(10).ToDictionary(x => x.Name, x => x.RealmPoints);
            
            foreach (var player in topRpPlayers)
            {
                var thisPlayer = _player.GetPlayerInfo(player.Key);
                topRP.Add(thisPlayer);
            }

            _cache.Set(_topRPKey, topRP, DateTime.Now.AddMinutes(120));
        }

        return topRP;
    }

    public IDictionary<string, ClientStatus> GetAllClientStatuses()
    {
        Dictionary<string, ClientStatus> playersOnline = new Dictionary<string, ClientStatus>();
        List<GameClient> clients = (List<GameClient>)WorldMgr.GetAllClients();

        foreach (GameClient client in clients)
        {
            if (client?.Account == null || client?.Account?.Name == null || client?.Account?.PrivLevel == null)
            {
                continue;
            }
            ClientStatus clientStatus = new ClientStatus();
            clientStatus.PrivLevel = client?.Account?.PrivLevel ?? 1;
            clientStatus.State = (int)client.ClientState;
            clientStatus.IsTester = client.Account.IsTester;
            playersOnline.TryAdd(client?.Account?.Name, clientStatus);
        }

        return playersOnline;
    }

    #endregion
}