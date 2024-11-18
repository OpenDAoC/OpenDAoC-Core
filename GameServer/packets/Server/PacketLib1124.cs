using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using DOL.AI.Brain;
using DOL.Database;
using DOL.GS.Keeps;
using DOL.GS.Quests;
using DOL.Language;
using log4net;

namespace DOL.GS.PacketHandler
{
	[PacketLib(1124, GameClient.eClientVersion.Version1124)]
	public class PacketLib1124 : PacketLib1123
	{
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private const ushort MAX_STORY_LENGTH = 1000; // Via trial and error, 1.108 client.

		/// <summary>
		/// Constructs a new PacketLib for Client Version 1.124
		/// </summary>
		/// <param name="client">the gameclient this lib is associated with</param>
		public PacketLib1124(GameClient client) : base(client)
		{
			icons = 1;
		}

		public override void SendKeepInfo(IGameKeep keep)
		{
			if (m_gameClient.Player == null)
				return;

			using (GSTCPPacketOut pak = new GSTCPPacketOut(GetPacketCode(eServerPackets.KeepInfo)))
			{
				pak.WriteShort((ushort)keep.KeepID);
				pak.WriteShort(0);
				pak.WriteInt((uint)keep.X);
				pak.WriteInt((uint)keep.Y);
				pak.WriteShort((ushort)keep.Heading);
				pak.WriteByte((byte)keep.Realm);
				pak.WriteByte((byte)keep.Level);//level
				pak.WriteShort(0);//unk
				pak.WriteByte(0);//model // patch 0072
				pak.WriteByte(0);//unk

				SendTCP(pak);
			}
		}

		public override void SendLivingEquipmentUpdate(GameLiving living)
		{
			using (GSTCPPacketOut pak = new GSTCPPacketOut(GetPacketCode(eServerPackets.EquipmentUpdate)))
			{
				ICollection<DbInventoryItem> items = null;
				if (living.Inventory != null)
					items = living.Inventory.VisibleItems;

				pak.WriteShort((ushort)living.ObjectID);
				pak.WriteByte((byte)living.VisibleActiveWeaponSlots);
				pak.WriteByte((byte)living.CurrentSpeed); // new in 189b+, speed
				pak.WriteByte((byte)((living.IsCloakInvisible ? 0x01 : 0x00) | (living.IsHelmInvisible ? 0x02 : 0x00))); // new in 189b+, cloack/helm visibility
				pak.WriteByte((byte)((living.IsCloakHoodUp ? 0x01 : 0x00) | (int)living.rangeAttackComponent.ActiveQuiverSlot)); //bit0 is hood up bit4 to 7 is active quiver

				if (items != null)
				{
					pak.WriteByte((byte)items.Count);
					foreach (DbInventoryItem item in items)
					{
						ushort model = (ushort)(item.Model & 0x1FFF);
						int slot = item.SlotPosition;
						//model = GetModifiedModel(model);
						int texture = item.Emblem != 0 ? item.Emblem : item.Color;
						if (item.SlotPosition == Slot.LEFTHAND || item.SlotPosition == Slot.CLOAK) // for test only cloack and shield
							slot = slot | ((texture & 0x010000) >> 9); // slot & 0x80 if new emblem
						pak.WriteByte((byte)slot);
						if ((texture & ~0xFF) != 0)
							model |= 0x8000;
						else if ((texture & 0xFF) != 0)
							model |= 0x4000;
						if (item.Effect != 0)
							model |= 0x2000;

						pak.WriteShort(model);

						if (item.SlotPosition > Slot.RANGED || item.SlotPosition < Slot.RIGHTHAND)
							pak.WriteByte((byte)item.Extension);

						if ((texture & ~0xFF) != 0)
							pak.WriteShort((ushort)texture);
						else if ((texture & 0xFF) != 0)
							pak.WriteByte((byte)texture);
						if (item.Effect != 0)
							pak.WriteShort((ushort)item.Effect); // effect changed to short
					}
				}
				else
				{
					pak.WriteByte(0x00);
				}

				SendTCP(pak);
			}
		}

		public override void SendLoginGranted(byte color)
		{
			using (GSTCPPacketOut pak = new GSTCPPacketOut(GetPacketCode(eServerPackets.LoginGranted)))
			{
				pak.WritePascalString(m_gameClient.Account.Name);
				pak.WritePascalString(GameServer.Instance.Configuration.ServerNameShort); //server name
				pak.WriteByte(0x0C); //Server ID
				pak.WriteByte(color);
				pak.WriteByte(0x00);
				pak.WriteByte(0x00);
				SendTCP(pak);
			}
		}

