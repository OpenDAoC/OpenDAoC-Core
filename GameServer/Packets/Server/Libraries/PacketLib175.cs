using System;
using System.Collections.Generic;
using System.Reflection;
using Core.GS.Crafting;
using Core.GS.Enums;
using Core.GS.Expansions.Foundations;
using Core.GS.Players.Titles;
using Core.GS.PlayerTitles;
using log4net;

namespace Core.GS.Packets.Server;

[PacketLib(175, GameClient.eClientVersion.Version175)]
public class PacketLib175 : PacketLib174
{
	/// <summary>
	/// Defines a logger for this class.
	/// </summary>
	private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

	/// <summary>
	/// Constructs a new PacketLib for Version 1.75 clients
	/// </summary>
	/// <param name="client">the gameclient this lib is associated with</param>
	public PacketLib175(GameClient client):base(client)
	{
	}

	public override void SendCustomTextWindow(string caption, IList<string> text)
	{
		if (text == null)
			return;

		using (GsTcpPacketOut pak = new GsTcpPacketOut(GetPacketCode(EServerPackets.DetailWindow)))
		{
			pak.WriteByte(0); // new in 1.75
			if (caption == null)
				caption = "";
			if (caption.Length > byte.MaxValue)
				caption = caption.Substring(0, byte.MaxValue);
			pak.WritePascalString(caption); //window caption

			WriteCustomTextWindowData(pak, text);

			//Trailing Zero!
			pak.WriteByte(0);
			SendTCP(pak);
		}
	}

	public override void SendPlayerTitles()
	{
		var titles = m_gameClient.Player.Titles;
		using (GsTcpPacketOut pak = new GsTcpPacketOut(GetPacketCode(EServerPackets.DetailWindow)))
		{
			pak.WriteByte(1); // new in 1.75
			pak.WritePascalString("Player Statistics"); //window caption

			byte line = 1;
			foreach (string str in m_gameClient.Player.FormatStatistics())
			{
				pak.WriteByte(line++);
				pak.WritePascalString(str);
			}

			pak.WriteByte(200);
			long titlesCountPos = pak.Position;
			pak.WriteByte(0); // length of all titles part
			pak.WriteByte((byte)titles.Count);
			line = 0;
			foreach (IPlayerTitle title in titles)
			{
				pak.WriteByte(line++);
				pak.WritePascalString(title.GetDescription(m_gameClient.Player));
			}
			long titlesLen = (pak.Position - titlesCountPos - 1); // include titles count
			if (titlesLen > byte.MaxValue)
				log.WarnFormat("Titles block is too long! {0} (player: {1})", titlesLen, m_gameClient.Player);
			//Trailing Zero!
			pak.WriteByte(0);
			//Set titles length
			pak.Position = titlesCountPos;
			pak.WriteByte((byte)titlesLen); // length of all titles part
			SendTCP(pak);
		}
	}

	public override void SendPlayerTitleUpdate(GamePlayer player)
	{
		using (GsTcpPacketOut pak = new GsTcpPacketOut(GetPacketCode(EServerPackets.VisualEffect)))
		{
			pak.WriteShort((ushort) player.ObjectID);
			pak.WriteByte(0x0B); // subcode
			IPlayerTitle title = player.CurrentTitle;
			if (title == PlayerTitleMgr.ClearTitle)
			{
				pak.WriteByte(0); // flag
				pak.WriteInt(0); // unk1 + str len
			}
			else
			{
				pak.WriteByte(1); // flag
				string val = GameServer.ServerRules.GetPlayerTitle(m_gameClient.Player, player);
				pak.WriteShort((ushort) val.Length);
				pak.WriteShort(0); // unk1
				pak.WriteStringBytes(val);
			}
			SendTCP(pak);
		}
	}

