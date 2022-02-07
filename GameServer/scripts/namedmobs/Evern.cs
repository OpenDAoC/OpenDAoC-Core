using System;
using System.Collections;
using DOL.AI.Brain;
using DOL.Events;
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.Styles;
using DOL.GS.SkillHandler;
using DOL.GS.Spells;

namespace DOL.GS
{
	public class Evern : GameNPC
	{
		public Evern() : base() { }
		public static GameNPC SI_Gnat = new GameNPC();
		public override bool AddToWorld()
		{
			Model = 400;
			Name = "Evern";
			Size = 120;
			Level = (byte)Util.Random(70, 75);
			Gender = eGender.Neutral;
			TetherRange = 1700;//important for fairy heals and mechanic
			Flags = eFlags.GHOST;

			EvernBrain sBrain = new EvernBrain();
			SetOwnBrain(sBrain);
			sBrain.AggroLevel = 100;
			sBrain.AggroRange = 500;
			base.AddToWorld();
			return true;
		}
	}
}
namespace DOL.AI.Brain
{
	public class EvernBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public EvernBrain() : base() { }

		public override void Think()
		{
			if (Body.InCombat == true && Body.IsAlive && HasAggro)
			{
				if (Body.TargetObject != null)
				{
					if(Body.HealthPercent<100)
                    {
						if(Util.Chance(10))
                        {
						  new RegionTimer(Body, new RegionTimerCallback(DoSpawn), 5000);
						}
                    }
				}
			}
			if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000) && !HasAggro)
			{
				this.Body.Health = this.Body.MaxHealth;

				foreach (GameNPC npc in Body.GetNPCsInRadius(4500))
				{
					if (npc == null) break;
					if (npc.Brain is EvernFairyBrain)
					{
						if (npc.RespawnInterval == -1)
						{
							npc.Die(npc);//we kill all fairys if boss reset
						}
					}
				}
			}
			if (Body.IsOutOfTetherRange)//important he must be engaged in his "lair" else it will not work, reset method if he is too far
            {
				this.Body.Health = this.Body.MaxHealth;
				Body.MoveTo(Body.CurrentRegionID, Body.SpawnPoint.X, Body.SpawnPoint.Y, Body.SpawnPoint.Z, 200);
				ClearAggroList();

				foreach (GameNPC npc in Body.GetNPCsInRadius(4500))
				{
					if (npc == null) break;
					if (npc.Brain is EvernFairyBrain)
					{
						if (npc.RespawnInterval == -1)
						{
							npc.Die(npc);//we kill all fairys if boss reset
						}
					}
				}
			}
			base.Think();
		}
		private int DoSpawn(RegionTimer timer)
		{
			Spawn();
			return 0;
		}
		public void Spawn() // We define here adds
		{
			EvernFairy Add = new EvernFairy();
			Add.X = 429764;
			Add.Y = 380398;
			Add.Z = 2726;
			Add.CurrentRegionID = 200;
			Add.Heading = 3889;
			Add.AddToWorld();
		}
		
	}
}

///////////////////////////////////Evern Fairys//////////////////////////////////23stones
namespace DOL.GS
{
	public class EvernFairy : GameNPC
	{
		public EvernFairy() : base() { }
		public static GameNPC OF_EvernFairy = new GameNPC();
		public override int MaxHealth
		{
			get { return 1500 * Constitution / 100; }
		}

		public override bool AddToWorld()
		{
			Model = 603;
			Name = "Wraith Fairy";
			MeleeDamageType = eDamageType.Thrust;
			Constitution = 100;
			RespawnInterval = -1;
			Size = 50;
			Flags = eFlags.FLYING;
			Level = (byte)Util.Random(50, 52);
			Gender = eGender.Female;
			EvernFairyBrain adds = new EvernFairyBrain();
			SetOwnBrain(adds);
			base.AddToWorld();
			return true;
		}
	}
}
namespace DOL.AI.Brain
{
	public class EvernFairyBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public EvernFairyBrain()
			: base()
		{
			AggroLevel = 100;
			AggroRange = 0;
		}
		