		public override void SendNPCCreate(GameNPC npc)
		{
			if (m_gameClient.Player == null || npc.IsVisibleTo(m_gameClient.Player) == false)
				return;

			// Mines are not shown to enemy players.
			if (npc is GameMine)
			{
				if (GameServer.ServerRules.IsAllowedToAttack((npc as GameMine).Owner, m_gameClient.Player, true))
					return;
			}

			if (npc is GameMovingObject)
				SendMovingObjectCreate(npc as GameMovingObject);
			else
			{
				using (GSTCPPacketOut pak = new GSTCPPacketOut(GetPacketCode(eServerPackets.NPCCreate)))
				{
					short speed = 0;
					ushort speedZ = 0;

					if (npc.IsMoving && !npc.IsAtDestination)
					{
						speed = npc.CurrentSpeed;
						speedZ = (ushort) npc.movementComponent.Velocity.Z;
					}

					pak.WriteShort((ushort) npc.ObjectID);
					pak.WriteShort((ushort) speed);
					pak.WriteShort(npc.Heading);
					pak.WriteShort((ushort) npc.Z);
					pak.WriteInt((uint) npc.X);
					pak.WriteInt((uint) npc.Y);
					pak.WriteShort(speedZ);
					pak.WriteShort(npc.Model);
					pak.WriteByte(npc.Size);
					byte level = npc.GetDisplayLevel(m_gameClient.Player);

					if ((npc.Flags & GameNPC.eFlags.STATUE) != 0)
						level |= 0x80;

					pak.WriteByte(level);
					byte flags = (byte) (GameServer.ServerRules.GetLivingRealm(m_gameClient.Player, npc) << 6);

					if ((npc.Flags & GameNPC.eFlags.GHOST) != 0)
						flags |= 0x01;

					if (npc.Inventory != null)
						flags |= 0x02; // If mob has equipment, then only show it after the client gets the 0xBD packet.

					if ((npc.Flags & GameNPC.eFlags.PEACE) != 0)
						flags |= 0x10;

					if ((npc.Flags & GameNPC.eFlags.FLYING) != 0)
						flags |= 0x20;

					if ((npc.Flags & GameNPC.eFlags.TORCH) != 0)
						flags |= 0x04;

					pak.WriteByte(flags);
					pak.WriteByte(0x20); //TODO this is the default maxstick distance

					string add = string.Empty;
					byte flags2 = 0x00;

					if (npc.Brain is IControlledBrain)
						flags2 |= 0x80;

					if ((npc.Flags & GameNPC.eFlags.CANTTARGET) != 0)
					{
						if (m_gameClient.Account.PrivLevel > 1)
							add += "-DOR";
						else
							flags2 |= 0x01;
					}

					if ((npc.Flags & GameNPC.eFlags.DONTSHOWNAME) != 0)
					{
						if (m_gameClient.Account.PrivLevel > 1)
							add += "-NON";
						else
							flags2 |= 0x02;
					}

					if (npc.IsStealthed)
						flags2 |= 0x04;

					eQuestIndicator questIndicator = npc.GetQuestIndicator(m_gameClient.Player);

					if (questIndicator == eQuestIndicator.Available)
						flags2 |= 0x08;

					if (questIndicator == eQuestIndicator.Finish)
						flags2 |= 0x10;

					//flags2 |= 0x20;//hex 32 - water mob?
					//flags2 |= 0x40;//hex 64 - unknown
					//flags2 |= 0x80;//hex 128 - has owner

					pak.WriteByte(flags2);

					byte flags3 = 0x00;

					if (questIndicator == eQuestIndicator.Lesson)
						flags3 |= 0x01;

					if (questIndicator == eQuestIndicator.Lore)
						flags3 |= 0x02;

					if (questIndicator == eQuestIndicator.Pending)
						flags3 |= 0x20;

					pak.WriteByte(flags3); // new in 1.71 (region instance ID from StoC_0x20) OR flags 3?
					pak.WriteShort(0x00); // new in 1.71 unknown

					string name = npc.Name;
					string guildName = npc.GuildName;

					if (LanguageMgr.GetTranslation(m_gameClient, npc) is DbLanguageGameNpc translation)
					{
						if (!string.IsNullOrEmpty(translation.Name))
							name = translation.Name;

						if (!string.IsNullOrEmpty(translation.GuildName))
							guildName = translation.GuildName;
					}

					if (name.Length + add.Length + 2 > 47) // Clients crash with too long names.
						name = name[..(47 - add.Length - 2)];

					if (add.Length > 0)
						name = string.Format("[{0}]{1}", name, add);

					pak.WritePascalString(name);

					if (guildName.Length > 47)
						pak.WritePascalString(guildName.Substring(0, 47));
					else
						pak.WritePascalString(guildName);

					pak.WriteByte(0x00);
					SendTCP(pak);
				}
			}

			// If the client needs us to send a create packet for its steed, it means we updated it in 'PlayerPositionUpdateHandler' but they fell off.
			// It can happen because of lag or if a NPC fails to broadcast its position.
			if (npc == m_gameClient.Player.Steed)
			{
				GamePlayer player = m_gameClient.Player;
				player.Out.SendRiding(player, npc, false);
			}

			// Hack to make NPCs untargetable with TAB on a PvP server. There might be a better way to do it.
			// Relies on 'SendObjectGuildID' not to be called after this.
			if (GameServer.Instance.Configuration.ServerType == EGameServerType.GST_PvP)
			{
				if (npc.Brain is IControlledBrain npcBrain)
					SendPetFakeFriendlyGuildID(npc, npcBrain);
				else
					SendNpcFakeFriendlyGuildID(npc);
			}

			void SendPetFakeFriendlyGuildID(GameNPC pet, IControlledBrain petBrain)
			{
				GamePlayer playerOwner = petBrain.GetPlayerOwner();
				GamePlayer player = m_gameClient.Player;
				Guild playerGuild = player.Guild;

				// Leave if the player we send this packet to isn't the pet's owner and isn't in the same guild or group.
				if (playerOwner != player)
				{
					Guild playerOwnerGuild = playerOwner.Guild;

					if (playerOwnerGuild == null || playerGuild == null || playerOwnerGuild != playerGuild)
					{
						Group playerOwnerGroup = playerOwner.Group;

						if (playerOwnerGroup == null || !playerOwnerGroup.GetMembersInTheGroup().Contains(player))
							return;
					}
				}

				// Make the client believe the pet is in the same guild as them.
				// Use a dummy guild for guildless players.
				SendObjectGuildID(pet, playerGuild ?? Guild.DummyGuild);
				SendObjectGuildID(player, playerGuild ?? Guild.DummyGuild);
			}

			void SendNpcFakeFriendlyGuildID(GameNPC npc)
			{
				if (npc.Flags.HasFlag(GameNPC.eFlags.PEACE) || npc.Realm != eRealm.None)
				{
					GamePlayer player = m_gameClient.Player;
					Guild playerGuild = player.Guild;

					// Make the client believe the NPC is in the same guild as them.
					// Use a dummy guild for guildless players.
					SendObjectGuildID(npc, playerGuild ?? Guild.DummyGuild);
					SendObjectGuildID(player, playerGuild ?? Guild.DummyGuild);
				}
			}
		}

