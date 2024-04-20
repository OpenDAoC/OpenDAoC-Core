using DOL.GS.PacketHandler;

namespace DOL.GS.Commands
{
    [CmdAttribute("&checkconcolor", ePrivLevel.Player, "Check the target's con color server-side.", "/checkconcolor")]
    public class CheckConColorCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if (IsSpammingCommand(client.Player, "checkconcolor"))
                return;

            GamePlayer player = client.Player;
            GameObject target = player.TargetObject;

            if (target == null)
            {
                client.Out.SendMessage("This command requires a target.", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
                return;
            }

            client.Out.SendMessage($"{ConLevels.GetConColor(ConLevels.GetConLevel(player.EffectiveLevel, target.EffectiveLevel))}", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
        }
    }
}
