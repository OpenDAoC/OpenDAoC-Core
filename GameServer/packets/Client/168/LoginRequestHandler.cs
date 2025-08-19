using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DOL.Database;
using DOL.GS.ServerProperties;

namespace DOL.GS.PacketHandler.Client.v168
{
	/// <summary>
	/// Handles the login request packet.
	/// </summary>
	/// <remarks>
	/// Included is a PHP snippet for generating passwords that will work with the system/hashing algorithm DOL uses:
	/// 
	/// PHP version of CryptPass(string password):
	///
	///	$pass = "abc";
	///	cryptPassword($pass);
	///
	///	function cryptPassword($pass)
	///	{
	///		$len = strlen($pass);
	///		$res = string.Empty;
	///		for ($i = 0; $i < $len; $i++)
	///		{
	///			$res = $res . chr(ord(substr($pass, $i, 1)) >> 8);
	///			$res = $res . chr(ord(substr($pass, $i, 1)));
	///		}
	///
	///		$hash = strtoupper(md5($res));
	///		$len = strlen($hash);
	///		for ($i = ($len-1)&~1; $i >= 0; $i-=2)
	///		{
	///			if (substr($hash, $i, 1) == "0")
	///				$hash = substr($hash, 0, $i) . substr($hash, $i+1, $len);
	///		}
	///
	///		$crypted = "##" . $hash;
	///		return $crypted;
	///	}
	/// </remarks>
	[PacketHandlerAttribute(PacketHandlerType.TCP, eClientPackets.LoginRequest, "Handles the login.", eClientStatus.None)]
	public class LoginRequestHandler : IPacketHandler
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly Logging.Logger Log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

		private static DateTime _lastAccountCreateTime;
		private static HttpClient _httpClient = new HttpClient();
		private static HashSet<GameClient> _clientsLoggingIn = new();
		private static Lock _lock = new();

		public async void HandlePacket(GameClient client, GSPacketIn packet)
		{
			// Prevent multiple concurrent logins for the same client.
			if (client == null)
				return;

			lock (_lock)
			{
				if (!_clientsLoggingIn.Add(client))
					return;
			}

			try
			{
				await HandlePacketInternal(client, packet);
			}
			finally
			{
				lock (_lock)
				{
					_clientsLoggingIn.Remove(client);
				}
			}
		}

