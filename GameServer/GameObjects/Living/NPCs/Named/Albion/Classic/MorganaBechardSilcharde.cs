using System;
using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS.PacketHandler;

namespace Core.GS;

#region Morgana
public class Morgana : GameNpc
{
	public Morgana() : base() { }

	[ScriptLoadedEvent]
	public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
	{
		if (log.IsInfoEnabled)
			log.Info("Morgana Initializing...");
	}
	public static int BechardCount = 0;
	public static int SilchardeCount = 0;
	public static int BechardMinionCount = 10;
	public static int SilchardeMinionCount = 10;
	public static int BechardDemonicMinionsCount = 0;
	public static int SilchardeDemonicMinionsCount = 0;
	public override bool AddToWorld()
	{
		foreach (GameNpc npc in GetNPCsInRadius(5000))
		{
			if (npc.Brain is MorganaBrain)
				return false;
		}
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(700000001);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;

		MorganaBrain sbrain = new MorganaBrain();
		SetOwnBrain(sbrain);
		base.AddToWorld();
		return true;
	}
}
#endregion Morgana

#region Bechard
public class Bechard : GameEpicNPC
{
	public Bechard() : base() { }
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
		get { return 7000; }
	}
	public override bool AddToWorld()
	{
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(700000009);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;
		++Morgana.BechardCount;
		BechardKilled = false;

		BechardBrain sbrain = new BechardBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = true;
		RespawnInterval = -1;
		base.AddToWorld();
		return true;
	}
	public static bool BechardKilled = false;
	public override void Die(GameObject killer)
	{
		--Morgana.BechardCount;
		BechardKilled = true;
		SpawnDemonic();
		base.Die(killer);
	}
	private void SpawnDemonic()
	{
		Point3D spawn = new Point3D(306041, 670103, 3310);
		for (int i = 0; i < Morgana.BechardMinionCount + Util.Random(4, 6); i++)
		{
			DemonicMinion npc = new DemonicMinion();
			npc.X = spawn.X + Util.Random(-150, 150);
			npc.Y = spawn.Y + Util.Random(-150, 150);
			npc.Z = spawn.Z;
			npc.Heading = 3148;
			npc.CurrentRegion = CurrentRegion;
			npc.PackageID = "BechardMinion";
			npc.AddToWorld();
		}
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
#endregion Bechard

#region Silcharde
public class Silcharde : GameEpicNPC
{
	public Silcharde() : base() { }
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
		get { return 7000; }
	}
	public override bool AddToWorld()
	{
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(700000008);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;
		++Morgana.SilchardeCount;
		SilchardeKilled = false;

		SilchardeBrain sbrain = new SilchardeBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = true;
		RespawnInterval = -1;
		base.AddToWorld();
		return true;
	}

	public static bool SilchardeKilled = false;
	public override void Die(GameObject killer)
	{
		SilchardeKilled = true;
		--Morgana.SilchardeCount;
		SpawnDemonic();
		base.Die(killer);
	}
	private void SpawnDemonic()
	{
		Point3D spawn = new Point3D(306041, 670103, 3310);
		for (int i = 0; i < Morgana.SilchardeMinionCount + Util.Random(4, 6); i++)
		{
			DemonicMinion npc = new DemonicMinion();
			npc.X = spawn.X + Util.Random(-150, 150);
			npc.Y = spawn.Y + Util.Random(-150, 150); 
			npc.Z = spawn.Z;
			npc.Heading = 3148;
			npc.CurrentRegion = CurrentRegion;
			npc.PackageID = "SilchardeMinion";
			npc.AddToWorld();
		}
	}
}
#endregion Silcharde

#region Demonic Minion
public class DemonicMinion : GameNpc
{
	public DemonicMinion() : base() { }

	public override bool AddToWorld()
	{
		Name = "demonic minion";
		Level = (byte)Util.Random(30, 33);
		Model = 606;
		RoamingRange = 200;
		MaxSpeedBase = 245;
		Flags = ENpcFlags.FLYING;

		DemonicMinionBrain sbrain = new DemonicMinionBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = true;
		RespawnInterval = -1;
		base.AddToWorld();
		return true;
	}
	public override void Die(GameObject killer)
	{
		if (PackageID == "BechardMinion")
			++Morgana.BechardDemonicMinionsCount;
		if (PackageID == "SilchardeMinion")
			++Morgana.SilchardeDemonicMinionsCount;
		base.Die(killer);
	}
}
#endregion Demonic Minion