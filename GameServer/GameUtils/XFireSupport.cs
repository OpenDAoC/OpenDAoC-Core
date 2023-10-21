using System;
using Core.Events;

namespace Core.GS.GameEvents
{
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
}
namespace Core.GS.Commands
{
	[Command("&xfire", EPrivLevel.Player, "Xfire support", "/xfire <on|off>")]
	public class CheckXFireCommandHandler : ACommandHandler, ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{
			if (client.Player == null)
				return;
			if (args.Length < 2)
			{
				DisplaySyntax(client);
				return;
			}
			byte flag = 0;
			if (args[1].ToLower().Equals("on"))
			{
				client.Player.ShowXFireInfo = true;
				DisplayMessage(client, "Your XFire flag is ON. Your character data will be sent to the XFire service ( if you have XFire installed ). Use '/xfire off' to disable sending character data to the XFire service.");
				flag = 1;
			}
			else if (args[1].ToLower().Equals("off"))
			{
				client.Player.ShowXFireInfo = false;
				DisplayMessage(client, "Your XFire flag is OFF. TODO correct message.");
			}
			else
			{
				DisplaySyntax(client);
				return;
			}
			client.Player.Out.SendXFireInfo(flag);
		}
	}
}
