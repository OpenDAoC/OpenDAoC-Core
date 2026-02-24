using DOL.GS.PacketHandler;

namespace DOL.GS.Commands
{
    [CmdAttribute(
        "&gtrange",
        ePrivLevel.Player,
        "Gives a range to a ground target",
        "/gtrange")]
    public class GroundTargetRangeCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if (IsSpammingCommand(client.Player, "gtrange"))
                return;

            if (!client.Player.GroundTarget.IsValid)
            {
                client.Out.SendMessage("Range to target: You don't have a ground target set.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            int range = client.Player.GetDistanceTo(client.Player.GroundTarget);
            client.Out.SendMessage($"Range to target: {range} units.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
        }
    }
}
