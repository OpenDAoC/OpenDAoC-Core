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
    public class AtlasEventTP : GameNPC
	{
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		public static int EventRPCap = ServerProperties.Properties.EVENT_RPCAP;
		public static int EventLVCap = ServerProperties.Properties.EVENT_LVCAP;

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
			
			if (base.CurrentRegionID == 252 || base.CurrentRegionID == 165)
			{
				player.Out.SendMessage("Hello " + player.Name + "!\n\n" + "If you need so, I can port you back to your Realm's [event zone]", eChatType.CT_Say,eChatLoc.CL_PopupWindow);
				return true;
			}
			if (player.Level != EventLVCap)
			{
				player.Out.SendMessage("Hello " + player.Name + "!\n\n" + "Speak to my Event Level colleague to attain enough experience before joining the Battleground!", eChatType.CT_Say,eChatLoc.CL_PopupWindow);
				return true;
			}
			player.Out.SendMessage("Hello " + player.Name + "!\n\n" + "Are you ready to [fight]?", eChatType.CT_Say,eChatLoc.CL_PopupWindow);

			if(WorldMgr.GetAllClientsCount() >= 100)
            {
				player.Out.SendMessage("Additionally, I can port you to the [solo zone]", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
			}

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
					t.MoveTo(27, 342521, 385230, 5410, 1756);
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
				case "solo zone":
					if (t.Group == null)
					{
						log.Info("Solo player");
						int randX = Util.Random(223000, 235000);
						int randY = Util.Random(216000, 227000);
						int z = 6000;

						t.StartInvulnerabilityTimer(ServerProperties.Properties.TIMER_PVP_TELEPORT * 1000, null);
						t.MoveTo(27, randX, randY, z, t.Heading);

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
            log.Info("Atlas Basic Event Teleporter initialized");
        }	
    }
}