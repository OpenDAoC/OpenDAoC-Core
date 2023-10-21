using System;
using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS;
using Core.GS.AI.Brains;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.GameUtils;
using Core.GS.Server;
using Core.GS.Skills;

namespace Core.GS;

#region Cailean
public class Cailean : GameEpicNPC
{
	public Cailean() : base() { }

	[ScriptLoadedEvent]
	public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
	{
		if (log.IsInfoEnabled)
			log.Info("Cailean Initializing...");
	}
	public override void TakeDamage(GameObject source, EDamageType damageType, int damageAmount, int criticalAmount)
	{
		if (source is GamePlayer || source is GameSummonedPet)
		{
			if (IsOutOfTetherRange)
			{
				if (damageType == EDamageType.Body || damageType == EDamageType.Cold ||
					damageType == EDamageType.Energy || damageType == EDamageType.Heat
					|| damageType == EDamageType.Matter || damageType == EDamageType.Spirit ||
					damageType == EDamageType.Crush || damageType == EDamageType.Thrust
					|| damageType == EDamageType.Slash)
				{
					GamePlayer truc;
					if (source is GamePlayer)
						truc = (source as GamePlayer);
					else
						truc = ((source as GameSummonedPet).Owner as GamePlayer);
					if (truc != null)
						truc.Out.SendMessage(Name + " is too far away from it's habbitat and is immune to your damage!", EChatType.CT_System,
							EChatLoc.CL_ChatWindow);
					base.TakeDamage(source, damageType, 0, 0);
					return;
				}
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
		if (IsAlive && keyName == AbilityConstants.CCImmunity)
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
		foreach (GameNpc npc in GetNPCsInRadius(8000))
		{
			if (npc.Brain is CaileanBrain)
				return false;
		}
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60158846);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;

		RespawnInterval = ServerProperty.SET_EPIC_QUEST_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
		CaileanBrain sbrain = new CaileanBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = false;//load from database
		SaveIntoDatabase();
		base.AddToWorld();
		return true;
	}
    public override void Die(GameObject killer)
    {
		foreach (GameNpc npc in GetNPCsInRadius(8000))
		{
			if (npc != null && npc.IsAlive && npc.Brain is WalkingTreeBrain)
				npc.RemoveFromWorld();
		}
		foreach (GameNpc npc in GetNPCsInRadius(8000))
		{
			if (npc != null && npc.IsAlive && npc.Brain is WalkingTree2Brain)
				npc.RemoveFromWorld();
		}
		base.Die(killer);
    }
}
#endregion Cailean

#region Cailean's Trees 4-5 yellows
public class WalkingTree : GameNpc
{
	public WalkingTree() : base()
	{
	}
	public override int GetResist(EDamageType damageType)
	{
		switch (damageType)
		{
			case EDamageType.Slash: return 15;// dmg reduction for melee dmg
			case EDamageType.Crush: return 15;// dmg reduction for melee dmg
			case EDamageType.Thrust: return 15;// dmg reduction for melee dmg
			default: return 15;// dmg reduction for rest resists
		}
	}
	public override int MaxHealth
	{
		get { return 2000; }
	}
	public override double GetArmorAF(EArmorSlot slot)
	{
		return 200;
	}
	public override double GetArmorAbsorb(EArmorSlot slot)
	{
		// 85% ABS is cap.
		return 0.10;
	}
	public override bool AddToWorld()
	{
		Model = 1703;
		Name = "walking tree";
		Level = (byte)Util.Random(48, 50);
		Size = (byte)Util.Random(50, 55);
		RespawnInterval = -1;
		RoamingRange = 200;

		LoadedFromScript = true;
		WalkingTreeBrain sbrain = new WalkingTreeBrain();
		SetOwnBrain(sbrain);
		base.AddToWorld();
		return true;
	}
}
#endregion Cailean's Trees 4-5 yellows

#region Cailean's Trees 8-10 blue
public class WalkingTree2 : GameNpc
{
	public WalkingTree2() : base()
	{
	}
	public override int GetResist(EDamageType damageType)
	{
		switch (damageType)
		{
			case EDamageType.Slash: return 15;// dmg reduction for melee dmg
			case EDamageType.Crush: return 15;// dmg reduction for melee dmg
			case EDamageType.Thrust: return 15;// dmg reduction for melee dmg
			default: return 15;// dmg reduction for rest resists
		}
	}
	public override int MaxHealth
	{
		get { return 1500; }
	}
	public override double GetArmorAF(EArmorSlot slot)
	{
		return 200;
	}
	public override double GetArmorAbsorb(EArmorSlot slot)
	{
		// 85% ABS is cap.
		return 0.10;
	}
	public override bool AddToWorld()
	{
		Model = 1703;
		Name = "rotted tree";
		Level = (byte)Util.Random(40, 44);
		Size = (byte)Util.Random(40, 50);
		RespawnInterval = -1;
		MaxSpeedBase = 0;

		LoadedFromScript = true;
		WalkingTree2Brain sbrain = new WalkingTree2Brain();
		SetOwnBrain(sbrain);
		base.AddToWorld();
		return true;
	}
}
#endregion Cailean's Trees 8-10 blue