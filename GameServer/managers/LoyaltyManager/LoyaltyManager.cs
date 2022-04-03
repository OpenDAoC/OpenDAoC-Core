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
    public LoyaltyManager()
    {
    }
    
    public static PlayerLoyalty GetPlayerLoyalty(GamePlayer player)
    {
        if (player == null) return null;
        List<AccountXRealmLoyalty> realmLoyalty = new List<AccountXRealmLoyalty>(DOLDB<AccountXRealmLoyalty>.SelectObjects(DB.Column("AccountID").IsEqualTo(player.Client.Account.ObjectId)));
        
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

        return playerLoyalty;
    }
    
    public static RealmLoyalty GetPlayerRealmLoyalty(GamePlayer player)
    {
        if (player == null) return null;
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

        return realmLoyalty;
    }

}