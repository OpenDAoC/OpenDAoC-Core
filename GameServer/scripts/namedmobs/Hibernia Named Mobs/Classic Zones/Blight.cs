using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;

namespace DOL.GS
{
	public class Blight : GameEpicBoss
	{
		public Blight() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Blight Initializing...");
		}
		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 40;// dmg reduction for melee dmg
				case eDamageType.Crush: return 40;// dmg reduction for melee dmg
				case eDamageType.Thrust: return 40;// dmg reduction for melee dmg
				default: return 70;// dmg reduction for rest resists
			}
		}
		public override double AttackDamage(InventoryItem weapon)
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
		public override double GetArmorAF(eArmorSlot slot)
		{
			return 350;
		}
		public override double GetArmorAbsorb(eArmorSlot slot)
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
		public override bool AddToWorld()
		{
			Name = "Blight";
			Model = 26;
			Level = 70;
			Size = 150;
			MaxDistance = 2500;
			TetherRange = 2600;

			RespawnInterval = -1;
			BlightBrain sbrain = new BlightBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = true;
			bool success = base.AddToWorld();
			if (success)
			{
				new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(Show_Effect), 500);
			}
			return success;
		}
		#region Show Effects
		protected int Show_Effect(ECSGameTimer timer)
		{
			if (IsAlive)
			{
				foreach (GamePlayer player in GetPlayersInRadius(3000))
				{
					if (player != null)
						player.Out.SendSpellEffectAnimation(this, this, 5117, 0, false, 0x01);
				}
				new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(DoCast), 2000);
			}
			return 0;
		}
		protected int DoCast(ECSGameTimer timer)
		{
			if (IsAlive)
				new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(Show_Effect), 2000);
			return 0;
		}
		#endregion
		public override void Die(GameObject killer)
        {
			int respawnTime = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;
			new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(SpawnFireBlight), respawnTime);
            base.Die(killer);
        }
		private int SpawnFireBlight(ECSGameTimer timer)
        {
			BlightControllerBrain.CreateLateBlight = false;
			BlightControllerBrain.CreateFleshBlight = false;
			BlightControllerBrain.CreateBlight = false;
			FireBlight.FireBlightCount = 0;
			LateBlight.LateBlightCount = 0;
			FleshBlight.FleshBlightCount = 0;

			for (int i = 0; i < 8; i++)
			{
				foreach (GameNPC npc in GetNPCsInRadius(8000))
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
				if (ad != null && (ad.AttackResult == eAttackResult.HitUnstyled || ad.AttackResult == eAttackResult.HitStyle))
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
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.Power = 0;
					spell.RecastDelay = 2;
					spell.ClientEffect = 360;
					spell.Icon = 360;
					spell.Damage = 400;
					spell.DamageType = (int)eDamageType.Heat;
					spell.Name = "Fire Strike";
					spell.Range = 500;
					spell.SpellID = 11899;
					spell.Target = "Enemy";
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					m_BlightDD = new Spell(spell, 60);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_BlightDD);
				}
				return m_BlightDD;
			}
		}
		#endregion
	}
}
namespace DOL.AI.Brain
{
	public class BlightBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public BlightBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 800;
			ThinkInterval = 1500;
		}

		public override void Think()
		{
			if (!HasAggressionTable())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
			}
			base.Think();
		}
	}
}
//////////////////////////////////////////////////////////////////////Fire Blight////////////////////////////////
#region Fire Blight
namespace DOL.GS
{
	public class FireBlight : GameNPC
	{
		public FireBlight() : base()
		{
		}
		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 15;// dmg reduction for melee dmg
				case eDamageType.Crush: return 15;// dmg reduction for melee dmg
				case eDamageType.Thrust: return 15;// dmg reduction for melee dmg
				default: return 15;// dmg reduction for rest resists
			}
		}
		public override double AttackDamage(InventoryItem weapon)
		{
			return base.AttackDamage(weapon) * Strength / 100;
		}
		public override int MaxHealth
		{
			get { return 2500; }
		}
		public override double GetArmorAF(eArmorSlot slot)
		{
			return 200;
		}
		public override double GetArmorAbsorb(eArmorSlot slot)
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
			base.AddToWorld();
			return true;
		}
        public override void Die(GameObject killer)
        {
			++FireBlightCount;
			base.Die(killer);
        }
    }
}
namespace DOL.AI.Brain
{
	public class FireBlightBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public FireBlightBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 800;
			ThinkInterval = 1500;
		}
		public override void Think()
		{
			if (HasAggro && Body.TargetObject != null)
			{
				foreach (GameNPC npc in Body.GetNPCsInRadius(5000))
				{
					if (npc != null && npc.IsAlive && npc != Body && npc.Brain is FireBlightBrain brain)
					{
						GameLiving target = Body.TargetObject as GameLiving;
						if (!brain.HasAggro && brain != Body.Brain && target != null && target.IsAlive)
							brain.AddToAggroList(target, 10);
					}
				}
			}
			base.Think();
		}
	}
}
#endregion