		public override void SendPlayerCreate(GamePlayer playerToCreate)
		{
			if (playerToCreate == null)
			{
				if (log.IsErrorEnabled)
					log.Error("SendPlayerCreate: playerToCreate == null");
				return;
			}

			if (m_gameClient.Player == null)
			{
				if (log.IsErrorEnabled)
					log.Error("SendPlayerCreate: m_gameClient.Player == null");
				return;
			}

			if (playerToCreate.CurrentRegion == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("SendPlayerCreate: playerToCreate.CurrentRegion == null");
				return;
			}

			if (playerToCreate.CurrentZone == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("SendPlayerCreate: playerToCreate.CurrentZone == null");
				return;
			}

			if (!playerToCreate.IsVisibleTo(m_gameClient.Player))
				return;

			// Players sometimes see other players standing up on their mount (includes boats and siege engines).
			// It seems to happen when a player is created, then updated (the rider disappears) before their mount is created (the rider reappears, standing up).
			// Preventing the player update if the mount hasn't been created yet is very tricky since that information isn't known.
			// Instead, we can force the mount to be created before its rider.
			// However, it means it's possible for the steed to be created twice in quick succession (probably fine).
			if (playerToCreate.Steed != null)
				m_gameClient.Out.SendNPCCreate(playerToCreate.Steed);

			using (GSTCPPacketOut pak = new GSTCPPacketOut(GetPacketCode(eServerPackets.PlayerCreate172)))
			{
				pak.WriteFloatLowEndian(playerToCreate.X);
				pak.WriteFloatLowEndian(playerToCreate.Y);
				pak.WriteFloatLowEndian(playerToCreate.Z);
				pak.WriteShort((ushort)playerToCreate.Client.SessionID);
				pak.WriteShort((ushort)playerToCreate.ObjectID);
				pak.WriteShort(playerToCreate.Heading);
				pak.WriteShort(playerToCreate.Model);
				pak.WriteByte(playerToCreate.GetDisplayLevel(m_gameClient.Player));

				int flags = (GameServer.ServerRules.GetLivingRealm(m_gameClient.Player, playerToCreate) & 0x03) << 2;
				if (playerToCreate.IsAlive == false) flags |= 0x01;
				if (playerToCreate.IsUnderwater) flags |= 0x02; //swimming
				if (playerToCreate.IsStealthed) flags |= 0x10;
				if (playerToCreate.IsWireframe) flags |= 0x20;
				if (playerToCreate.CharacterClass.ID == (int)eCharacterClass.Vampiir) flags |= 0x40; //Vamp fly
				pak.WriteByte((byte)flags);

				pak.WriteByte(playerToCreate.GetFaceAttribute(eCharFacePart.EyeSize)); //1-4 = Eye Size / 5-8 = Nose Size
				pak.WriteByte(playerToCreate.GetFaceAttribute(eCharFacePart.LipSize)); //1-4 = Ear size / 5-8 = Kin size
				pak.WriteByte(playerToCreate.GetFaceAttribute(eCharFacePart.MoodType)); //1-4 = Ear size / 5-8 = Kin size
				pak.WriteByte(playerToCreate.GetFaceAttribute(eCharFacePart.EyeColor)); //1-4 = Skin Color / 5-8 = Eye Color
				pak.WriteByte(playerToCreate.GetFaceAttribute(eCharFacePart.HairColor)); //Hair: 1-4 = Color / 5-8 = unknown
				pak.WriteByte(playerToCreate.GetFaceAttribute(eCharFacePart.FaceType)); //1-4 = Unknown / 5-8 = Face type
				pak.WriteByte(playerToCreate.GetFaceAttribute(eCharFacePart.HairStyle)); //1-4 = Unknown / 5-8 = Hair Style

				pak.WriteByte(0x00); // new in 1.74
				pak.WriteByte(0x00); //unknown
				pak.WriteByte(0x00); //unknown
				pak.WritePascalString(GameServer.ServerRules.GetPlayerName(m_gameClient.Player, playerToCreate));
				pak.WritePascalString(GameServer.ServerRules.GetPlayerGuildName(m_gameClient.Player, playerToCreate));
				pak.WritePascalString(GameServer.ServerRules.GetPlayerLastName(m_gameClient.Player, playerToCreate));
				//RR 12 / 13
				pak.WritePascalString(GameServer.ServerRules.GetPlayerPrefixName(m_gameClient.Player, playerToCreate));
				pak.WritePascalString(GameServer.ServerRules.GetPlayerTitle(m_gameClient.Player, playerToCreate)); // new in 1.74, NewTitle
				if (playerToCreate.IsOnHorse)
				{
					pak.WriteByte(playerToCreate.ActiveHorse.ID);
					if (playerToCreate.ActiveHorse.BardingColor == 0 && playerToCreate.ActiveHorse.Barding != 0 && playerToCreate.Guild != null)
					{
						int newGuildBitMask = (playerToCreate.Guild.Emblem & 0x010000) >> 9;
						pak.WriteByte((byte)(playerToCreate.ActiveHorse.Barding | newGuildBitMask));
						pak.WriteShortLowEndian((ushort)playerToCreate.Guild.Emblem);
					}
					else
					{
						pak.WriteByte(playerToCreate.ActiveHorse.Barding);
						pak.WriteShort(playerToCreate.ActiveHorse.BardingColor);
					}
					pak.WriteByte(playerToCreate.ActiveHorse.Saddle);
					pak.WriteByte(playerToCreate.ActiveHorse.SaddleColor);
				}
				else
				{
					pak.WriteByte(0); // trailing zero
				}

				SendTCP(pak);
			}

			SendObjectGuildID(playerToCreate, playerToCreate.Guild); //used for nearest friendly/enemy object buttons and name colors on PvP server

			if (playerToCreate.GuildBanner != null)
				SendRvRGuildBanner(playerToCreate, true);
		}

