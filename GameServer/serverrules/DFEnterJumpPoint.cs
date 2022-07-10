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

using DOL.Database;
using DOL.GS.Keeps;
using DOL.GS.PacketHandler;
using DOL.Events;
using JNogueira.Discord.Webhook.Client;

namespace DOL.GS.ServerRules
{
	/// <summary>
	/// Handles DF entrance jump point allowing only one realm to enter on Normal server type.
	/// </summary>
	public class DFEnterJumpPoint : IJumpPointHandler
	{
		/// <summary>
		/// Decides whether player can jump to the target point.
		/// All messages with reasons must be sent here.
		/// Can change destination too.
		/// </summary>
		/// <param name="targetPoint">The jump destination</param>
		/// <param name="player">The jumping player</param>
		/// <returns>True if allowed</returns>
		public bool IsAllowedToJump(ZonePoint targetPoint, GamePlayer player)
		{
            if (player.Client.Account.PrivLevel > 1)
            {
                return true;
            }
			if (GameServer.Instance.Configuration.ServerType != eGameServerType.GST_Normal)
				return true;
			if (ServerProperties.Properties.ALLOW_ALL_REALMS_DF)
				return true;
			if (player.Realm == PreviousOwner && LastRealmSwapTick + GracePeriod >= GameLoop.GameLoopTime)
				return true;
			return (player.Realm == DarknessFallOwner);
		}

		public static eRealm DarknessFallOwner = eRealm.None;
		public static eRealm PreviousOwner = eRealm.None;

		public static long GracePeriod = 900 * 1000; //15 mins
		public static long LastRealmSwapTick = 0;
		
		/// <summary>
		/// initialize the darkness fall entrance system
		/// </summary>
		/// <param name="e"></param>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		[ScriptLoadedEvent]
		public static void OnScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			CheckDFOwner();
			GameEventMgr.AddHandler(KeepEvent.KeepTaken,new DOLEventHandler(OnKeepTaken));
		}

		/// <summary>
		/// check if a realm have more keep at start
		/// to know the DF owner
		/// </summary>
		
		private static void CheckDFOwner()
		{
			int albcount = GameServer.KeepManager.GetKeepCountByRealm(eRealm.Albion);
			int midcount = GameServer.KeepManager.GetKeepCountByRealm(eRealm.Midgard);
			int hibcount = GameServer.KeepManager.GetKeepCountByRealm(eRealm.Hibernia);
			
			if (albcount > midcount && albcount > hibcount)
			{
				DarknessFallOwner = eRealm.Albion;
			}
			if (midcount > albcount && midcount > hibcount)
			{
				DarknessFallOwner = eRealm.Midgard;
			}
			if (hibcount > midcount && hibcount > albcount)
			{
				DarknessFallOwner = eRealm.Hibernia;
			}
		}



		/// <summary>
		/// when  keep is taken it check if the realm which take gain the control of DF
		/// </summary>
		/// <param name="e"></param>
		/// <param name="sender"></param>
		/// <param name="arguments"></param>
		public static void OnKeepTaken(DOLEvent e, object sender, EventArgs arguments)
		{
			KeepEventArgs args = arguments as KeepEventArgs;
			eRealm realm = (eRealm) args.Keep.Realm ;
			if (realm != DarknessFallOwner )
			{
				// TODO send message to chat and discord web hook
				string oldDFOwner = GlobalConstants.RealmToName(DarknessFallOwner);
				int currentDFOwnerTowerCount = GameServer.KeepManager.GetKeepCountByRealm(DarknessFallOwner);
				int challengerOwnerTowerCount = GameServer.KeepManager.GetKeepCountByRealm(realm);
				if (currentDFOwnerTowerCount < challengerOwnerTowerCount)
                {
					PreviousOwner = DarknessFallOwner;
					LastRealmSwapTick = GameLoop.GameLoopTime;
					DarknessFallOwner = realm;
				}
					
				string realmName = "";

				string messageDFGetControl = string.Format("The forces of {0} have gained access to Darkness Falls!", GlobalConstants.RealmToName(realm));
				string messageDFLostControl = string.Format("{0} will lose access to Darkness Falls in 15 minutes!", oldDFOwner);

				if (oldDFOwner != GlobalConstants.RealmToName(DarknessFallOwner))
				{ 
					BroadcastMessage(messageDFLostControl, eRealm.None);
					BroadcastMessage(messageDFGetControl, eRealm.None);
				}
			}
		}
		
		/// <summary>
		/// Method to broadcast messages, if eRealm.None all can see,
		/// else only the right realm can see
		/// </summary>
		/// <param name="message">The message</param>
		/// <param name="realm">The realm</param>
		public static void BroadcastMessage(string message, eRealm realm)
		{
			foreach (GameClient client in WorldMgr.GetAllClients())
			{
				if (client.Player == null)
					continue;
				if ((client.Account.PrivLevel != 1 || realm == eRealm.None) || client.Player.Realm == realm)
				{
					client.Out.SendMessage(message, eChatType.CT_Important, eChatLoc.CL_SystemWindow);
				}
			}
			
			if (ServerProperties.Properties.DISCORD_ACTIVE && !string.IsNullOrEmpty(ServerProperties.Properties.DISCORD_RVR_WEBHOOK_ID))
			{
				var client = new DiscordWebhookClient(ServerProperties.Properties.DISCORD_RVR_WEBHOOK_ID);

				// Create your DiscordMessage with all parameters of your message.
				var discordMessage = new DiscordMessage(
					"",
					username: "Atlas RvR",
					avatarUrl: "https://cdn.discordapp.com/attachments/919610633656369214/928728399449571388/keep.png",
					tts: false,
					embeds: new[]
					{
						new DiscordMessageEmbed(
							author: new DiscordMessageEmbedAuthor("Darkness Falls"),
							color: 0,
							description: message
						)
					}
				);
				
				client.SendToDiscord(discordMessage);
			}
			
		}

	}
}
