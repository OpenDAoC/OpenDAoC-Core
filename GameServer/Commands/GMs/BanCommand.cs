using System;
using System.Reflection;
using Core.Database;
using Core.Database.Tables;
using Core.GS.Database;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.PacketHandler;
using Core.Language;
using log4net;

namespace Core.GS.Commands;

[Command(
	"&ban",
	EPrivLevel.GM,
	"GMCommands.Ban.Description",
	"GMCommands.Ban.Usage.IP",
	"GMCommands.Ban.Usage.Account",
	"GMCommands.Ban.Usage.Both",
	"#<ClientID> can be used in place of player name.  Use /clientlist to see playing clients."
)]
public class BanCommand : ACommandHandler, ICommandHandler
{
	private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

	public void OnCommand(GameClient client, string[] args)
	{
		if (args.Length < 3)
		{
			DisplaySyntax(client);
			return;
		}

		GameClient gc = null;

		if (args[2].StartsWith("#"))
		{
			try
			{
				int sessionID = Convert.ToInt32(args[1][1..]);
				gc = ClientService.GetClientFromId(sessionID);
			}
			catch
			{
				DisplayMessage(client, "Invalid client ID");
			}
		}
		else
		{
			gc = ClientService.GetPlayerByExactName(args[1])?.Client;
		}

		var acc = gc != null ? gc.Account : CoreDb<DbAccount>.SelectObject(DB.Column("Name").IsLike(args[2]));
		if (acc == null)
		{
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Ban.UnableToFindPlayer"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
			return;
		}

		if (client.Account.PrivLevel < acc.PrivLevel)
		{
			DisplayMessage(client, "Your privlevel is not high enough to ban this player.");
			return;
		}

		if (client.Account.Name == acc.Name)
		{
			DisplayMessage(client, "Your can't ban yourself!");
			return;
		}

		try
		{
			DbBans b = new DbBans
			                    {
				                    DateBan = DateTime.Now,
				                    Author = client.Player.Name,
				                    Ip = acc.LastLoginIP,
				                    Account = acc.Name
			                    };

			if (args.Length >= 4)
				b.Reason = String.Join(" ", args, 3, args.Length - 3);
			else
				b.Reason = "No Reason.";

			switch (args[1].ToLower())
			{
					#region Account
				case "account":
					var acctBans = CoreDb<DbBans>.SelectObjects(DB.Column("Type").IsEqualTo("A").Or(DB.Column("Type").IsEqualTo("B")).And(DB.Column("Account").IsEqualTo(acc.Name)));
					if (acctBans.Count > 0)
					{
						client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Ban.AAlreadyBanned"), EChatType.CT_Important, EChatLoc.CL_SystemWindow);
						return;
					}

					b.Type = "A";
					client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Ban.ABanned", acc.Name), EChatType.CT_Important, EChatLoc.CL_SystemWindow);
					break;
					#endregion Account
					#region IP
				case "ip":
					var ipBans = CoreDb<DbBans>.SelectObjects(DB.Column("Type").IsEqualTo("I").Or(DB.Column("Type").IsEqualTo("B")).And(DB.Column("Ip").IsEqualTo(acc.LastLoginIP)));
					if (ipBans.Count > 0)
					{
						client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Ban.IAlreadyBanned"), EChatType.CT_Important, EChatLoc.CL_SystemWindow);
						return;
					}

					b.Type = "I";
					client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Ban.IBanned", acc.LastLoginIP), EChatType.CT_Important, EChatLoc.CL_SystemWindow);
					break;
					#endregion IP
					#region Both
				case "both":
					var acctIpBans = CoreDb<DbBans>.SelectObjects(DB.Column("Type").IsEqualTo("B").And(DB.Column("Account").IsEqualTo(acc.Name)).And(DB.Column("Ip").IsEqualTo(acc.LastLoginIP)));
					if (acctIpBans.Count > 0)
					{
						client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Ban.BAlreadyBanned"), EChatType.CT_Important, EChatLoc.CL_SystemWindow);
						return;
					}

					b.Type = "B";
					client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Ban.BBanned", acc.Name, acc.LastLoginIP), EChatType.CT_Important, EChatLoc.CL_SystemWindow);
					break;
					#endregion Both
					#region Default
				default:
					{
						DisplaySyntax(client);
						return;
					}
					#endregion Default
			}
			GameServer.Database.AddObject(b);

			if (log.IsInfoEnabled)
				log.Info($"Ban added [{args[1].ToLower()}]: {acc.Name} ({acc.LastLoginIP})");
			return;
		}
		catch (Exception e)
		{
			if (log.IsErrorEnabled)
				log.Error("/ban Exception", e);
		}

		// if not returned here, there is an error
		DisplaySyntax(client);
	}
}