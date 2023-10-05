using System;
using DOL.Language;

namespace DOL.GS.Commands;

[Command(
	"]jump",
	ePrivLevel.GM,
	"GMCommands.DebugJump.Description",
	"GMCommands.DebugJump.Usage")]
public class DebugJumpCommand : ACommandHandler, ICommandHandler
{
	public void OnCommand(GameClient client, string[] args)
	{
		if (args.Length != 6)
		{
			DisplaySyntax(client);
			return;
		}

		ushort zoneID = 0;
		if (!ushort.TryParse(args[1], out zoneID))
		{
			DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.DebugJump.InvalidZoneID", args[1]));
			return;
		}

		Zone z = WorldMgr.GetZone(zoneID);
		if (z == null)
		{
			DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.DebugJump.UnknownZoneID", args[1]));
			return;
		}
		
		ushort RegionID = z.ZoneRegion.ID;
		int X = z.XOffset + Convert.ToInt32(args[2]);
		int Y = z.YOffset + Convert.ToInt32(args[3]);
		int Z = Convert.ToInt32(args[4]);
		ushort Heading = Convert.ToUInt16(args[5]);

		if (!CheckExpansion(client, RegionID))
			return;

		client.Player.MoveTo(RegionID, X, Y, Z, Heading);
	}

	public bool CheckExpansion(GameClient client, ushort RegionID)
	{
		Region reg = WorldMgr.GetRegion(RegionID);
		if (reg == null)
		{
			DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.DebugJump.UnknownRegion", RegionID.ToString()));
			return false;
		}
		else if (reg.Expansion > (int)client.ClientType)
		{
			DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.DebugJump.RegionNotSuppByClient", reg.Description));
			return false;
		}
		return true;
	}
}