using System;
using Core.Database.Tables;
using Core.GS.AI.Brains;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.GameUtils;
using Core.GS.Server;
using Core.GS.Skills;
using Core.GS.Spells;
using Core.GS.World;

namespace Core.GS;

#region Myrddraxis
public class Myrddraxis : GameEpicBoss
{
	protected String[] m_deathAnnounce;
	public Myrddraxis() : base() 
	{
		m_deathAnnounce = new String[] { "The earth lurches beneath your feet as {0} staggers and topples to the ground.",
			"A glowing light begins to form on the mound that served as {0}'s lair." };
	}
    #region Custom methods
    public void BroadcastMessage(String message)
	{
		foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
		{
			player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_ChatWindow);
		}
	}
	/// <summary>
	/// Post a message in the server news and award a dragon kill point for
	/// every XP gainer in the raid.
	/// </summary>
	/// <param name="killer">The living that got the killing blow.</param>
	protected void ReportNews(GameObject killer)
	{
		int numPlayers = GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE).Count;
		String message = String.Format("{0} has been slain by a force of {1} warriors!", Name, numPlayers);
		NewsMgr.CreateNews(message, killer.Realm, ENewsType.PvE, true);

		if (ServerProperty.GUILD_MERIT_ON_DRAGON_KILL > 0)
		{
			foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
			{
				if (player.IsEligibleToGiveMeritPoints)
				{
					GuildEventHandler.MeritForNPCKilled(player, this, ServerProperty.GUILD_MERIT_ON_DRAGON_KILL);
				}
			}
		}
	}
	/// <summary>
	/// Award dragon kill point for each XP gainer.
	/// </summary>
	/// <returns>The number of people involved in the kill.</returns>
	protected int AwardDragonKillPoint()
	{
		int count = 0;
		foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
		{
			player.KillsDragon++;
			count++;
		}
		return count;
	}
	public override void Die(GameObject killer)
	{
		foreach (GameNpc heads in WorldMgr.GetNPCsFromRegion(CurrentRegionID))
		{
			if (heads != null)
			{
				if (heads.IsAlive && (heads.Brain is MyrddraxisSecondHeadBrain || heads.Brain is MyrddraxisThirdHeadBrain || heads.Brain is MyrddraxisFourthHeadBrain || heads.Brain is MyrddraxisFifthHeadBrain))
					heads.Die(heads);
			}
		}
		// debug
		if (killer == null)
			log.Error("Dragon Killed: killer is null!");
		else
			log.Debug("Dragon Killed: killer is " + killer.Name + ", attackers:");
		bool canReportNews = true;
		// due to issues with attackers the following code will send a notify to all in area in order to force quest credit
		foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
		{
			player.Notify(GameLivingEvent.EnemyKilled, killer, new EnemyKilledEventArgs(this));
			if (canReportNews && GameServer.ServerRules.CanGenerateNews(player) == false)
			{
				if (player.Client.Account.PrivLevel == (int)EPrivLevel.Player)
					canReportNews = false;
			}
		}

		AwardDragonKillPoint();

		base.Die(killer);
		foreach (String message in m_deathAnnounce)
		{
			BroadcastMessage(String.Format(message, Name));
		}
		if (canReportNews)
		{
			ReportNews(killer);
		}
	}
	#endregion
	[ScriptLoadedEvent]
	public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
	{
		if (log.IsInfoEnabled)
			log.Info("Myrddraxis Initializing...");
	}
	public override int GetResist(EDamageType damageType)
	{
		switch (damageType)
		{
			case EDamageType.Slash: return 40; // dmg reduction for melee dmg
			case EDamageType.Crush: return 40; // dmg reduction for melee dmg
			case EDamageType.Thrust: return 40; // dmg reduction for melee dmg
			default: return 70; // dmg reduction for rest resists
		}
	}
	public override double GetArmorAF(EArmorSlot slot)
	{
		return 350;
	}
	public override double GetArmorAbsorb(EArmorSlot slot)
	{
		// 85% ABS is cap.
		return 0.20;
	}
	public override int MaxHealth
	{
		get { return 100000; }
	}
	public override int AttackRange
	{
		get { return 550; }
		set { }
	}
	public override bool HasAbility(string keyName)
	{
		if (IsAlive && keyName == AbilityConstants.CCImmunity)
			return true;

		return base.HasAbility(keyName);
	}
    public override void OnAttackEnemy(AttackData ad)
    {
		if(ad != null && (ad.AttackResult == EAttackResult.HitUnstyled || ad.AttackResult == EAttackResult.HitStyle))
        {
			if(Util.Chance(25))
            {
				switch(Util.Random(1,2))
                {
					case 1: CastSpell(HydraDisease, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells)); break;
					case 2: CastSpell(Hydra_Haste_Debuff, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells)); break;
				}					
			}
        }
        base.OnAttackEnemy(ad);
    }
	public override bool AddToWorld()
	{
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60164337);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;

		RespawnInterval = ServerProperty.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
		MaxSpeedBase = 0;
		X = 32302;
		Y = 32221;
		Z = 15635;
		Heading = 492;

		Faction = FactionMgr.GetFactionByID(105);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(82));

		CanSpawnHeads = false;
		if(CanSpawnHeads == false)
        {
			SpawnHeads();
			CanSpawnHeads = true;
        }

		MyrddraxisBrain sbrain = new MyrddraxisBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = false;//load from database
		SaveIntoDatabase();
		base.AddToWorld();
		return true;
	}
    #region Spawn Heads
    public static bool CanSpawnHeads = false;
	public void SpawnHeads()
    {
		//Second Head
		MyrddraxisSecondHead Add1 = new MyrddraxisSecondHead();
		Add1.X = 32384;
		Add1.Y = 31942;
		Add1.Z = 15931;
		Add1.CurrentRegion = CurrentRegion;
		Add1.Heading = 455;
		Add1.Flags = ENpcFlags.FLYING;
		Add1.RespawnInterval = -1;
		Add1.AddToWorld();

		//Third Head
		MyrddraxisThirdHead Add2 = new MyrddraxisThirdHead();
		Add2.X = 32187;
		Add2.Y = 32205;
		Add2.Z = 15961;
		Add2.CurrentRegion = CurrentRegion;
		Add2.Heading = 4095;
		Add2.Flags = ENpcFlags.FLYING;
		Add2.RespawnInterval = -1;
		Add2.AddToWorld();

		//Fourth Head
		MyrddraxisFourthHead Add3 = new MyrddraxisFourthHead();
		Add3.X = 32371;
		Add3.Y = 32351;
		Add3.Z = 15936;
		Add3.CurrentRegion = CurrentRegion;
		Add3.Heading = 971;
		Add3.Flags = ENpcFlags.FLYING;
		Add3.RespawnInterval = -1;
		Add3.AddToWorld();

		//Fifth Head
		MyrddraxisFifthHead Add4 = new MyrddraxisFifthHead();
		Add4.X = 32576;
		Add4.Y = 32133;
		Add4.Z = 15936;
		Add4.CurrentRegion = CurrentRegion;
		Add4.Heading = 4028;
		Add4.Flags = ENpcFlags.FLYING;
		Add4.RespawnInterval = -1;
		Add4.AddToWorld();
	}
	#endregion
	#region spells
	private Spell m_HydraDisease;
	private Spell HydraDisease
	{
		get
		{
			if (m_HydraDisease == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.RecastDelay = Util.Random(25, 35);
				spell.ClientEffect = 4375;
				spell.Icon = 4375;
				spell.Name = "Disease";
				spell.Message1 = "You are diseased!";
				spell.Message2 = "{0} is diseased!";
				spell.Message3 = "You look healthy.";
				spell.Message4 = "{0} looks healthy again.";
				spell.TooltipId = 4375;
				spell.Range = 0;
				spell.Radius = 800;
				spell.Duration = 120;
				spell.SpellID = 11843;
				spell.Target = "Enemy";
				spell.Type = "Disease";
				spell.Uninterruptible = true;
				spell.MoveCast = true;
				spell.DamageType = (int)EDamageType.Energy; //Energy DMG Type
				m_HydraDisease = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_HydraDisease);
			}
			return m_HydraDisease;
		}
	}
	private Spell m_Hydra_Haste_Debuff;
	private Spell Hydra_Haste_Debuff
	{
		get
		{
			if (m_Hydra_Haste_Debuff == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.RecastDelay = 60;
				spell.Duration = 60;
				spell.ClientEffect = 5427;
				spell.Icon = 5427;
				spell.Name = "Combat Speed Debuff";
				spell.TooltipId = 5427;
				spell.Range = 0;
				spell.Value = 24;
				spell.Radius = 800;
				spell.SpellID = 11844;
				spell.Target = "Enemy";
				spell.Type = ESpellType.CombatSpeedDebuff.ToString();
				spell.Uninterruptible = true;
				spell.MoveCast = true;
				m_Hydra_Haste_Debuff = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Hydra_Haste_Debuff);
			}
			return m_Hydra_Haste_Debuff;
		}
	}
	#endregion
}
#endregion Myrddraxis

