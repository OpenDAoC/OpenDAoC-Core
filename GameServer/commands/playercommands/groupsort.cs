/*
 *
 * Atlas - Group Sort
 *
 */

using System;
using DOL.Language;


namespace DOL.GS.Commands
{
	[Cmd("&groupsort",
		 ePrivLevel.Player,
		 "Sort players in the group",
		 "'/groupsort switch <#> <#>' - switches two group members.")]
	// "Example: /groupsort manual bard druid druid hero",
	// "/groupsort manual classnames - sorts the group in the order of classes entered.",

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
							
							Console.WriteLine(client.Player.GroupIndex);
							if (client.Player.Group != null)
							{
								foreach (GamePlayer player in client.Player.Group.GetMembersInTheGroup())
								{
									Console.WriteLine($"Player {player.Name} has index {player.GroupIndex}");
								}
							}

							break;
						}
					
					case "switch":
						if (args.Length >= 4)
						{
							switchX = args[2];
							switchY = args[3];
						}
						if (switchX == string.Empty || switchY == string.Empty)
						{
							DisplayMessage(client, "Use '/groupsort switch <#> <#>' - switches two group members.");
							return;
						}
						
						int switchXIndex = Convert.ToInt32(switchX);
						int switchYIndex = Convert.ToInt32(switchY);
						var switchSource = Convert.ToByte(switchXIndex - 1);
						var switchTarget = Convert.ToByte(switchYIndex - 1);

						var groupMembersCount = client.Player.Group.MemberCount;

						if (groupMembersCount < switchSource || groupMembersCount < switchTarget)
						{
							DisplayMessage(client, "Use '/groupsort switch <#> <#>' - switches two group members.");
							return;
						}
						
						var player1 = client.Player.Group.GetMemberByIndex(switchSource);
						var player2 = client.Player.Group.GetMemberByIndex(switchTarget);
						
						Console.WriteLine($"switchX: {switchX} switchY: {switchY} player1: {player1.Name} player2: {player2.Name}");
						

						client.Player.Group.SwitchPlayers(player1, player2);


						break;
				}
				return;
			}
			//if nothing matched, then they tried to invent thier own commands -- show syntax
			DisplaySyntax(client);
		}
	}
}