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
		    if (EventRPCap == 0) return false;
			GamePlayer t = (GamePlayer) source;
			TurnTo(t.X,t.Y);
			switch(str)
			{
				case "fight":
					if (t.Group != null && t.Group.MemberCount > 1)
					{
						foreach (var groupMember in t.Group.GetPlayersInTheGroup())
						{
							if (groupMember.GetDistanceTo(t) > 5000)
							{
								t.MoveTo(27, groupMember.X, groupMember.Y, groupMember.Z, groupMember.Heading);
								break;
							}
						}
					}
					
					int RandLoc = Util.Random(1, 3);

					switch (RandLoc)
					{
						case 1:
                            t.MoveTo(27, 253621, 21794, 6620, 3268);
                            break;
						case 2:
                            t.MoveTo(27, 222073, 206206, 7900, 4010);
                            break;
						case 3:
                            t.MoveTo(27, 221020, 232192, 5247, 2471);
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
            log.Info("Atlas Event Teleporter 2 initialized");
        }	
    }
}