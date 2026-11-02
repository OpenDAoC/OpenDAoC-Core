using System;
using DOL.GS.Housing;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;
using DOL.Language;

namespace DOL.GS.Commands
{
    public abstract class AbstractRentCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        private const string MaxKeyword = "max";

        protected abstract long CopperPerUnit { get; }

        // Translation strings.
        protected abstract string KeyPrefix { get; }
        protected abstract string NotEnoughFundsKey { get; }
        protected abstract string NotEnoughGuildFundsKey { get; }

        protected abstract bool TryChargePlayer(GamePlayer player, long amount, long copper);
        protected abstract bool TryChargeGuild(Guild guild, long amount, long copper);
        protected abstract string GetGuildAnnouncement(GamePlayer player, long amount);

        protected virtual void OnPaid(GameClient client, long amount) { }

        public void OnCommand(GameClient client, string[] args)
        {
            if (args.Length < 3)
            {
                DisplaySyntax(client);
                return;
            }

            long unit = CopperPerUnit;

            // Reject misconfigured rate.
            if (unit <= 0)
            {
                DisplaySyntax(client);
                return;
            }

            // null means "fill the lockbox to the max".
            long? requested = null;

            if (!args[2].Equals(MaxKeyword, StringComparison.OrdinalIgnoreCase))
            {
                // Rejects non-numbers, zero/negative amounts, and amount * unit overflowing.
                if (!long.TryParse(args[2], out long amount) || amount <= 0 || amount > long.MaxValue / unit)
                {
                    DisplaySyntax(client);
                    return;
                }

                requested = amount;
            }

            switch (args[1].ToLower())
            {
                case "personal":
                    PayPersonal(client, requested);
                    break;
                case "guild":
                    PayGuild(client, requested);
                    break;
                default:
                    DisplaySyntax(client);
                    break;
            }
        }

        private void PayPersonal(GameClient client, long? requested)
        {
            GamePlayer player = client.Player;

            House house = HouseMgr.GetHouseByPlayer(player);
            if (house == null)
            {
                Say(client, "NoHouse");
                return;
            }

            if (!house.CanPayRent(player))
            {
                Say(client, "NoPayRentPerm");
                return;
            }

            if (!TryResolveAmount(client, house, requested, out long amount, out long copper))
                return;

            if (!TryChargePlayer(player, amount, copper))
            {
                Say(client, NotEnoughFundsKey);
                return;
            }

            house.KeptMoney += copper;
            house.SaveIntoDatabase();

            player.SaveIntoDatabase();
            client.Out.SendUpdatePoints();

            Say(client, "YouSpend", amount);
            OnPaid(client, amount);
        }

        private void PayGuild(GameClient client, long? requested)
        {
            GamePlayer player = client.Player;

            Guild guild = player.Guild;
            if (guild == null)
            {
                Say(client, "NoGuild");
                return;
            }

            House house = HouseMgr.GetHouse(guild.GuildHouseNumber);
            if (house == null || !house.DatabaseItem.GuildHouse)
            {
                Say(client, "NoGuildHouse");
                return;
            }

            if (!house.CanPayRent(player))
            {
                Say(client, "NoPayRentPerm");
                return;
            }

            if (!TryResolveAmount(client, house, requested, out long amount, out long copper))
                return;

            if (!TryChargeGuild(guild, amount, copper))
            {
                Say(client, NotEnoughGuildFundsKey);
                return;
            }

            house.KeptMoney += copper;
            house.SaveIntoDatabase();
            guild.SaveIntoDatabase();

            guild.SendMessageToGuildMembers(GetGuildAnnouncement(player, amount), eChatType.CT_Guild, eChatLoc.CL_ChatWindow);

            Say(client, "YouSpendGuild", amount);
            OnPaid(client, amount);
        }

