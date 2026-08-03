using System.Numerics;
using DOL.AI.Brain;
using DOL.GS;
using DOL.GS.PacketHandler;
using OpenDAoC.Pathing;
using static DOL.GS.Pathfinder;

namespace DOL.GS
{
	public class Ick : GameNPC
	{
		public Ick() : base() { }

		public override bool AddToWorld()
		{
			foreach (GameNPC npc in GetNPCsInRadius(5000))
			{
				if (npc != null && npc.IsAlive && npc.Brain is IckAddBrain)
					npc.RemoveFromWorld();
			}

			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60162371);
			LoadTemplate(npcTemplate);

			SetOwnBrain(new IckBrain());
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			return base.AddToWorld();
		}

		public override void ProcessDeath(GameObject killer)
		{
			Message.MessageToArea(this, "Ick bursts apart, and a writhing knot of worms spills out!", eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow, WorldMgr.VISIBILITY_DISTANCE);
			SpawnWorms();
			base.ProcessDeath(killer);
		}

		private void SpawnWorms()
		{
			Vector3 position = new(X, Y, Z);
			Zone zone = CurrentZone;
			bool usePathfinding = zone != null && zone.IsPathfindingEnabled;
			EDtPolyFlags[] filters = usePathfinding ? PathfindingProvider.Instance.DefaultFilters : null;

			for (int i = 0; i < 10; i++)
			{
				// Pick positions on the navmesh whenever possible, so that worms can't spawn inside walls.
				Vector3 spawnPoint = usePathfinding ?
					PathfindingProvider.Instance.GetRandomPoint(zone, position, 100, filters) ?? position :
					new(X + Util.Random(-100, 100), Y + Util.Random(-100, 100), Z);

				IckAdd npc = new IckAdd();
				npc.X = (int) spawnPoint.X;
				npc.Y = (int) spawnPoint.Y;
				npc.Z = (int) spawnPoint.Z;
				npc.Heading = Heading;
				npc.CurrentRegion = CurrentRegion;
				npc.AddToWorld();
			}
		}

		public override void DealDamage(AttackData ad)
		{
			if (ad != null && ad.AttackType == AttackData.eAttackType.Spell && ad.Damage > 0 && Brain is IckBrain brain && brain.IsLeeching)
				ChangeHealth(this, eHealthChangeType.Spell, ad.Damage);

			base.DealDamage(ad);
		}
	}

	public class IckAdd : GameNPC
	{
		private const int DESPAWN_DELAY = 180000; // Worms burrow away if they're left alone.
		private const int DESPAWN_RETRY_INTERVAL = 30000;

		public IckAdd() : base() { }

		public override bool AddToWorld()
		{
			Name = "Ick worm";
			Level = (byte) Util.Random(17, 19);
			Model = 458;
			Size = 17;
			SetOwnBrain(new IckAddBrain());
			LoadedFromScript = true;
			RespawnInterval = -1;
			new ECSGameTimer(this, Despawn, DESPAWN_DELAY);
			return base.AddToWorld();
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

namespace DOL.AI.Brain
{
	public class IckBrain : StandardMobBrain
	{
		private const int LIFE_LEECH_INTERVAL = 20000;

		private ECSGameTimer _lifeLeechTimer;
		private byte _normalSize;

		public IckBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 400;
			ThinkInterval = 1500;
		}

		public bool IsLeeching { get; private set; }

		public override bool Start()
		{
			if (!base.Start())
				return false;

			_lifeLeechTimer ??= new ECSGameTimer(Body, LifeLeechTick);
			_lifeLeechTimer.Start(LIFE_LEECH_INTERVAL);
			return true;
		}

		public override bool Stop()
		{
			if (!base.Stop())
				return false;

			_lifeLeechTimer?.Stop();
			_lifeLeechTimer = null;

			if (IsLeeching)
				EndLifeLeech();

			return true;
		}

		public override void Think()
		{
			if (IsLeeching && HasAggro && Body.TargetObject != null)
				TryCastSpell(IckDD, 100);

			base.Think();
		}

		private int LifeLeechTick(ECSGameTimer timer)
		{
			if (IsLeeching)
			{
				EndLifeLeech();
				Message.MessageToArea(Body, $"{Body.Name}'s stolen life energy fades and its body returns to normal.", eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow, WorldMgr.VISIBILITY_DISTANCE);
			}
			else if (HasAggro && Body.TargetObject != null)
			{
				Message.MessageToArea(Body, "Ick grows in size, drinking in stolen life; his wounds closing as he drains!", eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow, WorldMgr.VISIBILITY_DISTANCE);
				_normalSize = Body.Size;
				Body.Size = 50;
				IsLeeching = true;
			}

			return LIFE_LEECH_INTERVAL;
		}

		private void EndLifeLeech()
		{
			IsLeeching = false;
			Body.Size = _normalSize;
		}

		private static Spell IckDD => ScriptSpells.GetOrCreate("IckLifeDrain", 20, static db =>
		{
			db.CastTime = 0;
			db.Power = 0;
			db.RecastDelay = Util.Random(5, 8);
			db.ClientEffect = 581;
			db.Icon = 581;
			db.Damage = 80;
			db.DamageType = (int) eDamageType.Body;
			db.Name = "LifeDrain";
			db.Range = 1500;
			db.SpellID = 11945;
			db.Target = eSpellTarget.ENEMY.ToString();
			db.Type = eSpellType.DirectDamageNoVariance.ToString();
			db.Uninterruptible = true;
			db.MoveCast = true;
		});
	}

	public class IckAddBrain : StandardMobBrain
	{
		public IckAddBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 1500;
			ThinkInterval = 1500;
		}
	}
}
