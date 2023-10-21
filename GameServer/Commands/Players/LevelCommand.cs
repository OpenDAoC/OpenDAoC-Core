using System;
using Core.Database;
using Core.Database.Tables;
using Core.GS.Database;
using Core.GS.Enums;
using Core.GS.Players.Titles;
using Core.GS.Scripts;

namespace Core.GS.Commands;

[Command("&level", //command to handle
EPrivLevel.Player, //minimum privelege level
"Allows you to level a character to level 20 instantly if you have reached level 39 during soft launch", "/level - use this command at level 1 while at a trainer", "/level gear - use this command once you are level 20 to receive a full ROG suit")] //usage
public class LevelCommand : ACommandHandler, ICommandHandler
{
	private const string SoftLaunchLevelKey = "SoftLaunchSlashLevel";
	private const string SoftLaunchLevelGearKey = "SoftLaunchSlashLevelGear";

	public void OnCommand(GameClient client, string[] args)
	{
		var targetLevel = ServerProperties.Properties.SLASH_LEVEL_TARGET;

		if (args.Length < 2)
		{
			var today = DateTime.Now;
			var endSoftLaunch = new DateTime(2022, 07, 18, 15, 30,00);
		
			if (targetLevel <= 1)
			{
				DisplayMessage(client, "/level is disabled on this server.");
				return;
			}
		
			if (today < endSoftLaunch && client.Account.PrivLevel == 1)
			{
				client.Player.Out.SendMessage($"This command will be available after {endSoftLaunch} UTC+1", EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return;
			}
			var alreadyUsed = CoreDb<DbAccountXCustomParam>.SelectObject(DB.Column("Name").IsEqualTo(client.Account.Name).And(DB.Column("KeyName").IsEqualTo(SoftLaunchLevelKey)));
			
			if (alreadyUsed != null)
			{
				client.Player.Out.SendMessage("You have already used your /level credit.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return;
			}

			if ((int)client.Player.Realm == 2)
			{
				var hasCredit = AchievementUtil.CheckPlayerCredit("SoftLaunch39", client.Player, (int)client.Player.Realm);
				if (!hasCredit)
				{
					client.Player.Out.SendMessage($"You are not eligible to use /level on {client.Player.Realm.ToString()}.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
					return;
				}
			}
			else
			{
				var hasCredit = AchievementUtil.CheckAccountCredit("SoftLaunch39", client.Player);
		
				if (!hasCredit)
				{
					client.Player.Out.SendMessage("You are not eligible to use /level.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
					return;
				}
			}
			
			
			if (client.Player.TargetObject is not (GameTrainer or MasterTrainer))
			{
				client.Player.Out.SendMessage("You need to be at your trainer to use this command.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return;
			}
			
			if (client.Player.Level != 1)
			{
				client.Player.Out.SendMessage("/level can only be used at level 1.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return;
			}
		
			client.Out.SendCustomDialog("Do you want to use /level on this character?\n The reward can only be redeemed once per account no matter how many level 39 you have.", SlashLevelResponseHandler);
		}
		else
		{
			if (args[1] == "gear")
			{
				if (!client.Player.UsedLevelCommand)
				{
					client.Player.Out.SendMessage("This command can only be used by /level characters.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
					return;
				}
				
				if(client.Player.Level != targetLevel)
				{
					client.Player.Out.SendMessage($"This command can only be used at level {targetLevel}.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
					return;
				}
				
				var alreadyUsed = CoreDb<DbAccountXCustomParam>.SelectObject(DB.Column("Name").IsEqualTo(client.Account.Name).And(DB.Column("KeyName").IsEqualTo(SoftLaunchLevelGearKey)));
				
				if (alreadyUsed != null)
				{
					client.Player.Out.SendMessage("You have already received your /level complementary gear.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
					return;
				}

				var slashlevelgear = new DbAccountXCustomParam();
				slashlevelgear.Name = client.Account.Name;
				slashlevelgear.KeyName = SoftLaunchLevelGearKey;
				slashlevelgear.Value = "1";
				GameServer.Database.AddObject(slashlevelgear);
				
				client.Player.Out.SendMessage($"You have been given gear for your level {targetLevel}.", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
				
			}
			else
			{
				DisplaySyntax(client);
			}
		}
		
		
	}
	
	protected virtual void SlashLevelResponseHandler(GamePlayer player, byte response)
	{
		if (response == 1)
		{
			int targetLevel = ServerProperties.Properties.SLASH_LEVEL_TARGET;

			if( targetLevel < 1 || targetLevel > 50 )
				targetLevel = 20;

			long newXP;
			newXP = player.GetExperienceNeededForLevel(targetLevel) - player.Experience;

			if (newXP < 0)
				newXP = 0;

			player.GainExperience(EXpSource.Other, newXP);
			player.UsedLevelCommand = true;
			player.Out.SendMessage($"You have been rewarded enough Experience to reach level {targetLevel}, right click on your trainer to gain levels!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
			player.Out.SendMessage($"Use '/level gear' when you have reached {targetLevel} to receive a set of complementary ROGs.", EChatType.CT_System, EChatLoc.CL_SystemWindow);

			player.SaveIntoDatabase();
			
			var slashlevel = new DbAccountXCustomParam();
			slashlevel.Name = player.Client.Account.Name;
			slashlevel.KeyName = SoftLaunchLevelKey;
			slashlevel.Value = "1";
			GameServer.Database.AddObject(slashlevel);
            
		}
		else
		{
			player.Out.SendMessage("Use the command again if you change your mind.", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
		}
	}
}