using System;
using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS.AI.Brains;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.Events;

namespace Core.GS;

#region Dra'argus the Mighty
public class DraargusMighty : GameEpicBoss
{
	public DraargusMighty() : base() { }

	[ScriptLoadedEvent]
	public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
	{
		if (log.IsInfoEnabled)
			log.Info("Dra'argus the Mighty Initializing...");
	}
	public override int GetResist(EDamageType damageType)
	{
		switch (damageType)
		{
			case EDamageType.Slash: return 20; // dmg reduction for melee dmg
			case EDamageType.Crush: return 20; // dmg reduction for melee dmg
			case EDamageType.Thrust: return 20; // dmg reduction for melee dmg
			default: return 30; // dmg reduction for rest resists
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
		get { return 30000; }
	}
	public override double AttackDamage(DbInventoryItem weapon)
	{
		return base.AttackDamage(weapon) * Strength / 100;
	}
	public override void StartAttack(GameObject target)
	{
		if (DraugynSphere.SphereCount > 0)
			return;
		else
			base.StartAttack(target);
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
		if (DraugynSphere.SphereCount > 0 && IsAlive && keyName == GS.Abilities.DamageImmunity)
			return true;
		return base.HasAbility(keyName);
	}
	public override bool AddToWorld()
	{
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60160055);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;
		RespawnInterval = ServerProperties.Properties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

		Faction = FactionMgr.GetFactionByID(9);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(9));
		CreateSphere();

		DraargusMightyBrain sbrain = new DraargusMightyBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = false;//load from database
		SaveIntoDatabase();
		base.AddToWorld();
		return true;
	}
	public void CreateSphere()
    {
		if (DraugynSphere.SphereCount == 0)
		{
			DraugynSphere Add = new DraugynSphere();
			Add.X = 26766;
			Add.Y = 37124;
			Add.Z = 9027;
			Add.CurrentRegion = CurrentRegion;
			Add.Heading = 966;
			Add.AddToWorld();
		}
	}
}
#endregion Dra'argus the Mighty

#region Draugyn Sphere
public class DraugynSphere : GameEpicNPC
{
	public DraugynSphere() : base() { }

	[ScriptLoadedEvent]
	public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
	{
		if (log.IsInfoEnabled)
			log.Info("Drau'gyn Sphere Initializing...");
	}
	public override int GetResist(EDamageType damageType)
	{
		switch (damageType)
		{
			case EDamageType.Slash: return 20;// dmg reduction for melee dmg
			case EDamageType.Crush: return 20;// dmg reduction for melee dmg
			case EDamageType.Thrust: return 20;// dmg reduction for melee dmg
			default: return 50;// dmg reduction for rest resists
		}
	}
	public override bool HasAbility(string keyName)
	{
		if (IsAlive && keyName == GS.Abilities.CCImmunity)
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
		return 0.15;
	}
	public override int MaxHealth
	{
		get { return 10000; }
	}
	public static int SphereCount = 0;
	public static bool IsSphereDead = false;
    public override void Die(GameObject killer)
    {
		--SphereCount;
		IsSphereDead = true;
        base.Die(killer);
    }
    public override bool AddToWorld()
	{
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60160133);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;
		IsSphereDead = false;

		Faction = FactionMgr.GetFactionByID(9);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(9));
		MaxSpeedBase = 0;
		++SphereCount;
		RespawnInterval = -1;

		DraugynSphereBrain sbrain = new DraugynSphereBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = true;
		bool success = base.AddToWorld();
		if(success)
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
				player.Out.SendSpellEffectAnimation(this, this, 55, 0, false, 0x01);

			return 3000;
		}

		return 0;
	}
	
	public override void StartAttack(GameObject target)
    {
    }
}
#endregion Draugyn Sphere