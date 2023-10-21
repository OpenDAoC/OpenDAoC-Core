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

#region Scurceol Hyrde
public class ScurceolHyrde : GameEpicBoss
{
	public ScurceolHyrde() : base() { }

	[ScriptLoadedEvent]
	public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
	{
		if (log.IsInfoEnabled)
			log.Info("Scurceol Hyrde Initializing...");
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
	public override void TakeDamage(GameObject source, EDamageType damageType, int damageAmount, int criticalAmount)
	{
		if (source is GamePlayer || source is GameSummonedPet)
		{
			if (LyftMihtOne.Orb1Count > 0 || LyftMihtTwo.Orb2Count > 0 || LyftMihtThree.Orb3Count > 0 || LyftMihtFour.Orb4Count > 0)
			{
				if (damageType == EDamageType.Body || damageType == EDamageType.Cold || damageType == EDamageType.Energy || damageType == EDamageType.Heat
					|| damageType == EDamageType.Matter || damageType == EDamageType.Spirit || damageType == EDamageType.Crush || damageType == EDamageType.Thrust
					|| damageType == EDamageType.Slash)
				{
					GamePlayer truc;
					if (source is GamePlayer)
						truc = (source as GamePlayer);
					else
						truc = ((source as GameSummonedPet).Owner as GamePlayer);
					if (truc != null)
						truc.Out.SendMessage(Name + " is overpowered and can't take any damage.", EChatType.CT_System, EChatLoc.CL_ChatWindow);
					base.TakeDamage(source, damageType, 0, 0);
					return;
				}
			}
			else//take dmg
			{
				base.TakeDamage(source, damageType, damageAmount, criticalAmount);
			}
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
	public override bool AddToWorld()
	{
		Model = 919;
		Level = 81;
		Name = "Scurceol Hyrde";
		Size = 125;
		ParryChance = 70;

		Strength = 260;
		Dexterity = 150;
		Constitution = 100;
		Quickness = 80;
		Piety = 200;
		Intelligence = 200;
		Charisma = 200;
		Empathy = 300;

		MaxSpeedBase = 250;
		MaxDistance = 3500;
		TetherRange = 3800;
		RespawnInterval = ServerProperties.Properties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

		SpawnOrbs();
		Faction = FactionMgr.GetFactionByID(8);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(8));

		ScurceolHyrdeBrain sbrain = new ScurceolHyrdeBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = false;//load from database
		SaveIntoDatabase();
		base.AddToWorld();
		return true;
	}
    public override void Die(GameObject killer)
    {
		foreach (GameNpc npc in GetNPCsInRadius(5000))
		{
			if (npc != null)
			{
				if (npc.IsAlive && (npc.Brain is LyftMihtBrain1 || npc.Brain is LyftMihtBrain2 || npc.Brain is LyftMihtBrain3 || npc.Brain is LyftMihtBrain4))
				{
					npc.RemoveFromWorld();
					LyftMihtOne.Orb1Count = 0;
					LyftMihtTwo.Orb2Count = 0;
					LyftMihtThree.Orb3Count = 0;
					LyftMihtFour.Orb4Count = 0;
				}
			}
		}
        base.Die(killer);
    }
    public void SpawnOrbs()
	{
		if (LyftMihtOne.Orb1Count == 0)
		{
			LyftMihtOne Add = new LyftMihtOne();
			Add.CurrentRegion = CurrentRegion;
			Add.AddToWorld();
		}
		if (LyftMihtTwo.Orb2Count == 0)
		{
			LyftMihtTwo Add = new LyftMihtTwo();
			Add.CurrentRegion = CurrentRegion;
			Add.AddToWorld();
		}
		if (LyftMihtThree.Orb3Count == 0)
		{
			LyftMihtThree Add = new LyftMihtThree();
			Add.CurrentRegion = CurrentRegion;
			Add.AddToWorld();
		}
		if (LyftMihtFour.Orb4Count == 0)
		{
			LyftMihtFour Add = new LyftMihtFour();
			Add.CurrentRegion = CurrentRegion;
			Add.AddToWorld();
		}
	}
}
#endregion Scurceol Hyrde

#region 1st Orb (Lyft Miht)
public class LyftMihtOne : GameEpicNPC
{
	public LyftMihtOne() : base() { }

	public override int GetResist(EDamageType damageType)
	{
		switch (damageType)
		{
			case EDamageType.Slash: return 40;// dmg reduction for melee dmg
			case EDamageType.Crush: return 40;// dmg reduction for melee dmg
			case EDamageType.Thrust: return 40;// dmg reduction for melee dmg
			default: return 50;// dmg reduction for rest resists
		}
	}
	public override bool HasAbility(string keyName)
	{
		if (IsAlive && keyName == GS.Abilities.CCImmunity)
			return true;

		return base.HasAbility(keyName);
	}
	public override double GetArmorAF(EArmorSlot slot)
	{
		return 250;
	}
	public override double GetArmorAbsorb(EArmorSlot slot)
	{
		// 85% ABS is cap.
		return 0.15;
	}
	public override int MaxHealth
	{
		get { return 15000; }
	}
    public override void StartAttack(GameObject target)
    {
    }
	public static int Orb1Count = 0;
    public override void Die(GameObject killer)
    {
		--Orb1Count;
        base.Die(killer);
    }
    public override bool AddToWorld()
	{
		Model = 2049;
		Level = 90;
		Name = "Lyft Miht";
		X = 51236;
		Y = 20849;
		Z = 17669;
		Heading = 2560;
		Size = 20;
		MaxSpeedBase = 0;
		RespawnInterval = -1;//Util.Random(180000, 480000);
		Flags = ENpcFlags.FLYING;
		++Orb1Count;

		Faction = FactionMgr.GetFactionByID(8);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(8));

		LyftMihtBrain1 sbrain = new LyftMihtBrain1();
		SetOwnBrain(sbrain);
		LoadedFromScript = true;
		base.AddToWorld();
		return true;
	}
}
#endregion 1st Orb (Lyft Miht)

#region 2nd Orb (Lyft Miht)
public class LyftMihtTwo : GameEpicNPC
{
	public LyftMihtTwo() : base() { }

