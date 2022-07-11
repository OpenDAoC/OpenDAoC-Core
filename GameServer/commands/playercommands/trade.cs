/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 */

using DOL.Language;
using DOL.GS.ServerProperties;
using DOL.GS.PacketHandler;
using DOL.GS.Scripts.discord;


namespace DOL.GS.Commands
{
	[CmdAttribute(
		 "&trade",
		 ePrivLevel.Player,
		 "Broadcast a trade message to other players in the same region",
		 "/trade <message>")]
	public class TradeChannelCommandHandler : AbstractCommandHandler, ICommandHandler
	{
		private const string tradeTimeoutString = "lastTradeTick";

		public void OnCommand(GameClient client, string[] args)
		{
			if (args.Length < 2)
			{
				DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Broadcast.NoText"));
				return;
			}
			if (client.Player.IsMuted)
			{
				client.Player.Out.SendMessage("You have been muted. You cannot broadcast.", eChatType.CT_Staff, eChatLoc.CL_SystemWindow);
				return;
			}
			
			var lastTradeTick = client.Player.TempProperties.getProperty<long>(tradeTimeoutString);
			var slowModeLength = Properties.TRADE_SLOWMODE_LENGTH * 1000;
			
			if ((GameLoop.GameLoopTime - lastTradeTick) < slowModeLength && client.Account.PrivLevel == 1) // 60 secs
			{
				// Message: You must wait {0} seconds before using this command again.
				ChatUtil.SendSystemMessage(client, "PLCommands.Trade.List.Wait", 60 - (GameLoop.GameLoopTime - lastTradeTick) / 1000);
				return;
			}
			
			string message = string.Join(" ", args, 1, args.Length - 1);
			
			Broadcast(client.Player, message);

			if (client.Account.PrivLevel == 1)
			{
				client.Player.TempProperties.setProperty(tradeTimeoutString, GameLoop.GameLoopTime);
			}
		}

		private void Broadcast(GamePlayer player, string message)
		{
			foreach (GameClient c in WorldMgr.GetClientsOfRealm(player.Realm))
			{
				if (c.Player.Realm == player.Realm || player.Client.Account.PrivLevel > 1)
				{
					c.Out.SendMessage($"[Trade] {player.Name}: {message}", eChatType.CT_Trade, eChatLoc.CL_ChatWindow);
				}
			}
			
			if (Properties.DISCORD_ACTIVE) WebhookMessage.LogChatMessage(player, eChatType.CT_Trade, message);
		}
	}
}