#region 2nd Head of Myrddraxis
public class MyrddraxisSecondHead : GameNpc
{
	public MyrddraxisSecondHead() : base() { }

	[ScriptLoadedEvent]
	public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
	{
		if (log.IsInfoEnabled)
			log.Info("Second Head of Myrddraxis Initializing...");
	}
	public override int GetResist(EDamageType damageType)
	{
		switch (damageType)
		{
			case EDamageType.Slash: return 40;// dmg reduction for melee dmg
			case EDamageType.Crush: return 40;// dmg reduction for melee dmg
			case EDamageType.Thrust: return 40;// dmg reduction for melee dmg
			default: return 70;// dmg reduction for rest resists
		}
	}
	public override int AttackRange
	{
		get { return 350; }
		set { }
	}
	public override bool HasAbility(string keyName)
	{
		if (IsAlive && keyName == AbilityConstants.CCImmunity)
			return true;

		return base.HasAbility(keyName);
	}
	public override double GetArmorAF(EArmorSlot slot)
	{
		return 300;
	}
	public override double GetArmorAbsorb(EArmorSlot slot)
	{
		// 85% ABS is cap.
		return 0.25;
	}
	public override int MaxHealth
	{
		get { return 40000; }
	}
	public static int SecondHeadCount = 0;
	public override void Die(GameObject killer)
	{
		--SecondHeadCount;
		base.Die(killer);
	}
    public override void DealDamage(AttackData ad)
    {
		if(ad != null)
        {
			foreach(GameNpc hydra in GetNPCsInRadius(2000))
            {
				if(hydra != null)
                {
					if(hydra.IsAlive && hydra.Brain is MyrddraxisBrain)
                    {
						hydra.Health += ad.Damage / 2;//dmg heals hydra
                    }
                }
            }
			foreach (GameNpc heads in GetNPCsInRadius(2000))
			{
				if (heads != null)
				{
					if (heads.IsAlive && (heads.Brain is MyrddraxisThirdHeadBrain || heads.Brain is MyrddraxisFourthHeadBrain || heads.Brain is MyrddraxisFifthHeadBrain))
					{
						heads.Health += ad.Damage / 10;//dmg heals heads but not the one that is being attacked
					}
				}
			}
		}
        base.DealDamage(ad);
    }
    public override bool AddToWorld()
	{
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60165727);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;

