using System;
using DOL.Database;
using DOL.Language;

namespace DOL.GS.Commands;

[Command(
	"&zonepoint",
	new[] { "&zp" },
	EPrivLevel.GM,
	"GMCommands.Zonepoint.Description",
	"GMCommands.Zonepoint.Usage", "GMCommands.Zonepoint.UsageClass")]
public class ZonePointCommand : ACommandHandler, ICommandHandler
{
	public void OnCommand(GameClient client, string[] args)
	{
		if (args.Length < 4)
		{
			DisplaySyntax(client);
			return;
		}

		if (args[1] == "add")
		{
			var zp = new DbZonePoint();
			zp.Id = Convert.ToUInt16(args[2]);
			zp.Realm = Convert.ToUInt16(Convert.ToUInt16(args[3]));
			zp.TargetX = client.Player.X;
			zp.TargetY = client.Player.Y;
			zp.TargetZ = client.Player.Z;
			zp.TargetRegion = client.Player.CurrentRegionID;
			zp.TargetHeading = client.Player.Heading;
			zp.SourceRegion = 0;
			zp.SourceX = 0;
			zp.SourceY = 0;
			zp.SourceZ = 0;
			zp.AllowAdd = true;

			GameServer.Database.AddObject(zp);
			DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language,
				"GMCommands.Zonepoint.ZonepointAdded",
				zp.Id, zp.Realm, zp.TargetX, zp.TargetY, zp.TargetZ, zp.TargetRegion));
		}

	}
}