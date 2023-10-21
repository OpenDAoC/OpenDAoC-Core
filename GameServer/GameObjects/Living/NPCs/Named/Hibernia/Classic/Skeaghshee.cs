using System;
using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS.Styles;
using Core.GS;
using Core.GS.AI.Brains;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.GameUtils;
using Core.GS.Server;

namespace Core.GS;

public class Skeaghshee : GameEpicBoss
{
	public Skeaghshee() : base() { }

	[ScriptLoadedEvent]
	public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
	{
		if (log.IsInfoEnabled)
			log.Info("Skeaghshee Initializing...");
	}
	//he is immune to any magic dmg
	public override void TakeDamage(GameObject source, EDamageType damageType, int damageAmount, int criticalAmount)
	{
		if (source is GamePlayer || source is GameSummonedPet)
		{
			if (damageType == EDamageType.Body || damageType == EDamageType.Cold ||
				damageType == EDamageType.Energy || damageType == EDamageType.Heat
				|| damageType == EDamageType.Matter || damageType == EDamageType.Spirit)
			{
				GamePlayer truc;
				if (source is GamePlayer)
					truc = (source as GamePlayer);
				else
					truc = ((source as GameSummonedPet).Owner as GamePlayer);
				if (truc != null)
					truc.Out.SendMessage(Name + " is immune to magic damage!", EChatType.CT_System,EChatLoc.CL_ChatWindow);
				base.TakeDamage(source, damageType, 0, 0);
				return;
			}
			else //take dmg
			{
				base.TakeDamage(source, damageType, damageAmount, criticalAmount);
			}
		}
	}
	public override int GetResist(EDamageType damageType)
	{
		switch (damageType)
		{
			case EDamageType.Slash: return 20;// dmg reduction for melee dmg
			case EDamageType.Crush: return 20;// dmg reduction for melee dmg
			case EDamageType.Thrust: return 20;// dmg reduction for melee dmg
			default: return 70;// dmg reduction for rest resists
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
	public static int TauntID = 247;
	public static int TauntClassID = 44;
	public static Style taunt = SkillBase.GetStyleByID(TauntID, TauntClassID);

	public static int BehindID = 256;
	public static int BehindClassID = 44;
	public static Style behind = SkillBase.GetStyleByID(BehindID, BehindClassID);

	public static int BehindFollowUpID = 259;
	public static int BehindFollowUpClassID = 44;
	public static Style behindFollowUp = SkillBase.GetStyleByID(BehindFollowUpID, BehindFollowUpClassID);

	public static int AfterParryID = 246;
	public static int AfterParryClassID = 44;
	public static Style afterParry = SkillBase.GetStyleByID(AfterParryID, AfterParryClassID);

	public override bool AddToWorld()
	{
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60166165);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;
		LoadEquipmentTemplateFromDatabase("d39e7d76-c7f3-4f79-a074-1eb441e83271");
		if (!Styles.Contains(taunt))
			Styles.Add(taunt);
		if (!Styles.Contains(behind))
			Styles.Add(behind);
		if (!Styles.Contains(behindFollowUp))
			Styles.Add(behindFollowUp);
		if (!Styles.Contains(afterParry))
			Styles.Add(afterParry);

		RespawnInterval = ServerProperty.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
		SkeaghsheeBrain sbrain = new SkeaghsheeBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = false;//load from database
		SaveIntoDatabase();
		base.AddToWorld();
		return true;
	}
    public override void OnAttackedByEnemy(AttackData ad)
    {
		if (ad != null)
        {
			if(ad.AttackResult == EAttackResult.Parried)
            {
				styleComponent.NextCombatStyle = afterParry;
				styleComponent.NextCombatBackupStyle = taunt;
            }
        }
        base.OnAttackedByEnemy(ad);
    }
    public override void OnAttackEnemy(AttackData ad)
    {
		if(ad != null)
        {
			if (ad.AttackResult == EAttackResult.HitStyle && ad.Style.ID == 259 && ad.Style.ClassID == 44)
			{
				styleComponent.NextCombatStyle = behindFollowUp;
				styleComponent.NextCombatBackupStyle = taunt;
			}
		}
        base.OnAttackEnemy(ad);
    }
}