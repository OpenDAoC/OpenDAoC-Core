using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.GS;

#region Blight
public class Blight : GameEpicBoss
{
	public Blight() : base() { }

	[ScriptLoadedEvent]
	public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
	{
		if (log.IsInfoEnabled)
			log.Info("Blight Initializing...");
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
		return base.AttackDamage(weapon) * Strength / 100;
	}
	public override int AttackRange
	{
		get { return 350; }
		set { }
	}
	public override void StartAttack(GameObject target)
	{
		if (BlightBrain.canGrowth)
			return;
		else
			base.StartAttack(target);
	}
	public override bool HasAbility(string keyName)
	{
		if (IsAlive && keyName == GS.Abilities.CCImmunity)
			return true;
		if (BlightBrain.canGrowth && IsAlive && keyName == GS.Abilities.DamageImmunity)
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
	#region Stats
	public override short Charisma { get => base.Charisma; set => base.Charisma = 200; }
	public override short Piety { get => base.Piety; set => base.Piety = 200; }
	public override short Intelligence { get => base.Intelligence; set => base.Intelligence = 200; }
	public override short Empathy { get => base.Empathy; set => base.Empathy = 400; }
	public override short Dexterity { get => base.Dexterity; set => base.Dexterity = 200; }
	public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
	public override short Strength { get => base.Strength; set => base.Strength = 200; }
	#endregion

	public void BroadcastMessage(String message)
	{
		foreach (GamePlayer player in GetPlayersInRadius(3500))
		{
			player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_ChatWindow);
		}
	}
	public override bool AddToWorld()
	{
		BlightBrain.canGrowth = true;
		Name = "Blight";
		Model = 26;
		Level = 70;
		Size = 35;
		MaxDistance = 2500;
		TetherRange = 2600;
		BroadcastMessage("Bloody bones fly from all directions into a swirling cloud of gore in the air before you. The bones begin to join together forming a single giant skeleton.");

		RespawnInterval = -1;
		BlightBrain sbrain = new BlightBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = true;
		bool success = base.AddToWorld();
		if (success)
		{
			new EcsGameTimer(this, new EcsGameTimer.EcsTimerCallback(Show_Effect), 500);
		}
		return success;
	}

	protected int Show_Effect(EcsGameTimer timer)
	{
		if (IsAlive)
		{
			foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
				player.Out.SendSpellEffectAnimation(this, this, 5117, 0, false, 0x01);

			return 4000;
		}

		return 0;
	}

	public override void Die(GameObject killer)
    {
		int respawnTime = ServerProperties.Properties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;
		new EcsGameTimer(this, new EcsGameTimer.EcsTimerCallback(SpawnFireBlight), respawnTime);
        base.Die(killer);
    }
	private int SpawnFireBlight(EcsGameTimer timer)
    {
		BlightControllerBrain.CreateLateBlight = false;
		BlightControllerBrain.CreateFleshBlight = false;
		BlightControllerBrain.CreateBlight = false;
		FireBlight.FireBlightCount = 0;
		LateBlight.LateBlightCount = 0;
		FleshBlight.FleshBlightCount = 0;

		for (int i = 0; i < 8; i++)
		{
			foreach (GameNpc npc in GetNPCsInRadius(8000))
			{
				if (npc.Brain is BlightControllerBrain)
                {
					FireBlight boss = new FireBlight();
					boss.X = npc.X + Util.Random(-500, 500);
					boss.Y = npc.Y + Util.Random(-500, 500);
					boss.Z = npc.Z;
					boss.Heading = npc.Heading;
					boss.CurrentRegion = npc.CurrentRegion;
					boss.AddToWorld();
				}
			}
		}
		return 0;
	}
	public override void OnAttackEnemy(AttackData ad) //on enemy actions
	{
		if (Util.Chance(25))
		{
			if (ad != null && (ad.AttackResult == EAttackResult.HitUnstyled || ad.AttackResult == EAttackResult.HitStyle))
				CastSpell(BlightDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
		}
		base.OnAttackEnemy(ad);
	}
	#region Spells
	private Spell m_BlightDD;
	public Spell BlightDD
	{
		get
		{
			if (m_BlightDD == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.Power = 0;
				spell.RecastDelay = 2;
				spell.ClientEffect = 360;
				spell.Icon = 360;
				spell.Damage = 400;
				spell.DamageType = (int)EDamageType.Heat;
				spell.Name = "Fire Strike";
				spell.Range = 500;
				spell.SpellID = 11899;
				spell.Target = "Enemy";
				spell.Type = ESpellType.DirectDamageNoVariance.ToString();
				m_BlightDD = new Spell(spell, 60);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_BlightDD);
			}
			return m_BlightDD;
		}
	}
	#endregion
}
#endregion Blight

