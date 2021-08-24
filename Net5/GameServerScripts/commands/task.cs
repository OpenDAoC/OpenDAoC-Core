using DOL.GS;
using DOL.GS.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS.Scripts
{
    [CmdAttribute(
       "&task",
       ePrivLevel.Player,
         "Displays the current realm bonuses status.", "/task")]
    public class TaskCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if (!IsSpammingCommand(client.Player, "task"))
            {
                client.Out.SendCustomTextWindow("Task Bonuses", ZoneBonusRotator.GetTextList());
            }
        }
    }
}
