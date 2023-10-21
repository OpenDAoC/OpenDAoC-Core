using System;
using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS.AI.Brains;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.PacketHandler;

namespace Core.GS;

#region Sister Blythe
public class SisterBlythe : GameEpicNPC
{
	public SisterBlythe() : base() { }

	[ScriptLoadedEvent]
	public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
	{
		if (log.IsInfoEnabled)
			log.Info("Sister Blythe Initializing...");
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
						truc.Out.SendMessage(Name + " can't be attacked from this distance!", EChatType.CT_System, EChatLoc.CL_ChatWindow);
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
		foreach (GameNpc npc in GetNPCsInRadius(8000))
		{
			if (npc.Brain is SisterBlytheBrain)
				return false;
		}
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(12982);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;
		SpawnExecutioners();

		RespawnInterval = ServerProperties.Properties.SET_EPIC_QUEST_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
		SisterBlytheBrain sbrain = new SisterBlytheBrain();
		SetOwnBrain(sbrain);
		base.AddToWorld();
		return true;
	}
    public override void Die(GameObject killer)
    {
		foreach (GameNpc npc in GetNPCsInRadius(4500))
		{
			if (npc != null && npc.IsAlive && npc.Brain is FallenExecutionerBrain)
				npc.Die(this);
		}
		base.Die(killer);
    }
    private void SpawnExecutioners()
	{
		Point3D spawn = new Point3D(322192, 671493, 2764);
		for (int i = 0; i < 4; i++)
		{
			FallenExecutioner npc = new FallenExecutioner();
			npc.X = spawn.X + Util.Random(-150, 150);
			npc.Y = spawn.Y + Util.Random(-150, 150);
			npc.Z = spawn.Z;
			npc.Heading = Heading;
			npc.CurrentRegion = CurrentRegion;
			npc.AddToWorld();
		}
	}
}
#endregion Sister Blythe

#region Fallen Executioners
public class FallenExecutioner : GameNpc
{
	public FallenExecutioner() : base() { }

	public override bool AddToWorld()
	{
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60160685);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;
		++SisterBlytheBrain.FallenExecutionerCount;

		FallenExecutionerBrain sbrain = new FallenExecutionerBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = true;
		RespawnInterval = -1;
		base.AddToWorld();
		return true;
	}
	
    public override void Die(GameObject killer)
    {
		--SisterBlytheBrain.FallenExecutionerCount;
        base.Die(killer);
    }
}
#endregion Fallen Executioners