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
using System.Collections;
using System.Collections.Generic; // Hinzugefügt: Für Dictionary
using DOL.GS.Keeps;

namespace DOL.GS.PacketHandler.Client.v168
{
	[PacketHandlerAttribute(PacketHandlerType.TCP, eClientPackets.ShowWarmapRequest, "Show Warmap", eClientStatus.PlayerInGame)]
	public class WarmapShowRequestHandler : PacketHandler
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>Definiert die X/Y/Z-Koordinaten und die Blickrichtung für einen Teleport.</summary>
		public struct TeleportLocation
		{
			public int X;
			public int Y;
			public int Z;
			public ushort Heading;

			public TeleportLocation(int x, int y, int z, ushort heading)
			{
				X = x;
				Y = y;
				Z = z;
				Heading = heading;
			}
		}

		private static readonly Dictionary<ushort, TeleportLocation> KeepTeleportLocations = new Dictionary<ushort, TeleportLocation>()
		{
            // Hibernia Keeps
            { 106, new TeleportLocation(409943, 607106, 8588, 3075) }, // Ailinne
            { 105, new TeleportLocation(448260, 666891, 6812, 4095) }, // Scataigh
            { 103, new TeleportLocation(442211, 578542, 8972, 2042) }, // nGed
            { 104, new TeleportLocation(475451, 596571, 8460, 3066) }, // da Behn
            { 102, new TeleportLocation(474569, 543271, 8324, 3071) }, // Bolg
            { 101, new TeleportLocation(431337, 508335, 8844, 2049) }, // Crim
            { 100, new TeleportLocation(472164, 501244, 8868, 2174) }, // Crauchon
            
            // Midgard Keeps
            { 80, new TeleportLocation(639349, 345895, 8756, 1027) }, // Fensalir
            { 81, new TeleportLocation(684664, 391064, 8788, 2048) }, // Arvakre
            { 79, new TeleportLocation(609628, 377849, 8996, 2037) }, // Glenlock
            { 77, new TeleportLocation(639950, 412130, 8516, 2497) }, // Hlidskialf
            { 78, new TeleportLocation(603012, 408550, 8372, 1020) }, // Blendrake
            { 76, new TeleportLocation(534443, 365387, 8812, 3088) }, // Nottmor
            { 75, new TeleportLocation(533670, 407442, 9036, 1022) }, // Bledmeer
            
            // Albion Keeps
            { 56, new TeleportLocation(627140, 607067, 8668, 2209) }, // Renaris
            { 55, new TeleportLocation(600731, 648787, 8516, 105) },  // Hurbury
            { 53, new TeleportLocation(606521, 574815, 8972, 1) },    // Bold
            { 54, new TeleportLocation(581780, 602761, 8588, 2047) }, // Sursbrook
            { 52, new TeleportLocation(573112, 549037, 8324, 1305) }, // Erasleigh
            { 51, new TeleportLocation(617045, 510315, 8844, 4089) }, // Berkstead
            { 50, new TeleportLocation(576554, 502127, 8868, 1241) }, // Benowyc
        };

