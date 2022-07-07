using System;
using log4net;
using System.Reflection;
using DOL.GS;
using DOL.GS.Keeps;
using DOL.GS.Movement;
using System.Threading.Tasks;
using DOL.GS.PacketHandler;

namespace DOL.AI.Brain
{
	/// <summary>
	/// Brain Class for Area Capture Guards
	/// </summary>
	public class KeepGuardBrain : StandardMobBrain
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		public GameKeepGuard guard;
		public long LastNPCAggroCheck;
		public virtual long NPC_AGGRO_DELAY => 10000; //10s

		/// <summary>
		/// Constructor for the Brain setting default values
		/// </summary>
		public KeepGuardBrain()
			: base()
		{
			AggroLevel = 90;
			AggroRange = 1000;
			LastNPCAggroCheck = 0;
		}

		public void SetAggression(int aggroLevel, int aggroRange)
		{
			AggroLevel = aggroLevel;
			AggroRange = aggroRange;
		}

		public override int ThinkInterval
		{
			get
			{
				return 500;
			}
		}

        public override void AttackMostWanted()
        {
			//Commenting out LOS Check on AttackMostWanted - Caused issues with not aggroing or switching aggro target
			// if(Body.TargetObject != null && Body.TargetObject is GamePlayer pl)
            // {
			// 	pl.Out.SendCheckLOS(Body, pl, new CheckLOSResponse(CheckAggroLOS));

            //     if (!AggroLOS) { return; }
			// }
		
			base.AttackMostWanted();
        }

        /// <summary>
        /// Actions to be taken on each Think pulse
        /// </summary>
        public override void Think()
		{
			if (guard == null)
				guard = Body as GameKeepGuard;
			if (guard == null)
			{
				Stop();
				base.KillFSM();
				return;
			}

			if(Body.TargetObject != null && (Body.TargetObject is GamePlayer pl))
            {
				pl.Out.SendCheckLOS(Body, pl, new CheckLOSResponse(CheckAggroLOS));
			}

			if ((guard is GuardArcher || guard is GuardStaticArcher || guard is GuardLord))
			{
				// Drop aggro and disengage if the target is out of range or out of LoS.
				if (Body.IsAttacking && Body.TargetObject is GameLiving living && (Body.IsWithinRadius(Body.TargetObject, AggroRange, false) == false)) //|| !AggroLOS))
				{
					FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
					//Body.StopAttack();
					RemoveFromAggroList(living);
					//Body.TargetObject = null;
					
				}

				if (guard.attackComponent.AttackState && guard.CanUseRanged)
				{
					guard.SwitchToRanged(guard.TargetObject);
				}
			}

			////if we are not doing an action, let us see if we should move somewhere
			//if (guard.CurrentSpellHandler == null && !guard.IsMoving && !guard.attackComponent.AttackState && !guard.InCombat)
			//{
			//	// Tolakram - always clear the aggro list so if this is done by mistake the list will correctly re-fill on next think
			//	ClearAggroList();

			//	if (guard.GetDistanceTo(guard.SpawnPoint, 0) > 50)
			//	{
			//		FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
			//		//guard.WalkToSpawn();
			//	}
			//}

			//Eden - Portal Keeps Guards max distance
            if (guard.Level > 200 && !guard.IsWithinRadius(guard.SpawnPoint, 2000))
			{
				FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
				//ClearAggroList();
				//guard.WalkToSpawn();
			}
            else if (guard.InCombat == false && guard.IsWithinRadius(guard.SpawnPoint, 6000) == false)
			{
				FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
				//ClearAggroList();
				//guard.WalkToSpawn();
			}

			// We want guards to check aggro even when they are returning home, which StandardMobBrain does not, so add checks here
			if (guard.CurrentSpellHandler == null && !guard.attackComponent.AttackState && !guard.InCombat)
			{
				CheckPlayerAggro();
				CheckNPCAggro();

				if (HasAggro && Body.IsReturningHome)
				{
					FSM.SetCurrentState(eFSMStateType.AGGRO);
					//Body.StopMoving();
					//AttackMostWanted();
				}
			}

			base.Think();
		}

		/// <summary>
		/// Check Area for Players to attack
		/// </summary>
		public override void CheckPlayerAggro()
		{
			if (Body.attackComponent.AttackState || Body.CurrentSpellHandler != null)
			{
				return;
			}

			foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
			{
                if (player == null) continue;
                if (GameServer.ServerRules.IsAllowedToAttack(Body, player, true))
				{
					player.Out.SendCheckLOS(Body, player, new CheckLOSResponse(CheckAggroLOS));
					if ( !Body.IsWithinRadius( player, AggroRange ) )
                        continue;
                    if ((Body as GameKeepGuard).Component != null && !GameServer.KeepManager.IsEnemy(Body as GameKeepGuard, player, true))
						continue;
					if (Body is GuardStealther == false && player.IsStealthed)
						continue;

					WarMapMgr.AddGroup((byte)player.CurrentZone.ID, player.X, player.Y, player.Name, (byte)player.Realm);

					if (DOL.GS.ServerProperties.Properties.ENABLE_DEBUG)
					{
						Body.Say("Want to attack player " + player.Name);
					}
                	if (AggroLOS)
                    {
						AddToAggroList(player, player.EffectiveLevel << 1);
					}
					
					return;
				}
			}
		}

		/// <summary>
		/// Check area for NPCs to attack
		/// </summary>
		public override void CheckNPCAggro()
		{
			if (Body.attackComponent.AttackState || Body.CurrentSpellHandler != null)
				return;

			//check NPCs is expensive, so we only do it slowly
			if (GameLoop.GameLoopTime - LastNPCAggroCheck < NPC_AGGRO_DELAY) return;

			LastNPCAggroCheck = GameLoop.GameLoopTime;

			foreach (GameNPC npc in Body.GetNPCsInRadius((ushort)AggroRange))
			{
				if (npc == null || npc.Brain == null || npc is GameKeepGuard || (npc.Brain as IControlledBrain) == null)
					continue;

				GamePlayer player = (npc.Brain as IControlledBrain).GetPlayerOwner();
				
				if (player == null)
					continue;

				if (GameServer.ServerRules.IsAllowedToAttack(Body, npc, true))
				{
					player.Out.SendCheckLOS(Body, npc, new CheckLOSResponse(CheckAggroLOS));
					if ((Body as GameKeepGuard).Component != null && !GameServer.KeepManager.IsEnemy(Body as GameKeepGuard, player, true))
					{
						continue;
					}

					WarMapMgr.AddGroup((byte)player.CurrentZone.ID, player.X, player.Y, player.Name, (byte)player.Realm);

					if (DOL.GS.ServerProperties.Properties.ENABLE_DEBUG)
					{
						Body.Say("Want to attack player " + player.Name + " pet " + npc.Name);
					}
					if (AggroLOS)
                    {
						AddToAggroList(npc, (npc.Level + 1) << 1);
					}
					return;
				}
			}
		}

		public override int CalculateAggroLevelToTarget(GameLiving target)
		{
			GamePlayer checkPlayer = null;
			if (target is GameNPC && (target as GameNPC).Brain is IControlledBrain)
				checkPlayer = ((target as GameNPC).Brain as IControlledBrain).GetPlayerOwner();
			if (target is GamePlayer)
				checkPlayer = target as GamePlayer;
			if (checkPlayer == null)
				return 0;
			if (GameServer.KeepManager.IsEnemy(Body as GameKeepGuard, checkPlayer, true))
				return AggroLevel;
			return 0;
		}
		
		/*
		public override bool AggroLOS
		{
			get { return true; }
		}*/
	}
}
