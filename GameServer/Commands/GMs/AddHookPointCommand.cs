using System;
using Core.Database;
using Core.GS.Keeps;
using Core.Language;

namespace Core.GS.Commands;

[Command(
	 "&addhookpoint",
	 EPrivLevel.GM,
	 "GMCommands.HookPoint.Description",
	 "GMCommands.HookPoint.Usage")]
public class AddHookPointCommand : ACommandHandler, ICommandHandler
{
	public void OnCommand(GameClient client, string[] args)
	{
		if (args.Length < 3)
		{
			DisplaySyntax(client);
			return;
		}
		int id = 0;
		int skin = 0;
		try
		{
			GameKeepComponent comp = client.Player.TargetObject as GameKeepComponent;
			if (comp == null)
			{
				DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.HookPoint.NoGKCTarget"));
				return;
			}
			skin = Convert.ToInt32(args[1]);
			id = Convert.ToInt32(args[2]);
			DbKeepHookPoint dbkeephp = new DbKeepHookPoint();
			dbkeephp.HookPointID = id;
			dbkeephp.KeepComponentSkinID = skin;
			dbkeephp.X = client.Player.X - comp.X;
			dbkeephp.Y = client.Player.Y - comp.Y;
			dbkeephp.Z = client.Player.Z - comp.Z;
			dbkeephp.Heading = client.Player.Heading - comp.Heading;
			GameServer.Database.AddObject(dbkeephp);
		}
		catch (Exception e)
		{
			DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Error", e.Message));
		}
	}
}