using System;
using System.Numerics;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.PacketHandler;
using OpenDAoC.Pathing;
using static DOL.GS.Pathfinder;

namespace DOL.GS
{
	public class Mokkurvalve : GameEpicBoss
	{
		public Mokkurvalve() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Mokkurvalve Initializing...");
		}
		public void BroadcastMessage(String message)
		{
			foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
			{
				player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_ChatWindow);
			}
		}
		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 20;// dmg reduction for melee dmg
				case eDamageType.Crush: return 20;// dmg reduction for melee dmg
				case eDamageType.Thrust: return 20;// dmg reduction for melee dmg
				default: return 30;// dmg reduction for rest resists
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
			get { return 30000; }
		}
		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60164144);
			LoadTemplate(npcTemplate);
			RespawnInterval = ServerProperties.Properties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

			MokkurvalveBrain sbrain = new MokkurvalveBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
        public override void ProcessDeath(GameObject killer)
        {
			BroadcastMessage("Part of " + Name + "'s body falls to the ground.");
			SpawnShardsAfterDeath();
            base.ProcessDeath(killer);
        }
		private void SpawnShardsAfterDeath()
        {
			Vector3 position = new(X, Y, Z);
			Zone zone = CurrentZone;
			bool usePathfinding = zone != null && zone.IsPathfindingEnabled;
			EDtPolyFlags[] filters = usePathfinding ? PathfindingProvider.Instance.DefaultFilters : null;

			for (int i = 0; i < 20; i++)
			{
				// Pick positions on the navmesh whenever possible, so that shards can't spawn inside walls.
				Vector3 spawnPoint = usePathfinding ?
					PathfindingProvider.Instance.GetRandomPoint(zone, position, 200, filters) ?? position :
					new(X + Util.Random(-200, 200), Y + Util.Random(-200, 200), Z);

				MokkurvalveAdds add = new MokkurvalveAdds();
				add.X = (int) spawnPoint.X;
				add.Y = (int) spawnPoint.Y;
				add.Z = (int) spawnPoint.Z;
				add.Heading = Heading;
				add.CurrentRegion = CurrentRegion;
				add.AddToWorld();
			}
		}
    }
}
namespace DOL.AI.Brain
{
	public class MokkurvalveBrain : StandardMobBrain
	{
		private static readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public MokkurvalveBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 600;
			ThinkInterval = 1500;
		}
		public void BroadcastMessage(String message)
		{
			foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
			{
				player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_ChatWindow);
			}
		}
		private bool CanSpawnShard = false;
		private bool RemoveAdds = false;
		public override void Think()
		{
			if (!CheckProximityAggro())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
				CanSpawnShard = false;
				if (!RemoveAdds)
				{
					foreach (GameNPC npc in Body.GetNPCsInRadius(8000))
					{
						if (npc != null && npc.IsAlive && npc.Brain is MokkurvalveAddsBrain)
							npc.Die(Body);
					}
					RemoveAdds = true;
				}
			}
			if (HasAggro && Body.TargetObject != null)
			{
				RemoveAdds = false;
				if(!CanSpawnShard)
                {
					new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(SpawnShards), Util.Random(15000, 35000));
					CanSpawnShard = true;
                }
			}
			base.Think();
		}
		private int SpawnShards(ECSGameTimer timer)
        {
			if (HasAggro && Body.TargetObject != null)
			{
				BroadcastMessage("Part of " + Body.Name + "'s body falls to the ground.");
				MokkurvalveAdds add = new MokkurvalveAdds();
				add.X = Body.X + Util.Random(-200, 200);
				add.Y = Body.Y + Util.Random(-200, 200);
				add.Z = Body.Z;
				add.Heading = Body.Heading;
				add.CurrentRegion = Body.CurrentRegion;
				add.AddToWorld();
			}
			CanSpawnShard = false;
			return 0;
        }
	}
}
////////////////////////////////////////////////////////////adds//////////////////////////////////////////
namespace DOL.GS
{
	public class MokkurvalveAdds : GameNPC
	{
		private const int DESPAWN_DELAY = 180000; // Death-spawned adds despawn if they're left alone.
		private const int DESPAWN_RETRY_INTERVAL = 30000;

		public MokkurvalveAdds() : base() { }
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
			get { return 1500; }
		}
		public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
		public override short Strength { get => base.Strength; set => base.Strength = 150; }
		public override bool AddToWorld()
		{
			Model = 1770;
			Size = (byte)Util.Random(25, 35);
			Name = "Mokkurvalve's shard";
			RespawnInterval = -1;
			Level = (byte)Util.Random(42, 44);
			MaxSpeedBase = 225;

			MokkurvalveAddsBrain sbrain = new MokkurvalveAddsBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = true;
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
		public override long ExperienceValue => 0;
	}
}
namespace DOL.AI.Brain
{
	public class MokkurvalveAddsBrain : StandardMobBrain
	{
		private static readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public MokkurvalveAddsBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 600;
			ThinkInterval = 1500;
		}
		public override void Think()
		{
			base.Think();
		}
	}
}
