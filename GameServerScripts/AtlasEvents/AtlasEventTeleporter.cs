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
using DOL.Database;

namespace DOL.GS.Scripts
{
    public class AtlasEventTeleporter : GameNPC
	{
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		
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
			
			player.Out.SendMessage("Hello " + player.Name + "!\n\n" + "Are you ready to [fight]?", eChatType.CT_Say,eChatLoc.CL_PopupWindow);
			player.Out.SendMessage("If you need so, I can port you back to your Realm's [event zone]", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
			return true;
		}
		public override bool WhisperReceive(GameLiving source, string str)
		{
			if(!base.WhisperReceive(source,str)) return false;
		  	if(!(source is GamePlayer)) return false;
		    GamePlayer t = (GamePlayer) source;
			TurnTo(t.X,t.Y);
			switch(str)
			{
				case "fight":
					if (t.Level != ServerProperties.Properties.EVENT_LVCAP)
					{
						t.Out.SendMessage("You must be level " + ServerProperties.Properties.EVENT_LVCAP + " to enter the event, speak with my colleague!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
						return false;
					}
					
					//case recently in combat
					if (t.InCombatInLast(TeleportDelay))
					{
	                    t.Out.SendMessage("You need to wait a little longer before porting again.", eChatType.CT_Say,eChatLoc.CL_PopupWindow);
                        return false;
					}
					
					//case solo
					if (t.Group == null)
					{
						log.Info("Solo player");
						int randX = Util.Random(223000, 235000);
						int randY = Util.Random(216000, 227000);
						int z = 6000;
						
						t.StartInvulnerabilityTimer(ServerProperties.Properties.TIMER_PVP_TELEPORT*1000,null);
						t.MoveTo(27, randX, randY, z, t.Heading);
						
					}
					//case grouped
					else if (t.Group != null && t.Group.MemberCount > 1)
					{
						log.Info("Player in group");
						foreach (GamePlayer groupMember in t.Group.GetPlayersInTheGroup())
						{
							if (GetDistanceTo(groupMember) > 5000 && groupMember.InCombat == false)
							{
								log.Info("Distance > 5k");
								t.StartInvulnerabilityTimer(ServerProperties.Properties.TIMER_PVP_TELEPORT*1000,null);
								t.MoveTo(groupMember.CurrentRegionID, groupMember.X, groupMember.Y, groupMember.Z, groupMember.Heading);
								return true;
							}
						}
						log.Info("Distance < 5k");
						int randX = Util.Random(205000, 253000);
						int randY = Util.Random(204000, 216000);
						int z = 9000;
						
						t.StartInvulnerabilityTimer(ServerProperties.Properties.TIMER_PVP_TELEPORT*1000,null);
						t.MoveTo(27, randX, randY, z, t.Heading);

					}
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
            log.Info("Atlas Event Teleporter initialized");
        }	
    }
}