		RespawnInterval = -1;
		MaxSpeedBase = 0;
		++SecondHeadCount;
		Faction = FactionMgr.GetFactionByID(105);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(82));

		MyrddraxisSecondHeadBrain sbrain = new MyrddraxisSecondHeadBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = true;
		base.AddToWorld();
		return true;
	}
}
#endregion 2nd Head of Myrddraxis

#region 3th Head of Myrddraxis
public class MyrddraxisThirdHead : GameNpc
{
	public MyrddraxisThirdHead() : base() { }

	[ScriptLoadedEvent]
	public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
	{
		if (log.IsInfoEnabled)
			log.Info("Third Head of Myrddraxis Initializing...");
	}
	public override int GetResist(EDamageType damageType)
	{
		switch (damageType)
		{
			case EDamageType.Slash: return 40;// dmg reduction for melee dmg
			case EDamageType.Crush: return 40;// dmg reduction for melee dmg
			case EDamageType.Thrust: return 40;// dmg reduction for melee dmg
			default: return 70;// dmg reduction for rest resists
		}
	}
	public override int AttackRange
	{
		get { return 350; }
		set { }
	}
	public override bool HasAbility(string keyName)
	{
		if (IsAlive && keyName == AbilityConstants.CCImmunity)
			return true;

		return base.HasAbility(keyName);
	}
	public override double GetArmorAF(EArmorSlot slot)
	{
		return 300;
	}
	public override double GetArmorAbsorb(EArmorSlot slot)
	{
		// 85% ABS is cap.
		return 0.25;
	}
	public override int MaxHealth
	{
		get { return 40000; }
	}
	public static int ThirdHeadCount = 0;
	public override void Die(GameObject killer)
	{
		--ThirdHeadCount;
		base.Die(killer);
	}
	public override void DealDamage(AttackData ad)
	{
		if (ad != null)
		{
			foreach (GameNpc hydra in GetNPCsInRadius(2000))
			{
				if (hydra != null)
				{
					if (hydra.IsAlive && hydra.Brain is MyrddraxisBrain)
					{
						hydra.Health += ad.Damage / 2;//dmg heals hydra
					}
				}
			}
			foreach (GameNpc heads in GetNPCsInRadius(2000))
			{
				if (heads != null)
				{
					if (heads.IsAlive && (heads.Brain is MyrddraxisSecondHeadBrain || heads.Brain is MyrddraxisFourthHeadBrain || heads.Brain is MyrddraxisFifthHeadBrain))
					{
						heads.Health += ad.Damage / 10;//dmg heals heads but not the one that is being attacked
					}
				}
			}
		}
		base.DealDamage(ad);
	}
	public override bool AddToWorld()
	{
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60167005);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;

