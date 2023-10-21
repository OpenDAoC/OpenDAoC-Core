using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Core.Database;
using Core.Database.Tables;
using Core.GS.Database;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.Languages;
using log4net;

namespace Core.GS.Players.Managers;

public static class AppealMgr
{
	/// <summary>
	/// Defines a logger for this class.
	/// </summary>
	private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

	private static int m_CallbackFrequency = 5 * 60 * 1000; // How often appeal stat updates are sent out.
	private static volatile Timer m_timer = null;
	public static List<GamePlayer> StaffList = new List<GamePlayer>();
	public static int TotalAppeals;

	#region Initialisation/Unloading
	[ScriptLoadedEvent]
	public static void OnScriptCompiled(CoreEvent e, object sender, EventArgs args)
	{
		if (!ServerProperties.Properties.DISABLE_APPEALSYSTEM)
		{

			//Register and load the DB.
			//Obsolete with GSS Table Registering in SVN : 3337
			//GameServer.Database.RegisterDataObject(typeof(DBAppeal));
			GameEventMgr.AddHandler(GamePlayerEvent.GameEntered, new CoreEventHandler(PlayerEnter));
			GameEventMgr.AddHandler(GamePlayerEvent.Quit, new CoreEventHandler(PlayerQuit));
			GameEventMgr.AddHandler(GamePlayerEvent.Linkdeath, new CoreEventHandler(PlayerQuit));
			m_timer = new Timer(new TimerCallback(RunTask), m_timer, 0, m_CallbackFrequency);
		}
	}

	[ScriptUnloadedEvent]
	public static void OnScriptUnloaded(CoreEvent e, object sender, EventArgs args)
	{
		if (!ServerProperties.Properties.DISABLE_APPEALSYSTEM)
		{
			GameEventMgr.RemoveHandler(GamePlayerEvent.GameEntered, new CoreEventHandler(PlayerEnter));
			GameEventMgr.RemoveHandler(GamePlayerEvent.Quit, new CoreEventHandler(PlayerQuit));
			GameEventMgr.RemoveHandler(GamePlayerEvent.Linkdeath, new CoreEventHandler(PlayerQuit));
			RunTask(null);
		}
	}
	#endregion

	#region Methods

	private static void RunTask(object state)
	{
		NotifyStaff();
		if (m_timer != null)
			m_timer.Change(m_CallbackFrequency, Timeout.Infinite);
		return;
	}