		public override void SendPlayerPositionAndObjectID()
		{
			if (m_gameClient.Player == null) return;

			using (GSTCPPacketOut pak = new GSTCPPacketOut(GetPacketCode(eServerPackets.PositionAndObjectID)))
			{
				if (m_gameClient.Player.X <= 0)
				{
					int x = 0;
				}
				pak.WriteFloatLowEndian(m_gameClient.Player.X);
				pak.WriteFloatLowEndian(m_gameClient.Player.Y);
				pak.WriteFloatLowEndian(m_gameClient.Player.Z);
				pak.WriteShort((ushort)m_gameClient.Player.ObjectID); //This is the player's objectid not Sessionid!!!
				pak.WriteShort(m_gameClient.Player.Heading);

				int flags = 0;
				Zone zone = m_gameClient.Player.CurrentZone;
				if (zone == null) return;

				if (m_gameClient.Player.CurrentZone.IsDivingEnabled)
					flags = 0x80 | (m_gameClient.Player.IsUnderwater ? 0x01 : 0x00);

				if (zone.IsDungeon)
				{
					pak.WriteShort((ushort)(zone.XOffset / 0x2000));
					pak.WriteShort((ushort)(zone.YOffset / 0x2000));
				}
				else
				{
					pak.WriteShort(0);
					pak.WriteShort(0);
				}
				//Dinberg - Changing to allow instances...
				pak.WriteShort(m_gameClient.Player.CurrentRegion.Skin);
				pak.WriteByte((byte)(flags));
				if (m_gameClient.Player.CurrentRegion.IsHousing)
				{
					pak.WritePascalString(GameServer.Instance.Configuration.ServerName); //server name
				}
				else pak.WriteByte(0);
				pak.WriteByte(0); // rest is unknown for now
				pak.WriteByte(0); // flag?
				pak.WriteByte(0); // flag? these seemingly randomly have a value, most common is last 2 bytes are 34 08
				pak.WriteByte(0); // flag?
				SendTCP(pak);
			}
		}

		protected override void SendQuestPacket(AbstractQuest quest, byte index)
		{
			if (quest == null)
			{
				using (GSTCPPacketOut pak = new GSTCPPacketOut(GetPacketCode(eServerPackets.QuestEntry)))
				{
					pak.WriteByte(index);
					pak.WriteByte(0);
					pak.WriteByte(0);
					pak.WriteByte(0);
					pak.WriteByte(0);
					pak.WriteByte(0);
					SendTCP(pak);
					return;
				}
			}
			else if (quest is RewardQuest)
			{
				RewardQuest rewardQuest = quest as RewardQuest;
				using (GSTCPPacketOut pak = new GSTCPPacketOut(GetPacketCode(eServerPackets.QuestEntry)))
				{
					pak.WriteByte((byte)index);
					pak.WriteByte((byte)rewardQuest.Name.Length);
					pak.WriteShort(0x00); // unknown
					pak.WriteByte((byte)rewardQuest.Goals.Count);
					pak.WriteByte((byte)rewardQuest.Level);
					pak.WriteStringBytes(rewardQuest.Name);
					pak.WritePascalString(rewardQuest.Description);
					int goalindex = 0;
					foreach (RewardQuest.QuestGoal goal in rewardQuest.Goals)
					{
						goalindex++;
						String goalDesc = String.Format("{0}\r", goal.Description);
						pak.WriteShortLowEndian((ushort)goalDesc.Length);
						pak.WriteStringBytes(goalDesc);
						pak.WriteShortLowEndian((ushort)goal.ZoneID2);
						pak.WriteShortLowEndian((ushort)goal.XOffset2);
						pak.WriteShortLowEndian((ushort)goal.YOffset2);
						pak.WriteShortLowEndian(0x00);  // unknown
						pak.WriteShortLowEndian((ushort)goal.Type);
						pak.WriteShortLowEndian(0x00);  // unknown
						pak.WriteShortLowEndian((ushort)goal.ZoneID1);
						pak.WriteShortLowEndian((ushort)goal.XOffset1);
						pak.WriteShortLowEndian((ushort)goal.YOffset1);
						pak.WriteByte((byte)((goal.IsAchieved) ? 0x01 : 0x00));
						if (goal.QuestItem == null)
						{
							pak.WriteByte(0x00);
						}
						else
						{
							pak.WriteByte((byte)goalindex);
							WriteTemplateData(pak, goal.QuestItem, 1);
						}
					}
					SendTCP(pak);
					return;
				}
			}
			else
			{
				using (GSTCPPacketOut pak = new GSTCPPacketOut(GetPacketCode(eServerPackets.QuestEntry)))
				{
					pak.WriteByte((byte)index);

					string name = string.Format("{0} (Level {1})", quest.Name, quest.Level);
					string desc = string.Format("[Step #{0}]: {1}", quest.Step, quest.Description);
					if (name.Length > byte.MaxValue)
					{
						if (log.IsWarnEnabled)
						{
							log.Warn(quest.GetType().ToString() + ": name is too long for 1.68+ clients (" + name.Length + ") '" + name + "'");
						}
						name = name.Substring(0, byte.MaxValue);
					}
					if (desc.Length > byte.MaxValue)
					{
						if (log.IsWarnEnabled)
						{
							log.Warn(quest.GetType().ToString() + ": description is too long for 1.68+ clients (" + desc.Length + ") '" + desc + "'");
						}
						desc = desc.Substring(0, byte.MaxValue);
					}
					pak.WriteByte((byte)name.Length);
					pak.WriteShortLowEndian((ushort)desc.Length);
					pak.WriteByte(0); // Quest Zone ID ?
					pak.WriteByte(0);
					pak.WriteStringBytes(name); //Write Quest Name without trailing 0
					pak.WriteStringBytes(desc); //Write Quest Description without trailing 0

					SendTCP(pak);
				}
			}
		}

