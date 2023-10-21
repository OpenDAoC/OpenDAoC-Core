using System.Collections.Generic;
using Core.Base.Enums;
using Core.Database;
using Core.Database.Tables;
using Core.GS.Database;
using Core.GS.ECS;
using Core.GS.Keeps;
using Core.GS.ServerProperties;
using Core.Language;

namespace Core.GS.PacketHandler.Client.v168
{
	[PacketHandler(EPacketHandlerType.TCP, EClientPackets.DoorRequest, "Door Interact Request Handler", EClientStatus.PlayerInGame)]
	public class DoorRequestHandler : IPacketHandler
	{
		public static int m_handlerDoorID;

		/// <summary>
		/// door index which is unique
		/// </summary>
		public void HandlePacket(GameClient client, GsPacketIn packet)
		{
			var doorID = (int) packet.ReadInt();
			m_handlerDoorID = doorID;
			var doorState = (byte) packet.ReadByte();
			int doorType = doorID / 100000000;

			int radius = Properties.WORLD_PICKUP_DISTANCE * 4;
			int zoneDoor = doorID / 1000000;

			string debugText = "";

			// For ToA the client always sends the same ID so we need to construct an id using the current zone
			if (client.Player.CurrentRegion.Expansion == (int)EClientExpansion.TrialsOfAtlantis)
			{
				debugText = $"ToA DoorID:{doorID} ";

				doorID -= zoneDoor * 1000000;
				zoneDoor = client.Player.CurrentZone.ID;
				doorID += zoneDoor * 1000000;
				m_handlerDoorID = doorID;

				// experimental to handle a few odd TOA door issues
				if (client.Player.CurrentRegion.IsDungeon)
					radius *= 4;
			}

			// debug text
			if (client.Account.PrivLevel > 1 || Properties.ENABLE_DEBUG)
			{
				if (doorType == 7)
				{
					int ownerKeepId = (doorID / 100000) % 1000;
					int towerNum = (doorID / 10000) % 10;
					int keepID = ownerKeepId + towerNum * 256;
					int componentID = (doorID / 100) % 100;
					int doorIndex = doorID % 10;
					client.Out.SendDebugMessage($"Keep Door ID:{doorID} state:{doorState} (Owner Keep:{ownerKeepId} KeepID:{keepID} ComponentID:{componentID} DoorIndex:{doorIndex} TowerNumber:{towerNum})");

					if (keepID > 255 && ownerKeepId < 10)
					{
						ChatUtil.SendDebugMessage(client, "Warning: Towers with an Owner Keep ID < 10 will have untargetable doors!");
					}
				}
				else if (doorType == 9)
				{
					int doorIndex = doorID - doorType * 10000000;
					client.Out.SendDebugMessage($"House DoorID:{doorID} state:{doorState} (doorType:{doorType} doorIndex:{doorIndex})");
				}
				else
				{
					int fixture = (doorID - zoneDoor * 1000000);
					int fixturePiece = fixture;
					fixture /= 100;
					fixturePiece = fixturePiece - fixture * 100;

					client.Out.SendDebugMessage($"{debugText}DoorID:{doorID} state:{doorState} zone:{zoneDoor} fixture:{fixture} fixturePiece:{fixturePiece} Type:{doorType}");
				}
			}

			if (client.Player.TargetObject is GameDoor && !client.Player.IsWithinRadius(client.Player.TargetObject, radius))
			{
				client.Player.Out.SendMessage("You are too far to open this door", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
				return;
			}

			var door = CoreDb<DbDoor>.SelectObject(DB.Column("InternalID").IsEqualTo(doorID));
			if (door != null)
			{
				if (doorType == 7 || doorType == 9)
				{
					new ChangeDoorAction(client.Player, doorID, doorState, radius).Start(1);
					return;
				}

				if (client.Account.PrivLevel == 1)
				{
					if (door.Locked == 0)
					{
						if (door.Health == 0)
						{
							new ChangeDoorAction(client.Player, doorID, doorState, radius).Start(1);
							return;
						}

						if (GameServer.Instance.Configuration.ServerType == EGameServerType.GST_PvP ||
                            GameServer.Instance.Configuration.ServerType == EGameServerType.GST_PvE)
						{
							if (door.Realm != 0)
							{
								new ChangeDoorAction(client.Player, doorID, doorState, radius).Start(1);
								return;
							}
						}
						else
						{
							if (client.Player.Realm == (ERealm) door.Realm || door.Realm == 6)
							{
								new ChangeDoorAction(client.Player, doorID, doorState, radius).Start(1);
								return;
							}
						}
					}
				}
                else if (client.Account.PrivLevel > 1)
				{
					client.Out.SendDebugMessage("GM: Forcing locked door open. ");
					client.Out.SendDebugMessage($"PosternDoor: {door.IsPostern}");

					new ChangeDoorAction(client.Player, doorID, doorState, radius).Start(1);
					return;
				}
			}
            else // door == null
			{
				if (doorType != 9 && client.Account.PrivLevel > 1 && client.Player.CurrentRegion.IsInstance == false)
				{
					if (client.Player.TempProperties.GetProperty(DoorMgr.WANT_TO_ADD_DOORS, false))
					{
						client.Player.Out.SendCustomDialog(
							"This door is not in the database. Place yourself nearest to this door and click Accept to add it.", AddingDoor);
					}
					else
					{
						client.Player.Out.SendMessage(
							"This door is not in the database. Use '/door show' to enable the add door dialog when targeting doors.",
							EChatType.CT_Important, EChatLoc.CL_SystemWindow);
					}
				}

				new ChangeDoorAction(client.Player, doorID, doorState, radius).Start(1);
				return;
			}
		}


		public void AddingDoor(GamePlayer player, byte response)
		{
			if (response != 0x01)
				return;

			int doorType = m_handlerDoorID/100000000;
			if (doorType == 7)
			{
				GuardPositionMgr.CreateDoor(m_handlerDoorID, player);
			}
			else
			{
				var door = new DbDoor();
				door.ObjectId = null;
				door.InternalID = m_handlerDoorID;
				door.Name = "door";
				door.Type = m_handlerDoorID/100000000;
				door.Level = 20;
				door.Realm = 6;
				door.X = player.X;
				door.Y = player.Y;
				door.Z = player.Z;
				door.Heading = player.Heading;
				GameServer.Database.AddObject(door);

				player.Out.SendMessage("Added door " + m_handlerDoorID + " to the database!", EChatType.CT_Important,
				                       EChatLoc.CL_SystemWindow);
				GameServer.Database.SaveObject(door);
				DoorMgr.Init();
			}
		}

		/// <summary>
		/// Handles the door state change actions
		/// </summary>
		protected class ChangeDoorAction : EcsGameTimerWrapperBase
		{
			/// <summary>
			/// The target door Id
			/// </summary>
			protected readonly int m_doorId;

			/// <summary>
			/// The door state
			/// </summary>
			protected readonly int m_doorState;

			/// <summary>
			/// allowed distance to door
			/// </summary>
			protected readonly int m_radius;

			/// <summary>
			/// Constructs a new ChangeDoorAction
			/// </summary>
			/// <param name="actionSource">The action source</param>
			/// <param name="doorId">The target door Id</param>
			/// <param name="doorState">The door state</param>
			public ChangeDoorAction(GamePlayer actionSource, int doorId, int doorState, int radius)
				: base(actionSource)
			{
				m_doorId = doorId;
				m_doorState = doorState;
				m_radius = radius;
			}

			/// <summary>
			/// Called on every timer tick
			/// </summary>
			protected override int OnTick(EcsGameTimer timer)
			{
				GamePlayer player = (GamePlayer) timer.Owner;
				List<GameDoorBase> doorList = DoorMgr.getDoorByID(m_doorId);

				if (doorList.Count > 0)
				{
					bool success = false;
					foreach (GameDoorBase mydoor in doorList)
					{
						if (success)
							break;
						if (mydoor is GameKeepDoor)
						{
							var door = mydoor as GameKeepDoor;
							//portal keeps left click = right click
							if (door.Component.Keep is GameKeepTower && door.Component.Keep.KeepComponents.Count > 1)
								door.Interact(player);
							success = true;
						}
						else
						{
							if (player.IsWithinRadius(mydoor, m_radius))
							{
								if (m_doorState == 0x01)
									mydoor.Open(player);
								else
									mydoor.Close(player);
								success = true;
							}
						}
					}

					if (!success)
						player.Out.SendMessage(
							LanguageMgr.GetTranslation(player.Client.Account.Language, "DoorRequestHandler.OnTick.TooFarAway", doorList[0].Name),
							EChatType.CT_System, EChatLoc.CL_SystemWindow);
				}
				else
				{
					//new frontiers we don't want this, i.e. relic gates etc
					if (player.CurrentRegionID == 163 && player.Client.Account.PrivLevel == 1)
						return 0;
					/*
					//create a bug report
					BugReport report = new BugReport();
					report.DateSubmitted = DateTime.Now;
					report.ID = GameServer.Database.GetObjectCount<BugReport>() + 1;
					report.Message = "There is a missing door at location Region: " + player.CurrentRegionID + " X:" + player.X + " Y: " + player.Y + " Z: " + player.Z;
					report.Submitter = player.Name;
					GameServer.Database.AddObject(report);
					 */

					player.Out.SendDebugMessage("Door {0} not found in door list, opening via GM door hack.", m_doorId);

					//else basic quick hack
					var door = new GameDoor();
					door.DoorID = m_doorId;
					door.X = player.X;
					door.Y = player.Y;
					door.Z = player.Z;
					door.Realm = ERealm.Door;
					door.CurrentRegion = player.CurrentRegion;
					
					if (player.IsWithinRadius(door, m_radius))
					{
						door.Open(player);
					}
					else
					{
						player.Out.SendMessage(
							LanguageMgr.GetTranslation(player.Client.Account.Language, "DoorRequestHandler.OnTick.TooFarAway", doorList[0].Name),
							EChatType.CT_System, EChatLoc.CL_SystemWindow);
					}
				}

				return 0;
			}
		}
	}
}
