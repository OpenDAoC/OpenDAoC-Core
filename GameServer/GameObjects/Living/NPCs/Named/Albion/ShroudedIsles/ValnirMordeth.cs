using System;
using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS.AI.Brains;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.GameUtils;
using Core.GS.Server;

namespace Core.GS;

#region Valnir Mordeth
public class ValnirMordeth : GameEpicBoss
{
	public ValnirMordeth() : base() { }

	[ScriptLoadedEvent]
	public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
	{
		if (log.IsInfoEnabled)
			log.Info("Valnir Mordeth Initializing...");
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
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(700000014);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;
		RespawnInterval = ServerProperty.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

		Faction = FactionMgr.GetFactionByID(64);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(64));
		canSpawnAdds = false;
		if(canSpawnAdds==false)
        {
			SpawnAdds();
			canSpawnAdds = true;
        }
		ValnirMordethBrain sbrain = new ValnirMordethBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = false;//load from database
		SaveIntoDatabase();
		base.AddToWorld();
		return true;
	}
    public override void Die(GameObject killer)
    {
		foreach (GameNpc npc in GetNPCsInRadius(5000))
		{
			if (npc != null && npc.IsAlive && npc.Brain is ValnirMordethAddBrain)
				npc.Die(this);
		}
		base.Die(killer);
    }
    private bool canSpawnAdds = false;
	private void SpawnAdds()
    {
		for (int i = 0; i < Util.Random(2, 3); i++)
		{
			ValnirMordethAdd add = new ValnirMordethAdd();
			add.X = X + Util.Random(-200, 200);
			add.Y = Y + Util.Random(-200, 200);
			add.Z = Z;
			add.Heading = Heading;
			add.CurrentRegion = CurrentRegion;
			add.PackageID = "MordethBaf";
			add.AddToWorld();
		}
	}
	public override void OnAttackEnemy(AttackData ad)
	{
		if (Util.Chance(30))
		{
			if (ad != null && (ad.AttackResult == EAttackResult.HitUnstyled || ad.AttackResult == EAttackResult.HitStyle))
				CastSpell(ValnirLifeDrain, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
		}
		base.OnAttackEnemy(ad);
	}
	public override void DealDamage(AttackData ad)
	{
		if (ad != null && ad.AttackType == EAttackType.Spell && ad.Damage > 0)
			Health += ad.Damage;
		base.DealDamage(ad);
	}
	private Spell m_ValnirLifeDrain;
	private Spell ValnirLifeDrain
	{
		get
		{
			if (m_ValnirLifeDrain == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.RecastDelay = 2;
				spell.ClientEffect = 14352;
				spell.Icon = 14352;
				spell.TooltipId = 14352;
				spell.Damage = 500;
				spell.Name = "Lifedrain";
				spell.Range = 400;
				spell.SpellID = 11903;
				spell.Target = ESpellTarget.ENEMY.ToString();
				spell.Type = ESpellType.DirectDamageNoVariance.ToString();
				spell.Uninterruptible = true;
				spell.MoveCast = true;
				spell.DamageType = (int)EDamageType.Body;
				m_ValnirLifeDrain = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_ValnirLifeDrain);
			}
			return m_ValnirLifeDrain;
		}
	}
}
#endregion Valnir Mordeth

#region Mordeth adds
public class ValnirMordethAdd : GameNpc
{
	public ValnirMordethAdd() : base() { }

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
	public override double AttackDamage(DbInventoryItem weapon)
	{
		return base.AttackDamage(weapon) * Strength / 100;
	}
	public override int AttackRange
	{
		get { return 350; }
		set { }
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
	public override int MaxHealth
	{
		get { return 5000; }
	}
    public override short Dexterity { get => base.Dexterity; set => base.Dexterity = 200; }
    public override short Empathy { get => base.Empathy; set => base.Empathy = 200; }
    public override short Charisma { get => base.Charisma; set => base.Charisma = 200; }
    public override short Piety { get => base.Piety; set => base.Piety = 200; }
    public override short Intelligence { get => base.Intelligence; set => base.Intelligence = 200; }
    public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
    public override short Strength { get => base.Strength; set => base.Strength = 120; }
	public static int EssenceGhoulCount = 0;
    public override bool AddToWorld()
	{
		Model = 921;
		Name = "Essence Ghoul";
		Level = (byte)Util.Random(62, 66);
		Size = (byte)Util.Random(55, 65);
		MaxSpeedBase = 225;
		RespawnInterval = -1;
		++EssenceGhoulCount;

		Faction = FactionMgr.GetFactionByID(64);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(64));

		ValnirMordethAddBrain sbrain = new ValnirMordethAddBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = true;
		base.AddToWorld();
		return true;
	}
    public override long ExperienceValue => 0;
    public override void DropLoot(GameObject killer)
    {
    }
    public override void Die(GameObject killer)
    {
		--EssenceGhoulCount;
        base.Die(killer);
    }
    public override void DealDamage(AttackData ad)
	{
		if (ad != null && ad.AttackType == EAttackType.Spell && ad.Damage > 0)
		{
			foreach(GameNpc boss in GetNPCsInRadius(5000))
            {
				if (boss != null && boss.IsAlive && boss.Brain is ValnirMordethBrain)
					boss.Health += ad.Damage;//heal boss
            }
			Health += ad.Damage;//heal self
		}
		base.DealDamage(ad);
	}
}
#endregion Mordeth adds