namespace Core.GS.Commands;

[Command(
"&siegeweapon",
EPrivLevel.GM,
"creates siege weapons",
"/siegeweapon create miniram/lightram/mediumram/heavyram/catapult/ballista/cauldron/trebuchet")]
public class SiegeWeaponCommand : ACommandHandler, ICommandHandler
{
	public void OnCommand(GameClient client, string[] args)
	{
		if (args.Length < 3)
		{
			DisplaySyntax(client);
			return;
		}

		switch (args[1].ToLower())
		{
			case "create":
				{
					switch (args[2].ToLower())
					{
						case "miniram":
							{
								GameSiegeRam ram = new GameSiegeRam();
								ram.X = client.Player.X;
								ram.Y = client.Player.Y;
								ram.Z = client.Player.Z;
								ram.CurrentRegion = client.Player.CurrentRegion;
								ram.Model = 2605;
								ram.Level = 0;
								ram.Name = "mini ram";
								ram.Realm = client.Player.Realm;
								ram.AddToWorld();
								break;
							}
						case "lightram":
							{
								GameSiegeRam ram = new GameSiegeRam();
								ram.X = client.Player.X;
								ram.Y = client.Player.Y;
								ram.Z = client.Player.Z;
								ram.CurrentRegion = client.Player.CurrentRegion;
								ram.Model = 2600;
								ram.Level = 1;
								ram.Name = "light ram";
								ram.Realm = client.Player.Realm;
								ram.AddToWorld();
								break;
							}
						case "mediumram":
							{
								GameSiegeRam ram = new GameSiegeRam();
								ram.X = client.Player.X;
								ram.Y = client.Player.Y;
								ram.Z = client.Player.Z;
								ram.CurrentRegion = client.Player.CurrentRegion;
								ram.Model = 2601;
								ram.Level = 2;
								ram.Name = "medium ram";
								ram.Realm = client.Player.Realm;
								ram.AddToWorld();
								break;
							}
						case "heavyram":
							{
								GameSiegeRam ram = new GameSiegeRam();
								ram.X = client.Player.X;
								ram.Y = client.Player.Y;
								ram.Z = client.Player.Z;
								ram.CurrentRegion = client.Player.CurrentRegion;
								ram.Model = 2602;
								ram.Level = 3;
								ram.Name = "heavy ram";
								ram.Realm = client.Player.Realm;
								ram.AddToWorld();
								break;
							}
							case "catapult":
							{
								GameSiegeCatapult cat = new GameSiegeCatapult();
								cat.X = client.Player.X;
								cat.Y = client.Player.Y;
								cat.Z = client.Player.Z;
								cat.CurrentRegion = client.Player.CurrentRegion;
								cat.Model = 0xA26;
								cat.Level = 3;
								cat.Name = "catapult";
								cat.Realm = client.Player.Realm;
								cat.AddToWorld();
								break;
							}
							case "ballista":
							{
								GameSiegeBallista bal = new GameSiegeBallista();
								bal.X = client.Player.X;
								bal.Y = client.Player.Y;
								bal.Z = client.Player.Z;
								bal.CurrentRegion = client.Player.CurrentRegion;
								bal.Model = 0x0A55;
								bal.Level = 3;
								bal.Name = "field ballista";
								bal.Realm = client.Player.Realm;
								bal.AddToWorld();
								break;
							}
							case "cauldron":
							{
								GameSiegeRam ram = new GameSiegeRam();
								ram.X = client.Player.X;
								ram.Y = client.Player.Y;
								ram.Z = client.Player.Z;
								ram.CurrentRegion = client.Player.CurrentRegion;
								ram.Model =  0xA2F;
								ram.Level = 3;
								ram.Name = "cauldron of boiling oil";
								ram.Realm = client.Player.Realm;
								ram.AddToWorld();
								break;
							}
							case "trebuchet":
							{
								GameSiegeTrebuchet tre = new GameSiegeTrebuchet();
								tre.X = client.Player.X;
								tre.Y = client.Player.Y;
								tre.Z = client.Player.Z;
								tre.CurrentRegion = client.Player.CurrentRegion;
								tre.Model = 0xA2E;
								tre.Level = 3;
								tre.Name = "trebuchet";
								tre.Realm = client.Player.Realm;
								tre.AddToWorld();
								break;
							}
					}
					break;
				}
		}
	}
}