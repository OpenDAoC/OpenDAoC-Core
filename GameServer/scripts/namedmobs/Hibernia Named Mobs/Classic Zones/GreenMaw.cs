using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using OpenDAoC.Pathing;
using static DOL.GS.Pathfinder;

namespace DOL.GS
{
	public class GreenMaw : GameEpicNPC
	{
		public GreenMaw() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Green Maw Initializing...");
		}
		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 20;// dmg reduction for melee dmg
				case eDamageType.Crush: return 20;// dmg reduction for melee dmg
				case eDamageType.Thrust: return 20;// dmg reduction for melee dmg
				default: return 20;// dmg reduction for rest resists
			}
		}

		public override int MeleeAttackRange => 350;
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
			get { return 10000; }
		}
		public override bool AddToWorld()
		{
			foreach (GameNPC npc in GetNPCsInRadius(8000))
			{
				if (npc.Brain is GreenMawBrain)
					return false;
			}
			foreach (GameNPC npc in WorldMgr.GetNPCsFromRegion(CurrentRegionID))
			{
				if (npc != null && npc.IsAlive && npc.Brain is GreenMawAddBrain)
					npc.RemoveFromWorld();
			}
			foreach (GameNPC npc in WorldMgr.GetNPCsFromRegion(CurrentRegionID))
			{
				if (npc != null && npc.IsAlive && npc.Brain is GreenMawAdd2Brain)
					npc.RemoveFromWorld();
			}
			foreach (GameNPC npc in WorldMgr.GetNPCsFromRegion(CurrentRegionID))
			{
				if (npc != null && npc.IsAlive && npc.Brain is GreenMawAdd3Brain)
					npc.RemoveFromWorld();
			}
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(50022);
			LoadTemplate(npcTemplate);

			RespawnInterval = ServerProperties.Properties.SET_EPIC_QUEST_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
			GreenMawBrain sbrain = new GreenMawBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
		public override void ProcessDeath(GameObject killer)
		{
			SpawnCopies();
			base.ProcessDeath(killer);
		}
		private void SpawnCopies()
		{
			Vector3 position = new(X, Y, Z);
			Zone zone = CurrentZone;
			bool usePathfinding = zone != null && zone.IsPathfindingEnabled;
			EDtPolyFlags[] filters = usePathfinding ? PathfindingProvider.Instance.DefaultFilters : null;
			GreenMawWave wave = new();

			for (int i = 0; i < 3; i++)
			{
				// Pick positions on the navmesh whenever possible, so that copies can't spawn inside walls.
				Vector3 spawnPoint = usePathfinding ?
					PathfindingProvider.Instance.GetRandomPoint(zone, position, 50, filters) ?? position :
					new(X + Util.Random(-50, 50), Y + Util.Random(-50, 50), Z);

				GreenMawAdd npc = new GreenMawAdd();
				npc.X = (int) spawnPoint.X;
				npc.Y = (int) spawnPoint.Y;
				npc.Z = (int) spawnPoint.Z;
				npc.Heading = Heading;
				npc.CurrentRegion = CurrentRegion;
				npc.Wave = wave;
				wave.Add(npc);
				npc.AddToWorld();
			}
		}
	}

	public class GreenMawWave
	{
		private readonly List<GameNPC> _members = new();
		private int _nextWaveTriggered;

		public void Add(GameNPC npc)
		{
			lock (_members)
				_members.Add(npc);
		}

		public void Remove(GameNPC npc)
		{
			lock (_members)
				_members.Remove(npc);
		}

		public bool RemoveAndCheckLastAlive(GameNPC npc)
		{
			lock (_members)
			{
				_members.Remove(npc);

				if (_members.Count > 0)
					return false;
			}

			return Interlocked.Exchange(ref _nextWaveTriggered, 1) == 0;
		}
	}
}
namespace DOL.AI.Brain
{
	public class GreenMawBrain : StandardMobBrain
	{
		private static readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public GreenMawBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 450;
			ThinkInterval = 1500;
		}

		public override void Think()
		{
			if (!CheckProximityAggro())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
			}
			base.Think();
		}
	}
}

#region Green maw Copies Red
namespace DOL.GS
{
	public class GreenMawAdd : GameNPC
	{
		private const int DESPAWN_DELAY = 180000; // Death-spawned adds despawn if they're left alone.
		private const int DESPAWN_RETRY_INTERVAL = 30000;