		RespawnInterval = -1;
		MaxSpeedBase = 0;
		++ThirdHeadCount;

		Faction = FactionMgr.GetFactionByID(105);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(82));

		MyrddraxisThirdHeadBrain sbrain = new MyrddraxisThirdHeadBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = true;
		base.AddToWorld();
		return true;
	}
}
#endregion 3th Head of Myrddraxis

#region 4th Head of Myrddraxis
public class MyrddraxisFourthHead : GameNpc
{
	public MyrddraxisFourthHead() : base() { }

	[ScriptLoadedEvent]
	public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
	{
		if (log.IsInfoEnabled)
			log.Info("Fourth Head of Myrddraxis Initializing...");
	}
	public override int GetResist(EDamageType damageType)
	{
		switch (damageType)
		{
			case EDamageType.Slash: return 40;// dmg reduction for melee dmg
			case EDamageType.Crush: return 40;// dmg reduction for melee dmg
			case EDamageType.Thrust: return 40;// dmg reduction for melee dmg
			default: return 70;// dmg reduction for rest resists
		}
	}
	public override int AttackRange
	{
		get { return 350; }
		set { }
	}
	public override bool HasAbility(string keyName)
	{
		if (IsAlive && keyName == AbilityConstants.CCImmunity)
			return true;

		return base.HasAbility(keyName);
	}
	public override double GetArmorAF(EArmorSlot slot)
	{
		return 300;
	}
	public override double GetArmorAbsorb(EArmorSlot slot)
	{
		// 85% ABS is cap.
		return 0.25;
	}
	public override int MaxHealth
	{
		get { return 40000; }
	}
	public static int FourthHeadCount = 0;
	public override void Die(GameObject killer)
	{
		--FourthHeadCount;
		base.Die(killer);
	}
	public override void DealDamage(AttackData ad)
	{
		if (ad != null)
		{
			foreach (GameNpc hydra in GetNPCsInRadius(2000))
			{
				if (hydra != null)
				{
					if (hydra.IsAlive && hydra.Brain is MyrddraxisBrain)
					{
						hydra.Health += ad.Damage / 2;//dmg heals hydra
					}
				}
			}
			foreach (GameNpc heads in GetNPCsInRadius(2000))
			{
				if (heads != null)
				{
					if (heads.IsAlive && (heads.Brain is MyrddraxisSecondHeadBrain || heads.Brain is MyrddraxisThirdHeadBrain || heads.Brain is MyrddraxisFifthHeadBrain))
					{
						heads.Health += ad.Damage / 10;//dmg heals heads but not the one that is being attacked
					}
				}
			}
		}
		base.DealDamage(ad);
	}
	public override bool AddToWorld()
	{
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60161055);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;

