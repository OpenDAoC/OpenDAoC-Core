using System;
using System.Collections.Generic;
using System.Linq;
using DOL.Database;
using DOL.Events;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;

namespace DOL.GS;

public class LoyaltyManager

{
    private static Dictionary<GamePlayer, PlayerLoyalty> _CachedPlayerLoyaltyDict;

    public static object CachedDictLock = new object();

    public LoyaltyManager()
    {
        _CachedPlayerLoyaltyDict = new Dictionary<GamePlayer, PlayerLoyalty>();
    }
    
    public static PlayerLoyalty GetPlayerLoyalty(GamePlayer player)
    {
        if (player == null) return null;
        PlayerLoyalty playerLoyalty = null;

        if (_CachedPlayerLoyaltyDict == null) _CachedPlayerLoyaltyDict = new Dictionary<GamePlayer, PlayerLoyalty>();
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
        List<AccountXRealmLoyalty> realmLoyalty = new List<AccountXRealmLoyalty>(DOLDB<AccountXRealmLoyalty>.SelectObjects(DB.Column("AccountID").IsEqualTo(player.Client.Account.ObjectId)));
        if (_CachedPlayerLoyaltyDict == null) _CachedPlayerLoyaltyDict = new Dictionary<GamePlayer, PlayerLoyalty>();

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
            if (realm.Realm == (int) eRealm.Albion)
                albLoyalty = realm.LoyalDays;
            if (realm.Realm == (int) eRealm.Hibernia)
                hibLoyalty = realm.LoyalDays;
            if (realm.Realm == (int) eRealm.Midgard)
                midLoyalty = realm.LoyalDays;
        }

        var albPercent = albLoyalty > 30 ? 30 / 30.0 : albLoyalty/30.0;
        var hibPercent = hibLoyalty > 30 ? 30/30.0 : hibLoyalty / 30.0;
        var midPercent = midLoyalty > 30 ? 30/30.0 : midLoyalty/ 30.0;
        
        var playerLoyalty = new PlayerLoyalty
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
    
    public static RealmLoyalty GetPlayerRealmLoyalty(GamePlayer player)
    {
        if (player == null) return null;

        RealmLoyalty realmLoyalty = null;

        PlayerLoyalty totalLoyalty = GetPlayerLoyalty(player);

        int days = 0;
        double percent = 0;

        switch (player.Realm)
        {
            case eRealm.Albion:
                days = totalLoyalty.AlbLoyaltyDays;
                percent = totalLoyalty.AlbPercent;
                break;
            case eRealm.Hibernia:
                days = totalLoyalty.HibLoyaltyDays;
                percent = totalLoyalty.HibPercent;
                break;
            case eRealm.Midgard:
                days = totalLoyalty.MidLoyaltyDays;
                percent = totalLoyalty.MidPercent;
                break;
        }

        realmLoyalty = new RealmLoyalty()
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
        List<AccountXRealmLoyalty> realmLoyalty = new List<AccountXRealmLoyalty>(DOLDB<AccountXRealmLoyalty>.SelectObjects(DB.Column("AccountID").IsEqualTo(player.Client.Account.ObjectId)));
        DateTime lastUpdatedTime = realmLoyalty.First().LastLoyaltyUpdate;
        lastUpdatedTime.AddDays(days);
    }
    
    public static void LoyaltyUpdateAddHours(GamePlayer player, int hours)
    {
        List<AccountXRealmLoyalty> realmLoyalty = new List<AccountXRealmLoyalty>(DOLDB<AccountXRealmLoyalty>.SelectObjects(DB.Column("AccountID").IsEqualTo(player.Client.Account.ObjectId)));
        DateTime lastUpdatedTime = realmLoyalty.First().LastLoyaltyUpdate;
        lastUpdatedTime.AddHours(hours);
    }
    
    public static DateTime GetLastLoyaltyUpdate(GamePlayer player)
    {
        List<AccountXRealmLoyalty> realmLoyalty = new List<AccountXRealmLoyalty>(DOLDB<AccountXRealmLoyalty>.SelectObjects(DB.Column("AccountID").IsEqualTo(player.Client.Account.ObjectId)));
        DateTime lastUpdatedTime = realmLoyalty.First().LastLoyaltyUpdate;
        return lastUpdatedTime;
    }

    public static void UpdateLoyalty(GamePlayer player, PlayerLoyalty loyalty)
    {
        
    }

}