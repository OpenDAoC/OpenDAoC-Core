namespace DOL.GS.Commands;

[Command(
	"&gmrelicpad",
	EPrivLevel.GM,
	"GMCommands.GMRelicPad.Description",
	"GMCommands.GMRelicPad.Usage")]
public class GmRelicPadCommand : ACommandHandler, ICommandHandler
{
	public void OnCommand(GameClient client, string[] args)
	{
		if (args.Length != 4 || (args[1] != "magic" && args[1] != "strength"))
		{
			DisplaySyntax(client);
			return;
		}

		ushort emblem = ushort.Parse(args[3]);
		emblem += (ushort)((args[1] == "magic") ? 10 : 0);

		GameRelicPad pad = new GameRelicPad();
		pad.Name = args[2];
		pad.Realm = (ERealm)byte.Parse(args[3]);
		pad.Emblem = emblem;
		pad.CurrentRegionID = client.Player.CurrentRegionID;
		pad.X = client.Player.X;
		pad.Y = client.Player.Y;
		pad.Z = client.Player.Z;
		pad.Heading = client.Player.Heading;
		pad.AddToWorld();
		pad.SaveIntoDatabase();
	}
}