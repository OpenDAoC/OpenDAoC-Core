/*
 *
 * ATLAS - Hardcore
 *
 */

using DOL.Events;
using DOL.GS.PacketHandler;
using DOL.GS.PlayerTitles;

namespace DOL.GS.Commands
{
    [CmdAttribute(
        "&hardcore",
        ePrivLevel.Player,
        "Flags a player as Hardcore. Dying after activating Hardcore will result in the character deletion.",
        "/hardcore on")]
    public class HardcoreCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if (IsSpammingCommand(client.Player, "hardcore"))
                return;
            
            if (client.Player.HCFlag){
                client.Out.SendMessage("Your Hardcore flag is ON! Death will result in the character deletion.", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                return;
            }

            if (args.Length < 2)
            {
                DisplaySyntax(client);
                return;
            }

            if (args[1].ToLower().Equals("on"))
            {
                if (client.Player.Level != 1)
                {
                    client.Out.SendMessage("You must be level 1 to activate Hardcore.", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                    return;
                }
                client.Out.SendCustomDialog("Do you really want to activate the Hardcore flag? Death will be permanent.", new CustomDialogResponse(HardcoreResponseHandler));
            }
        }
        
        protected virtual void HardcoreResponseHandler(GamePlayer player, byte response)
        {
            if (response == 1)
            {
                {
                    player.Emote(eEmote.StagFrenzy);
                    player.HCFlag = true;
                    player.Out.SendMessage("Your HARDCORE flag is ON. Your character will be deleted at death.", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                    player.CurrentTitle = new HardCoreTitle();
                }
            }
            else
            {
                player.Out.SendMessage("Use the command again if you change your mind.", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
            }
        }
    }
}
#region title
namespace DOL.GS.PlayerTitles
{
    public class HardCoreTitle : SimplePlayerTitle
    {

        public override string GetDescription(GamePlayer player)
        {
            return "Hardcore";
        }
        
        public override string GetValue(GamePlayer source, GamePlayer player)
        {
            return "Hardcore";
        }
        
        public override void OnTitleGained(GamePlayer player)
        {
            player.Out.SendMessage("You have gained the Hardcore title!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
        }

        public override bool IsSuitable(GamePlayer player)
        {
            return player.HCFlag || player.HCCompleted;
        }
    }
}
#endregion