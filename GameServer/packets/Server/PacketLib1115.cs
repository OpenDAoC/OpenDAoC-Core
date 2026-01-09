using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Reflection;
using System.Security.Cryptography;
using DOL.Database;
using DOL.GS.Keeps;

namespace DOL.GS.PacketHandler
{
    [PacketLib(1115, GameClient.eClientVersion.Version1115)]
    public class PacketLib1115 : PacketLib1114
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Constructs a new PacketLib for Client Version 1.115
        /// </summary>
        /// <param name="client">the gameclient this lib is associated with</param>
        public PacketLib1115(GameClient client)
            : base(client)
        {

        }

        /// <summary>
        /// Reply on Server Opening to Client Encryption Request
        /// Actually forces Encryption Off to work with Portal.
        /// </summary>
        public override void SendVersionAndCryptKey()
		{
			//Construct the new packet
			using (var pak = PooledObjectFactory.GetForTick<GSTCPPacketOut>().Init(GetPacketCode(eServerPackets.CryptKey)))
			{
				pak.WriteByte((byte)m_gameClient.ClientType);

				//Disable encryption (1110+ always encrypt)
				pak.WriteByte(0x00);

				// Reply with current version
				pak.WriteString((((int)m_gameClient.Version) / 1000) + "." + (((int)m_gameClient.Version) - 1000), 5);

				// revision, last seen (c) 0x63
				pak.WriteByte(0x00);

				// Build number
				pak.WriteByte(0x00); // last seen : 0x44 0x05
				pak.WriteByte(0x00);
				SendTCP(pak);
				m_gameClient.PacketProcessor.SendPendingPackets();
			}
		}

		public override void SendLoginGranted(byte color)
		{
			using (var pak = PooledObjectFactory.GetForTick<GSTCPPacketOut>().Init(GetPacketCode(eServerPackets.LoginGranted)))
			{
				pak.WritePascalString(m_gameClient.Account.Name);
				pak.WritePascalString(GameServer.Instance.Configuration.ServerNameShort); //server name
				pak.WriteByte(0x29); //Server ID
				pak.WriteByte(0x07); // test value...
				pak.WriteByte(0x00);
				pak.WriteByte(0x00);
				SendTCP(pak);
			}
		}

		/// <summary>
		/// New Item Packet Data in v1.115
		/// </summary>
		/// <param name="pak"></param>
		/// <param name="item"></param>
		protected override void WriteItemData(GSTCPPacketOut pak, DbInventoryItem item)
		{
			if (item == null)
			{
				pak.Fill(0x00, 23); // added one short in front of item data, v1.115
				return;
			}

			// Unknown
			pak.WriteShort((ushort)0);
			base.WriteItemData(pak, item);
		}

		/// <summary>
		/// New Item Packet Template Data in v1.115
		/// </summary>
		/// <param name="pak"></param>
		/// <param name="template"></param>
		/// <param name="count"></param>
		protected override void WriteTemplateData(GSTCPPacketOut pak, DbItemTemplate template, int count)
		{
			if (template == null)
			{
				pak.Fill(0x00, 23); // added one short in front of item data, v1.115
				return;
			}

			// Unknown
			pak.WriteShort((ushort)0);
			base.WriteTemplateData(pak, template, count);
		}

		/// <summary>
		/// Default Keep Model changed for 1.1115
		/// </summary>
		/// <param name="keep"></param>
		public override void SendKeepInfo(IGameKeep keep)
		{
			if (m_gameClient.Player == null)
				return;

			using (var pak = PooledObjectFactory.GetForTick<GSTCPPacketOut>().Init(GetPacketCode(eServerPackets.KeepInfo)))
			{
				pak.WriteShort((ushort)keep.KeepID);
				pak.WriteShort(0);
				pak.WriteInt((uint)keep.X);
				pak.WriteInt((uint)keep.Y);
				pak.WriteShort((ushort)keep.Heading);
				pak.WriteByte((byte)keep.Realm);
				pak.WriteByte((byte)keep.Level);//level
				pak.WriteShort(0);//unk
				pak.WriteByte(0xF7);//model
				pak.WriteByte(0);//unk

				SendTCP(pak);
			}
		}

