using System;
using DOL.GS.PacketHandler;

namespace DOL.GS.Commands
{
    [CmdAttribute(
        "&time",
        ePrivLevel.Player,
        "time in game",
        "/time")]
    public class TimeCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if (IsSpammingCommand(client.Player, "time", 1000))
                return;

            if ((ePrivLevel) client.Account.PrivLevel == ePrivLevel.Admin) // admins only
            {
                try
                {
                    if (args.Length == 3)
                    {
                        uint speed = 0;
                        uint time = 0;
                        speed = Convert.ToUInt32(args[1]);
                        time = Convert.ToUInt32(args[2]);
                        WorldMgr.ChangeGameTime(speed, time / 1000.0);
                        return;
                    }
                    else
                        throw new Exception();
                }
                catch
                {
                    client.Out.SendMessage("ADMIN Usage: /time <speed> (24 is normal, higher numbers make faster days) <time> (1 - 1000) - Reset days with new length, starting at the given time.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                }
            }

            if (client.Player != null)
            {
                uint cTime = WorldMgr.GetCurrentGameTime(client.Player);
                uint hour = cTime / 1000 / 60 / 60;
                uint minute = cTime / 1000 / 60 % 60;
                uint seconds = cTime / 1000 % 60;
                bool pm = false;

                if (hour == 12)
                    pm = true;
                else if (hour > 12)
                {
                    hour -= 12;
                    pm = true;
                }
                else if (hour == 0)
                    hour = 12;

                client.Out.SendMessage($"It is {hour}:{minute:00}:{seconds:00} {(pm ? "pm" : "am")}", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                client.Out.SendMessage($"Night time: {client.Player.CurrentRegion.IsNightTime}", eChatType.CT_System, eChatLoc.CL_SystemWindow);
            }
        }
    }
}