	public override void SendUpdatePlayer()
	{
		GamePlayer player = m_gameClient.Player;
		if (player == null)
			return;

		using (GsTcpPacketOut pak = new GsTcpPacketOut(GetPacketCode(EServerPackets.VariousUpdate)))
		{
			pak.WriteByte(0x03); //subcode
			pak.WriteByte(0x0e); //number of entry
			pak.WriteByte(0x00); //subtype
			pak.WriteByte(0x00); //unk
			//entry :

			pak.WriteByte(player.GetDisplayLevel(m_gameClient.Player)); //level
			pak.WritePascalString(player.Name);

			pak.WriteByte((byte) (player.MaxHealth >> 8)); // maxhealth high byte ?
			pak.WritePascalString(player.PlayerClass.Name); // class name
			pak.WriteByte((byte) (player.MaxHealth & 0xFF)); // maxhealth low byte ?

			pak.WritePascalString( /*"The "+*/player.PlayerClass.Profession); // Profession

			pak.WriteByte(0x00); //unk

            pak.WritePascalString(player.PlayerClass.GetTitle(player, player.Level));

			//todo make function to calcule realm rank
			//client.Player.RealmPoints
			//todo i think it s realmpoint percent not realrank
			pak.WriteByte((byte) player.RealmLevel); //urealm rank
			pak.WritePascalString(player.RealmRankTitle(player.Client.Account.Language));
			pak.WriteByte((byte) player.RealmSpecialtyPoints); // realm skill points

			pak.WritePascalString(player.PlayerClass.BaseName); // base class

			pak.WriteByte((byte)(HouseMgr.GetHouseNumberByPlayer(player) >> 8)); // personal house high byte
			pak.WritePascalString(player.GuildName);
			pak.WriteByte((byte)(HouseMgr.GetHouseNumberByPlayer(player) & 0xFF)); // personal house low byte

			pak.WritePascalString(player.LastName);

			pak.WriteByte(0x0); // ML Level
			pak.WritePascalString(player.RaceName);

			pak.WriteByte(0x0);
			if (player.GuildRank != null)
				pak.WritePascalString(player.GuildRank.Title);
			else
				pak.WritePascalString("");
			pak.WriteByte(0x0);

			ACraftingSkill skill = CraftingMgr.getSkillbyEnum(player.CraftingPrimarySkill);
			if (skill != null)
				pak.WritePascalString(skill.Name); //crafter guilde: alchemist
			else
				pak.WritePascalString("None"); //no craft skill at start

			pak.WriteByte(0x0);
			pak.WritePascalString(player.CraftTitle.GetValue(player, player)); //crafter title: legendary alchemist

			pak.WriteByte(0x0);
			pak.WritePascalString("None"); //ML title

			// new in 1.75
			pak.WriteByte(0x0);
			string title = "None";
			if (player.CurrentTitle != PlayerTitleMgr.ClearTitle)
				title = GameServer.ServerRules.GetPlayerTitle(player, player);
			pak.WritePascalString(title); // new in 1.74
			SendTCP(pak);
		}
	}

