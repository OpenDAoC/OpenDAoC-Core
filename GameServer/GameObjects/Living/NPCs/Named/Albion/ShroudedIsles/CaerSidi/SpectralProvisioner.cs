using System;
using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS.PacketHandler;

namespace Core.GS.Scripts;

#region Spectral Provisioner
public class SpectralProvisioner : GameEpicBoss
{
public SpectralProvisioner()
	: base() { }
	public override int GetResist(EDamageType damageType)
	{
		switch (damageType)
		{
			case EDamageType.Slash: return 40;// dmg reduction for melee dmg
			case EDamageType.Crush: return 40;// dmg reduction for melee dmg
			case EDamageType.Thrust: return 40;// dmg reduction for melee dmg
			default: return 70;// dmg reduction for rest resists
		}
	}
	public override void TakeDamage(GameObject source, EDamageType damageType, int damageAmount, int criticalAmount)
	{
		if (source is GamePlayer || source is GameSummonedPet)
		{
			if (damageType == EDamageType.Heat || damageType == EDamageType.Spirit || damageType == EDamageType.Cold) //take no damage
			{
				GamePlayer truc;
				if (source is GamePlayer)
					truc = (source as GamePlayer);
				else
					truc = ((source as GameSummonedPet).Owner as GamePlayer);
				if (truc != null)
					truc.Out.SendMessage("The Spectral Provisioner is immune to this form of attack.", EChatType.CT_System,EChatLoc.CL_ChatWindow);

				base.TakeDamage(source, damageType, 0, 0);
				return;
			}
			else //take dmg
			{
				base.TakeDamage(source, damageType, damageAmount, criticalAmount);
			}
		}
	}
	public override double GetArmorAF(EArmorSlot slot)
    {
	    return 350;
    }
	
	public override double AttackDamage(DbInventoryItem weapon)
	{
		return base.AttackDamage(weapon) * ServerProperties.Properties.EPICS_DMG_MULTIPLIER;
	}
	public override bool HasAbility(string keyName)
	{
		if (IsAlive && keyName == "CCImmunity")
			return true;

		return base.HasAbility(keyName);
	}
	public override double GetArmorAbsorb(EArmorSlot slot)
	{
		// 85% ABS is cap.
		return 0.20;
	}

	public override short MaxSpeedBase => (short) (191 + Level * 2);
	public override int MaxHealth => 100000;

	public override int AttackRange
	{
		get => 180;
		set { }
	}
	public override bool AddToWorld()
	{
		Level = 77;
		Gender = EGender.Neutral;
		BodyType = 11; // undead
		MaxDistance = 0;
		TetherRange = 0;
		RoamingRange = 0;
		MaxSpeedBase = 300;
		CurrentSpeed = 300;

		RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60166427);
		LoadTemplate(npcTemplate);
		SpectralProvisionerBrain.point1check = false;
		SpectralProvisionerBrain.point2check = false;
		SpectralProvisionerBrain.point3check = false;
		SpectralProvisionerBrain.point4check = false;
		SpectralProvisionerBrain.point5check = false;
		SpectralProvisionerBrain.point6check = false;
		SpectralProvisionerBrain.point7check = false;
		SpectralProvisionerBrain.point8check = false;
		SpectralProvisionerBrain sBrain = new SpectralProvisionerBrain();
		SetOwnBrain(sBrain);
		LoadedFromScript = true;
		base.AddToWorld();
		return true;
	}
   
	[ScriptLoadedEvent]
	public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
	{
		if (log.IsInfoEnabled)
			log.Info("Spectral Provisioner NPC Initializing...");
	}
	public override void ReturnToSpawnPoint(short speed)
	{
		if (IsAlive)
			return;
		base.ReturnToSpawnPoint(speed);
	}
	public override void StartAttack(GameObject target)
    {
    }
	public override bool IsVisibleToPlayers => true;
}
#endregion Spectral Provisioner

#region Spectral Provisioner Spawner
public class SpectralProvisionerSpawner : GameNpc
{
	public SpectralProvisionerSpawner() : base()
	{
	}
	public override bool AddToWorld()
	{
		Name = "Spectral Provisioner Spawner";
		GuildName = "DO NOT REMOVE";
		Level = 50;
		Model = 665;
		RespawnInterval = 5000;
		Flags = (ENpcFlags)28;

		SpectralProvisionerSpawnerBrain sbrain = new SpectralProvisionerSpawnerBrain();
		SetOwnBrain(sbrain);
		base.AddToWorld();
		return true;
	}
}
#endregion Spectral Provisioner Spawner