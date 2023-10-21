using System;
using Core.GS.Enums;
using Core.GS.Languages;
using Core.GS.PacketHandler;
using Core.GS.ServerProperties;

namespace Core.GS.Commands;

[Command("&freelevel", //command to handle
              EPrivLevel.Player, //minimum privelege level
              "Display state of FreeLevel", //command description
              "/freelevel")] //command usage
public class FreeLevelCommand : ACommandHandler, ICommandHandler
{
	public void OnCommand(GameClient client, string[] args)
	{
		//flag 1 = above level, 2 = elligable, 3= time until, 4 = level and time until, 5 = level until
		byte state = client.Player.FreeLevelState;
		string message = "";

		if (args.Length == 2 && args[1] == "decline")
		{
			if (state == 2)
			{
				// NOT SURE FOR THIS MESSAGE
				message = LanguageMgr.GetTranslation(client.Account.Language, "PLCommands.FreeLevel.Removed");
				// we decline THIS ONE, but next level, we will gain another freelevel !!
				client.Player.LastFreeLevel = client.Player.Level - 1;
				client.Player.Out.SendPlayerFreeLevelUpdate();
			}
			else
			{
				// NOT SURE FOR THIS MESSAGE
				message = LanguageMgr.GetTranslation(client.Account.Language, "PLCommands.FreeLevel.NoFreeLevel");
			}
			client.Out.SendMessage(message, EChatType.CT_System, EChatLoc.CL_SystemWindow);
			return;
		}

		TimeSpan t = new TimeSpan();

		switch (client.Player.Realm)
		{
			case ERealm.Albion:
				t = client.Player.LastFreeLeveled.AddDays(Properties.FREELEVEL_DAYS_ALBION) - DateTime.Now;
				break;
			case ERealm.Midgard:
				t = client.Player.LastFreeLeveled.AddDays(Properties.FREELEVEL_DAYS_MIDGARD) - DateTime.Now;
				break;
			case ERealm.Hibernia:
				t = client.Player.LastFreeLeveled.AddDays(Properties.FREELEVEL_DAYS_HIBERNIA) - DateTime.Now;
				Console.WriteLine("derp");
				break;
		}

		switch (state)
		{
			case 1:
				message = LanguageMgr.GetTranslation(client.Account.Language, "PLCommands.FreeLevel.AboveMaximumLevel");
				break;
			case 2:
				message = LanguageMgr.GetTranslation(client.Account.Language, "PLCommands.FreeLevel.EligibleFreeLevel");
				break;
			case 3:
				// NOT SURE FOR THIS MESSAGE
				message = LanguageMgr.GetTranslation(client.Account.Language, "PLCommands.FreeLevel.FreeLevelIn", t.Days, t.Hours, t.Minutes);
				break;
			case 4:
				// NOT SURE FOR THIS MESSAGE
				message = LanguageMgr.GetTranslation(client.Account.Language, "PLCommands.FreeLevel.FreeLevelIn2", t.Days, t.Hours, t.Minutes);
				break;
			case 5:
				message = LanguageMgr.GetTranslation(client.Account.Language, "PLCommands.FreeLevel.FreeLevelSoon");
				break;

		}
		client.Out.SendMessage(message, EChatType.CT_System, EChatLoc.CL_SystemWindow);
	}
}