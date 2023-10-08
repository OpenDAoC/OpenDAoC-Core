using System;
using DOL.GS.Housing;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Commands;

[Command("&rent", //command to handle
	EPrivLevel.Player, //minimum privelege level
	"Pay house rent", //command description
    "Use /rent personal/guild <gold> to pay.")]
public class RentCommand : ACommandHandler, ICommandHandler
{
	public void OnCommand(GameClient client, string[] args)
	{

		if (args.Length < 2)
		{
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Rent.CmdUsage"),
                EChatType.CT_System, EChatLoc.CL_SystemWindow);

			return;
		}

        if (args.Length < 3)
        {
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Rent.CorrectFormat"),
                EChatType.CT_System, EChatLoc.CL_SystemWindow);
            return;
        }

        long amount = 0;
        try
        {
            amount = Int64.Parse(args[2]);
        }
        catch
        {
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Rent.CorrectFormat"),
                EChatType.CT_System, EChatLoc.CL_SystemWindow);

            return;
        }

        var goldToAdd = amount * 10000;

		switch (args[1].ToLower())
		{
			case "personal":
				{
                    House house = HouseMgr.GetHouseByPlayer(client.Player);
                    if (house == null)
                    {
                        client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Rent.NoHouse"),
                            EChatType.CT_System, EChatLoc.CL_SystemWindow);

                        return;
                    }

                    if (!house.CanPayRent(client.Player))
                    {
                        client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Rent.NoPayRentPerm"),
                            EChatType.CT_System, EChatLoc.CL_SystemWindow);

                        return;
                    }

                    if (house.KeptMoney >= (HouseMgr.GetRentByModel(house.Model) * ServerProperties.Properties.RENT_LOCKBOX_PAYMENTS))
                    {
                        client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Rent.AlreadyMaxMoney"),
                            EChatType.CT_System, EChatLoc.CL_SystemWindow);

                        return;
                    }

                    if (house.KeptMoney + goldToAdd > (HouseMgr.GetRentByModel(house.Model) * ServerProperties.Properties.RENT_LOCKBOX_PAYMENTS))
                    {
                        client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Rent.TooMuchMoney"),
                            EChatType.CT_System, EChatLoc.CL_SystemWindow);

                        return;
                    }
                    
                    if(!client.Player.RemoveMoney(goldToAdd))
                    {
                        client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Rent.NotEnoughMoney"),
                            EChatType.CT_System, EChatLoc.CL_SystemWindow);

                        return;
                    }

                    house.KeptMoney += goldToAdd;
                    house.SaveIntoDatabase();
                    
                    client.Player.SaveIntoDatabase();

                    client.Out.SendUpdatePoints();
                    client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Rent.YouSpend", amount),
                        EChatType.CT_System, EChatLoc.CL_SystemWindow);
				} break;
			case "guild":
				{
                    House house = HouseMgr.GetHouse(client.Player.Guild.GuildHouseNumber);
                    
                    if (house == null)
                    {
                        client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Rent.NoGuildHouse"),
                            EChatType.CT_System, EChatLoc.CL_SystemWindow);

                        return;
                    }
                    
                    if (house.DatabaseItem.GuildHouse && client.Player.GuildName == house.DatabaseItem.GuildName)
                    {
                        if (house.CanPayRent(client.Player))
                        {
                            if (client.Player.Guild.GetGuildBank() - goldToAdd <= 0)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Rent.NotEnoughGuildMoney"),
                                    EChatType.CT_System, EChatLoc.CL_SystemWindow);
                                return;
                            }

                            if (house.KeptMoney >= (HouseMgr.GetRentByModel(house.Model) * ServerProperties.Properties.RENT_LOCKBOX_PAYMENTS))
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Rent.AlreadyMaxMoney"),
                                    EChatType.CT_System, EChatLoc.CL_SystemWindow);

                                return;
                            }

                            if (house.KeptMoney + goldToAdd > (HouseMgr.GetRentByModel(house.Model) * ServerProperties.Properties.RENT_LOCKBOX_PAYMENTS))
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Rent.TooMuchMoney"),
                                    EChatType.CT_System, EChatLoc.CL_SystemWindow);

                                return;
                            }

                            house.KeptMoney += goldToAdd;
                            house.SaveIntoDatabase();

                            client.Player.Guild.WithdrawGuildBank(client.Player, goldToAdd);
                            client.Player.Guild.SaveIntoDatabase();

                            var message = $"{client.Player.Name} withdrew {amount} gold to pay the guild house's rent.";
                            
                            client.Player.Guild.SendMessageToGuildMembers(message, EChatType.CT_Guild, EChatLoc.CL_ChatWindow);

                            return;
                        }
                        
                        client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Rent.NoPayRentPerm"),
                                EChatType.CT_System, EChatLoc.CL_SystemWindow);

                            return;
                        
                    }

                    DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Rent.NotAHouseGuildLeader"));
				} break;
			default:
				{
                    DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Rent.CorrectFormat"));
				} break;
		}
	}
}