using System;
using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS.AI.Brains;

namespace Core.GS;

#region Cronwort
public class Cronwort : GameEpicBoss
{
	public Cronwort() : base() { }

	[ScriptLoadedEvent]
	public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
	{
		if (log.IsInfoEnabled)
			log.Info("Cronwort Initializing...");
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
	#region Stats
	public override short Charisma { get => base.Charisma; set => base.Charisma = 200; }
	public override short Piety { get => base.Piety; set => base.Piety = 200; }
	public override short Intelligence { get => base.Intelligence; set => base.Intelligence = 200; }
	public override short Empathy { get => base.Empathy; set => base.Empathy = 400; }
	public override short Dexterity { get => base.Dexterity; set => base.Dexterity = 200; }
	public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
	public override short Strength { get => base.Strength; set => base.Strength = 300; }
	#endregion
	public override bool AddToWorld()
	{
		Name = "Cronwort";
		Model = 903;
		Size = 80;
		Level = 68;
		MaxDistance = 2500;
		TetherRange = 2600;
		SpawnAdds();

		RespawnInterval = ServerProperties.Properties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
		CronwortBrain sbrain = new CronwortBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = false;//load from database
		SaveIntoDatabase();
		base.AddToWorld();
		return true;
	}
	private void SpawnAdds()
    {
		for (int i = 0; i < 6; i++)
		{
			Breanwort add = new Breanwort();
			add.X = X + Util.Random(-200, 200);
			add.Y = Y + Util.Random(-200, 200);
			add.Z = Z;
			add.Heading = Heading;
			add.CurrentRegion = CurrentRegion;
			add.AddToWorld();
		}
	}
    public override void Die(GameObject killer)
    {
		foreach (GameNpc adds in GetNPCsInRadius(8000))
		{
			if (adds != null && adds.IsAlive && adds.Brain is BreanwortBrain)
				adds.RemoveFromWorld();
		}
		base.Die(killer);
    }
	public override void OnAttackEnemy(AttackData ad) //on enemy actions
	{
		if (Util.Chance(25))
		{
			if (ad != null && (ad.AttackResult == EAttackResult.HitUnstyled || ad.AttackResult == EAttackResult.HitStyle))
				CastSpell(CronworttDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
		}
		base.OnAttackEnemy(ad);
	}
	private Spell m_CronwortDD;
	public Spell CronworttDD
	{
		get
		{
			if (m_CronwortDD == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.Power = 0;
				spell.RecastDelay = 2;
				spell.ClientEffect = 5435;
				spell.Icon = 5435;
				spell.Damage = 400;
				spell.DamageType = (int)EDamageType.Energy;
				spell.Name = "Energy Shock";
				spell.Range = 500;
				spell.SpellID = 11904;
				spell.Target = "Enemy";
				spell.Type = ESpellType.DirectDamageNoVariance.ToString();
				m_CronwortDD = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_CronwortDD);
			}
			return m_CronwortDD;
		}
	}
}
#endregion Cronwort

#region Breanwort add
public class Breanwort : GameNpc
{
	public Breanwort() : base()
	{
	}
	public override int GetResist(EDamageType damageType)
	{
		switch (damageType)
		{
			case EDamageType.Slash: return 15;// dmg reduction for melee dmg
			case EDamageType.Crush: return 15;// dmg reduction for melee dmg
			case EDamageType.Thrust: return 15;// dmg reduction for melee dmg
			default: return 15;// dmg reduction for rest resists
		}
	}
	public override double AttackDamage(DbInventoryItem weapon)
	{
		return base.AttackDamage(weapon) * Strength / 100;
	}
	public override int MaxHealth
	{
		get { return 4000; }
	}
	public override double GetArmorAF(EArmorSlot slot)
	{
		return 200;
	}
	public override double GetArmorAbsorb(EArmorSlot slot)
	{
		// 85% ABS is cap.
		return 0.10;
	}
	public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
	public override short Strength { get => base.Strength; set => base.Strength = 150; }
	public override bool AddToWorld()
	{
		Model = 903;
		Name = "Breanwort";
		Level = (byte)Util.Random(61, 62);
		Size = (byte)Util.Random(25, 35);
		RespawnInterval = -1;
		RoamingRange = 200;
		LoadedFromScript = true;
		BreanwortBrain sbrain = new BreanwortBrain();
		
		SetOwnBrain(sbrain);
		base.AddToWorld();
		return true;
	}
}
#endregion Breanwort add