		public override void SendRegions(ushort regionId)
		{
			if (!m_gameClient.Socket.Connected)
				return;
			Region region = WorldMgr.GetRegion(regionId);
			if (region == null)
				return;
			using (GSTCPPacketOut pak = new GSTCPPacketOut(GetPacketCode(eServerPackets.ClientRegion)))
			{
				//				pak.WriteByte((byte)((region.Expansion + 1) << 4)); // Must be expansion
				pak.WriteByte(0); // but this packet sended when client in old region. but this field must show expanstion for jump destanation region
								  //Dinberg - trying to get instances to work.
				pak.WriteByte((byte)region.Skin); // This was pak.WriteByte((byte)region.ID);
				pak.Fill(0, 20);
				pak.FillString(region.ServerPort.ToString(), 5);
				pak.FillString(region.ServerPort.ToString(), 5);
				string ip = region.ServerIP;
				if (ip == "any" || ip == "0.0.0.0" || ip == "127.0.0.1" || ip.StartsWith("10.") || ip.StartsWith("192.168."))
					ip = ((IPEndPoint)m_gameClient.Socket.LocalEndPoint).Address.ToString();
				pak.FillString(ip, 20);
				SendTCP(pak);
			}
		}

		public override void SendSiegeWeaponAnimation(GameSiegeWeapon siegeWeapon)
		{
			if (siegeWeapon == null)
				return;
			byte[] siegeID = new byte[siegeWeapon.ObjectID]; // test
			using (var pak = new GSTCPPacketOut(GetPacketCode(eServerPackets.SiegeWeaponAnimation)))
			{
				pak.WriteInt((uint)siegeWeapon.ObjectID);
				pak.WriteInt(
					(uint)
					(siegeWeapon.TargetObject == null
					 ? (siegeWeapon.GroundTarget == null ? 0 : siegeWeapon.GroundTarget.X)
					 : siegeWeapon.TargetObject.X));
				pak.WriteInt(
					(uint)
					(siegeWeapon.TargetObject == null
					 ? (siegeWeapon.GroundTarget == null ? 0 : siegeWeapon.GroundTarget.Y)
					 : siegeWeapon.TargetObject.Y));
				pak.WriteInt(
					(uint)
					(siegeWeapon.TargetObject == null
					 ? (siegeWeapon.GroundTarget == null ? 0 : siegeWeapon.GroundTarget.Z)
					 : siegeWeapon.TargetObject.Z));
				pak.WriteInt((uint)(siegeWeapon.TargetObject == null ? 0 : siegeWeapon.TargetObject.ObjectID));
				pak.WriteShort(siegeWeapon.Effect);
				pak.WriteShort((ushort)(siegeWeapon.SiegeWeaponTimer.TimeUntilElapsed));
				pak.WriteByte((byte)siegeWeapon.SiegeWeaponTimer.CurrentAction);
				switch ((byte)siegeWeapon.SiegeWeaponTimer.CurrentAction)
				{
					case 0x01: //aiming
						{
							pak.WriteByte((byte)(siegeWeapon.TargetObject == null ? 0 : siegeWeapon.TargetObject.HealthPercent)); //Send target health percent
							//pak.WriteByte(siegeID[1]); // lowest value of siegeweapon.ObjectID
							pak.WriteShort((ushort)(siegeWeapon.TargetObject == null ? 0x0000 : siegeWeapon.TargetObject.ObjectID));
							break;
						}
					case 0x02: //arming
						{
							pak.WriteByte((byte)(siegeWeapon.TargetObject == null ? 0 : siegeWeapon.TargetObject.HealthPercent)); //Send target health percent
							pak.WriteShort(0x0000); //Aiming target ID is null when arming
							// pak.WriteByte(0x5F);
							// pak.WriteShort(0xD000);
							break;
						}
					case 0x03: // loading
						{
							pak.Fill(0, 3);
							break;
						}
				}
				//pak.WriteShort(0x5FD0);
				//pak.WriteByte(0x00);
				SendTCP(pak);
			}
		}

		public override void SendSiegeWeaponFireAnimation(GameSiegeWeapon siegeWeapon, int timer)
		{
			if (siegeWeapon == null)
				return;
			using (var pak = new GSTCPPacketOut(GetPacketCode(eServerPackets.SiegeWeaponAnimation)))
			{
				pak.WriteInt((uint)siegeWeapon.ObjectID);
				pak.WriteInt((uint)(siegeWeapon.TargetObject == null ? siegeWeapon.GroundTarget.X : siegeWeapon.TargetObject.X));
				pak.WriteInt((uint)(siegeWeapon.TargetObject == null ? siegeWeapon.GroundTarget.Y : siegeWeapon.TargetObject.Y));
				pak.WriteInt((uint)(siegeWeapon.TargetObject == null ? siegeWeapon.GroundTarget.Z + 50 : siegeWeapon.TargetObject.Z + 50));
				pak.WriteInt((uint)(siegeWeapon.TargetObject == null ? 0 : siegeWeapon.TargetObject.ObjectID));
				pak.WriteShort(siegeWeapon.Effect);
				pak.WriteShort((ushort)(timer));
				pak.WriteByte((byte)SiegeTimer.eAction.Fire);
				pak.WriteByte((byte)(siegeWeapon.TargetObject == null ? 0 : siegeWeapon.TargetObject.HealthPercent)); //Send target health percent
				pak.WriteShort(0x0000); //Aiming target ID is null on firing
				// pak.WriteShort(0xE134); // default ammo type, the only type currently supported on DOL
				// pak.WriteByte(0x08); // always this flag when firing
				SendTCP(pak);
			}
		}

