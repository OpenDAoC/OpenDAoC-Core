using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using DOL.Database;
using DOL.Events;
using DOL.GS.Effects;
using DOL.GS.Friends;
using DOL.GS.Housing;
using DOL.GS.PacketHandler;
using DOL.GS.Quests;

namespace DOL.GS.Commands
{
	[Command(
		"&player",
		EPrivLevel.GM,
		"Various Admin/GM commands to edit characters.",
		"/player name <newName>",
		"/player lastname <change|reset> <newLastName>",
		"/player level <newLevel>",
        "/player levelup",
        "/player reset - Reset and re-level a player to their current level.",
		"/player realm <newRealm>",
		"/player inventory [wear|bag|vault|house|cons]",
		"/player <rps|bps|xp|xpa|clxp|mlxp> <amount>",
		"/player stat <typeofStat> <value>",
		"/player money <copp|silv|gold|plat|mith> <amount>",
		"/player respec <all|line|realm|dol|champion> <amount=1>",
		"/player model <reset|[change]> <modelid>",
		"/player friend <list|playerName>",
		"/player <rez|kill> <albs|mids|hibs|self|all>", // if realm not specified, it will rez target.
		"/player jump <group|guild|cg|bg> <name>", // to jump a group to you, just type in a player's name and his or her entire group will come with.
		"/player kick <all>",
		"/player save <all>",
		"/player purge",
		"/player update",
		"/player info",
		"/player location - write a location string to the chat window",
		"/player showgroup",
		"/player showeffects",
		"/player startchampion - Starts the target on the path of the Champion.",
		"/player clearchampion - Remove all Champion XP and levels from this player.",
		"/player respecchampion - Respec this players Champion skills.",
		"/player saddlebags <0 - 15> - Set what horse saddlebags are active on this player",
		"/player startml - Start this players Master Level training.",
		"/player setml <level> - Set this players current Master Level.",
		"/player setmlstep <level> <step> [false] - Sets a step for an ML level to finished. 0 to set as unfinished.",
        "/player allchars <PlayerName>", 
        "/player class <list|classID|className> - view a list of classes, or change the targets class.",
        "/player areas - list all the areas the player is currently inside of "
		)]
	public class PlayerCommand : AbstractCommandHandler, ICommandHandler
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public void OnCommand(GameClient client, string[] args)
        {
            if (args.Length == 1)
            {
                DisplaySyntax(client);
                return;
            }

            switch (args[1])
            {
                #region name

                case "name":
                    {
                        var player = client.Player.TargetObject as GamePlayer;
                        if (args.Length != 3)
                        {
                            DisplaySyntax(client);
                            return;
                        }

                        if (player == null)
                            player = client.Player;

                        var character = CoreDb<DbCoreCharacters>.SelectObject(DB.Column("Name").IsEqualTo(args[2]));

                        if (character != null)
                        {
                            client.Out.SendMessage("Duplicate Name!", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                            return;
                        }

                        string oldName = player.Name;

                        player.Name = args[2];
                        player.Out.SendMessage(
                            client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has changed your name to " + player.Name +
                            "!", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                        client.Out.SendMessage("You successfully changed this players name to " + player.Name + "!",
                                               EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                        client.Out.SendMessage("Tell the player to Log out and back in to complete the change.", EChatType.CT_Important,
                                               EChatLoc.CL_SystemWindow);

                        // Log change
                        AuditMgr.AddAuditEntry(client, EAuditType.Character, EAuditSubtype.CharacterRename, oldName, args[2]);

                        player.SaveIntoDatabase();
                        break;
                    }

                #endregion

                #region lastname

                case "lastname":
                    {
                        var player = client.Player.TargetObject as GamePlayer;
                        if (args.Length > 4)
                        {
                            DisplaySyntax(client);
                            return;
                        }

                        if (player == null)
                            player = client.Player;

                        switch (args[2])
                        {
                            case "change":
                                {
                                    player.LastName = args[3];
                                    player.Out.SendMessage(
                                        client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has changed your lastname to " +
                                        player.LastName + "!", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                                    client.Out.SendMessage("You successfully changed " + player.Name + "'s lastname to " + player.LastName + "!",
                                                           EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                                    player.SaveIntoDatabase();
                                    break;
                                }

                            case "reset":
                                {
                                    player.LastName = null;
                                    client.Out.SendMessage("You cleared " + player.Name + "'s lastname successfully!", EChatType.CT_Important,
                                                           EChatLoc.CL_SystemWindow);
                                    player.Out.SendMessage(
                                        client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has cleared your lastname!",
                                        EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                                    player.SaveIntoDatabase();
                                    break;
                                }
                        }
                        break;
                    }

                #endregion

                #region level / reset
                case "levelup":
                    var pToLevel = client.Player.TargetObject as GamePlayer;
                    if (pToLevel == null)
                        pToLevel = client.Player;

                    if (pToLevel.Level != byte.MaxValue)
                    {
                        if (pToLevel.Level < 40 || pToLevel.IsLevelSecondStage)
                        {
                            pToLevel.Level++;

                            client.Out.SendMessage("You gave " + pToLevel.Name + " a free level!",
                                                       EChatType.CT_Important, EChatLoc.CL_SystemWindow);

                            if (pToLevel != client.Player)
                                pToLevel.Out.SendMessage(
                                    client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has given you a free level!",
                                    EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                        }
                        else
                        {
                            pToLevel.GainExperience(EXpSource.Other, pToLevel.ExperienceForCurrentLevelSecondStage - pToLevel.Experience);

                            client.Out.SendMessage("You gave " + pToLevel.Name + " a free half level!",
                                                       EChatType.CT_Important, EChatLoc.CL_SystemWindow);

                            if (pToLevel != client.Player)
                                pToLevel.Out.SendMessage(
                                    client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has given you a free half level!",
                                    EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                        }
                        
                    }

                    break;
                case "reset":
                case "level":
                    {
                        try
                        {
                            var player = client.Player.TargetObject as GamePlayer;
                            if (player == null)
                                player = client.Player;

                            byte newLevel = player.Level;

                            if (args[1] == "level")
                            {
                                newLevel = Convert.ToByte(args[2]);
                            }

                            if (newLevel <= 0 || newLevel > 255)
                            {
                                client.Out.SendMessage(player.Name + "'s level can only be set to a number 1 to 255!", EChatType.CT_Important,
                                                       EChatLoc.CL_SystemWindow);
                                return;
                            }

                            if (newLevel < player.Level || args[1] == "reset")
                            {
                                player.Reset();
                            }

                            int curLevel = player.Level;

                            if (newLevel > curLevel)
                            {
                                bool curSecondStage = player.IsLevelSecondStage;
                                if (newLevel > curLevel && curSecondStage)
                                {
                                    player.GainExperience(EXpSource.Other, player.GetExperienceValueForLevel(++curLevel));
                                }
                                if (newLevel != curLevel || !curSecondStage)
                                    player.Level = newLevel;

                                // If new level is more than 40, then we have
                                // to add the skill points from half-levels
                                if (newLevel > 40)
                                {
                                    if (curLevel < 40)
                                        curLevel = 40;
                                    for (int i = curLevel; i < newLevel; i++)
                                    {
                                        // we skip the first add if was in level 2nd stage
                                        if (curSecondStage)
                                            curSecondStage = false;
                                    }
                                }
                            }

                            if (args[1] == "reset")
                            {
                                client.Out.SendMessage("You have reset " + player.Name + "!", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                                player.Out.SendMessage(
                                    client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has respecced your skills and reset your spec points!",
                                    EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                            }
                            else
                            {
                                client.Out.SendMessage("You changed " + player.Name + "'s level successfully to " + newLevel + "!",
                                                       EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                                player.Out.SendMessage(
                                    client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has changed your level to " + newLevel + "!",
                                    EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                            }


                            player.Out.SendUpdatePlayer();
                            player.Out.SendUpdatePoints();
                            player.Out.SendCharStatsUpdate();
                            player.UpdatePlayerStatus();
                            player.SaveIntoDatabase();
                        }

                        catch (Exception)
                        {
                            DisplaySyntax(client);
                            return;
                        }
                    }
                    break;

                #endregion

				#region Start Champion

				case "startchampion":
					try
					{
						var player = client.Player.TargetObject as GamePlayer;
						if (player == null)
							player = client.Player;

						if (player.Champion == false)
						{
							player.Champion = true;
							player.SaveIntoDatabase();
							client.Out.SendMessage(player.Name + " is now on the path of the Champion!", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
							player.Out.SendMessage(client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has started you on the path of the Champion!", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
						}
						else
						{
							client.Out.SendMessage(player.Name + " is already on the path of the Champion!", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
						}

					}
					catch
					{
						DisplaySyntax(client);
						return;
					}
					break;

				#endregion Start Champion

				#region Clear / Respec Champion

				case "clearchampion":

                    try
                    {
                        var player = client.Player.TargetObject as GamePlayer;
                        if (player == null)
                            player = client.Player;

                        player.RemoveChampionLevels();
                        client.Out.SendMessage("You have cleared " + player.Name + "'s Champion levels!", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                        player.Out.SendMessage(client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has cleared your Champion levels!", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                    }

                    catch (Exception)
                    {
                        DisplaySyntax(client);
                        return;
                    }
                    break;

				case "respecchampion":

					try
					{
						var player = client.Player.TargetObject as GamePlayer;
						if (player == null)
							player = client.Player;

						player.RespecChampionSkills();
						client.Out.SendMessage("You have respecced " + player.Name + "'s Champion levels!", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
						player.Out.SendMessage(client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has respecced your Champion levels!", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
					}

					catch (Exception)
					{
						DisplaySyntax(client);
						return;
					}
					break;

                #endregion Clear / Respec Champion

				#region Master Levels

				case "startml":

					try
					{
						var player = client.Player.TargetObject as GamePlayer;
						if (player == null)
							player = client.Player;

						if (player.MLGranted == false)
						{
							player.MLGranted = true;
							player.SaveIntoDatabase();
							client.Out.SendMessage(player.Name + " is now ready to start Master Level training!", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
							player.Out.SendMessage(client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has started your Master Level training!", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
						}
						else
						{
							client.Out.SendMessage(player.Name + " has already started Master Level training!", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
						}
					}
					catch (Exception)
					{
						DisplaySyntax(client);
						return;
					}
					break;

				case "setml":

					try
					{
						var player = client.Player.TargetObject as GamePlayer;
						if (player == null)
							player = client.Player;

						byte level = Convert.ToByte(args[2]);

						if (level > GamePlayer.ML_MAX_LEVEL) level = GamePlayer.ML_MAX_LEVEL;

						player.MLLevel = level;
						player.MLExperience = 0;
						player.SaveIntoDatabase();
						player.Out.SendUpdatePlayer();
						player.Out.SendMasterLevelWindow((byte)player.MLLevel);
						client.Out.SendMessage(player.Name + " Master Level is set to " + level + "!", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
						player.Out.SendMessage(client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has set your Master Level to " + level + "!", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
					}
					catch (Exception)
					{
						DisplaySyntax(client);
						return;
					}
					break;

				case "setmlline":

					try
					{
						var player = client.Player.TargetObject as GamePlayer;
						if (player == null)
							player = client.Player;

						byte line = Convert.ToByte(args[2]);

						if (line > 1) line = 1;

						player.MLLine = line;
						player.SaveIntoDatabase();
						player.RefreshSpecDependantSkills(true);
						player.Out.SendUpdatePlayerSkills();
						player.Out.SendUpdatePlayer();
						player.Out.SendMasterLevelWindow((byte)player.MLLevel);
						client.Out.SendMessage(player.Name + " Master Line is set to " + line + "!", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
						player.Out.SendMessage(client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has set your Master Line to " + line + "!", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
					}
					catch (Exception)
					{
						DisplaySyntax(client);
						return;
					}
					break;

				case "setmlstep":

					try
					{
						var player = client.Player.TargetObject as GamePlayer;
						if (player == null)
							player = client.Player;

						if (player.MLLevel == GamePlayer.ML_MAX_LEVEL)
						{
							client.Out.SendMessage(player.Name + " has already finished all Master Levels!", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
							return;
						}

						byte level = Convert.ToByte(args[2]);
						if (level > GamePlayer.ML_MAX_LEVEL)
						{
							client.Out.SendMessage("Valid levels are 0 - " + GamePlayer.ML_MAX_LEVEL + "!", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
							return;
						}

						// Possible steps per level varies, max appears to be 11
						byte step = Convert.ToByte(args[3]);

						bool setFinished = true;
						if (args.Length > 4)
						{
							setFinished = Convert.ToBoolean(args[4]);
						}

						if (setFinished && player.HasFinishedMLStep(player.MLLevel + 1, step))
						{
							client.Out.SendMessage(player.Name + " has already finished step " + step + " for Master Level " + (player.MLLevel + 1) + "!", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
						}
						else if (setFinished == false && player.HasFinishedMLStep(player.MLLevel + 1, step) == false)
						{
							client.Out.SendMessage(player.Name + " has not yet finished step " + step + " for Master Level " + (player.MLLevel + 1) + "!", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
						}
						else
						{
							player.SetFinishedMLStep(player.MLLevel + 1, step, setFinished);
							if (setFinished)
							{
								client.Out.SendMessage(player.Name + " has now finished step " + step + " for Master Level " + (player.MLLevel + 1) + "!", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
								player.Out.SendMessage(client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has set step " + step + " completed for Master Level " + (player.MLLevel + 1) + "!", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
							}
							else
							{
								client.Out.SendMessage(player.Name + " has no longer finished step " + step + " for Master Level " + (player.MLLevel + 1) + "!", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
								player.Out.SendMessage(client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has set step " + step + " as unfinished for Master Level " + (player.MLLevel + 1) + "!", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
							}
							player.SaveIntoDatabase();
							player.Out.SendMasterLevelWindow(level);
							player.Out.SendUpdatePlayer();
						}
					}
					catch (Exception)
					{
						DisplaySyntax(client);
						return;
					}
					break;

				#endregion Master Levels

                #region realm

                case "realm":
                    {
                        try
                        {
                            byte newRealm = Convert.ToByte(args[2]);
                            var player = client.Player.TargetObject as GamePlayer;

                            if (args.Length != 3)
                            {
                                DisplaySyntax(client);
                                return;
                            }

                            if (player == null)
                                player = client.Player;

                            if (newRealm < 0 || newRealm > 3)
                            {
                                client.Out.SendMessage(player.Name + "'s realm can only be set to numbers 0-3!", EChatType.CT_Important,
                                                       EChatLoc.CL_SystemWindow);
                                return;
                            }

                            player.Realm = (ERealm)newRealm;

                            client.Out.SendMessage(
                                "You successfully changed " + player.Name + "'s realm to " + GlobalConstants.RealmToName((ERealm)newRealm) +
                                "!", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                            player.Out.SendMessage(
                                client.Player.Name + " has changed your realm to " + GlobalConstants.RealmToName((ERealm)newRealm) + "!",
                                EChatType.CT_Important, EChatLoc.CL_SystemWindow);

                            player.Out.SendUpdatePlayer();
                            player.SaveIntoDatabase();
                        }

                        catch (Exception)
                        {
                            DisplaySyntax(client);
                            return;
                        }
                    }
                    break;

                #endregion

                #region model

                case "model":
                    {
                        var player = client.Player.TargetObject as GamePlayer;

                        try
                        {
                            if (args.Length > 4)
                            {
                                DisplaySyntax(client);
                                return;
                            }

                            if (player == null)
                                player = client.Player;


                            switch (args[2])
                            {
                                case "reset":
                                    {
                                        player.Model = (ushort)player.Client.Account.Characters[player.Client.ActiveCharIndex].CreationModel;
                                        client.Out.SendMessage("You changed " + player.Name + " back to his or her original model successfully!",
                                                               EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                                        player.Out.SendMessage(
                                            client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel +
                                            ") has changed your model back to its original creation model!", EChatType.CT_Important,
                                            EChatLoc.CL_SystemWindow);
                                        player.Out.SendUpdatePlayer();
                                        player.SaveIntoDatabase();
                                    }
                                    break;

                                default:
                                    {
                                        ushort modelID = 0;
                                        int modelIndex = 0;

                                        if (args[2] == "change")
                                            modelIndex = 3;
                                        else
                                            modelIndex = 2;

                                        if (ushort.TryParse(args[modelIndex], out modelID) == false)
                                        {
                                            DisplaySyntax(client, args[1]);
                                            return;
                                        }

                                        player.Model = modelID;
                                        client.Out.SendMessage("You successfully changed " + player.Name + "'s form! (ID:#" + modelID + ")",
                                                               EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                                        player.Out.SendMessage(
                                            client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has changed your form! (ID:#" + modelID +
                                            ")", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                                        player.Out.SendUpdatePlayer();
                                        player.SaveIntoDatabase();
                                    }
                                    break;
                            }
                        }
                        catch (Exception)
                        {
                            DisplaySyntax(client);
                            return;
                        }
                    }
                    break;

                #endregion

                #region money

                case "money":
                    {
                        var player = client.Player.TargetObject as GamePlayer;

                        try
                        {
                            if (args.Length != 4)
                            {
                                DisplaySyntax(client);
                                return;
                            }

                            if (player == null)
                                player = client.Player;

                            switch (args[2])
                            {
                                case "copp":
                                    {
                                        long amount = long.Parse(args[3]);
                                        player.AddMoney(amount);
                                        InventoryLogging.LogInventoryAction(client.Player, player, eInventoryActionType.Other, amount);
                                        client.Out.SendMessage("You gave " + player.Name + " copper successfully!", EChatType.CT_Important,
                                                               EChatLoc.CL_SystemWindow);
                                        player.Out.SendMessage(
                                            client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has given you some copper!",
                                            EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                                        return;
                                    }


                                case "silv":
                                    {
                                        long amount = long.Parse(args[3]) * 100;
                                        player.AddMoney(amount);
                                        InventoryLogging.LogInventoryAction(client.Player, player, eInventoryActionType.Other, amount);
                                        client.Out.SendMessage("You gave " + player.Name + " silver successfully!", EChatType.CT_Important,
                                                               EChatLoc.CL_SystemWindow);
                                        player.Out.SendMessage(
                                            client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has given you some silver!",
                                            EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                                        return;
                                    }

                                case "gold":
                                    {
                                        long amount = long.Parse(args[3]) * 100 * 100;
                                        player.AddMoney(amount);
                                        InventoryLogging.LogInventoryAction(client.Player, player, eInventoryActionType.Other, amount);
                                        client.Out.SendMessage("You gave " + player.Name + " gold successfully!", EChatType.CT_Important,
                                                               EChatLoc.CL_SystemWindow);
                                        player.Out.SendMessage(
                                            client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has given you some gold!",
                                            EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                                        return;
                                    }

                                case "plat":
                                    {
                                        long amount = long.Parse(args[3]) * 100 * 100 * 1000;
                                        player.AddMoney(amount);
                                        InventoryLogging.LogInventoryAction(client.Player, player, eInventoryActionType.Other, amount);
                                        client.Out.SendMessage("You gave " + player.Name + " platinum successfully!", EChatType.CT_Important,
                                                               EChatLoc.CL_SystemWindow);
                                        player.Out.SendMessage(
                                            client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has given you some platinum!",
                                            EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                                        return;
                                    }

                                case "mith":
                                    {
                                        long amount = long.Parse(args[3]) * 100 * 100 * 1000 * 1000;
                                        player.AddMoney(amount);
                                        InventoryLogging.LogInventoryAction(client.Player, player, eInventoryActionType.Other, amount);
                                        client.Out.SendMessage("You gave " + player.Name + " mithril successfully!", EChatType.CT_Important,
                                                               EChatLoc.CL_SystemWindow);
                                        player.Out.SendMessage(
                                            client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has given you some mithril!",
                                            EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                                        return;
                                    }
                            }
                            player.Out.SendUpdatePlayer();
                            player.SaveIntoDatabase();
                        }

                        catch (Exception)
                        {
                            DisplaySyntax(client);
                            return;
                        }
                    }
                    break;

                #endregion

                #region points

                case "rps":
                    {
                        var player = client.Player.TargetObject as GamePlayer;
                        try
                        {
                            if (args.Length != 3)
                            {
                                DisplaySyntax(client);
                                return;
                            }

                            if (player == null)
                                player = client.Player;

                            long amount = long.Parse(args[2]);
                            player.GainRealmPoints(amount, false);
                            client.Out.SendMessage("You gave " + player.Name + " " + amount + " realmpoints succesfully!",
                                                   EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                            player.Out.SendMessage(
                                client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has given you " + amount + " realmpoints!",
                                EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                            player.SaveIntoDatabase();
                            player.Out.SendUpdatePlayer();
                        }
                        catch (Exception)
                        {
                            DisplaySyntax(client);
                            return;
                        }
                    }
                    break;


                case "xp":
                case "xpa":
                    {
                        var player = client.Player.TargetObject as GamePlayer;
                        try
                        {
                            if (args.Length != 3)
                            {
                                DisplaySyntax(client);
                                return;
                            }

                            if (player == null)
                                player = client.Player;

                            EXpSource xpSource = EXpSource.Other;
                            if (args[1].ToLower() == "xpa")
                            {
                                xpSource = EXpSource.NPC;
                            }

                            long amount = long.Parse(args[2]);
                            player.GainExperience(xpSource, amount, false);
                            client.Out.SendMessage("You gave " + player.Name + " " + amount + " experience succesfully!",
                                                   EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                            player.Out.SendMessage(
                                client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has given you " + amount + " experience!",
                                EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                            player.SaveIntoDatabase();
                            player.Out.SendUpdatePlayer();
                        }
                        catch (Exception)
                        {
                            DisplaySyntax(client);
                            return;
                        }
                    }
                    break;

                case "clxp":
                    {
                        var player = client.Player.TargetObject as GamePlayer;
                        try
                        {
                            if (args.Length != 3)
                            {
                                DisplaySyntax(client);
                                return;
                            }

                            if (player == null)
                                player = client.Player;

                            long amount = long.Parse(args[2]);
                            player.GainChampionExperience(amount, EXpSource.GM);
                            client.Out.SendMessage("You gave " + player.Name + " " + amount + " Champion experience succesfully!", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                            player.Out.SendMessage(client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has given you " + amount + " Champion experience!", EChatType.CT_Important, EChatLoc.CL_SystemWindow);

							// now see if player gained any CL and level them up
							bool gainedLevel = false;
							while (player.ChampionLevel < player.ChampionMaxLevel && player.ChampionExperience >= player.ChampionExperienceForNextLevel)
							{
								player.ChampionLevelUp();
								gainedLevel = true;
							}

							if (gainedLevel)
							{
								player.Out.SendMessage("You reached champion level " + player.ChampionLevel + "!", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
							}


                            player.SaveIntoDatabase();
                            player.Out.SendUpdatePlayer();
                        }
                        catch (Exception)
                        {
                            DisplaySyntax(client);
                            return;
                        }
                    }
                    break;

				case "mlxp":
					{
						var player = client.Player.TargetObject as GamePlayer;
						try
						{
							if (args.Length != 3)
							{
								DisplaySyntax(client);
								return;
							}

							if (player == null)
								player = client.Player;

							// WIP, For the moment it simply sets MLExperience - Tolakram

							long amount = long.Parse(args[2]);

							player.MLExperience += amount;
							client.Out.SendMessage("You gave " + player.Name + " " + amount + " ML experience succesfully!", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
							player.Out.SendMessage(client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has given you " + amount + " ML experience!", EChatType.CT_Important, EChatLoc.CL_SystemWindow);

							if (player.MLExperience > player.GetMLExperienceForLevel(player.MLLevel + 1))
							{
								player.MLExperience = player.GetMLExperienceForLevel(player.MLLevel + 1);
							}

							if (player.MLExperience < 0)
							{
								player.MLExperience = 0;
							}

							player.SaveIntoDatabase();
							player.Out.SendUpdatePlayer();
							player.Out.SendMasterLevelWindow((byte)player.MLLevel);
						}
						catch (Exception)
						{
							DisplaySyntax(client);
							return;
						}
					}
					break;

                case "bps":
                    {
                        var player = client.Player.TargetObject as GamePlayer;
                        try
                        {
                            if (args.Length != 3)
                            {
                                DisplaySyntax(client);
                                return;
                            }

                            if (player == null)
                                player = client.Player;

                            long amount = long.Parse(args[2]);
                            player.GainBountyPoints(amount, false);
                            client.Out.SendMessage("You gave " + player.Name + " " + amount + " bountypoints succesfully!",
                                                   EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                            player.Out.SendMessage(
                                client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has given you " + amount + " bountypoints!",
                                EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                            player.SaveIntoDatabase();
                            player.Out.SendUpdatePlayer();
                        }
                        catch (Exception)
                        {
                            DisplaySyntax(client);
                            return;
                        }
                    }
                    break;

                #endregion

                #region stat

                case "stat":
                    {
                        var player = client.Player.TargetObject as GamePlayer;

                        try
                        {
                            short value = Convert.ToInt16(args[3]);

                            if (args.Length != 4)
                            {
                                DisplaySyntax(client);
                                return;
                            }

                            if (player == null)
                                player = client.Player;

                            switch (args[2])
                            {
                                /*1*/
                                case "dex":
                                    {
                                        player.ChangeBaseStat(EStat.DEX, value);
                                        player.Out.SendMessage(
                                            client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has given you " + value + " dexterity!",
                                            EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                                        client.Out.SendMessage("You gave " + player.Name + " " + value + " dexterity successfully!",
                                                               EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                                    }
                                    break;

                                /*2*/
                                case "str":
                                    {
                                        player.ChangeBaseStat(EStat.STR, value);
                                        player.Out.SendMessage(
                                            client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has given you " + value + " strength!",
                                            EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                                        client.Out.SendMessage("You gave " + player.Name + " " + value + " strength successfully!",
                                                               EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                                    }
                                    break;

                                /*3*/
                                case "con":
                                    {
                                        player.ChangeBaseStat(EStat.CON, value);
                                        player.Out.SendMessage(
                                            client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has given you " + value +
                                            " consititution!", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                                        client.Out.SendMessage("You gave " + player.Name + " " + value + " constitution successfully!",
                                                               EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                                    }
                                    break;

                                /*4*/
                                case "emp":
                                    {
                                        player.ChangeBaseStat(EStat.EMP, value);
                                        player.Out.SendMessage(
                                            client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has given you " + value + " empathy!",
                                            EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                                        client.Out.SendMessage("You gave " + player.Name + " " + value + " empathy successfully!",
                                                               EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                                    }
                                    break;

                                /*5*/
                                case "int":
                                    {
                                        player.ChangeBaseStat(EStat.INT, value);
                                        player.Out.SendMessage(
                                            client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has given you " + value +
                                            " intelligence!", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                                        client.Out.SendMessage("You gave " + player.Name + " " + value + " intelligence successfully!",
                                                               EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                                    }
                                    break;

                                /*6*/
                                case "pie":
                                    {
                                        player.ChangeBaseStat(EStat.PIE, value);
                                        player.Out.SendMessage(
                                            client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has given you " + value + " piety!",
                                            EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                                        client.Out.SendMessage("You gave " + player.Name + " " + value + " piety successfully!",
                                                               EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                                    }
                                    break;

                                /*7*/
                                case "qui":
                                    {
                                        player.ChangeBaseStat(EStat.QUI, value);
                                        player.Out.SendMessage(
                                            client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has given you " + value + " quickness!",
                                            EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                                        client.Out.SendMessage("You gave " + player.Name + " " + value + " quickness successfully!",
                                                               EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                                    }
                                    break;

                                /*8*/
                                case "cha":
                                    {
                                        player.ChangeBaseStat(EStat.CHR, value);
                                        player.Out.SendMessage(
                                            client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has given you " + value + " charisma!",
                                            EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                                        client.Out.SendMessage("You gave " + player.Name + " " + value + " charisma successfully!",
                                                               EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                                    }
                                    break;

                                /*all*/
                                case "all":
                                    {
                                        player.ChangeBaseStat(EStat.CHR, value); //1
                                        player.ChangeBaseStat(EStat.QUI, value); //2
                                        player.ChangeBaseStat(EStat.INT, value); //3
                                        player.ChangeBaseStat(EStat.PIE, value); //4
                                        player.ChangeBaseStat(EStat.EMP, value); //5
                                        player.ChangeBaseStat(EStat.CON, value); //6
                                        player.ChangeBaseStat(EStat.STR, value); //7
                                        player.ChangeBaseStat(EStat.DEX, value); //8
                                        player.Out.SendMessage(
                                            client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has given you " + value +
                                            " to all stats!", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                                        client.Out.SendMessage("You gave " + player.Name + " " + value + " to all stats successfully!",
                                                               EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                                    }
                                    break;

                                default:
                                    {
                                        client.Out.SendMessage("Try using: dex, str, con, emp, int, pie, qui, cha, or all as a type of stat.",
                                                               EChatType.CT_System, EChatLoc.CL_SystemWindow);
                                    }
                                    break;
                            }

                            player.Out.SendCharStatsUpdate();
                            player.Out.SendUpdatePlayer();
                            player.SaveIntoDatabase();
                        }
                        catch (Exception)
                        {
                            DisplaySyntax(client);
                            return;
                        }
                    }
                    break;

                #endregion

                #region friend

                case "friend":
                    {
                        var player = client.Player.TargetObject as GamePlayer;

                        if (args.Length != 3)
                        {
                            DisplaySyntax(client);
                            return;
                        }

                        if (player == null)
                        {
                            client.Out.SendMessage("You need a valid target!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
                            return;
                        }

                        if (args[2] == "list")
                        {
                            string[] list = player.SerializedFriendsList;
                            client.Out.SendCustomTextWindow(player.Name + "'s Friend List", list);
                            return;
                        }

                        string name = string.Join(" ", args, 2, args.Length - 2);

                        int result = 0;
                        GameClient fclient = WorldMgr.GuessClientByPlayerNameAndRealm(name, 0, true, out result);
                        if (fclient != null && !GameServer.ServerRules.IsSameRealm(fclient.Player, player.Client.Player, true))
                        {
                            fclient = null;
                        }

                        if (fclient == null)
                        {
                            name = args[2];
                            if (player.GetFriends().Contains(name) && player.RemoveFriend(name))
                            {
                                player.Out.SendMessage(
                                    client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has removed " + player.Name +
                                    " from your friend list!", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                                client.Out.SendMessage("Removed " + name + " from " + player.Name + "'s friend list successfully!",
                                                       EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                                return;
                            }
                            else
                            {
                                // nothing found
                                client.Out.SendMessage("No players online with name " + name + ".", EChatType.CT_Important,
                                                       EChatLoc.CL_SystemWindow);
                                return;
                            }
                        }

                        switch (result)
                        {
                            case 2: // name not unique
                                client.Out.SendMessage("Character name is not unique.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
                                return;
                            case 3: // exact match
                            case 4: // guessed name
                                if (fclient == player.Client)
                                {
                                    client.Out.SendMessage("You can't add that player to his or her own friend list!", EChatType.CT_Important,
                                                           EChatLoc.CL_SystemWindow);
                                    return;
                                }

                                name = fclient.Player.Name;
                                if (player.GetFriends().Contains(name) && player.RemoveFriend(name))
                                {
                                    player.Out.SendMessage(
                                        client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has removed " + name +
                                        " from your friend list!", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                                    client.Out.SendMessage("Removed " + name + " from " + player.Name + "'s friend list successfully!",
                                                           EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                                }
                                else if (player.AddFriend(name))
                                {
                                    player.Out.SendMessage(
                                        client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has added " + name +
                                        " to your friend list!", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                                    client.Out.SendMessage("Added " + name + " to " + player.Name + "'s friend list successfully!",
                                                           EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                                }
                                return;
                        }
                        player.Out.SendUpdatePlayer();
                        player.SaveIntoDatabase();
                    }
                    break;

                #endregion

                #region respec

                case "respec":
                    {
                        var player = client.Player.TargetObject as GamePlayer;

                        if (args.Length < 2 || args.Length > 4)
                        {
                            DisplaySyntax(client);
                            return;
                        }

                        if (player == null)
                            player = client.Player;

                        int amount = 1;
                        if (args.Length == 4)
                        {
                            try
                            {
                                amount = Convert.ToInt32(args[3]);
                            }
                            catch
                            {
                                amount = 1;
                            }
                        }

                        switch (args[2])
                        {
                            case "line":
                                {
                                    player.RespecAmountSingleSkill += amount;
                                    player.Client.Out.SendMessage(
                                        client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has awarded you " + amount +
                                        " single respec!", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                                    client.Out.SendMessage(amount + " single respec given successfully to " + player.Name + "!",
                                                           EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                                    break;
                                }
                            case "all":
                                {
                                    player.RespecAmountAllSkill += amount;
                                    player.Client.Out.SendMessage(
                                        client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has awarded you " + amount +
                                        " full respec!", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                                    client.Out.SendMessage(amount + " full respec given successfully to " + player.Name + "!",
                                                           EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                                    break;
                                }
                            case "realm":
                                {
                                    player.RespecAmountRealmSkill += amount;
                                    player.Client.Out.SendMessage(
                                        client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has awarded you " + amount +
                                        " realm respec!", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                                    client.Out.SendMessage(amount + " realm respec given successfully to " + player.Name + "!",
                                                           EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                                    break;
                                }
                            case "dol":
                                {
                                    player.RespecAmountDOL += amount;
                                    player.Client.Out.SendMessage(
                                        client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has awarded you " + amount +
                                        " DOL (full) respec!", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                                    client.Out.SendMessage(amount + " DOL (full) respec given successfully to " + player.Name + "!",
                                                           EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                                    break;
                                }
                            case "champion":
                                {
                                    player.RespecAmountChampionSkill += amount;
                                    player.Client.Out.SendMessage(
                                        client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has awarded you " + amount +
                                        " Champion respec!", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                                    client.Out.SendMessage(amount + " champion respec given successfully to " + player.Name + "!",
                                                           EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                                    break;
                                }
                            /*case "ml":
                            {
                                //
                                player.Client.Out.SendMessage(client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has awarded you an ML respec!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                                client.Out.SendMessage("ML respec given successfully to " + player.Name + "!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                                break;
                            }*/
                        }

                        player.Client.Player.SaveIntoDatabase();
                    }
                    break;

                #endregion

                #region realm

                case "purge":
                    {
                        var player = client.Player.TargetObject as GamePlayer;
                        bool m_hasEffect;

                        if (args.Length != 2)
                        {
                            DisplaySyntax(client);
                            return;
                        }

                        if (player == null)
                        {
                            client.Out.SendMessage("You need a valid target!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
                            return;
                        }

                        m_hasEffect = false;

                        lock (player.EffectList)
                        {
                            foreach (GameSpellEffect effect in player.EffectList)
                            {
                                if (!effect.SpellHandler.HasPositiveEffect)
                                {
                                    m_hasEffect = true;
                                    break;
                                }
                            }
                        }

                        if (!m_hasEffect)
                        {
                            SendResistEffect(player);
                            return;
                        }

                        lock (player.EffectList)
                        {
                            foreach (GameSpellEffect effect in player.EffectList)
                            {
                                if (!effect.SpellHandler.HasPositiveEffect)
                                {
                                    effect.Cancel(false);
                                }
                            }
                        }
                    }
                    break;

                #endregion

                #region save

                case "save":
                    {
                        var player = client.Player.TargetObject as GamePlayer;

                        if (args.Length > 3 || args.Length < 2)
                        {
                            DisplaySyntax(client);
                            return;
                        }

                        if (player == null && args.Length == 2)
                        {
                            client.Out.SendMessage("You need a valid target!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
                            return;
                        }

                        if (args.Length == 2 && player != null)
                        {
                            player.Out.SendMessage(
                                client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has saved your character.",
                                EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                            client.Out.SendMessage(player.Name + " saved successfully!", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                            player.SaveIntoDatabase();
                        }

                        if (args.Length == 3)
                        {
                            switch (args[2])
                            {
                                case "all":
                                    {
                                        foreach (GameClient c in WorldMgr.GetAllPlayingClients())
                                        {
                                            if (c != null && c.Player != null) c.Player.SaveIntoDatabase();
                                        }
                                        client.Out.SendMessage("Saved all characters!", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                                    }
                                    break;

                                default:
                                    {
                                        DisplaySyntax(client);
                                        return;
                                    }
                            }
                        }
                    }
                    break;

                #endregion

                #region kick

                case "kick":
                    {
                        var player = client.Player.TargetObject as GamePlayer;

                        if (args.Length > 3 || args.Length < 2)
                        {
                            DisplaySyntax(client);
                            return;
                        }

                        if (player == null && args.Length == 2)
                        {
                            client.Out.SendMessage("You need a valid target!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
                            return;
                        }

                        if (args.Length == 2 && player != null)
                        {
                            if (player.Client.Account.PrivLevel > 1)
                            {
                                client.Out.SendMessage(
                                    "Please use /kick <name> to kick Gamemasters. This is used to prevent accidental kicks.",
                                    EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                                return;
                            }
                            player.Client.Out.SendPlayerQuit(true);
                            player.Client.Player.SaveIntoDatabase();
                            player.Client.Player.Quit(true);
                            return;
                        }

                        if (args.Length == 3)
                        {
                            switch (args[2])
                            {
                                case "all":
                                    {
                                        foreach (GameClient allplayer in WorldMgr.GetAllPlayingClients())
                                        {
                                            if (allplayer.Account.PrivLevel == 1)
                                            {
                                                allplayer.Out.SendMessage(
                                                    client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has kicked all players!",
                                                    EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                                                allplayer.Out.SendPlayerQuit(true);
                                                allplayer.Player.SaveIntoDatabase();
                                                allplayer.Player.Quit(true);
                                                return;
                                            }
                                        }
                                    }
                                    break;

                                default:
                                    {
                                        DisplaySyntax(client);
                                        return;
                                    }
                            }
                        }
                    }
                    break;

                #endregion

                #region rez kill

                case "rez":
                    {
                        var player = client.Player.TargetObject as GamePlayer;

                        if (args.Length > 3 || args.Length < 2)
                        {
                            DisplaySyntax(client);
                            return;
                        }

                        if (player == null && args.Length == 2)
                        {
                            client.Out.SendMessage("You need a valid target!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
                            return;
                        }

                        if (args.Length == 2 && player != null)
                        {
                            if (!(player.IsAlive))
                            {
                                player.Health = player.MaxHealth;
                                player.Mana = player.MaxMana;
                                player.Endurance = player.MaxEndurance;
                                player.MoveTo(client.Player.CurrentRegionID, client.Player.X, client.Player.Y, client.Player.Z,
                                              client.Player.Heading);

                                client.Out.SendMessage("You resurrected " + player.Name + " successfully!", EChatType.CT_Important,
                                                       EChatLoc.CL_SystemWindow);
                                //player.Out.SendMessage(client.Player.Name +" has resurrected you!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);

                                player.StopReleaseTimer();
                                player.Out.SendPlayerRevive(player);
                                player.Out.SendStatusUpdate();
                                player.Out.SendMessage("You have been resurrected by " + client.Player.GetName(0, false) + "!",
                                                       EChatType.CT_System, EChatLoc.CL_SystemWindow);
                                player.Notify(GamePlayerEvent.Revive, player);
                            }
                            else
                            {
                                client.Out.SendMessage("Player is not dead!", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                                return;
                            }
                        }

                        if (args.Length >= 3)
                        {
                            switch (args[2])
                            {
                                case "albs":
                                    {
                                        foreach (GameClient aplayer in WorldMgr.GetClientsOfRealm(ERealm.Albion))
                                        {
                                            if (!(aplayer.Player.IsAlive))
                                            {
                                                aplayer.Player.Health = aplayer.Player.MaxHealth;
                                                aplayer.Player.Mana = aplayer.Player.MaxMana;
                                                aplayer.Player.Endurance = aplayer.Player.MaxEndurance;
                                                aplayer.Player.MoveTo(client.Player.CurrentRegionID, client.Player.X, client.Player.Y, client.Player.Z,
                                                                      client.Player.Heading);

                                                aplayer.Player.StopReleaseTimer();
                                                aplayer.Player.Out.SendPlayerRevive(aplayer.Player);
                                                aplayer.Player.Out.SendStatusUpdate();
                                                aplayer.Player.Out.SendMessage("You have been resurrected by " + client.Player.GetName(0, false) + "!",
                                                                               EChatType.CT_System, EChatLoc.CL_SystemWindow);
                                                aplayer.Player.Notify(GamePlayerEvent.Revive, aplayer.Player);
                                            }
                                        }
                                    }
                                    break;

                                case "hibs":
                                    {
                                        foreach (GameClient hplayer in WorldMgr.GetClientsOfRealm(ERealm.Hibernia))
                                        {
                                            if (!(hplayer.Player.IsAlive))
                                            {
                                                hplayer.Player.Health = hplayer.Player.MaxHealth;
                                                hplayer.Player.Mana = hplayer.Player.MaxMana;
                                                hplayer.Player.Endurance = hplayer.Player.MaxEndurance;
                                                hplayer.Player.MoveTo(client.Player.CurrentRegionID, client.Player.X, client.Player.Y, client.Player.Z,
                                                                      client.Player.Heading);

                                                hplayer.Player.StopReleaseTimer();
                                                hplayer.Player.Out.SendPlayerRevive(hplayer.Player);
                                                hplayer.Player.Out.SendStatusUpdate();
                                                hplayer.Player.Out.SendMessage("You have been resurrected by " + client.Player.GetName(0, false) + "!",
                                                                               EChatType.CT_System, EChatLoc.CL_SystemWindow);
                                                hplayer.Player.Notify(GamePlayerEvent.Revive, hplayer.Player);
                                            }
                                        }
                                    }
                                    break;

                                case "mids":
                                    {
                                        foreach (GameClient mplayer in WorldMgr.GetClientsOfRealm(ERealm.Midgard))
                                        {
                                            if (!(mplayer.Player.IsAlive))
                                            {
                                                mplayer.Player.Health = mplayer.Player.MaxHealth;
                                                mplayer.Player.Mana = mplayer.Player.MaxMana;
                                                mplayer.Player.Endurance = mplayer.Player.MaxEndurance;
                                                mplayer.Player.MoveTo(client.Player.CurrentRegionID, client.Player.X, client.Player.Y, client.Player.Z,
                                                                      client.Player.Heading);

                                                mplayer.Player.StopReleaseTimer();
                                                mplayer.Player.Out.SendPlayerRevive(mplayer.Player);
                                                mplayer.Player.Out.SendStatusUpdate();
                                                mplayer.Player.Out.SendMessage("You have been resurrected by " + client.Player.GetName(0, false) + "!",
                                                                               EChatType.CT_System, EChatLoc.CL_SystemWindow);
                                                mplayer.Player.Notify(GamePlayerEvent.Revive, mplayer.Player);
                                            }
                                        }
                                    }
                                    break;

                                case "self":
                                    {
                                        GamePlayer selfplayer = client.Player;

                                        if (!(selfplayer.IsAlive))
                                        {
                                            selfplayer.Health = selfplayer.MaxHealth;
                                            selfplayer.Mana = selfplayer.MaxMana;
                                            selfplayer.Endurance = selfplayer.MaxEndurance;
                                            selfplayer.MoveTo(client.Player.CurrentRegionID, client.Player.X, client.Player.Y, client.Player.Z,
                                                              client.Player.Heading);

                                            selfplayer.Out.SendMessage("You revive yourself.", EChatType.CT_Important, EChatLoc.CL_SystemWindow);

                                            selfplayer.StopReleaseTimer();
                                            selfplayer.Out.SendPlayerRevive(selfplayer);
                                            selfplayer.Out.SendStatusUpdate();
                                            selfplayer.Notify(GamePlayerEvent.Revive, selfplayer);
                                        }
                                        else
                                        {
                                            client.Out.SendMessage("You are not dead!", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                                            return;
                                        }
                                    }
                                    break;

                                case "all":
                                    {
                                        foreach (GameClient allplayer in WorldMgr.GetAllPlayingClients())
                                        {
                                            if (!(allplayer.Player.IsAlive))
                                            {
                                                allplayer.Player.Health = allplayer.Player.MaxHealth;
                                                allplayer.Player.Mana = allplayer.Player.MaxMana;
                                                allplayer.Player.Endurance = allplayer.Player.MaxEndurance;
                                                allplayer.Player.MoveTo(client.Player.CurrentRegionID, client.Player.X, client.Player.Y, client.Player.Z,
                                                                        client.Player.Heading);

                                                allplayer.Player.StopReleaseTimer();
                                                allplayer.Player.Out.SendPlayerRevive(allplayer.Player);
                                                allplayer.Player.Out.SendStatusUpdate();
                                                allplayer.Player.Out.SendMessage("You have been resurrected by " + client.Player.GetName(0, false) + "!",
                                                                                 EChatType.CT_System, EChatLoc.CL_SystemWindow);
                                                allplayer.Player.Notify(GamePlayerEvent.Revive, allplayer.Player);
                                            }
                                        }
                                    }
                                    break;

                                default:
                                    {
                                        client.Out.SendMessage("SYNTAX: /player rez <albs|mids|hibs|all>", EChatType.CT_System,
                                                               EChatLoc.CL_SystemWindow);
                                    }
                                    break;
                            }
                        }
                    }
                    break;

                case "kill":
                    {
                        var player = client.Player.TargetObject as GamePlayer;

                        if (args.Length < 2 || args.Length > 3)
                        {
                            DisplaySyntax(client);
                            return;
                        }

                        if (player == null && args.Length == 2)
                        {
                            client.Out.SendMessage("You need a valid target!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
                            return;
                        }

                        if (args.Length == 2 && player != null)
                        {
                            if (player.Client.Account.PrivLevel > 1)
                            {
                                client.Out.SendMessage("This command can not be used on Gamemasters!", EChatType.CT_Important,
                                                       EChatLoc.CL_SystemWindow);
                                return;
                            }

                            if (player.IsAlive)
                            {
                                KillPlayer(client.Player, player);
                                client.Out.SendMessage("You killed " + player.Name + " successfully!", EChatType.CT_Important,
                                                       EChatLoc.CL_SystemWindow);
                                player.Out.SendMessage(client.Player.Name + " has killed you!", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                            }
                            else
                            {
                                client.Out.SendMessage("Player is not alive!", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                                return;
                            }
                        }
                        if (args.Length < 3)
                            return;
                        switch (args[2])
                        {
                            case "albs":
                                {
                                    foreach (GameClient aplayer in WorldMgr.GetClientsOfRealm(ERealm.Albion))
                                    {
                                        if (aplayer.Player.IsAlive && aplayer.Account.PrivLevel == 1)
                                        {
                                            KillPlayer(client.Player, aplayer.Player);
                                        }
                                    }
                                }
                                break;

                            case "mids":
                                {
                                    foreach (GameClient mplayer in WorldMgr.GetClientsOfRealm(ERealm.Midgard))
                                    {
                                        if (mplayer.Player.IsAlive && mplayer.Account.PrivLevel == 1)
                                        {
                                            KillPlayer(client.Player, mplayer.Player);
                                        }
                                    }
                                }
                                break;
                            case "hibs":
                                {
                                    foreach (GameClient hplayer in WorldMgr.GetClientsOfRealm(ERealm.Hibernia))
                                    {
                                        if (hplayer.Player.IsAlive && hplayer.Account.PrivLevel == 1)
                                        {
                                            KillPlayer(client.Player, hplayer.Player);
                                        }
                                    }
                                }
                                break;

                            case "self":
                                {
                                    GamePlayer selfplayer = client.Player;

                                    if (!(selfplayer.IsAlive))
                                    {
                                        client.Out.SendMessage("You are already dead. Use /player rez <self> to resurrect yourself.",
                                                               EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                                        return;
                                    }
                                    else
                                    {
                                        KillPlayer(client.Player, client.Player);
                                        client.Out.SendMessage("Good bye cruel world!", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                                    }
                                }
                                break;

                            case "all":
                                {
                                    foreach (GameClient allplayer in WorldMgr.GetAllPlayingClients())
                                    {
                                        if (allplayer.Player.IsAlive && allplayer.Account.PrivLevel == 1)
                                        {
                                            KillPlayer(client.Player, allplayer.Player);
                                        }
                                    }
                                }
                                break;

                            default:
                                {
                                    client.Out.SendMessage("'" + args[2] + "' is not a valid arguement.", EChatType.CT_Important,
                                                           EChatLoc.CL_SystemWindow);
                                }
                                break;
                        }
                        /*End Switch Statement*/
                    }
                    break;

                #endregion

                #region jump

                case "jump":
                    {
                        if (args.Length < 4)
                        {
                            DisplaySyntax(client);
                            return;
                        }

                        switch (args[2])
                        {
                            case "guild":
                                {
                                    short count = 0;

                                    foreach (GameClient pname in WorldMgr.GetAllPlayingClients())
                                    {
                                        string guild = string.Join(" ", args, 3, args.Length - 3);

                                        if (args[3] == null)
                                        {
                                            DisplaySyntax(client);
                                            return;
                                        }

                                        if (pname.Player.GuildName == guild && guild != "")
                                        {
                                            count++;
                                            pname.Player.MoveTo(client.Player.CurrentRegionID, client.Player.X, client.Player.Y, client.Player.Z,
                                                                client.Player.Heading);
                                        }
                                    }

                                    client.Out.SendMessage(count + " players jumped!", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                                }
                                break;

                            case "group":
                                {
                                    short count = 0;

                                    foreach (GameClient pname in WorldMgr.GetAllPlayingClients())
                                    {
                                        string name = args[3];

                                        if (name == null)
                                        {
                                            DisplaySyntax(client);
                                            return;
                                        }

                                        if (name == pname.Player.Name)
                                        {
                                            foreach (GameLiving groupedplayers in pname.Player.Group.GetMembersInTheGroup())
                                            {
                                                groupedplayers.MoveTo(client.Player.CurrentRegionID, client.Player.X, client.Player.Y, client.Player.Z,
                                                                      client.Player.Heading);
                                                count++;
                                            }
                                        }
                                    }

                                    client.Out.SendMessage(count + " players jumped!", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                                }
                                break;

                            case "cg":
                                {
                                    short count = 0;

                                    foreach (GameClient pname in WorldMgr.GetAllPlayingClients())
                                    {
                                        string name = args[3];

                                        if (name == null)
                                        {
                                            DisplaySyntax(client);
                                            return;
                                        }
                                        var cg = (ChatGroup)pname.Player.TempProperties.getProperty<object>(ChatGroup.CHATGROUP_PROPERTY, null);

                                        if (name == pname.Player.Name)
                                        {
                                            foreach (GamePlayer cgplayers in cg.Members.Keys)
                                            {
                                                cgplayers.MoveTo(client.Player.CurrentRegionID, client.Player.X, client.Player.Y, client.Player.Z,
                                                                 client.Player.Heading);
                                                count++;
                                            }
                                        }
                                    }

                                    client.Out.SendMessage(count + " players jumped!", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                                }
                                break;


                            case "bg":
                                {
                                    short count = 0;

                                    foreach (GameClient pname in WorldMgr.GetAllPlayingClients())
                                    {
                                        string name = args[3];

                                        if (name == null)
                                        {
                                            DisplaySyntax(client);
                                            return;
                                        }
                                        var cg = (BattleGroup)pname.Player.TempProperties.getProperty<object>(BattleGroup.BATTLEGROUP_PROPERTY, null);

                                        if (name == pname.Player.Name)
                                        {
                                            foreach (GamePlayer cgplayers in cg.Members.Keys)
                                            {
                                                cgplayers.MoveTo(client.Player.CurrentRegionID, client.Player.X, client.Player.Y, client.Player.Z,
                                                                 client.Player.Heading);
                                                count++;
                                            }
                                        }
                                    }

                                    client.Out.SendMessage(count + " players jumped!", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                                }
                                break;
                            default:
                                {
                                    client.Out.SendMessage("'" + args[2] + "' is not a valid arguement.", EChatType.CT_Important,
                                                           EChatLoc.CL_SystemWindow);
                                }
                                break;
                        }
                    }
                    break;

                #endregion

                #region update

                case "update":
                    {
                        var player = client.Player.TargetObject as GamePlayer;

                        if (args.Length != 2)
                        {
                            DisplaySyntax(client);
                            return;
                        }

                        if (player == null)
                            player = client.Player;

                        player.Out.SendUpdatePlayer();
                        player.Out.SendCharStatsUpdate();
                        player.Out.SendUpdatePoints();
                        player.Out.SendUpdateMaxSpeed();
                        player.Out.SendStatusUpdate();
                        player.Out.SendCharResistsUpdate();
                        client.Out.SendMessage(player.Name + " updated successfully!", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                    }
                    break;

                #endregion

				#region Saddlebags

				case "saddlebags":
					{
						var player = client.Player.TargetObject as GamePlayer;

						if (args.Length != 3)
						{
							DisplaySyntax(client);
							return;
						}

						if (player == null)
							player = client.Player;

						byte activeBags = 0;

						if (byte.TryParse(args[2], out activeBags))
						{
							if (activeBags <= 0x0F)
							{
								player.ActiveSaddleBags = activeBags;
								player.SaveIntoDatabase();
								client.Player.Out.SendMessage(string.Format("{0}'s active saddlebags set to 0x{1:X2}!", player.Name, player.ActiveSaddleBags), EChatType.CT_Important, EChatLoc.CL_SystemWindow);
								player.Out.SendMessage(string.Format("Your active saddlebags have been set to 0x{0:X2} by {1}!", player.ActiveSaddleBags, client.Player.Name), EChatType.CT_Important, EChatLoc.CL_SystemWindow);
								player.Out.SendSetControlledHorse(player);
							}
							else
							{
								DisplayMessage(client, "Valid saddlebag values are between 0 and 15!");
							}
						}
						else
						{
							DisplaySyntax(client);
						}

					}
					break;

				#endregion Saddlebags

				#region info

				case "info":
                    {
                        var player = client.Player.TargetObject as GamePlayer;

                        if (args.Length != 2)
                        {
                            DisplaySyntax(client);
                            return;
                        }

                        if (player == null)
                            player = client.Player;

                        Show_Info(player, client);
                    }
                    break;

                #endregion

                #region location

                case "location":
                    {
                        var player = client.Player.TargetObject as GamePlayer;

                        client.Out.SendMessage("\"" + player.Name + "\", " +
                                               player.CurrentRegionID + ", " +
                                               player.X + ", " +
                                               player.Y + ", " +
                                               player.Z + ", " +
                                               player.Heading,
                                               EChatType.CT_System, EChatLoc.CL_SystemWindow);
                    }
                    break;

                #endregion

                #region show group - effects

                case "showgroup":
                    {
                        var player = client.Player.TargetObject as GamePlayer;

                        if (args.Length != 2)
                        {
                            DisplaySyntax(client);
                            return;
                        }

                        if (player == null)
                        {
                            client.Out.SendMessage("You need a valid target!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
                            return;
                        }

                        if (player.Group == null)
                        {
                            client.Out.SendMessage("Player does not have a group!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
                            return;
                        }

                        var text = new List<string>();

                        foreach (GamePlayer p in player.Group.GetPlayersInTheGroup())
                        {
                            text.Add(p.Name + " " + p.Level + " " + p.CharacterClass.Name);
                        }

                        client.Out.SendCustomTextWindow("Group Members", text);
                        break;
                    }
                case "showeffects":
                    {
                        var player = client.Player.TargetObject as GamePlayer;

                        if (args.Length != 2)
                        {
                            DisplaySyntax(client);
                            return;
                        }

                        if (player == null)
                        {
                            client.Out.SendMessage("You need a valid target!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
                            return;
                        }

                        var effects = new List<string>();
                        ArrayList positiveEffects = new ArrayList();
                        ArrayList negativeEffects = new ArrayList();

                        if (positiveEffects.Count > 0)
                            positiveEffects.Clear();
                        if (negativeEffects.Count > 0)
                            negativeEffects.Clear();

                        if (player.effectListComponent != null)
                        {

                            foreach (EcsGameSpellEffect e in player.effectListComponent.GetSpellEffects())
                            {
                                if (e.HasPositiveEffect)
                                    positiveEffects.Add(e);
                                if (!e.HasPositiveEffect)
                                    negativeEffects.Add(e);
                            }

                            effects.Add(" ");
                            effects.Add(" - Positive Spell Effects");
                            if (positiveEffects.Count > 0)
                            {
                                // List active spell effects
                                foreach (EcsGameSpellEffect e in positiveEffects)
                                {
                                    var caster = "NONE";
                                    if (e.SpellHandler.Caster.Name != null)
                                    {
                                        caster = e.SpellHandler.Caster.Name;
                                        if (e.SpellHandler.Caster.Name == player.Name)
                                            caster = "SELF";
                                    }

                                    effects.Add(" -- " + e.SpellHandler.Spell.Name + " (" + e.EffectType + ", level " + e.SpellHandler.Spell.Level + "): " + caster + " (Caster), " + (e.GetRemainingTimeForClient() / 1000) + " seconds remaining");
                                }
                            }

                            effects.Add(" ");
                            effects.Add(" - Negative Spell Effects");
                            if (negativeEffects.Count > 0)
                            {
                                // List active spell effects
                                foreach (EcsGameSpellEffect e in negativeEffects)
                                {
                                    var caster = "NONE";
                                    if (e.SpellHandler.Caster.Name != null)
                                    {
                                        caster = e.SpellHandler.Caster.Name;
                                        if (e.SpellHandler.Caster.Name == player.Name)
                                            caster = "SELF";
                                    }

                                    effects.Add(" -- " + e.SpellHandler.Spell.Name + " (" + e.EffectType + ", level " + e.SpellHandler.Spell.Level + "): " + caster + " (Caster), " + (e.GetRemainingTimeForClient() / 1000) + " seconds remaining");
                                }
                            }
                        }
                        client.Out.SendCustomTextWindow("Player Effects", effects);
                        break;
                    }

                #endregion

                #region inventory

                case "inventory":
                    {
                        var player = client.Player.TargetObject as GamePlayer;

                        if (player == null)
                        {
                            client.Out.SendMessage("You need a valid target!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
                            return;
                        }

                        if (args.Length == 2)
                        {
                            Show_Inventory(player, client, "");
                            return;
                        }
                        else if (args.Length == 3)
                        {
                            Show_Inventory(player, client, args[2].ToLower());
                            return;
                        }

                        DisplaySyntax(client);
                        break;
                    }

                #endregion

                #region allcharacters

                case "allchars":
                    {
                        var targetPlayer = client.Player.TargetObject as GamePlayer;
                        GameClient targetClient = targetPlayer == null ? null : targetPlayer.Client;

                        if (args.Length > 2)
                        {
                            targetClient = WorldMgr.GetClientByPlayerName(args[2], true, false);
                        }

                        if (targetClient == null)
                        {
                            DisplaySyntax(client, args[1]);
                            return;
                        }
                        else
                        {
                            string characterNames = string.Empty;

                            foreach (DbCoreCharacters acctChar in targetClient.Account.Characters)
                            {
                                if (acctChar != null)
                                    characterNames += acctChar.Name + " " + acctChar.LastName + "\n";
                            }

                            client.Out.SendMessage(characterNames, EChatType.CT_Say, EChatLoc.CL_PopupWindow);
                        }
                    }
                    break;

                #endregion allcharacters

                #region class
                case "class":
                    {
                        var targetPlayer = client.Player.TargetObject as GamePlayer;
                        GameClient targetClient = targetPlayer == null ? null : targetPlayer.Client;

                        if (args.Length < 3)
                        {
                            DisplayMessage(client, "/player class <list|classID|className>");
                            return;
                        }

                        switch (args[2])
                        {
                            case "list":
                                {
                                    IList<string> classList = new List<string>();

                                    foreach (ECharacterClass cl in Enum.GetValues(typeof(ECharacterClass)))
                                    {
                                        classList.Add(Enum.GetName(typeof(ECharacterClass), cl) + " - " + (int)cl);
                                    }

                                    client.Player.Out.SendCustomTextWindow("[Class IDs List]", classList);
                                }
                                break;
                            default:
                                {
                                    if (targetPlayer == null)
                                    {
                                        DisplayMessage(client, "You must have a player target to use this command!");
                                        return;
                                    }

                                    if (int.TryParse(args[2], out int valueInt))
                                    {
                                        SetClass(targetPlayer, valueInt);
                                    }
                                    else if (Enum.TryParse(args[2], true, out ECharacterClass valueEnum))
                                    {
                                        SetClass(targetPlayer, (byte)valueEnum);
                                    }
                                    else
                                    {
                                        DisplayMessage(client, "You must use either the ID or the name of the class. Check /player class list.");
                                        return;
                                    }
                                }
                                break;
                        }
                    }
                    break; 
                #endregion

                #region areas
                case "areas":
                    {
                        var targetPlayer = client.Player.TargetObject as GamePlayer;
                        if (targetPlayer == null) targetPlayer = client.Player;

                        List<string> areaList = new List<string>();

                        foreach (AbstractArea area in targetPlayer.CurrentAreas)
                        {
                            string areaInfo = area.GetType().Name + ", ID:" + area.ID;
                            if (area is QuestSearchArea)
                            {
                                QuestSearchArea questArea = area as QuestSearchArea;

                                if (questArea.DataQuest != null)
                                {
                                    areaInfo += " : DataQuest ID: " + questArea.DataQuest.ID;

                                    if (questArea.Step > 0)
                                    {
                                        areaInfo += ", Area Quest Step = " + questArea.Step;
                                    }
                                    else
                                    {
                                        areaInfo += ", Eligible = " + questArea.DataQuest.CheckQuestQualification(targetPlayer);
                                    }
                                }
                            }
                            areaList.Add(areaInfo);
                        }

                        if (areaList.Count == 0) areaList.Add("None");

                        client.Player.Out.SendCustomTextWindow(targetPlayer.Name + " - Current Areas", areaList);
                    }
                    break;
                #endregion
            }
        }

		private void SendResistEffect(GamePlayer target)
		{
			if (target != null)
			{
				foreach (GamePlayer nearPlayer in target.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
				{
					nearPlayer.Out.SendSpellEffectAnimation(target, target, 7011, 0, false, 0);
				}
			}
		}

		private static void KillPlayer(GameLiving killer, GamePlayer player)
		{
			int damage = player.Health;
			if (damage > 0)
				player.TakeDamage(killer, EDamageType.Natural, damage, 0);
		}

		private void Show_Inventory(GamePlayer player, GameClient client, string limitType)
		{
			var text = new List<string>();
			text.Add("  - Name Lastname : " + player.Name + " " + player.LastName);
			text.Add("  - Realm Level Class : " + GlobalConstants.RealmToName(player.Realm) + " " + player.Level + " " +
					 player.CharacterClass.Name);
			text.Add(" ");
			text.Add(Money.GetShortString(player.GetCurrentMoney()));
			text.Add(" ");

			bool limitShown = false;


			if (limitType == "" || limitType == "wear")
			{
				limitShown = true;
				text.Add("  ----- Wearing:");

				foreach (InventoryItem item in player.Inventory.EquippedItems)
				{
					text.Add("     [" + GlobalConstants.SlotToName(item.Item_Type) + "] " + item.Name + " (" + item.Id_nb + ")");
				}
				text.Add(" ");
			}

			if (limitType == "" || limitType == "bag")
			{
				limitShown = true;
				text.Add("  ----- Backpack:");
				foreach (InventoryItem item in player.Inventory.AllItems)
				{
					if (item.SlotPosition >= (int)eInventorySlot.FirstBackpack &&
						item.SlotPosition <= (int)eInventorySlot.LastBackpack)
					{
						text.Add(item.Count.ToString("000") + " " + item.Name + " (" + item.Id_nb + ")");
					}
				}
			}

			if (limitType == "vault")
			{
				limitShown = true;
				text.Add("  ----- Vault:");
				foreach (InventoryItem item in player.Inventory.AllItems)
				{
					if (item.SlotPosition >= (int)eInventorySlot.FirstVault && item.SlotPosition <= (int)eInventorySlot.LastVault)
					{
						text.Add(item.Count.ToString("000") + " " + item.Name + " (" + item.Id_nb + ")");
					}
				}
			}

			if (limitType == "house")
			{
				limitShown = true;
				text.Add("  ----- Housing:");
				foreach (InventoryItem item in player.Inventory.AllItems)
				{
					if (item.SlotPosition >= (int)eInventorySlot.HouseVault_First &&
						item.SlotPosition <= (int)eInventorySlot.HouseVault_Last)
					{
						text.Add(item.Count.ToString("000") + " " + item.Name + " (" + item.Id_nb + ")");
					}
				}
			}

			if (limitType == "cons")
			{
				limitShown = true;
				text.Add("  ----- GameConsignmentMerchant:");
				foreach (InventoryItem item in player.Inventory.AllItems)
				{
					if (item.SlotPosition >= (int)eInventorySlot.Consignment_First &&
						item.SlotPosition <= (int)eInventorySlot.Consignment_Last)
					{
						text.Add(item.Count.ToString("000") + " " + item.Name + " (" + item.Id_nb + ")");
					}
				}
			}

			if (!limitShown)
			{
				text.Add("Unkown command.  Use wear | bag | vault | house | cons");
			}


			client.Out.SendCustomTextWindow("PLAYER INVENTORY LISTING", text);
		}

		private void Show_Info(GamePlayer player, GameClient client)
		{
			var text = new List<string>();
			text.Add(" ");
			text.Add("PLAYER INFORMATION (Client # " + player.Client.SessionID + ", " + player.GetType().FullName + ")");
			text.Add("  - Name Lastname : " + player.Name + " " + player.LastName);
			text.Add("  - Realm Level Gender Class : " + GlobalConstants.RealmToName(player.Realm) + " " + player.Level + " " + player.Gender + " " + player.CharacterClass.Name + " (" + player.CharacterClass.ID + ")");
			text.Add("  - Guild : " + player.GuildName + " " + (player.GuildRank != null ? "Rank: " + player.GuildRank.RankLevel.ToString() : ""));
			text.Add("  - XPs/RPs/BPs : " + player.Experience + " xp, " + player.RealmPoints + " rp, " + player.BountyPoints + " bp");

            if (player.DamageRvRMemory > 0)
                text.Add("  - Damage RvR Memory: " + player.DamageRvRMemory);
			
            if (player.Champion)
			{
				text.Add("  - Champion :  CL " + player.ChampionLevel + ", " + player.ChampionExperience + " clxp");

				string activeBags = "None";
				if (player.ActiveSaddleBags != 0)
				{
					if (player.ActiveSaddleBags == (byte)EHorseSaddleBag.All)
					{
						activeBags = "All";
					}
					else
					{
						activeBags = "";

						if ((player.ActiveSaddleBags & (byte)EHorseSaddleBag.LeftFront) > 0)
						{
							if (activeBags != "")
								activeBags += ", ";

							activeBags += "LeftFront";
						}
						if ((player.ActiveSaddleBags & (byte)EHorseSaddleBag.RightFront) > 0)
						{
							if (activeBags != "")
								activeBags += ", ";

							activeBags += "RightFront";
						}
						if ((player.ActiveSaddleBags & (byte)EHorseSaddleBag.LeftRear) > 0)
						{
							if (activeBags != "")
								activeBags += ", ";

							activeBags += "LeftRear";
						}
						if ((player.ActiveSaddleBags & (byte)EHorseSaddleBag.RightRear) > 0)
						{
							if (activeBags != "")
								activeBags += ", ";

							activeBags += "RightRear";
						}
					}
				}

				text.Add(string.Format("  - ActiveSaddleBags : {0} (0x{1:X2})", activeBags, player.ActiveSaddleBags));
			}
			else
			{
				text.Add("  - Champion :  Not Started");
			}
			if (player.MLGranted)
			{
				text.Add("  - Master Levels :  ML " + player.MLLevel + ", " + player.MLExperience + " mlxp , MLLine " + player.MLLine);
			}
			else
			{
				text.Add("  - Master Levels :  Not Started");
			}
			text.Add("  - Craftingskill : " + player.CraftingPrimarySkill + "");
			text.Add("  - Money : " + Money.GetString(player.GetCurrentMoney()) + "");
			text.Add("  - Model ID : " + player.Model);
			text.Add("  - Region OID : " + player.ObjectID);
			text.Add("  - AFK Message: " + player.TempProperties.getProperty<string>(GamePlayer.AFK_MESSAGE) + "");
			text.Add(" ");
			text.Add("HOUSE INFORMATION ");
			text.Add("  - Personal House : " + HouseMgr.GetHouseNumberByPlayer(player));
			if (player.CurrentHouse != null && player.CurrentHouse.HouseNumber > 0)
				text.Add("  - Current House : " + player.CurrentHouse.HouseNumber);
			text.Add("  - In House : " + player.InHouse);
			text.Add(" ");
			text.Add("ACCOUNT INFORMATION ");
			text.Add("  - Account Name & IP : " + player.Client.Account.Name + " from " + player.Client.Account.LastLoginIP);
			text.Add("  - Priv. Level : " + player.Client.Account.PrivLevel);
			text.Add("  - Client Version: " + player.Client.Account.LastClientVersion);
			text.Add(" ");
			text.Add("CHARACTER STATS ");

			String sCurrent = "";
			String sTitle = "";
			int cnt = 0;

			for (EProperty stat = EProperty.Stat_First; stat <= EProperty.Stat_Last; stat++, cnt++)
			{
				sTitle += GlobalConstants.PropertyToName(stat) + "/";
				sCurrent += player.GetModified(stat) + "/";
				if (cnt == 3)
				{
					text.Add("  - Current stats " + sTitle + " : " + sCurrent);
					sTitle = "";
					sCurrent = "";
				}
			}
			text.Add("  - Current stats " + sTitle + " : " + sCurrent);

			sCurrent = "";
			sTitle = "";
			cnt = 0;
			for (EProperty res = EProperty.Resist_First; res <= EProperty.Resist_Last; res++, cnt++)
			{
				sTitle += GlobalConstants.PropertyToName(res) + "/";
				sCurrent += player.GetModified(res) + "/";
				if (cnt == 2)
				{
					text.Add("  - Current " + sTitle + " : " + sCurrent);
					sCurrent = "";
					sTitle = "";
				}
				if (cnt == 5)
				{
					text.Add("  - Current " + sTitle + " : " + sCurrent);
					sCurrent = "";
					sTitle = "";
				}
			}
			text.Add("  - Current " + sTitle + " : " + sCurrent);

			text.Add("  - Maximum Health : " + player.MaxHealth);
			text.Add("  - Current AF and ABS : " + player.GetModified(EProperty.ArmorFactor) + " AF, " +
					 player.GetModified(EProperty.ArmorAbsorption) + " ABS");
			text.Add(" ");
			text.Add("SPECCING INFORMATIONS ");
			text.Add("  - Respecs availables : " + player.RespecAmountDOL + " dol, " + player.RespecAmountSingleSkill +
					 " single, " + player.RespecAmountAllSkill + " full");
			text.Add("  - Remaining spec. points : " + player.SkillSpecialtyPoints);
			sTitle = "  - Player specialisations : ";
			sCurrent = "";
			foreach (Specialization spec in player.GetSpecList())
			{
				sCurrent += spec.Name + " = " + spec.Level + " ; ";
			}
			text.Add(sTitle + sCurrent);

			client.Out.SendCustomTextWindow("PLAYER & ACCOUNT INFORMATION", text);
        }
        public void SetClass(GamePlayer target, int classID)
        {
            //remove all their tricks and abilities!
            target.RemoveAllSpecs();
            target.RemoveAllSpellLines();
            target.styleComponent.RemoveAllStyles();

            //reset before, and after changing the class.
            target.Reset();
            target.SetCharacterClass(classID);
            target.Reset();

            //this is just for additional updates
            //that add all the new class changes.
            target.OnLevelUp(0);

            target.Out.SendUpdatePlayer();
            target.Out.SendUpdatePlayerSkills();
            target.Out.SendUpdatePoints();
        }
	}
}