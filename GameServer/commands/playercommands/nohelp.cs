/*
 *
 * Atlas - Solo challenge
 *
 */

using System;
using System.Collections.Generic;
using System.Linq;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.GS.PlayerTitles;

namespace DOL.GS.Commands
{
	[CmdAttribute(
		"&nohelp",
		ePrivLevel.Player,
		"Toggle nohelp on or off, to follow the path of solitude and stop receiving help from  your realm", "/nohelp>")]
	public class NoHelpCommandHandler : AbstractCommandHandler, ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{
			if (IsSpammingCommand(client.Player, "nohelp"))
				return;

			if (!client.Player.NoHelp)
			{
				const string customKey = "grouped_char";
				var hasGrouped = DOLDB<DOLCharactersXCustomParam>.SelectObject(DB.Column("DOLCharactersObjectId")
					.IsEqualTo(client.Player.ObjectId).And(DB.Column("KeyName").IsEqualTo(customKey)));

				DateTime d1 = new DateTime(2022, 1, 4);
				DateTime d2 = new DateTime(2022, 1, 7, 21,0,0);

				if (client.Player.Level == 1 || hasGrouped == null && client.Player.CreationDate >= d1 && client.Player.CreationDate <= d2)
				{
					client.Out.SendCustomDialog("Do you want to follow the path of Solitude?",
						new CustomDialogResponse(NoHelpInitiate));
				}
				else
				{
					client.Player.Out.SendMessage("You have already received help and cannot join this challenge.",
						eChatType.CT_Important, eChatLoc.CL_SystemWindow);
				}
			}


			if (client.Player.NoHelp)
				client.Out.SendCustomDialog(
					"Feeling lonely? Abandoning this path will void all your efforts in this challenge.",
					new CustomDialogResponse(NoHelpAbandon));
		}

		protected virtual void NoHelpInitiate(GamePlayer player, byte response)
		{
			if (response == 1)
			{
				{
					player.Emote(eEmote.Rude);
					player.NoHelp = true;
					player.Out.SendMessage(
						"You have chosen the path of solitude and will no longer receive any help from members of your Realm.",
						eChatType.CT_Important, eChatLoc.CL_SystemWindow);

					if (player.HCFlag)
						player.CurrentTitle = new HardCoreSoloTitle();
					else
						player.CurrentTitle = new NoHelpTitle();
				}
			}
			else
			{
				player.Out.SendMessage("Use the command again if you change your mind.", eChatType.CT_Important,
					eChatLoc.CL_SystemWindow);
			}
		}

		protected virtual void NoHelpAbandon(GamePlayer player, byte response)
		{
			if (response == 1)
			{
				{
					player.Emote(eEmote.Surrender);
					player.NoHelp = false;
					player.Out.SendMessage("You have chickened out. You can now run back to your ...friends.",
						eChatType.CT_Important, eChatLoc.CL_SystemWindow);
					const string customKey = "grouped_char";
					var hasGrouped = DOLDB<DOLCharactersXCustomParam>.SelectObject(DB.Column("DOLCharactersObjectId")
						.IsEqualTo(player.ObjectId).And(DB.Column("KeyName").IsEqualTo(customKey)));
					if (hasGrouped == null)
					{
						DOLCharactersXCustomParam groupedChar = new DOLCharactersXCustomParam();
						groupedChar.DOLCharactersObjectId = player.ObjectId;
						groupedChar.KeyName = customKey;
						groupedChar.Value = "1";
						GameServer.Database.AddObject(groupedChar);
					}

					player.CurrentTitle = PlayerTitleMgr.ClearTitle;
				}
			}
			else
			{
				player.Out.SendMessage("Use the command again if get scared once more.", eChatType.CT_Important,
					eChatLoc.CL_SystemWindow);
			}
		}

		[CmdAttribute(
			"&sololadder",
			ePrivLevel.Player,
			"Displays the Solo Ladder.",
			"/sololadder")]
		public class SoloLadderCommandHandler : AbstractCommandHandler, ICommandHandler
		{
			public class SoloCharacter : IComparable<SoloCharacter>
			{
				public string CharacterName { get; set; }

				public int CharacterLevel { get; set; }

				public string CharacterClass { get; set; }

				public bool isHC { get; set; }

				public override string ToString()
				{
					if (isHC)
						return string.Format("{0} the level {1} {2} <hc>", CharacterName, CharacterLevel,
							CharacterClass);

					return string.Format("{0} the level {1} {2}", CharacterName, CharacterLevel, CharacterClass);

				}

				public int CompareTo(SoloCharacter compareLevel)
				{
					// A null value means that this object is greater.
					if (compareLevel == null)
						return 1;

					return CharacterLevel.CompareTo(compareLevel.CharacterLevel);
				}
			}

			public void OnCommand(GameClient client, string[] args)
			{
				if (IsSpammingCommand(client.Player, "sololadder"))
					return;

				IList<string> textList = GetSoloLadder();
				client.Out.SendCustomTextWindow("Solo Ladder", textList);
				return;

				IList<string> GetSoloLadder()

				{
					IList<string> output = new List<string>();
					IList<SoloCharacter> soloCharacters = new List<SoloCharacter>();
					IList<DOLCharacters> characters = GameServer.Database.SelectObjects<DOLCharacters>("NoHelp = '1'")
						.OrderByDescending(x => x.Level).Take(50).ToList();

					output.Add("Top 50 Solo characters:\n");

					foreach (DOLCharacters c in characters)
					{
						if (c == null)
							continue;

						string className = ((eCharacterClass) c.Class).ToString();
						

						soloCharacters.Add(new SoloCharacter() {CharacterName = c.Name, CharacterLevel = c.Level, CharacterClass = className, isHC = c.HCFlag});

					}

					int position = 0;

					foreach (SoloCharacter soloCharacter in soloCharacters)
					{
						position++;
						output.Add(position + ". " + soloCharacter);
					}

					return output;
				}

			}
		}
	}
}

namespace DOL.GS.PlayerTitles
{
	public class NoHelpTitle : SimplePlayerTitle
	{

		public override string GetDescription(GamePlayer player)
		{
			return "Solo Beetle";
		}

		public override string GetValue(GamePlayer source, GamePlayer player)
		{
			return "Solo Beetle";
		}

		public override void OnTitleGained(GamePlayer player)
		{
			player.Out.SendMessage("You have gained the Solo Beetle title!", eChatType.CT_Important,
				eChatLoc.CL_SystemWindow);
		}

		public override bool IsSuitable(GamePlayer player)
		{
			const string customKey2 = "solo_to_50";
			var solo_to_50 = DOLDB<DOLCharactersXCustomParam>.SelectObject(DB.Column("DOLCharactersObjectId")
				.IsEqualTo(player.ObjectId).And(DB.Column("KeyName").IsEqualTo(customKey2)));

			return player.NoHelp || solo_to_50 != null;
		}
	}
}