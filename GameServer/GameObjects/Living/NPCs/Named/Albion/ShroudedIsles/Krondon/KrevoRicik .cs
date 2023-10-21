using System;
using Core.AI.Brain;
using Core.Database;
using Core.Events;

namespace Core.GS;

#region Krevo Ricik
public class KrevoRicik : GameEpicBoss
{
	public KrevoRicik() : base() { }

	[ScriptLoadedEvent]
	public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
	{
		if (log.IsInfoEnabled)
			log.Info("Krevo Ricik Initializing...");
	}
	public override int GetResist(EDamageType damageType)
	{
		switch (damageType)
		{
			case EDamageType.Slash: return 20;// dmg reduction for melee dmg
			case EDamageType.Crush: return 20;// dmg reduction for melee dmg
			case EDamageType.Thrust: return 20;// dmg reduction for melee dmg
			default: return 30;// dmg reduction for rest resists
		}
	}
	public override double AttackDamage(DbInventoryItem weapon)
	{
		return base.AttackDamage(weapon) * Strength / 100;
	}
	public override int AttackRange
	{
		get { return 350; }
		set { }
	}
	public override bool HasAbility(string keyName)
	{
		if (IsAlive && keyName == GS.Abilities.CCImmunity)
			return true;

		return base.HasAbility(keyName);
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
		get { return 30000; }
	}
	public override bool AddToWorld()
	{
		Model = 919;
		Level = (byte)(Util.Random(72, 75));
		Name = "Krevo Ricik";
		Size = 120;

		Strength = 280;
		Dexterity = 150;
		Constitution = 100;
		Quickness = 80;
		Piety = 200;
		Intelligence = 200;
		Charisma = 200;
		Empathy = 400;

		MaxSpeedBase = 250;
		MaxDistance = 3500;
		TetherRange = 3800;
		RespawnInterval = ServerProperties.Properties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

		Faction = FactionMgr.GetFactionByID(8);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(8));

		KrevoRicikBrain sbrain = new KrevoRicikBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = false;//load from database
		SaveIntoDatabase();
		base.AddToWorld();
		return true;
	}
    public override void Die(GameObject killer)
    {
		foreach (GameNpc add in GetNPCsInRadius(4000))
		{
			if (add == null) continue;
			if (add.IsAlive && add.Brain is KrevoAddBrain)
				add.Die(this);
		}
		base.Die(killer);
    }
}
#endregion Krevo Ricik

#region Krevo adds
public class KrevolAdd : GameEpicNPC
{
	public KrevolAdd() : base() { }

	public override int GetResist(EDamageType damageType)
	{
		switch (damageType)
		{
			case EDamageType.Slash: return 30;// dmg reduction for melee dmg
			case EDamageType.Crush: return 30;// dmg reduction for melee dmg
			case EDamageType.Thrust: return 30;// dmg reduction for melee dmg
			default: return 30;// dmg reduction for rest resists
		}
	}
	public override double AttackDamage(DbInventoryItem weapon)
	{
		return base.AttackDamage(weapon) * Strength / 120;
	}
	public override int AttackRange
	{
		get { return 350; }
		set { }
	}
	public override double GetArmorAF(EArmorSlot slot)
	{
		return 400;
	}
	public override double GetArmorAbsorb(EArmorSlot slot)
	{
		// 85% ABS is cap.
		return 0.25;
	}
	public override int MaxHealth
	{
		get { return 4000; }
	}
	public override void Die(GameObject killer)
	{
		base.Die(killer);
	}
	public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
	public override short Strength { get => base.Strength; set => base.Strength = 120; }
	public override bool AddToWorld()
	{
		Model = 902;
		Level = (byte)(Util.Random(62, 64));
		Name = "forgoten ghost";
		Size = (byte)(Util.Random(50, 70));
		MaxSpeedBase = 250;
		RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

		Faction = FactionMgr.GetFactionByID(8);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(8));

		KrevoAddBrain sbrain = new KrevoAddBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = true;
		bool success = base.AddToWorld();
		if (success)
		{
			new EcsGameTimer(this, new EcsGameTimer.EcsTimerCallback(Explode),30000); //30 seconds until this will explode and deal heavy 
		}
		return success;
	}
	private int Explode(EcsGameTimer timer)
	{
		if (IsAlive)
		{
			SetGroundTarget(X, Y, Z);
			CastSpell(KrevoAddBomb, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			new EcsGameTimer(this, new EcsGameTimer.EcsTimerCallback(KillBomb), 500);
		}
		return 0;
	}
	private int KillBomb(EcsGameTimer timer)
	{
		if (IsAlive)
			Die(this);
		return 0;
	}
	private Spell m_KrevoAddBomb;
	private Spell KrevoAddBomb
	{
		get
		{
			if (m_KrevoAddBomb == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.RecastDelay = 0;
				spell.ClientEffect = 6159;
				spell.Icon = 6159;
				spell.TooltipId = 6159;
				spell.Damage = 800;
				spell.Name = "Dark Explosion";
				spell.Range = 1500;
				spell.Radius = 700;
				spell.SpellID = 11890;
				spell.Target = ESpellTarget.AREA.ToString();
				spell.Type = ESpellType.DirectDamageNoVariance.ToString();
				spell.DamageType = (int)EDamageType.Matter;
				m_KrevoAddBomb = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_KrevoAddBomb);
			}
			return m_KrevoAddBomb;
		}
	}
}
#endregion Krevo adds