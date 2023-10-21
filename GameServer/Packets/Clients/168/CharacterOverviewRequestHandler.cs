using System.Reflection;
using Core.Database.Tables;
using Core.GS.Enums;
using log4net;

namespace Core.GS.Packets.Clients;

[PacketHandler(EPacketHandlerType.TCP, EClientPackets.CharacterOverviewRequest, "Handles account realm info and sending char overview", EClientStatus.LoggedIn)]
public class CharacterOverviewRequestHandler : IPacketHandler
{
	private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

	public void HandlePacket(GameClient client, GsPacketIn packet)
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

	private void _HandlePacket1124(GameClient client, GsPacketIn packet)
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