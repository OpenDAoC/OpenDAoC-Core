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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DOL.Database;
using DOL.Language;
using DOL.GS.PacketHandler;

namespace DOL.GS.Commands
{
	[CmdAttribute("&groupsort",
		 ePrivLevel.Player,
		 "Sort players in the group by classes.",
		 "/groupsort manual classnames - sorts the group in the order of classes entered.",
		 "Example: /groupsort manual bard druid druid hero",
		 "/groupsort switch # # - switches two group members.")]
	public class GroupSortCommandHandler : AbstractCommandHandler, ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{
			if (client.Player.Group == null)
			{
				DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Groupsort.InGroup"));
				return;
			}

			if (args.Length < 2)
			{
				DisplaySyntax(client);
				return;
			}

			string command = args[1].ToLower();

			string switchX = string.Empty;
			string switchY = string.Empty;

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
							ArrayList currentGroup = new ArrayList();
							foreach (GameLiving player in client.Player.Group.GetMembersInTheGroup())
							{
								if (player.Group != null && player.Group.MemberCount > 1)
								{
									currentGroup.Add(player);
									Console.WriteLine("Group List: ... "+currentGroup);
								}
							}
							ArrayList playerList = new ArrayList();
							string text = string.Empty;
							foreach (GamePlayer grouped in currentGroup)
							{
								text += grouped.Group.GroupMemberString(grouped);
								client.Out.SendMessage("Grouped List: "+ text, eChatType.CT_Say, eChatLoc.CL_ChatWindow);
								foreach (GamePlayer gpl in grouped.Group.GetPlayersInTheGroup())
								{
									playerList.Add(gpl);
									Console.WriteLine("Player List: ... "+playerList);
								}
							}
							
							break;
						}

					case "switch":
						{
							if (args.Length >= 4)
							{
								switchX = args[2];
								switchY = args[3];
							}
							if (switchX == string.Empty || switchY == string.Empty)
							{
								DisplayMessage(client, "/groupsort switch # # - switches two group members.");
								return;
							}
							else
							{
								// get position/slot in group and exchange the spots + update group
								
							}
								
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