#region Late Blight
namespace DOL.GS
{
	public class LateBlight : GameNPC
	{
		public LateBlight() : base()
		{
		}
		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 15;// dmg reduction for melee dmg
				case eDamageType.Crush: return 15;// dmg reduction for melee dmg
				case eDamageType.Thrust: return 15;// dmg reduction for melee dmg
				default: return 15;// dmg reduction for rest resists
			}
		}
		public override double AttackDamage(InventoryItem weapon)
		{
			return base.AttackDamage(weapon) * Strength / 100;
		}
		public override int MaxHealth
		{
			get { return 5000; }
		}
		public override double GetArmorAF(eArmorSlot slot)
		{
			return 200;
		}
		public override double GetArmorAbsorb(eArmorSlot slot)
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
			base.AddToWorld();
			return true;
		}
		public override void Die(GameObject killer)
		{
			++LateBlightCount;
			base.Die(killer);
		}
	}
}
namespace DOL.AI.Brain
{
	public class LateBlightBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public LateBlightBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 800;
			ThinkInterval = 1500;
		}
		public override void Think()
		{
			if (HasAggro && Body.TargetObject != null)
			{
				foreach (GameNPC npc in Body.GetNPCsInRadius(5000))
				{
					if (npc != null && npc.IsAlive && npc != Body && npc.Brain is LateBlightBrain brain)
					{
						GameLiving target = Body.TargetObject as GameLiving;
						if (!brain.HasAggro && brain != Body.Brain && target != null && target.IsAlive)
							brain.AddToAggroList(target, 10);
					}
				}
			}
			base.Think();
		}
	}
}
#endregion

#region Flesh Blight
namespace DOL.GS
{
	public class FleshBlight : GameNPC
	{
		public FleshBlight() : base()
		{
		}
		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 15;// dmg reduction for melee dmg
				case eDamageType.Crush: return 15;// dmg reduction for melee dmg
				case eDamageType.Thrust: return 15;// dmg reduction for melee dmg
				default: return 15;// dmg reduction for rest resists
			}
		}
		public override double AttackDamage(InventoryItem weapon)
		{
			return base.AttackDamage(weapon) * Strength / 100;
		}
		public override int MaxHealth
		{
			get { return 10000; }
		}
		public override double GetArmorAF(eArmorSlot slot)
		{
			return 200;
		}
		public override double GetArmorAbsorb(eArmorSlot slot)
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
			Level = (byte)Util.Random(62, 65);
			Size = 100;
			RespawnInterval = -1;
			RoamingRange = 200;

			LoadedFromScript = true;
			FleshBlightBrain sbrain = new FleshBlightBrain();
			SetOwnBrain(sbrain);
			base.AddToWorld();
			return true;
		}
		public override void Die(GameObject killer)
		{
			++FleshBlightCount;
			base.Die(killer);
		}
	}
}
namespace DOL.AI.Brain
{
	public class FleshBlightBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public FleshBlightBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 800;
			ThinkInterval = 1500;
		}
		public override void Think()
		{
			if (HasAggro && Body.TargetObject != null)
			{
				foreach (GameNPC npc in Body.GetNPCsInRadius(5000))
				{
					if (npc != null && npc.IsAlive && npc != Body && npc.Brain is FleshBlightBrain brain)
					{
						GameLiving target = Body.TargetObject as GameLiving;
						if (!brain.HasAggro && brain != Body.Brain && target != null && target.IsAlive)
							brain.AddToAggroList(target, 10);
					}
				}
			}
			base.Think();
		}
	}
}
#endregion