		public override void SendWarmapUpdate(ICollection<IGameKeep> list)
		{
			if (m_gameClient.Player == null) return;

			using (var pak = PooledObjectFactory.GetForTick<GSTCPPacketOut>().Init(GetPacketCode(eServerPackets.WarMapClaimedKeeps)))
			{
				int KeepCount = 0;
				int TowerCount = 0;
				foreach (AbstractGameKeep keep in list)
				{
					// New Agramon tower are counted as keep
					if (keep is GameKeep || (keep.KeepID & 0xFF) > 150)
						KeepCount++;
					else
						TowerCount++;
				}
				// Flame on relic temples. Intermediate bits unknown / uncesessary.
				// Castle Excalibur  = 1 << 1
				// Castle Myrddin    = 1 << 3
				// Mjollner Faste    = 1 << 5
				// Grallarhorn Faste = 1 << 7
				// Dun Lamfhota      = 1 << 9
				// Dun Dagda         = 1 << 11
				pak.WriteShort(RelicGateMgr.GetRelicTempleWarmapFlags());
				pak.WriteByte((byte)KeepCount);
				pak.WriteByte((byte)TowerCount);
				byte albStr = 0;
				byte hibStr = 0;
				byte midStr = 0;
				byte albMagic = 0;
				byte hibMagic = 0;
				byte midMagic = 0;
				foreach (GameRelic relic in RelicMgr.GetRelics())
				{
					switch (relic.OriginalRealm)
					{
						case eRealm.Albion:
							if (relic.RelicType == eRelicType.Strength)
							{
								albStr = (byte)relic.Realm;
							}
							if (relic.RelicType == eRelicType.Magic)
							{
								albMagic = (byte)relic.Realm;
							}
							break;
						case eRealm.Hibernia:
							if (relic.RelicType == eRelicType.Strength)
							{
								hibStr = (byte)relic.Realm;
							}
							if (relic.RelicType == eRelicType.Magic)
							{
								hibMagic = (byte)relic.Realm;
							}
							break;
						case eRealm.Midgard:
							if (relic.RelicType == eRelicType.Strength)
							{
								midStr = (byte)relic.Realm;
							}
							if (relic.RelicType == eRelicType.Magic)
							{
								midMagic = (byte)relic.Realm;
							}
							break;
					}
				}
				pak.WriteByte(albStr);
				pak.WriteByte(midStr);
				pak.WriteByte(hibStr);
				pak.WriteByte(albMagic);
				pak.WriteByte(midMagic);
				pak.WriteByte(hibMagic);
				foreach (AbstractGameKeep keep in list)
				{
					int keepId = keep.KeepID;

					/*if (ServerProperties.Properties.USE_NEW_KEEPS == 1 || ServerProperties.Properties.USE_NEW_KEEPS == 2)
					{
						keepId -= 12;
						if ((keep.KeepID > 74 && keep.KeepID < 114) || (keep.KeepID > 330 && keep.KeepID < 370) || (keep.KeepID > 586 && keep.KeepID < 626)
							|| (keep.KeepID > 842 && keep.KeepID < 882) || (keep.KeepID > 1098 && keep.KeepID < 1138))
							keepId += 5;
					}*/

					int id = keepId & 0xFF;
					int tower = keep.KeepID >> 8;
					int map = (id / 25) - 1;

					int index = id - (map * 25 + 25);

					// Special Agramon zone
					if ((keep.KeepID & 0xFF) > 150)
						index = keep.KeepID - 151;

					int flag = (byte)keep.Realm; // 3 bits
					Guild guild = keep.Guild;
					string name = string.Empty;
					// map is now 0 indexed
					pak.WriteByte((byte)(((map - 1) << 6) | (index << 3) | tower));
					if (guild != null)
					{
						flag |= (byte)eRealmWarmapKeepFlags.Claimed;
						name = guild.Name;
					}

					//Teleport
					if (m_gameClient.Account.PrivLevel > (int)ePrivLevel.Player)
					{
						flag |= (byte)eRealmWarmapKeepFlags.Teleportable;
					}
					else
					{
						if (GameServer.KeepManager.FrontierRegionsList.Contains(m_gameClient.Player.CurrentRegionID) && m_gameClient.Player.Realm == keep.Realm)
						{
							GameKeep theKeep = keep as GameKeep;
							if (theKeep != null)
							{
								AbstractGameKeep supply1 = null;
								AbstractGameKeep supply2 = null;
								AbstractGameKeep last1 = null;
								AbstractGameKeep last2 = null;
								AbstractGameKeep town1 = null;
								AbstractGameKeep town2 = null;
								switch (m_gameClient.Player.Realm)
								{
										case eRealm.Midgard:
												supply1 = GameServer.KeepManager.GetKeepByID(79);   // Glenlock
												supply2 = GameServer.KeepManager.GetKeepByID(75);   // Bledmeer
												last1 = GameServer.KeepManager.GetKeepByID(80);   // Fensalir
												last2 = GameServer.KeepManager.GetKeepByID(81);   // Arvakre
												town1 = GameServer.KeepManager.GetKeepByID(12);   // Godrborg
												town2 = GameServer.KeepManager.GetKeepByID(13);   // Rensamark
												break;
										case eRealm.Albion:
												supply1 = GameServer.KeepManager.GetKeepByID(50);   // Benowyc
												supply2 = GameServer.KeepManager.GetKeepByID(53);   // Boldiam
												last1 = GameServer.KeepManager.GetKeepByID(56);   // Benowyc
												last2 = GameServer.KeepManager.GetKeepByID(55);   // Boldiam
												town1 = GameServer.KeepManager.GetKeepByID(10);   // Catterick Hamlet
												town2 = GameServer.KeepManager.GetKeepByID(11);   // Dinas Emrys
												break;
										case eRealm.Hibernia:
												supply1 = GameServer.KeepManager.GetKeepByID(103);   // nGed
												supply2 = GameServer.KeepManager.GetKeepByID(100);   // Crauchon
												last1 = GameServer.KeepManager.GetKeepByID(106);   // Ailinne
												last2 = GameServer.KeepManager.GetKeepByID(105);   // Scataigh
												town1 = GameServer.KeepManager.GetKeepByID(14);   // Crair Treflan
												town2 = GameServer.KeepManager.GetKeepByID(15);   // Magh Tuireadh
												break;
								}
								// Towns teleports are always available
								if (keep.KeepID == town1.KeepID || keep.KeepID == town2.KeepID)
									{
										flag |= (byte)eRealmWarmapKeepFlags.Teleportable;
									}
								
								
								// Teleport Flags for keeps inside our own realm
								// Summary: If we own middle keep (nged) and all towers from keep
								if (m_gameClient.Player.Realm == keep.OriginalRealm && (keep as GameKeep).OwnsAllTowers == true)
								{
									if (supply1.Realm == supply1.OriginalRealm && (last1.Realm == last1.OriginalRealm || last2.Realm == last2.OriginalRealm))
									{
										flag |= (byte)eRealmWarmapKeepFlags.Teleportable;
									}
									else if (supply1.Realm != supply1.OriginalRealm)
									{
										if (keep.KeepID == last1.KeepID || keep.KeepID == last2.KeepID)
										{
											flag |= (byte)eRealmWarmapKeepFlags.Teleportable; // Can always teleport to last two keeps
										}
									}
								} 
								else if (m_gameClient.Player.Realm != keep.OriginalRealm && (keep as GameKeep).OwnsAllTowers == true && supply1.Realm == supply1.OriginalRealm && supply2.Realm == supply2.OriginalRealm && (last1.Realm == last1.OriginalRealm || last2.Realm == last2.OriginalRealm))
								{
									if (keep.Name == "Dun Crauchon" || keep.Name == "Dun Crimthain" || keep.Name == "Dun Bolg" || keep.Name == "Nottmoor Faste" || keep.Name == "Bledmeer Faste" || keep.Name == "Blendrake Faste" || keep.Name == "Caer Benowyc" || keep.Name == "Caer Erasleigh" || keep.Name == "Caer Berkstead")
									{
										// Special case for border keeps, only allow teleport if both supply keeps are owned
										flag |= (byte)eRealmWarmapKeepFlags.Teleportable;
									}
								}
							}
						}
					}

					if (keep.InCombat)
					{
						flag |= (byte)eRealmWarmapKeepFlags.UnderSiege;
					}

					pak.WriteByte((byte)flag);
					pak.WritePascalString(name);
				}
				SendTCP(pak);
			}
		}
	}
}