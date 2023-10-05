using System.Collections.Generic;
using DOL.GS.PacketHandler;
using DOL.GS.PlayerTitles;

namespace DOL.GS.Commands
{
	[Command(
		 "&titlegm",
		 ePrivLevel.GM,
		 "Changes target player's titles",
		 "/titlegm <add> <class type> - add a title to the target player",
		 "/titlegm <remove> <class type> - remove a title from the target player",
		 "/titlegm <set> <class type> - sets current title of the target player",
		 "/titlegm <list> - lists all target player's titles")]
	public class TitleGmCommand : ACommandHandler, ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{
			GamePlayer target = client.Player.TargetObject as GamePlayer;
			if (target == null)
			{
				client.Out.SendMessage("You must target a player to change his titles!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
				return;
			}

			if (args.Length > 2)
			{
				IPlayerTitle title = PlayerTitleMgr.GetTitleByTypeName(args[2]);
				if (title == null)
				{
					client.Out.SendMessage("Title '" + args[2] + "' not found.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
					return;
				}

				switch (args[1].ToLower())
				{
					case "add":
						{
							target.AddTitle(title);
							return;
						}

					case "remove":
						{
							target.RemoveTitle(title);
							return;
						}

					case "set":
						{
							target.CurrentTitle = title;
							return;
						}
				}
			}
			else if (args.Length > 1)
			{
				switch (args[1].ToLower())
				{
					case "list":
						{
							var list = new List<string>();
							foreach (IPlayerTitle title in target.Titles)
							{
								list.Add("- " + title.GetDescription(target));
								list.Add(" (" + title.GetType().FullName + ")");
							}
							list.Add(" ");
							list.Add("Current:");
							list.Add("- " + target.CurrentTitle.GetDescription(target));
							list.Add(" (" + target.CurrentTitle.GetType().FullName + ")");
							client.Out.SendCustomTextWindow(target.Name + "'s titles", list);
							return;
						}
				}
			}

			DisplaySyntax(client);
		}
	}
}
