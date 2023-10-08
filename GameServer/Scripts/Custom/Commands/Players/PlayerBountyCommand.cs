using DOL.GS.Commands;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;

namespace DOL.GS.Scripts
{
    [Command(
        "&bounty",
        EPrivLevel.Player,
        "Allows to set a bounty on an enemy player", "/bounty list", "/bounty add <amount>")]
    public class PlayerBountyCommand : ACommandHandler, ICommandHandler
    {
        private const string KILLEDBY = "KilledBy";

        private int amount;
        private GamePlayer killerPlayer;

        private int minBountyReward = Properties.BOUNTY_MIN_REWARD;
        private int maxBountyReward = Properties.BOUNTY_MAX_REWARD;
        private int minLoyalty = Properties.BOUNTY_MIN_LOYALTY;

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
                BountyMgr.ResetBounty();
                return;
            }

            if (args[1] == "list")
            {
                client.Out.SendCustomTextWindow("Active Bounties", BountyMgr.GetTextList(client.Player));
                return;
            }

            if (args[1] == "add")
            {
                if (client.Player.Level > 35)
                {
                    client.Out.SendMessage("You are too high level to call a bounty!", EChatType.CT_Important,
                        EChatLoc.CL_SystemWindow);
                    return;
                }

                var playerLoyalty = RealmLoyaltyMgr.GetPlayerRealmLoyalty(client.Player).Days;

                if (playerLoyalty < minLoyalty)
                {
                    client.Out.SendMessage($"You need to have at least {minLoyalty} days of Realm Loyalty to post a bounty.", EChatType.CT_Important,
                        EChatLoc.CL_SystemWindow);
                    return;
                }

                killerPlayer = client.Player.TempProperties.GetProperty<GamePlayer>(KILLEDBY);

                amount = minBountyReward;

                if (args.Length == 3)
                {
                    if (!int.TryParse(args[2], out amount))
                    {
                        amount = minBountyReward;
                    }

                    if (amount < minBountyReward)
                    {
                        client.Out.SendMessage("The minimum Bounty amount is 50g", EChatType.CT_Important,
                            EChatLoc.CL_SystemWindow);
                        amount = minBountyReward;
                    }

                    if (amount > maxBountyReward)
                    {
                        client.Out.SendMessage("The maximum Bounty amount is 1000g", EChatType.CT_Important,
                            EChatLoc.CL_SystemWindow);
                        amount = maxBountyReward;
                    }
                }

                if (killerPlayer == null)
                {
                    client.Out.SendMessage("You have not been ganked ..yet!", EChatType.CT_Important,
                        EChatLoc.CL_SystemWindow);
                    return;
                }

                if (killerPlayer.Client.Account.PrivLevel > 1)
                {
                    client.Out.SendMessage("You can't set a bounty on a GM!", EChatType.CT_Important,
                        EChatLoc.CL_SystemWindow);
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
                    player.Out.SendMessage("You dont have enough money!", EChatType.CT_Merchant,
                        EChatLoc.CL_SystemWindow);
                    return;
                }

                BountyMgr.AddBounty(player, killerPlayer, amount);
            }
            else
            {
                player.Out.SendMessage("Use the command again if you change your mind.", EChatType.CT_Important,
                    EChatLoc.CL_SystemWindow);
            }
        }
    }
}