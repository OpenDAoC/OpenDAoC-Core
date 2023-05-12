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

using System.Linq;

namespace DOL.GS.Commands
{
	/// <summary>
	/// Handles all user-based interaction for the '/benchmark' command
	/// </summary>
	[CmdAttribute(
		// Enter '/benchmark' to list all associated subcommands
		"&benchmark",
		// Message: '/benchmark' - Performs a system benchmark of the specified type. This is used to gauge overall system performance.
		"AdminCommands.Benchmark.CmdList.Description",
		// Message: <----- '/{0}' Command {1}----->
		"AllCommands.Header.General.Commands",
		// Required minimum privilege level to use the command
		ePrivLevel.Admin,
		// Message: Performs a system benchmark of the specified type. This is used to gauge overall system performance.
		"AdminCommands.Benchmark.Description",
		// Syntax: /benchmark listskills
		"AdminCommands.Benchmark.Syntax.Listskills",
		// Message: Tests the total amount of time (in milliseconds) the system takes to list a set number of cached skills. This does not include spellcasting specializations.
		"AdminCommands.Benchmark.Usage.Listskills",
		// Syntax: /benchmark listspells
		"AdminCommands.Benchmark.Syntax.Listspells",
		// Message: Tests the total amount of time (in milliseconds) the system takes to list a sec number of cached spells.
		"AdminCommands.Benchmark.Usage.Listspells"
	)]
	public class BenchmarkCommand : AbstractCommandHandler, ICommandHandler
	{		
		public void OnCommand(GameClient client, string[] args)
		{
			if (args.Length < 2)
			{
				// Lists '/benchmark' commands' syntax (see section above)
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
				#region List Skills
				// --------------------------------------------------------------------------------
				// LISTSKILLS
				// '/benchmark listskills'
				// The system tracks how long it takes to list 1000 usable skills
				// --------------------------------------------------------------------------------
				case "listskills":
				{
					start = GameTimer.GetTickCount();

					void ActionSkill(int inc)
					{
						var tmp = client.Player.GetAllUsableSkills(true);
					}
					
					// For each usable skill, execute the ActionSkill function until the max range is hit
					Util.ForEach(Enumerable.Range(min, max).AsParallel(), ActionSkill);
					// Final duration to list full range of spells/skills
					spent = GameTimer.GetTickCount() - start;
					
					// Message: The skills benchmark took {0}ms to list {1} usable skills.
					ChatUtil.SendTypeMessage((int)eMsg.Success, client, "AdminCommands.Benchmark.Msg.SkillsIterations", spent, max);
					return;
				}
				#endregion List Skills
				#region List Spells
				// --------------------------------------------------------------------------------
				// LISTSPELLS
				// '/benchmark listspells'
				// The system tracks how long it takes to list usable spells
				// --------------------------------------------------------------------------------
				case "listspells":
				{
					start = GameTimer.GetTickCount();

					void ActionSpell(int inc)
					{
						var tmp = client.Player.GetAllUsableListSpells(true);
					}
					
					Util.ForEach(Enumerable.Range(min, max).AsParallel(), ActionSpell);
					// Final duration to list full range of spells/skills
					spent = GameTimer.GetTickCount() - start;
					
					// Message: The spells benchmark took {0}ms to list {1} usable spells.
					ChatUtil.SendTypeMessage((int)eMsg.Success, client, "AdminCommands.Benchmark.Msg.SpellsIterations", spent, max);
					return;
				}
				#endregion List Spells
			}
	
		}

	}
}