/// <summary>
/// //////////////////////////////////////////////////////////////Blight Controller/////////////////////////////
/// </summary>
#region Blight Controller - control when and what kind of blights will spawn
namespace DOL.GS
{
	public class BlightController : GameNPC
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
			Flags = (GameNPC.eFlags)28;
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

			foreach (GameNPC npc in GetNPCsInRadius(8000))
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
}

namespace DOL.AI.Brain
{
	public class BlightControllerBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log =
			log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public BlightControllerBrain()
			: base()
		{
			AggroLevel = 0; //neutral
			AggroRange = 0;
			ThinkInterval = 1000;
		}
		public static bool CreateLateBlight = false;
		public static bool CreateFleshBlight = false;
		public static bool CreateBlight = false;

		public override void Think()
		{
			if(FireBlight.FireBlightCount == 8)
				SpawnLateBlight();
			if (LateBlight.LateBlightCount == 4)
				SpawnFleshBlight();
			if (FleshBlight.FleshBlightCount == 2)
				SpawnBlight();
			base.Think();
		}
		public void SpawnLateBlight()
		{
			foreach (GameNPC npc in Body.GetNPCsInRadius(8000))
			{
				if (npc.Brain is LateBlightBrain)
					return;
			}
			if (FireBlight.FireBlightCount == 8 && !CreateLateBlight)
			{
				for (int i = 0; i < 4; i++)
				{
					LateBlight boss = new LateBlight();
					boss.X = Body.X + Util.Random(-500, 500);
					boss.Y = Body.Y + Util.Random(-500, 500);
					boss.Z = Body.Z;
					boss.Heading = Body.Heading;
					boss.CurrentRegion = Body.CurrentRegion;
					boss.AddToWorld();
				}
				CreateLateBlight = true;
			}
		}
		public void SpawnFleshBlight()
		{
			foreach (GameNPC npc in Body.GetNPCsInRadius(8000))
			{
				if (npc.Brain is FleshBlightBrain)
					return;
			}
			if (LateBlight.LateBlightCount == 4 && FireBlight.FireBlightCount == 8 && !CreateFleshBlight)
			{
				for (int i = 0; i < 2; i++)
				{
					FleshBlight boss = new FleshBlight();
					boss.X = Body.X + Util.Random(-500, 500);
					boss.Y = Body.Y + Util.Random(-500, 500);
					boss.Z = Body.Z;
					boss.Heading = Body.Heading;
					boss.CurrentRegion = Body.CurrentRegion;
					boss.AddToWorld();
				}
				CreateFleshBlight = true;
			}
		}
		public void SpawnBlight()
		{
			foreach (GameNPC npc in Body.GetNPCsInRadius(8000))
			{
				if (npc.Brain is BlightBrain)
					return;
			}
			if (FleshBlight.FleshBlightCount == 2 && LateBlight.LateBlightCount == 4 && FireBlight.FireBlightCount == 8 && !CreateBlight)
			{
				Blight boss = new Blight();
				boss.X = Body.X + Util.Random(-500, 500);
				boss.Y = Body.Y + Util.Random(-500, 500);
				boss.Z = Body.Z;
				boss.Heading = Body.Heading;
				boss.CurrentRegion = Body.CurrentRegion;
				boss.AddToWorld();
				CreateBlight = true;
			}
		}
	}
}
#endregion