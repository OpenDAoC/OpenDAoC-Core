using Core.GS.Enums;
using Core.GS.Keeps;
using Core.GS.Packets.Server;
using Core.GS.Players.Clients;

namespace Core.GS.Packets.Clients;

[PacketHandler(EPacketHandlerType.TCP, EClientPackets.ShowWarmapRequest, "Show Warmap", EClientStatus.PlayerInGame)]
public class WarmapShowRequestHandler : IPacketHandler
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

	public void HandlePacket(GameClient client, GsPacketIn packet)
	{
		int code = packet.ReadByte();
		int RealmMap = packet.ReadByte();
		int keepId = packet.ReadByte();

		if (client == null || client.Player == null)
			return;

		//hack fix new keep ids
		else if ((int)client.Version >= (int)GameClient.eClientVersion.Version190 && (int)client.Version < (int)GameClient.eClientVersion.Version1115)
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

					if (client.Account.PrivLevel == (int)EPrivLevel.Player &&
						(client.Player.InCombat || client.Player.CurrentRegionID != 163 || GameRelic.IsPlayerCarryingRelic(client.Player)))
					{
						return;
					}

					AGameKeep keep = null;

					if (keepId > 6)
					{
						keep = GameServer.KeepManager.GetKeepByID(keepId);
					}

					if (keep == null && keepId > 6)
					{
						return;
					}

					if (client.Account.PrivLevel == (int)EPrivLevel.Player)
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

							// Missing: Supply line check
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
							client.Player.Out.SendMessage("You cannot teleport unless you are near a valid portal stone.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
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