	public override int GetResist(EDamageType damageType)
	{
		switch (damageType)
		{
			case EDamageType.Slash: return 40;// dmg reduction for melee dmg
			case EDamageType.Crush: return 40;// dmg reduction for melee dmg
			case EDamageType.Thrust: return 40;// dmg reduction for melee dmg
			default: return 50;// dmg reduction for rest resists
		}
	}
	public override bool HasAbility(string keyName)
	{
		if (IsAlive && keyName == GS.Abilities.CCImmunity)
			return true;

		return base.HasAbility(keyName);
	}
	public override double GetArmorAF(EArmorSlot slot)
	{
		return 250;
	}
	public override double GetArmorAbsorb(EArmorSlot slot)
	{
		// 85% ABS is cap.
		return 0.15;
	}
	public override int MaxHealth
	{
		get { return 15000; }
	}
	public override void StartAttack(GameObject target)
	{
	}
	public static int Orb2Count = 0;
    public override void Die(GameObject killer)
    {
		--Orb2Count;
        base.Die(killer);
    }
    public override bool AddToWorld()
	{
		Model = 2049;
		Level = 90;
		Name = "Lyft Miht";
		X = 51316;
		Y = 19228;
		Z = 17408;
		Heading = 1919;
		Size = 20;
		MaxSpeedBase = 0;
		RespawnInterval = -1;// Util.Random(180000, 480000);
		Flags = ENpcFlags.FLYING;
		++Orb2Count;

		Faction = FactionMgr.GetFactionByID(8);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(8));

		LyftMihtBrain2 sbrain = new LyftMihtBrain2();
		SetOwnBrain(sbrain);
		LoadedFromScript = true;
		base.AddToWorld();
		return true;
	}
}
#endregion 2nd Orb (Lyft Miht)

