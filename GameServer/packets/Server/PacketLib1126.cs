using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using DOL.Database;

namespace DOL.GS.PacketHandler
{
	[PacketLib(1126, GameClient.eClientVersion.Version1126)]
	public class PacketLib1126 : PacketLib1125
	{
		private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);
		private static readonly eInventorySlot[] _visibleEquipmentSlots =
		[
			eInventorySlot.RightHandWeapon,
			eInventorySlot.LeftHandWeapon,
			eInventorySlot.TwoHandWeapon,
			eInventorySlot.DistanceWeapon,
			eInventorySlot.HeadArmor,
			eInventorySlot.HandsArmor,
			eInventorySlot.FeetArmor,
			eInventorySlot.TorsoArmor,
			eInventorySlot.Cloak,
			eInventorySlot.LegsArmor,
			eInventorySlot.ArmsArmor,
		];

		public PacketLib1126(GameClient client) : base(client) { }

		/// <summary>
		/// 1126 update - less info / shorter packet sent back
		/// </summary>
		public override void SendDupNameCheckReply(string name, byte result)
		{
			if (m_gameClient?.Account == null)
				return;

			using (var pak = PooledObjectFactory.GetForTick<GSTCPPacketOut>().Init(GetPacketCode(eServerPackets.DupNameCheckReply)))
			{
				pak.FillString(name, 24);
				pak.WriteByte(result);
				SendTCP(pak);
			}
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
				//Disable encryption (1110+ always encrypt)
				pak.WriteIntLowEndian(0);
				// pak.Write(key, 0, key.Length);

				// From now on we expect RSA!
				// _gameClient.PacketProcessor.Encoding.EncryptionState = eEncryptionState.PseudoRC4Encrypted; // disabled by the launcher

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

		public override async void SendCharacterOverview(eRealm realm)
		{
			if (realm is < eRealm._FirstPlayerRealm or > eRealm._LastPlayerRealm)
				throw new Exception($"CharacterOverview requested for unknown realm {realm}");

			if (m_gameClient.Account.Characters == null || m_gameClient.Account.Characters.Length == 0)
			{
				using (var pak = PooledObjectFactory.GetForTick<GSTCPPacketOut>().Init(GetPacketCode(eServerPackets.CharacterOverview1126)))
				{
					pak.WriteIntLowEndian(0);
					pak.WriteIntLowEndian(0);
					pak.WriteIntLowEndian(0);
					pak.WriteIntLowEndian(0);
					SendTCP(pak);
					return;
				}
			}

			int firstSlot = (int) realm * 100;
			int lastSlot = firstSlot + 9;
			uint enableRealmSwitcherBit = (uint) (GameServer.ServerRules.IsAllowedCharsInAllRealms(m_gameClient) ? 1 : 0);

			Dictionary<int, DbCoreCharacter> charsBySlot = new();

			foreach (DbCoreCharacter character in m_gameClient.Account.Characters)
			{
				if (character.AccountSlot < firstSlot || character.AccountSlot > lastSlot)
					continue;

				if (charsBySlot.TryAdd(character.AccountSlot, character))
					continue;

				if (log.IsErrorEnabled)
					log.Error($"SendCharacterOverview - Duplicate char in slot? Slot: {character.AccountSlot}, Account: {character.AccountName}");
			}

			var itemsByOwnerId = await BuildItemsByOwnerId(charsBySlot, firstSlot, lastSlot);

			using (var pak = PooledObjectFactory.GetForTick<GSTCPPacketOut>().Init(GetPacketCode(eServerPackets.CharacterOverview1126)))
			{
				pak.WriteIntLowEndian(enableRealmSwitcherBit); // 0x01 & 0x02 are flags.
				pak.WriteIntLowEndian(0);
				pak.WriteIntLowEndian(0);
				pak.WriteIntLowEndian(0);

				for (int i = firstSlot; i <= lastSlot; i++)
				{
					if (!charsBySlot.TryGetValue(i, out DbCoreCharacter character))
					{
						pak.WriteByte(0);
						continue;
					}

					if (!itemsByOwnerId.TryGetValue(character.ObjectId, out Dictionary<eInventorySlot, DbInventoryItem> charItems))
						charItems = new();

					byte extensionTorso = 0;
					byte extensionGloves = 0;
					byte extensionBoots = 0;

					if (charItems.TryGetValue(eInventorySlot.TorsoArmor, out DbInventoryItem item))
						extensionTorso = item.Extension;

					if (charItems.TryGetValue(eInventorySlot.HandsArmor, out item))
						extensionGloves = item.Extension;

					if (charItems.TryGetValue(eInventorySlot.FeetArmor, out item))
						extensionBoots = item.Extension;

					string locationDescription = string.Empty;
					Region region = WorldMgr.GetRegion((ushort) character.Region);

					if (region != null)
					{
						locationDescription = m_gameClient.GetTranslatedSpotDescription(region, character.Xpos, character.Ypos, character.Zpos);

						if (locationDescription.Length > 22)
							locationDescription = locationDescription[..22];
					}

					string className = string.Empty;

					if (character.Class != 0)
						className = ((eCharacterClass) character.Class).ToString();

					string raceName = m_gameClient.RaceToTranslatedName(character.Race, character.Gender);

					charItems.TryGetValue(eInventorySlot.RightHandWeapon, out DbInventoryItem rightHandWeapon);
					charItems.TryGetValue(eInventorySlot.LeftHandWeapon, out DbInventoryItem leftHandWeapon);
					charItems.TryGetValue(eInventorySlot.TwoHandWeapon, out DbInventoryItem twoHandWeapon);
					charItems.TryGetValue(eInventorySlot.DistanceWeapon, out DbInventoryItem distanceWeapon);
					charItems.TryGetValue(eInventorySlot.HeadArmor, out DbInventoryItem helmet);
					charItems.TryGetValue(eInventorySlot.HandsArmor, out DbInventoryItem gloves);
					charItems.TryGetValue(eInventorySlot.FeetArmor, out DbInventoryItem boots);
					charItems.TryGetValue(eInventorySlot.TorsoArmor, out DbInventoryItem torso);
					charItems.TryGetValue(eInventorySlot.Cloak, out DbInventoryItem cloak);
					charItems.TryGetValue(eInventorySlot.LegsArmor, out DbInventoryItem legs);
					charItems.TryGetValue(eInventorySlot.ArmsArmor, out DbInventoryItem arms);

					ushort rightHandColor = 0;
					ushort helmetColor = 0;
					ushort glovesColor = 0;
					ushort bootsColor = 0;
					ushort leftHandWeaponColor = 0;
					ushort torsoColor = 0;
					ushort cloakColor = 0;
					ushort legsColor = 0;
					ushort armsColor = 0;

					if (rightHandWeapon != null)
						rightHandColor = (ushort) (rightHandWeapon.Emblem != 0 ? rightHandWeapon.Emblem : rightHandWeapon.Color);

					if (helmet != null)
						helmetColor = (ushort) (helmet.Emblem != 0 ? helmet.Emblem : helmet.Color);

					if (gloves != null)
						glovesColor = (ushort) (gloves.Emblem != 0 ? gloves.Emblem : gloves.Color);

					if (boots != null)
						bootsColor = (ushort) (boots.Emblem != 0 ? boots.Emblem : boots.Color);

					if (leftHandWeapon != null)
						leftHandWeaponColor = (ushort) (leftHandWeapon.Emblem != 0 ? leftHandWeapon.Emblem : leftHandWeapon.Color);

					if (torso != null)
						torsoColor = (ushort) (torso.Emblem != 0 ? torso.Emblem : torso.Color);

					if (cloak != null)
						cloakColor = (ushort) (cloak.Emblem != 0 ? cloak.Emblem : cloak.Color);

					if (legs != null)
						legsColor = (ushort) (legs.Emblem != 0 ? legs.Emblem : legs.Color);

					if (arms != null)
						armsColor = (ushort) (arms.Emblem != 0 ? arms.Emblem : arms.Color);

					pak.WriteByte((byte) character.Level);
					pak.WritePascalStringIntLE(character.Name);
					pak.WriteIntLowEndian(0x18);
					pak.WriteByte(1); // always 1 ?
					pak.WriteByte(character.EyeSize); // seems to be : 0xF0 = eyes, 0x0F = nose
					pak.WriteByte(character.LipSize); // seems to be : 0xF0 = lips, 0xF = jaw
					pak.WriteByte(character.EyeColor); // seems to be : 0xF0 = eye color, 0x0F = skin tone
					pak.WriteByte(character.HairColor);
					pak.WriteByte(character.FaceType); // seems to be : 0xF0 = face
					pak.WriteByte(character.HairStyle); // seems to be : 0xF0 = hair
					pak.WriteByte((byte) ((extensionBoots << 4) | extensionGloves));
					pak.WriteByte((byte) ((extensionTorso << 4) | (character.IsCloakHoodUp ? 0x1 : 0x0)));
					pak.WriteByte(character.CustomisationStep); // 1 = auto generate config, 2= config ended by player, 3= enable config to player
					pak.WriteByte(character.MoodType);
					pak.Fill(0x0, 13);
					pak.WritePascalStringIntLE(locationDescription);
					pak.WritePascalStringIntLE(className);
					pak.WritePascalStringIntLE(raceName);
					pak.WriteShortLowEndian((ushort) character.CurrentModel);
					pak.WriteByte((byte) character.Region);

					if (region == null || (int) m_gameClient.ClientType > region.Expansion)
						pak.WriteByte(0x00);
					else
						pak.WriteByte((byte) (region.Expansion + 1)); //0x04-Cata zone, 0x05 - DR zone

					pak.WriteShortLowEndian((ushort) (helmet != null ? helmet.Model : 0));
					pak.WriteShortLowEndian((ushort) (gloves != null ? gloves.Model : 0));
					pak.WriteShortLowEndian((ushort) (boots != null ? boots.Model : 0));
					pak.WriteShortLowEndian(rightHandColor);
					pak.WriteShortLowEndian((ushort) (torso != null ? torso.Model : 0));
					pak.WriteShortLowEndian((ushort) (cloak != null ? cloak.Model : 0));
					pak.WriteShortLowEndian((ushort) (legs != null ? legs.Model : 0));
					pak.WriteShortLowEndian((ushort) (arms != null ? arms.Model : 0));

					pak.WriteShortLowEndian(helmetColor);
					pak.WriteShortLowEndian(glovesColor);
					pak.WriteShortLowEndian(bootsColor);
					pak.WriteShortLowEndian(leftHandWeaponColor);
					pak.WriteShortLowEndian(torsoColor);
					pak.WriteShortLowEndian(cloakColor);
					pak.WriteShortLowEndian(legsColor);
					pak.WriteShortLowEndian(armsColor);

					pak.WriteShortLowEndian((ushort) (rightHandWeapon != null ? rightHandWeapon.Model : 0));
					pak.WriteShortLowEndian((ushort) (leftHandWeapon != null ? leftHandWeapon.Model : 0));
					pak.WriteShortLowEndian((ushort) (twoHandWeapon != null ? twoHandWeapon.Model : 0));
					pak.WriteShortLowEndian((ushort) (distanceWeapon != null ? distanceWeapon.Model : 0));

					pak.WriteByte((byte) character.Strength);
					pak.WriteByte((byte) character.Quickness);
					pak.WriteByte((byte) character.Constitution);
					pak.WriteByte((byte) character.Dexterity);
					pak.WriteByte((byte) character.Intelligence);
					pak.WriteByte((byte) character.Piety);
					pak.WriteByte((byte) character.Empathy);
					pak.WriteByte((byte) character.Charisma);

					pak.WriteByte((byte) character.Class);
					pak.WriteByte((byte) character.Realm);
					pak.WriteByte((byte) ((((character.Race & 0x10) << 2) + (character.Race & 0x0F)) | (character.Gender << 7)));

					if ((eActiveWeaponSlot) character.ActiveWeaponSlot is eActiveWeaponSlot.TwoHanded)
					{
						pak.WriteByte(0x02);
						pak.WriteByte(0x02);
					}
					else if ((eActiveWeaponSlot) character.ActiveWeaponSlot is eActiveWeaponSlot.Distance)
					{
						pak.WriteByte(0x03);
						pak.WriteByte(0x03);
					}
					else
					{
						pak.WriteByte((byte) (rightHandWeapon != null ? 0x00 : 0xFF));
						pak.WriteByte((byte) (leftHandWeapon != null ? 0x01 : 0xFF));
					}

					pak.WriteByte(0); // SI = 1, Classic = 0
					pak.WriteByte((byte) character.Constitution);
					pak.WriteByte(0); // unknown
				}

				SendTCP(pak);
			}

			async Task<Dictionary<string, Dictionary<eInventorySlot, DbInventoryItem>>> BuildItemsByOwnerId(Dictionary<int, DbCoreCharacter> charsBySlot, int firstSlot, int lastSlot)
			{
				Dictionary<string, Dictionary<eInventorySlot, DbInventoryItem>> itemsByOwnerId = new();

				if (charsBySlot.Count == 0)
					return itemsByOwnerId;

				var items = await DOLDB<DbInventoryItem>.SelectObjectsAsync(DB.Column("OwnerID").IsIn(charsBySlot.Values.Select(c => c.ObjectId)).And(DB.Column("SlotPosition").IsIn(_visibleEquipmentSlots)));

				foreach (DbInventoryItem item in items)
				{
					try
					{
						if (!itemsByOwnerId.ContainsKey(item.OwnerID))
							itemsByOwnerId.Add(item.OwnerID, new());

						itemsByOwnerId[item.OwnerID].Add((eInventorySlot) item.SlotPosition, item);
					}
					catch (Exception ex)
					{
						if (log.IsErrorEnabled)
							log.Error($"SendCharacterOverview - Duplicate item on character? OwnerID: {item.OwnerID}, SlotPosition: {item.SlotPosition}, Account: {m_gameClient.Account.Name}", ex);
					}
				}

				return itemsByOwnerId;
			}
		}

		public override void SendRegions(ushort regionId)
		{
			if (!m_gameClient.Socket.Connected)
				return;

			Region region = WorldMgr.GetRegion(regionId);
			if (region == null)
				return;
			using (var pak = PooledObjectFactory.GetForTick<GSTCPPacketOut>().Init(GetPacketCode(eServerPackets.ClientRegion)))
			{
				var ip = region.ServerIP;
				if (ip == "any" || ip == "0.0.0.0" || ip == "127.0.0.1" || ip.StartsWith("10.") || ip.StartsWith("192.168."))
					ip = ((IPEndPoint)m_gameClient.Socket.LocalEndPoint).Address.ToString();
				pak.WritePascalStringIntLE(ip);
				pak.WriteIntLowEndian(region.ServerPort);
				pak.WriteIntLowEndian(region.ServerPort);
				SendTCP(pak);
			}
		}

		public override void SendUpdateWeaponAndArmorStats()
		{
			if (m_gameClient.Player == null)
				return;

			using (var pak = PooledObjectFactory.GetForTick<GSTCPPacketOut>().Init(GetPacketCode(eServerPackets.VariousUpdate)))
			{
				pak.WriteByte(0x05); //subcode
				pak.WriteByte(6); //number of entries
				pak.WriteByte(0x00); //subtype
				pak.WriteByte(0x00); //unk

				// weapondamage
				var wd = (int)(m_gameClient.Player.WeaponDamage(m_gameClient.Player.ActiveWeapon) * 100.0);
				pak.WriteByte((byte)(wd / 256));
				pak.WriteByte(0x00);
				pak.WriteByte((byte)(wd % 256));
				pak.WriteByte(0x00);
				// weaponskill
				int ws = m_gameClient.Player.DisplayedWeaponSkill;
				pak.WriteByte((byte)(ws >> 8));
				pak.WriteByte(0x00);
				pak.WriteByte((byte)(ws & 0xff));
				pak.WriteByte(0x00);
				// overall EAF
				int eaf = m_gameClient.Player.EffectiveOverallAF;
				pak.WriteByte((byte)(eaf >> 8));
				pak.WriteByte(0x00);
				pak.WriteByte((byte)(eaf & 0xff));
				pak.WriteByte(0x00);
				SendTCP(pak);
			}
		}

		public override void SendAddFriends(string[] friendNames)
		{
			using (var pak = PooledObjectFactory.GetForTick<GSTCPPacketOut>().Init(GetPacketCode(eServerPackets.AddFriend)))
			{
				pak.WriteByte((byte)friendNames.Length);
				foreach (string friend in friendNames)
				{
					pak.WritePascalStringIntLE(friend);
				}
				SendTCP(pak);
			}
		}

		public override void SendRemoveFriends(string[] friendNames)
		{
			using (var pak = PooledObjectFactory.GetForTick<GSTCPPacketOut>().Init(GetPacketCode(eServerPackets.RemoveFriend)))
			{
				pak.WriteByte(0x00);
				foreach (string friend in friendNames)
				{
					pak.WritePascalStringIntLE(friend);
				}
				SendTCP(pak);
			}
		}
	}
}