	public override void SendCharStatsUpdate()
	{
		if (m_gameClient.Player == null)
			return;

		EStat[] updateStats =
		{
			EStat.STR,
			EStat.DEX,
			EStat.CON,
			EStat.QUI,
			EStat.INT,
			EStat.PIE,
			EStat.EMP,
			EStat.CHR,
		};

		int[] baseStats = new int[updateStats.Length];
		int[] modStats = new int[updateStats.Length];
		int[] itemCaps = new int[updateStats.Length];
		int itemCap = (int)(m_gameClient.Player.Level * 1.5);
		int bonusCap = m_gameClient.Player.Level / 2 + 1;

		for (int i = 0; i < updateStats.Length; i++)
		{
			int cap = itemCap;
			switch ((EProperty)updateStats[i])
			{
				case EProperty.Strength:
					cap += m_gameClient.Player.ItemBonus[(int)EProperty.StrCapBonus];
					break;
				case EProperty.Dexterity:
					cap += m_gameClient.Player.ItemBonus[(int)EProperty.DexCapBonus];
					break;
				case EProperty.Constitution:
					cap += m_gameClient.Player.ItemBonus[(int)EProperty.ConCapBonus];
					break;
				case EProperty.Quickness:
					cap += m_gameClient.Player.ItemBonus[(int)EProperty.QuiCapBonus];
					break;
				case EProperty.Intelligence:
					cap += m_gameClient.Player.ItemBonus[(int)EProperty.IntCapBonus];
					break;
				case EProperty.Piety:
					cap += m_gameClient.Player.ItemBonus[(int)EProperty.PieCapBonus];
					break;
				case EProperty.Charisma:
					cap += m_gameClient.Player.ItemBonus[(int)EProperty.ChaCapBonus];
					break;
				case EProperty.Empathy:
					cap += m_gameClient.Player.ItemBonus[(int)EProperty.EmpCapBonus];
					break;
				default: break;
			}

			if (updateStats[i] == m_gameClient.Player.PlayerClass.ManaStat)
				cap += m_gameClient.Player.ItemBonus[(int)EProperty.AcuCapBonus];

			itemCaps[i] = Math.Min(cap, itemCap + bonusCap);
		}

		using (GsTcpPacketOut pak = new GsTcpPacketOut(GetPacketCode(EServerPackets.StatsUpdate)))
		{
			// base
			for (int i = 0; i < updateStats.Length; i++)
			{
				baseStats[i] = m_gameClient.Player.GetBaseStat(updateStats[i]);

				if (updateStats[i] == EStat.CON)
					baseStats[i] -= m_gameClient.Player.TotalConstitutionLostAtDeath;

				pak.WriteShort((ushort)baseStats[i]);
			}

			pak.WriteShort(0);

			// buffs/debuffs only; remove base, item bonus, RA bonus, class bonus
			for (int i = 0; i < updateStats.Length; i++)
			{
				modStats[i] = m_gameClient.Player.GetModified((EProperty)updateStats[i]);
				int abilityBonus = m_gameClient.Player.AbilityBonus[(int)updateStats[i]];
				int acuityItemBonus = 0;

				if (updateStats[i] == m_gameClient.Player.PlayerClass.ManaStat)
				{
					if (m_gameClient.Player.PlayerClass.ID != (int)EPlayerClass.Scout && m_gameClient.Player.PlayerClass.ID != (int)EPlayerClass.Hunter && m_gameClient.Player.PlayerClass.ID != (int)EPlayerClass.Ranger
						&& m_gameClient.Player.PlayerClass.ID != (int)EPlayerClass.Nightshade)
					{
						abilityBonus += m_gameClient.Player.AbilityBonus[(int)EProperty.Acuity];

						if (m_gameClient.Player.PlayerClass.ClassType != EPlayerClassType.PureTank)
							acuityItemBonus = m_gameClient.Player.ItemBonus[(int)EProperty.Acuity];
					}
				}

				int buff = modStats[i] - baseStats[i];
				buff -= abilityBonus;
				buff -= Math.Min(itemCaps[i], m_gameClient.Player.ItemBonus[(int)updateStats[i]] + acuityItemBonus);

				pak.WriteShort((ushort)buff);
			}

			pak.WriteShort(0);

			// item bonuses
			for (int i = 0; i < updateStats.Length; i++)
			{
				int acuityItemBonus = 0;

				if (updateStats[i] == m_gameClient.Player.PlayerClass.ManaStat)
				{
					if (m_gameClient.Player.PlayerClass.ID != (int)EPlayerClass.Scout && m_gameClient.Player.PlayerClass.ID != (int)EPlayerClass.Hunter && m_gameClient.Player.PlayerClass.ID != (int)EPlayerClass.Ranger
						&& m_gameClient.Player.PlayerClass.ID != (int)EPlayerClass.Nightshade)
					{

						if (m_gameClient.Player.PlayerClass.ClassType != EPlayerClassType.PureTank)
							acuityItemBonus = m_gameClient.Player.ItemBonus[(int)EProperty.Acuity];
					}
				}

				pak.WriteShort((ushort)(m_gameClient.Player.ItemBonus[(int)updateStats[i]] + acuityItemBonus));
			}

			pak.WriteShort(0);

			// item caps
			for (int i = 0; i < updateStats.Length; i++)
			{
				pak.WriteByte((byte)itemCaps[i]);
			}

			pak.WriteByte(0);

			// RA bonuses
			for (int i = 0; i < updateStats.Length; i++)
			{
				int acuityItemBonus = 0;
				if (m_gameClient.Player.PlayerClass.ClassType != EPlayerClassType.PureTank && (int)updateStats[i] == (int)m_gameClient.Player.PlayerClass.ManaStat)
				{
					if (m_gameClient.Player.PlayerClass.ID != (int)EPlayerClass.Scout && m_gameClient.Player.PlayerClass.ID != (int)EPlayerClass.Hunter && m_gameClient.Player.PlayerClass.ID != (int)EPlayerClass.Ranger
						&& m_gameClient.Player.PlayerClass.ID != (int)EPlayerClass.Nightshade)
					{
						acuityItemBonus = m_gameClient.Player.AbilityBonus[(int)EProperty.Acuity];
					}
				}
				pak.WriteByte((byte)(m_gameClient.Player.AbilityBonus[(int)updateStats[i]] + acuityItemBonus));
			}

			pak.WriteByte(0);

			//Why don't we and mythic use this class bonus byte?
			//pak.Fill(0, 9);
			//if (m_gameClient.Player.CharacterClass.ID == (int)eCharacterClass.Vampiir)
			//	pak.WriteByte((byte)(m_gameClient.Player.Level - 5)); // Vampire bonuses
			//else
			pak.WriteByte(0x00); // FF if resists packet
			pak.WriteByte((byte)m_gameClient.Player.TotalConstitutionLostAtDeath);
			pak.WriteShort((ushort)m_gameClient.Player.MaxHealth);
			pak.WriteShort(0);

			SendTCP(pak);
		}
	}

