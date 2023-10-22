using System;
using Core.Database.Tables;
using Core.GS.AI.Brains;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.GameUtils;
using Core.GS.Server;
using Core.GS.Skills;
using Core.GS.Spells;
using Core.GS.World;

namespace Core.GS;

#region Orshom
public class Orshom : GameEpicBoss
{
	public Orshom() : base() { }

	[ScriptLoadedEvent]
	public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
	{
		if (log.IsInfoEnabled)
			log.Info("Orshom Brong Initializing...");
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
		if (IsAlive && keyName == AbilityConstants.CCImmunity)
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
		Level = 80;
		Name = "Orshom Brong";
		Size = 175;
		ParryChance = 50;

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
		RespawnInterval = ServerProperty.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

		Faction = FactionMgr.GetFactionByID(8);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(8));

		OrshomBrain sbrain = new OrshomBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = false;//load from database
		SaveIntoDatabase();
		base.AddToWorld();
		return true;
	}
}
#endregion Orshom

#region Fire Pit Mob
public class OrshomFire : GameEpicNpc
{
	public OrshomFire() : base() { }

	[ScriptLoadedEvent]
	public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
	{
		if (log.IsInfoEnabled)
			log.Info("Orshom's Fire Initializing...");
	}
	public override int GetResist(EDamageType damageType)
	{
		switch (damageType)
		{
			case EDamageType.Slash: return 20;// dmg reduction for melee dmg
			case EDamageType.Crush: return 20;// dmg reduction for melee dmg
			case EDamageType.Thrust: return 20;// dmg reduction for melee dmg
			default: return 20;// dmg reduction for rest resists
		}
	}
	public override void TakeDamage(GameObject source, EDamageType damageType, int damageAmount, int criticalAmount)
	{
		if (source is GamePlayer || source is GameSummonedPet)
		{
			if (damageType == EDamageType.Cold)//only cold dmg can hit it
			{
				base.TakeDamage(source, damageType, damageAmount, criticalAmount);
			}
			else //no dmg
			{
				GamePlayer truc;
				if (source is GamePlayer)
					truc = (source as GamePlayer);
				else
					truc = ((source as GameSummonedPet).Owner as GamePlayer);
				if (truc != null)
					truc.Out.SendMessage(this.Name + " is immune to your damage!", EChatType.CT_System,
						EChatLoc.CL_ChatWindow);
				base.TakeDamage(source, damageType, 0, 0);
				return;
			}
		}
	}
	public override void StartAttack(GameObject target)
    {
    }
    public override bool HasAbility(string keyName)
	{
		if (IsAlive && keyName == AbilityConstants.CCImmunity)
			return true;

		return base.HasAbility(keyName);
	}
	public override double GetArmorAF(EArmorSlot slot)
	{
		return 200;
	}
	public override double GetArmorAbsorb(EArmorSlot slot)
	{
		// 85% ABS is cap.
		return 0.15;
	}
	public override int MaxHealth
	{
		get { return 10000; }
	}
	public static int FireCount = 0;
    public override void Die(GameObject killer)
    {
		--FireCount;
        base.Die(killer);
    }
    public override bool AddToWorld()
	{
		Model = 665;
		Level = 80;
		Name = "Orshom's Fire";
		Size = 100;
		Dexterity = 200;
		Piety = 200;
		Intelligence = 200;
		Charisma = 200;
		Empathy = 200;
		++FireCount;

		MaxSpeedBase = 0;
		RespawnInterval = -1;

		Faction = FactionMgr.GetFactionByID(8);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(8));

		OrshomFireBrain sbrain = new OrshomFireBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = true;
		bool success = base.AddToWorld();
		if (success)
		{
			new EcsGameTimer(this, new EcsGameTimer.EcsTimerCallback(Show_Effect), 500);
		}
		return success;
	}

	protected int Show_Effect(EcsGameTimer timer)
	{
		if (IsAlive)
		{
			foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
				player.Out.SendSpellEffectAnimation(this, this, 7025, 0, false, 0x01);
			
			SetGroundTarget(X, Y, Z);
			CastSpell(Fire_aoe, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			return 3000;
		}

		return 0;
	}

	private Spell m_Fire_aoe;
	private Spell Fire_aoe
	{
		get
		{
			if (m_Fire_aoe == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.RecastDelay = 0;
				spell.ClientEffect = 7025;
				spell.Icon = 7025;
				spell.TooltipId = 7025;
				spell.Damage = 500;
				spell.Name = "Fire Burn";
				spell.Radius = 300; 
				spell.Range = 240;
				spell.SpellID = 11751;
				spell.Target = ESpellTarget.AREA.ToString();
				spell.Type = ESpellType.DirectDamageNoVariance.ToString();
				spell.Uninterruptible = true;
				spell.MoveCast = true;
				spell.DamageType = (int)EDamageType.Heat;
				m_Fire_aoe = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Fire_aoe);
			}

			return m_Fire_aoe;
		}
	}
}
#endregion Fire Pit Mob