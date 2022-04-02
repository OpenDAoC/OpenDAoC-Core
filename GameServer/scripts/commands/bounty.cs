using System;
using DOL.GS.Commands;
using DOL.GS.PacketHandler;


namespace DOL.GS.Scripts
{
    [CmdAttribute(
        "&bounty",
        ePrivLevel.Player,
        "Allows to set a bounty on an enemy player", "/bounty add <amount>")]
    public class BountyCommandHandler : AbstractCommandHandler, ICommandHandler
    {

        public const string KILLEDBY = "KilledBy";

        public void OnCommand(GameClient client, string[] args)
        {
            if (IsSpammingCommand(client.Player, "Bounty"))
            {
                return;
            }
            
            if (args.Length < 2)
            {
                DisplaySyntax(client);
                return;
            }
            
            if (args[1] == "clear")
            {
                BountyManager.ActiveBounties.Clear();
                return;
            }

            if (args[1] == "list")
            {
                client.Out.SendCustomTextWindow("Active Bounties", BountyManager.GetTextList());
                return;
            }

            if (args[1] == "add")
            {
                if (client.Player.TempProperties.getProperty<GamePlayer>(KILLEDBY) == null)
                {
                    client.Out.SendMessage("You have not been ganked ..yet!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                    return;
                }
                
                if (client.Player.Level > 35)
                {
                    client.Out.SendMessage("You are too high level to call a bounty!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                    return;
                }
                
                int amount = 0;
                if (args.Length == 3)
                {
                    if (!int.TryParse(args[2], out amount))
                    {
                        amount = 50;
                    }
                }

                GamePlayer killerPlayer = client.Player.TempProperties.getProperty<GamePlayer>(KILLEDBY);

                if (killerPlayer.Client.Account.PrivLevel > 1)
                {
                    client.Out.SendMessage("You can't set a bounty on a GM!", eChatType.CT_System,
                        eChatLoc.CL_SystemWindow);
                    return;
                }
                
                BountyManager.AddBounty(client.Player, killerPlayer, amount);
                
            }
            else
            {          
                DisplaySyntax(client);
                return; 
            }
        }
        
    }
}
