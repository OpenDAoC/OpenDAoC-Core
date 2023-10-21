using System.Linq;

namespace Core.GS.Commands
{
	// See the comments above 'using' about SendMessage translation IDs
	[Command(
		// Enter '/benchmark' to list all commands
		"&benchmark",
		// Message: <----- '/benchmark' Commands (plvl 3) ----->
		"AdminCommands.Header.Syntax.Benchmark",
		EPrivLevel.Admin,
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
	public class BenchmarkCommand : ACommandHandler, ICommandHandler
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
			
				#region Listskills
				// The system tracks how long it takes to list 1000 usable skills
				// Syntax: /benchmark listskills
				// Args:   /benchmark args[1]
				// See the comments above 'using' about SendMessage translation IDs
				case "listskills":
				{
					start = GameLoop.GetCurrentTime();

					void ActionSkill(int inc)
					{
						var tmp = client.Player.GetAllUsableSkills(true);
					}
					
					// For each usable skill, execute the ActionSkill method until the max range is hit
					Util.ForEach(Enumerable.Range(min, max).AsParallel(), ActionSkill);
					
					// Final duration to list full range of spells/skills
					spent = GameLoop.GetCurrentTime() - start;
					
					// Message: "The skills benchmark took {0}ms to list {1} usable skills."
					ChatUtil.SendErrorMessage(client, "AdminCommands.Benchmark.Msg.SkillsIterations", spent, max);
					return;
				}
				#endregion Listskills

				#region Listspells
				// The system tracks how long it takes to list usable spells
				// Syntax: /benchmark listspells
				// Args:   /benchmark args[1]
				// See the comments above 'using' about SendMessage translation IDs
				case "listspells":
				{
					start = GameLoop.GetCurrentTime();

					void ActionSpell(int inc)
					{
						var tmp = client.Player.GetAllUsableListSpells(true);
					}
					
					Util.ForEach(Enumerable.Range(min, max).AsParallel(), ActionSpell);
					
					spent = GameLoop.GetCurrentTime() - start;
					
					// Message: "The spells benchmark took {0}ms to list {1} usable spells."
					ChatUtil.SendErrorMessage(client, "AdminCommands.Benchmark.Msg.SpellsIterations", spent, max);
					return;
				}
				#endregion Listspells
			}
		}
	}
}
