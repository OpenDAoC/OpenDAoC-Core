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

            if (client.Player.Level > 35)
            {
                client.Out.SendMessage("You are too high level to call a bounty!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                return;
            }
            
            if (client.Player.TempProperties.getProperty<GamePlayer>(KILLEDBY) == null)
            {
                client.Out.SendMessage("You have not been ganked ..yet!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                return;
            }

            if (args.Length < 2)
            {
                DisplaySyntax(client);
                return;
            }

            if (args[1] == "add")
            {
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
                
                AddBounty(client.Player, killerPlayer, amount);
                
            }
            else
            {          
                DisplaySyntax(client);
                return; 
            }
        }

        private static void AddBounty(GamePlayer killed, GamePlayer killer,  int amount = 50)
        {
            if (amount < 50) amount = 50;
            killed.Out.SendMessage($"You have called the head of {killer.Name} for {amount} gold!", eChatType.CT_System,
                eChatLoc.CL_SystemWindow);
            killed.TempProperties.removeProperty(KILLEDBY);
            
            BountyPoster poster = new BountyPoster(killed, killer, amount);
            
            
            foreach (var client in WorldMgr.GetAllPlayingClients())
            {
                if (client.Player.Realm != killed.Realm) continue;

                var message =
                    $"{killed.Name} is offering {amount} gold for the head of {killer.Name} in {killer.CurrentZone.Description}";
                
                client.Player.Out.SendMessage(message, eChatType.CT_ScreenCenterSmaller,
                        eChatLoc.CL_SystemWindow);
                client.Player.Out.SendMessage(message, eChatType.CT_Broadcast,
                    eChatLoc.CL_SystemWindow);
            }
        }
    }
}
