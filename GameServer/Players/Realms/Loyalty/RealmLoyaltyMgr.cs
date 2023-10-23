using System;
using System.Collections.Generic;
using System.Linq;
using Core.Database;
using Core.Database.Tables;
using Core.GS.Database;
using Core.GS.Enums;

namespace Core.GS.Players;

public class RealmLoyaltyMgr
{
    private static Dictionary<GamePlayer, PlayerLoyaltyUtil> _CachedPlayerLoyaltyDict;

    public static object CachedDictLock = new object();

    public RealmLoyaltyMgr()
    {
        _CachedPlayerLoyaltyDict = new Dictionary<GamePlayer, PlayerLoyaltyUtil>();
    }
    
    public static PlayerLoyaltyUtil GetPlayerLoyalty(GamePlayer player)
    {
        if (player == null) return null;
        PlayerLoyaltyUtil playerLoyalty = null;

        if (_CachedPlayerLoyaltyDict == null) _CachedPlayerLoyaltyDict = new Dictionary<GamePlayer, PlayerLoyaltyUtil>();
        bool alreadyExists = false;

        //need to do this since we can't safely lock the object in the IF statement below
        lock (CachedDictLock)
        {
            if (_CachedPlayerLoyaltyDict.ContainsKey(player))
                alreadyExists = true;
        }
        
        if (!alreadyExists)
        {
            CachePlayer(player);
        }

        lock(CachedDictLock)
            playerLoyalty = _CachedPlayerLoyaltyDict[player];

        return playerLoyalty;
    }

    public static void CachePlayer(GamePlayer player)
    {
        List<DbAccountXRealmLoyalty> realmLoyalty = new List<DbAccountXRealmLoyalty>(CoreDb<DbAccountXRealmLoyalty>.SelectObjects(DB.Column("AccountID").IsEqualTo(player.Client.Account.ObjectId)));
        if (_CachedPlayerLoyaltyDict == null) _CachedPlayerLoyaltyDict = new Dictionary<GamePlayer, PlayerLoyaltyUtil>();

        lock (CachedDictLock)
        {
            if (_CachedPlayerLoyaltyDict.ContainsKey(player))
            {
                _CachedPlayerLoyaltyDict.Remove(player);
            }
        }

        var midLoyalty = 0;
        var hibLoyalty = 0;
        var albLoyalty = 0;

        foreach (var realm in realmLoyalty)
        {
            if (realm.Realm == (int) ERealm.Albion)
                albLoyalty = realm.LoyalDays;
            if (realm.Realm == (int) ERealm.Hibernia)
                hibLoyalty = realm.LoyalDays;
            if (realm.Realm == (int) ERealm.Midgard)
                midLoyalty = realm.LoyalDays;
        }

        var albPercent = albLoyalty > 30 ? 30 / 30.0 : albLoyalty/30.0;
        var hibPercent = hibLoyalty > 30 ? 30/30.0 : hibLoyalty / 30.0;
        var midPercent = midLoyalty > 30 ? 30/30.0 : midLoyalty/ 30.0;
        
        var playerLoyalty = new PlayerLoyaltyUtil
        {
            AlbLoyaltyDays = albLoyalty,
            AlbPercent = albPercent,
            MidLoyaltyDays = midLoyalty,
            MidPercent = midPercent,
            HibLoyaltyDays = hibLoyalty,
            HibPercent = hibPercent
        };
            
        lock(CachedDictLock)
            _CachedPlayerLoyaltyDict.Add(player, playerLoyalty);
    }
    
    public static RealmLoyaltyUtil GetPlayerRealmLoyalty(GamePlayer player)
    {
        if (player == null) return null;

        RealmLoyaltyUtil realmLoyalty = null;

        PlayerLoyaltyUtil totalLoyalty = GetPlayerLoyalty(player);

        int days = 0;
        double percent = 0;

        switch (player.Realm)
        {
            case ERealm.Albion:
                days = totalLoyalty.AlbLoyaltyDays;
                percent = totalLoyalty.AlbPercent;
                break;
            case ERealm.Hibernia:
                days = totalLoyalty.HibLoyaltyDays;
                percent = totalLoyalty.HibPercent;
                break;
            case ERealm.Midgard:
                days = totalLoyalty.MidLoyaltyDays;
                percent = totalLoyalty.MidPercent;
                break;
        }

        realmLoyalty = new RealmLoyaltyUtil()
        {
            Days = days,
            Percent = percent
        };
        
        /*
        List<AccountXRealmLoyalty> Loyalty = new List<AccountXRealmLoyalty>(DOLDB<AccountXRealmLoyalty>.SelectObjects(DB.Column("AccountID").IsEqualTo(player.Client.Account.ObjectId)));

        int days = 0;
        double percent = 0;

        foreach (var realm in Loyalty)
        {
            if (realm.Realm != (int) player.Realm) continue;
            days = realm.LoyalDays;
            percent = realm.LoyalDays > 30 ? 30 / 30.0 : realm.LoyalDays / 30.0;
        }

        var realmLoyalty = new RealmLoyalty()
        {
            Days = days,
            Percent = percent
        };
        */

        return realmLoyalty;
    }

    public static void LoyaltyUpdateAddDays(GamePlayer player, int days)
    {
        List<DbAccountXRealmLoyalty> realmLoyalty = new List<DbAccountXRealmLoyalty>(CoreDb<DbAccountXRealmLoyalty>.SelectObjects(DB.Column("AccountID").IsEqualTo(player.Client.Account.ObjectId)));
        DateTime lastUpdatedTime = realmLoyalty.First().LastLoyaltyUpdate;
        lastUpdatedTime.AddDays(days);
    }
    
    public static void LoyaltyUpdateAddHours(GamePlayer player, int hours)
    {
        List<DbAccountXRealmLoyalty> realmLoyalty = new List<DbAccountXRealmLoyalty>(CoreDb<DbAccountXRealmLoyalty>.SelectObjects(DB.Column("AccountID").IsEqualTo(player.Client.Account.ObjectId)));
        DateTime lastUpdatedTime = realmLoyalty.First().LastLoyaltyUpdate;
        lastUpdatedTime.AddHours(hours);
    }
    
    public static DateTime GetLastLoyaltyUpdate(GamePlayer player)
    {
        List<DbAccountXRealmLoyalty> realmLoyalty = new List<DbAccountXRealmLoyalty>(CoreDb<DbAccountXRealmLoyalty>.SelectObjects(DB.Column("AccountID").IsEqualTo(player.Client.Account.ObjectId)));
        DateTime lastUpdatedTime = realmLoyalty.First().LastLoyaltyUpdate;
        return lastUpdatedTime;
    }

    public static void UpdateLoyalty(GamePlayer player, PlayerLoyaltyUtil loyalty)
    {
        
    }

    public static void HandlePVPKill(GamePlayer player)
    {
        return; //disabled due to low pop
        List<DbAccountXRealmLoyalty> rloyal = new List<DbAccountXRealmLoyalty>(CoreDb<DbAccountXRealmLoyalty>.SelectObjects(DB.Column("AccountID").IsEqualTo(player.Client.Account.ObjectId)));
        
        foreach (var loyalty in rloyal)
        {
            if (loyalty.Realm == (int) player.Realm)
            {
                //do nothing
            }
            else
            {
                loyalty.LoyalDays = 0;
                if (loyalty.LoyalDays < loyalty.MinimumLoyalDays)
                    loyalty.LoyalDays = loyalty.MinimumLoyalDays;
            }
        }

        GameServer.Database.SaveObject(rloyal);
    }
}