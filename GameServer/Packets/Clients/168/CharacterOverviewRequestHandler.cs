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
 */

using System.Reflection;
using DOL.Database;
using log4net;

namespace DOL.GS.PacketHandler.Client.v168
{
	[PacketHandler(PacketHandlerType.TCP, eClientPackets.CharacterOverviewRequest, "Handles account realm info and sending char overview", eClientStatus.LoggedIn)]
	public class CharacterOverviewRequestHandler : IPacketHandler
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		public void HandlePacket(GameClient client, GSPacketIn packet)
		{
			// This actually prevents 1124 from entering the game. Should it be > instead of >=?
			if (client.Version >= GameClient.eClientVersion.Version1124) // 1124 support
			{
				_HandlePacket1124(client, packet);
				return;
			}

			client.ClientState = GameClient.eClientState.CharScreen;
			if (client.Player != null)
			{
				try
				{
					// find the cached character and force it to update with the latest saved character
					DbCoreCharacter cachedCharacter = null;
					foreach (DbCoreCharacter accountChar in client.Account.Characters)
					{
						if (accountChar.ObjectId == client.Player.InternalID)
						{
							cachedCharacter = accountChar;
							break;
						}
					}

					if (cachedCharacter != null)
					{
						cachedCharacter = client.Player.DBCharacter;
					}
				}
				catch (System.Exception ex)
				{
					log.ErrorFormat("Error attempting to update cached player. {0}", ex.Message);
				}
			}

			client.Player = null;

			//reset realm if no characters
			if((client.Account.Characters == null || client.Account.Characters.Length <= 0) && client.Account.Realm != (int)ERealm.None)
			{
				client.Account.Realm = (int)ERealm.None;
			}

			string accountName = packet.ReadString(24);

			if(accountName.EndsWith("-X")) 
			{
				if(GameServer.ServerRules.IsAllowedCharsInAllRealms(client))
				{
					client.Out.SendRealm(ERealm.None);
				}
				else
				{
					//Requests to know what realm an account is
					//assigned to... if Realm::NONE is sent, the
					//Realm selection screen is shown
					switch(client.Account.Realm)
					{
						case 1: client.Out.SendRealm(ERealm.Albion); break;
						case 2: client.Out.SendRealm(ERealm.Midgard); break;
						case 3: client.Out.SendRealm(ERealm.Hibernia); break;
						default: client.Out.SendRealm(ERealm.None); break;
					}
				}
			} 
			else 
			{
				ERealm chosenRealm;

				if(client.Account.Realm == (int)ERealm.None || GameServer.ServerRules.IsAllowedCharsInAllRealms(client))
				{
					// allow player to choose the realm if not set already or if allowed by server rules
					if(accountName.EndsWith("-S"))      chosenRealm = ERealm.Albion;
					else if(accountName.EndsWith("-N")) chosenRealm = ERealm.Midgard;
					else if(accountName.EndsWith("-H")) chosenRealm = ERealm.Hibernia;
					else
					{
						if (log.IsErrorEnabled)
							log.Error("User has chosen unknown realm: "+accountName+"; account="+client.Account.Name);
						client.Out.SendRealm(ERealm.None);
						return;
					}

					if (client.Account.Realm == (int)ERealm.None && !GameServer.ServerRules.IsAllowedCharsInAllRealms(client))
					{
						// save the choice
						client.Account.Realm = (int)chosenRealm;
						GameServer.Database.SaveObject(client.Account);
						// 2008-01-29 Kakuri - Obsolete
						//GameServer.Database.WriteDatabaseTable( typeof( Account ) );
					}
				}
				else
				{
					// use saved realm ignoring what user has chosen if server rules do not allow to choose the realm
					chosenRealm = (ERealm)client.Account.Realm;
				}

				client.Out.SendCharacterOverview(chosenRealm);
			}
		}

		private void _HandlePacket1124(GameClient client, GSPacketIn packet)
		{
			client.ClientState = GameClient.eClientState.CharScreen;

			if (client.Player != null)
			{
				try
				{
					// find the cached character and force it to update with the latest saved character
					DbCoreCharacter cachedCharacter = null;
					foreach (DbCoreCharacter accountChar in client.Account.Characters)
					{
						if (accountChar.ObjectId == client.Player.InternalID)
						{
							cachedCharacter = accountChar;
							break;
						}
					}

					if (cachedCharacter != null)
						cachedCharacter = client.Player.DBCharacter;
				}
				catch (System.Exception ex)
				{
					log.ErrorFormat("Error attempting to update cached player. {0}", ex.Message);
				}
			}

			client.Player = null;

			//reset realm if no characters
			if ((client.Account.Characters == null || client.Account.Characters.Length <= 0) && client.Account.Realm != (int)ERealm.None)
				client.Account.Realm = (int)ERealm.None;

			//string accountName = packet.Readstring(24);
			byte realm = (byte)packet.ReadByte();
			if (realm == 0)
			{
				if (GameServer.ServerRules.IsAllowedCharsInAllRealms(client))
					client.Out.SendRealm(ERealm.None);
				else
				{
					//Requests to know what realm an account is
					//assigned to... if Realm::NONE is sent, the
					//Realm selection screen is shown
					switch (client.Account.Realm)
					{
						case 1: client.Out.SendRealm(ERealm.Albion); break;
						case 2: client.Out.SendRealm(ERealm.Midgard); break;
						case 3: client.Out.SendRealm(ERealm.Hibernia); break;
						default: client.Out.SendRealm(ERealm.None); break;
					}
				}
			}
			else
			{
				ERealm chosenRealm;

				if (client.Account.Realm == (int)ERealm.None || GameServer.ServerRules.IsAllowedCharsInAllRealms(client))
				{
					// allow player to choose the realm if not set already or if allowed by server rules
					if (realm == 1)
						chosenRealm = ERealm.Albion;
					else if (realm == 2)
						chosenRealm = ERealm.Midgard;
					else if (realm == 3)
						chosenRealm = ERealm.Hibernia;
					else
					{
						log.Error($"User has chosen unknown realm: {realm}; account={client.Account.Name}");
						client.Out.SendRealm(ERealm.None);
						return;
					}

					client.Account.Realm = (int)chosenRealm;
				}
				else
					// use saved realm ignoring what user has chosen if server rules do not allow to choose the realm
					chosenRealm = (ERealm)client.Account.Realm;

				client.Out.SendCharacterOverview(chosenRealm);
			}
		}
	}
}
