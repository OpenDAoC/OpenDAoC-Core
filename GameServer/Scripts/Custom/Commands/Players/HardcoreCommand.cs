using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Core.GS.PacketHandler;
using Core.GS.PlayerTitles;
using Core.Database;
using Core.Events;
using log4net;

namespace Core.GS.Commands
{
    [Command(
        "&hardcore",
        EPrivLevel.Player,
        "Flags a player as Hardcore. Dying after activating Hardcore will result in the character deletion.",
        "/hardcore on")]
    public class HardcoreCommand : ACommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if (IsSpammingCommand(client.Player, "hardcore"))
                return;
            
            if (client.Player.RealmPoints > 0)
                return;
            
            if (client.Player.HCFlag){
                client.Out.SendMessage("Your Hardcore flag is ON! Death will result in the character deletion.", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
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
                    client.Out.SendMessage("You must be level 1 to activate Hardcore.", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                    return;
                }
                client.Out.SendCustomDialog("Do you really want to activate the Hardcore flag? Death will be permanent.", new CustomDialogResponse(HardcoreResponseHandler));
            }
        }
        
        protected virtual void HardcoreResponseHandler(GamePlayer player, byte response)
        {
            if (response == 1)
            {
                if (player.Level > 1)
                {
                    player.Out.SendMessage("You must be level 1 to activate Hardcore.", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                    return;
                }
                
                player.Emote(EEmote.StagFrenzy);
                player.HCFlag = true;
                player.Out.SendMessage("Your HARDCORE flag is ON. Your character will be deleted at death.", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                
                NoHelpCommand.NoHelpActivate(player);

                if (player.NoHelp)
                {
                    player.CurrentTitle = new HardCoreSoloTitle();
                }
                else
                {
                    player.CurrentTitle = new HardcoreTitle();
                }
                
            }
            else
            {
                player.Out.SendMessage("Use the command again if you change your mind.", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
            }
        }
    }
}