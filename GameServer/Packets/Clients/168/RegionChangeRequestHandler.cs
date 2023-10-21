using System;
using System.Collections;
using System.Reflection;
using Core.Database;
using Core.GS.Quests;
using Core.GS.ServerRules;
using log4net;

namespace Core.GS.PacketHandler.Client.v168
{
	[PacketHandler(EPacketHandlerType.TCP, EClientPackets.PlayerRegionChangeRequest, "Player Region Change Request handler.", EClientStatus.PlayerInGame)]
	public class PlayerRegionChangeRequestHandler : IPacketHandler
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// Holds jump point types
		/// </summary>
		protected readonly Hashtable m_customJumpPointHandlers = new Hashtable();

		public void HandlePacket(GameClient client, GsPacketIn packet)
		{
			ushort zonePointId = client.Version >= GameClient.eClientVersion.Version1126 ? packet.ReadShortLowEndian() : packet.ReadShort();
			ERealm playerRealm = client.Player.Realm;

			// If we are in TrialsOfAtlantis then base the target jump on the current region realm instead of the players realm.
			// This is only used if zone table has the proper realms defined, otherwise it reverts to old behavior.
			if (client.Player.CurrentRegion.Expansion == (int) EClientExpansion.TrialsOfAtlantis && client.Player.CurrentZone.Realm != ERealm.None)
				playerRealm = client.Player.CurrentZone.Realm;

			WhereClause whereClause = DB.Column("Id").IsEqualTo(zonePointId);

			if (client.Account.PrivLevel == 1)
			{
				WhereClause realmFilter = DB.Column("Realm").IsEqualTo((byte) playerRealm).Or(DB.Column("Realm").IsEqualTo(0)).Or(DB.Column("Realm").IsNull());
				whereClause = whereClause.And(realmFilter);
			}

			DbZonePoint zonePoint = CoreDb<DbZonePoint>.SelectObject(whereClause);

			if (zonePoint == null)
			{
				ChatUtil.SendDebugMessage(client, $"Invalid ZonePoint. Wrong ID or mismatching realm. (ID: {zonePointId}) (playerRealm: {(byte) playerRealm})");
				return;
			}

			// Some jump points are handled code side, such as instances. As such, region may be zero in the database.
			if (zonePoint.TargetRegion == 0)
			{
				zonePoint = new()
				{
					Id = zonePointId
				};
			}

			if (client.Account.PrivLevel > 1)
				ChatUtil.SendDebugMessage(client, $"ZonePoint (ID: {zonePointId}) (TargetRegion: {zonePoint.TargetRegion}) (ClassType: {zonePoint.ClassType})");

			if (zonePoint.TargetRegion != 0)
			{
				Region reg = WorldMgr.GetRegion(zonePoint.TargetRegion);

				if (reg != null)
				{
					if (client.Player.CurrentRegion.IsCustom == false && reg.IsDisabled)
					{
						if (client.Player.Mission is not TaskDungeonMission taskDungeonMission || taskDungeonMission.TaskRegion.Skin != reg.Skin)
						{
							client.Out.SendMessage("This region has been disabled!", EChatType.CT_System, EChatLoc.CL_SystemWindow);

							if (client.Account.PrivLevel == 1)
								return;
						}
					}
				}
			}

			// Allow the region to either deny exit or handle the zonepoint in a custom way.
			if (client.Player.CurrentRegion.OnZonePoint(client.Player, zonePoint) == false)
				return;

			DbBattleground bg = GameServer.KeepManager.GetBattleground(zonePoint.TargetRegion);

			if (bg != null && client.Player.Level < bg.MinLevel && client.Player.Level > bg.MaxLevel && client.Player.RealmLevel >= bg.MaxRealmLevel)
				return;

			IJumpPointHandler customHandler = null;

			if (!string.IsNullOrEmpty(zonePoint.ClassType))
			{
				customHandler = (IJumpPointHandler) m_customJumpPointHandlers[zonePoint.ClassType];

				// Check for db change to update cached handler.
				if (customHandler != null && customHandler.GetType().FullName != zonePoint.ClassType)
					customHandler = null;

				if (customHandler == null)
				{
					// Instances need to use a special handler. This is because some instances will result in duplicated zonepoints, such as if Tir Na Nog were to be instanced for a quest.
					string typeName = client.Player.CurrentRegion.IsInstance ? "DOL.GS.ServerRules.InstanceDoorJumpPoint" : zonePoint.ClassType;
					Type type = ScriptMgr.GetType(typeName);

					if (type == null)
						Log.Error($"ZonePoint not found (ID: {zonePoint.Id}) (Class {typeName})");
					else if (!typeof(IJumpPointHandler).IsAssignableFrom(type))
						Log.Error($"ZonePoint must implement {nameof(IJumpPointHandler)} interface (ID: {zonePoint.Id}) (Class {typeName})");
					else
					{
						try
						{
							customHandler = (IJumpPointHandler) Activator.CreateInstance(type);
						}
						catch (Exception e)
						{
							customHandler = null;
							Log.Error($"Error when creating a new instance of jump point handler (ID: {zonePoint.Id}) (Class {typeName})", e);
						}
					}
				}

				if (customHandler != null)
					m_customJumpPointHandlers[zonePoint.ClassType] = customHandler;
			}

			new RegionChangeRequestHandler(client.Player, zonePoint, customHandler).Start(1);
		}