	public override void SendCharResistsUpdate()
	{
		if (m_gameClient.Player == null)
			return;

		EResist[] updateResists =
		{
			EResist.Crush,
			EResist.Slash,
			EResist.Thrust,
			EResist.Heat,
			EResist.Cold,
			EResist.Matter,
			EResist.Body,
			EResist.Spirit,
			EResist.Energy,
		};

		int[] racial = new int[updateResists.Length];
		int[] caps = new int[updateResists.Length];
		int cap = m_gameClient?.Player != null ? (m_gameClient.Player.Level >> 1) + 1 : 1;

		for (int i = 0; i < updateResists.Length; i++)
		{
			caps[i] = cap;
		}

		using (GsTcpPacketOut pak = new GsTcpPacketOut(GetPacketCode(EServerPackets.StatsUpdate)))
		{

			// racial resists
			for (int i = 0; i < updateResists.Length; i++)
			{
				racial[i] = SkillBase.GetRaceResist(m_gameClient.Player.Race, updateResists[i]);
				pak.WriteShort((ushort)racial[i]);
			}

			// buffs/debuffs only; remove base, item bonus, RA bonus, race bonus
			for (int i = 0; i < updateResists.Length; i++)
			{
				int mod = m_gameClient.Player.GetModified((EProperty)updateResists[i]);
				int buff = mod - racial[i] - m_gameClient.Player.AbilityBonus[(int)updateResists[i]] - Math.Min(caps[i], m_gameClient.Player.ItemBonus[(int)updateResists[i]]);
				pak.WriteShort((ushort)buff);
			}

			// item bonuses
			for (int i = 0; i < updateResists.Length; i++)
			{
				pak.WriteShort((ushort)(m_gameClient.Player.ItemBonus[(int)updateResists[i]]));
			}

			// item caps
			for (int i = 0; i < updateResists.Length; i++)
			{
				pak.WriteByte((byte)caps[i]);
			}

			// RA bonuses
			for (int i = 0; i < updateResists.Length; i++)
			{
				pak.WriteByte((byte)(m_gameClient.Player.AbilityBonus[(int)updateResists[i]]));
			}

			pak.WriteByte(0xFF); // FF if resists packet
			pak.WriteByte(0);
			pak.WriteShort(0);
			pak.WriteShort(0);

			SendTCP(pak);
		}
	}

