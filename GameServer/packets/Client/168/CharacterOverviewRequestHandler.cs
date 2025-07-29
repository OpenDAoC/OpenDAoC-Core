using System.Reflection;

namespace DOL.GS.PacketHandler.Client.v168
{
	[PacketHandlerAttribute(PacketHandlerType.TCP, eClientPackets.CharacterOverviewRequest, "Handles account realm info and sending char overview", eClientStatus.LoggedIn)]
	public class CharacterOverviewRequestHandler : IPacketHandler
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

		public void HandlePacket(GameClient client, GSPacketIn packet)
		{
			// This actually prevents 1124 from entering the game. Should it be > instead of >=?
			if (client.Version >= GameClient.eClientVersion.Version1124) // 1124 support
			{
				_HandlePacket1124(client, packet);
				return;
			}

			client.ClientState = GameClient.eClientState.CharScreen;

			//reset realm if no characters
			if((client.Account.Characters == null || client.Account.Characters.Length <= 0) && client.Account.Realm != (int)eRealm.None)
			{
				client.Account.Realm = (int)eRealm.None;
			}

			string accountName = packet.ReadString(24);

			if(accountName.EndsWith("-X")) 
			{
				if(GameServer.ServerRules.IsAllowedCharsInAllRealms(client))
				{
					client.Out.SendRealm(eRealm.None);
				}
				else
				{
					//Requests to know what realm an account is
					//assigned to... if Realm::NONE is sent, the
					//Realm selection screen is shown
					switch(client.Account.Realm)
					{
						case 1: client.Out.SendRealm(eRealm.Albion); break;
						case 2: client.Out.SendRealm(eRealm.Midgard); break;
						case 3: client.Out.SendRealm(eRealm.Hibernia); break;
						default: client.Out.SendRealm(eRealm.None); break;
					}
				}
			} 
			else 
			{
				eRealm chosenRealm;

				if(client.Account.Realm == (int)eRealm.None || GameServer.ServerRules.IsAllowedCharsInAllRealms(client))
				{
					// allow player to choose the realm if not set already or if allowed by server rules
					if(accountName.EndsWith("-S"))      chosenRealm = eRealm.Albion;
					else if(accountName.EndsWith("-N")) chosenRealm = eRealm.Midgard;
					else if(accountName.EndsWith("-H")) chosenRealm = eRealm.Hibernia;
					else
					{
						if (log.IsErrorEnabled)
							log.Error("User has chosen unknown realm: "+accountName+"; account="+client.Account.Name);
						client.Out.SendRealm(eRealm.None);
						return;
					}

					if (client.Account.Realm == (int)eRealm.None && !GameServer.ServerRules.IsAllowedCharsInAllRealms(client))
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
					chosenRealm = (eRealm)client.Account.Realm;
				}

				client.Out.SendCharacterOverview(chosenRealm);
			}
		}

		private void _HandlePacket1124(GameClient client, GSPacketIn packet)
		{
			client.ClientState = GameClient.eClientState.CharScreen;

			//reset realm if no characters
			if ((client.Account.Characters == null || client.Account.Characters.Length <= 0) && client.Account.Realm != (int)eRealm.None)
				client.Account.Realm = (int)eRealm.None;

			//string accountName = packet.Readstring(24);
			byte realm = (byte)packet.ReadByte();
			if (realm == 0)
			{
				if (GameServer.ServerRules.IsAllowedCharsInAllRealms(client))
					client.Out.SendRealm(eRealm.None);
				else
				{
					//Requests to know what realm an account is
					//assigned to... if Realm::NONE is sent, the
					//Realm selection screen is shown
					switch (client.Account.Realm)
					{
						case 1: client.Out.SendRealm(eRealm.Albion); break;
						case 2: client.Out.SendRealm(eRealm.Midgard); break;
						case 3: client.Out.SendRealm(eRealm.Hibernia); break;
						default: client.Out.SendRealm(eRealm.None); break;
					}
				}
			}
			else
			{
				eRealm chosenRealm;

				if (client.Account.Realm == (int)eRealm.None || GameServer.ServerRules.IsAllowedCharsInAllRealms(client))
				{
					// allow player to choose the realm if not set already or if allowed by server rules
					if (realm == 1)
						chosenRealm = eRealm.Albion;
					else if (realm == 2)
						chosenRealm = eRealm.Midgard;
					else if (realm == 3)
						chosenRealm = eRealm.Hibernia;
					else
					{
						log.Error($"User has chosen unknown realm: {realm}; account={client.Account.Name}");
						client.Out.SendRealm(eRealm.None);
						return;
					}

					client.Account.Realm = (int)chosenRealm;
				}
				else
					// use saved realm ignoring what user has chosen if server rules do not allow to choose the realm
					chosenRealm = (eRealm)client.Account.Realm;

				client.Out.SendCharacterOverview(chosenRealm);
			}
		}
	}
}
