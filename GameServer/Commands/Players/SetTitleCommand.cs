using System;
using System.Linq;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.PlayerTitles;
using DOL.GS.Commands;

namespace DOL.GS.Commands
{
	[Command(
		 "&settitle",
		 EPrivLevel.Player,
		 "Sets the current player title",
		 "/settitle <index> - to change current title using index in the list")]
	public class SetTitleCommand : AbstractCommandHandler, ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{
			if (IsSpammingCommand(client.Player, "settitle"))
				return;

			int index = -1;
			if (args.Length > 1)
			{
				try { index = int.Parse(args[1]); }
				catch { }

				IPlayerTitle current = client.Player.CurrentTitle;
				if (current != null && current.IsForced(client.Player))
					client.Out.SendMessage("You cannot change the current title.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
				else
				{
					var titles = client.Player.Titles.ToArray();
					if (index < 0 || index >= titles.Length)
						client.Player.CurrentTitle = PlayerTitleMgr.ClearTitle;
					else
						client.Player.CurrentTitle = (IPlayerTitle)titles[index];
				}
			}
			else
			{
				client.Out.SendPlayerTitles();
			}
		}
	}
}