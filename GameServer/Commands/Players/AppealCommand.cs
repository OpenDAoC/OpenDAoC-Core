﻿
using Core.GS.Events;
using DOL.Language;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;

namespace DOL.GS.Commands
{
    //The appeal command is really just a tool to redirect the players concerns to the proper command.
    //most of it's functionality is built into the client.
    [Command(
        "&appeal",
        EPrivLevel.Player,
        "/appeal")]
    
        // "Usage: '/appeal <appeal type> <appeal text>",
        // "Where <appeal type> is one of the following:",
        // "  Harassment, Naming, Conduct, Stuck, Emergency or Other",
        // "and <appeal text> is a description of your issue.",
        // "If you have submitted an appeal, you can check its",
        // "status by typing '/checkappeal'."
    public class AppealCommand : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
			if (IsSpammingCommand(client.Player, "appeal"))
				return;

			if (ServerProperties.ServerProperties.DISABLE_APPEALSYSTEM)
            {
                //AppealMgr.MessageToClient(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Appeal.SystemDisabled"));
                client.Out.SendMessage("The /appeal system has moved to Discord. Use the #appeal channel on our Discord to be assisted on urgent matters.",EChatType.CT_Staff,EChatLoc.CL_SystemWindow);
                return;
            }

			if (client.Player.IsMuted)
			{
				return;
			}
            
