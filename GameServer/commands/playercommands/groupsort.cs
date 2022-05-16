/*
 *
 * Atlas - Group Sort
 *
 */

using System;
using System.Collections.Generic;
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
			string switchPlayerOne = string.Empty;
			string switchPlayerTwo = string.Empty;
			string switchPlayerThree = string.Empty;
			string switchPlayerFour = string.Empty;
			string switchPlayerFive = string.Empty;
			string switchPlayerSix = string.Empty;
			string switchPlayerSeven = string.Empty;
			string switchPlayerEight = string.Empty;

			// /groupsort for leaders -- Make sue it is the group leader using this command, if it is, execute it.
			if (command == "manual" || command == "switch")
			{
				if (client.Player != client.Player.Group.Leader)
				{
					DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Groupsort.Leader"));
					return;
				}

				var groupMembersCount = client.Player.Group.MemberCount;
				
				switch (command)
				{
					
					case "manual":
						{
							if (args.Length >= 4)
							{
								switchPlayerOne = args[2];
								switchPlayerTwo = args[3];
								switchPlayerThree = args[4];
								switchPlayerFour = args[5];
								switchPlayerFive = args[6];
								switchPlayerSix = args[7];
								switchPlayerSeven = args[8];
								switchPlayerEight = args[9];
							}

							List<string> playerlist = new List<string>();
							playerlist.Add(switchPlayerOne);
							playerlist.Add(switchPlayerTwo);
							playerlist.Add(switchPlayerThree);
							playerlist.Add(switchPlayerFour);
							playerlist.Add(switchPlayerFive);
							playerlist.Add(switchPlayerSix);
							playerlist.Add(switchPlayerSeven);
							playerlist.Add(switchPlayerEight);
							
							int switchOneIndex = Convert.ToInt32(switchPlayerOne);
							int switchTwoIndex = Convert.ToInt32(switchPlayerTwo);
							int switchThreeIndex = Convert.ToInt32(switchPlayerThree);
							int switchFourIndex = Convert.ToInt32(switchPlayerFour);
							int switchFiveIndex = Convert.ToInt32(switchPlayerFive);
							int switchSixIndex = Convert.ToInt32(switchPlayerSix);
							int switchSevenIndex = Convert.ToInt32(switchPlayerSeven);
							int switchEightIndex = Convert.ToInt32(switchPlayerEight);
							
							if (switchPlayerOne == string.Empty || switchPlayerTwo == string.Empty || switchPlayerThree == string.Empty 
							    || switchPlayerFour == string.Empty || switchPlayerFive == string.Empty || switchPlayerSix == string.Empty 
							    || switchPlayerSeven == string.Empty || switchPlayerEight == string.Empty)
							{
								DisplayMessage(client, "Use '/groupsort manual <classname> <classname> - sorts the group in the order of classes entered.");
								return;
							}

							//var groupList = client.Player.Group.GetMembersInTheGroup();
							
							List<String> classlist = new List<String>();
							foreach (GamePlayer player in client.Player.Group.GetMembersInTheGroup())
							{
								classlist.Add(player.CharacterClass.Name);
								Console.WriteLine(""+ classlist);
							}
							
							playerlist.Clear();
							classlist.Clear();
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
						
						if (groupMembersCount < switchSource || groupMembersCount < switchTarget)
						{
							DisplayMessage(client, "Use '/groupsort switch <#> <#>' - switches two group members.");
							return;
						}
						
						var player1 = client.Player.Group.GetMemberByIndex(switchSource);
						var player2 = client.Player.Group.GetMemberByIndex(switchTarget);

						client.Player.Group.SwitchPlayers(player1, player2);


						break;
				}
				return;
			}
			//if nothing matched, then they tried to invent their own commands -- show syntax
			DisplaySyntax(client);
		}
	}
}