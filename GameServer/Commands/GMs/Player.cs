using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DOL.Database;
using DOL.Events;
using DOL.GS.Effects;
using DOL.GS.Friends;
using DOL.GS.Housing;
using DOL.GS.PacketHandler;
using DOL.GS.Quests;

namespace DOL.GS.Commands
{
	[Cmd(
		"&player",
		ePrivLevel.GM,
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
	public class PlayerCommandHandler : AbstractCommandHandler, ICommandHandler
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

                        var character = DOLDB<DbCoreCharacter>.SelectObject(DB.Column("Name").IsEqualTo(args[2]));

                        if (character != null)
                        {
                            client.Out.SendMessage("Duplicate Name!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                            return;
                        }

                        string oldName = player.Name;

                        player.Name = args[2];
                        player.Out.SendMessage(
                            client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has changed your name to " + player.Name +
                            "!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                        client.Out.SendMessage("You successfully changed this players name to " + player.Name + "!",
                                               eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                        client.Out.SendMessage("Tell the player to Log out and back in to complete the change.", eChatType.CT_Important,
                                               eChatLoc.CL_SystemWindow);

                        // Log change
                        AuditMgr.AddAuditEntry(client, AuditType.Character, AuditSubtype.CharacterRename, oldName, args[2]);

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
                                        player.LastName + "!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                                    client.Out.SendMessage("You successfully changed " + player.Name + "'s lastname to " + player.LastName + "!",
                                                           eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                                    player.SaveIntoDatabase();
                                    break;
                                }

                            case "reset":
                                {
                                    player.LastName = null;
                                    client.Out.SendMessage("You cleared " + player.Name + "'s lastname successfully!", eChatType.CT_Important,
                                                           eChatLoc.CL_SystemWindow);
                                    player.Out.SendMessage(
                                        client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has cleared your lastname!",
                                        eChatType.CT_Important, eChatLoc.CL_SystemWindow);
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
                                                       eChatType.CT_Important, eChatLoc.CL_SystemWindow);

                            if (pToLevel != client.Player)
                                pToLevel.Out.SendMessage(
                                    client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has given you a free level!",
                                    eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                        }
                        else
                        {
                            pToLevel.GainExperience(eXPSource.Other, pToLevel.ExperienceForCurrentLevelSecondStage - pToLevel.Experience);

                            client.Out.SendMessage("You gave " + pToLevel.Name + " a free half level!",
                                                       eChatType.CT_Important, eChatLoc.CL_SystemWindow);

                            if (pToLevel != client.Player)
                                pToLevel.Out.SendMessage(
                                    client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has given you a free half level!",
                                    eChatType.CT_Important, eChatLoc.CL_SystemWindow);
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
                                client.Out.SendMessage(player.Name + "'s level can only be set to a number 1 to 255!", eChatType.CT_Important,
                                                       eChatLoc.CL_SystemWindow);
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
                                    player.GainExperience(eXPSource.Other, player.GetExperienceValueForLevel(++curLevel));
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
                                client.Out.SendMessage("You have reset " + player.Name + "!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                                player.Out.SendMessage(
                                    client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has respecced your skills and reset your spec points!",
                                    eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                            }
                            else
                            {
                                client.Out.SendMessage("You changed " + player.Name + "'s level successfully to " + newLevel + "!",
                                                       eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                                player.Out.SendMessage(
                                    client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has changed your level to " + newLevel + "!",
                                    eChatType.CT_Important, eChatLoc.CL_SystemWindow);
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
							client.Out.SendMessage(player.Name + " is now on the path of the Champion!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
							player.Out.SendMessage(client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has started you on the path of the Champion!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
						}
						else
						{
							client.Out.SendMessage(player.Name + " is already on the path of the Champion!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
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
                        client.Out.SendMessage("You have cleared " + player.Name + "'s Champion levels!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                        player.Out.SendMessage(client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has cleared your Champion levels!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
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
						client.Out.SendMessage("You have respecced " + player.Name + "'s Champion levels!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
						player.Out.SendMessage(client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has respecced your Champion levels!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
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
							client.Out.SendMessage(player.Name + " is now ready to start Master Level training!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
							player.Out.SendMessage(client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has started your Master Level training!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
						}
						else
						{
							client.Out.SendMessage(player.Name + " has already started Master Level training!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
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
						client.Out.SendMessage(player.Name + " Master Level is set to " + level + "!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
						player.Out.SendMessage(client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has set your Master Level to " + level + "!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
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
						client.Out.SendMessage(player.Name + " Master Line is set to " + line + "!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
						player.Out.SendMessage(client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has set your Master Line to " + line + "!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
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
							client.Out.SendMessage(player.Name + " has already finished all Master Levels!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
							return;
						}

						byte level = Convert.ToByte(args[2]);
						if (level > GamePlayer.ML_MAX_LEVEL)
						{
							client.Out.SendMessage("Valid levels are 0 - " + GamePlayer.ML_MAX_LEVEL + "!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
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
							client.Out.SendMessage(player.Name + " has already finished step " + step + " for Master Level " + (player.MLLevel + 1) + "!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
						}
						else if (setFinished == false && player.HasFinishedMLStep(player.MLLevel + 1, step) == false)
						{
							client.Out.SendMessage(player.Name + " has not yet finished step " + step + " for Master Level " + (player.MLLevel + 1) + "!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
						}
						else
						{
							player.SetFinishedMLStep(player.MLLevel + 1, step, setFinished);
							if (setFinished)
							{
								client.Out.SendMessage(player.Name + " has now finished step " + step + " for Master Level " + (player.MLLevel + 1) + "!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
								player.Out.SendMessage(client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has set step " + step + " completed for Master Level " + (player.MLLevel + 1) + "!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
							}
							else
							{
								client.Out.SendMessage(player.Name + " has no longer finished step " + step + " for Master Level " + (player.MLLevel + 1) + "!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
								player.Out.SendMessage(client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has set step " + step + " as unfinished for Master Level " + (player.MLLevel + 1) + "!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
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
                                client.Out.SendMessage(player.Name + "'s realm can only be set to numbers 0-3!", eChatType.CT_Important,
                                                       eChatLoc.CL_SystemWindow);
                                return;
                            }

                            player.Realm = (eRealm)newRealm;

                            client.Out.SendMessage(
                                "You successfully changed " + player.Name + "'s realm to " + GlobalConstants.RealmToName((eRealm)newRealm) +
                                "!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                            player.Out.SendMessage(
                                client.Player.Name + " has changed your realm to " + GlobalConstants.RealmToName((eRealm)newRealm) + "!",
                                eChatType.CT_Important, eChatLoc.CL_SystemWindow);

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
                                                               eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                                        player.Out.SendMessage(
                                            client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel +
                                            ") has changed your model back to its original creation model!", eChatType.CT_Important,
                                            eChatLoc.CL_SystemWindow);
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
                                                               eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                                        player.Out.SendMessage(
                                            client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has changed your form! (ID:#" + modelID +
                                            ")", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
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
                                        client.Out.SendMessage("You gave " + player.Name + " copper successfully!", eChatType.CT_Important,
                                                               eChatLoc.CL_SystemWindow);
                                        player.Out.SendMessage(
                                            client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has given you some copper!",
                                            eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                                        return;
                                    }


                                case "silv":
                                    {
                                        long amount = long.Parse(args[3]) * 100;
                                        player.AddMoney(amount);
                                        InventoryLogging.LogInventoryAction(client.Player, player, eInventoryActionType.Other, amount);
                                        client.Out.SendMessage("You gave " + player.Name + " silver successfully!", eChatType.CT_Important,
                                                               eChatLoc.CL_SystemWindow);
                                        player.Out.SendMessage(
                                            client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has given you some silver!",
                                            eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                                        return;
                                    }

                                case "gold":
                                    {
                                        long amount = long.Parse(args[3]) * 100 * 100;
                                        player.AddMoney(amount);
                                        InventoryLogging.LogInventoryAction(client.Player, player, eInventoryActionType.Other, amount);
                                        client.Out.SendMessage("You gave " + player.Name + " gold successfully!", eChatType.CT_Important,
                                                               eChatLoc.CL_SystemWindow);
                                        player.Out.SendMessage(
                                            client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has given you some gold!",
                                            eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                                        return;
                                    }

                                case "plat":
                                    {
                                        long amount = long.Parse(args[3]) * 100 * 100 * 1000;
                                        player.AddMoney(amount);
                                        InventoryLogging.LogInventoryAction(client.Player, player, eInventoryActionType.Other, amount);
                                        client.Out.SendMessage("You gave " + player.Name + " platinum successfully!", eChatType.CT_Important,
                                                               eChatLoc.CL_SystemWindow);
                                        player.Out.SendMessage(
                                            client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has given you some platinum!",
                                            eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                                        return;
                                    }

                                case "mith":
                                    {
                                        long amount = long.Parse(args[3]) * 100 * 100 * 1000 * 1000;
                                        player.AddMoney(amount);
                                        InventoryLogging.LogInventoryAction(client.Player, player, eInventoryActionType.Other, amount);
                                        client.Out.SendMessage("You gave " + player.Name + " mithril successfully!", eChatType.CT_Important,
                                                               eChatLoc.CL_SystemWindow);
                                        player.Out.SendMessage(
                                            client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has given you some mithril!",
                                            eChatType.CT_Important, eChatLoc.CL_SystemWindow);
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
                                                   eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                            player.Out.SendMessage(
                                client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has given you " + amount + " realmpoints!",
                                eChatType.CT_Important, eChatLoc.CL_SystemWindow);
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

                            eXPSource xpSource = eXPSource.Other;
                            if (args[1].ToLower() == "xpa")
                            {
                                xpSource = eXPSource.NPC;
                            }

                            long amount = long.Parse(args[2]);
                            player.GainExperience(xpSource, amount, false);
                            client.Out.SendMessage("You gave " + player.Name + " " + amount + " experience succesfully!",
                                                   eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                            player.Out.SendMessage(
                                client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has given you " + amount + " experience!",
                                eChatType.CT_Important, eChatLoc.CL_SystemWindow);
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
                            player.GainChampionExperience(amount, eXPSource.GM);
                            client.Out.SendMessage("You gave " + player.Name + " " + amount + " Champion experience succesfully!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                            player.Out.SendMessage(client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has given you " + amount + " Champion experience!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);

							// now see if player gained any CL and level them up
							bool gainedLevel = false;
							while (player.ChampionLevel < player.ChampionMaxLevel && player.ChampionExperience >= player.ChampionExperienceForNextLevel)
							{
								player.ChampionLevelUp();
								gainedLevel = true;
							}

							if (gainedLevel)
							{
								player.Out.SendMessage("You reached champion level " + player.ChampionLevel + "!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
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
							client.Out.SendMessage("You gave " + player.Name + " " + amount + " ML experience succesfully!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
							player.Out.SendMessage(client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has given you " + amount + " ML experience!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);

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
                                                   eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                            player.Out.SendMessage(
                                client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has given you " + amount + " bountypoints!",
                                eChatType.CT_Important, eChatLoc.CL_SystemWindow);
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
                                        player.ChangeBaseStat(eStat.DEX, value);
                                        player.Out.SendMessage(
                                            client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has given you " + value + " dexterity!",
                                            eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                                        client.Out.SendMessage("You gave " + player.Name + " " + value + " dexterity successfully!",
                                                               eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                                    }
                                    break;

                                /*2*/
                                case "str":
                                    {
                                        player.ChangeBaseStat(eStat.STR, value);
                                        player.Out.SendMessage(
                                            client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has given you " + value + " strength!",
                                            eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                                        client.Out.SendMessage("You gave " + player.Name + " " + value + " strength successfully!",
                                                               eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                                    }
                                    break;

                                /*3*/
                                case "con":
                                    {
                                        player.ChangeBaseStat(eStat.CON, value);
                                        player.Out.SendMessage(
                                            client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has given you " + value +
                                            " consititution!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                                        client.Out.SendMessage("You gave " + player.Name + " " + value + " constitution successfully!",
                                                               eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                                    }
                                    break;

                                /*4*/
                                case "emp":
                                    {
                                        player.ChangeBaseStat(eStat.EMP, value);
                                        player.Out.SendMessage(
                                            client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has given you " + value + " empathy!",
                                            eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                                        client.Out.SendMessage("You gave " + player.Name + " " + value + " empathy successfully!",
                                                               eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                                    }
                                    break;

                                /*5*/
                                case "int":
                                    {
                                        player.ChangeBaseStat(eStat.INT, value);
                                        player.Out.SendMessage(
                                            client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has given you " + value +
                                            " intelligence!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                                        client.Out.SendMessage("You gave " + player.Name + " " + value + " intelligence successfully!",
                                                               eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                                    }
                                    break;

                                /*6*/
                                case "pie":
                                    {
                                        player.ChangeBaseStat(eStat.PIE, value);
                                        player.Out.SendMessage(
                                            client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has given you " + value + " piety!",
                                            eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                                        client.Out.SendMessage("You gave " + player.Name + " " + value + " piety successfully!",
                                                               eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                                    }
                                    break;

                                /*7*/
                                case "qui":
                                    {
                                        player.ChangeBaseStat(eStat.QUI, value);
                                        player.Out.SendMessage(
                                            client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has given you " + value + " quickness!",
                                            eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                                        client.Out.SendMessage("You gave " + player.Name + " " + value + " quickness successfully!",
                                                               eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                                    }
                                    break;

                                /*8*/
                                case "cha":
                                    {
                                        player.ChangeBaseStat(eStat.CHR, value);
                                        player.Out.SendMessage(
                                            client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has given you " + value + " charisma!",
                                            eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                                        client.Out.SendMessage("You gave " + player.Name + " " + value + " charisma successfully!",
                                                               eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                                    }
                                    break;

                                /*all*/
                                case "all":
                                    {
                                        player.ChangeBaseStat(eStat.CHR, value); //1
                                        player.ChangeBaseStat(eStat.QUI, value); //2
                                        player.ChangeBaseStat(eStat.INT, value); //3
                                        player.ChangeBaseStat(eStat.PIE, value); //4
                                        player.ChangeBaseStat(eStat.EMP, value); //5
                                        player.ChangeBaseStat(eStat.CON, value); //6
                                        player.ChangeBaseStat(eStat.STR, value); //7
                                        player.ChangeBaseStat(eStat.DEX, value); //8
                                        player.Out.SendMessage(
                                            client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has given you " + value +
                                            " to all stats!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                                        client.Out.SendMessage("You gave " + player.Name + " " + value + " to all stats successfully!",
                                                               eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                                    }
                                    break;

                                default:
                                    {
                                        client.Out.SendMessage("Try using: dex, str, con, emp, int, pie, qui, cha, or all as a type of stat.",
                                                               eChatType.CT_System, eChatLoc.CL_SystemWindow);
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
                            client.Out.SendMessage("You need a valid target!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            return;
                        }

                        if (args[2] == "list")
                        {
                            string[] list = player.SerializedFriendsList;
                            client.Out.SendCustomTextWindow(player.Name + "'s Friend List", list);
                            return;
                        }

                        string name = string.Join(" ", args, 2, args.Length - 2);
                        GamePlayer targetPlayer = ClientService.GetPlayerByPartialName(name, out ClientService.PlayerGuessResult result);;

                        if (targetPlayer != null && !GameServer.ServerRules.IsSameRealm(targetPlayer, player.Client.Player, true))
                            targetPlayer = null;

                        if (targetPlayer == null)
                        {
                            name = args[2];

                            if (player.GetFriends().Contains(name) && player.RemoveFriend(name))
                            {
                                player.Out.SendMessage($"{client.Player.Name} (PrivLevel: {client.Account.PrivLevel}) has removed {name} from your friend list!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                                client.Out.SendMessage($"Removed {name} from {player.Name}'s friend list successfully!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            else
                            {
                                client.Out.SendMessage($"No players online with name {name}.", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                                return;
                            }
                        }

                        switch (result)
                        {
                            case ClientService.PlayerGuessResult.FOUND_MULTIPLE:
                            {
                                client.Out.SendMessage("Character name is not unique.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            case ClientService.PlayerGuessResult.FOUND_EXACT:
                            case ClientService.PlayerGuessResult.FOUND_PARTIAL:
                            {
                                if (targetPlayer == player)
                                {
                                    client.Out.SendMessage("You can't add that player to his or her own friend list!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                                    return;
                                }

                                name = targetPlayer.Name;

                                if (player.GetFriends().Contains(name) && player.RemoveFriend(name))
                                {
                                    player.Out.SendMessage($"{client.Player.Name} (PrivLevel: {client.Account.PrivLevel}) has removed {name} from your friend list!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                                    client.Out.SendMessage($"Removed {name} from {player.Name}'s friend list successfully!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                                }
                                else if (player.AddFriend(name))
                                {
                                    player.Out.SendMessage($"{client.Player.Name} (PrivLevel: {client.Account.PrivLevel}) has added {name} to your friend list!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                                    client.Out.SendMessage($"Added {name} to {player.Name}'s friend list successfully!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                                }

                                break;
                            }
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
                                        " single respec!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                                    client.Out.SendMessage(amount + " single respec given successfully to " + player.Name + "!",
                                                           eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                                    break;
                                }
                            case "all":
                                {
                                    player.RespecAmountAllSkill += amount;
                                    player.Client.Out.SendMessage(
                                        client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has awarded you " + amount +
                                        " full respec!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                                    client.Out.SendMessage(amount + " full respec given successfully to " + player.Name + "!",
                                                           eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                                    break;
                                }
                            case "realm":
                                {
                                    player.RespecAmountRealmSkill += amount;
                                    player.Client.Out.SendMessage(
                                        client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has awarded you " + amount +
                                        " realm respec!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                                    client.Out.SendMessage(amount + " realm respec given successfully to " + player.Name + "!",
                                                           eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                                    break;
                                }
                            case "dol":
                                {
                                    player.RespecAmountDOL += amount;
                                    player.Client.Out.SendMessage(
                                        client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has awarded you " + amount +
                                        " DOL (full) respec!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                                    client.Out.SendMessage(amount + " DOL (full) respec given successfully to " + player.Name + "!",
                                                           eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                                    break;
                                }
                            case "champion":
                                {
                                    player.RespecAmountChampionSkill += amount;
                                    player.Client.Out.SendMessage(
                                        client.Player.Name + "(PrivLevel: " + client.Account.PrivLevel + ") has awarded you " + amount +
                                        " Champion respec!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                                    client.Out.SendMessage(amount + " champion respec given successfully to " + player.Name + "!",
                                                           eChatType.CT_Important, eChatLoc.CL_SystemWindow);
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
                            client.Out.SendMessage("You need a valid target!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
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
                        GamePlayer player = client.Player.TargetObject as GamePlayer;

                        if (args.Length is > 3 or < 2)
                        {
                            DisplaySyntax(client);
                            return;
                        }

                        if (player == null && args.Length == 2)
                        {
                            client.Out.SendMessage("You need a valid target!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            return;
                        }

                        if (args.Length == 2 && player != null)
                        {
                            player.Out.SendMessage($"{client.Player.Name} (PrivLevel: {client.Account.PrivLevel}) has saved your character.",eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                            client.Out.SendMessage($"{player.Name} saved successfully!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                            player.SaveIntoDatabase();
                        }

                        if (args.Length == 3)
                        {
                            switch (args[2])
                            {
                                case "all":
                                {
                                    foreach (GamePlayer otherPlayer in ClientService.GetPlayers())
                                        otherPlayer.SaveIntoDatabase();

                                    client.Out.SendMessage("Saved all characters!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                                    break;
                                }
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
                            client.Out.SendMessage("You need a valid target!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            return;
                        }

                        if (args.Length == 2 && player != null)
                        {
                            if (player.Client.Account.PrivLevel > 1)
                            {
                                client.Out.SendMessage(
                                    "Please use /kick <name> to kick Gamemasters. This is used to prevent accidental kicks.",
                                    eChatType.CT_Important, eChatLoc.CL_SystemWindow);
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
                                        foreach (GamePlayer otherPlayer in ClientService.GetNonGmPlayers())
                                        {
                                            otherPlayer.Out.SendMessage($"{client.Player.Name} (PrivLevel: {client.Account.PrivLevel}) has kicked all players!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                                            otherPlayer.Out.SendPlayerQuit(true);
                                            otherPlayer.SaveIntoDatabase();
                                            otherPlayer.Quit(true);
                                            continue;
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
                        GamePlayer player = client.Player.TargetObject as GamePlayer;

                        if (args.Length is > 3 or < 2)
                        {
                            DisplaySyntax(client);
                            return;
                        }

                        if (player == null && args.Length == 2)
                        {
                            client.Out.SendMessage("You need a valid target!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            return;
                        }

                        if (args.Length == 2 && player != null)
                        {
                            if (!player.IsAlive)
                            {
                                player.Health = player.MaxHealth;
                                player.Mana = player.MaxMana;
                                player.Endurance = player.MaxEndurance;
                                player.MoveTo(client.Player.CurrentRegionID, client.Player.X, client.Player.Y, client.Player.Z, client.Player.Heading);
                                client.Out.SendMessage($"You resurrected {player.Name} successfully!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                                player.StopReleaseTimer();
                                player.Out.SendPlayerRevive(player);
                                player.Out.SendStatusUpdate();
                                player.Out.SendMessage($"You have been resurrected by {client.Player.GetName(0, false)}!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                player.Notify(GamePlayerEvent.Revive, player);
                            }
                            else
                            {
                                client.Out.SendMessage("Player is not dead!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                                return;
                            }
                        }

                        if (args.Length >= 3)
                        {
                            switch (args[2])
                            {
                                case "albs":
                                    {
                                        foreach (GamePlayer albPlayer in ClientService.GetPlayersOfRealm(eRealm.Albion))
                                        {
                                            if (!albPlayer.IsAlive)
                                            {
                                                albPlayer.Health = albPlayer.MaxHealth;
                                                albPlayer.Mana = albPlayer.MaxMana;
                                                albPlayer.Endurance = albPlayer.MaxEndurance;
                                                albPlayer.MoveTo(client.Player.CurrentRegionID, client.Player.X, client.Player.Y, client.Player.Z, client.Player.Heading);
                                                albPlayer.StopReleaseTimer();
                                                albPlayer.Out.SendPlayerRevive(albPlayer);
                                                albPlayer.Out.SendStatusUpdate();
                                                albPlayer.Out.SendMessage($"You have been resurrected by {client.Player.GetName(0, false)}!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                                albPlayer.Notify(GamePlayerEvent.Revive, albPlayer);
                                            }
                                        }
                                    }
                                    break;

                                case "hibs":
                                    {
                                        foreach (GamePlayer hibPlayer in ClientService.GetPlayersOfRealm(eRealm.Hibernia))
                                        {
                                            if (!hibPlayer.IsAlive)
                                            {
                                                hibPlayer.Health = hibPlayer.MaxHealth;
                                                hibPlayer.Mana = hibPlayer.MaxMana;
                                                hibPlayer.Endurance = hibPlayer.MaxEndurance;
                                                hibPlayer.MoveTo(client.Player.CurrentRegionID, client.Player.X, client.Player.Y, client.Player.Z, client.Player.Heading);
                                                hibPlayer.StopReleaseTimer();
                                                hibPlayer.Out.SendPlayerRevive(hibPlayer);
                                                hibPlayer.Out.SendStatusUpdate();
                                                hibPlayer.Out.SendMessage($"You have been resurrected by {client.Player.GetName(0, false)}!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                                hibPlayer.Notify(GamePlayerEvent.Revive, hibPlayer);
                                            }
                                        }
                                    }
                                    break;
                                case "mids":
                                    {
                                        foreach (GamePlayer midPlayer in ClientService.GetPlayersOfRealm(eRealm.Midgard))
                                        {
                                            if (!midPlayer.IsAlive)
                                            {
                                                midPlayer.Health = midPlayer.MaxHealth;
                                                midPlayer.Mana = midPlayer.MaxMana;
                                                midPlayer.Endurance = midPlayer.MaxEndurance;
                                                midPlayer.MoveTo(client.Player.CurrentRegionID, client.Player.X, client.Player.Y, client.Player.Z, client.Player.Heading);
                                                midPlayer.StopReleaseTimer();
                                                midPlayer.Out.SendPlayerRevive(midPlayer);
                                                midPlayer.Out.SendStatusUpdate();
                                                midPlayer.Out.SendMessage($"You have been resurrected by {client.Player.GetName(0, false)}!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                                midPlayer.Notify(GamePlayerEvent.Revive, midPlayer);
                                            }
                                        }
                                    }
                                    break;

                                case "self":
                                    {
                                        GamePlayer self = client.Player;

                                        if (!self.IsAlive)
                                        {
                                            self.Health = self.MaxHealth;
                                            self.Mana = self.MaxMana;
                                            self.Endurance = self.MaxEndurance;
                                            self.MoveTo(client.Player.CurrentRegionID, client.Player.X, client.Player.Y, client.Player.Z, client.Player.Heading);
                                            self.Out.SendMessage("You revive yourself.", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                                            self.StopReleaseTimer();
                                            self.Out.SendPlayerRevive(self);
                                            self.Out.SendStatusUpdate();
                                            self.Notify(GamePlayerEvent.Revive, self);
                                        }
                                        else
                                        {
                                            client.Out.SendMessage("You are not dead!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                                            return;
                                        }
                                    }
                                    break;

                                case "all":
                                    {
                                        foreach (GamePlayer otherPlayer in ClientService.GetPlayers<object>(Predicate, default))
                                        {
                                            otherPlayer.Health = otherPlayer.MaxHealth;
                                            otherPlayer.Mana = otherPlayer.MaxMana;
                                            otherPlayer.Endurance = otherPlayer.MaxEndurance;
                                            otherPlayer.MoveTo(client.Player.CurrentRegionID, client.Player.X, client.Player.Y, client.Player.Z, client.Player.Heading);
                                            otherPlayer.StopReleaseTimer();
                                            otherPlayer.Out.SendPlayerRevive(otherPlayer);
                                            otherPlayer.Out.SendStatusUpdate();
                                            otherPlayer.Out.SendMessage($"You have been resurrected by {client.Player.GetName(0, false)}!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                            otherPlayer.Notify(GamePlayerEvent.Revive, otherPlayer);
                                        }

                                        static bool Predicate(GamePlayer x, object unused)
                                        {
                                            return !x.IsAlive;
                                        }
                                    }
                                    break;
                                default:
                                    {
                                        client.Out.SendMessage("SYNTAX: /player rez <albs|mids|hibs|all>", eChatType.CT_System, eChatLoc.CL_SystemWindow);
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
                            client.Out.SendMessage("You need a valid target!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            return;
                        }

                        if (args.Length == 2 && player != null)
                        {
                            if (player.Client.Account.PrivLevel > 1)
                            {
                                client.Out.SendMessage("This command can not be used on Gamemasters!", eChatType.CT_Important,
                                                       eChatLoc.CL_SystemWindow);
                                return;
                            }

                            if (player.IsAlive)
                            {
                                KillPlayer(client.Player, player);
                                client.Out.SendMessage("You killed " + player.Name + " successfully!", eChatType.CT_Important,
                                                       eChatLoc.CL_SystemWindow);
                                player.Out.SendMessage(client.Player.Name + " has killed you!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                            }
                            else
                            {
                                client.Out.SendMessage("Player is not alive!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                                return;
                            }
                        }
                        if (args.Length < 3)
                            return;
                        switch (args[2])
                        {
                            case "albs":
                                {
                                    foreach (GamePlayer albPlayer in ClientService.GetPlayersOfRealm(eRealm.Albion))
                                    {
                                        if (albPlayer.IsAlive && albPlayer.Client.Account.PrivLevel == 1)
                                            KillPlayer(client.Player, albPlayer);
                                    }
                                }
                                break;

                            case "mids":
                                {
                                    foreach (GamePlayer midPlayer in ClientService.GetPlayersOfRealm(eRealm.Midgard))
                                    {
                                        if (midPlayer.IsAlive && midPlayer.Client.Account.PrivLevel == 1)
                                            KillPlayer(client.Player, midPlayer);
                                    }
                                }
                                break;

                            case "hibs":
                                {
                                    foreach (GamePlayer hibPlayer in ClientService.GetPlayersOfRealm(eRealm.Hibernia))
                                    {
                                        if (hibPlayer.IsAlive && hibPlayer.Client.Account.PrivLevel == 1)
                                            KillPlayer(client.Player, hibPlayer);
                                    }
                                }
                                break;

                            case "self":
                                {
                                    GamePlayer self = client.Player;

                                    if (!self.IsAlive)
                                    {
                                        client.Out.SendMessage("You are already dead. Use /player rez <self> to resurrect yourself.", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                                        return;
                                    }
                                    else
                                    {
                                        KillPlayer(client.Player, client.Player);
                                        client.Out.SendMessage("Good bye cruel world!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                                    }
                                }
                                break;

                            case "all":
                                {
                                    foreach (GamePlayer otherPlayer in ClientService.GetNonGmPlayers())
                                    {
                                        if (otherPlayer.IsAlive)
                                            KillPlayer(client.Player, otherPlayer);
                                    }
                                }
                                break;

                            default:
                                {
                                    client.Out.SendMessage($"'{args[2]}' is not a valid argument.", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
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
                                    if (args[3] == null)
                                    {
                                        DisplaySyntax(client);
                                        return;
                                    }

                                    short count = 0;
                                    string guildName = string.Join(" ", args, 3, args.Length - 3);
                                    List<GamePlayer> players = ClientService.GetPlayers(Predicate, guildName);

                                    foreach (GamePlayer guildMember in players)
                                    {
                                        count++;
                                        guildMember.MoveTo(client.Player.CurrentRegionID, client.Player.X, client.Player.Y, client.Player.Z, client.Player.Heading);
                                    }

                                    client.Out.SendMessage($"{count} players jumped!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);

                                    static bool Predicate(GamePlayer player, string guildName)
                                    {
                                        return !string.IsNullOrEmpty(player.GuildName) && player.GuildName.Equals(guildName);
                                    }
                                }
                                break;

                            case "group":
                                {
                                    if (args[3] == null)
                                    {
                                        DisplaySyntax(client);
                                        return;
                                    }

                                    short count = 0;
                                    string name = args[3];
                                    GamePlayer player = ClientService.GetPlayerByExactName(name);

                                    if (player != null)
                                    {
                                        foreach (GameLiving groupMember in player.Group.GetMembersInTheGroup())
                                        {
                                            groupMember.MoveTo(client.Player.CurrentRegionID, client.Player.X, client.Player.Y, client.Player.Z, client.Player.Heading);
                                            count++;
                                        }
                                    }

                                    client.Out.SendMessage($"{count} players jumped!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                                }
                                break;

                            case "cg":
                                {
                                    if (args[3] == null)
                                    {
                                        DisplaySyntax(client);
                                        return;
                                    }

                                    short count = 0;
                                    string name = args[3];
                                    GamePlayer player = ClientService.GetPlayerByExactName(name);

                                    if (player != null)
                                    {
                                        ChatGroup cg = player.TempProperties.GetProperty<ChatGroup>(ChatGroup.CHATGROUP_PROPERTY, null);

                                        if (cg != null)
                                        {
                                            foreach (GamePlayer bgMember in cg.Members.Keys)
                                            {
                                                bgMember.MoveTo(client.Player.CurrentRegionID, client.Player.X, client.Player.Y, client.Player.Z, client.Player.Heading);
                                                count++;
                                            }
                                        }
                                    }

                                    client.Out.SendMessage($"{count} players jumped!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                                }
                                break;

                            case "bg":
                                {
                                    if (args[3] == null)
                                    {
                                        DisplaySyntax(client);
                                        return;
                                    }

                                    short count = 0;
                                    string name = args[3];
                                    GamePlayer player = ClientService.GetPlayerByExactName(name);

                                    if (player != null)
                                    {
                                        BattleGroup bg = player.TempProperties.GetProperty<BattleGroup>(BattleGroup.BATTLEGROUP_PROPERTY, null);

                                        if (bg != null)
                                        {
                                            foreach (GamePlayer bgMember in bg.Members.Keys)
                                            {
                                                bgMember.MoveTo(client.Player.CurrentRegionID, client.Player.X, client.Player.Y, client.Player.Z, client.Player.Heading);
                                                count++;
                                            }
                                        }
                                    }

                                    client.Out.SendMessage($"{count} players jumped!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                                }
                                break;

                            default:
                                {
                                    client.Out.SendMessage($"'{args[2]}' is not a valid argument.", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
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
                        client.Out.SendMessage(player.Name + " updated successfully!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
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
								client.Player.Out.SendMessage(string.Format("{0}'s active saddlebags set to 0x{1:X2}!", player.Name, player.ActiveSaddleBags), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
								player.Out.SendMessage(string.Format("Your active saddlebags have been set to 0x{0:X2} by {1}!", player.ActiveSaddleBags, client.Player.Name), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
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
                                               eChatType.CT_System, eChatLoc.CL_SystemWindow);
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
                            client.Out.SendMessage("You need a valid target!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            return;
                        }

                        if (player.Group == null)
                        {
                            client.Out.SendMessage("Player does not have a group!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
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
                            client.Out.SendMessage("You need a valid target!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
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

                            foreach (ECSGameSpellEffect e in player.effectListComponent.GetSpellEffects())
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
                                foreach (ECSGameSpellEffect e in positiveEffects)
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
                                foreach (ECSGameSpellEffect e in negativeEffects)
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
                            client.Out.SendMessage("You need a valid target!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
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
                       GamePlayer targetPlayer;

                        if (args.Length > 2)
                            targetPlayer = ClientService.GetPlayerByExactName(args[2]);
                        else
                            targetPlayer = client.Player.TargetObject as GamePlayer;

                        if (targetPlayer == null)
                        {
                            DisplaySyntax(client, args[1]);
                            return;
                        }
                        else
                        {
                            string characterNames = string.Empty;

                            foreach (DbCoreCharacter acctChar in targetPlayer.Client.Account.Characters)
                            {
                                if (acctChar != null)
                                    characterNames += $"{acctChar.Name} {acctChar.LastName}\n";
                            }

                            client.Out.SendMessage(characterNames, eChatType.CT_Say, eChatLoc.CL_PopupWindow);
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

                                    foreach (eCharacterClass cl in Enum.GetValues(typeof(eCharacterClass)))
                                    {
                                        classList.Add(Enum.GetName(typeof(eCharacterClass), cl) + " - " + (int)cl);
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
                                    else if (Enum.TryParse(args[2], true, out eCharacterClass valueEnum))
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
				player.TakeDamage(killer, eDamageType.Natural, damage, 0);
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

				foreach (DbInventoryItem item in player.Inventory.EquippedItems)
				{
					text.Add("     [" + GlobalConstants.SlotToName(item.Item_Type) + "] " + item.Name + " (" + item.Id_nb + ")");
				}
				text.Add(" ");
			}

			if (limitType == "" || limitType == "bag")
			{
				limitShown = true;
				text.Add("  ----- Backpack:");
				foreach (DbInventoryItem item in player.Inventory.AllItems)
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
				foreach (DbInventoryItem item in player.Inventory.AllItems)
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
				foreach (DbInventoryItem item in player.Inventory.AllItems)
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
				foreach (DbInventoryItem item in player.Inventory.AllItems)
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
					if (player.ActiveSaddleBags == (byte)eHorseSaddleBag.All)
					{
						activeBags = "All";
					}
					else
					{
						activeBags = "";

						if ((player.ActiveSaddleBags & (byte)eHorseSaddleBag.LeftFront) > 0)
						{
							if (activeBags != "")
								activeBags += ", ";

							activeBags += "LeftFront";
						}
						if ((player.ActiveSaddleBags & (byte)eHorseSaddleBag.RightFront) > 0)
						{
							if (activeBags != "")
								activeBags += ", ";

							activeBags += "RightFront";
						}
						if ((player.ActiveSaddleBags & (byte)eHorseSaddleBag.LeftRear) > 0)
						{
							if (activeBags != "")
								activeBags += ", ";

							activeBags += "LeftRear";
						}
						if ((player.ActiveSaddleBags & (byte)eHorseSaddleBag.RightRear) > 0)
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
			text.Add("  - AFK Message: " + player.TempProperties.GetProperty<string>(GamePlayer.AFK_MESSAGE) + "");
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

			for (eProperty stat = eProperty.Stat_First; stat <= eProperty.Stat_Last; stat++, cnt++)
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
			for (eProperty res = eProperty.Resist_First; res <= eProperty.Resist_Last; res++, cnt++)
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
			text.Add("  - Current AF and ABS : " + player.GetModified(eProperty.ArmorFactor) + " AF, " +
					 player.GetModified(eProperty.ArmorAbsorption) + " ABS");
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