		private int HealingEffectTimer(RegionTimer timer)
		{
			foreach (GamePlayer ppl in Body.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
			{
				foreach (GameNPC evern in Body.GetNPCsInRadius(1800))
				{
					if (evern != null && evern.IsAlive == true && evern.Brain is EvernBrain)
					{
						Body.TurnTo(evern,true);
						ppl.Out.SendSpellEffectAnimation(Body, evern, 1414, 0, false, 0x01);//finished heal effect
						evern.Health += Body.MaxHealth / 5;
						if(healcheck1==true)
                        {
							healcheck1 = false;
						}
						if (healcheck2 == true)
						{
							healcheck2 = false;
						}
						if (healcheck3 == true)
						{
							healcheck3 = false;
						}
						if (healcheck4 == true)
						{
							healcheck4 = false;
						}
						if (healcheck5 == true)
						{
							healcheck5 = false;
						}
						if (healcheck6 == true)
						{
							healcheck6 = false;
						}
						if (healcheck7 == true)
						{
							healcheck7 = false;
						}
						if (healcheck8 == true)
						{
							healcheck8 = false;
						}
						if (healcheck9 == true)
						{
							healcheck9 = false;
						}
						if (healcheck10 == true)
						{
							healcheck10 = false;
						}
					}					
				}
			}
			return 0;
		}
		private int CastingHealEffect(RegionTimer timer)
		{
			foreach (GamePlayer ppl in Body.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
			{
				foreach (GameNPC evern in Body.GetNPCsInRadius(1800))
				{
					if (evern != null && evern.IsAlive == true && evern.Brain is EvernBrain)
					{
						Body.TurnTo(evern, true);
						if (evern.HealthPercent < 100)
						{
							ppl.Out.SendSpellCastAnimation(Body, 1414, 4);//casting heal effect
							new RegionTimer(Body, new RegionTimerCallback(HealingEffectTimer), 3000);
						}
					}
				}
			}
			return 0;
		}

		public static bool healcheck1 = false;
		public static bool healcheck2 = false;
		public static bool healcheck3 = false;
		public static bool healcheck4 = false;
		public static bool healcheck5 = false;
		public static bool healcheck6 = false;
		public static bool healcheck7 = false;
		public static bool healcheck8 = false;
		public static bool healcheck9 = false;
		public static bool healcheck10 = false;
		public override void Think()
		{
			Point3D point1 = new Point3D();
			point1.X = 430333; point1.Y = 379905; point1.Z = 2463;
			Point3D point2 = new Point3D();
			point2.X = 429814; point2.Y = 379895; point2.Z = 2480;
			Point3D point3 = new Point3D();
			point3.X = 429309; point3.Y = 379894; point3.Z = 2454;
			Point3D point4 = new Point3D();
			point4.X = 430852; point4.Y = 380150; point4.Z = 2444;
			Point3D point5 = new Point3D();
			point5.X = 428801; point5.Y = 380156; point5.Z = 2428;
			Point3D point6 = new Point3D();
			point6.X = 430854; point6.Y = 380680; point6.Z = 2472;
			Point3D point7 = new Point3D();
			point7.X = 429186; point7.Y = 380418; point7.Z = 2478;
			Point3D point8 = new Point3D();
			point8.X = 430462; point8.Y = 380411; point8.Z = 2443;
			Point3D point9 = new Point3D();
			point9.X = 430468; point9.Y = 380813; point9.Z = 2474;
			Point3D point10 = new Point3D();
			point10.X = 429057; point10.Y = 380920; point10.Z = 2452;

			
			
			if (Body.IsAlive == true)
            {
                #region PickRandomLandSpot
                int rand = Util.Random(1, 10);
				switch (rand)
                {
					case 1:
                        {
							if(!Body.IsMoving)
							Body.WalkTo(point1, 60);
                        }break;
					case 2:
                        {
							if(!Body.IsMoving)
							Body.WalkTo(point2, 60);
						}
						break;
					case 3:
						{
							if (!Body.IsMoving)
								Body.WalkTo(point3, 60);
						}
						break;
					case 4:
						{
							if (!Body.IsMoving)
								Body.WalkTo(point4, 60);
						}
						break;
					case 5:
						{
							if (!Body.IsMoving)
								Body.WalkTo(point5, 60);
						}
						break;
					case 6:
						{
							if (!Body.IsMoving)
								Body.WalkTo(point6, 60);
						}
						break;
					case 7:
						{
							if (!Body.IsMoving)
								Body.WalkTo(point7, 60);
						}
						break;
					case 8:
						{
							if (!Body.IsMoving)
								Body.WalkTo(point8, 60);
						}
						break;
					case 9:
						{
							if (!Body.IsMoving)
								Body.WalkTo(point9, 60);
						}
						break;
					case 10:
						{
							if (!Body.IsMoving)
								Body.WalkTo(point10, 60);
						}
						break;
				}
				if (Body.IsWithinRadius(point1, 15))
				{
					Body.MaxSpeedBase = 0;
					Body.StopMovingAt(point1);
					Body.IsReturningHome = false;
					Body.CancelWalkToSpawn();
					if (!Body.IsCasting && healcheck1==false)
					{
						new RegionTimer(Body, new RegionTimerCallback(CastingHealEffect), 3000);
						healcheck1 = true;
					}
				}
				
				if (Body.IsWithinRadius(point2, 15))
				{
					Body.MaxSpeedBase = 0;
					Body.StopMovingAt(point2);
					Body.IsReturningHome = false;
					Body.CancelWalkToSpawn();
					if (!Body.IsCasting && healcheck2 == false)
					{
						new RegionTimer(Body, new RegionTimerCallback(CastingHealEffect), 3000);
						healcheck2 = true;
					}
				}
				if (Body.IsWithinRadius(point3, 15))
				{
					Body.MaxSpeedBase = 0;
					Body.StopMovingAt(point3);
					Body.IsReturningHome = false;
					Body.CancelWalkToSpawn();
					if (!Body.IsCasting && healcheck3 == false)
					{
						new RegionTimer(Body, new RegionTimerCallback(CastingHealEffect), 3000); 
						healcheck3 = true;
					}
				}
				if (Body.IsWithinRadius(point4, 15))
				{
					Body.MaxSpeedBase = 0;
					Body.StopMovingAt(point4);
					Body.IsReturningHome = false;
					Body.CancelWalkToSpawn();
					if (!Body.IsCasting && healcheck4 == false)
					{
						new RegionTimer(Body, new RegionTimerCallback(CastingHealEffect), 3000);
						healcheck4 = true;
					}
				}
				if (Body.IsWithinRadius(point5, 15))
				{
					Body.MaxSpeedBase = 0;
					Body.StopMovingAt(point5);
					Body.IsReturningHome = false;
					Body.CancelWalkToSpawn();
					if (!Body.IsCasting && healcheck5 == false)
					{
						new RegionTimer(Body, new RegionTimerCallback(CastingHealEffect), 3000);
						healcheck5 = true;
					}
				}
				if (Body.IsWithinRadius(point6, 15))
				{
					Body.MaxSpeedBase = 0;
					Body.StopMovingAt(point6);
					Body.IsReturningHome = false;
					Body.CancelWalkToSpawn();
					if (!Body.IsCasting && healcheck6 == false)
					{
						new RegionTimer(Body, new RegionTimerCallback(CastingHealEffect), 3000);
						healcheck6 = true;
					}
				}
				if (Body.IsWithinRadius(point7, 15))
				{
					Body.MaxSpeedBase = 0;
					Body.StopMovingAt(point7);
					Body.IsReturningHome = false;
					Body.CancelWalkToSpawn();
					if (!Body.IsCasting && healcheck7 == false)
					{
						new RegionTimer(Body, new RegionTimerCallback(CastingHealEffect), 3000);
						healcheck7 = true;
					}
				}
				if (Body.IsWithinRadius(point8, 15))
				{
					Body.MaxSpeedBase = 0;
					Body.StopMovingAt(point8);
					Body.IsReturningHome = false;
					Body.CancelWalkToSpawn();
					if (!Body.IsCasting && healcheck8 == false)
					{
						new RegionTimer(Body, new RegionTimerCallback(CastingHealEffect), 3000);
						healcheck8 = true;
					}
				}
				if (Body.IsWithinRadius(point9, 15))
				{
					Body.MaxSpeedBase = 0;
					Body.StopMovingAt(point9);
					Body.IsReturningHome = false;
					Body.CancelWalkToSpawn();
					if (!Body.IsCasting && healcheck9 == false)
					{
						new RegionTimer(Body, new RegionTimerCallback(CastingHealEffect), 3000);
						healcheck9 = true;
					}
				}
				if (Body.IsWithinRadius(point10, 15))
				{
					Body.MaxSpeedBase = 0;
					Body.StopMovingAt(point10);
					Body.IsReturningHome = false;
					Body.CancelWalkToSpawn();
					if (!Body.IsCasting && healcheck10 == false)
					{
						new RegionTimer(Body, new RegionTimerCallback(CastingHealEffect), 3000);
						healcheck10=true;
					}
				}
                #endregion
            }
            base.Think();
		}
	}
}