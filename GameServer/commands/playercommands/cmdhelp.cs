using System;
using System.Collections.Generic;
using System.Threading;
using DOL.Language;

namespace DOL.GS.Commands
{
	[CmdAttribute("&cmdhelp", //command to handle
		ePrivLevel.Player, //minimum privelege level
		"Displays available commands", //command description
		//usage
		"'/cmdhelp' displays a list of all the commands and their descriptions",
		"'/cmdhelp <plvl>' displays a list of all commands that require at least plvl",
		"'/cmdhelp <cmd>' displays the usage for cmd")]
	public class CmdHelpCommandHandler : AbstractCommandHandler, ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{
			if (IsSpammingCommand(client.Player, "cmdhelp"))
				return;

			ePrivLevel privilegeLevel = (ePrivLevel)client.Account.PrivLevel;
			bool isCommand = true;

			if (args.Length > 1)
			{
				try
				{
					privilegeLevel = (ePrivLevel)Convert.ToUInt32(args[1]);
				}
				catch (Exception)
				{
					isCommand = false;
				}
			}

			if (isCommand)
			{
                String[] commandList = GetCommandList(privilegeLevel);
				DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Cmdhelp.PlvlCommands", privilegeLevel.ToString()));

                foreach (String command in commandList)
					DisplayMessage(client, command);
			}
			else
			{
				string command = args[1];

				if (command[0] != '&')
					command = "&" + command;

				ScriptMgr.GameCommand gameCommand = ScriptMgr.GetCommand(command);

				if (gameCommand == null)
                    DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Cmdhelp.NoCommand", command));
                else
				{
					DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Cmdhelp.Usage", command));

					foreach (String usage in gameCommand.Usage)
						DisplayMessage(client, usage);
				}
			}
		}

        private static IDictionary<ePrivLevel, String[]> m_commandLists = new Dictionary<ePrivLevel, String[]>();
        private readonly static Lock _lock = new();

        private String[] GetCommandList(ePrivLevel privilegeLevel)
        {
            lock (_lock)
            {
                if (!m_commandLists.Keys.Contains(privilegeLevel))
                {
                    String[] commandList = ScriptMgr.GetCommandList(privilegeLevel, true);
                    Array.Sort(commandList);
                    m_commandLists.Add(privilegeLevel, commandList);
                }

                return m_commandLists[privilegeLevel];
            }
        }
    }
}
