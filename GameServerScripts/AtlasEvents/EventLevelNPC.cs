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
		public override bool AddToWorld()
		{
			Name = "Event Level";
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

            if (player.Level < EventLVCap)
            {
                player.Out.SendMessage("Hello " + player.Name + ",\n I can give you enough [experience] to defend your Realm in the battleground.",
                    eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                return true;
            }

            player.Out.SendMessage("You look like a veteran, go fight for your Realm!", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
            return true;
        }

        public override bool WhisperReceive(GameLiving source, string str)
        {
            if (!base.WhisperReceive(source, str))
                return false;
            int targetLevel = Properties.EVENT_LVCAP;

            GamePlayer player = source as GamePlayer;

            if (player == null)
                return false;

            switch(str)
            {
                case "experience":
                    if (player.Level >= targetLevel) {
                       player.Out.SendMessage("You look like a veteran, go fight for your Realm!", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                       return false;
                    }
                    else {
                        player.Out.SendMessage("I have given you enough experience to fight, now make Realm proud!", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                        player.Level = (byte)targetLevel;
                        return true;
                    }
                default: 
                    return false;

                return true;
            }
        }

    }
}