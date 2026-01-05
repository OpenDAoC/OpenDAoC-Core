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

using DOL.GS.Keeps;

namespace DOL.GS.PacketHandler.Client.v168
{
	[PacketHandlerAttribute(PacketHandlerType.TCP, eClientPackets.ShowWarmapRequest, "Show Warmap", eClientStatus.PlayerInGame)]
	public class WarmapShowRequestHandler : IPacketHandler
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

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
						(client.Player.InCombat || client.Player.CurrentRegionID != 163 || GameRelic.IsPlayerCarryingRelic(client.Player)))
					{
						return;
					}

					AbstractGameKeep keep = null;
                                        AbstractGameKeep supply1 = null;
                                        AbstractGameKeep supply2 = null;

					if (keepId > 6)
					{
						keep = GameServer.KeepManager.GetKeepByID(keepId);
					}

					if (keep == null && keepId > 6)
					{
						return;
					}

					if (client.Account.PrivLevel == (int)ePrivLevel.Player)
					{
						bool found = false;

						if (keep != null)
						{
							// if we are requesting to teleport to a keep we need to check that keeps requirements first

							if (keep.Realm != client.Player.Realm)
							{
								return;
							}

							if (keep is GameKeep && ((keep as GameKeep).OwnsAllTowers == false || keep.InCombat))
							{
								return;
							}

							// start a basic supplyline check (we can enhance this later)
                                                          //based on the center keeps in NF
							switch (client.Player.Realm)
                                                        {
                                                                case eRealm.Midgard:
                                                                        supply1 = GameServer.KeepManager.GetKeepByID(79);   // Glenlock
                                                                        supply2 = GameServer.KeepManager.GetKeepByID(75);   // Bledmeer
                                                                        break;
                                                                case eRealm.Albion:
                                                                        supply1 = GameServer.KeepManager.GetKeepByID(50);   // Benowyc
                                                                        supply2 = GameServer.KeepManager.GetKeepByID(53);   // Boldiam
                                                                        break;
                                                                case eRealm.Hibernia:
                                                                        supply1 = GameServer.KeepManager.GetKeepByID(103);   // nGed
                                                                        supply2 = GameServer.KeepManager.GetKeepByID(100);   // Crauchon
                                                                        break;
                                                        }
							// this handles only keeps outside our own realm
                                                           // can enhance this later too
                                                        if (client.Player.Realm != keep.OriginalRealm && (supply1.Realm != supply1.OriginalRealm ||
                                                                supply2.Realm != supply2.OriginalRealm))
                                                        {
                                                            client.Player.Out.SendMessage("The cannot teleport there at the moment.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                                            return;
                                                        }

							// need to check keeps outside our relam that we own, if we can tele or not to them based 
								//on supply lines in our realm


						}

						if (client.Player.CurrentRegionID == 163)
						{
							// We are in the frontiers and all keep requirements are met or we are not near a keep
							// this may be a portal stone in the RvR village, for example

							foreach (GameStaticItem item in client.Player.GetItemsInRadius(WorldMgr.INTERACT_DISTANCE))
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
					}

					int x = 0;
					int y = 0;
					int z = 0;
					ushort heading = 0;
					switch (keepId)
					{
						//sauvage
						case 1:
						//snowdonia
						case 2:
						//svas
						case 3:
						//vind
						case 4:
						//ligen
						case 5:
						//cain
						case 6:
						{
							GameServer.KeepManager.GetBorderKeepLocation(keepId, out x, out y, out z, out heading);
							break;
						}

						// Note: hard coded the relic towns because the keepId are
                                                //      already used for portal keeps in BG's so cannot use the database

                                                // catterick hamlet (alb)
                                                case 10:
                                                        x = 678437;
                                                        y = 568999;
                                                        z = 8104;
                                                        heading = 1130;
                                                        break;

                                                // dinas emrys (alb)
                                                case 11:
                                                        x = 566394;
                                                        y = 669741;
                                                        z = 8088;
                                                        heading = 4056;
                                                        break;

                                                // godrborg (mid)
                                                case 12:
                                                        x = 597577;
                                                        y = 303911;
                                                        z = 8088;
                                                        heading = 49;
                                                        break;

                                                // rensamark (mid)
                                                case 13:
                                                        x = 701001;
                                                        y = 418654;
                                                        z = 8088;
                                                        heading = 1921;
                                                        break;

                                                //crair treflan
                                                case 14:
                                                        x = 374153;
                                                        y = 574388;
                                                        z = 8040;
                                                        heading = 1979;
                                                        break;

                                                //magh tuireadh
                                                case 15:
                                                        x = 481218;
                                                        y = 667586;
                                                        z = 7879;
                                                        heading = 3727;
                                                        break;

						default:
						{
							if (keep != null && keep is GameKeep)
							{
								FrontiersPortalStone stone = keep.TeleportStone;
								if (stone != null) 
								{
									heading = stone.Heading;
									z = stone.Z;
									stone.GetTeleportLocation(out x, out y);
								}
								else
								{
									x = keep.X;
									y = keep.Y;
									z = keep.Z+150;
									heading = keep.Heading;
								}
							}
							break;
						}
					}

					if (x != 0)
					{
						client.Player.MoveTo(163, x, y, z, heading);
					}

					break;
				}
			}
		}
	}

}