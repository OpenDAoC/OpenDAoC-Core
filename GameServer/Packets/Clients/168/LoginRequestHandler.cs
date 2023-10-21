using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Core.Base.Enums;
using Core.Database;
using Core.Database.Tables;
using Core.GS.Database;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.GameLoop;
using Core.GS.Packets.Server;
using Core.GS.Players.Managers;
using Core.GS.Server;
using log4net;

namespace Core.GS.Packets.Clients;

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
///		$res = "";
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
[PacketHandler(EPacketHandlerType.TCP, EClientPackets.LoginRequest, "Handles the login.", EClientStatus.None)]
public class LoginRequestHandler : IPacketHandler
{
	/// <summary>
	/// Defines a logger for this class.
	/// </summary>
	private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

	private static DateTime m_lastAccountCreateTime;
	private readonly Dictionary<string, LockCount> m_locks = new Dictionary<string, LockCount>();
	private static HttpClient _httpClient = new HttpClient();

	public void HandlePacket(GameClient client, GsPacketIn packet)
	{
		if (client == null)
			return;

		string ipAddress = client.TcpEndpointAddress;

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

		// check server status
		if (GameServer.Instance.ServerStatus == EGameServerStatus.GSS_Closed)
		{
			client.Out.SendLoginDenied(ELoginError.GameCurrentlyClosed);
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

		// Handle connection
		EnterLock(userName);

		try
		{
			DbAccount playerAccount;
			// Make sure that client won't quit
			lock (client)
			{
				GameClient.eClientState state = client.ClientState;
				if (state != GameClient.eClientState.NotConnected)
				{
					Log.DebugFormat("wrong client state on connect {0} {1}", userName, state.ToString());
					return;
				}

				if (Log.IsInfoEnabled)
					Log.Info(string.Format("({0})User {1} logging on! ({2} type:{3} add:{4})", ipAddress, userName, client.Version,
										   (client.ClientType), client.ClientAddons.ToString("G")));
				// check client already connected

				GameClient otherClient = ClientService.GetClientFromAccountName(userName);

				if (otherClient != null)
				{
					if (otherClient.ClientState == GameClient.eClientState.Connecting)
					{
						if (Log.IsInfoEnabled)
							Log.Info("User is already connecting, ignored.");

						client.Out.SendLoginDenied(ELoginError.AccountAlreadyLoggedIn);
						client.IsConnected = false;
						return;
					} // in login

					if (otherClient.ClientState == GameClient.eClientState.Linkdead)
					{
						if (Log.IsInfoEnabled)
							Log.Info("User is still being logged out from linkdeath!");

						client.Out.SendLoginDenied(ELoginError.AccountIsInLogoutProcedure);
						client.IsConnected = false;
					}
					else
					{
						if (Log.IsInfoEnabled)
							Log.Info("User already logged in!");

						client.Out.SendLoginDenied(ELoginError.AccountAlreadyLoggedIn);
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
						client.Out.SendLoginDenied(ELoginError.AccountInvalid);
					else
						Log.Warn("Client or Client.Out null on invalid name failure.  Disconnecting.");

					client.IsConnected = false;
					return;
				}
				else
				{
					playerAccount = GameServer.Database.FindObjectByKey<DbAccount>(userName);

					client.PingTime = GameLoopMgr.GetCurrentTime();

					if (playerAccount == null)
					{
						//check autocreate ...

						if (GameServer.Instance.Configuration.AutoAccountCreation && ServerProperty.ALLOW_AUTO_ACCOUNT_CREATION)
						{
							// autocreate account
							if (string.IsNullOrEmpty(password))
							{
								client.Out.SendLoginDenied(ELoginError.AccountInvalid);
								client.IsConnected = false;

								if (Log.IsInfoEnabled)
									Log.Info("Account creation failed, no password set for Account: " + userName);

								return;
							}

							// check for account bombing
							TimeSpan ts;
							var allAccByIp = CoreDb<DbAccount>.SelectObjects(DB.Column("LastLoginIP").IsEqualTo(ipAddress));
							int totalacc = 0;
							foreach (DbAccount ac in allAccByIp)
							{
								ts = DateTime.Now - ac.CreationDate;
								if (ts.TotalMinutes < ServerProperty.TIME_BETWEEN_ACCOUNT_CREATION_SAMEIP && totalacc > 1)
								{
									Log.Warn("Account creation: too many from same IP within set minutes - " + userName + " : " + ipAddress);
									client.Out.SendLoginDenied(ELoginError.PersonalAccountIsOutOfTime);
									client.IsConnected = false;
									return;
								}

								totalacc++;
							}
							if (totalacc >= ServerProperty.TOTAL_ACCOUNTS_ALLOWED_SAMEIP)
							{
								Log.Warn("Account creation: too many accounts created from same ip - " + userName + " : " + ipAddress);
								client.Out.SendLoginDenied(ELoginError.AccountNoAccessThisGame);
								client.IsConnected = false;
								return;
							}

							// per timeslice - for preventing account bombing via different ip
							if (ServerProperty.TIME_BETWEEN_ACCOUNT_CREATION > 0)
							{
								ts = DateTime.Now - m_lastAccountCreateTime;
								if (ts.TotalMinutes < ServerProperty.TIME_BETWEEN_ACCOUNT_CREATION)
								{
									Log.Warn("Account creation: time between account creation too small - " + userName + " : " + ipAddress);
									client.Out.SendLoginDenied(ELoginError.PersonalAccountIsOutOfTime);
									client.IsConnected = false;
									return;
								}
							}

							m_lastAccountCreateTime = DateTime.Now;

							playerAccount = new DbAccount();
							playerAccount.Name = userName;
							playerAccount.Password = CryptPassword(password);
							playerAccount.Realm = 0;
							playerAccount.CreationDate = DateTime.Now;
							playerAccount.LastLogin = DateTime.Now;
							playerAccount.LastLoginIP = ipAddress;
							playerAccount.LastClientVersion = ((int)client.Version).ToString();
							playerAccount.Language = ServerProperty.SERV_LANGUAGE;
							playerAccount.PrivLevel = 1;

							if (Log.IsInfoEnabled)
								Log.Info("New account created: " + userName);

							GameServer.Database.AddObject(playerAccount);

							// Log account creation
							AuditMgr.AddAuditEntry(client, EAuditType.Account, EAuditSubType.AccountCreate, "", userName);
						}
						else
						{
							if (Log.IsInfoEnabled)
								Log.Info("No such account found and autocreation deactivated!");

							client.Out.SendLoginDenied(ELoginError.AccountNotFound);
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
								Log.Info("(" + client.TcpEndpoint + ") Wrong password!");

							client.Out.SendLoginDenied(ELoginError.WrongPassword);
							client.IsConnected = false;

							// Log failure
							AuditMgr.AddAuditEntry(client, EAuditType.Account, EAuditSubType.AccountFailedLogin, "", userName);
							return;
						}

						// QUEUE SERVICE :^)
						if (!playerAccount.IsTester && playerAccount.PrivLevel == 1 && !string.IsNullOrEmpty(ServerProperty.QUEUE_API_URI))
                        {
							var data = new Dictionary<string, string>()
                            {
								{ "name", playerAccount.Name }
                            };
							var payload = new FormUrlEncodedContent(data);
							var webRequest = new HttpRequestMessage(HttpMethod.Post, ServerProperty.QUEUE_API_URI + "/api/v1/whitelist/check")
							{
								Content = payload
							};
							var response = _httpClient.Send(webRequest);
							var statusCode = response.StatusCode;

							if (statusCode != HttpStatusCode.OK)
                            {
								if (Log.IsInfoEnabled)
									Log.Info("No such account found in queue service whitelist!");

								client.Out.SendLoginDenied(ELoginError.AccountNoAccessThisGame);
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
							playerAccount.Language = ServerProperty.SERV_LANGUAGE;
						}

						GameServer.Database.SaveObject(playerAccount);
					}
				}

				//Save the account table
				client.Account = playerAccount;
				
				// create session ID here to disable double login bug
				if (ClientService.ClientCount > GameServer.Instance.Configuration.MaxClientCount)
				{
					if (Log.IsInfoEnabled)
						Log.InfoFormat("Too many clients connected, denied login to " + playerAccount.Name);

					client.Out.SendLoginDenied(ELoginError.TooManyPlayersLoggedIn);
					client.IsConnected = false;
					return;
				}

				client.Out.SendLoginGranted();
				client.ClientState = GameClient.eClientState.Connecting;
				
				GameServer.Database.FillObjectRelations(client.Account);

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
				AuditMgr.AddAuditEntry(client, EAuditType.Account, EAuditSubType.AccountSuccessfulLogin, "", userName);
			}
		}
		catch (DatabaseException e)
		{
			if (Log.IsErrorEnabled)
				Log.Error("LoginRequestHandler", e);

			client.Out.SendLoginDenied(ELoginError.CannotAccessUserAccount);
			client.IsConnected = false;
		}
		catch (Exception e)
		{
			if (Log.IsErrorEnabled)
				Log.Error("LoginRequestHandler", e);

			client.Out.SendLoginDenied(ELoginError.CannotAccessUserAccount);
			client.IsConnected = false;
		}
		finally
		{
			client.PacketProcessor?.ProcessTcpQueue();

			if (client.IsConnected == false)
			{
				client.Disconnect();
				ClientService.OnClientDisconnect(client);
			}

			ExitLock(userName);
		}
	}

	public static string CryptPassword(string password)
	{
		MD5 md5 = new MD5CryptoServiceProvider();

		char[] pw = password.ToCharArray();

		var res = new byte[pw.Length * 2];
		for (int i = 0; i < pw.Length; i++)
		{
			res[i * 2] = (byte)(pw[i] >> 8);
			res[i * 2 + 1] = (byte)(pw[i]);
		}

		byte[] bytes = md5.ComputeHash(res);

		var crypted = new StringBuilder();
		crypted.Append("##");
		for (int i = 0; i < bytes.Length; i++)
		{
			crypted.Append(bytes[i].ToString("X"));
		}

		return crypted.ToString();
	}

	/// <summary>
	/// Acquires the lock on account.
	/// </summary>
	/// <param name="accountName">Name of the account.</param>
	private void EnterLock(string accountName)
	{
		// Safety check
		if (accountName == null)
		{
			accountName = string.Empty;
			Log.Warn("(Enter) No account name");
		}

		LockCount lockObj = null;
		lock (m_locks)
		{
			// Get/create lock object
			if (!m_locks.TryGetValue(accountName, out lockObj))
			{
				lockObj = new LockCount();
				m_locks.Add(accountName, lockObj);
			}

			if (lockObj == null)
			{
				Log.Error("(Enter) No lock object for account: '" + accountName + "'");
			}
			else
			{
				// Increase count of locks
				lockObj.count++;
			}
		}

		if (lockObj != null)
		{
			Monitor.Enter(lockObj);
		}
	}

	/// <summary>
	/// Releases the lock on account.
	/// </summary>
	/// <param name="accountName">Name of the account.</param>
	private void ExitLock(string accountName)
	{
		// Safety check
		if (accountName == null)
		{
			accountName = string.Empty;
			Log.Warn("(Exit) No account name");
		}

		LockCount lockObj = null;
		lock (m_locks)
		{
			// Get lock object
			if (!m_locks.TryGetValue(accountName, out lockObj))
			{
				Log.Error("(Exit) No lock object for account: '" + accountName + "'");
			}

			// Remove lock object if no more locks on it
			if (lockObj != null)
			{
				if (--lockObj.count <= 0)
				{
					m_locks.Remove(accountName);
				}
			}
		}

		Monitor.Exit(lockObj);
	}

	#region Nested type: LockCount

	/// <summary>
	/// This class is used as lock object. Contains the count of locks held.
	/// </summary>
	private class LockCount
	{
		/// <summary>
		/// Count of locks held.
		/// </summary>
		public int count;
	}

	#endregion
}