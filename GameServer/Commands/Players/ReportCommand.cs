
using System;

using DOL.GS.PacketHandler;
using DOL.GS.GameEvents;
using DOL.Database;

namespace DOL.GS.Commands
{
	[Command(
		"&report",
		EPrivLevel.Player,
		"'Reports a bug",
		"'Usage: /report <message>  Please be as detailed as possible.")]
	public class ReportCommand : AbstractCommandHandler, ICommandHandler
	{
		private const ushort MAX_REPORTS = 100;
		
		public void OnCommand(GameClient client, string[] args)
		{
			if (ServerProperties.ServerProperties.DISABLE_BUG_REPORTS)
			{
				DisplayMessage(client, "Bug reporting has been disabled for this server!");
				return;
			}

			if (IsSpammingCommand(client.Player, "report"))
				return;

			if (args.Length < 2)
			{
				DisplaySyntax(client);
				return;
			}

			if (client.Player.IsMuted)
			{
				client.Player.Out.SendMessage("You have been muted and are not allowed to submit bug reports.", EChatType.CT_Staff, EChatLoc.CL_SystemWindow);
				return;
			}

			string message = string.Join(" ", args, 1, args.Length - 1);
			DbBugReports report = new DbBugReports();

			if (ServerProperties.ServerProperties.MAX_BUGREPORT_QUEUE > 0)
			{
				//Andraste
				var reports = GameServer.Database.SelectAllObjects<DbBugReports>();
				bool found = false; int i = 0;
				for (i = 0; i < ServerProperties.ServerProperties.MAX_BUGREPORT_QUEUE; i++)
				{
					found = false;
					foreach (DbBugReports rep in reports) if (rep.ID == i) found = true;
					if (!found) break;
				}
				if (found)
				{
					client.Player.Out.SendMessage("There are too many reports, please contact a GM or wait until they are cleaned.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
					return;
				}

				report.ID = i;
			}
			else
			{
				// This depends on bugs never being deleted from the report table!
				report.ID = GameServer.Database.GetObjectCount<DbBugReports>() + 1;
			}
			
			report.Message = message;
			report.Submitter = client.Player.Name + " [" + client.Account.Name + "]";
			GameServer.Database.AddObject(report);
			client.Player.Out.SendMessage("Report submitted, if this is not a bug report it will be ignored!", EChatType.CT_System, EChatLoc.CL_SystemWindow);

			if (ServerProperties.ServerProperties.BUG_REPORT_EMAIL_ADDRESSES.Trim() != "")
			{
				if (client.Account.Mail == "")
					client.Player.Out.SendMessage("If you enter your email address for your account with /email command, your bug reports will send an email to the staff!", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
				else
				{
					Mail.MailMgr.SendMail(ServerProperties.ServerProperties.BUG_REPORT_EMAIL_ADDRESSES, GameServer.Instance.Configuration.ServerName + " bug report " + report.ID, report.Message, report.Submitter, client.Account.Mail);
				}
			}
		}
	}
}