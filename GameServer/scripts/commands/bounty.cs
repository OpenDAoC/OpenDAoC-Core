using DOL.GS.Commands;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;


namespace DOL.GS.Scripts
{
    [CmdAttribute(
        "&bounty",
        ePrivLevel.Player,
        "Allows to set a bounty on an enemy player", "/bounty list", "/bounty add <amount>")]
    public class BountyCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        private const string KILLEDBY = "KilledBy";

        private int amount;
        private GamePlayer killerPlayer;

        private int minBountyReward = Properties.BOUNTY_MIN_REWARD;
        private int maxBountyReward = Properties.BOUNTY_MAX_REWARD;

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

            // todo: remove this
            if (args[1] == "clear")
            {
                if (client.Account.PrivLevel < 3) return;
                BountyManager.ResetBounty();
                return;
            }

            if (args[1] == "list")
            {
                client.Out.SendCustomTextWindow("Active Bounties", BountyManager.GetTextList(client.Player));
                return;
            }

            if (args[1] == "add")
            {
                if (client.Player.Level > 35)
                {
                    client.Out.SendMessage("You are too high level to call a bounty!", eChatType.CT_Important,
                        eChatLoc.CL_SystemWindow);
                    return;
                }

                killerPlayer = client.Player.TempProperties.getProperty<GamePlayer>(KILLEDBY);

                amount = minBountyReward;

                if (args.Length == 3)
                {
                    if (!int.TryParse(args[2], out amount))
                    {
                        amount = minBountyReward;
                    }

                    if (amount < minBountyReward)
                    {
                        client.Out.SendMessage("The minimum Bounty amount is 50g", eChatType.CT_Important,
                            eChatLoc.CL_SystemWindow);
                        amount = minBountyReward;
                    }

                    if (amount > maxBountyReward)
                    {
                        client.Out.SendMessage("The maximum Bounty amount is 1000g", eChatType.CT_Important,
                            eChatLoc.CL_SystemWindow);
                        amount = maxBountyReward;
                    }
                }

                if (killerPlayer == null)
                {
                    client.Out.SendMessage("You have not been ganked ..yet!", eChatType.CT_Important,
                        eChatLoc.CL_SystemWindow);
                    return;
                }

                if (killerPlayer.Client.Account.PrivLevel > 1)
                {
                    client.Out.SendMessage("You can't set a bounty on a GM!", eChatType.CT_Important,
                        eChatLoc.CL_SystemWindow);
                    return;
                }

                client.Out.SendCustomDialog(
                    $"Do you want to post a bounty on {killerPlayer.Name}'s head for {amount}g?",
                    BountyResponseHandler);
            }
            else
            {
                DisplaySyntax(client);
            }
        }

        protected virtual void BountyResponseHandler(GamePlayer player, byte response)
        {
            if (response == 1)
            {
                if (!player.RemoveMoney(amount * 100 * 100,
                        $"You have posted a Bounty Hunt on {killerPlayer.Name} for {amount} gold."))
                {
                    player.Out.SendMessage("You dont have enough money!", eChatType.CT_Merchant,
                        eChatLoc.CL_SystemWindow);
                    return;
                }

                BountyManager.AddBounty(player, killerPlayer, amount);
            }
            else
            {
                player.Out.SendMessage("Use the command again if you change your mind.", eChatType.CT_Important,
                    eChatLoc.CL_SystemWindow);
            }
        }
    }
}