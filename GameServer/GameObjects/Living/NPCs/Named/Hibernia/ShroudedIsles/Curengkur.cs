using System;
using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS;

namespace Core.GS;

#region Curengkur
public class Curengkur : GameEpicBoss
{
	public Curengkur() : base() { }

	[ScriptLoadedEvent]
	public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
	{
		if (log.IsInfoEnabled)
			log.Info("Curengkur Initializing...");
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
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60159530);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;
		SpawnNest();

		Faction = FactionMgr.GetFactionByID(69);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(69));
		RespawnInterval = ServerProperties.Properties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
		CurengkurBrain sbrain = new CurengkurBrain();
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
			if (npc != null && npc.IsAlive && npc.Brain is CurengkurNestBrain)
				npc.RemoveFromWorld();
		}
		base.Die(killer);
    }
    private void SpawnNest()
    {
		foreach (GameNpc npc in GetNPCsInRadius(8000))
		{
			if (npc.Brain is CurengkurNestBrain)
				return;
		}
		CurengkurNest nest = new CurengkurNest();
		nest.X = X;
		nest.Y = Y;
		nest.Z = Z;
		nest.Heading = Heading;
		nest.CurrentRegion = CurrentRegion;
		nest.AddToWorld();
	}
}
#endregion Curengkur

#region Curengkur Nest
public class CurengkurNest : GameNpc
{
	public CurengkurNest() : base()
	{
	}
	
	#region Stats
	public override short Charisma { get => base.Charisma; set => base.Charisma = 200; }
	public override short Piety { get => base.Piety; set => base.Piety = 200; }
	public override short Intelligence { get => base.Intelligence; set => base.Intelligence = 200; }
	public override short Empathy { get => base.Empathy; set => base.Empathy = 400; }
	public override short Dexterity { get => base.Dexterity; set => base.Dexterity = 200; }
	public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
	public override short Strength { get => base.Strength; set => base.Strength = 400; }
    #endregion
    public override void StartAttack(GameObject target)
    {
    }
    public override bool AddToWorld()
	{
		Model = 665;
		Name = "Curengkur's Nest Radiation";
		Level = 70;
		Size = (byte)Util.Random(50, 55);
		RespawnInterval = 5000;
		Flags = (ENpcFlags)42;
		MaxSpeedBase = 0;
		Faction = FactionMgr.GetFactionByID(69);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(69));

		LoadedFromScript = true;
		CurengkurNestBrain sbrain = new CurengkurNestBrain();
		SetOwnBrain(sbrain);
		base.AddToWorld();
		return true;
	}
}
#endregion Curengkur Nest