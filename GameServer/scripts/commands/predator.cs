using DOL.GS.Commands;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;


namespace DOL.GS.Scripts
{
    [CmdAttribute(
        "&predator",
        ePrivLevel.Player,
        "Join the hunt or view your current prey", "/predator join", "/predator viewprey")]
    public class PredatorCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        private const string KILLEDBY = "KilledBy";

        private int amount;
        private GamePlayer killerPlayer;

        private int minBountyReward = Properties.BOUNTY_MIN_REWARD;
        private int maxBountyReward = Properties.BOUNTY_MAX_REWARD;
        private int minLoyalty = Properties.BOUNTY_MIN_LOYALTY;

        public void OnCommand(GameClient client, string[] args)
        {
            
            if (IsSpammingCommand(client.Player, "Predator"))
            {
                return;
            }

            if (args.Length < 2)
            {
                DisplaySyntax(client);
                return;
            }

            // todo: remove this
            if (args[1] == "viewprey")
            {
                //if player is in PredatorManager.ActivePlayers, view current target
                //else return message w/ error
                if (!PredatorManager.PlayerIsActive(client.Player))
                {
                    client.Out.SendMessage("You are not a part of the hunt!", eChatType.CT_Important,
                        eChatLoc.CL_SystemWindow);
                    return;
                }

                client.Out.SendCustomTextWindow("Your Prey", PredatorManager.GetActivePrey(client.Player));
            }
            //client.Out.SendCustomTextWindow("Active Bounties", BountyManager.GetTextList(client.Player));

            if (args[1] == "join")
            {
                if (client.Player.Level < 50)
                {
                    client.Out.SendMessage("You must be level 50 to join the hunt!", eChatType.CT_Important,
                        eChatLoc.CL_SystemWindow);
                    return;
                }
                
                PredatorManager.QueuePlayer(client.Player);

            }
            else if (args[1] == "reset")
            {
                PredatorManager.FullReset();
            }
            else if (args[1] == "insert")
            {
                PredatorManager.InsertQueuedPlayers();  
            }
            else
            {
                DisplaySyntax(client);
            }
        }
    }
}