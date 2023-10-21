using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.GS.AI.Brains;

namespace Core.GS.Scripts;

public class WarlordDorinakka : GameEpicBoss
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
	public override double GetArmorAF(EArmorSlot slot)
	{
		return 350;
	}

	public override double AttackDamage(DbInventoryItem weapon)
	{
		return base.AttackDamage(weapon) * ServerProperties.Properties.EPICS_DMG_MULTIPLIER;
	}
	
	public override double GetArmorAbsorb(EArmorSlot slot)
	{
		// 85% ABS is cap.
		return 0.20;
	}
	public override bool HasAbility(string keyName)
	{
		if (IsAlive && keyName == "CCImmunity")
			return true;

		return base.HasAbility(keyName);
	}
	public override short MaxSpeedBase => (short) (191 + Level * 2);
	public override int MaxHealth => 100000;

	public override int AttackRange
	{
		get => 180;
		set { }
	}
	
	public override byte ParryChance => 80;
	
	public override bool AddToWorld()
	{
		GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
		template.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 847, 0);
		Inventory = template.CloseTemplate();
		SwitchWeapon(EActiveWeaponSlot.TwoHanded);

		VisibleActiveWeaponSlots = 34;
		MeleeDamageType = EDamageType.Slash;
		Level = 81;
		Gender = EGender.Neutral;
		BodyType = 5; // giant
		MaxDistance = 1500;
		TetherRange = 2000;
		RoamingRange = 0;
		Realm = ERealm.None;
		ParryChance = 80; // 80% parry chance

		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60167787);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;

		RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds
		WarlordDorinakkaBrain adds = new WarlordDorinakkaBrain();
		SetOwnBrain(adds);
		base.AddToWorld();
		return true;
	}		
}