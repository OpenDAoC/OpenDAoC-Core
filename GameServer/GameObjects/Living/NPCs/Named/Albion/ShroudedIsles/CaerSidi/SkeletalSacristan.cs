using System;
using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS.AI.Brains;
using Core.GS.Enums;

namespace Core.GS.Scripts;

public class SkeletalSacristan : GameEpicBoss
{
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
    public override int MaxHealth
    {
        get { return 100000; }
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

    public override double GetArmorAF(EArmorSlot slot)
    {
        return 350;
    }

    public override double GetArmorAbsorb(EArmorSlot slot)
    {
        // 85% ABS is cap.
        return 0.20;
    }
    public override bool AddToWorld()
	{
		Model = 916;
		Name = "Skeletal Sacristan";
		Size = 85;
		Level = 77;
		Gender = EGender.Neutral;
		BodyType = 11; // undead
		MaxDistance = 0;
		TetherRange = 0;
		RoamingRange = 0;
        RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds

        INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60166180);
		LoadTemplate(npcTemplate);
		SkeletalSacristanBrain sBrain = new SkeletalSacristanBrain();
		SetOwnBrain(sBrain);
		base.AddToWorld();
        SaveIntoDatabase();
        LoadedFromScript = false;
		return true;
	}
	[ScriptLoadedEvent]
	public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
	{
		if (log.IsInfoEnabled)
			log.Info("Skeletal Sacristan NPC Initializing...");
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