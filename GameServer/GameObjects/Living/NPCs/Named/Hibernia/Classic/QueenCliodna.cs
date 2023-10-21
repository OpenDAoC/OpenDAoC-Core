using System;
using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.Events;

namespace Core.GS;

public class QueenCliodna : GameEpicBoss
{
	public QueenCliodna() : base() { }

	[ScriptLoadedEvent]
	public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
	{
		if (log.IsInfoEnabled)
			log.Info("Queen Cliodna Initializing...");
	}
	public override int GetResist(EDamageType damageType)
	{
		switch (damageType)
		{
			case EDamageType.Slash: return 20;// dmg reduction for melee dmg
			case EDamageType.Crush: return 20;// dmg reduction for melee dmg
			case EDamageType.Thrust: return 20;// dmg reduction for melee dmg
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
		get { return 30000; }
	}
    public override void StartAttack(GameObject target)//mob only cast
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
	public override bool AddToWorld()
	{
		Name = "Queen Cliodna";
		Model = 347;
		Level = 70;
		Size = 50;
		MaxDistance = 2500;
		TetherRange = 2600;

		GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
		template.AddNPCEquipment(EInventorySlot.TorsoArmor, 403, 39, 0, 0);//modelID,color,effect,extension
		template.AddNPCEquipment(EInventorySlot.ArmsArmor, 405, 39);
		template.AddNPCEquipment(EInventorySlot.LegsArmor, 404, 39);
		template.AddNPCEquipment(EInventorySlot.HandsArmor, 406, 43, 0, 0);
		template.AddNPCEquipment(EInventorySlot.FeetArmor, 407, 43, 0, 0);
		template.AddNPCEquipment(EInventorySlot.Cloak, 91, 39, 0, 0);
		template.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 468, 0, 0);
		Inventory = template.CloseTemplate();
		SwitchWeapon(EActiveWeaponSlot.TwoHanded);

		VisibleActiveWeaponSlots = 34;
		MeleeDamageType = EDamageType.Crush;
		RespawnInterval = ServerProperties.Properties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
		QueenCliodnaBrain sbrain = new QueenCliodnaBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = false;//load from database
		SaveIntoDatabase();
		base.AddToWorld();
		return true;
	}
}