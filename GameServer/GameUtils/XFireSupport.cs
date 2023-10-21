using System;
using Core.GS.Events;

namespace Core.GS.GameUtils;

public class XFirePlayerEnterExit
{
	private const string XFire_Property_Flag = "XFire_Property_Flag";

	[GameServerStartedEvent]
	public static void OnServerStart(CoreEvent e, object sender, EventArgs arguments)
	{
		GameEventMgr.AddHandler(GamePlayerEvent.GameEntered, new CoreEventHandler(PlayerEntered));
		GameEventMgr.AddHandler(GamePlayerEvent.Quit, new CoreEventHandler(PlayerQuit));
	}

	[GameServerStoppedEvent]
	public static void OnServerStop(CoreEvent e, object sender, EventArgs arguments)
	{
		GameEventMgr.RemoveHandler(GamePlayerEvent.GameEntered, new CoreEventHandler(PlayerEntered));
		GameEventMgr.RemoveHandler(GamePlayerEvent.Quit, new CoreEventHandler(PlayerQuit));
	}

	private static void PlayerEntered(CoreEvent e, object sender, EventArgs arguments)
	{
		GamePlayer player = sender as GamePlayer;
		if (player == null) return;
		//			if (player.IsAnonymous) return; TODO check /anon and xfire
		byte flag = 0;
		if (player.ShowXFireInfo)
			flag = 1;
		player.Out.SendXFireInfo(flag);
	}

	private static void PlayerQuit(CoreEvent e, object sender, EventArgs arguments)
	{
		GamePlayer player = sender as GamePlayer;
		if (player == null) return;

		player.Out.SendXFireInfo(0);
	}
}