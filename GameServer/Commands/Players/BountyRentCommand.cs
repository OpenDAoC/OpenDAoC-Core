using System;
using Core.GS.Enums;
using Core.GS.Housing;
using Core.GS.PacketHandler;
using Core.Language;

namespace Core.GS.Commands;

[Command("&bountyrent", //command to handle
	EPrivLevel.Player, //minimum privelege level
	"Pay house rent with bounty points.", //command description
    "Use /bountyrent personal/guild <amount> to pay.")]
public class BountyRentCommand : ACommandHandler, ICommandHandler
{
	public void OnCommand(GameClient client, string[] args)
	{
        long bpWorth = ServerProperties.Properties.RENT_BOUNTY_POINT_TO_GOLD;

		if (args.Length < 2)
		{
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Bountyrent.CmdUsage", bpWorth),
                EChatType.CT_System, EChatLoc.CL_SystemWindow);

			return;
		}

        if (args.Length < 3)
        {
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Bountyrent.CorrectFormat"),
                EChatType.CT_System, EChatLoc.CL_SystemWindow);

            return;
        }

        House house = client.Player.CurrentHouse;
		if (house == null)
		{
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Bountyrent.RangeOfAHouse"),
                EChatType.CT_System, EChatLoc.CL_SystemWindow);

			return;
		}

        long BPsToAdd = 0;
        try
        {
            BPsToAdd = Int64.Parse(args[2]);
        }
        catch
        {
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Bountyrent.CorrectFormat"),
                EChatType.CT_System, EChatLoc.CL_SystemWindow);

            return;
        }

		switch (args[1].ToLower())
		{
			case "personal":
				{
                    if (!house.CanPayRent(client.Player))
                    {
                        client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Bountyrent.NoPayRentPerm"),
                            EChatType.CT_System, EChatLoc.CL_SystemWindow);

                        return;
                    }

					if ((client.Player.BountyPoints -= BPsToAdd) < 0)
					{
                        client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Bountyrent.NotEnoughBp"),
                            EChatType.CT_System, EChatLoc.CL_SystemWindow);

						return;
					}

                    if (house.KeptMoney >= (HouseMgr.GetRentByModel(house.Model) * ServerProperties.Properties.RENT_LOCKBOX_PAYMENTS))
                    {
                        client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Bountyrent.AlreadyMaxMoney"),
                            EChatType.CT_System, EChatLoc.CL_SystemWindow);

                        return;
                    }

                    if ((house.KeptMoney + (BPsToAdd * bpWorth)) > (HouseMgr.GetRentByModel(house.Model) * ServerProperties.Properties.RENT_LOCKBOX_PAYMENTS))
                    {
                        client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Bountyrent.ToManyMoney"),
                            EChatType.CT_System, EChatLoc.CL_SystemWindow);

                        return;
                    }

                    house.KeptMoney += (BPsToAdd * bpWorth);
                    house.SaveIntoDatabase();

                    client.Player.BountyPoints -= BPsToAdd;
                    client.Player.SaveIntoDatabase();

                    client.Out.SendUpdatePoints();
                    client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Bountyrent.YouSpend", BPsToAdd, ((BPsToAdd * bpWorth) / bpWorth)),
                        EChatType.CT_System, EChatLoc.CL_SystemWindow);
				} break;
			case "guild":
				{
                    if (house.DatabaseItem.GuildHouse && client.Player.GuildName == house.DatabaseItem.GuildName)
                    {
                        if (house.CanPayRent(client.Player))
                        {
                            if ((client.Player.Guild.BountyPoints -= BPsToAdd) < 0)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Bountyrent.NotEnoughGuildBp"),
                                    EChatType.CT_System, EChatLoc.CL_SystemWindow);
                                return;
                            }

                            if (house.KeptMoney >= (HouseMgr.GetRentByModel(house.Model) * ServerProperties.Properties.RENT_LOCKBOX_PAYMENTS))
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Bountyrent.AlreadyMaxMoney"),
                                    EChatType.CT_System, EChatLoc.CL_SystemWindow);

                                return;
                            }

                            if ((house.KeptMoney + (BPsToAdd * bpWorth)) > (HouseMgr.GetRentByModel(house.Model) * ServerProperties.Properties.RENT_LOCKBOX_PAYMENTS))
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Bountyrent.ToManyMoney"),
                                    EChatType.CT_System, EChatLoc.CL_SystemWindow);

                                return;
                            }

                            house.KeptMoney += (BPsToAdd * bpWorth);
                            house.SaveIntoDatabase();

                            client.Player.Guild.BountyPoints -= BPsToAdd;
                            client.Player.Guild.SaveIntoDatabase();

                            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Bountyrent.YouSpendGuild", BPsToAdd, ((BPsToAdd * bpWorth) / bpWorth)),
                                EChatType.CT_System, EChatLoc.CL_SystemWindow);

                            return;
                        }
                        else
                        {
                            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Bountyrent.NoPayRentPerm"),
                                EChatType.CT_System, EChatLoc.CL_SystemWindow);

                            return;
                        }
                    }

                    DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Bountyrent.NotAHouseGuildLeader"));
				} break;
			default:
				{
                    DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Bountyrent.CorrectFormat"));
				} break;
		}
	}
}