		public override void SendSiegeWeaponInterface(GameSiegeWeapon siegeWeapon, int time)
		{
			using (GSTCPPacketOut pak = new GSTCPPacketOut(GetPacketCode(eServerPackets.SiegeWeaponInterface)))
			{
				ushort flag = (ushort)((siegeWeapon.EnableToMove ? 1 : 0) | siegeWeapon.AmmoType << 8);
				pak.WriteShort(flag); //byte Ammo,  byte SiegeMoving(1/0)
				pak.WriteByte(0);
				pak.WriteByte(0); // Close interface(1/0)
				pak.WriteByte((byte)(time));//time x 100 eg 50 = 5000ms
				pak.WriteByte((byte)siegeWeapon.Ammo.Count); // external ammo count
				pak.WriteByte((byte)siegeWeapon.SiegeWeaponTimer.CurrentAction);
				pak.WriteByte((byte)siegeWeapon.AmmoSlot);
				pak.WriteShort(siegeWeapon.Effect);
				pak.WriteShort(0); // SiegeHelperTimer ?
				pak.WriteShort(0); // SiegeTimer ?
				pak.WriteShort((ushort)siegeWeapon.ObjectID);

				string name = siegeWeapon.Name;

				LanguageDataObject translation = LanguageMgr.GetTranslation(m_gameClient, siegeWeapon);
				if (translation != null)
				{
					if (!string.IsNullOrEmpty(((DbLanguageGameNpc)translation).Name))
						name = ((DbLanguageGameNpc)translation).Name;
				}

				//pak.WritePascalString(name + " (" + siegeWeapon.CurrentState.ToString() + ")");
				foreach (DbInventoryItem item in siegeWeapon.Ammo)
				{
					if (item == null)
					{
						pak.Fill(0x00, 24);
						continue;
					}
					pak.WriteByte((byte)siegeWeapon.Ammo.IndexOf(item));
					pak.WriteShort(0); // unique objectID , can probably be 0
					pak.WriteByte((byte)item.Level);
					pak.WriteByte(0); // value1
					pak.WriteByte(0); //value2
					pak.WriteByte(0); // unknown
					pak.WriteByte((byte)item.Object_Type);
					pak.WriteByte(1); // unknown
					pak.WriteByte(0);//
					pak.WriteByte((byte)item.Count);
					//pak.WriteByte((byte)(item.Hand * 64));
					//pak.WriteByte((byte)((item.Type_Damage * 64) + item.Object_Type));
					//pak.WriteShort((ushort)item.Weight);
					pak.WriteByte(item.ConditionPercent); // % of con
					pak.WriteByte(item.DurabilityPercent); // % of dur
					pak.WriteByte((byte)item.Quality); // % of qua
					pak.WriteByte((byte)item.Bonus); // % bonus
					pak.WriteByte((byte)item.BonusLevel); // guessing
					pak.WriteShort((ushort)item.Model);
					pak.WriteByte((byte)item.Extension);
					pak.WriteShort(0); // unknown
					pak.WriteByte(4); // unknown flags?
					pak.WriteShort(0); // unknown
					if (item.Count > 1)
						pak.WritePascalString(item.Count + " " + item.Name);
					else
						pak.WritePascalString(item.Name);
				}
				pak.WritePascalString(name + " (" + siegeWeapon.CurrentState.ToString() + ")");
				SendTCP(pak);
			}
		}

		public override void SendVersionAndCryptKey()
		{
			//Construct the new packet
			using (var pak = new GSTCPPacketOut(GetPacketCode(eServerPackets.CryptKey)))
			{
				pak.WriteByte((byte)m_gameClient.ClientType);

				//Disable encryption (1110+ always encrypt)
				pak.WriteByte(0x00);

				// Reply with current version
				pak.WriteString((((int)m_gameClient.Version) / 1000) + "." + (((int)m_gameClient.Version) - 1000), 5);

				// revision, last seen (c) 0x63
				pak.WriteByte((byte)m_gameClient.MinorRev[0]);

				// Build number
				pak.WriteByte(m_gameClient.MajorBuild); // last seen : 0x44 0x05
				pak.WriteByte(m_gameClient.MinorBuild);
				SendTCP(pak);
				m_gameClient.PacketProcessor.SendPendingPackets();
			}
		}

		protected virtual void WriteGroupMemberUpdate(GSTCPPacketOut pak, bool updateIcons, bool updateMap, GameLiving living)
		{
			pak.WriteByte((byte)(living.GroupIndex + 1)); // From 1 to 8
			if (living.CurrentRegion != m_gameClient.Player.CurrentRegion)
			{
				pak.WriteByte(0x00); // health
				pak.WriteByte(0x00); // mana
				pak.WriteByte(0x00); // endu
				pak.WriteByte(0x20); // player state (0x20 = another region)
				if (updateIcons)
				{
					pak.WriteByte((byte)(0x80 | living.GroupIndex));
					pak.WriteByte(0);
				}
				return;
			}
			var player = living as GamePlayer;

			pak.WriteByte(player?.CharacterClass?.HealthPercentGroupWindow ?? living.HealthPercent);
			pak.WriteByte(living.ManaPercent);
			pak.WriteByte(living.EndurancePercent); // new in 1.69

			byte playerStatus = 0;
			if (!living.IsAlive)
				playerStatus |= 0x01;
			if (living.IsMezzed)
				playerStatus |= 0x02;
			if (living.IsDiseased)
				playerStatus |= 0x04;
			if (living.IsPoisoned)
				playerStatus |= 0x08;
			if (player?.Client.ClientState == GameClient.eClientState.Linkdead)
				playerStatus |= 0x10;
			if (living.DebuffCategory[(int)eProperty.SpellRange] != 0 || living.DebuffCategory[(int)eProperty.ArcheryRange] != 0)
				playerStatus |= 0x40;
			pak.WriteByte(playerStatus);
			// 0x00 = Normal , 0x01 = Dead , 0x02 = Mezzed , 0x04 = Diseased ,
			// 0x08 = Poisoned , 0x10 = Link Dead , 0x20 = In Another Region, 0x40 - NS

			if (updateIcons)
			{
				pak.WriteByte((byte)(0x80 | living.GroupIndex));

				lock (living.effectListComponent.EffectsLock)
				{
					byte i = 0;
					var effects = living.effectListComponent.GetAllEffects();
					if (living is GamePlayer necro && (eCharacterClass) necro.CharacterClass.ID is eCharacterClass.Necromancer && necro.HasShadeModel)
						effects.AddRange(necro.ControlledBrain.Body.effectListComponent.GetAllEffects().Where(e => e.TriggersImmunity));
					foreach (var effect in effects)
					{
						if (effect is ECSGameEffect && !effect.IsDisabled)
							i++;
					}
					pak.WriteByte(i);
					foreach (var effect in effects)
					{
						if (effect is ECSGameEffect && !effect.IsDisabled)
						{
							pak.WriteByte(0);
							pak.WriteShort(effect.Icon);
						}
					}
				}
			}
			if (updateMap)
				WriteGroupMemberMapUpdate(pak, living);
		}

