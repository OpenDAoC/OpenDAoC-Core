using System;
using System.Collections.Generic;
using DOL.GS.Keeps;
using DOL.GS.ServerRules;
using Microsoft.Extensions.Caching.Memory;

namespace DOL.GS.API;

public class Realm
{
    private IMemoryCache _cache;

    public Realm()
    {
        _cache = new MemoryCache(new MemoryCacheOptions());
    }

    #region Keep Info

    public class KeepInfo
    {
        public string Name { get; set; }
        public string OriginalRealm { get; set; }
        public string CurrentRealm { get; set; }
        public string ClaimingGuild { get; set; }
        public int Level { get; set; }
        public int UnderSiege { get; set; }

        public KeepInfo()
        {
        }
        public KeepInfo(GameKeep keep)
        {
            if (keep == null)
                return;

            Name = keep.Name;
            OriginalRealm = GlobalConstants.RealmToName(keep.OriginalRealm);
            CurrentRealm = RealmIDtoString((int) keep.Realm);
            ClaimingGuild = keep.Guild?.Name;
            Level = keep.DifficultyLevel;
            UnderSiege = keep.InCombat ? 1 : 0;
        }
    }

    public List<KeepInfo> GetKeepsByRealm(eRealm realm)
    {
        string _keepsCacheKey = "api_keeps_"+realm;
        var keepInfos = new List<KeepInfo>();
        var cache = _cache.Get<List<KeepInfo>>(_keepsCacheKey);
        
        if (cache == null)
        {
            ICollection<AbstractGameKeep> keepList;
        
            switch (realm){
                case eRealm.Albion:
                
                    keepList = GameServer.KeepManager.GetKeepsOfRegion(1);
                
                    foreach (AbstractGameKeep keep in keepList)
                    {
                        var gk = keep as GameKeep;
                        
                        if (gk != null)
                        {
                            if (gk.Name.ToLower().Contains("myrddin") || gk.Name.ToLower().Contains("excalibur"))
                                continue;
                            
                            keepInfos.Add(new KeepInfo(gk));

                        }
                    }
                    break;
            
                case eRealm.Midgard:
                    keepList = GameServer.KeepManager.GetKeepsOfRegion(100);
                
                    foreach (AbstractGameKeep keep in keepList)
                    {
                        var gk = keep as GameKeep;
                        
                        if (gk != null)
                        {
                            if (gk.Name.ToLower().Contains("grallarhorn") || gk.Name.ToLower().Contains("mjollner"))
                                continue;
                            keepInfos.Add(new KeepInfo(gk));

                        }
                    }
                    break;
            
                case eRealm.Hibernia:
                    keepList = GameServer.KeepManager.GetKeepsOfRegion(200);
                
                    foreach (AbstractGameKeep keep in keepList)
                    {
                        var gk = keep as GameKeep;
                        
                        if (gk != null)
                        {
                            if (gk.Name.ToLower().Contains("dagda") || gk.Name.ToLower().Contains("lamfhota"))
                                continue;
                            keepInfos.Add(new KeepInfo(gk));

                        }
                    }
                    break;
            }
            _cache.Set(_keepsCacheKey, keepInfos, DateTime.Now.AddMinutes(1));
        }
        else
        {
            keepInfos = cache;
        }
        
        return keepInfos;
    }

    #endregion

    #region Relic Info

    public class RelicInfo
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string OriginalRealm { get; set; }
        public string CurrentRealm { get; set; }
        public RelicInfo()
        {
        }

        public RelicInfo(GameRelic relic)
        {
            if (relic == null)
                return;

            Name = relic.Name;
            Type = relic.RelicType.ToString();
            OriginalRealm = GlobalConstants.RealmToName(relic.OriginalRealm);
            CurrentRealm = RealmIDtoString((int) relic.Realm);
        }
    }

    public List<RelicInfo> GetAllRelics()
    {
        string _allRelicsCacheKey = "api_relics_all";
        var relicInfos = new List<RelicInfo>();
        var cache = _cache.Get<List<RelicInfo>>(_allRelicsCacheKey);
        if (cache == null)
        {
            foreach (GameRelic relic in RelicMgr.getNFRelics())
            {
                var tempRelic = new RelicInfo(relic);
                relicInfos.Add(tempRelic);
            }
            _cache.Set(_allRelicsCacheKey, relicInfos, DateTime.Now.AddMinutes(1));
        }
        else
        {
            relicInfos = cache;
        }
        
        return relicInfos;
    }

    #endregion

    #region BG Info

    public class BGInfo
    {
        public string Name { get; set; }
        public string CurrentRealm { get; set; }
        public int UnderSiege { get; set; }

        public BGInfo()
        {
        }
        public BGInfo(GameKeep keep)
        {
            if (keep == null)
                return;

            Name = keep.Name;
            CurrentRealm = RealmIDtoString((int) keep.Realm);
            UnderSiege = keep.InCombat ? 1 : 0;
        }
    }
    public List<BGInfo> GetBGStatus()
    {
        var _keepsCacheKey = "api_keeps_bg";
        var keepInfos = new List<BGInfo>();
        var cache = _cache.Get<List<BGInfo>>(_keepsCacheKey);
        
        if (cache == null)
        {
            ICollection<AbstractGameKeep> keepList;
            
            ushort[] bgRegions = {253, 252, 251, 250};
            
            foreach (var region in bgRegions)
            {
                keepList = GameServer.KeepManager.GetKeepsOfRegion(region);
                
                foreach (AbstractGameKeep keep in keepList)
                {
                    var gk = keep as GameKeep;
                    
                    if (gk != null)
                    {
                        keepInfos.Add(new BGInfo(gk));

                    }
                }
            }
            
            _cache.Set(_keepsCacheKey, keepInfos, DateTime.Now.AddMinutes(1));
        }
        else
        {
            keepInfos = cache;
        }
        
        return keepInfos;
    }
    
    #endregion
    public string GetDFOwner()
    {
        return GlobalConstants.RealmToName(DFEnterJumpPoint.DarknessFallOwner);
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
}