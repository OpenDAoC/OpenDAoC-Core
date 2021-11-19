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
							int SoloRandLoc = Util.Random(1, 4);
							switch (SoloRandLoc)
							{
								case 1:
									t.MoveTo(27, 233781, 227178, 5124, 1538);
									break;
								case 2:
									t.MoveTo(27, 229129, 231088, 5169, 1808);
									break;
								case 3:
									t.MoveTo(27, 218884, 230568, 5184, 2514);
									break;
								case 4:
									t.MoveTo(27, 220771, 222700, 4780, 3069);
									break;
							}
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
								t.MoveTo(groupMember.CurrentRegionID, groupMember.X, groupMember.Y, groupMember.Z, groupMember.Heading);
								return true;
							}
						}
						log.Info("Distance < 5k");
						int GroupRandLoc = Util.Random(1, 7);
						switch (GroupRandLoc)
						{
							case 1:
								t.MoveTo(27, 205877, 217055, 5249, 3521);
								break;
							case 2:
								t.MoveTo(27, 211378, 207917, 6862, 3293);
								break;
							case 3:
								t.MoveTo(27, 217277, 202645, 6983, 30);
								break;
							case 4:
								t.MoveTo(27, 227651, 202965, 4968, 403);
								break;
							case 5:
								t.MoveTo(27, 240408, 205320, 8445, 3866);
								break;
							case 6:
								t.MoveTo(27, 249477, 206588, 7295, 327);
								break;
							case 7:
								t.MoveTo(27, 251844, 225446, 5392, 875);
								break;
						}
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