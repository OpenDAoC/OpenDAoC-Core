using System;
using Core.Database;
using Core.Database.Tables;
using Core.GS.Database;
using Core.GS.Enums;
using Core.GS.PacketHandler;
using Core.GS.Packets;
using Core.GS.PlayerTitles;

namespace Core.GS.Commands;

[Command(
	"&nohelp",
	EPrivLevel.Player,
	"Toggle nohelp on or off, to follow the path of solitude and stop receiving help from  your realm", "/nohelp>")]
public class NoHelpCommand : ACommandHandler, ICommandHandler
{
	public void OnCommand(GameClient client, string[] args)
	{
		if (IsSpammingCommand(client.Player, "nohelp"))
			return;
		
		if (!client.Player.NoHelp)
		{
			if (client.Player.RealmPoints > 0)
			{
				client.Player.Out.SendMessage("You have already received help and cannot join this challenge.", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
				return;
			}
		
			const string customKey = "grouped_char";
			var hasGrouped = CoreDb<DbCoreCharacterXCustomParam>.SelectObject(DB.Column("DOLCharactersObjectId")
				.IsEqualTo(client.Player.ObjectId).And(DB.Column("KeyName").IsEqualTo(customKey)));
		
			DateTime d1 = new DateTime(2022, 1, 4);
			DateTime d2 = new DateTime(2022, 1, 7, 21,0,0);
		
			if (client.Player.Level == 1 || hasGrouped == null && client.Player.CreationDate >= d1 && client.Player.CreationDate <= d2)
			{
				client.Out.SendCustomDialog("Do you want to follow the path of Solitude?",
					new CustomDialogResponse(NoHelpInitiate));
			}
			else
			{
				client.Player.Out.SendMessage("You have already received help and cannot join this challenge.",
					EChatType.CT_Important, EChatLoc.CL_SystemWindow);
			}
		}


		if (client.Player.NoHelp)
		{
			if (client.Player.HCFlag)
			{
				client.Player.Out.SendMessage("You cannot leave this challenge while you are in a Hardcore game.",
					EChatType.CT_Important, EChatLoc.CL_SystemWindow);
				return;
			}
			client.Out.SendCustomDialog(
					"Feeling lonely? Abandoning this path will void all your efforts in this challenge.",
					new CustomDialogResponse(NoHelpAbandon));
		}
			
	}

	protected virtual void NoHelpInitiate(GamePlayer player, byte response)
	{
		if (response == 1)
		{
			if (player.Level > 1)
			{
				player.Out.SendMessage("You have already received help and cannot join this challenge.",
					EChatType.CT_Important, EChatLoc.CL_SystemWindow);
				return;
			}
			
			NoHelpActivate(player);
		}
		else
		{
			player.Out.SendMessage("Use the command again if you change your mind.", EChatType.CT_Important,
				EChatLoc.CL_SystemWindow);
		}
	}

	public static void NoHelpActivate(GamePlayer player)
	{
		player.Emote(EEmote.Rude);
		player.NoHelp = true;
		player.Out.SendMessage(
			"You have chosen the path of solitude and will no longer receive any help from members of your Realm.",
			EChatType.CT_Important, EChatLoc.CL_SystemWindow);

		if (player.HCFlag)
			player.CurrentTitle = new HardCoreSoloTitle();
		else
			player.CurrentTitle = new NoHelpTitle();
	}

	protected virtual void NoHelpAbandon(GamePlayer player, byte response)
	{
		if (response == 1 && player.Level < 50)
		{
			{
				player.Emote(EEmote.Surrender);
				player.NoHelp = false;
				player.Out.SendMessage("You have chickened out. You can now run back to your ...friends.",
					EChatType.CT_Important, EChatLoc.CL_SystemWindow);
				const string customKey = "grouped_char";
				var hasGrouped = CoreDb<DbCoreCharacterXCustomParam>.SelectObject(DB.Column("DOLCharactersObjectId")
					.IsEqualTo(player.ObjectId).And(DB.Column("KeyName").IsEqualTo(customKey)));
				if (hasGrouped == null)
				{
					DbCoreCharacterXCustomParam groupedChar = new DbCoreCharacterXCustomParam();
					groupedChar.DOLCharactersObjectId = player.ObjectId;
					groupedChar.KeyName = customKey;
					groupedChar.Value = "1";
					GameServer.Database.AddObject(groupedChar);
				}

				player.CurrentTitle = PlayerTitleMgr.ClearTitle;
			}
		}
		else
		{
			player.Out.SendMessage("Use the command again if get scared once more.", EChatType.CT_Important,
				EChatLoc.CL_SystemWindow);
		}
	}
}