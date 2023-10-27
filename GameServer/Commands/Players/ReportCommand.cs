using Core.Database.Tables;
using Core.GS.Enums;
using Core.GS.Scripts;
using Core.GS.Server;

namespace Core.GS.Commands;

[Command(
	"&report",
	EPrivLevel.Player,
	"'Reports a bug",
	"'Usage: /report <message>  Please be as detailed as possible.")]
public class ReportCommand : ACommandHandler, ICommandHandler
{
	private const ushort MAX_REPORTS = 100;
	
	public void OnCommand(GameClient client, string[] args)
	{
		if (ServerProperty.DISABLE_BUG_REPORTS)
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
		DbBugReport report = new DbBugReport();

		if (ServerProperty.MAX_BUGREPORT_QUEUE > 0)
		{
			//Andraste
			var reports = GameServer.Database.SelectAllObjects<DbBugReport>();
			bool found = false; int i = 0;
			for (i = 0; i < ServerProperty.MAX_BUGREPORT_QUEUE; i++)
			{
				found = false;
				foreach (DbBugReport rep in reports) if (rep.ID == i) found = true;
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
			report.ID = GameServer.Database.GetObjectCount<DbBugReport>() + 1;
		}
		
		report.Message = message;
		report.Submitter = client.Player.Name + " [" + client.Account.Name + "]";
		GameServer.Database.AddObject(report);
		client.Player.Out.SendMessage("Report submitted, if this is not a bug report it will be ignored!", EChatType.CT_System, EChatLoc.CL_SystemWindow);

		if (ServerProperty.BUG_REPORT_EMAIL_ADDRESSES.Trim() != "")
		{
			if (client.Account.Mail == "")
				client.Player.Out.SendMessage("If you enter your email address for your account with /email command, your bug reports will send an email to the staff!", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
			else
			{
				MailMgr.SendMail(ServerProperty.BUG_REPORT_EMAIL_ADDRESSES, GameServer.Instance.Configuration.ServerName + " bug report " + report.ID, report.Message, report.Submitter, client.Account.Mail);
			}
		}
	}
}