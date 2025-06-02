using System.Collections.Generic;
using DOL.Database;
using DOL.GS.Appeal;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Commands
{
	[CmdAttribute(
		"&checkappeal",
		ePrivLevel.Player,
		"Checks the status of your appeal or cancels it.",
		"Usage:",
		"/checkappeal view - View your appeal status.",
		"/checkappeal cancel - Cancel your appeal and remove it from the queue.",
		"Use /appeal to file an appeal.")]

	public class CheckAppealCommandHandler : AbstractCommandHandler, ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{
			if (IsSpammingCommand(client.Player, "checkappeal"))
				return;

			if (ServerProperties.Properties.DISABLE_APPEALSYSTEM)
			{
				//AppealMgr.MessageToClient(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Appeal.SystemDisabled"));
				client.Out.SendMessage("The /appeal system has moved to Discord. Use the #appeal channel on our Discord to be assisted on urgent matters.",eChatType.CT_Staff,eChatLoc.CL_SystemWindow);
				return;
			}

			if (args.Length < 2)
			{
				DisplaySyntax(client);
				return;
			}

			switch (args[1])
			{

					#region checkappeal cancel
				case "remove":
				case "delete":
				case "cancel":
					{
						if (args.Length < 2)
						{
							DisplaySyntax(client);
							return;
						}
						DbAppeal appeal = AppealMgr.GetAppeal(client.Player);
						if (appeal != null)
						{
							if (appeal.Status == "Being Helped")
							{
								AppealMgr.MessageToClient(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Appeal.CantCancelWhile"));
								return;
							}
							else
							{
								AppealMgr.CancelAppeal(client.Player, appeal);
								break;
							}
						}
						AppealMgr.MessageToClient(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Appeal.DoNotHaveAppeal"));
						break;
					}
					#endregion checkappeal cancel
					#region checkappeal view
				case "display":
				case "show":
				case "list":
				case "view":
					{
						if (args.Length < 2)
						{
							DisplaySyntax(client);
							return;
						}
						DbAppeal appeal = AppealMgr.GetAppeal(client.Player);
						if (appeal != null)
						{
							//Let's view it.
							List<string> msg = new List<string>();
							//note: we do not show the player his Appeals priority.
							msg.Add("[Player]: " + appeal.Name + ", [Status]: " + appeal.Status + ", [Issue]: " + appeal.Text + ", [Time]: " + appeal.Timestamp + ".\n");
							msg.Add(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Appeal.CurrentStaffAvailable", AppealMgr.GetAvailableStaffMembers().Count + "\n"));
							msg.Add(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Appeal.PleaseBePatient") + "\n");
							msg.Add(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Appeal.IfYouLogOut") + "\n");
							msg.Add(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Appeal.ToCancelYourAppeal"));
							client.Out.SendCustomTextWindow("Your Appeal", msg);
							return;
						}
						AppealMgr.MessageToClient(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Appeal.NoAppealToView"));
						break;

					}
				default:
					{
						DisplaySyntax(client);
						return;
					}
					#endregion checkappeal view

			}
		}
	}
}
