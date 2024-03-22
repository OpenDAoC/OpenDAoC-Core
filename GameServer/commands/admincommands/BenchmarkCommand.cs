/* <--- SendMessage Standardization --->
*  All messages now use translation IDs to both
*  centralize their location and standardize the method
*  of message calls used throughout this project. All messages affected
*  are in English. Other languages are not yet supported.
* 
*  To  find a message at its source location, either use
*  the message body contained in the comment above the return
*  (e.g., // Message: "This is a message.") or the
*  translation ID (e.g., "AdminCommands.Account.Description").
* 
*  To perform message changes, take note of your server settings.
*  If the `serverproperty` table setting `use_dblanguage`
*  is set to `True`, you must make your changes from the
*  `languagesystem` DB table.
* 
*  If the `serverproperty` table setting
*  `update_existing_db_system_sentences_from_files` is set to `True`,
*  perform changes to messages from this file at "GameServer >
*  language > EN > Commands > AdminCommands.txt".
*
*  OPTIONAL: After changing a message, paste the new content
*  into the comment above the affected message return(s). This is
*  done for ease of reference. */

namespace DOL.GS.Commands
{
	// See the comments above 'using' about SendMessage translation IDs
	[CmdAttribute(
		// Enter '/benchmark' to list all commands
		"&benchmark",
		// Message: <----- '/benchmark' Commands (plvl 3) ----->
		"AdminCommands.Header.Syntax.Benchmark",
		ePrivLevel.Admin,
		// Message: "Performs a system benchmark of the specified type. This is used to gauge overall system performance."
		"AdminCommands.Benchmark.Description",
		// Syntax: /benchmark listskills
		"AdminCommands.Benchmark.Syntax.Listskills",
		// Message: "Tests the total amount of time (in milliseconds) the system takes to list a set number of cached skills. This does not include spellcasting specializations."
		"AdminCommands.Benchmark.Usage.Listskills",
		// Syntax: /benchmark listspells
		"AdminCommands.Benchmark.Syntax.Listspells",
		// Message: "Tests the total amount of time (in milliseconds) the system takes to list a sec number of cached spells."
		"AdminCommands.Benchmark.Usage.Listspells")]
	public class BenchmarkCommand : AbstractCommandHandler, ICommandHandler
	{		
		public void OnCommand(GameClient client, string[] args)
		{
			if (args.Length < 2)
			{
				// Lists '/benchmark' commands' syntax (see '&benchmark' section above)
				DisplaySyntax(client);
				return;
			}
			
			double start,spent;
			// Controls maximum range for both skills/spells list
			var max = 1000;
			// Controls minimum range for both skills/spells list
			var min = 0;

			switch(args[1].ToLower())
			{
				// The system tracks how long it takes to list 1000 usable skills
				// Syntax: /benchmark listskills
				// Args:   /benchmark args[1]
				// See the comments above 'using' about SendMessage translation IDs
				case "listskills":
				{
					start = GameLoop.GetCurrentTime();

					// For each usable skill, execute the ActionSkill method until the max range is hit
					for (int i = min; i < max; i++)
						client.Player.GetAllUsableSkills(true);

					// Final duration to list full range of spells/skills
					spent = GameLoop.GetCurrentTime() - start;
					
					// Message: "The skills benchmark took {0}ms to list {1} usable skills."
					ChatUtil.SendErrorMessage(client, "AdminCommands.Benchmark.Msg.SkillsIterations", spent, max);
					return;
				}

				// The system tracks how long it takes to list usable spells
				// Syntax: /benchmark listspells
				// Args:   /benchmark args[1]
				// See the comments above 'using' about SendMessage translation IDs
				case "listspells":
				{
					start = GameLoop.GetCurrentTime();

					for (int i = min; i < max; i++)
						client.Player.GetAllUsableListSpells(true);

					spent = GameLoop.GetCurrentTime() - start;
					
					// Message: "The spells benchmark took {0}ms to list {1} usable spells."
					ChatUtil.SendErrorMessage(client, "AdminCommands.Benchmark.Msg.SpellsIterations", spent, max);
					return;
				}
			}
		}
	}
}
