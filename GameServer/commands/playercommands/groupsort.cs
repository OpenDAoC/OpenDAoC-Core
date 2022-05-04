/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 */
using DOL.Language;
using DOL.GS.PacketHandler;

namespace DOL.GS.Commands
{
	[CmdAttribute("&groupsort",
		 ePrivLevel.Player,
		 "Sort players in the group by classes.",
		 "/groupsort manual classnames - sorts the group in the order of classes entered.",
		 "/groupsort switch # # - switches two group members.")]
	public class GroupSortCommandHandler : AbstractCommandHandler, ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{
			// If they are not in a group, then this command should not work at all
			if (client.Player.Group == null)
			{
				string display = string.Empty;
				display += "Sort players in the group by classes.\n";
				display += "/groupsort manual classnames - sorts the group in the order of classes entered.\n";
				display += "Example: /groupsort manual bard druid druid hero\n";
				display += "/groupsort switch # # - switches two group members.\n";

				DisplayMessage(client, display);
				return;
			}

			if (args.Length < 2)
			{
				DisplaySyntax(client);
				return;
			}

			string command = args[1].ToLower();

			// /groupsort for leaders -- Make sue it is the group leader using this command, if it is, execute it.
			if (command == "manual" || command == "switch")
			{
				if (client.Player != client.Player.Group.Leader)
				{
					DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Groupsort.Leader"));
					return;
				}

				switch (command)
				{
					case "manual":
						{
							DisplayMessage(client, "lineup - bard - warden - druid - druid -.....");
							break;
						}

					case "switch":
						{
							DisplayMessage(client, "switch #playerX with #playerY in Group");
							break;
						}
				}
				return;
			}

			//if nothing matched, then they tried to invent thier own commands -- show syntax
			DisplaySyntax(client);
		}
	}
}