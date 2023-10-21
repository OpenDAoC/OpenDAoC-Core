using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.GS.ECS;
using Core.GS.Enums;

namespace Core.GS;

public class Ydenia : GameEpicNPC
{
	public Ydenia() : base() { }

	public override int GetResist(EDamageType damageType)
	{
		switch (damageType)
		{
			case EDamageType.Slash: return 20;// dmg reduction for melee dmg
			case EDamageType.Crush: return 20;// dmg reduction for melee dmg
			case EDamageType.Thrust: return 20;// dmg reduction for melee dmg
			default: return 20;// dmg reduction for rest resists
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
		get { return 10000; }
	}
	public override bool AddToWorld()
	{
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60168100);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;

		RespawnInterval = ServerProperties.Properties.SET_EPIC_QUEST_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
		YdeniaBrain sbrain = new YdeniaBrain();
		SetOwnBrain(sbrain);
		base.AddToWorld();
		return true;
	}
    public override void Die(GameObject killer)
    {
		var throwPlayer = TempProperties.GetProperty<EcsGameTimer>("ydenia_teleport");//cancel teleport
		if (throwPlayer != null)
		{
			throwPlayer.Stop();
			TempProperties.RemoveProperty("ydenia_teleport");
		}
		base.Die(killer);
    }
	public override void DealDamage(AttackData ad)
	{
		if (ad != null && ad.AttackType == EAttackType.Spell && ad.Damage > 0 && ad.DamageType == EDamageType.Body)
		{
			Health += ad.Damage;
		}
		base.DealDamage(ad);
	}
}