		protected override void WriteGroupMemberMapUpdate(GSTCPPacketOut pak, GameLiving living)
		{
			if (living.CurrentSpeed != 0)
			{
				Zone zone = living.CurrentZone;
				if (zone == null)
					return;
				pak.WriteByte((byte)(0x40 | living.GroupIndex));
				//Dinberg - ZoneSkinID for group members aswell.
				pak.WriteShort(zone.ZoneSkinID);
				pak.WriteShort((ushort)(living.X - zone.XOffset));
				pak.WriteShort((ushort)(living.Y - zone.YOffset));
			}
		}

		protected override void WriteItemData(GSTCPPacketOut pak, DbInventoryItem item)
		{
			if (item == null)
			{
				pak.Fill(0x00, 24); // +1 item.Effect changed to short
				return;
			}
			pak.WriteShort((ushort)0); // item uniqueID
			pak.WriteByte((byte)item.Level);

			int value1; // some object types use this field to display count
			int value2; // some object types use this field to display count
			switch (item.Object_Type)
			{
				case (int)eObjectType.GenericItem:
					value1 = item.Count & 0xFF;
					value2 = (item.Count >> 8) & 0xFF;
					break;
				case (int)eObjectType.Arrow:
				case (int)eObjectType.Bolt:
				case (int)eObjectType.Poison:
					value1 = item.Count;
					value2 = item.SPD_ABS;
					break;
				case (int)eObjectType.Thrown:
					value1 = item.DPS_AF;
					value2 = item.Count;
					break;
				case (int)eObjectType.Instrument:
					value1 = (item.DPS_AF == 2 ? 0 : item.DPS_AF);
					value2 = 0;
					break; // unused
				case (int)eObjectType.Shield:
					value1 = item.Type_Damage;
					value2 = item.DPS_AF;
					break;
				case (int)eObjectType.AlchemyTincture:
				case (int)eObjectType.SpellcraftGem:
					value1 = 0;
					value2 = 0;
					/*
					must contain the quality of gem for spell craft and think same for tincture
					*/
					break;
				case (int)eObjectType.HouseWallObject:
				case (int)eObjectType.HouseFloorObject:
				case (int)eObjectType.GardenObject:
					value1 = 0;
					value2 = item.SPD_ABS;
					/*
					Value2 byte sets the width, only lower 4 bits 'seem' to be used (so 1-15 only)

					The byte used for "Hand" (IE: Mini-delve showing a weapon as Left-Hand
					usabe/TwoHanded), the lower 4 bits store the height (1-15 only)
					*/
					break;

				default:
					value1 = item.DPS_AF;
					value2 = item.SPD_ABS;
					break;
			}
			pak.WriteByte((byte)value1);
			pak.WriteByte((byte)value2);

			if (item.Object_Type == (int)eObjectType.GardenObject)
				pak.WriteByte((byte)(item.DPS_AF));
			else
				pak.WriteByte((byte)(item.Hand << 6));

			pak.WriteByte((byte)((item.Type_Damage > 3 ? 0 : item.Type_Damage << 6) | item.Object_Type));
			pak.WriteByte(0x00); //unk 1.112
			pak.WriteShort((ushort)item.Weight);
			pak.WriteByte(item.ConditionPercent); // % of con
			pak.WriteByte(item.DurabilityPercent); // % of dur
			pak.WriteByte((byte)item.Quality); // % of qua
			pak.WriteByte((byte)item.Bonus); // % bonus
			pak.WriteByte((byte)item.BonusLevel); // 1.109
			pak.WriteShort((ushort)item.Model);
			pak.WriteByte((byte)item.Extension);
			int flag = 0;
			int emblem = item.Emblem;
			int color = item.Color;
			if (emblem != 0)
			{
				pak.WriteShort((ushort)emblem);
				flag |= (emblem & 0x010000) >> 16; // = 1 for newGuildEmblem
			}
			else
			{
				pak.WriteShort((ushort)color);
			}
			//flag |= 0x01; // newGuildEmblem
			flag |= 0x02; // enable salvage button
			AbstractCraftingSkill skill = CraftingMgr.getSkillbyEnum(m_gameClient.Player.CraftingPrimarySkill);
			if (skill != null && skill is AdvancedCraftingSkill/* && ((AdvancedCraftingSkill)skill).IsAllowedToCombine(_gameClient.Player, item)*/)
				flag |= 0x04; // enable craft button
			ushort icon1 = 0;
			ushort icon2 = 0;
			string spell_name1 = string.Empty;
			string spell_name2 = string.Empty;
			if (item.Object_Type != (int)eObjectType.AlchemyTincture)
			{
				if (item.SpellID > 0/* && item.Charges > 0*/)
				{
					SpellLine chargeEffectsLine = SkillBase.GetSpellLine(GlobalSpellsLines.Item_Effects);
					if (chargeEffectsLine != null)
					{
						List<Spell> spells = SkillBase.GetSpellList(chargeEffectsLine.KeyName);
						foreach (Spell spl in spells)
						{
							if (spl.ID == item.SpellID)
							{
								flag |= 0x08;
								icon1 = spl.Icon;
								spell_name1 = spl.Name; // or best spl.Name ?
								break;
							}
						}
					}
				}
				if (item.SpellID1 > 0/* && item.Charges > 0*/)
				{
					SpellLine chargeEffectsLine = SkillBase.GetSpellLine(GlobalSpellsLines.Item_Effects);
					if (chargeEffectsLine != null)
					{
						List<Spell> spells = SkillBase.GetSpellList(chargeEffectsLine.KeyName);
						foreach (Spell spl in spells)
						{
							if (spl.ID == item.SpellID1)
							{
								flag |= 0x10;
								icon2 = spl.Icon;
								spell_name2 = spl.Name; // or best spl.Name ?
								break;
							}
						}
					}
				}
			}
			pak.WriteByte((byte)flag);
			if ((flag & 0x08) == 0x08)
			{
				pak.WriteShort((ushort)icon1);
				pak.WritePascalString(spell_name1);
			}
			if ((flag & 0x10) == 0x10)
			{
				pak.WriteShort((ushort)icon2);
				pak.WritePascalString(spell_name2);
			}
			pak.WriteShort((ushort)item.Effect); // item effect changed to short
			string name = item.Name;
			if (item.Count > 1)
				name = item.Count + " " + name;
			if (item.SellPrice > 0)
			{
				if (ServerProperties.Properties.CONSIGNMENT_USE_BP)
					name += "[" + item.SellPrice.ToString() + " BP]";
				else
					name += "[" + Money.GetString(item.SellPrice) + "]";
			}
			if (name == null) name = string.Empty;
			if (name.Length > 55)
				name = name.Substring(0, 55);
			pak.WritePascalString(name);
		}

