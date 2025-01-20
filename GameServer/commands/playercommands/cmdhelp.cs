using System;
using System.Collections.Generic;
using System.Threading;
using DOL.Language;

namespace DOL.GS.Commands
{
    [CmdAttribute("&cmdhelp",
        ePrivLevel.Player,
        "Displays available commands",
        "'/cmdhelp' displays a list of all the commands and their descriptions",
        "'/cmdhelp <cmd>' displays the usage for cmd")]
    public class CmdHelpCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        private static IDictionary<ePrivLevel, string[]> m_commandLists = new Dictionary<ePrivLevel, string[]>();
        private readonly static Lock _lock = new();

        public void OnCommand(GameClient client, string[] args)
        {
            if (IsSpammingCommand(client.Player, "cmdhelp"))
                return;

            ePrivLevel privilegeLevel = (ePrivLevel) client.Account.PrivLevel;

            if (args.Length == 0)
            {
                string[] commandList = GetCommandList(privilegeLevel);
                DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Cmdhelp.PlvlCommands", privilegeLevel.ToString()));

                foreach (string command in commandList)
                    DisplayMessage(client, command);

                return;
            }

            string commandArg = args[1];

            if (commandArg[0] != '/')
                commandArg = $"/{commandArg}";

            ScriptMgr.GameCommand gameCommand = ScriptMgr.GetCommand($"&{commandArg[1..]}");

            if (gameCommand == null || (ePrivLevel) gameCommand.m_lvl > privilegeLevel)
            {
                DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Cmdhelp.NoCommand", commandArg));
                return;
            }

            DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Cmdhelp.Usage", commandArg));

            foreach (string usage in gameCommand.Usage)
                DisplayMessage(client, usage);
        }

        private static string[] GetCommandList(ePrivLevel privilegeLevel)
        {
            lock (_lock)
            {
                if (m_commandLists.TryGetValue(privilegeLevel, out string[] value))
                    return value;

                value = ScriptMgr.GetCommandList(privilegeLevel, true);
                Array.Sort(value);
                m_commandLists.Add(privilegeLevel, value);
                return value;
            }
        }
    }
}
