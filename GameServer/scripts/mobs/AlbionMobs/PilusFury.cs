using DOL.AI.Brain;
using DOL.GS.PacketHandler;
using System;
using DOL.GS;
using DOL.Database;
using System.Collections.Generic;
using System.Linq;

namespace DOL.GS
{
	public class PilusFury : GameNPC
	{
		public PilusFury() : base() { }
		#region Stats
		public override short Intelligence { get => base.Intelligence; set => base.Intelligence = 200; }
		public override short Dexterity { get => base.Dexterity; set => base.Dexterity = 200; }
		#endregion
		public override bool AddToWorld()
		{
			Name = "Pilus'Fury";
			Model = 665;
			Level = (byte)Util.Random(65, 70);
			Size = 50;
			Flags = (GameNPC.eFlags)44;//notarget noname flying
			MaxSpeedBase = 0;
			RespawnInterval = -1;

			PilusFuryBrain sbrain = new PilusFuryBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = true;
			base.AddToWorld();
			return true;
		}
		public void BroadcastMessage(String message)
		{
			foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
			{
				player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow);
			}
		}
		public override void StartAttack(GameObject target)
		{
		}
		public override bool IsVisibleToPlayers => true;
	}
}
namespace DOL.AI.Brain
{
	public class PilusFuryBrain : APlayerVicinityBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private List<Point3D> _points = new List<Point3D>();

		public PilusFuryBrain() : base()
		{
			ThinkInterval = 1500;
			
			_points.Add(new Point3D(33374, 42009, 15007));//400 range
			_points.Add(new Point3D(33376, 41368, 15007));//200
			_points.Add(new Point3D(33374, 40973, 15007));//200
			_points.Add(new Point3D(33369, 40587, 15007));//200
			_points.Add(new Point3D(33370, 40194, 15007));//400
			_points.Add(new Point3D(33372, 39597, 15007));//200
			_points.Add(new Point3D(33368, 39196, 15007));//200
			_points.Add(new Point3D(33374, 38797, 15007));//200
			_points.Add(new Point3D(33370, 38653, 15007));//200
			_points.Add(new Point3D(33368, 38257, 15007));//200
			_points.Add(new Point3D(33364, 37859, 15007));//200
			_points.Add(new Point3D(33365, 37667, 15007));//200
		}
		
		List<GameLiving> DD_Enemys = new List<GameLiving>();
		private bool CanDD = false;
		
