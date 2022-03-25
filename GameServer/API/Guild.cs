using System;
using System.Collections.Generic;
using Microsoft.Extensions.Caching.Memory;

namespace DOL.GS.API;

public class Guild
{
    private static IMemoryCache _cache;

    public Guild()
    {
        _cache = new MemoryCache(new MemoryCacheOptions());
    }

    private static int eRealmToID(eRealm realm)
    {
        switch (realm)
        {
            case eRealm.None: return 0;
            case eRealm.Albion: return 1;
            case eRealm.Midgard: return 2;
            case eRealm.Hibernia: return 3;
            default: return 0;
        }
    }

    #region Guild Info

    public class GuildInfo
    {
        public string Name { get; set; }
        public int RealmID { get; set; }
        public string Realm { get; set; }
        public int Emblem { get; set; }
        public long RealmPoints { get; set; }
        public long BountyPoints { get; set; }
        
        public int Members { get; set; }
    }

    public GuildInfo GetGuildInfo(string guildName)
    {
        string _guildInfoCacheKey = "api_guild_info_" + guildName;

        if (!_cache.TryGetValue(_guildInfoCacheKey, out GuildInfo guildInfo))
        {
            var guild = GuildMgr.GetGuildByName(guildName);
            
            if (guild == null)
                return null;
            
            var membersCount = 0;
            var members = GuildMgr.GetAllGuildMembers(guild.GuildID);
            membersCount = members.Count;

            guildInfo = new GuildInfo()
            {
                Name = guild.Name,
                RealmID = eRealmToID(guild.Realm),
                Realm = GlobalConstants.RealmToName(guild.Realm),
                Emblem = guild.Emblem,
                RealmPoints = guild.RealmPoints,
                BountyPoints = guild.BountyPoints,
                Members = membersCount
            };

            _cache.Set(_guildInfoCacheKey, guildInfo, DateTime.Now.AddMinutes(10));
        }


        return guildInfo;
    }

    public List<GuildInfo> GetAllGuilds()
    {
        string _allGuildsCacheKey = "api_all_guilds";

        if (!_cache.TryGetValue(_allGuildsCacheKey, out List<GuildInfo> allGuilds))
        {
            var guilds = GuildMgr.GetAllGuilds();

            allGuilds = new List<GuildInfo>(guilds.Count);

            foreach (DOL.GS.Guild guild in guilds)
            {
                if (guild == null)
                    continue;
                
                var members = GuildMgr.GetAllGuildMembers(guild.GuildID);
                var membersCount = members.Count;

                GuildInfo guildInfo = new GuildInfo();
                guildInfo.Name = guild.Name;
                guildInfo.RealmID = eRealmToID(guild.Realm);
                guildInfo.Realm = GlobalConstants.RealmToName(guild.Realm);
                guildInfo.Emblem = guild.Emblem;
                guildInfo.RealmPoints = guild.RealmPoints;
                guildInfo.BountyPoints = guild.BountyPoints;
                guildInfo.Members = membersCount;

                allGuilds.Add(guildInfo);
            }

            _cache.Set(_allGuildsCacheKey, allGuilds, DateTime.Now.AddMinutes(120));
        }

        return allGuilds;
    }
    
    public List<GuildInfo> GetTopRPGuilds()
    {
        string _topRPGuildsCacheKey = "api_toprp_guilds";

        if (!_cache.TryGetValue(_topRPGuildsCacheKey, out List<GuildInfo> allGuilds))
        {
            var guilds = GuildMgr.GetAllGuilds();

            allGuilds = new List<GuildInfo>(guilds.Count);
            
            guilds.Sort((x, y) => y.RealmPoints.CompareTo(x.RealmPoints));

            foreach (DOL.GS.Guild guild in guilds.GetRange(0,10))
            {
                if (guild == null)
                    continue;
                
                var members = GuildMgr.GetAllGuildMembers(guild.GuildID);
                var membersCount = members.Count;

                GuildInfo guildInfo = new GuildInfo();
                guildInfo.Name = guild.Name;
                guildInfo.RealmID = eRealmToID(guild.Realm);
                guildInfo.Realm = GlobalConstants.RealmToName(guild.Realm);
                guildInfo.Emblem = guild.Emblem;
                guildInfo.RealmPoints = guild.RealmPoints;
                guildInfo.BountyPoints = guild.BountyPoints;
                guildInfo.Members = membersCount;

                allGuilds.Add(guildInfo);
            }

            _cache.Set(_topRPGuildsCacheKey, allGuilds, DateTime.Now.AddMinutes(120));
        }

        return allGuilds;
    }

    #endregion
}