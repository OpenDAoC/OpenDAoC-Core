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
        private static SortedDictionary<ePrivLevel, List<string>> m_commandLists;
        private readonly static Lock _lock = new();

        public void OnCommand(GameClient client, string[] args)
        {
            if (IsSpammingCommand(client.Player, "cmdhelp"))
                return;

            ePrivLevel privilegeLevel = (ePrivLevel) client.Account.PrivLevel;

            if (args.Length == 1)
            {
                ShowUseableCommands(client);
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

        private void ShowUseableCommands(GameClient client)
        {
            if (m_commandLists == null)
            {
                lock (_lock)
                {
                    m_commandLists = ScriptMgr.GetCommandList(true);
                }
            }

            foreach (var pair in m_commandLists)
            {
                if (pair.Key > (ePrivLevel) client.Account.PrivLevel)
                    continue;

                DisplayMessage(client, "\n");
                DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Cmdhelp.PlvlCommands", pair.Key));

                foreach (string usage in pair.Value)
                    DisplayMessage(client, usage);
            }
        }
    }
}
