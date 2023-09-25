using System.Collections.Generic;
using DOL.Database;
using DOL.GS.Appeal;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Commands
{
    [CmdAttribute(
        "&gmappeal",
        new string[] { "&gmhelp" },
        ePrivLevel.GM,
        "Commands for server staff to assist players with their Appeals.",
        "/gmappeal view <player name> - Views the appeal of a specific player.",
        "/gmappeal list - Lists all the current Appeals from online players only, in a window.",
        "/gmappeal listall - Will list Appeals of both offline and online players, in a window.",
        "/gmappeal assist <player name> - Take ownership of the player's appeal and lets other staff know you are helping this player.",
        "/gmappeal jumpto - Will jump you to the player you are currently assisting (must use /gmappeal assist first).",
        "/gmappeal jumpback - Will jump you back to where you were after you've helped the player (must use /gmappeal jumpto first).",
        "/gmappeal close <player name> - Closes the appeal and removes it from the queue.",
        "/gmappeal closeoffline <player name> - Closes an appeal of a player who is not online.",
        "/gmappeal release <player name> - Releases ownership of the player's appeal so someone else can help them.",
        "/gmappeal mute - Toggles receiving appeal notices, for yourself, for this session.",
        "/gmappeal commands - Lists all the commands in a pop up window.")]

    public class GMAppealCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if (ServerProperties.Properties.DISABLE_APPEALSYSTEM)
            {
                AppealMgr.MessageToClient(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Appeal.SystemDisabled"));
                return;
            }

            if (args.Length < 2)
            {
                DisplaySyntax(client);
                return;
            }

            switch (args[1])
            {

                #region gmappeal assist
                case "assist":
                    {
                        if (args.Length < 3)
                        {
                            DisplaySyntax(client);
                            return;
                        }

                        string targetName = args[2];
                        GamePlayer targetPlayer = ClientService.GetPlayerByPartialName(targetName, out ClientService.PlayerGuessResult result);

                        switch (result)
                        {
                            case ClientService.PlayerGuessResult.NOT_FOUND:
                            {
                                AppealMgr.MessageToClient(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Appeal.PlayerNotFound", targetName));
                                return;
                            }
                            case ClientService.PlayerGuessResult.FOUND_MULTIPLE:
                            {
                                AppealMgr.MessageToClient(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Appeal.NameNotUnique"));
                                return;
                            }
                            case ClientService.PlayerGuessResult.FOUND_EXACT:
                            case ClientService.PlayerGuessResult.FOUND_PARTIAL:
                                break;
                        }

                        DbAppeal appeal = AppealMgr.GetAppealByPlayerName(targetPlayer.Name);

                        if (appeal != null)
                        {
                            if (appeal.Status != "Being Helped")
                            {
                                AppealMgr.ChangeStatus(client.Player.Name, targetPlayer, appeal, "Being Helped");
                                string message = LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Appeal.RandMessage" + Util.Random(4), targetPlayer.Name);
                                client.Player.TempProperties.SetProperty("AppealAssist", targetPlayer);
                                client.Player.SendPrivateMessage(targetPlayer, message);
                                targetPlayer.Out.SendPlaySound(eSoundType.Craft, 0x04);
                                return;
                            }
                            else
                            {
                                AppealMgr.MessageToClient(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Appeal.BeingHelped"));
                                break;
                            }
                        }

                        AppealMgr.MessageToClient(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Appeal.DoesntHaveAppeal"));
                        break;
                    }
                #endregion gmappeal assist
                #region gmappeal view

                case "view":
                    {
                        if (args.Length < 3)
                        {
                            DisplaySyntax(client);
                            return;
                        }

                        string targetName = args[2];
                        GamePlayer playerTarget = ClientService.GetPlayerByPartialName(targetName, out ClientService.PlayerGuessResult result);

                        switch (result)
                        {
                            case ClientService.PlayerGuessResult.NOT_FOUND:
                            {
                                AppealMgr.MessageToClient(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Appeal.PlayerNotFound", targetName));
                                return;
                            }
                            case ClientService.PlayerGuessResult.FOUND_MULTIPLE:
                            {
                                AppealMgr.MessageToClient(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Appeal.NameNotUnique"));
                                return;
                            }
                            case ClientService.PlayerGuessResult.FOUND_EXACT:
                            case ClientService.PlayerGuessResult.FOUND_PARTIAL:
                                break;
                        }

                        DbAppeal appeal = AppealMgr.GetAppealByPlayerName(playerTarget.Name);

                        if (appeal != null)
                        {
                            //Let's view it.
                            List<string> msg = new()
                            {
                                $"[Appeal]: {appeal.Name}, [Status]: {appeal.Status}, [Priority]: {appeal.SeverityToName}, [Issue]: {appeal.Text}, [Time]: {appeal.Timestamp}.\n",
                                "To assist them with the appeal use /gmappeal assist <player name>.\n",
                                "To jump yourself to the player use /gmappeal jumpto.\n",
                                "For a full list of possible commands, use /gmappeal (with no arguments)"
                            };

                            client.Out.SendCustomTextWindow("Viewing " + appeal.Name + "'s Appeal", msg);
                            return;
                        }

                        AppealMgr.MessageToClient(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Appeal.DoesntHaveAppeal"));
                        break;
                    }
                #endregion gmappeal view
                #region gmappeal release
                case "release":
                    {
                        if (args.Length < 3)
                        {
                            DisplaySyntax(client);
                            return;
                        }

                        string targetName = args[2];
                        GamePlayer playerTarget = ClientService.GetPlayerByPartialName(targetName, out ClientService.PlayerGuessResult result);

                        switch (result)
                        {
                            case ClientService.PlayerGuessResult.NOT_FOUND:
                            {
                                AppealMgr.MessageToClient(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Appeal.PlayerNotFound", targetName));
                                return;
                            }
                            case ClientService.PlayerGuessResult.FOUND_MULTIPLE:
                            {
                                AppealMgr.MessageToClient(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Appeal.NameNotUnique"));
                                return;
                            }
                            case ClientService.PlayerGuessResult.FOUND_EXACT:
                            case ClientService.PlayerGuessResult.FOUND_PARTIAL:
                                break;
                        }

                        DbAppeal appeal = AppealMgr.GetAppealByPlayerName(playerTarget.Name);

                        if (appeal != null)
                        {
                            if (appeal.Status == "Being Helped")
                            {
                                AppealMgr.ChangeStatus(client.Player.Name, playerTarget, appeal, "Open");
                                client.Player.TempProperties.RemoveProperty("AppealAssist");
                                return;
                            }
                            else
                            {
                                AppealMgr.MessageToClient(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Appeal.NotBeingHelped"));
                                return;
                            }
                        }

                        AppealMgr.MessageToClient(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Appeal.DoesntHaveAppeal"));
                        return;
                    }
                #endregion gmappeal release
                #region gmappeal list
                case "list":
                case "listall":
                    {

                        int low = 0;
                        int med = 0;
                        int high = 0;
                        int crit = 0;
                        string caption;
                        IList<DbAppeal> appeallist;
                        List<string> msg = new List<string>();

                        if (args[1] == "listall")
                        {
                            caption = "Offline and Online Player Appeals";
                            appeallist = AppealMgr.GetAllAppealsOffline();
                        }
                        else
                        {
                            caption = "Online Player Appeals";
                            appeallist = AppealMgr.GetAllAppeals();
                        }

                        if (appeallist.Count < 1 || appeallist == null)
                        {
                            AppealMgr.MessageToClient(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Appeal.NoAppealsinQueue"));
                            return;
                        }

                        foreach (DbAppeal a in appeallist)
                        {
                            switch (a.Severity)
                            {
                                case (int)AppealMgr.eSeverity.Low:
                                    low++;
                                    break;
                                case (int)AppealMgr.eSeverity.Medium:
                                    med++;
                                    break;
                                case (int)AppealMgr.eSeverity.High:
                                    high++;
                                    break;
                                case (int)AppealMgr.eSeverity.Critical:
                                    crit++;
                                    break;
                            }
                        }
                        int total = appeallist.Count;
                        msg.Add(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Appeal.CurrentStaffAvailable", AppealMgr.StaffList.Count, total) + "\n");
                        msg.Add("Appeals ordered by severity: ");
                        msg.Add("Critical:" + crit + ", High:" + high + " Med:" + med + ", Low:" + low + ".\n");
                        if (crit > 0)
                        {
                            msg.Add("Critical priority appeals:\n");
                            foreach (DbAppeal a in appeallist)
                            {
                                if (a.Severity == (int)AppealMgr.eSeverity.Critical)
                                {
                                    msg.Add("[Name]: " + a.Name + ", [Status]: " + a.Status + ", [Priority]: " + a.SeverityToName + " [Issue]: " + a.Text + ", [Time]: " + a.Timestamp + ".\n");
                                }
                            }
                        }
                        if (high > 0)
                        {
                            msg.Add("High priority appeals:\n");
                            foreach (DbAppeal a in appeallist)
                            {
                                if (a.Severity == (int)AppealMgr.eSeverity.High)
                                {
                                    msg.Add("[Name]: " + a.Name + ", [Status]: " + a.Status + ", [Priority]: " + a.SeverityToName + ", [Issue]: " + a.Text + ", [Time]: " + a.Timestamp + ".\n");
                                }
                            }
                        }
                        if (med > 0)
                        {
                            msg.Add("Medium priority Appeals:\n");
                            foreach (DbAppeal a in appeallist)
                            {
                                if (a.Severity == (int)AppealMgr.eSeverity.Medium)
                                {
                                    msg.Add("[Name]: " + a.Name + ", [Status]: " + a.Status + ", [Priority]: " + a.SeverityToName + ", [Issue]: " + a.Text + ", [Time]: " + a.Timestamp + ".\n");
                                }
                            }
                        }
                        if (low > 0)
                        {
                            msg.Add("Low priority appeals:\n");
                            foreach (DbAppeal a in appeallist)
                            {
                                if (a.Severity == (int)AppealMgr.eSeverity.Low)
                                {
                                    msg.Add("[Name]: " + a.Name + ", [Status]: " + a.Status + ", [Priority]: " + a.SeverityToName + ", [Issue]: " + a.Text + ", [Time]: " + a.Timestamp + ".\n");
                                }
                            }
                        }
                        client.Out.SendCustomTextWindow(caption, msg);
                    }

                    break;
                #endregion gmappeal list
                #region gmappeal close
                case "close":
                    {
                        if (args.Length < 3)
                        {
                            DisplaySyntax(client);
                            return;
                        }

                        string targetName = args[2];
                        GamePlayer player = ClientService.GetPlayerByPartialName(targetName, out ClientService.PlayerGuessResult result);

                        switch (result)
                        {
                            case ClientService.PlayerGuessResult.NOT_FOUND:
                            {
                                AppealMgr.MessageToClient(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Appeal.PlayerNotFound", targetName));
                                return;
                            }
                            case ClientService.PlayerGuessResult.FOUND_MULTIPLE:
                            {
                                AppealMgr.MessageToClient(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Appeal.NameNotUnique"));
                                return;
                            }
                            case ClientService.PlayerGuessResult.FOUND_EXACT:
                            case ClientService.PlayerGuessResult.FOUND_PARTIAL:
                                break;
                        }

                        DbAppeal appeal = AppealMgr.GetAppealByPlayerName(player.Name);

                        if (appeal == null)
                        {
                            AppealMgr.MessageToClient(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Appeal.DoesntHaveAppeal"));
                            return;
                        }

                        AppealMgr.CloseAppeal(client.Player.Name, player, appeal);
                        client.Player.TempProperties.RemoveProperty("AppealAssist");
                        return;
                    }

                #endregion gmappeal close
                #region gmappeal closeoffline
                case "closeoffline":
                    {
                        if (args.Length < 3)
                        {
                            DisplaySyntax(client);
                            return;
                        }
                        string targetName = args[2];
                        DbAppeal appeal = AppealMgr.GetAppealByPlayerName(targetName);
                        if (appeal == null)
                        {
                            AppealMgr.MessageToClient(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Appeal.CantFindAppeal"));
                            return;
                        }
                        AppealMgr.CloseAppeal(client.Player.Name, appeal);

                        //just incase the player is actually online let's check so we can handle it properly
                        string targetNameTwo = args[2];
                        GamePlayer targetPlayer = ClientService.GetPlayerByPartialName(targetNameTwo, out ClientService.PlayerGuessResult resultTwo);

                        switch (resultTwo)
                        {
                            case ClientService.PlayerGuessResult.NOT_FOUND:
                                return; // player isn't online so we're fine.
                            case ClientService.PlayerGuessResult.FOUND_MULTIPLE:
                            {
                                AppealMgr.MessageToClient(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Appeal.NameNotUnique"));
                                return;
                            }
                            case ClientService.PlayerGuessResult.FOUND_EXACT:
                            case ClientService.PlayerGuessResult.FOUND_PARTIAL:
                            {
                                //cleaning up the player since he really was online.
                                AppealMgr.MessageToClient(targetPlayer.Client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Appeal.StaffClosedYourAppeal", client.Player.Name));
                                targetPlayer.Out.SendPlaySound(eSoundType.Craft, 0x02);
                                targetPlayer.TempProperties.SetProperty("HasPendingAppeal", false);
                                break;
                            }
                        }

                        break;
                    }

                #endregion gmappeal closeoffline
                #region gmappeal jumpto
                case "jumpto":
                    {
                        try
                        {
                            GamePlayer p = client.Player.TempProperties.GetProperty<GamePlayer>("AppealAssist");
                            if (p.ObjectState == GameObject.eObjectState.Active)
                            {
                                GameLocation oldlocation = new GameLocation("old", client.Player.CurrentRegionID, client.Player.X, client.Player.Y, client.Player.Z);
                                client.Player.TempProperties.SetProperty("AppealJumpOld", oldlocation);
                                client.Player.MoveTo(p.CurrentRegionID, p.X, p.Y, p.Z, p.Heading);
                            }
                            break;
                        }
                        catch
                        {
                            AppealMgr.MessageToClient(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Appeal.MustBeAssisting"));
                            break;
                        }
                    }
                case "jumpback":
                    {
                        GameLocation jumpback = client.Player.TempProperties.GetProperty<GameLocation>("AppealJumpOld");

                        if (jumpback != null)
                        {
                            client.Player.MoveTo(jumpback);
                            //client.Player.TempProperties.removeProperty("AppealJumpOld");
                            break;
                        }
                        else
                        {
                            AppealMgr.MessageToClient(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Appeal.NoLocationToJump"));
                        }
                        break;
                    }

                #endregion gmappeal jumpto
                #region gmappeal mute
                case "mute":
                    {
                        bool mute = client.Player.TempProperties.GetProperty<bool>("AppealMute");
                        if (mute == false)
                        {
                            client.Player.TempProperties.SetProperty("AppealMute", true);
                            AppealMgr.MessageToClient(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Appeal.NoLongerReceiveMsg"));
                            AppealMgr.MessageToClient(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Appeal.UseCmdTurnBackOn"));
                            AppealMgr.StaffList.Remove(client.Player);
                        }
                        else
                        {
                            client.Player.TempProperties.SetProperty("AppealMute", false);
                            AppealMgr.MessageToClient(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Appeal.NowReceiveMsg"));
                            AppealMgr.MessageToClient(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Appeal.UseCmdTurnBackOff"));
                            AppealMgr.StaffList.Add(client.Player);
                        }

                        break;
                    }
                #endregion gmappeal mute
                #region gmappeal commands
                case "commands":
                case "cmds":
                case "help":
                    //List all the commands in a pop up window
                    List<string> helpmsg = new List<string>();
                    helpmsg.Add("Commands for server staff to assist players with their Appeals.");
                    helpmsg.Add("/gmappeal view <player name> - Views the appeal of a specific player.");
                    helpmsg.Add("/gmappeal list - Lists all the current Appeals from online players only, in a window.");
                    helpmsg.Add("/gmappeal listall - Will list Appeals of both offline and online players, in a window.");
                    helpmsg.Add("/gmappeal assist <player name> - Take ownership of the player's appeal and lets other staff know you are helping this player.");
                    helpmsg.Add("/gmappeal jumpto - Will jump you to the player you are currently assisting (must use /gmappeal assist first).");
                    helpmsg.Add("/gmappeal jumpback - Will jump you back to where you were after you've helped the player (must use /gmappeal jumpto first).");
                    helpmsg.Add("/gmappeal close <player name> - Closes the appeal and removes it from the queue.");
                    helpmsg.Add("/gmappeal closeoffline <player name> - Closes an appeal of a player who is not online.");
                    helpmsg.Add("/gmappeal release <player name> - Releases ownership of the player's appeal so someone else can help them.");
                    helpmsg.Add("/gmappeal mute - Toggles receiving appeal notices, for yourself, for this session.");
                    client.Out.SendCustomTextWindow("/gmappeal commands list", helpmsg);
                    break;
                #endregion gmappeal commands
                default:
                    {
                        DisplaySyntax(client);
                        return;
                    }
            }
            return;
        }
    }
}
