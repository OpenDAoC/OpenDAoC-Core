using System;
using System.Reflection;
using DOL.AI.Brain;
using DOL.Database;
using DOL.GS.Keeps;
using DOL.GS.Quests;
using DOL.Language;

namespace DOL.GS.PacketHandler
{
	[PacketLib(171, GameClient.eClientVersion.Version171)]
	public class PacketLib171 : PacketLib170
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// Constructs a new PacketLib for Version 1.71 clients
		/// </summary>
		/// <param name="client">the gameclient this lib is associated with</param>
		public PacketLib171(GameClient client)
			: base(client)
		{
		}

		public override void SendPlayerPositionAndObjectID()
		{
			if (m_gameClient.Player == null) return;

			using (GSTCPPacketOut pak = GSTCPPacketOut.GetForTick(p => p.Init(GetPacketCode(eServerPackets.PositionAndObjectID))))
			{
				pak.WriteShort((ushort)m_gameClient.Player.ObjectID); //This is the player's objectid not Sessionid!!!
				pak.WriteShort((ushort)m_gameClient.Player.Z);
				pak.WriteInt((uint)m_gameClient.Player.X);
				pak.WriteInt((uint)m_gameClient.Player.Y);
				pak.WriteShort(m_gameClient.Player.Heading);

				int flags = 0;
				if (m_gameClient.Player.CurrentZone.IsDivingEnabled)
					flags = 0x80 | (m_gameClient.Player.IsUnderwater ? 0x01 : 0x00);
				pak.WriteByte((byte)(flags));

				pak.WriteByte(0x00);	//TODO Unknown
				Zone zone = m_gameClient.Player.CurrentZone;
				if (zone == null) return;
				pak.WriteShort((ushort)(zone.XOffset / 0x2000));
				pak.WriteShort((ushort)(zone.YOffset / 0x2000));
				//Dinberg - Changing to allow instances...
				pak.WriteShort(m_gameClient.Player.CurrentRegion.Skin);
				pak.WriteShort(0x00); //TODO: unknown, new in 1.71
				SendTCP(pak);
			}
		}

		public override void SendObjectCreate(GameObject obj)
		{
			if (obj == null || m_gameClient.Player == null)
				return;

			if (obj.IsVisibleTo(m_gameClient.Player) == false)
				return;

			using (GSTCPPacketOut pak = GSTCPPacketOut.GetForTick(p => p.Init(GetPacketCode(eServerPackets.ObjectCreate))))
			{
				pak.WriteShort((ushort)obj.ObjectID);
				if (obj is GameStaticItem)
					pak.WriteShort((ushort)(obj as GameStaticItem).Emblem);
				else pak.WriteShort(0);
				pak.WriteShort(obj.Heading);
				pak.WriteShort((ushort)obj.Z);
				pak.WriteInt((uint)obj.X);
				pak.WriteInt((uint)obj.Y);
				int flag = ((byte)obj.Realm & 3) << 4;
				ushort model = obj.Model;
				if (obj.IsUnderwater)
				{
					if (obj is GameNPC)
						model |= 0x8000;
					else
						flag |= 0x01; // Underwater
				}
				pak.WriteShort(model);
				if (obj is GameKeepBanner)
					flag |= 0x08;
				if (obj is GameStaticItemTimed && m_gameClient.Player != null && ((GameStaticItemTimed)obj).IsOwner(m_gameClient.Player))
					flag |= 0x04;
				pak.WriteShort((ushort)flag);
				pak.WriteInt(0x0); //TODO: unknown, new in 1.71

				string name = obj.Name;
				LanguageDataObject translation = null;
				if (obj is GameStaticItem)
				{
					translation = LanguageMgr.GetTranslation(m_gameClient, (GameStaticItem)obj);
					if (translation != null)
					{
						if (obj is WorldInventoryItem)
						{
							//if (!string.IsNullOrEmpty(((DBLanguageItem)translation).Name))
							//    name = ((DBLanguageItem)translation).Name;
						}
						else
						{
							if (!string.IsNullOrEmpty(((DbLanguageGameObject)translation).Name))
								name = ((DbLanguageGameObject)translation).Name;
						}
					}
				}
				pak.WritePascalString(name);

				if (obj is GameDoorBase door)
				{
					pak.WriteByte(4);
					pak.WriteInt((uint) door.DoorId);
				}
				else pak.WriteByte(0x00);
				SendTCP(pak);
			}
		}