#region 3rd Orb (Lyft Miht)
public class LyftMihtThree : GameEpicNPC
{
	public LyftMihtThree() : base() { }

	public override int GetResist(EDamageType damageType)
	{
		switch (damageType)
		{
			case EDamageType.Slash: return 40;// dmg reduction for melee dmg
			case EDamageType.Crush: return 40;// dmg reduction for melee dmg
			case EDamageType.Thrust: return 40;// dmg reduction for melee dmg
			default: return 50;// dmg reduction for rest resists
		}
	}
	public override bool HasAbility(string keyName)
	{
		if (IsAlive && keyName == GS.Abilities.CCImmunity)
			return true;

		return base.HasAbility(keyName);
	}
	public override double GetArmorAF(EArmorSlot slot)
	{
		return 250;
	}
	public override double GetArmorAbsorb(EArmorSlot slot)
	{
		// 85% ABS is cap.
		return 0.15;
	}
	public override int MaxHealth
	{
		get { return 15000; }
	}
	public override void StartAttack(GameObject target)
	{
	}
	public static int Orb3Count = 0;
    public override void Die(GameObject killer)
    {
		--Orb3Count;
        base.Die(killer);
    }
    public override bool AddToWorld()
	{
		Model = 2049;
		Level = 90;
		Name = "Lyft Miht";
		X = 52702;
		Y = 19214;
		Z = 17751;
		Heading = 1024;
		Size = 20;
		MaxSpeedBase = 0;
		RespawnInterval = -1;// Util.Random(180000, 480000);
		Flags = ENpcFlags.FLYING;
		++Orb3Count;

		Faction = FactionMgr.GetFactionByID(8);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(8));

		LyftMihtBrain3 sbrain = new LyftMihtBrain3();
		SetOwnBrain(sbrain);
		LoadedFromScript = true;
		base.AddToWorld();
		return true;
	}
}
#endregion 3rd Orb (Lyft Miht)

#region 4th Orb (Lyft Miht)
public class LyftMihtFour : GameEpicNPC
{
	public LyftMihtFour() : base() { }

	public override int GetResist(EDamageType damageType)
	{
		switch (damageType)
		{
			case EDamageType.Slash: return 40;// dmg reduction for melee dmg
			case EDamageType.Crush: return 40;// dmg reduction for melee dmg
			case EDamageType.Thrust: return 40;// dmg reduction for melee dmg
			default: return 50;// dmg reduction for rest resists
		}
	}
	public override bool HasAbility(string keyName)
	{
		if (IsAlive && keyName == GS.Abilities.CCImmunity)
			return true;

		return base.HasAbility(keyName);
	}
	public override double GetArmorAF(EArmorSlot slot)
	{
		return 250;
	}
	public override double GetArmorAbsorb(EArmorSlot slot)
	{
		// 85% ABS is cap.
		return 0.15;
	}
	public override int MaxHealth
	{
		get { return 15000; }
	}
	public override void StartAttack(GameObject target)
	{
	}
	public static int Orb4Count = 0;
    public override void Die(GameObject killer)
    {
		--Orb4Count;
        base.Die(killer);
    }
    public override bool AddToWorld()
	{
		Model = 2049;
		Level = 90;
		Name = "Lyft Miht";
		X = 52713;
		Y = 20841;
		Z = 17511;
		Heading = 1536;
		Size = 20;
		Flags = ENpcFlags.FLYING;
		MaxSpeedBase = 0;
		RespawnInterval = -1;// Util.Random(180000, 480000);
		++Orb4Count;

		Faction = FactionMgr.GetFactionByID(8);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(8));

		LyftMihtBrain4 sbrain = new LyftMihtBrain4();
		SetOwnBrain(sbrain);
		LoadedFromScript = true;
		base.AddToWorld();
		return true;
	}
}
#endregion 4th Orb (Lyft Miht)