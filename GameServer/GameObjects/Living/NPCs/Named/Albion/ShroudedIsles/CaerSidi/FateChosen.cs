using System;
using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS.AI.Brains;
using Core.GS.Enums;
using Core.GS.Events;

namespace Core.GS;

public class FateChosen : GameEpicBoss
{
	public FateChosen() : base() 
	{
	}

	[ScriptLoadedEvent]
	public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
	{
		if (log.IsInfoEnabled)
			log.Info("Fate's Chosen Initializing...");
	}
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
	public override double AttackDamage(DbInventoryItem weapon)
	{
		return base.AttackDamage(weapon) * Strength / 100  * ServerProperties.Properties.EPICS_DMG_MULTIPLIER;
	}
	public override int AttackRange
	{
		get { return 350; }
		set { }
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
	public override bool AddToWorld()
	{
		GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
		template.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 567, 0);
		Inventory = template.CloseTemplate();
		SwitchWeapon(EActiveWeaponSlot.TwoHanded);

		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(50040);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;

		RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
		FateChosenBrain sbrain = new FateChosenBrain();
		SetOwnBrain(sbrain);
		base.AddToWorld();
		return true;
	}
}