		public override void SendNPCCreate(GameNPC npc)
		{

			if (m_gameClient.Player == null || npc.IsVisibleTo(m_gameClient.Player) == false)
				return;

			//Added by Suncheck - Mines are not shown to enemy players
			if (npc is GameMine)
			{
				if (GameServer.ServerRules.IsAllowedToAttack((npc as GameMine).Owner, m_gameClient.Player, true))
				{
					return;
				}
			}

			if (npc is GameMovingObject)
			{
				SendMovingObjectCreate(npc as GameMovingObject);
				return;
			}

			using (GSTCPPacketOut pak = GSTCPPacketOut.GetForTick(p => p.Init(GetPacketCode(eServerPackets.NPCCreate))))
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
				if((npc.Flags&GameNPC.eFlags.STATUE)!=0)
				{
					level |= 0x80;
				}
				pak.WriteByte(level);

				byte flags = (byte)(GameServer.ServerRules.GetLivingRealm(m_gameClient.Player, npc) << 6);
				if ((npc.Flags & GameNPC.eFlags.GHOST) != 0) flags |= 0x01;
				if (npc.Inventory != null) flags |= 0x02; //If mob has equipment, then only show it after the client gets the 0xBD packet
				if ((npc.Flags & GameNPC.eFlags.PEACE) != 0) flags |= 0x10;
				if ((npc.Flags & GameNPC.eFlags.FLYING) != 0) flags |= 0x20;
				if((npc.Flags & GameNPC.eFlags.TORCH) != 0) flags |= 0x04;

				pak.WriteByte(flags);
				pak.WriteByte(0x20); //TODO this is the default maxstick distance

				string add = string.Empty;
				byte flags2 = 0x00;
				IControlledBrain brain = npc.Brain as IControlledBrain;
				if (m_gameClient.Version >= GameClient.eClientVersion.Version187)
				{
					if (brain != null)
					{
						flags2 |= 0x80; // have Owner
					}
				}
				if ((npc.Flags & GameNPC.eFlags.CANTTARGET) != 0)
					if (m_gameClient.Account.PrivLevel > 1) add += "-DOR"; // indicates DOR flag for GMs
				else flags2 |= 0x01;
				if ((npc.Flags & GameNPC.eFlags.DONTSHOWNAME) != 0)
					if (m_gameClient.Account.PrivLevel > 1) add += "-NON"; // indicates NON flag for GMs
				else flags2 |= 0x02;

				if (npc.IsStealthed)
					flags2 |= 0x04;

				eQuestIndicator questIndicator = npc.GetQuestIndicator(m_gameClient.Player);

				if (questIndicator == eQuestIndicator.Available)
					flags2 |= 0x08;//hex 8 - quest available
				if (questIndicator == eQuestIndicator.Finish)
					flags2 |= 0x10;//hex 16 - quest finish
				//flags2 |= 0x20;//hex 32 - water mob?
				//flags2 |= 0x40;//hex 64 - unknown
				//flags2 |= 0x80;//hex 128 - has owner


				pak.WriteByte(flags2); // flags 2

				byte flags3 = 0x00;
				if (questIndicator == eQuestIndicator.Lesson)
					flags3 |= 0x01;
				if (questIndicator == eQuestIndicator.Lore)
					flags3 |= 0x02;
				pak.WriteByte(flags3); // new in 1.71 (region instance ID from StoC_0x20) OR flags 3?
				pak.WriteShort(0x00); // new in 1.71 unknown

				string name = npc.Name;
				string guildName = npc.GuildName;

				LanguageDataObject translation = LanguageMgr.GetTranslation(m_gameClient, npc);
				if (translation != null)
				{
					if (!string.IsNullOrEmpty(((DbLanguageGameNpc)translation).Name))
						name = ((DbLanguageGameNpc)translation).Name;

					if (!string.IsNullOrEmpty(((DbLanguageGameNpc)translation).GuildName))
						guildName = ((DbLanguageGameNpc)translation).GuildName;
				}

				if (name.Length + add.Length + 2 > 47) // clients crash with too long names
					name = name.Substring(0, 47 - add.Length - 2);
				if (add.Length > 0)
					name = string.Format("[{0}]{1}", name, add);

				pak.WritePascalString(name);

				if (guildName.Length > 47)
					pak.WritePascalString(guildName.Substring(0, 47));
				else pak.WritePascalString(guildName);

				pak.WriteByte(0x00);
				SendTCP(pak);
			}
		}

		public override void SendFindGroupWindowUpdate(GamePlayer[] list)
		{
			if (m_gameClient.Player == null) return;
			using (GSTCPPacketOut pak = GSTCPPacketOut.GetForTick(p => p.Init(GetPacketCode(eServerPackets.FindGroupUpdate))))
			{
				if (list != null)
				{
					pak.WriteByte((byte)list.Length);
					byte nbleader = 0;
					byte nbsolo = 0x1E;
					foreach (GamePlayer player in list)
					{
						if (player.Group != null)
						{
							pak.WriteByte(nbleader++);
						}
						else
						{
							pak.WriteByte(nbsolo++);
						}
						pak.WriteByte(player.Level);
						pak.WritePascalString(player.Name);
						pak.WriteString(player.CharacterClass.Name, 4);
						//Dinberg:Instances - you know the score by now ;)
						//ZoneSkinID for clientside positioning of objects.
						if (player.CurrentZone != null)
							pak.WriteByte((byte)player.CurrentZone.ZoneSkinID);
						else
							pak.WriteByte(255);
						pak.WriteByte(0); // duration
						pak.WriteByte(0); // objective
						pak.WriteByte(0);
						pak.WriteByte(0);
						pak.WriteByte((byte)(player.Group != null ? 1 : 0));
						pak.WriteByte(0);
					}
				}
				else
				{
					pak.WriteByte(0x00);
				}
				SendTCP(pak);
			}
		}

		protected override void SendQuestPacket(AbstractQuest quest, byte index)
		{
			using (GSTCPPacketOut pak = GSTCPPacketOut.GetForTick(p => p.Init(GetPacketCode(eServerPackets.QuestEntry))))
			{
				pak.WriteByte(index);
				if (quest.Step <= 0)
				{
					pak.WriteByte(0);
					pak.WriteByte(0);
					pak.WriteByte(0);
				}
				else
				{
					string name = quest.Name;
					string desc = quest.Description;
					if (name.Length > byte.MaxValue)
					{
						if (log.IsWarnEnabled) log.Warn(quest.GetType().ToString() + ": name is too long for 1.71 clients (" + name.Length + ") '" + name + "'");
						name = name.Substring(0, byte.MaxValue);
					}
					if (desc.Length > ushort.MaxValue)
					{
						if (log.IsWarnEnabled) log.Warn(quest.GetType().ToString() + ": description is too long for 1.71 clients (" + desc.Length + ") '" + desc + "'");
						desc = desc.Substring(0, ushort.MaxValue);
					}
					if (name.Length + desc.Length > 2048 - 10)
					{
						if (log.IsWarnEnabled) log.Warn(quest.GetType().ToString() + ": name + description length is too long and would have crashed the client.\nName (" + name.Length + "): '" + name + "'\nDesc (" + desc.Length + "): '" + desc + "'");
						name = name.Substring(0, 32);
						desc = desc.Substring(0, 2048 - 10 - name.Length); // all that's left
					}
					pak.WriteByte((byte)name.Length);
					pak.WriteShort((ushort)desc.Length);
					pak.WriteStringBytes(name); //Write Quest Name without trailing 0
					pak.WriteStringBytes(desc); //Write Quest Description without trailing 0
				}
				SendTCP(pak);
			}
		}

		public override void SendLivingDataUpdate(GameLiving living, bool updateStrings)
		{
			using (GSTCPPacketOut pak = GSTCPPacketOut.GetForTick(p => p.Init(GetPacketCode(eServerPackets.ObjectDataUpdate))))
			{
				pak.WriteShort((ushort)living.ObjectID);
				pak.WriteByte(0);
				pak.WriteByte(living.Level);
				GamePlayer player = living as GamePlayer;
				if (player != null)
				{
					pak.WritePascalString(GameServer.ServerRules.GetPlayerGuildName(m_gameClient.Player, player));
					pak.WritePascalString(GameServer.ServerRules.GetPlayerLastName(m_gameClient.Player, player));
				}
				else if (!updateStrings)
				{
					pak.WriteByte(0xFF);
				}
				else
				{
					pak.WritePascalString(living.GuildName);
					pak.WritePascalString(living.Name);
				}

				SendTCP(pak);
			}
		}

		public override void SendPlayerFreeLevelUpdate()
		{
			GamePlayer player = m_gameClient.Player;
			using (GSTCPPacketOut pak = GSTCPPacketOut.GetForTick(p => p.Init(GetPacketCode(eServerPackets.VisualEffect))))
			{
				pak.WriteShort((ushort)player.ObjectID);
				pak.WriteByte(0x09); // subcode

				byte flag = player.FreeLevelState;

				TimeSpan t = new TimeSpan((long)(DateTime.Now.Ticks - player.LastFreeLeveled.Ticks));

				ushort time = 0;
				//time is in minutes
				switch (player.Realm)
				{
					case eRealm.Albion:
						time = (ushort)((ServerProperties.Properties.FREELEVEL_DAYS_ALBION * 24 * 60) - t.TotalMinutes);
						break;
					case eRealm.Midgard:
						time = (ushort)((ServerProperties.Properties.FREELEVEL_DAYS_MIDGARD * 24 * 60) - t.TotalMinutes);
						break;
					case eRealm.Hibernia:
						time = (ushort)((ServerProperties.Properties.FREELEVEL_DAYS_HIBERNIA * 24 * 60) - t.TotalMinutes);
						break;
				}

				//flag 1 = above level, 2 = elligable, 3= time until, 4 = level and time until, 5 = level until
				pak.WriteByte(flag); //flag
				pak.WriteShort(0); //unknown
				pak.WriteShort(time); //time
				SendTCP(pak);
			}
		}

		public override void SendRegionColorScheme(byte color)
		{
			using (GSTCPPacketOut pak = GSTCPPacketOut.GetForTick(p => p.Init(GetPacketCode(eServerPackets.VisualEffect))))
			{
				pak.WriteShort(0); // not used
				pak.WriteByte(0x05); // subcode
				pak.WriteByte(color);
				pak.WriteInt(0); // not used
				SendTCP(pak);
			}
		}
	}
}
