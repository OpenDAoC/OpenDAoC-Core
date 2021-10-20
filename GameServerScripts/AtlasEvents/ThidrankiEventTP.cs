/*
 * 
 * ATLAS Thidranki Event Teleporter
 *
 */

using System;
using DOL.GS;
using DOL.Events;
using DOL.GS.PacketHandler;
using log4net;
using System.Reflection;

namespace DOL.GS.Scripts
{
    public class ThidrankiEventTP : GameNPC
	{
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		public static int EventRPCap = ServerProperties.Properties.EVENT_RPCAP;

		public static int TeleportDelay = 40000; //value in milliseconds
        public override bool AddToWorld()
        {
            Model = 2026;
            Name = "Event Teleporter";
            GuildName = "Atlas Event";
            Level = 50;
            Size = 60;
            Flags |= eFlags.PEACE;
            return base.AddToWorld();
        }
		public override bool Interact(GamePlayer player)
		{
			if (!base.Interact(player)) return false;
			TurnTo(player.X, player.Y);
			
			if (base.CurrentRegionID == 252)
			{
				player.Out.SendMessage("Hello " + player.Name + "!\n\n" + "If you need so, I can port you back to your Realm's [event zone]", eChatType.CT_Say,eChatLoc.CL_PopupWindow);
				return true;
			}
			if (player.Level > 24)
			{
				player.Out.SendMessage("Hello " + player.Name + "!\n\n" + "looks like you're ready to move on to the [next challenge]!", eChatType.CT_Say,eChatLoc.CL_PopupWindow);
				return true;
			}
			player.Out.SendMessage("Hello " + player.Name + "!\n\n" + "Are you ready to [fight]?", eChatType.CT_Say,eChatLoc.CL_PopupWindow);
			return true;
		}
		public override bool WhisperReceive(GameLiving source, string str)
		{
			if(!base.WhisperReceive(source,str)) return false;
		  	if(!(source is GamePlayer)) return false;
		    if (EventRPCap == 0) return false;
			GamePlayer t = (GamePlayer) source;
			TurnTo(t.X,t.Y);
			switch(str)
			{
				case "fight":
					if (!t.InCombatPvPInLast(TeleportDelay))
					{
						if (t.RealmPoints < EventRPCap)
						{
							switch (t.Realm)
							{
								case eRealm.Albion:
									t.MoveTo(252, 38113, 53507, 4160, 3268);
									break;
								case eRealm.Midgard:
									t.MoveTo(252, 53568, 23643, 4530, 3268);
									break;
								case eRealm.Hibernia:
									t.MoveTo(252, 17367, 18248, 4320, 3268);
									break;
							}
						}
						else { t.Client.Out.SendMessage("You have reached the Realm Rank cap for this event.", eChatType.CT_Say, eChatLoc.CL_PopupWindow); }
					}
					else
					{
						t.Client.Out.SendMessage("You need to wait a little longer before porting again.", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
					}
					break;
				case "next challenge":
					if (t.Level > 24)
					{
						switch (t.Realm)
						{
							case eRealm.Albion:
								t.MoveTo(165, 584218, 585297, 5106, 1058);
								break;
							case eRealm.Midgard:
								t.MoveTo(165, 575510, 537421, 4840, 608);
								break;
							case eRealm.Hibernia:
								t.MoveTo(165, 536869, 585832, 5848, 1855);
								break;
						}
					}
					else { t.Client.Out.SendMessage("Speak to my Free Level colleague to attain enough experience first!", eChatType.CT_Say, eChatLoc.CL_PopupWindow); }
					break;
				case "event zone":
					switch (t.Realm)
					{
						case eRealm.Albion:
							t.MoveTo(330, 52759, 39528, 4677, 36);
							break;
						case eRealm.Midgard:
							t.MoveTo(334, 52160, 39862, 5472, 46);
							break;
						case eRealm.Hibernia:
							t.MoveTo(335, 52836, 40401, 4672, 441);
							break;
					}
					break;
				default: break;
			}
			return true;
		}
		private void SendReply(GamePlayer target, string msg)
			{
				target.Client.Out.SendMessage(
					msg,
					eChatType.CT_Say,eChatLoc.CL_PopupWindow);
			}
		[ScriptLoadedEvent]
        public static void OnScriptCompiled(DOLEvent e, object sender, EventArgs args)
        {
            log.Info("Thidranki Event Teleporter initialized");
        }	
    }
}