		protected override void WriteTemplateData(GSTCPPacketOut pak, DbItemTemplate template, int count)
		{
			if (template == null)
			{
				pak.Fill(0x00, 24); // 1.109 +1 byte
				return;
			}
			pak.WriteShort(0); // objectID
			pak.WriteByte((byte)template.Level);

			int value1;
			int value2;

			switch (template.Object_Type)
			{
				case (int)eObjectType.Arrow:
				case (int)eObjectType.Bolt:
				case (int)eObjectType.Poison:
				case (int)eObjectType.GenericItem:
					value1 = count; // Count
					value2 = template.SPD_ABS;
					break;
				case (int)eObjectType.Thrown:
					value1 = template.DPS_AF;
					value2 = count; // Count
					break;
				case (int)eObjectType.Instrument:
					value1 = (template.DPS_AF == 2 ? 0 : template.DPS_AF);
					value2 = 0;
					break;
				case (int)eObjectType.Shield:
					value1 = template.Type_Damage;
					value2 = template.DPS_AF;
					break;
				case (int)eObjectType.AlchemyTincture:
				case (int)eObjectType.SpellcraftGem:
					value1 = 0;
					value2 = 0;
					/*
					must contain the quality of gem for spell craft and think same for tincture
					*/
					break;
				case (int)eObjectType.GardenObject:
					value1 = 0;
					value2 = template.SPD_ABS;
					/*
					Value2 byte sets the width, only lower 4 bits 'seem' to be used (so 1-15 only)

					The byte used for "Hand" (IE: Mini-delve showing a weapon as Left-Hand
					usabe/TwoHanded), the lower 4 bits store the height (1-15 only)
					*/
					break;

				default:
					value1 = template.DPS_AF;
					value2 = template.SPD_ABS;
					break;
			}
			pak.WriteByte((byte)value1);
			pak.WriteByte((byte)value2);

			if (template.Object_Type == (int)eObjectType.GardenObject)
				pak.WriteByte((byte)(template.DPS_AF));
			else
				pak.WriteByte((byte)(template.Hand << 6));
			pak.WriteByte((byte)((template.Type_Damage > 3
				? 0
				: template.Type_Damage << 6) | template.Object_Type));
			pak.Fill(0x00, 1); // 1.109, +1 byte, no clue what this is  - Tolakram
			pak.WriteShort((ushort)template.Weight);
			pak.WriteByte(template.BaseConditionPercent);
			pak.WriteByte(template.BaseDurabilityPercent);
			pak.WriteByte((byte)template.Quality);
			pak.WriteByte((byte)template.Bonus);
			pak.WriteByte((byte)template.BonusLevel); // 1.109
			pak.WriteShort((ushort)template.Model);
			pak.WriteByte((byte)template.Extension);
			if (template.Emblem != 0)
				pak.WriteShort((ushort)template.Emblem);
			else
				pak.WriteShort((ushort)template.Color);
			pak.WriteByte((byte)template.Flags);
			pak.WriteShort((ushort)template.Effect);
			if (count > 1)
				pak.WritePascalString(String.Format("{0} {1}", count, template.Name));
			else
				pak.WritePascalString(template.Name);
		}

		public override void SendGroupMemberUpdate(bool updateIcons, bool updateMap, GameLiving living)
		{
			if (m_gameClient.Player?.Group == null)
				return;

			var group = m_gameClient.Player.Group;
			using (var pak = new GSTCPPacketOut(GetPacketCode(eServerPackets.GroupMemberUpdate)))
			{
				if (living.Group != group)
					return;
				WriteGroupMemberUpdate(pak, updateIcons, updateMap, living);
				pak.WriteByte(0x00);
				SendTCP(pak);
			}
		}

		public override void SendGroupMembersUpdate(bool updateIcons, bool updateMap)
		{
			if (m_gameClient.Player?.Group == null)
				return;

			using (var pak = new GSTCPPacketOut(GetPacketCode(eServerPackets.GroupMemberUpdate)))
			{
				foreach (var living in m_gameClient.Player.Group.GetMembersInTheGroup())
					WriteGroupMemberUpdate(pak, updateIcons, updateMap, living);
				pak.WriteByte(0x00);
				SendTCP(pak);
			}
		}
	}
}