	public static void NotifyStaff()
	{
		//here we keep the staff up to date on the status of the appeal queue, if there are open Appeals.
		IList<DbAppeal> Appeals = GetAllAppeals();

		if (Appeals.Count == 0)
			return;

		int low = 0;
		int med = 0;
		int high = 0;
		int crit = 0;
		foreach (DbAppeal a in Appeals)
		{
			if (a == null) { return; }
			if (a.Severity < 1) { return; }
			switch (a.Severity)
			{
				case (int)EAppealSeverity.Low:
					low++;
					break;
				case (int)EAppealSeverity.Medium:
					med++;
					break;
				case (int)EAppealSeverity.High:
					high++;
					break;
				case (int)EAppealSeverity.Critical:
					crit++;
					break;
			}
		}
		//There are some Appeals to handle, let's send out an update to staff.
		if (Appeals.Count >= 2)
		{
			MessageToAllStaff("There are " + Appeals.Count + " Appeals in the queue.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
			MessageToAllStaff("There are " + Appeals.Count + " Appeals in the queue.", EChatType.CT_Say, EChatLoc.CL_ChatWindow);
		}
		if (Appeals.Count == 1)
		{
			MessageToAllStaff("There is " + Appeals.Count + " appeal in the queue.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
			MessageToAllStaff("There is " + Appeals.Count + " appeal in the queue.", EChatType.CT_Say, EChatLoc.CL_ChatWindow);
		}

		MessageToAllStaff("Crit:" + crit + ", High:" + high + ", Med:" + med + ", Low:" + low + ".  [use /gmappeal]", EChatType.CT_System, EChatLoc.CL_SystemWindow);
		MessageToAllStaff("Crit:" + crit + ", High:" + high + ", Med:" + med + ", Low:" + low + ".  [use /gmappeal]", EChatType.CT_Say, EChatLoc.CL_ChatWindow);

		if (crit >= 1)
		{
			MessageToAllStaff("Critical Appeals may need urgent attention!", EChatType.CT_YouDied, EChatLoc.CL_SystemWindow);
			log.Warn("There is a critical appeal which may need urgent attention!");
		}

	}

	public static void MessageToAllStaff(string msg)
	{
		if (msg == null) { return; }

		foreach (GamePlayer staffplayer in StaffList)
		{
			if (staffplayer.Client != null && staffplayer.Client.Player != null)
			{
				MessageToClient(staffplayer.Client, msg);
				// If GM has '/alert appeal on' set, receive audible alert when an appeal is submitted or requires assistance
				if (staffplayer.Client.Player.TempProperties.GetProperty<bool>("AppealAlert") == false)
				{
					staffplayer.Out.SendSoundEffect(2567, 0, 0, 0, 0, 0); // 2567 = Cat_Meow_08.wav
				}
			}
		}
		return;
	}

	public static void MessageToClient(GameClient client, string msg)
	{
		if (msg == null) return;
		if (client == null || client.Player == null) return;
		client.Player.Out.SendMessage("[Appeals]: " + msg, EChatType.CT_Important, EChatLoc.CL_ChatWindow);
		return;
	}

	public static void MessageToAllStaff(string msg, EChatType chattype, EChatLoc chatloc)
	{
		if (msg == null) { return; }

		foreach (GamePlayer staffplayer in StaffList)
		{
			staffplayer.Out.SendMessage("[Appeals]: " + msg, chattype, chatloc);
			// If GM has '/alert appeal on' set, receive audible alert when an appeal is submitted or requires assistance
			if (staffplayer?.Client?.Player?.TempProperties?.GetProperty<bool>("AppealAlert") == false)
			{
				staffplayer.Out.SendSoundEffect(2567, 0, 0, 0, 0, 0); // 2567 = Cat_Meow_08.wav
			}
		}
		return;
	}

	public static DbAppeal GetAppealByPlayerName(string name)
	{
		DbAppeal appeal = CoreDb<DbAppeal>.SelectObject(DB.Column("Name").IsEqualTo(name));
		return appeal;
	}

	public static DbAppeal GetAppealByAccountName(string name)
	{
		DbAppeal appeal = CoreDb<DbAppeal>.SelectObject(DB.Column("Account").IsEqualTo(name));
		return appeal;
	}

	/// <summary>
	/// Gets a combined list of Appeals for every player that is online.
	/// </summary>
	/// <returns></returns>
	public static IList<DbAppeal> GetAllAppeals()
	{
		List<DbAppeal> result = new();

		foreach (GamePlayer player in ClientService.GetPlayers())
		{
			DbAppeal ap = GetAppealByPlayerName(player.Name);

			if (ap != null)
				result.Add(ap);
		}

		TotalAppeals = result.Count;
		return result;
	}

	/// <summary>
	/// Gets a combined list of Appeals including player Appeals who are offline.
	/// </summary>
	/// <returns></returns>
	public static IList<DbAppeal> GetAllAppealsOffline()
	{
		return GameServer.Database.SelectAllObjects<DbAppeal>();
	}
	/// <summary>
	/// Creates a New Appeal
	/// </summary>
	/// <param name="Name"></param>The name of the Player who filed the appeal.
	/// <param name="Severity"></param>The Severity of the appeal (low, medium, high, critical)
	/// <param name="Status"></param>The status of the appeal (Open or InProgress)
	/// <param name="Text"></param>The text content of the appeal
	public static void CreateAppeal(GamePlayer Player, int Severity, string Status, string Text)
	{
		if (Player.IsMuted)
		{
			Player.Out.SendMessage("[Appeals]: " + LanguageMgr.GetTranslation(Player.Client.Account.Language, "Scripts.Players.Appeal.YouAreMuted"), EChatType.CT_Important, EChatLoc.CL_ChatWindow);
			return;
		}
		bool HasPendingAppeal = Player.TempProperties.GetProperty<bool>("HasPendingAppeal");
		if (HasPendingAppeal)
		{
			Player.Out.SendMessage("[Appeals]: " + LanguageMgr.GetTranslation(Player.Client.Account.Language, "Scripts.Players.Appeal.AlreadyActiveAppeal"), EChatType.CT_Important, EChatLoc.CL_ChatWindow);
			return;
		}
		string eText = GameServer.Database.Escape(Text); //prevent SQL injection
		string TimeStamp = DateTime.Now.ToLongTimeString() + " " + DateTime.Now.ToLongDateString();
		DbAppeal appeal = new DbAppeal(Player.Name, Player.Client.Account.Name, Severity, Status, TimeStamp, eText);
		GameServer.Database.AddObject(appeal);
		Player.TempProperties.SetProperty("HasPendingAppeal", true);
		Player.Out.SendMessage("[Appeals]: " + LanguageMgr.GetTranslation(Player.Client.Account.Language, "Scripts.Players.Appeal.AppealSubmitted"), EChatType.CT_Important, EChatLoc.CL_ChatWindow);
		Player.Out.SendMessage("[Appeals]: " + LanguageMgr.GetTranslation(Player.Client.Account.Language, "Scripts.Players.Appeal.IfYouLogOut"), EChatType.CT_Important, EChatLoc.CL_ChatWindow);
		Player.Out.SendPlaySound(ESoundType.Craft, 0x04);
		NotifyStaff();
		return;
	}

	/// <summary>
	/// Sets and saves the new status of the appeal
	/// </summary>
	/// <param name="name"></param>The name of the staff member making this change.
	/// <param name="appeal"></param>The appeal to change the status of.
	/// <param name="status">the new status (Open, Being Helped)</param>
	public static void ChangeStatus(string staffname, GamePlayer target, DbAppeal appeal, string status)
	{
		appeal.Status = status;
		appeal.Dirty = true;
		GameServer.Database.SaveObject(appeal);
		MessageToAllStaff("Staffmember " + staffname + " has changed the status of " + target.Name + "'s appeal to " + status + ".");
		target.Out.SendMessage("[Appeals]: " + LanguageMgr.GetTranslation(target.Client, "Scripts.Players.Appeal.StaffChangedStatus", staffname, status), EChatType.CT_Important, EChatLoc.CL_ChatWindow);
		return;
	}

	/// <summary>
	/// Removes an appeal from the queue and deletes it from the db.
	/// </summary>
	/// <param name="name"></param>The name of the staff member making this change.
	/// <param name="appeal"></param>The appeal to remove.
	/// <param name="Player"></param>The Player whose appeal we are closing.
	public static void CloseAppeal(string staffname, GamePlayer Player, DbAppeal appeal)
	{
		MessageToAllStaff("[Appeals]: " + "Staffmember " + staffname + " has just closed " + Player.Name + "'s appeal.");
		Player.Out.SendMessage("[Appeals]: " + LanguageMgr.GetTranslation(Player.Client.Account.Language, "Scripts.Players.Appeal.StaffClosedYourAppeal", staffname), EChatType.CT_Important, EChatLoc.CL_ChatWindow);
		Player.Out.SendPlaySound(ESoundType.Craft, 0x02);
		GameServer.Database.DeleteObject(appeal);
		Player.TempProperties.SetProperty("HasPendingAppeal", false);
		return;
	}
	/// <summary>
	/// Removes an appeal from an offline player and deletes it from the db.
	/// </summary>
	/// <param name="name"></param>The name of the staff member making this change.
	/// <param name="appeal"></param>The appeal to remove.
	public static void CloseAppeal(string staffname, DbAppeal appeal)
	{
		MessageToAllStaff("[Appeals]: " + "Staffmember " + staffname + " has just closed " + appeal.Name + "'s (offline) appeal.");
		GameServer.Database.DeleteObject(appeal);
		return;
	}

	public static void CancelAppeal(GamePlayer Player, DbAppeal appeal)
	{
		MessageToAllStaff("[Appeals]: " + Player.Name + " has canceled their appeal.");
		Player.Out.SendMessage("[Appeals]: " + LanguageMgr.GetTranslation(Player.Client.Account.Language, "Scripts.Players.Appeal.CanceledYourAppeal"), EChatType.CT_Important, EChatLoc.CL_ChatWindow);
		Player.Out.SendPlaySound(ESoundType.Craft, 0x02);
		GameServer.Database.DeleteObject(appeal);
		Player.TempProperties.SetProperty("HasPendingAppeal", false);
		return;
	}

	#endregion

	#region Player enter

	public static void PlayerEnter(CoreEvent e, object sender, EventArgs args)
	{
		GamePlayer player = sender as GamePlayer;
		if (player == null) { return; }
		if (player.Client.Account.PrivLevel > (uint)EPrivLevel.Player)
		{

			StaffList.Add(player);
			
			IList<DbAppeal> Appeals = GetAllAppeals();
			if (Appeals.Count > 0)
			{
				player.Out.SendMessage("[Appeals]: " + "There are " + Appeals.Count + " appeals in the queue!  Use /gmappeal to work the appeals queue.", EChatType.CT_Important, EChatLoc.CL_ChatWindow);
			}
		}

		//Check if there is an existing appeal belonging to this player.
		DbAppeal appeal = GetAppealByAccountName(player.Client.Account.Name);

		if (appeal == null)
		{
			player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Scripts.Players.Appeal.LoginMessage"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
			return;
		}
		if (appeal.Name != player.Name)
		{
			//players account has an appeal but it dosn't belong to this player, let's change it.
			appeal.Name = player.Name;
			appeal.Dirty = true;
			GameServer.Database.SaveObject(appeal);
		}
		player.Out.SendMessage("[Appeals]: " + LanguageMgr.GetTranslation(player.Client.Account.Language, "Scripts.Players.Appeal.YouHavePendingAppeal"), EChatType.CT_Important, EChatLoc.CL_ChatWindow);
		player.TempProperties.SetProperty("HasPendingAppeal", true);
		NotifyStaff();
	}

	#endregion

	#region Player quit
	public static void PlayerQuit(CoreEvent e, object sender, EventArgs args)
	{
		GamePlayer player = sender as GamePlayer;
		if (player == null)
			return;
		if (player.Client.Account.PrivLevel > (uint)EPrivLevel.Player)
		{
			StaffList.Remove(player);
		}
	}
	#endregion Player quit
}