            //Help display
            if (args.Length == 1)
            {
                DisplaySyntax(client);
                if (client.Account.PrivLevel > (uint)EPrivLevel.Player)
                {
                    AppealMgr.MessageToClient(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Appeal.UseGMappeal"));
                }
            }
            
            //Support for EU Clients
            
            if (args.Length == 2 && args[1].ToLower() == "cancel")
            {
                CheckAppealCommand cch = new CheckAppealCommand();
                cch.OnCommand(client, args);
                return;
            }
            
            if (args.Length > 1)
            {
                bool HasPendingAppeal = client.Player.TempProperties.getProperty<bool>("HasPendingAppeal");
                if (HasPendingAppeal)
                {
                    AppealMgr.MessageToClient(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Appeal.AlreadyActiveAppeal", client.Player.Name));
                    return;
                }
                if (args.Length < 5)
                {
                    AppealMgr.MessageToClient(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Appeal.NeedMoreDetail"));
                    return;
                }
                int severity = 0;
                switch (args[1].ToLower())
                {
                    case "harassment":
                        {
                            severity = (int)AppealMgr.eSeverity.High;
                            args[1] = "";
                            break;
                        }
                    case "naming":
                        {
                            severity = (int)AppealMgr.eSeverity.Low;
                            args[1] = "";
                            break;
                        }
                    case "other":
                    case "conduct":
                        {
                            severity = (int)AppealMgr.eSeverity.Medium;
                            args[1] = "";
                            break;
                        }
                    case "stuck":
                    case "emergency":
                        {
                            severity = (int)AppealMgr.eSeverity.Critical;
                            args[1] = "";
                            break;
                        }
                    default:
                        {
                            severity = (int)AppealMgr.eSeverity.Medium;
                            break;
                        }
            
                }
                string message = string.Join(" ", args, 1, args.Length - 1);
                GamePlayer p = client.Player as GamePlayer;
                AppealMgr.CreateAppeal(p, severity, "Open", message);
                return;
            }
            return;
        }
    }
    
    #region reportbug
    //handles /reportbug command that is issued from the client /appeal function.
    [Command(
    "&reportbug",
    EPrivLevel.Player, "Use /appeal to file an appeal")]
    public class ReportBugCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if (ServerProperties.ServerProperties.DISABLE_APPEALSYSTEM)
            {
                AppealMgr.MessageToClient(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Appeal.SystemDisabled"));
                return;
            }
    
            if (args.Length < 5)
            {
                AppealMgr.MessageToClient(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Appeal.NeedMoreDetail"));
                return;
            }
    
            //send over the info to the /report command
            args[1] = "";
            //strip these words if they are the first word in the bugreport text
            switch (args[2].ToLower())
            {
                case "harassment":
                case "naming":
                case "other":
                case "conduct":
                case "stuck":
                case "emergency":
                    {
                        args[2] = "";
                        break;
                    }
            }
            ReportCommand report = new ReportCommand();
            report.OnCommand(client, args);
            AppealMgr.MessageToAllStaff(client.Player.Name + " submitted a bug report.");
            return;
        }
    }
    #endregion reportbug
    #region reportharass
    //handles /reportharass command that is issued from the client /appeal function.
    [Command(
    "&reportharass",
    EPrivLevel.Player, "Use /appeal to file an appeal")]
    public class ReportHarassCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if (ServerProperties.ServerProperties.DISABLE_APPEALSYSTEM)
            {
                AppealMgr.MessageToClient(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Appeal.SystemDisabled"));
                return;
            }
            bool HasPendingAppeal = client.Player.TempProperties.getProperty<bool>("HasPendingAppeal");
            if (HasPendingAppeal)
            {
                AppealMgr.MessageToClient(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Appeal.AlreadyActiveAppeal", client.Player.Name));
                return;
            }
            if (args.Length < 7)
            {
                AppealMgr.MessageToClient(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Appeal.NeedMoreDetail"));
                return;
            }
            //strip these words if they are the first word in the appeal text
            switch (args[1].ToLower())
            {
                case "harassment":
                case "naming":
                case "other":
                case "conduct":
                case "stuck":
                case "emergency":
                    {
                        args[1] = "";
                        break;
                    }
            }
            string message = string.Join(" ", args, 1, args.Length - 1);
            GamePlayer p = client.Player as GamePlayer;
            AppealMgr.CreateAppeal(p, (int)AppealMgr.eSeverity.High, "Open", message);
            return;
        }
    }
    #endregion reportharass
    #region reporttos
    //handles /reporttos command that is issued from the client /appeal function.
    [Command(
    "&reporttos",
    EPrivLevel.Player, "Use /appeal to file an appeal")]
    public class ReportTosCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if (ServerProperties.ServerProperties.DISABLE_APPEALSYSTEM)
            {
                AppealMgr.MessageToClient(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Appeal.SystemDisabled"));
                return;
            }
            bool HasPendingAppeal = client.Player.TempProperties.getProperty<bool>("HasPendingAppeal");
            if (HasPendingAppeal)
            {
                AppealMgr.MessageToClient(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Appeal.AlreadyActiveAppeal", client.Player.Name));
                return;
            }
            if (args.Length < 7)
            {
                AppealMgr.MessageToClient(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Appeal.NeedMoreDetail"));
                return;
            }
            switch (args[1])
            {
                case "NAME":
                    {
                        //strip these words if they are the first word in the appeal text
                        switch (args[2].ToLower())
                        {
                            case "harassment":
                            case "naming":
                            case "other":
                            case "conduct":
                            case "stuck":
                            case "emergency":
                                {
                                    args[2] = "";
                                    break;
                                }
                        }
                        string message = string.Join(" ", args, 2, args.Length - 2);
                        GamePlayer p = client.Player as GamePlayer;
                        AppealMgr.CreateAppeal(p, (int)AppealMgr.eSeverity.Low, "Open", message);
                        break;
                    }
                case "TOS":
                    {
                        //strip these words if they are the first word in the appeal text
                        switch (args[2].ToLower())
                        {
                            case "harassment":
                            case "naming":
                            case "other":
                            case "conduct":
                            case "stuck":
                            case "emergency":
                                {
                                    args[2] = "";
                                    break;
                                }
                        }
                        string message = string.Join(" ", args, 2, args.Length - 2);
                        GamePlayer p = client.Player as GamePlayer;
                        AppealMgr.CreateAppeal(p, (int)AppealMgr.eSeverity.Medium, "Open", message);
                        break;
                    }
            }
            return;
        }
    }
    #endregion reporttos
    #region reportstuck
    //handles /reportharass command that is issued from the client /appeal function.
    [Command(
    "&reportstuck",
    EPrivLevel.Player, "Use /appeal to file an appeal")]
    public class ReportStuckCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if (ServerProperties.ServerProperties.DISABLE_APPEALSYSTEM)
            {
                AppealMgr.MessageToClient(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Appeal.SystemDisabled"));
                return;
            }
            bool HasPendingAppeal = client.Player.TempProperties.getProperty<bool>("HasPendingAppeal");
            if (HasPendingAppeal)
            {
                AppealMgr.MessageToClient(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Appeal.AlreadyActiveAppeal", client.Player.Name));
                return;
            }
            if (args.Length < 5)
            {
                AppealMgr.MessageToClient(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Appeal.NeedMoreDetail"));
                return;
            }
            //strip these words if they are the first word in the appeal text
            switch (args[1].ToLower())
            {
                case "harassment":
                case "naming":
                case "other":
                case "conduct":
                case "stuck":
                case "emergency":
                    {
                        args[1] = "";
                        break;
                    }
            }
            string message = string.Join(" ", args, 1, args.Length - 1);
            GamePlayer p = client.Player as GamePlayer;
            AppealMgr.CreateAppeal(p, (int)AppealMgr.eSeverity.Critical, "Open", message);
            return;
        }
    }
    #endregion reportstuck
    #region emergency
    //handles /appea command that is issued from the client /appeal function (emergency appeal).
    [Command(
    "&appea",
    EPrivLevel.Player, "Use /appeal to file an appeal")]
    public class EmergencyAppealCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if (ServerProperties.ServerProperties.DISABLE_APPEALSYSTEM)
            {
                AppealMgr.MessageToClient(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Appeal.SystemDisabled"));
                return;
            }
            bool HasPendingAppeal = client.Player.TempProperties.getProperty<bool>("HasPendingAppeal");
            if (HasPendingAppeal)
            {
                AppealMgr.MessageToClient(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Appeal.AlreadyActiveAppeal", client.Player.Name));
                return;
            }
            if (args.Length < 5)
            {
                AppealMgr.MessageToClient(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Appeal.NeedMoreDetail"));
                return;
            }
            //strip these words if they are the first word in the appeal text
            switch (args[1].ToLower())
            {
                case "harassment":
                case "naming":
                case "other":
                case "conduct":
                case "stuck":
                case "emergency":
                    {
                        args[1] = "";
                        break;
                    }
            }
            string message = string.Join(" ", args, 1, args.Length - 1);
            GamePlayer p = client.Player as GamePlayer;
            AppealMgr.CreateAppeal(p, (int)AppealMgr.eSeverity.Critical, "Open", message);
            return;
        }
    }
    #endregion emergency
}