		RespawnInterval = -1;
		MaxSpeedBase = 0;
		++FourthHeadCount;

		Faction = FactionMgr.GetFactionByID(105);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(82));

		MyrddraxisFourthHeadBrain sbrain = new MyrddraxisFourthHeadBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = true;
		base.AddToWorld();
		return true;
	}
}
#endregion 4th Head of Myrddraxis

#region 5th Head of Myrddraxis
public class MyrddraxisFifthHead : GameNpc
{
	public MyrddraxisFifthHead() : base() { }

	[ScriptLoadedEvent]
	public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
	{
		if (log.IsInfoEnabled)
			log.Info("Fifth Head of Myrddraxis Initializing...");
	}
	public override int GetResist(EDamageType damageType)
	{
		switch (damageType)
		{
			case EDamageType.Slash: return 40;// dmg reduction for melee dmg
			case EDamageType.Crush: return 40;// dmg reduction for melee dmg
			case EDamageType.Thrust: return 40;// dmg reduction for melee dmg
			default: return 70;// dmg reduction for rest resists
		}
	}
	public override int AttackRange
	{
		get { return 350; }
		set { }
	}
	public override bool HasAbility(string keyName)
	{
		if (IsAlive && keyName == AbilityConstants.CCImmunity)
			return true;

		return base.HasAbility(keyName);
	}
	public override double GetArmorAF(EArmorSlot slot)
	{
		return 300;
	}
	public override double GetArmorAbsorb(EArmorSlot slot)
	{
		// 85% ABS is cap.
		return 0.25;
	}
	public override int MaxHealth
	{
		get { return 40000; }
	}
	public static int FifthHeadCount = 0;
	public override void Die(GameObject killer)
	{
		--FifthHeadCount;
		base.Die(killer);
	}
	public override void DealDamage(AttackData ad)
	{
		if (ad != null)
		{
			foreach (GameNpc hydra in GetNPCsInRadius(2000))
			{
				if (hydra != null)
				{
					if (hydra.IsAlive && hydra.Brain is MyrddraxisBrain)
					{
						hydra.Health += ad.Damage / 2;//dmg heals hydra
					}
				}
			}
			foreach (GameNpc heads in GetNPCsInRadius(2000))
			{
				if (heads != null)
				{
					if (heads.IsAlive && (heads.Brain is MyrddraxisSecondHeadBrain || heads.Brain is MyrddraxisThirdHeadBrain || heads.Brain is MyrddraxisFourthHeadBrain))
					{
						heads.Health += ad.Damage / 10;//dmg heals heads but not the one that is being attacked
					}
				}
			}
		}
		base.DealDamage(ad);
	}
	public override bool AddToWorld()
	{
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60160835);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;
		RespawnInterval = -1;
		MaxSpeedBase = 0;
		++FifthHeadCount;

		Faction = FactionMgr.GetFactionByID(105);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(82));

		MyrddraxisFifthHeadBrain sbrain = new MyrddraxisFifthHeadBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = true;
		base.AddToWorld();
		return true;
	}
}
#endregion 5th Head of Myrddraxis