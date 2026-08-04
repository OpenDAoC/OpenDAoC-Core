using System;
using System.Collections.Generic;
using System.Numerics;
using DOL.AI.Brain;
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.Events;
using OpenDAoC.Pathing;
using static DOL.GS.Pathfinder;

namespace DOL.GS
{
	public class QueenQunilaria : GameEpicBoss
	{
		public QueenQunilaria() : base() { }
		public readonly List<GameNPC> Minions = new List<GameNPC>();
		public readonly object MinionsLock = new object();

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Queen Qunilaria Initializing...");
		}
		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 20; // dmg reduction for melee dmg
				case eDamageType.Crush: return 20; // dmg reduction for melee dmg
				case eDamageType.Thrust: return 20; // dmg reduction for melee dmg
				default: return 30; // dmg reduction for rest resists
			}
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
		public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
		{
			if (source is GamePlayer || source is GameSummonedPet)
			{
				if (IsOutOfTetherRange)
				{
					if (damageType == eDamageType.Body || damageType == eDamageType.Cold || damageType == eDamageType.Energy || damageType == eDamageType.Heat
						|| damageType == eDamageType.Matter || damageType == eDamageType.Spirit || damageType == eDamageType.Crush || damageType == eDamageType.Thrust
						|| damageType == eDamageType.Slash)
					{
						GamePlayer truc;
						if (source is GamePlayer)
							truc = (source as GamePlayer);
						else
							truc = ((source as GameSummonedPet).Owner as GamePlayer);
						if (truc != null)
							truc.Out.SendMessage(this.Name + " is immune to any damage!", eChatType.CT_System, eChatLoc.CL_ChatWindow);
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

		public override int MeleeAttackRange => 350;
		public override bool HasAbility(string keyName)
		{
			if (IsAlive && keyName == GS.Abilities.CCImmunity)
				return true;

			return base.HasAbility(keyName);
		}
		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60165085);
			LoadTemplate(npcTemplate);
			RespawnInterval = ServerProperties.Properties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

			Faction = FactionMgr.GetFactionByID(93);
			foreach (GameNPC npc in WorldMgr.GetNPCsFromRegion(CurrentRegionID))
			{
				if (npc != null)
				{
					if (npc.IsAlive && npc.Brain is QunilariaAddBrain)
					{
						npc.Die(npc);
					}
				}
			}
			QueenQunilariaBrain sbrain = new QueenQunilariaBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
        public override void ProcessDeath(GameObject killer)
        {
			SpawnAdds();
            base.ProcessDeath(killer);
        }
		public void SpawnAdds()
		{
			Vector3 position = new(X, Y, Z);
			Zone zone = CurrentZone;
			bool usePathfinding = zone != null && zone.IsPathfindingEnabled;
			EDtPolyFlags[] filters = usePathfinding ? PathfindingProvider.Instance.DefaultFilters : null;

			for (int i = 0; i < Util.Random(15,22); i++)
			{
				// Pick positions on the navmesh whenever possible, so that adds can't spawn inside walls.
				Vector3 spawnPoint = usePathfinding ?
					PathfindingProvider.Instance.GetRandomPoint(zone, position, 100, filters) ?? position :
					new(X + Util.Random(-100, 100), Y + Util.Random(-100, 100), Z);

				QunilariaAdd2 Add1 = new QunilariaAdd2();
				Add1.X = (int) spawnPoint.X;
				Add1.Y = (int) spawnPoint.Y;
				Add1.Z = (int) spawnPoint.Z;
				Add1.CurrentRegion = CurrentRegion;
				Add1.Heading = Heading;
				Add1.RespawnInterval = -1;
				Add1.AddToWorld();
				lock (MinionsLock)
					Minions.Add(Add1);
			}
		}
	}
}
namespace DOL.AI.Brain
{
	public class QueenQunilariaBrain : StandardMobBrain
	{
		private static readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public QueenQunilariaBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 600;
			ThinkInterval = 1500;
		}
		public void SpawnAdds()
		{
			if (Body is not QueenQunilaria queen)
				return;

			lock (queen.MinionsLock)
			{
				queen.Minions.RemoveAll(npc => npc == null || !npc.IsAlive || npc.ObjectState is not GameObject.eObjectState.Active);
				int minionCount = queen.Minions.Count;
				for (int i = 0; i < 3; i++)
				{
					if (minionCount < 4)
					{
						QunilariaAdd Add1 = new QunilariaAdd();
						Add1.X = Body.X + Util.Random(-100, 100);
						Add1.Y = Body.Y + Util.Random(-100, 100);
						Add1.Z = Body.Z;
						Add1.CurrentRegion = Body.CurrentRegion;
						Add1.Heading = Body.Heading;
						Add1.RespawnInterval = -1;
						Add1.PackageID = "QunilariaCombatAdd";
						Add1.AddToWorld();
						queen.Minions.Add(Add1);
						++minionCount;
					}
				}
			}
		}
        public override void OnAttackedByEnemy(AttackData ad)
        {
			if(Util.Chance(25))
            {
				SpawnAdds();
            }
            base.OnAttackedByEnemy(ad);
        }
		private bool RemoveAdds = false;
		public override void Think()
		{
			if (!CheckProximityAggro())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
				if (!RemoveAdds)
				{
					foreach (GameNPC npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
					{
						if (npc != null)
						{
							if (npc.IsAlive && npc.Brain is QunilariaAddBrain && npc.PackageID == "QunilariaCombatAdd")
							{
								npc.Die(npc);
							}
						}
					}
					RemoveAdds = true;
				}
			}
			if (Body.TargetObject != null && Body.IsAlive && HasAggro)
				RemoveAdds = false;
			base.Think();
		}
	}
}
/// <summary>
/// //////////////////////////////////////////////////////////////////adds//////////////////////////////////////////////////////////////////
/// </summary>
namespace DOL.AI.Brain
{
	public class QunilariaAddBrain : StandardMobBrain
	{
		public QunilariaAddBrain()
			: base()
		{
			AggroLevel = 100;
			AggroRange = 800;
		}

