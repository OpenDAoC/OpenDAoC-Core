using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.Json.Serialization;
using DOL.GS;
using DOL.Database;
using DOL.GS.PacketHandler;


namespace DOL.GS.Utils;


//Utility class for checking realmtimer of a character/account and acting accordingly 
public class RealmTimer
{

    public static bool CanPvP(GamePlayer player)
    {
        if (ServerProperties.Properties.PVP_REALM_TIMER_MINUTES == 0) return true;

        if (player == null) return false;
        
        Account playerAccount = GameServer.Database.FindObjectByKey<Account>(player.AccountName);

        if (playerAccount == null) return false;

        if (playerAccount.Realm_Timer_Last_Combat == null || playerAccount.Realm_Timer_Realm == 0) return true;

        double timeLeftOnTimer = TimeLeftOnTimer(player);
        if (timeLeftOnTimer <= 0) 
            return true; 
        else if (timeLeftOnTimer > 0 && player.Realm == (eRealm)CurrentRealm(player))
            return true;
        else
            return false;
    }

    public static void CheckRealmTimer(GamePlayer player)
    {
        bool playerInRVRZone = false;
        if (player.CurrentZone.IsRvR)
        {
            AbstractArea area = player.CurrentZone?.GetAreasOfSpot(player.X, player.Y, player.Z).FirstOrDefault() as AbstractArea;

            //forest sauvage
            if (player.CurrentZone.ID == 11)
            {
                //Check if in Castle Sauvage Area
                if (area != null && area.Description.Equals("Castle Sauvage"))
                    return;
                //Check if players Y loc is greater than 60K (South of Castle Sauvage)
                else if (player.Y > (player.CurrentZone.YOffset + 60000))
                    return;
                else
                    playerInRVRZone = true;
            }
            //snowdonia
            else if (player.CurrentZone.ID == 12) 
            {
                //Check if in Snowdonia Fortress Area
                if (area != null && area.Description.Equals("Snowdonia Fortress"))
                    return;
                //Check if players Y loc is greater than 58K and X loc is between 12K and 30K (South of Snowdonia Fortress)
                else if (player.Y > (player.CurrentZone.YOffset + 58000) && (player.CurrentZone.XOffset + 12000) < player.X && player.X < (player.CurrentZone.XOffset + 30000))
                    return;
                else
                    playerInRVRZone = true;
            }
            //uppland
            else if (player.CurrentZone.ID == 111) 
            {
                //Check if in Svasud Faste Area
                if (area != null && area.Description.Equals("Svasud Faste"))
                    return;
                else
                    playerInRVRZone = true;
            } 
            else
                playerInRVRZone = true;
        }

        if(playerInRVRZone && !CanPvP(player))
        {
            player.Out.SendMessage(
							"Your realm timer is set for another realm. You are unable to PvP for this realm until it expires! Please use /realmtimer to check the current realm timer status",
							eChatType.CT_System, eChatLoc.CL_SystemWindow);
            player.MoveToBind();
        }

    }

    public static void SaveRealmTimer(GamePlayer player)
    {
        //Don't save realmtimer during duels
        if(player.DuelTarget != null)
            return;

        Account playerAccount = GameServer.Database.FindObjectByKey<Account>(player.AccountName);
        
        DateTime LastCombatTickPvPDateTime = DateTime.Now.AddMilliseconds(-(GameLoop.GameLoopTime - player.LastCombatTickPvP));

        //Don't update realmtimer it is still in effect and players realm is not the realm_timer_realm
        eRealm current_realm_timer_realm = (eRealm)CurrentRealm(player);
        if(current_realm_timer_realm != eRealm.None && player.Realm != current_realm_timer_realm)
            return;
        //Save realm timer if LastCombatTickPvPDateTime is more recent than what is saved in DB
        else if(player.LastCombatTickPvP > 0 && LastCombatTickPvPDateTime > playerAccount.Realm_Timer_Last_Combat)
        {
            playerAccount.Realm_Timer_Last_Combat = LastCombatTickPvPDateTime;
            playerAccount.Realm_Timer_Realm = (int)player.Realm;
            GameServer.Database.SaveObject(playerAccount);
        }

        
    }

    public static int CurrentRealm(GamePlayer player)
    {
        //if realm timer is not active, set realm to none
        if(TimeLeftOnTimer(player) == 0)
            return (int)eRealm.None;

        Account playerAccount = GameServer.Database.FindObjectByKey<Account>(player.AccountName);
        DateTime LastCombatTickPvPDateTime = DateTime.Now.AddMilliseconds(-(GameLoop.GameLoopTime - player.LastCombatTickPvP));
        
        //Return Realm_Timer_Realm if realm timer is active. Help prevent realm timer from switching realms on duels/etc.
        if ((DateTime.Now - playerAccount.Realm_Timer_Last_Combat).TotalMinutes < ServerProperties.Properties.PVP_REALM_TIMER_MINUTES)
            return (int)playerAccount.Realm_Timer_Realm;
        //Return players current realm.
        else
            return (int)player.Realm;
    }

    public static double TimeLeftOnTimer(GamePlayer player)
    {
        Account playerAccount = GameServer.Database.FindObjectByKey<Account>(player.AccountName);

        double timeSinceLastCombat = (DateTime.Now - playerAccount.Realm_Timer_Last_Combat).TotalMinutes;
        //If DB realm_timer_last_combat value is within the pvp_realm_timer_minutes & this player is not the realm in DB, return the time remaing based on DB value
        if (timeSinceLastCombat < ServerProperties.Properties.PVP_REALM_TIMER_MINUTES && (eRealm)playerAccount.Realm_Timer_Realm != player.Realm)    
            return ServerProperties.Properties.PVP_REALM_TIMER_MINUTES - timeSinceLastCombat;

        //Get datetime of this players Last Combat Tick PvP
        DateTime LastCombatTickPvPDateTime = DateTime.Now.AddMilliseconds(-(GameLoop.GameLoopTime - player.LastCombatTickPvP));

        //Check if Realm_Timer_Last_Combat was more recent than LastCombatTickPvP
        if(player.LastCombatTickPvP == 0 || LastCombatTickPvPDateTime < playerAccount.Realm_Timer_Last_Combat)
            LastCombatTickPvPDateTime = playerAccount.Realm_Timer_Last_Combat;

        //Return time left on realm timer. If timeSinceLastCombat > PVP_REALM_TIMER_MINUTES, return 0;
        timeSinceLastCombat = (DateTime.Now - LastCombatTickPvPDateTime).TotalMinutes;
        if (timeSinceLastCombat < ServerProperties.Properties.PVP_REALM_TIMER_MINUTES)    
            return ServerProperties.Properties.PVP_REALM_TIMER_MINUTES - timeSinceLastCombat;
        else
            return 0;
    }
    
   

}