#region Fire Blight
public class FireBlight : GameNpc
{
	public FireBlight() : base()
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
	public override double AttackDamage(DbInventoryItem weapon)
	{
		return base.AttackDamage(weapon) * Strength / 100;
	}
	public override int MaxHealth
	{
		get { return 2500; }
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
	public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
	public override short Strength { get => base.Strength; set => base.Strength = 150; }
	public static int FireBlightCount = 0;
	public override bool AddToWorld()
	{
		Model = 26;
		Name = "Fire Blight";
		Level = (byte)Util.Random(38, 44);
		Size = 50;
		RespawnInterval = -1;
		RoamingRange = 200;

		LoadedFromScript = true;
		FireBlightBrain sbrain = new FireBlightBrain();
		SetOwnBrain(sbrain);
		bool success = base.AddToWorld();
		if (success)
		{
			new EcsGameTimer(this, new EcsGameTimer.EcsTimerCallback(Show_Effect), 500);
		}
		return success;
	}
	#region Show Effects
	private protected int Show_Effect(EcsGameTimer timer)
	{
		if (IsAlive)
		{
			foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
				player.Out.SendSpellEffectAnimation(this, this, 4216, 0, false, 0x01);

			return 3000;
		}

		return 0;
	}
	
	#endregion
	public override void Die(GameObject killer)
    {
		++FireBlightCount;
		base.Die(killer);
    }
}
#endregion Fire Blight

#region Late Blight
public class LateBlight : GameNpc
{
	public LateBlight() : base()
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
	public override double AttackDamage(DbInventoryItem weapon)
	{
		return base.AttackDamage(weapon) * Strength / 100;
	}
	public override int MaxHealth
	{
		get { return 5000; }
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
	public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
	public override short Strength { get => base.Strength; set => base.Strength = 200; }
	public static int LateBlightCount = 0;
	public override bool AddToWorld()
	{
		Model = 26;
		Name = "Late Blight";
		Level = (byte)Util.Random(50, 55);
		Size = 70;
		RespawnInterval = -1;
		RoamingRange = 200;

		LoadedFromScript = true;
		LateBlightBrain sbrain = new LateBlightBrain();
		SetOwnBrain(sbrain);
		bool success = base.AddToWorld();
		if (success)
		{
			new EcsGameTimer(this, new EcsGameTimer.EcsTimerCallback(Show_Effect), 500);
		}
		return success;
	}
	#region Show Effects
	private protected int Show_Effect(EcsGameTimer timer)
	{
		if (IsAlive)
		{
			foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
				player.Out.SendSpellEffectAnimation(this, this, 4216, 0, false, 0x01);

			return 3000;
		}
		return 0;
	}
	
	#endregion
	public override void Die(GameObject killer)
	{
		++LateBlightCount;
		base.Die(killer);
	}
}
#endregion Late Blight

#region Flesh Blight
public class FleshBlight : GameNpc
{
	public FleshBlight() : base()
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
	public override double AttackDamage(DbInventoryItem weapon)
	{
		return base.AttackDamage(weapon) * Strength / 100;
	}
	public override int MaxHealth
	{
		get { return 10000; }
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
	public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
	public override short Strength { get => base.Strength; set => base.Strength = 200; }
	public static int FleshBlightCount = 0;
	public override bool AddToWorld()
	{
		Model = 26;
		Name = "Flesh Blight";
		Level = (byte)Util.Random(60, 63);
		Size = 100;
		RespawnInterval = -1;
		RoamingRange = 200;

		LoadedFromScript = true;
		FleshBlightBrain sbrain = new FleshBlightBrain();
		SetOwnBrain(sbrain);
		bool success = base.AddToWorld();
		if (success)
		{
			new EcsGameTimer(this, new EcsGameTimer.EcsTimerCallback(Show_Effect), 500);
		}
		return success;
	}
	#region Show Effects
	private protected int Show_Effect(EcsGameTimer timer)
	{
		if (IsAlive)
		{
			foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
				player?.Out.SendSpellEffectAnimation(this, this, 4216, 0, false, 0x01);

			return 3000;
		}

		return 0;
	}
	#endregion
	public override void Die(GameObject killer)
	{
		++FleshBlightCount;
		base.Die(killer);
	}
}
#endregion Flesh Blight

#region Blight Controller
// Controls when and what kind of Blights will spawn
public class BlightController : GameNpc
{
	public BlightController() : base()
	{
	}
	public override bool IsVisibleToPlayers => true;
	public override bool AddToWorld()
	{
		Name = "Blight Controller";
		GuildName = "DO NOT REMOVE";
		Level = 50;
		Model = 665;
		RespawnInterval = 5000;
		Flags = (ENpcFlags)28;
		SpawnFireBlight();

		BlightControllerBrain sbrain = new BlightControllerBrain();
		SetOwnBrain(sbrain);
		base.AddToWorld();
		return true;
	}
	public void SpawnFireBlight()
	{
		BlightControllerBrain.CreateLateBlight = false;
		BlightControllerBrain.CreateFleshBlight = false;
		BlightControllerBrain.CreateBlight = false;
		FireBlight.FireBlightCount = 0;
		LateBlight.LateBlightCount = 0;
		FleshBlight.FleshBlightCount = 0;

		foreach (GameNpc npc in GetNPCsInRadius(8000))
		{
			if (npc.Brain is FireBlightBrain)
				return;
		}
		for (int i = 0; i < 8; i++)
		{
			FireBlight boss = new FireBlight();
			boss.X = X + Util.Random(-500, 500);
			boss.Y = Y + Util.Random(-500, 500);
			boss.Z = Z;
			boss.Heading = Heading;
			boss.CurrentRegion = CurrentRegion;
			boss.AddToWorld();
		}
	}
}
#endregion Blight Controller