		public override void Think()
		{
			base.Think();
		}
	}
}
namespace DOL.GS
{
	public class QunilariaAdd : GameNPC
	{
		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 25; // dmg reduction for melee dmg
				case eDamageType.Crush: return 25; // dmg reduction for melee dmg
				case eDamageType.Thrust: return 25; // dmg reduction for melee dmg
				default: return 25; // dmg reduction for rest resists
			}
		}
		public override double GetArmorAF(eArmorSlot slot)
		{
			return 300;
		}
		public override double GetArmorAbsorb(eArmorSlot slot)
		{
			// 85% ABS is cap.
			return 0.20;
		}
		public override int MaxHealth
		{
			get { return 3000; }
		}

		public override bool CanDropLoot => false;
		public override long ExperienceValue => 0;
		public override bool AddToWorld()
		{
			Model = 764;
			Name = "Qunilaria's minion";
			Strength = 150;
			Dexterity = 200;
			Quickness = 100;
			Constitution = 100;
			RespawnInterval = -1;
			MaxSpeedBase = 225;

			Size = (byte)Util.Random(80, 100);
			Level = (byte)Util.Random(58, 64);
			Faction = FactionMgr.GetFactionByID(93);
			QunilariaAddBrain add = new QunilariaAddBrain();
			SetOwnBrain(add);
			base.AddToWorld();
			return true;
		}
	}
}
///////////////////////////////////////////////////////////////////////adds after boss dead////////////////////////////////////////////////////////////
namespace DOL.GS
{
	public class QunilariaAdd2 : GameNPC
	{
		private const int DESPAWN_DELAY = 180000; // Death-spawned adds despawn if they're left alone.
		private const int DESPAWN_RETRY_INTERVAL = 30000;

		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 25; // dmg reduction for melee dmg
				case eDamageType.Crush: return 25; // dmg reduction for melee dmg
				case eDamageType.Thrust: return 25; // dmg reduction for melee dmg
				default: return 25; // dmg reduction for rest resists
			}
		}
		public override double GetArmorAF(eArmorSlot slot)
		{
			return 300;
		}
		public override double GetArmorAbsorb(eArmorSlot slot)
		{
			// 85% ABS is cap.
			return 0.15;
		}
		public override int MaxHealth
		{
			get { return 1200; }
		}
		public override bool CanDropLoot => false;
		public override long ExperienceValue => 0;
		public override bool AddToWorld()
		{
			Model = 764;
			Name = "Qunilaria's minion";
			Strength = 50;
			Dexterity = 150;
			Quickness = 100;
			Constitution = 100;
			RespawnInterval = -1;
			MaxSpeedBase = 200;

			Size = 60;
			Level = (byte)Util.Random(52, 55);
			Faction = FactionMgr.GetFactionByID(93);
			QunilariaAddBrain add = new QunilariaAddBrain();
			SetOwnBrain(add);
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
	}
}