        private bool TryResolveAmount(GameClient client, House house, long? requested, out long amount, out long copper)
        {
            amount = 0;
            copper = 0;

            long unit = CopperPerUnit;
            long freeSpace = HouseMgr.GetRentByModel(house.Model) * Properties.RENT_LOCKBOX_PAYMENTS - house.KeptMoney;

            if (freeSpace <= 0)
            {
                Say(client, "AlreadyMaxMoney");
                return false;
            }

            if (requested == null)
            {
                amount = freeSpace / unit;

                // Less than one whole unit of room left.
                if (amount <= 0)
                {
                    Say(client, "AlreadyMaxMoney");
                    return false;
                }
            }
            else
            {
                amount = requested.Value;

                // No overflow here. OnCommand already limited amount to long.MaxValue / unit.
                if (amount * unit > freeSpace)
                {
                    Say(client, "TooMuchMoney");
                    return false;
                }
            }

            copper = amount * unit;
            return true;
        }

        private void Say(GameClient client, string keySuffix, params object[] args)
        {
            DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, $"{KeyPrefix}.{keySuffix}", args));
        }
    }

    [Cmd("&rent",
        ePrivLevel.Player,
        "Pay house rent",
        "Use /rent <personal | guild> <gold> to pay.",
        "Use /rent <personal | guild> max to fill the lockbox to the max.")]
    public class RentCommandHandler : AbstractRentCommandHandler
    {
        protected override long CopperPerUnit => 10000; // 1 gold.
        protected override string KeyPrefix => "Scripts.Players.Rent";
        protected override string NotEnoughFundsKey => "NotEnoughMoney";
        protected override string NotEnoughGuildFundsKey => "NotEnoughGuildMoney";

        protected override bool TryChargePlayer(GamePlayer player, long amount, long copper)
        {
            return player.RemoveMoney(copper);
        }

        protected override bool TryChargeGuild(Guild guild, long amount, long copper)
        {
            return guild.RemoveFromBank(copper, false);
        }

        protected override string GetGuildAnnouncement(GamePlayer player, long amount)
        {
            return $"{player.Name} withdrew {amount} gold to pay the guild house's rent.";
        }
    }

    [Cmd("&bountyrent",
        ePrivLevel.Player,
        "Pay house rent with bounty points",
        "Use /bountyrent <personal | guild> <amount> to pay.",
        "Use /bountyrent <personal | guild> max to fill the lockbox to the max.")]
    public class BountyRentCommandHandler : AbstractRentCommandHandler
    {
        protected override long CopperPerUnit => RentBountyPointToCopper;
        protected override string KeyPrefix => "Scripts.Players.Bountyrent";
        protected override string NotEnoughFundsKey => "NotEnoughBp";
        protected override string NotEnoughGuildFundsKey => "NotEnoughGuildBp";

        // RENT_BOUNTY_POINT_TO_GOLD is actually in copper.
        private static long RentBountyPointToCopper => Properties.RENT_BOUNTY_POINT_TO_GOLD;

        public override void DisplaySyntax(GameClient client)
        {
            base.DisplaySyntax(client);
            DisplayBountyPointsWorth(client);
        }

        public override void DisplaySyntax(GameClient client, string subcommand)
        {
            base.DisplaySyntax(client, subcommand);
            DisplayBountyPointsWorth(client);
        }

        public override void DisplaySyntax(GameClient client, string subcommand1, string subcommand2)
        {
            base.DisplaySyntax(client, subcommand1, subcommand2);
            DisplayBountyPointsWorth(client);
        }

        protected override bool TryChargePlayer(GamePlayer player, long amount, long copper)
        {
            if (player.BountyPoints < amount)
                return false;

            player.BountyPoints -= amount;
            return true;
        }

        protected override bool TryChargeGuild(Guild guild, long amount, long copper)
        {
            if (guild.BountyPoints < amount)
                return false;

            guild.BountyPoints -= amount;
            return true;
        }

        protected override string GetGuildAnnouncement(GamePlayer player, long amount)
        {
            return $"{player.Name} withdrew {amount} bounty point{(amount == 1 ? "" : "s")} to pay the guild house's rent.";
        }

        protected override void OnPaid(GameClient client, long amount)
        {
            DisplayBountyPointsWorth(client, amount);
        }

        private void DisplayBountyPointsWorth(GameClient client, long amount = 1)
        {
            DisplayMessage(client, $"{amount} bounty point{(amount == 1 ? "" : "s")} {(amount == 1 ? "is" : "are")} worth {Money.GetString(amount * RentBountyPointToCopper)}.");
        }
    }
}