		/// <summary>
		/// Handles player region change requests
		/// </summary>
		protected class RegionChangeRequestHandler : EcsGameTimerWrapperBase
		{
			/// <summary>
			/// Checks whether player is allowed to jump
			/// </summary>
			protected readonly IJumpPointHandler m_checkHandler;

			/// <summary>
			/// The target zone point
			/// </summary>
			protected readonly DbZonePoint m_zonePoint;

			/// <summary>
			/// Constructs a new RegionChangeRequestHandler
			/// </summary>
			/// <param name="actionSource">The action source</param>
			/// <param name="zonePoint">The target zone point</param>
			/// <param name="checker">The jump point checker instance</param>
			public RegionChangeRequestHandler(GamePlayer actionSource, DbZonePoint zonePoint, IJumpPointHandler checkHandler) : base(actionSource)
			{
				m_zonePoint = zonePoint ?? throw new ArgumentNullException(nameof(zonePoint));
				m_checkHandler = checkHandler;
			}

			/// <summary>
			/// Called on every timer tick
			/// </summary>
			protected override int OnTick(EcsGameTimer timer)
			{
				GamePlayer player = (GamePlayer) timer.Owner;
				Region reg = WorldMgr.GetRegion(m_zonePoint.TargetRegion);

				if (reg != null && reg.Expansion > (int)player.Client.ClientType)
				{
					player.Out.SendMessage("Destination region (" + reg.Description + ") is not supported by your client type.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
					return 0;
				}

				try
				{
					// Check if the zonepoint has source locations set  Check prior to any zonepoint modification by handlers.
					if (m_zonePoint.SourceRegion == 0)
					{
						m_zonePoint.SourceRegion = player.CurrentRegionID;
						m_zonePoint.SourceX = player.X;
						m_zonePoint.SourceY = player.Y;
						m_zonePoint.SourceZ = player.Z;
						GameServer.Database.SaveObject(m_zonePoint);
					}

				}
				catch (Exception ex)
				{
					Log.Error("Can't save updated ZonePoint with source info.", ex);
				}

				if (m_checkHandler != null)
				{
					try
					{
						if (!m_checkHandler.IsAllowedToJump(m_zonePoint, player))
							return 0;
					}
					catch (Exception e)
					{
						if (Log.IsErrorEnabled)
							Log.Error("Jump point handler (" + m_zonePoint.ClassType + ")", e);

						player.Out.SendMessage("exception in jump point (" + m_zonePoint.Id + ") handler...", EChatType.CT_System, EChatLoc.CL_SystemWindow);
						return 0;
					}
				}

				player.MoveTo(m_zonePoint.TargetRegion, m_zonePoint.TargetX, m_zonePoint.TargetY, m_zonePoint.TargetZ, m_zonePoint.TargetHeading);
				return 0;
			}
		}
	}
}
