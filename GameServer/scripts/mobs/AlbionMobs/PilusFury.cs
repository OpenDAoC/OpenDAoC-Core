using DOL.AI.Brain;
using DOL.GS.PacketHandler;
using System;
using DOL.GS;
using DOL.Database;
using System.Collections.Generic;

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
	public class PilusFuryBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public PilusFuryBrain() : base()
		{
			AggroLevel = 0;
			AggroRange = 0;
			ThinkInterval = 1500;
		}
		List<GameLiving> DD_Enemys = new List<GameLiving>();
		private bool CanDD = false;
		public override void Think()
		{
			Point3D point1 = new Point3D(33374, 42009, 15007);//400 range
			Point3D point2 = new Point3D(33376, 41368, 15007);//200
			Point3D point3 = new Point3D(33374, 40973, 15007);//200
			Point3D point4 = new Point3D(33369, 40587, 15007);//200
			Point3D point5 = new Point3D(33370, 40194, 15007);//400
			Point3D point6 = new Point3D(33372, 39597, 15007);//200
			Point3D point7 = new Point3D(33368, 39196, 15007);//200
			Point3D point8 = new Point3D(33374, 38797, 15007);//200
			Point3D point9 = new Point3D(33370, 38653, 15007);//200
			Point3D point10 = new Point3D(33368, 38257, 15007);//200
			Point3D point11 = new Point3D(33364, 37859, 15007);//200
			Point3D point12 = new Point3D(33365, 37667, 15007);//200
			if(Body.IsAlive)
            {
				foreach(GamePlayer player in Body.GetPlayersInRadius(10000))
                {
					if(player != null && player.IsAlive && player.Client.Account.PrivLevel == 1)
                    {
						if((player.IsWithinRadius(point1,400) || player.IsWithinRadius(point2, 200) || player.IsWithinRadius(point3, 200) || player.IsWithinRadius(point4, 200) || player.IsWithinRadius(point5, 400)
						|| player.IsWithinRadius(point6, 200) || player.IsWithinRadius(point7, 200) || player.IsWithinRadius(point8, 200) || player.IsWithinRadius(point9, 200) || player.IsWithinRadius(point10, 200)
						|| player.IsWithinRadius(point11, 200) || player.IsWithinRadius(point12, 200)))
                        {
							if (player.CharacterClass.ID == (int)eCharacterClass.Necromancer && player.ControlledBrain != null)
							{
								if (player.ControlledBrain.Body != null)
								{
									NecromancerPet pet = (NecromancerPet)player.ControlledBrain.Body;
									GamePlayer PetOwner = pet.Owner as GamePlayer;
									if (pet != null && !DD_Enemys.Contains(pet))
									{
										if ((pet.IsWithinRadius(point1, 400) || pet.IsWithinRadius(point2, 200) || pet.IsWithinRadius(point3, 200) || pet.IsWithinRadius(point4, 200) || pet.IsWithinRadius(point5, 400)
										|| pet.IsWithinRadius(point6, 200) || pet.IsWithinRadius(point7, 200) || pet.IsWithinRadius(point8, 200) || pet.IsWithinRadius(point9, 200) || pet.IsWithinRadius(point10, 200)
										|| pet.IsWithinRadius(point11, 200) || pet.IsWithinRadius(point12, 200)))
										{
											DD_Enemys.Add(pet);
											PetOwner.Out.SendMessage("Smoke seeps up through the cracks in the hall's floor.", eChatType.CT_Broadcast, eChatLoc.CL_ChatWindow);
										}
									}
								}
							}
							else
							{
								if (!DD_Enemys.Contains(player))
								{
									DD_Enemys.Add(player);
									player.Out.SendMessage("Smoke seeps up through the cracks in the hall's floor.", eChatType.CT_Broadcast, eChatLoc.CL_ChatWindow);
								}
							}
                        }
						if (!player.IsWithinRadius(point1, 400) && !player.IsWithinRadius(point2, 200) && !player.IsWithinRadius(point3, 200) && !player.IsWithinRadius(point4, 200) && !player.IsWithinRadius(point5, 400)
						&& !player.IsWithinRadius(point6, 200) && !player.IsWithinRadius(point7, 200) && !player.IsWithinRadius(point8, 200) && !player.IsWithinRadius(point9, 200) && !player.IsWithinRadius(point10, 200)
						&& !player.IsWithinRadius(point11, 200) && !player.IsWithinRadius(point12, 200))
						{
							if (player.CharacterClass.ID == (int)eCharacterClass.Necromancer && player.ControlledBrain != null)
							{
								if (player.ControlledBrain.Body != null)
								{
									NecromancerPet pet = (NecromancerPet)player.ControlledBrain.Body;
									if (pet != null && DD_Enemys.Contains(pet))
									{
										if (!pet.IsWithinRadius(point1, 400) && !pet.IsWithinRadius(point2, 200) && !pet.IsWithinRadius(point3, 200) && !pet.IsWithinRadius(point4, 200) && !pet.IsWithinRadius(point5, 400)
										&& !pet.IsWithinRadius(point6, 200) && !pet.IsWithinRadius(point7, 200) && !pet.IsWithinRadius(point8, 200) && !pet.IsWithinRadius(point9, 200) && !pet.IsWithinRadius(point10, 200)
										&& !pet.IsWithinRadius(point11, 200) && !pet.IsWithinRadius(point12, 200))
											DD_Enemys.Remove(pet);
									}
								}
							}
							else
							{
								if (DD_Enemys.Contains(player))
									DD_Enemys.Remove(player);
							}
						}
					}
					if (player != null && player.Client.Account.PrivLevel != 1)
					{
						if (player.CharacterClass.ID == (int)eCharacterClass.Necromancer && player.ControlledBrain != null)
						{
							if (player.ControlledBrain.Body != null)
							{
								NecromancerPet pet = (NecromancerPet)player.ControlledBrain.Body;
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
					if (player.CharacterClass.ID == (int)eCharacterClass.Necromancer && player.ControlledBrain != null)
					{
						if (player.ControlledBrain.Body != null)
						{
							NecromancerPet pet = (NecromancerPet)player.ControlledBrain.Body;
							if (pet != null && !pet.IsAlive && DD_Enemys.Contains(pet))
								DD_Enemys.Remove(pet);
						}
					}
					else
					{
						if (player != null && !player.IsAlive && DD_Enemys.Contains(player))
							DD_Enemys.Remove(player);
					}
				}
				foreach(GameNPC npc in Body.GetNPCsInRadius(10000))
                {
					if(npc != null && npc.IsAlive && npc is GamePet pet)
                    {
						if (pet is not NecromancerPet)
						{
							GamePlayer playerOwner = pet.Owner as GamePlayer;
							if (!DD_Enemys.Contains(pet))
							{
								if ((pet.IsWithinRadius(point1, 400) || pet.IsWithinRadius(point2, 200) || pet.IsWithinRadius(point3, 200) || pet.IsWithinRadius(point4, 200) || pet.IsWithinRadius(point5, 400)
								|| pet.IsWithinRadius(point6, 200) || pet.IsWithinRadius(point7, 200) || pet.IsWithinRadius(point8, 200) || pet.IsWithinRadius(point9, 200) || pet.IsWithinRadius(point10, 200)
								|| pet.IsWithinRadius(point11, 200) || pet.IsWithinRadius(point12, 200)))
									DD_Enemys.Add(pet);
							}
							if (DD_Enemys.Contains(pet))
							{
								if (!pet.IsWithinRadius(point1, 400) && !pet.IsWithinRadius(point2, 200) && !pet.IsWithinRadius(point3, 200) && !pet.IsWithinRadius(point4, 200) && !pet.IsWithinRadius(point5, 400)
								&& !pet.IsWithinRadius(point6, 200) && !pet.IsWithinRadius(point7, 200) && !pet.IsWithinRadius(point8, 200) && !pet.IsWithinRadius(point9, 200) && !pet.IsWithinRadius(point10, 200)
								&& !pet.IsWithinRadius(point11, 200) && !pet.IsWithinRadius(point12, 200))
									DD_Enemys.Remove(pet);
							}
							if (pet != null && !pet.IsAlive && DD_Enemys.Contains(pet))
								DD_Enemys.Remove(pet);
							if (playerOwner != null && playerOwner.IsAlive && playerOwner.Client.Account.PrivLevel != 1 && pet.IsAlive && DD_Enemys.Contains(pet))
								DD_Enemys.Remove(pet);
						}
					}
                }
				if (!CanDD && DD_Enemys.Count > 0)
				{
					new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(PrepareDD), 3000);
					CanDD = true;
				}
			}
			base.Think();
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
				target.StartInterruptTimer(ad, GS.ServerProperties.Properties.SPELL_INTERRUPT_DURATION);
			}
		}		
	}
}