	public override void SendPlayerCreate(GamePlayer playerToCreate)
	{
		Region playerRegion = playerToCreate.CurrentRegion;
		if (playerRegion == null)
		{
			if (log.IsWarnEnabled)
				log.Warn("SendPlayerCreate: playerRegion == null");
			return;
		}

		Zone playerZone = playerToCreate.CurrentZone;
		if (playerZone == null)
		{
			if (log.IsWarnEnabled)
				log.Warn("SendPlayerCreate: playerZone == null");
			return;
		}

		if (m_gameClient.Player == null || playerToCreate.IsVisibleTo(m_gameClient.Player) == false)
			return;

		using (GsTcpPacketOut pak = new GsTcpPacketOut(GetPacketCode(EServerPackets.PlayerCreate172)))
		{
			pak.WriteShort((ushort)playerToCreate.Client.SessionID);
			pak.WriteShort((ushort)playerToCreate.ObjectID);
			pak.WriteShort(playerToCreate.Model);
			pak.WriteShort((ushort)playerToCreate.Z);
            //Dinberg:Instances - as with all objects, we need to use a zoneSkinID for clientside positioning.
			pak.WriteShort(playerZone.ZoneSkinID);
			pak.WriteShort((ushort)playerRegion.GetXOffInZone(playerToCreate.X, playerToCreate.Y));
			pak.WriteShort((ushort)playerRegion.GetYOffInZone(playerToCreate.X, playerToCreate.Y));
			pak.WriteShort(playerToCreate.Heading);

			pak.WriteByte(playerToCreate.GetFaceAttribute(ECharFacePart.EyeSize)); //1-4 = Eye Size / 5-8 = Nose Size
			pak.WriteByte(playerToCreate.GetFaceAttribute(ECharFacePart.LipSize)); //1-4 = Ear size / 5-8 = Kin size
			pak.WriteByte(playerToCreate.GetFaceAttribute(ECharFacePart.MoodType)); //1-4 = Ear size / 5-8 = Kin size
			pak.WriteByte(playerToCreate.GetFaceAttribute(ECharFacePart.EyeColor)); //1-4 = Skin Color / 5-8 = Eye Color
			pak.WriteByte(playerToCreate.GetDisplayLevel(m_gameClient.Player));
			pak.WriteByte(playerToCreate.GetFaceAttribute(ECharFacePart.HairColor)); //Hair: 1-4 = Color / 5-8 = unknown
			pak.WriteByte(playerToCreate.GetFaceAttribute(ECharFacePart.FaceType)); //1-4 = Unknown / 5-8 = Face type
			pak.WriteByte(playerToCreate.GetFaceAttribute(ECharFacePart.HairStyle)); //1-4 = Unknown / 5-8 = Hair Style

			int flags = (GameServer.ServerRules.GetLivingRealm(m_gameClient.Player, playerToCreate) & 0x03) << 2;
			if (playerToCreate.IsAlive == false) flags |= 0x01;
			if (playerToCreate.IsUnderwater) flags |= 0x02; //swimming
			if (playerToCreate.IsStealthed)  flags |= 0x10;
			if (playerToCreate.IsWireframe) flags |= 0x20;
			if (playerToCreate.PlayerClass.ID == (int)EPlayerClass.Vampiir) flags |= 0x40; //Vamp fly
			pak.WriteByte((byte)flags);
			pak.WriteByte(0x00); // new in 1.74

			pak.WritePascalString(GameServer.ServerRules.GetPlayerName(m_gameClient.Player, playerToCreate));
			pak.WritePascalString(GameServer.ServerRules.GetPlayerGuildName(m_gameClient.Player, playerToCreate));
			pak.WritePascalString(GameServer.ServerRules.GetPlayerLastName(m_gameClient.Player, playerToCreate));
            //RR 12 / 13
            pak.WritePascalString(GameServer.ServerRules.GetPlayerPrefixName(m_gameClient.Player, playerToCreate));
            pak.WritePascalString(GameServer.ServerRules.GetPlayerTitle(m_gameClient.Player, playerToCreate)); // new in 1.74, NewTitle
			pak.WriteByte(0x00); // new in 1.75
			SendTCP(pak);
		}

		SendObjectGuildID(playerToCreate, playerToCreate.Guild); //used for nearest friendly/enemy object buttons and name colors on PvP server
	}

	public override void SendLoginGranted(byte color)
	{
		using (GsTcpPacketOut pak = new GsTcpPacketOut(GetPacketCode(EServerPackets.LoginGranted)))
		{
			pak.WriteByte(0x01); //isSI
			pak.WriteByte(ParseVersion((int)m_gameClient.Version, true));
			pak.WriteByte(ParseVersion((int)m_gameClient.Version, false));
			//pak.WriteByte(build);
			pak.WriteByte(0x00);
			pak.WritePascalString(m_gameClient.Account.Name);
			pak.WritePascalString(GameServer.Instance.Configuration.ServerNameShort); //server name
			pak.WriteByte(0x0C); //Server ID
			pak.WriteByte(color);
			pak.WriteByte(0x00);
			pak.WriteByte(0x00); // new in 1.75
			SendTCP(pak);
		}
	}

    public override void SendLoginGranted()
    {
        //[Freya] Nidel: Can use realm button in character selection screen

        if (ServerProperties.Properties.ALLOW_ALL_REALMS || m_gameClient.Account.PrivLevel > (int)EPrivLevel.Player)
        {
            SendLoginGranted(1);
        }
        else
        {
            SendLoginGranted(GameServer.ServerRules.GetColorHandling(m_gameClient));
        }
    }
}