		public GreenMawAdd() : base() { }
		public GreenMawWave Wave;
		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 20;// dmg reduction for melee dmg
				case eDamageType.Crush: return 20;// dmg reduction for melee dmg
				case eDamageType.Thrust: return 20;// dmg reduction for melee dmg
				default: return 20;// dmg reduction for rest resists
			}
		}

		public override int MeleeAttackRange => 350;
		public override double GetArmorAF(eArmorSlot slot)
		{
			return 250;
		}
		public override double GetArmorAbsorb(eArmorSlot slot)
		{
			// 85% ABS is cap.
			return 0.10;
		}
		public override int MaxHealth
		{
			get { return 5000; }
		}
		#region Stats
		public override short Dexterity { get => base.Dexterity; set => base.Dexterity = 200; }
		public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
		public override short Strength { get => base.Strength; set => base.Strength = 200; }
		#endregion
		public override bool AddToWorld()
		{
			Name = "Part of Green Maw";
			Level = (byte)Util.Random(58,60);
			Model = 136;
			Size = 120;
			GreenMawAddBrain sbrain = new GreenMawAddBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = true;
			RespawnInterval = -1;
			new ECSGameTimer(this, Despawn, DESPAWN_DELAY);
			base.AddToWorld();
			return true;
		}

		private int Despawn(ECSGameTimer timer)
		{
			if (!IsAlive)
				return 0;

			// Don't despawn mid fight.
			if (InCombat || Brain is StandardMobBrain { HasAggro: true })
				return DESPAWN_RETRY_INTERVAL;

			Wave?.Remove(this);
			RemoveFromWorld();
			return 0;
		}

        public override void ProcessDeath(GameObject killer)
        {
			if (Wave != null && Wave.RemoveAndCheckLastAlive(this))
				SpawnCopies();
			base.ProcessDeath(killer);
        }
		public override bool CanDropLoot => false;
		private void SpawnCopies()
		{
			Vector3 position = new(X, Y, Z);
			Zone zone = CurrentZone;
			bool usePathfinding = zone != null && zone.IsPathfindingEnabled;
			EDtPolyFlags[] filters = usePathfinding ? PathfindingProvider.Instance.DefaultFilters : null;
			GreenMawWave wave = new();

			for (int i = 0; i < 4; i++)
			{
				// Pick positions on the navmesh whenever possible, so that copies can't spawn inside walls.
				Vector3 spawnPoint = usePathfinding ?
					PathfindingProvider.Instance.GetRandomPoint(zone, position, 50, filters) ?? position :
					new(X + Util.Random(-50, 50), Y + Util.Random(-50, 50), Z);

				GreenMawAdd2 npc = new GreenMawAdd2();
				npc.X = (int) spawnPoint.X;
				npc.Y = (int) spawnPoint.Y;
				npc.Z = (int) spawnPoint.Z;
				npc.Heading = Heading;
				npc.CurrentRegion = CurrentRegion;
				npc.Wave = wave;
				wave.Add(npc);
				npc.AddToWorld();
			}
		}
	}
}
namespace DOL.AI.Brain
{
	public class GreenMawAddBrain : StandardMobBrain
	{
		private static readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public GreenMawAddBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 1000;
			ThinkInterval = 1500;
		}
		public override void Think()
		{
			base.Think();
		}
	}
}
#endregion

#region Green maw Copies Orange
namespace DOL.GS
{
	public class GreenMawAdd2 : GameNPC
	{
		private const int DESPAWN_DELAY = 180000; // Death-spawned adds despawn if they're left alone.
		private const int DESPAWN_RETRY_INTERVAL = 30000;

