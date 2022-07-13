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
using System;
using System.Linq;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS
{
	/// <summary>
	/// This class helps sending messages to other players
	/// </summary>
	public static class Message
	{
		/// <summary>
		/// Sends a message to the chat window of players inside
		/// INFO_DISTANCE radius of the center object
		/// </summary>
		/// <param name="centerObject">The center object of the message</param>
		/// <param name="message">The message to send</param>
		/// <param name="chatType">The type of message to send</param>
		/// <param name="excludes">An optional list of excluded players</param>
		public static void ChatToArea(GameObject centerObject, string message, eChatType chatType, params GameObject[] excludes)
		{
			ChatToArea(centerObject, message, chatType, WorldMgr.INFO_DISTANCE, excludes);
		}

		/// <summary>
		/// Sends a message to the chat window of players inside
		/// INFO_DISTANCE radius of the center object
		/// </summary>
		/// <param name="centerObject">The center object of the message</param>
		/// <param name="message">The message to send</param>
		/// <param name="chatType">The type of message to send</param>
		/// <remarks>If the centerObject is a player, he won't receive the message</remarks>
		public static void ChatToOthers(GameObject centerObject, string message, eChatType chatType)
		{
			ChatToArea(centerObject, message, chatType, WorldMgr.INFO_DISTANCE, centerObject);
		}

		/// <summary>
		/// Sends a message to the chat window of players inside
		/// a specific distance of the center object
		/// </summary>
		/// <param name="centerObject">The center object of the message</param>
		/// <param name="message">The message to send</param>
		/// <param name="chatType">The type of message to send</param>
		/// <param name="distance">The distance around the center where players will receive the message</param>
		/// <param name="excludes">An optional list of excluded players</param>
		/// <remarks>If the center object is a player, he will get the message too</remarks>
		public static void ChatToArea(GameObject centerObject, string message, eChatType chatType, ushort distance, params GameObject[] excludes)
		{
			MessageToArea(centerObject, message, chatType, eChatLoc.CL_ChatWindow, distance, excludes);
		}

		/// <summary>
		/// Sends a message to the system window of players inside
		/// INFO_DISTANCE radius of the center object
		/// </summary>
		/// <param name="centerObject">The center object of the message</param>
		/// <param name="message">The message to send</param>
		/// <param name="chatType">The type of message to send</param>
		/// <param name="excludes">An optional list of players to exclude from receiving the message</param>
		public static void SystemToArea(GameObject centerObject, string message, eChatType chatType, params GameObject[] excludes)
		{
			SystemToArea(centerObject, message, chatType, WorldMgr.INFO_DISTANCE, excludes);
		}

		/// <summary>
		/// Sends a message to the system window of players inside
		/// INFO_DISTANCE radius of the center object
		/// </summary>
		/// <param name="centerObject">The center object of the message</param>
		/// <param name="message">The message to send</param>
		/// <param name="chatType">The type of message to send</param>
		/// <remarks>If the centerObject is a player, he won't receive the message</remarks>
		public static void SystemToOthers(GameObject centerObject, string message, eChatType chatType)
		{
			SystemToArea(centerObject, message, chatType, WorldMgr.INFO_DISTANCE, centerObject);
		}

		/// <summary>
		/// Sends a message to the system window of players inside
		/// INFO_DISTANCE radius of the center object
		/// </summary>
		/// <param name="centerObject">The center object of the message</param>
		/// <param name="chatType">The type of message to send</param>
		/// <param name="LanguageMessageID">The language message ID</param>
		/// <param name="args">The Translation args</param>
		/// <remarks>If the centerObject is a player, he won't receive the message</remarks>
		public static void SystemToOthers2(GameObject centerObject, eChatType chatType, string LanguageMessageID, params object[] args)
		{
			if (LanguageMessageID == null || LanguageMessageID.Length <= 0) return;
			foreach (GamePlayer player in centerObject.GetPlayersInRadius(WorldMgr.INFO_DISTANCE))
			{
				if (!(centerObject is GamePlayer && centerObject == player))
				{
					player.MessageFromArea(centerObject, LanguageMgr.GetTranslation(player.Client.Account.Language, LanguageMessageID, args), chatType, eChatLoc.CL_SystemWindow);
				}
			}
		}

		/// <summary>
		/// Sends a message to the system window of players inside
		/// a specific distance of the center object
		/// </summary>
		/// <param name="centerObject">The center object of the message</param>
		/// <param name="message">The message to send</param>
		/// <param name="chatType">The type of message to send</param>
		/// <param name="distance">The distance around the center where players will receive the message</param>
		/// <param name="excludes">An optional list of excluded players</param>
		/// <remarks>If the center object is a player, he will get the message too</remarks>
		public static void SystemToArea(GameObject centerObject, string message, eChatType chatType, ushort distance, params GameObject[] excludes)
		{
			MessageToArea(centerObject, message, chatType, eChatLoc.CL_SystemWindow, distance, excludes);
		}

		/// <summary>
		/// Sends a message to GamePlayers within a specific radius of another object
		/// </summary>
		/// <param name="centerObject">The entity from which the message originates</param>
		/// <param name="message">The message string being sent</param>
		/// <param name="chatType">The chat type (e.g., CT_Say)</param>
		/// <param name="chatLoc">The UI location to display the message (i.e., chat, combat, popup windows)</param>
		/// <param name="distance">The maximum distance the message is sent from the centerObject (anyone outside will not receive message)</param>
		/// <param name="excludes">The entity(ies) that should not receive the message or else 'null'</param>
		public static void MessageToArea(GameObject centerObject, string message, eChatType chatType, eChatLoc chatLoc, ushort distance, params GameObject[] excludes)
		{
			if (string.IsNullOrEmpty(message)) return; // Don't send blank messages

			if (centerObject != null)
			{
				var newList = centerObject.GetPlayersInRadius(distance);
				if (newList == null) return;
				foreach (GamePlayer player in newList) // Send message to each GamePlayer within the specified distance of the centerObject
				{
					var excluded = false;

					if (excludes != null) // If entities are specified for exclusion (i.e., != null), then perform following actions
					{
						foreach (var obj in excludes) // For each param in excludes, set exclude to true for GamePlayers and don't send message
						{
							if (obj == player)
							{
								excluded = true;
								break;
							}
						}
					}
					if (excluded == false)
					{
						player?.MessageFromArea(centerObject, message, chatType, chatLoc); // If no excludes are specified (i.e., 'null'), send message to everyone in radius
					}
				}
			}
		}
	}
}
