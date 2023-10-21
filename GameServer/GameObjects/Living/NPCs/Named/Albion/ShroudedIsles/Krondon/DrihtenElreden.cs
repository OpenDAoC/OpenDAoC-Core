using System;
using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS.AI.Brains;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.Styles;

namespace Core.GS;

public class DrihtenElreden : GameEpicBoss
{
	public DrihtenElreden() : base() { }

	[ScriptLoadedEvent]
	public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
	{
		if (log.IsInfoEnabled)
			log.Info("Drihten Elreden Initializing...");
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
	public static int TauntID = 240;
	public static int TauntClassID = 10;
	public static Style taunt = SkillBase.GetStyleByID(TauntID, TauntClassID);

	public static int AfterEvadeID = 238;
	public static int AfterEvadeClassID = 10;
	public static Style AfterEvade = SkillBase.GetStyleByID(AfterEvadeID, AfterEvadeClassID);

	public static int EvadeFollowUPID = 242;
	public static int EvadeFollowUPClassID = 10;
	public static Style EvadeFollowUP = SkillBase.GetStyleByID(AfterEvadeID, EvadeFollowUPClassID);

    public override void OnAttackedByEnemy(AttackData ad)
    {
		if (ad != null && ad.AttackResult == EAttackResult.Evaded)
		{
			styleComponent.NextCombatBackupStyle = AfterEvade;
			styleComponent.NextCombatStyle = EvadeFollowUP;
		}
        base.OnAttackedByEnemy(ad);
    }
    public override void OnAttackEnemy(AttackData ad)
    {
		if(ad != null && ad.AttackResult == EAttackResult.HitUnstyled)
        {
			styleComponent.NextCombatBackupStyle = taunt;
			styleComponent.NextCombatStyle = AfterEvade;
        }
		if (ad != null && ad.AttackResult == EAttackResult.HitStyle && ad.Style.ID == 238 && ad.Style.ClassID == 10)
		{
			styleComponent.NextCombatStyle = EvadeFollowUP;
		}
		base.OnAttackEnemy(ad);
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
		Model = 919;
		Level = (byte)(Util.Random(78, 80));
		Name = "Drihten Elreden";
		Size = 120;

		Strength = 280;
		Dexterity = 150;
		Constitution = 100;
		Quickness = 80;
		Piety = 200;
		Intelligence = 200;
		Charisma = 200;
		Empathy = 400;

		if(!Styles.Contains(taunt))
			Styles.Add(taunt);
		if (!Styles.Contains(AfterEvade))
			Styles.Add(AfterEvade);
		if (!Styles.Contains(EvadeFollowUP))
			Styles.Add(EvadeFollowUP);

		GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
		template.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 881, 0, 0);
		Inventory = template.CloseTemplate();
		SwitchWeapon(EActiveWeaponSlot.TwoHanded);

		VisibleActiveWeaponSlots = 34;
		MeleeDamageType = EDamageType.Crush;
		ParryChance = 30;
		EvadeChance = 50;

		MaxSpeedBase = 250;
		MaxDistance = 3500;
		TetherRange = 3800;
		RespawnInterval = ServerProperties.Properties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

		Faction = FactionMgr.GetFactionByID(8);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(8));

		DrihtenElredenBrain sbrain = new DrihtenElredenBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = false;//load from database
		SaveIntoDatabase();
		base.AddToWorld();
		return true;
	}
}