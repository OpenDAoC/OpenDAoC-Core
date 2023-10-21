using System;
using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS.AI.Brains;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.GameUtils;

namespace Core.GS;

public class SpiritOfLordEmthoro : GameEpicBoss
{
	public SpiritOfLordEmthoro() : base() { }

	[ScriptLoadedEvent]
	public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
	{
		if (log.IsInfoEnabled)
			log.Info("Spirit of Lord Emthoro Initializing...");
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
	public override void TakeDamage(GameObject source, EDamageType damageType, int damageAmount, int criticalAmount)
	{
		if (source is GamePlayer || source is GameSummonedPet)
		{
			Point3D spawn = new Point3D(SpawnPoint.X, SpawnPoint.Y, SpawnPoint.Z);
			if (!source.IsWithinRadius(spawn, 440)) //take no damage
			{
				GamePlayer truc;
				if (source is GamePlayer)
					truc = (source as GamePlayer);
				else
					truc = ((source as GameSummonedPet).Owner as GamePlayer);
				if (truc != null)
					truc.Out.SendMessage(Name + " is immune to your damage!", EChatType.CT_System,
						EChatLoc.CL_ChatWindow);

				base.TakeDamage(source, damageType, 0, 0);
				return;
			}
			else //take dmg
			{
				base.TakeDamage(source, damageType, damageAmount, criticalAmount);
			}
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
	public override void Die(GameObject killer)
	{
		foreach (GameNpc npc in GetNPCsInRadius(5000))
		{
			if (npc != null && npc.IsAlive && npc.RespawnInterval == -1 && npc.PackageID == "EmthoroAdd")
				npc.Die(this);
		}
		base.Die(killer);
	}
	public override bool AddToWorld()
	{
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60166454);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;
		RespawnInterval = ServerProperties.Properties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

		Faction = FactionMgr.GetFactionByID(64);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(64));

		SpiritOfLordEmthoroBrain sbrain = new SpiritOfLordEmthoroBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = false;//load from database
		SaveIntoDatabase();
		base.AddToWorld();
		return true;
	}
	public override void DealDamage(AttackData ad)
	{
		if (ad != null && ad.AttackType == EAttackType.Spell && ad.Damage > 0)
			Health += ad.Damage;
		base.DealDamage(ad);
	}
}