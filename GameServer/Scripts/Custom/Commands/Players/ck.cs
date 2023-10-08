using System.Collections.Generic;
using DOL.GS.Keeps;
using DOL.GS.PacketHandler;

namespace DOL.GS.Commands
{
	[Command(
	   "&ck",
	   EPrivLevel.Player,
		 "Displays who owns the CK while in a battleground.", "/ck")]
	public class CkCommandHandler : ACommandHandler, ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{
			if (IsSpammingCommand(client.Player, "ck"))
				return;

			string bgName = client.Player.CurrentZone.Description;
			
			if (GameServer.KeepManager.GetBattleground(client.Player.CurrentRegionID) != null)
			{
				ICollection<AGameKeep> keepList =
					GameServer.KeepManager.GetKeepsOfRegion(client.Player.CurrentRegionID);
				foreach (AGameKeep keep in keepList)
				{
					ChatUtil.SendSystemMessage(client, KeepStringBuilder(keep));
				}
			}
			else
			{
				client.Out.SendMessage("You need to be in a battleground to use this command.", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
			}
		}
		private string KeepStringBuilder(AGameKeep keep)
		{
			string buffer = "";
			buffer += keep.Name + ": " + GlobalConstants.RealmToName(keep.Realm);
			if (keep.Guild != null)
			{
				buffer += " (" + keep.Guild.Name + ")";
			}
			buffer += "\n";
			return buffer;
		}
		
	}
}
