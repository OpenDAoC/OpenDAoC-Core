/*
 *  Script by clait
 *  
 *  This NPC will level the player to 10, 20, 30, 40 or 50
 * 
 */

using System;
using DOL;
using DOL.GS;
using DOL.Events;
using DOL.GS.PacketHandler;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using DOL.Database;
using DOL.GS.ServerProperties;
using log4net;

namespace DOL.GS.Scripts
{
    public class EventLevelNPC : GameNPC
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public static int EventLVCap = Properties.EVENT_LVCAP;
        public static int realmPoints = Properties.EVENT_START_RP;
        
		public override bool AddToWorld()
		{
			Name = "Event Booster";
            GuildName = "Atlas Event";
            Model = 1198;
            Level = 50;
            Model = 2026;
            Size = 60;
            Flags |= eFlags.PEACE;
            return base.AddToWorld();
		}

        [ScriptLoadedEvent]
        public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            if (log.IsInfoEnabled)
                log.Info("EventLevelNPC is loading...");
        }
        public void SendReply(GamePlayer player, string msg)
        {
            player.Out.SendMessage(msg, eChatType.CT_System, eChatLoc.CL_PopupWindow);
        }
        public override bool Interact(GamePlayer player)
        {

            if (!base.Interact(player))
                return false;

            player.Out.SendMessage("Hello " + player.Name + ",\n\n I can give you enough [experience] to defend your Realm in the battleground.\n\n Additionally, you might be interested in a small [realm level] boost.",
                eChatType.CT_Say, eChatLoc.CL_PopupWindow);
            return true;
        }

        public override bool WhisperReceive(GameLiving source, string str)
        {
            if (!base.WhisperReceive(source, str))
                return false;
            int targetLevel = EventLVCap;

            GamePlayer player = source as GamePlayer;

            if (player == null)
                return false;

            switch(str)
            {
                case "experience":
                    if (player.Level < EventLVCap)
                    {
                        player.Out.SendMessage("I have given you enough experience to fight, now make Realm proud!", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        player.Level = (byte)targetLevel;
                        player.Health = player.MaxHealth;
                        return true;
                    }
                    player.Out.SendMessage("You are a veteran already, go fight for your Realm!", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    return false;

                case "realm level":
                    
                    if (player.RealmPoints < realmPoints)
                    {
                        long realmPointsToGive = realmPoints - player.RealmPoints;
                        player.GainRealmPoints(realmPointsToGive/(long)Properties.RP_RATE);
                        player.Out.SendMessage($"I have given you {realmPointsToGive} RPs, now go get some more yourself!", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        return true;
                    }
                    player.Out.SendMessage("You have killed enough enemies already, go kill more!", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    return false;


                default:
                    return false;
            }
        }

    }
}