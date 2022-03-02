using System;
using System.Collections.Generic;
using System.Linq;
using DOL.Database;
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

        public KeepInfo() { }

        public KeepInfo(GameKeep keep)
        {
            if (keep == null)
                return;
            
            Name = keep.Name;
            OriginalRealm = GlobalConstants.RealmToName(keep.OriginalRealm);
            CurrentRealm = RealmIDtoString((int)keep.Realm);
            ClaimingGuild = keep.Guild?.Name;

        }
        
    }
    public List<KeepInfo> GetKeepsByRealm(eRealm realm)
    {
        ICollection<AbstractGameKeep> keepList;// = GameServer.KeepManager.GetKeepsOfRegion(1);
        //GameServer.KeepManager.GetKeepsOfRegion(100);
         //GameServer.KeepManager.GetKeepsOfRegion(200);
         
        List<KeepInfo> keepInfos = new List<KeepInfo>();
        
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

        return keepInfos;
    }

    public string GetDFOwner()
    {
        return GlobalConstants.RealmToName(DFEnterJumpPoint.DarknessFallOwner);
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

    #endregion
}