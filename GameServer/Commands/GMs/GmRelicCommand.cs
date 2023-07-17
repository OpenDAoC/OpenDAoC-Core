using System;
using DOL.GS.PacketHandler;
using DOL.GS;
using DOL.Database;

namespace DOL.GS.Commands
{
	[Command(
		"&gmrelic",
		EPrivLevel.GM,
		"GMCommands.GMRelic.Description",
		"GMCommands.GMRelic.Usage")]
	public class GmRelicCommand : AbstractCommandHandler, ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{
			if (args.Length != 3 || (args[1] != "magic" && args[1] != "strength"))
			{
				DisplaySyntax(client);
				return;
			}

			DbRelics relic = new DbRelics();

			relic.Heading = client.Player.Heading;
			relic.OriginalRealm = int.Parse(args[2]);
			relic.Realm = 0;
			relic.Region = client.Player.CurrentRegionID;
			relic.relicType = (args[1] == "strength") ? 0 : 1;
			relic.X = client.Player.X;
			relic.Y = client.Player.Y;
			relic.Z = client.Player.Z;
			relic.RelicID = UtilCollection.Random(100);
			GameServer.Database.AddObject(relic);
			RelicMgr.Init();
		}
	}
}