		public GreenMawAdd2() : base() { }
		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 20;// dmg reduction for melee dmg
				case eDamageType.Crush: return 20;// dmg reduction for melee dmg
				case eDamageType.Thrust: return 20;// dmg reduction for melee dmg
				default: return 20;// dmg reduction for rest resists
			}
		}

		public override int MeleeAttackRange => 350;
		public override double GetArmorAF(eArmorSlot slot)
		{
			return 200;
		}
		public override double GetArmorAbsorb(eArmorSlot slot)
		{
			// 85% ABS is cap.
			return 0.10;
		}
		public override int MaxHealth
		{
			get { return 3000; }
		}
		#region Stats
		public override short Dexterity { get => base.Dexterity; set => base.Dexterity = 200; }
		public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
		public override short Strength { get => base.Strength; set => base.Strength = 150; }
		#endregion
		public GreenMawWave Wave;
		public override bool AddToWorld()
		{
			Name = "Part of Green Maw";
			Level = (byte)Util.Random(53, 55);
			Model = 136;
			Size = 95;
			GreenMawAdd2Brain sbrain = new GreenMawAdd2Brain();
			SetOwnBrain(sbrain);
			LoadedFromScript = true;
			RespawnInterval = -1;
			new ECSGameTimer(this, Despawn, DESPAWN_DELAY);
			base.AddToWorld();
			return true;
		}

		private int Despawn(ECSGameTimer timer)
		{
			if (!IsAlive)
				return 0;

			// Don't despawn mid fight.
			if (InCombat || Brain is StandardMobBrain { HasAggro: true })
				return DESPAWN_RETRY_INTERVAL;

			Wave?.Remove(this);
			RemoveFromWorld();
			return 0;
		}

		public override void ProcessDeath(GameObject killer)
		{
			if (Wave != null && Wave.RemoveAndCheckLastAlive(this))
				SpawnCopies();
			base.ProcessDeath(killer);
		}
		public override bool CanDropLoot => false;
		private void SpawnCopies()
		{
			Vector3 position = new(X, Y, Z);
			Zone zone = CurrentZone;
			bool usePathfinding = zone != null && zone.IsPathfindingEnabled;
			EDtPolyFlags[] filters = usePathfinding ? PathfindingProvider.Instance.DefaultFilters : null;

			for (int i = 0; i < 2; i++)
			{
				// Pick positions on the navmesh whenever possible, so that copies can't spawn inside walls.
				Vector3 spawnPoint = usePathfinding ?
					PathfindingProvider.Instance.GetRandomPoint(zone, position, 50, filters) ?? position :
					new(X + Util.Random(-50, 50), Y + Util.Random(-50, 50), Z);

				GreenMawAdd3 npc = new GreenMawAdd3();
				npc.X = (int) spawnPoint.X;
				npc.Y = (int) spawnPoint.Y;
				npc.Z = (int) spawnPoint.Z;
				npc.Heading = Heading;
				npc.CurrentRegion = CurrentRegion;
				npc.AddToWorld();
			}
		}
	}
}
namespace DOL.AI.Brain
{
	public class GreenMawAdd2Brain : StandardMobBrain
	{
		private static readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public GreenMawAdd2Brain() : base()
		{
			AggroLevel = 100;
			AggroRange = 1000;
			ThinkInterval = 1500;
		}
		public override void Think()
		{
			base.Think();
		}
	}
}
#endregion

#region Green maw Copies Yellow
namespace DOL.GS
{
	public class GreenMawAdd3 : GameNPC
	{
		private const int DESPAWN_DELAY = 180000; // Death-spawned adds despawn if they're left alone.
		private const int DESPAWN_RETRY_INTERVAL = 30000;

		public GreenMawAdd3() : base() { }
		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 20;// dmg reduction for melee dmg
				case eDamageType.Crush: return 20;// dmg reduction for melee dmg
				case eDamageType.Thrust: return 20;// dmg reduction for melee dmg
				default: return 20;// dmg reduction for rest resists
			}
		}

		public override int MeleeAttackRange => 350;

		public override double GetArmorAF(eArmorSlot slot)
		{
			return 150;
		}
		public override double GetArmorAbsorb(eArmorSlot slot)
		{
			// 85% ABS is cap.
			return 0.10;
		}
		public override int MaxHealth
		{
			get { return 2500; }
		}
		#region Stats
		public override short Dexterity { get => base.Dexterity; set => base.Dexterity = 200; }
		public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
		public override short Strength { get => base.Strength; set => base.Strength = 150; }
		#endregion
		public override bool AddToWorld()
		{
			Name = "Part of Green Maw";
			Level = 50;
			Model = 136;
			Size = 70;
			GreenMawAdd3Brain sbrain = new GreenMawAdd3Brain();
			SetOwnBrain(sbrain);
			LoadedFromScript = true;
			RespawnInterval = -1;
			new ECSGameTimer(this, Despawn, DESPAWN_DELAY);
			base.AddToWorld();
			return true;
		}

		private int Despawn(ECSGameTimer timer)
		{
			if (!IsAlive)
				return 0;

			// Don't despawn mid fight.
			if (InCombat || Brain is StandardMobBrain { HasAggro: true })
				return DESPAWN_RETRY_INTERVAL;

			RemoveFromWorld();
			return 0;
		}

		public override bool CanDropLoot => false;
	}
}
namespace DOL.AI.Brain
{
	public class GreenMawAdd3Brain : StandardMobBrain
	{
		private static readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public GreenMawAdd3Brain() : base()
		{
			AggroLevel = 100;
			AggroRange = 1000;
			ThinkInterval = 1500;
		}
		public override void Think()
		{
			base.Think();
		}
	}
}
#endregion