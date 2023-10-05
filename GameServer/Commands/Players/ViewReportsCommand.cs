using System;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Commands;

[Command(
	"&viewreports",
	ePrivLevel.Player,
	"Allows you to view submitted bug reports.",
	"/viewreports")]
public class ViewReportsCommand : ACommandHandler, ICommandHandler
{
	public void OnCommand(GameClient client, string[] args)
	{
		if (IsSpammingCommand(client.Player, "viewreports"))
			return;

		try
		{
			// We recieved args, and must be admin
			switch (args[1])
			{
				case "close":
					{
						if (client.Account.PrivLevel < 2)
						{
							client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.ViewReport.NoPriv"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
							return;
						}
						if (args[2] == "")
						{
							client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.ViewReport.Help.Close"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
							break;
						}

						int repor = int.Parse(args[2]);
						DbBugReport report = GameServer.Database.FindObjectByKey<DbBugReport>(repor);
						if (report == null)
						{
							client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.ViewReport.InvalidReport"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
							break;
						}
						report.ClosedBy = client.Player.Name;
						report.DateClosed = DateTime.Now;
						GameServer.Database.SaveObject(report);
						break;
					}
				case "delete":
					{
						if (client.Account.PrivLevel < 2)
						{
							client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.ViewReport.NoPriv"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
							return;
						}
						if (args[2] == "")
						{
							client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.ViewReport.Help.Delete"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
							break;
						}
						int repor = int.Parse(args[2]);
						DbBugReport report = GameServer.Database.FindObjectByKey<DbBugReport>(repor);
						if (report == null)
						{
							client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.ViewReport.InvalidReport"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
							break;
						}
						// Create a counter to keep track of our BugReport ID
						int count = 1;
						GameServer.Database.DeleteObject(report);
						// Get all Database'd Bug Reports since we have deleted one
						var bugReports = GameServer.Database.SelectAllObjects<DbBugReport>();
						foreach (DbBugReport curReport in bugReports)
						{
							// Create new DB for bugreports without the one we deleted
							curReport.ID = count;
							GameServer.Database.SaveObject(curReport);
							count++;
						}
						client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.ViewReport.ReportDeleted", report.ID), eChatType.CT_System, eChatLoc.CL_SystemWindow);
						break;
					}
				default:
					{
						client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.ViewReport.UnknownCommand"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
						DisplayHelp(client);
					}
					break;
			} //switch
			return;
		}
		catch (Exception e)
		{
			e = new Exception();
			// Display bug reports to player
			string Reports = "---------- BUG REPORTS ------------\n";
			var dbo = GameServer.Database.SelectAllObjects<DbBugReport>();
			if (dbo.Count < 1)
			{
				Reports += "  - No Reports On File -\n";
				return;
			}

			foreach (DbBugReport repo in dbo)
			{
				Reports += repo.ID + ")";
				if (client.Account.PrivLevel > 2)
					Reports += repo.Submitter + "\n";
				Reports += "Submitted: " + repo.DateSubmitted + "\n";
				Reports += "Report: " + repo.Message + "\n";
				Reports += "Closed By: " + repo.ClosedBy + "\n";
				Reports += "Date Closed: " + repo.DateClosed + "\n\n";
				client.Out.SendMessage(Reports, eChatType.CT_Important, eChatLoc.CL_PopupWindow);
				Reports = "";
			}
		}
	}

	public void DisplayHelp(GameClient client)
	{
		client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.ViewReport.Usage"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
		client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.ViewReport.Help.Close"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
		client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.ViewReport.Help.Delete"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
	}
}