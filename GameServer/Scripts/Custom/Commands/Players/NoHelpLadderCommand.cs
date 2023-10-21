using System;
using System.Collections.Generic;
using System.Linq;
using Core.Database;
using Core.Database.Tables;
using Core.GS.Enums;

namespace Core.GS.Commands;

[Command(
		"&sololadder",
		EPrivLevel.Player,
		"Displays the Solo Ladder.",
		"/sololadder")]
public class NoHelpLadderCommand : ACommandHandler, ICommandHandler
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
			IList<DbCoreCharacter> characters = GameServer.Database.SelectObjects<DbCoreCharacter>(DB.Column("NoHelp").IsEqualTo(1))
				.OrderByDescending(x => x.Level).Take(50).ToList();

			output.Add("Top 50 Solo characters:\n");

			foreach (DbCoreCharacter c in characters)
			{
				if (c == null)
					continue;

				string className = ((EPlayerClass) c.Class).ToString();
				

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