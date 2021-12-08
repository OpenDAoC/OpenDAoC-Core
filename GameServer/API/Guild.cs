using System;
using Microsoft.Extensions.Caching.Memory;

namespace DOL.GS.API;

internal class Guild
{
    private IMemoryCache _cache;

    public Guild()
    {
        _cache = new MemoryCache(new MemoryCacheOptions());
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
    
    public static int eRealmToID(eRealm realm)
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
    
    public GuildInfo GetGuildInfo(string guildName)
    {
        string _guildInfoCacheKey = "api_guild_info_" + guildName;

        if (!_cache.TryGetValue(_guildInfoCacheKey, out GuildInfo guildInfo))
        {
            var guild = GuildMgr.GetGuildByName(guildName);
            
            if (guild == null)
                return null;
            
            guildInfo = new GuildInfo()
            {
                Name = guild.Name,
                RealmID = eRealmToID(guild.Realm),
                Realm = GlobalConstants.RealmToName(guild.Realm),
                Emblem = guild.Emblem,
                RealmPoints = guild.RealmPoints,
                BountyPoints = guild.BountyPoints
            };
            
            _cache.Set(_guildInfoCacheKey, guildInfo, DateTime.Now.AddMinutes(1));
        }
        

        return guildInfo;
    }

    #endregion
}