		private static async Task HandlePacketInternal(GameClient client, GSPacketIn packet)
		{
			byte major;
			byte minor;
			byte build;
			string password;
			string userName;
			
			/// <summary>
			/// Packet Format Change above 1.115
			/// </summary>
			
			if (client.Version < GameClient.eClientVersion.Version1115)
			{
				packet.Skip(2); //Skip the client_type byte
				
				major = (byte)packet.ReadByte();
				minor = (byte)packet.ReadByte();
				build = (byte)packet.ReadByte();
				password = packet.ReadString(20);
				
				
				bool v174;
				//the logger detection we had is no longer working
				//bool loggerUsing = false;
				switch (client.Version)
				{
					case GameClient.eClientVersion.Version168:
					case GameClient.eClientVersion.Version169:
					case GameClient.eClientVersion.Version170:
					case GameClient.eClientVersion.Version171:
					case GameClient.eClientVersion.Version172:
					case GameClient.eClientVersion.Version173:
						v174 = false;
						break;
					default:
						v174 = true;
						break;
				}
	
				if (v174)
				{
					packet.Skip(11);
				}
				else
				{
					packet.Skip(7);
				}
	
				uint c2 = packet.ReadInt();
				uint c3 = packet.ReadInt();
				uint c4 = packet.ReadInt();
	
				if (v174)
				{
					packet.Skip(27);
				}
				else
				{
					packet.Skip(31);
				}
	
				userName = packet.ReadString(20);
			}
			else if (client.Version < GameClient.eClientVersion.Version1126) // 1.125+ only // we support 1.109 and 1.125+ only
			{
				// client type
				packet.Skip(1);

				//version
				major = (byte)packet.ReadByte();
				minor = (byte)packet.ReadByte();
				build = (byte)packet.ReadByte();

				// revision
				packet.Skip(1);
				// build
				packet.Skip(2);

				if (client.Version <= GameClient.eClientVersion.Version1124)
				{
					userName = packet.ReadShortPascalStringLowEndian();
					password = packet.ReadShortPascalStringLowEndian();
				}
				else
				{
					userName = packet.ReadIntPascalStringLowEndian();
					password = packet.ReadIntPascalStringLowEndian();
				}
			}
			else
			{
				userName = packet.ReadIntPascalStringLowEndian();
				password = packet.ReadIntPascalStringLowEndian();
			}

			/*
			if (c2 == 0 && c3 == 0x05000000 && c4 == 0xF4000000)
			{
				loggerUsing = true;
				Log.Warn("logger detected (" + username + ")");
			}*/

			string ipAddress = client.TcpEndpointAddress;

			// check server status
			if (GameServer.Instance.ServerStatus == EGameServerStatus.GSS_Closed)
			{
				client.Out.SendLoginDenied(eLoginError.GameCurrentlyClosed);

				if (Log.IsInfoEnabled)
					Log.Info(ipAddress + " disconnected because game is closed!");

				client.IsConnected = false;
				return;
			}

			// check connection allowed with serverrules
			try
			{
				if (!GameServer.ServerRules.IsAllowedToConnect(client, userName))
				{
					if (Log.IsInfoEnabled)
						Log.Info(ipAddress + " disconnected because IsAllowedToConnect returned false!");

					return;
				}
			}
			catch (Exception e)
			{
				if (Log.IsErrorEnabled)
					Log.Error("Error shutting down Client after IsAllowedToConnect failed!", e);
			}

			try
			{
				DbAccount playerAccount;
				GameClient.eClientState state = client.ClientState;

				if (state is not GameClient.eClientState.NotConnected)
				{
					if (Log.IsDebugEnabled)
						Log.DebugFormat($"wrong client state on connect {userName} {state}");

					return;
				}

				if (Log.IsInfoEnabled)
					Log.Info(string.Format($"({ipAddress}) User {userName} logging on! ({client.Version} type:{client.ClientType} add:{client.ClientAddons:G})"));

				GameClient otherClient = ClientService.Instance.GetClientFromAccountName(userName);

				if (otherClient != null)
				{
					if (otherClient.ClientState is GameClient.eClientState.Connecting)
					{
						if (Log.IsInfoEnabled)
							Log.Info("User is already connecting, ignored.");

						client.Out.SendLoginDenied(eLoginError.AccountAlreadyLoggedIn);
						client.IsConnected = false;
						return;
					}

					// Check link death timer instead of client state to account for soft link deaths.
					if (otherClient.Player?.IsLinkDeathTimerRunning == true)
					{
						if (Log.IsInfoEnabled)
							Log.Info("User is still being logged out from linkdeath!");

						client.Out.SendLoginDenied(eLoginError.AccountIsInLogoutProcedure);
						client.IsConnected = false;
					}
					else
					{
						if (Log.IsInfoEnabled)
							Log.Info("User already logged in!");

						client.Out.SendLoginDenied(eLoginError.AccountAlreadyLoggedIn);
						client.IsConnected = false;
					}

					return;
				}

				Regex goodName = new Regex("^[a-zA-Z0-9]*$");
				if (!goodName.IsMatch(userName) || string.IsNullOrWhiteSpace(userName))
				{
					if (Log.IsInfoEnabled)
						Log.Info("Invalid symbols in account name \"" + userName + "\" found!");

					if (client != null && client.Out != null)
						client.Out.SendLoginDenied(eLoginError.InvalidAccount);
					else if (Log.IsWarnEnabled)
						Log.Warn("Client or Client.Out null on invalid name failure.  Disconnecting.");

					client.IsConnected = false;
					return;
				}
				else
				{
					playerAccount = await DOLDB<DbAccount>.FindObjectByKeyAsync(userName);
					client.PingTime = GameLoop.GameLoopTime;

					if (playerAccount == null)
					{
						if (GameServer.Instance.Configuration.AutoAccountCreation && Properties.ALLOW_AUTO_ACCOUNT_CREATION)
						{
							if (string.IsNullOrEmpty(password))
							{
								client.Out.SendLoginDenied(eLoginError.InvalidAccount);
								client.IsConnected = false;

								if (Log.IsInfoEnabled)
									Log.Info("Account creation failed, no password set for Account: " + userName);

								return;
							}

							TimeSpan timeSpan;
							int totalAccount = 0;
							var accountsFromSameIp = await DOLDB<DbAccount>.SelectObjectsAsync(DB.Column("LastLoginIP").IsEqualTo(ipAddress));

							foreach (DbAccount ac in accountsFromSameIp)
							{
								timeSpan = DateTime.Now - ac.CreationDate;

								if (timeSpan.TotalMinutes < Properties.TIME_BETWEEN_ACCOUNT_CREATION_SAMEIP)
								{
									if (Log.IsWarnEnabled)
										Log.Warn($"Account creation: too many from same IP within set minutes - {userName}:{ipAddress}");

									client.Out.SendLoginDenied(eLoginError.ServiceNotAvailable);
									client.IsConnected = false;
									return;
								}

								totalAccount++;
							}

							if (totalAccount >= Properties.TOTAL_ACCOUNTS_ALLOWED_SAMEIP)
							{
								if (Log.IsWarnEnabled)
									Log.Warn($"Account creation: too many accounts created from same ip - {userName}:{ipAddress}");

								client.Out.SendLoginDenied(eLoginError.ServiceNotAvailable);
								client.IsConnected = false;
								return;
							}

							if (Properties.TIME_BETWEEN_ACCOUNT_CREATION > 0)
							{
								timeSpan = DateTime.Now - _lastAccountCreateTime;

								if (timeSpan.TotalMinutes < Properties.TIME_BETWEEN_ACCOUNT_CREATION)
								{
									if (Log.IsWarnEnabled)
										Log.Warn($"Account creation: time between account creation too small - {userName}:{ipAddress}");

									client.Out.SendLoginDenied(eLoginError.ServiceNotAvailable);
									client.IsConnected = false;
									return;
								}
							}

							_lastAccountCreateTime = DateTime.Now;

							playerAccount = new DbAccount();
							playerAccount.Name = userName;
							playerAccount.Password = CryptPassword(password);
							playerAccount.Realm = 0;
							playerAccount.CreationDate = DateTime.Now;
							playerAccount.LastLogin = DateTime.Now;
							playerAccount.LastLoginIP = ipAddress;
							playerAccount.LastClientVersion = ((int)client.Version).ToString();
							playerAccount.Language = Properties.SERV_LANGUAGE;
							playerAccount.PrivLevel = 1;

							if (Log.IsInfoEnabled)
								Log.Info("New account created: " + userName);

							GameServer.Database.AddObject(playerAccount);

							// Log account creation
							AuditMgr.AddAuditEntry(client, AuditType.Account, AuditSubtype.AccountCreate, "", userName);
						}
						else
						{
							if (Log.IsInfoEnabled)
								Log.Info("No such account found and autocreation deactivated!");

							client.Out.SendLoginDenied(eLoginError.AccountNotFound);
							client.IsConnected = false;
							return;
						}
					}
					else
					{
						// check password
						if (!playerAccount.Password.StartsWith("##"))
						{
							playerAccount.Password = CryptPassword(playerAccount.Password);
						}

						if (!CryptPassword(password).Equals(playerAccount.Password))
						{
							if (Log.IsInfoEnabled)
								Log.Info("(" + client.TcpEndpointAddress + ") Wrong password!");

							client.Out.SendLoginDenied(eLoginError.IncorrectPassword);
							client.IsConnected = false;

							// Log failure
							AuditMgr.AddAuditEntry(client, AuditType.Account, AuditSubtype.AccountFailedLogin, "", userName);
							return;
						}

						// QUEUE SERVICE :^)
						if (!playerAccount.IsTester && playerAccount.PrivLevel == 1 && !string.IsNullOrEmpty(Properties.QUEUE_API_URI))
						{
							var data = new Dictionary<string, string>()
							{
								{ "name", playerAccount.Name }
							};
							var payload = new FormUrlEncodedContent(data);
							var webRequest = new HttpRequestMessage(HttpMethod.Post, Properties.QUEUE_API_URI + "/api/v1/whitelist/check")
							{
								Content = payload
							};
							var response = _httpClient.Send(webRequest);
							var statusCode = response.StatusCode;

							if (statusCode != HttpStatusCode.OK)
							{
								if (Log.IsInfoEnabled)
									Log.Info("No such account found in queue service whitelist!");

								client.Out.SendLoginDenied(eLoginError.AccountNoAccessThisGame);
								client.IsConnected = false;
								return;
							}
						}

						// save player infos
						playerAccount.LastLogin = DateTime.Now;
						playerAccount.LastLoginIP = ipAddress;
						playerAccount.LastClientVersion = ((int)client.Version).ToString();
						if (string.IsNullOrEmpty(playerAccount.Language))
						{
							playerAccount.Language = Properties.SERV_LANGUAGE;
						}

						GameServer.Database.SaveObject(playerAccount);
					}
				}

				//Save the account table
				client.Account = playerAccount;

				if (ClientService.Instance.ClientCount > Properties.MAX_PLAYERS)
				{
					if (Log.IsInfoEnabled)
						Log.InfoFormat("Too many clients connected, denied login to " + playerAccount.Name);

					client.Out.SendLoginDenied(eLoginError.TooManyPlayersLoggedIn);
					client.IsConnected = false;
					return;
				}

				client.Out.SendLoginGranted();
				client.ClientState = GameClient.eClientState.Connecting;

				// var clIP = ((IPEndPoint) client.Socket.RemoteEndPoint)?.Address.ToString();
				// var sharedClients = WorldMgr.GetClientsFromIP(clIP);
				// if (sharedClients.Count > 1)
				// {
				// 	foreach (var cl in sharedClients)
				// 	{
				// 		if (cl.Account.Name == client.Account.Name) continue;
				// 		var message = $"DUAL IP LOGIN: {client.Account.Name} is connecting from the same IP {clIP} as {cl.Account.Name} ({cl.Player?.Name} - L{cl.Player?.Level} {cl.Player?.CharacterClass.Name})";
				// 		GameServer.Instance.LogDualIPAction(message);
				// 	}
				// }

				// Log entry
				AuditMgr.AddAuditEntry(client, AuditType.Account, AuditSubtype.AccountSuccessfulLogin, "", userName);
			}
			catch (DatabaseException e)
			{
				if (Log.IsErrorEnabled)
					Log.Error("LoginRequestHandler", e);

				client.Out.SendLoginDenied(eLoginError.Error);
				client.IsConnected = false;
			}
			catch (Exception e)
			{
				if (Log.IsErrorEnabled)
					Log.Error("LoginRequestHandler", e);

				client.Out.SendLoginDenied(eLoginError.Error);
				client.IsConnected = false;
			}
			finally
			{
				client.PacketProcessor?.SendPendingPackets();

				if (client.IsConnected == false)
					client.Disconnect();
			}
		}

		public static string CryptPassword(string password)
		{
			char[] pw = password.ToCharArray();
			byte[] res = new byte[pw.Length * 2];

			for (int i = 0; i < pw.Length; i++)
			{
				res[i * 2] = (byte) (pw[i] >> 8);
				res[i * 2 + 1] = (byte) pw[i];
			}

			byte[] hash = MD5.HashData(res);
			StringBuilder stringBuilder = new();
			stringBuilder.Append("##");

			for (int i = 0; i < hash.Length; i++)
				stringBuilder.Append(hash[i].ToString("X"));

			return stringBuilder.ToString();
		}
	}
}