		public void HandlePacket(GameClient client, GSPacketIn packet)
		{
			int code = packet.ReadByte();
			int RealmMap = packet.ReadByte();
			int keepId = packet.ReadByte();

			if (client == null || client.Player == null)
				return;


			// this stops teleport to keeps working in NF (because the keeps are numbered wrong, or you can just update the client version
			//&& (int)client.Version < (int)GameClient.eClientVersion.Version1115)

			//hack fix new keep ids
			else if ((int)client.Version >= (int)GameClient.eClientVersion.Version190)
			{
				if (keepId >= 82)
					keepId -= 7;
				else if (keepId >= 62)
					keepId -= 12;
			}

			switch (code)
			{
				//warmap open
				//warmap update
				case 0:
					{
						client.Player.WarMapPage = (byte)RealmMap;
						break;
					}
				case 1:
					{
						client.Out.SendWarmapUpdate(GameServer.KeepManager.GetKeepsByRealmMap(client.Player.WarMapPage));
						WarMapMgr.SendFightInfo(client);
						break;
					}
				//teleport
				case 2:
					{
						client.Out.SendWarmapUpdate(GameServer.KeepManager.GetKeepsByRealmMap(client.Player.WarMapPage));
						WarMapMgr.SendFightInfo(client);

						if (client.Account.PrivLevel == (int)ePrivLevel.Player &&
							(client.Player.CurrentRegionID != 163 || GameRelic.IsPlayerCarryingRelic(client.Player)))
						{
							return;
						}

						AbstractGameKeep keep = null;
						AbstractGameKeep supply1 = null;
						AbstractGameKeep supply2 = null;

						// keepId > 6 sind die RvR Keeps. IDs 1-6 sind Border Keeps (Sauvage, Snowdonia, etc.)
						if (keepId > 6)
						{
							keep = GameServer.KeepManager.GetKeepByID((ushort)keepId); // Cast zu ushort für KeepID
						}

						if (keep == null && keepId > 6)
						{
							return;
						}

						if (client.Account.PrivLevel == (int)ePrivLevel.Player)
						{
							bool found = false;

							// ----------------------------------------------------
							// 1. ZIEL-KEEP PRÜFUNG (Muss dem eigenen Realm gehören und alle Türme besitzen)
							// ----------------------------------------------------
							if (keep != null)
							{
								// if we are requesting to teleport to a keep we need to check that keeps requirements first

								if (keep.Realm != client.Player.Realm)
								{
									client.Player.Out.SendMessage("You cannot teleport there at the moment.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
									return;
								}

								// Use pattern matching for clean casting and checking
								if (keep is GameKeep targetKeep && (targetKeep.OwnsAllTowers == false))
								{
									client.Player.Out.SendMessage("You cannot teleport there at the moment.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
									return;
								}
							}


							if (client.Player.CurrentRegionID == 163)
							{
								// We are in the frontiers and all keep requirements are met or we are not near a keep
								// this may be a portal stone in the RvR village, for example

								foreach (GameStaticItem item in client.Player.GetItemsInRadius(WorldMgr.PORAL_DISTANCE))
								{
									if (item is FrontiersPortalStone)
									{
										found = true;
										break;
									}
								}
							}

							if (!found)
							{
								client.Player.Out.SendMessage("You cannot teleport unless you are near a valid portal stone.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}

							// Check Keep & Tower status closest to the player
							AbstractGameKeep source_keep = GameServer.KeepManager.GetClosestKeepToSpot(client.Player.CurrentRegionID, client.Player, 10000);

							if (source_keep != null && source_keep is GameKeep sourceGameKeep)
							{
								if (sourceGameKeep.OwnsAllTowers == false)
								{
									client.Player.Out.SendMessage("You cannot teleport from here at the moment.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
									return;
								}
							}

						} // end privlevel player check
						int x = 0;
						int y = 0;
						int z = 0;
						ushort heading = 0;

						// Hier wird keepId als Zahl interpretiert, was korrekt ist.
						switch ((ushort)keepId)
						{
							// Border Keeps (ID 1-6)
							case 1: //sauvage
							case 2: //snowdonia
							case 3: //svas
							case 4: //vind
							case 5: //ligen
							case 6: //cain
								{
									GameServer.KeepManager.GetBorderKeepLocation((ushort)keepId, out x, out y, out z, out heading);
									break;
								}

							// Relic Towns (ID 10-15)
							case 10: // catterick hamlet (alb)
								x = 678437;
								y = 568999;
								z = 8104;
								heading = 1130;
								break;
							case 11: // dinas emrys (alb)
								x = 566394;
								y = 669741;
								z = 8088;
								heading = 4056;
								break;
							case 12: // godrborg (mid)
								x = 597577;
								y = 303911;
								z = 8088;
								heading = 49;
								break;
							case 13: // rensamark (mid)
								x = 701001;
								y = 418654;
								z = 8088;
								heading = 1921;
								break;
							case 14: //crair treflan (hib)
								x = 374223;
								y = 573036;
								z = 8040;
								heading = 1052;
								break;
							case 15: //magh tuireadh (hib)
								x = 481218;
								y = 667586;
								z = 7879;
								heading = 3727;
								break;

							// Standard Keeps (ID 50-106)
							default:
								{
									// keep-Objekt MUSS hier existieren, wenn wir diesen Block erreichen,
									// da es oben bei keepId > 6 gesetzt wurde.
									// Wir verwenden C# Pattern Matching (keep is GameKeep gameKeep),
									// um das Casten sicher zu handhaben und zu prüfen, ob es ein GameKeep ist.
									if (keep is GameKeep gameKeep)
									{
										FrontiersPortalStone stone = gameKeep.TeleportStone;
										if (stone != null)
										{
											// --- OPTIMIERTE LOGIK DURCH DICTIONARY ---
											if (KeepTeleportLocations.TryGetValue(gameKeep.KeepID, out TeleportLocation location))
											{
												x = location.X;
												y = location.Y;
												z = location.Z;
												heading = location.Heading;
											}
										}
									}
								}
							break;
						}
						if (x != 0)
						{
							client.Player.MoveTo(163, x, y, z, heading);
						}

						break;
					} // Ende case 2
			} // Ende switch (code)
		} // Ende HandlePacket
	} // Ende WarmapShowRequestHandler
} // Ende Namespace