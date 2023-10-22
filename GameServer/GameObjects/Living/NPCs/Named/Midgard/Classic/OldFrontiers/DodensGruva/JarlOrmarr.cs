using System;
using Core.Database.Tables;
using Core.GS.AI.Brains;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.GameUtils;
using Core.GS.Server;
using Core.GS.Skills;

namespace Core.GS;

public class JarlOrmarr : GameEpicBoss
{
	public JarlOrmarr() : base()
	{		
	}
	/// <summary>
	/// Add Jarl Ormarr to World
	/// </summary>
	public override bool AddToWorld()
	{
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(9918);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;

		// humanoid
		BodyType = 6;
		MeleeDamageType = EDamageType.Slash;
		Faction = FactionMgr.GetFactionByID(779);
		RespawnInterval = ServerProperty.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

		// right hand
		VisibleActiveWeaponSlots = (byte) EActiveWeaponSlot.Standard;			
		ScalingFactor = 40;
		base.SetOwnBrain(new JarlOrmarrBrain());
		LoadedFromScript = false; //load from database
		SaveIntoDatabase();
		base.AddToWorld();		
		return true;
	}
	
	public override double AttackDamage(DbInventoryItem weapon)
	{
		return base.AttackDamage(weapon) * Strength / 100;
	}
	public override int AttackRange
	{
		get
		{
			return 350;
		}
		set
		{ }
	}
	public override bool HasAbility(string keyName)
	{
		if (IsAlive && keyName == AbilityConstants.CCImmunity)
			return true;

		return base.HasAbility(keyName);
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
	/// <summary>
	/// Return to spawn point, Jarl Ormarr can't be attacked while it's
	/// on it's way.
	/// </summary>
	public override void ReturnToSpawnPoint(short speed)
	{
		base.ReturnToSpawnPoint(MaxSpeed);
	}
	public override void OnAttackedByEnemy(AttackData ad)
	{
		if (IsReturningToSpawnPoint)
			return;

		base.OnAttackedByEnemy(ad);
	}

	public override void Die(GameObject killer)
	{
		log.Debug($"{Name} killed by {killer.Name}");

		GamePlayer playerKiller = killer as GamePlayer;

		if (playerKiller?.Group != null)
		{
			foreach (GamePlayer groupPlayer in playerKiller.Group.GetPlayersInTheGroup())
				CoreRogMgr.GenerateReward(groupPlayer,OrbsReward);
		}

		base.Die(killer);
	}
	[ScriptLoadedEvent]
	public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
	{
		if (log.IsInfoEnabled)
			log.Info("Jarl Ormarr NPC Initializing...");
	}
}