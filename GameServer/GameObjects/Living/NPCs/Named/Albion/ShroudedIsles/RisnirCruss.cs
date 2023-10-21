using System;
using Core.AI.Brain;
using Core.Database;
using Core.Events;

namespace Core.GS;

public class RisnirCruss : GameEpicBoss
{
	public RisnirCruss() : base() { }

	[ScriptLoadedEvent]
	public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
	{
		if (log.IsInfoEnabled)
			log.Info("Ris'nir Cruss Initializing...");
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
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60165335);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;
		RespawnInterval = ServerProperties.Properties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

		Flags = ENpcFlags.FLYING;
		Faction = FactionMgr.GetFactionByID(20);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(20));

		RisnirCrussBrain sbrain = new RisnirCrussBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = false;//load from database
		SaveIntoDatabase();
		base.AddToWorld();
		return true;
	}
    public override void EnemyKilled(GameLiving enemy)
    {
		if (enemy != null && enemy is GamePlayer)
        {
			GameNpc add = new GameNpc();
			add.Name = "Apparition of " + enemy.Name;
			add.Model = 902;
			add.Size = (byte)Util.Random(45, 55);
			add.Level = (byte)Util.Random(55, 59);
			add.Strength = 150;
			add.Quickness = 80;
			add.MeleeDamageType = EDamageType.Crush;
			add.MaxSpeedBase = 225;
			add.PackageID = "RisnirCrussAdd";
			add.RespawnInterval = -1;
			add.X = enemy.X;
			add.Y = enemy.Y;
			add.Z = enemy.Z;
			add.CurrentRegion = CurrentRegion;
			add.Heading = Heading;
			add.Faction = FactionMgr.GetFactionByID(20);
			add.Faction.AddFriendFaction(FactionMgr.GetFactionByID(20));
			StandardMobBrain brain = new StandardMobBrain();
			add.SetOwnBrain(brain);
			brain.AggroRange = 600;
			brain.AggroLevel = 100;
			add.AddToWorld();
		}
        base.EnemyKilled(enemy);
    }
    public override void Die(GameObject killer)
    {
		foreach (GameNpc npc in GetNPCsInRadius(5000))
		{
			if (npc != null && npc.IsAlive && npc.RespawnInterval == -1 && npc.PackageID == "RisnirCrussAdd")
				npc.Die(this);
		}
		base.Die(killer);
    }
}