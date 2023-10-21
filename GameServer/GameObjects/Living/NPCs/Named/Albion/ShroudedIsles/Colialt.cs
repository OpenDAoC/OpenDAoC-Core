using System;
using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS.AI.Brains;

namespace Core.GS;

#region Colialt
public class Colialt : GameEpicBoss
{
	public Colialt() : base() { }

	[ScriptLoadedEvent]
	public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
	{
		if (log.IsInfoEnabled)
			log.Info("Colialt Initializing...");
	}
	public override int GetResist(EDamageType damageType)
	{
		switch (damageType)
		{
			case EDamageType.Slash: return 30;// dmg reduction for melee dmg
			case EDamageType.Crush: return 30;// dmg reduction for melee dmg
			case EDamageType.Thrust: return 30;// dmg reduction for melee dmg
			default: return 40;// dmg reduction for rest resists
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
		get { return 40000; }
	}
	private bool CanSpawnZombies = false;
	public override bool AddToWorld()
	{
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(700000018);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;
		RespawnInterval = ServerProperties.Properties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

		CanSpawnZombies = false;
		if(CanSpawnZombies == false)
        {
			SpawnZombies();
			CanSpawnZombies = true;
		}
		Faction = FactionMgr.GetFactionByID(64);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(64));

		ColialtBrain sbrain = new ColialtBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = false;//load from database
		SaveIntoDatabase();
		base.AddToWorld();
		return true;
	}
    public override void Die(GameObject killer)
    {
		foreach (GameNpc npc in GetNPCsInRadius(8000))
		{
			if (npc != null && npc.IsAlive && npc.Brain is ColialtAddsBrain)
				npc.Die(this);
		}
		base.Die(killer);
    }
    private void SpawnZombies()
    {
		for(int i=0; i<Util.Random(10,15); i++)
        {
			ColialtAdds add = new ColialtAdds();
			add.X = X + Util.Random(-500, 500);
			add.Y = Y + Util.Random(-500, 500);
			add.Z = Z;
			add.Heading = Heading;
			add.CurrentRegion = CurrentRegion;
			add.AddToWorld();
		}
    }
	public override void DealDamage(AttackData ad)
	{
		if (ad != null && ad.AttackType == EAttackType.Spell && ad.Damage > 0 && ColialtBrain.ColialtPhase)
		{
			Health += ad.Damage;
		}
		base.DealDamage(ad);
	}
	public override void StartAttack(GameObject target)
    {
		if (ColialtBrain.ColialtPhase)
			return;
		else
			base.StartAttack(target);
    }
}
#endregion Colialt

#region Colialt adds
public class ColialtAdds : GameNpc
{
	public ColialtAdds() : base() { }
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
    public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
    public override short Strength { get => base.Strength; set => base.Strength = 150; }
    public override bool AddToWorld()
	{
		Model = 921;
		Size = (byte)Util.Random(65, 75);
		Name = "ancient zombie";
		RespawnInterval = -1;
		Level = (byte)Util.Random(61, 66);
		MaxSpeedBase = 225;
		Faction = FactionMgr.GetFactionByID(64);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(64));

		ColialtAddsBrain sbrain = new ColialtAddsBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = true;
		base.AddToWorld();
		return true;
	}
}
#endregion Colialt adds