		public override void Think()
		{
			if(Body.IsAlive)
			{
				foreach(GamePlayer player in Body.GetPlayersInRadius(10000))
	            {
		            HandlePlayerCheck(player);
	            }
				foreach(GameNPC npc in Body.GetNPCsInRadius(10000))
				{
					HandleNpcCheck(npc);
				}
				
				if (!CanDD && DD_Enemys.Count > 0)
				{
					new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(PrepareDD), 3000);
					CanDD = true;
				}
			}
		}

		public override void KillFSM()
		{
			
		}

		private void HandlePlayerCheck(GamePlayer player)
		{
			if (player is {IsAlive: true} && player.Client.Account.PrivLevel == 1)
			{
				var nearbyPoint = _points.FirstOrDefault(point => ((point == _points[0] || point == _points[4]) && player.IsWithinRadius(point, 400)) 
				                                          || player.IsWithinRadius(point, 200));

				if (nearbyPoint != null)
				{
					if (player.CharacterClass.ID == (int) eCharacterClass.Necromancer && player.ControlledBrain != null)
					{
						if (player.ControlledBrain.Body != null)
						{
							NecromancerPet pet = (NecromancerPet) player.ControlledBrain.Body;
							HandleNpcCheck(pet);
						}
					}
					else
					{
						if (!DD_Enemys.Contains(player))
						{
							DD_Enemys.Add(player);
							player.Out.SendMessage("Smoke seeps up through the cracks in the hall's floor.",
								eChatType.CT_Broadcast, eChatLoc.CL_ChatWindow);
						}
					}
				}
				else 
				{
					if (player.CharacterClass.ID == (int) eCharacterClass.Necromancer && player.ControlledBrain != null)
					{
						if (player.ControlledBrain.Body != null && DD_Enemys.Contains(player.ControlledBrain.Body))
						{
							DD_Enemys.Remove(player.ControlledBrain.Body);
						}
					}

					if (DD_Enemys.Contains(player))
					{
						DD_Enemys.Remove(player);
					}
				}
			}

			if (player != null && player.Client.Account.PrivLevel != 1)
			{
				if (player.CharacterClass.ID == (int) eCharacterClass.Necromancer && player.ControlledBrain != null)
				{
					if (player.ControlledBrain.Body != null)
					{
						NecromancerPet pet = (NecromancerPet) player.ControlledBrain.Body;
						if (pet != null && DD_Enemys.Contains(pet))
							DD_Enemys.Remove(pet);
					}
				}
				else
				{
					if (DD_Enemys.Contains(player))
						DD_Enemys.Remove(player);
				}
			}

			if (player?.CharacterClass.ID == (int) eCharacterClass.Necromancer && player.ControlledBrain != null)
			{
				NecromancerPet pet = (NecromancerPet) player.ControlledBrain.Body;
				if (pet is {IsAlive: false} && DD_Enemys.Contains(pet))
					DD_Enemys.Remove(pet);
			}
			else
			{
				if (player is {IsAlive: false} && DD_Enemys.Contains(player))
					DD_Enemys.Remove(player);
			}
		}

		private void HandleNpcCheck(GameNPC npc)
		{
			if (npc is {IsAlive: true} and GameSummonedPet pet)
			{
				GamePlayer playerOwner = pet.Owner as GamePlayer;
				var nearbyPoint = _points.FirstOrDefault(point => ((point == _points[0] || point == _points[4]) && pet.IsWithinRadius(point, 400))
				                                          || pet.IsWithinRadius(point, 200));

				if (nearbyPoint != null)
				{
					if (!DD_Enemys.Contains(pet))
					{
						DD_Enemys.Add(pet);
					}
				}
				else
				{
					if (DD_Enemys.Contains(pet))
					{
						DD_Enemys.Remove(pet);
					}
				}

				if (pet is {IsAlive: false} && DD_Enemys.Contains(pet))
					DD_Enemys.Remove(pet);
				if (playerOwner is {IsAlive: true} && playerOwner.Client.Account.PrivLevel != 1 &&
				    pet.IsAlive && DD_Enemys.Contains(pet))
					DD_Enemys.Remove(pet);
			}
		}

		private int PrepareDD(ECSGameTimer timer)
        {
			if (DD_Enemys.Count > 0)
			{
				foreach (GameLiving targets in DD_Enemys)
				{
					if (targets.IsAlive && targets != null)
						DamageTarget(targets, Body);
				}
			}
			new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(ResetDD), 2000);
			return 0;
        }
		private int ResetDD(ECSGameTimer timer)
        {
			CanDD = false;
			return 0;
        }
		private protected void DamageTarget(GameLiving target, GameNPC caster)
        {
			AttackData ad = new AttackData();
			ad.AttackResult = eAttackResult.HitUnstyled;
			ad.Attacker = caster;
			ad.Target = target;
			ad.DamageType = eDamageType.Heat;
			ad.IsSpellResisted = false;
			ad.Damage = Util.Random(100,250);
			ad.CausesCombat = true;

			foreach (GamePlayer p in target.GetPlayersInRadius(false, WorldMgr.VISIBILITY_DISTANCE))
			{
				p.Out.SendSpellEffectAnimation(caster, target, 5700, 0, false, 1);
				p.Out.SendCombatAnimation(caster, target, 0, 0, 0, 0, 0x14, target.HealthPercent);
			}

			if(target is NecromancerPet pet)
			{
				if (pet != null && pet.Owner.IsAlive && pet.Owner != null)
				{
					GamePlayer PetOwner = pet.Owner as GamePlayer;
					PetOwner.OnAttackedByEnemy(ad);
				}
			}
			target.OnAttackedByEnemy(ad);
			caster.DealDamage(ad);

			if (target is GamePlayer && target != null)//combat timer and interrupt for target
			{
				target.LastAttackTickPvP = GameLoop.GameLoopTime;
				target.LastAttackedByEnemyTickPvP = GameLoop.GameLoopTime;
				target.StartInterruptTimer(GS.ServerProperties.Properties.SPELL_INTERRUPT_DURATION, ad.AttackType, ad.Attacker);
			}
		}		
	}
}

