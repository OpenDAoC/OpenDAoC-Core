using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;

namespace DOL.GS.Commands
{
    [CmdAttribute("&pairspells", ePrivLevel.Player, "Pair a spell with another.", "/pairspells <set|clear>")]
    public class PairSpellsCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if (IsSpammingCommand(client.Player, "pairspells"))
                return;

            if (!Properties.ALLOW_PAIRED_SPELLS)
            {
                client.Out.SendMessage("This command is not enabled on this server.", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
                return;
            }

            if (args.Length < 2)
            {
                DisplaySyntax(client);
                return;
            }

            GamePlayer player = client.Player;
            CastingComponent castingComponent = player.castingComponent;

            if (castingComponent.PairedSpellCommandInputStep != CastingComponent.PairedSpellInputStep.NONE)
            {
                client.Out.SendMessage("Finish your current pairing before starting a new one.", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
                return;
            }

            switch (args[1])
            {
                case "set":
                {
                    castingComponent.PairedSpellCommandInputStep = CastingComponent.PairedSpellInputStep.FIRST;
                    client.Out.SendMessage("The next two spells you use will be paired.", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
                    break;
                }
                case "clear":
                {
                    castingComponent.PairedSpellCommandInputStep = CastingComponent.PairedSpellInputStep.CLEAR;
                    client.Out.SendMessage("The next spell you use will be unpaired.", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
                    break;
                }
                default:
                {
                    DisplaySyntax(client);
                    return;
                }
            }
        }
    }
}
