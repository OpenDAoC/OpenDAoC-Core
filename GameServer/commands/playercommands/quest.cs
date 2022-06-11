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
using DOL.GS.PacketHandler;
using DOL.GS.Quests;
using System;

namespace DOL.GS.Commands
{
	[CmdAttribute(
		"&quest",
		new string[] {"&quests"},
		ePrivLevel.Player,
		"Display the players completed quests", "/quest")]
	public class QuestCommandHandler : AbstractCommandHandler, ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{
			if (IsSpammingCommand(client.Player, "quest"))
				return;

			string message = "";
			if (client.Player.QuestList == null || client.Player.QuestList.Count == 0)
				message = "You have no pending quests currently.";
			else
			{
				message = "You are currently working on" + client.Player.QuestList.Count + " quests.";
				/*
				message += "You are currently working on the following quests:\n";
				foreach (AbstractQuest quest in client.Player.QuestList)
				{
					message += String.Format("On step {0} of quest '{1}'\n", quest.Step, quest.Name);
					message += String.Format("What to do: {0}", quest.Description);
				}*/
			}
			if (client.Player.QuestListFinished == null || client.Player.QuestListFinished.Count == 0)
				message += "\nYou have not yet completed any quests.";
			else
			{
				if (client.Player.QuestListFinished.Count < 10)
				{
					message += "\nYou have completed the following quests:";

					foreach (AbstractQuest quest in client.Player.QuestListFinished)
					{
						// Need to protect from too long a list
						// We'll do an easy sloppy chop at 1500 characters (packet limit is 2048)
						if (message.Length < 1500)
							message += "\n" + quest.Name;
						else
							message += "";
					}
				}
				else
					message += "\nYou have completed " + client.Player.QuestListFinished.Count + " quests.";
			}

			message += "\nUse the '/journal' command to view your ongoing and completed quests.";
			DisplayMessage(client, message);
		}
	}
}