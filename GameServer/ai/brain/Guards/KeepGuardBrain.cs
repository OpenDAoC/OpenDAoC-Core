using DOL.GS;
using DOL.GS.Keeps;
using DOL.GS.PacketHandler;

namespace DOL.AI.Brain
{
	/// <summary>
	/// Brain Class for Area Capture Guards
	/// </summary>
	public class KeepGuardBrain : StandardMobBrain
	{
		protected GameKeepGuard _keepGuardBody;

		public override GameNPC Body
		{
			get => _keepGuardBody;
			set => _keepGuardBody = value is GameKeepGuard gameKeepGuard ? gameKeepGuard : new GameKeepGuard(); // Dummy object to avoid errors caused by bad DB entries
		}

		public override int ThinkInterval => 500;

		/// <summary>
		/// Constructor for the Brain setting default values
		/// </summary>
		public KeepGuardBrain() : base()
		{
			AggroLevel = 90;
			AggroRange = 1000;
		}

		public void SetAggression(int aggroLevel, int aggroRange)
		{
			AggroLevel = aggroLevel;
			AggroRange = aggroRange;
		}

		public override bool CheckProximityAggro()
		{
			if (Body is GuardArcher or GuardStaticArcher or GuardLord)
			{
				GameObject target = Body.TargetObject;

				// Ranged guards check LoS constantly
				if (target != null)
				{
					GamePlayer losChecker = null;

					if (target is GameNPC && (target as GameNPC).Brain is IControlledBrain)
						losChecker = ((target as GameNPC).Brain as IControlledBrain).GetPlayerOwner();
					else if (target is GamePlayer)
						losChecker = target as GamePlayer;

					if (losChecker != null)
						losChecker.Out.SendCheckLos(Body, target, new CheckLosResponse(LosCheckInCombatCallback));
				}

				// Drop aggro and disengage if the target is out of range
				if (Body.IsAttacking && !Body.IsWithinRadius(target, AggroRange, false))
				{
					FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);

					if (target is GameLiving livingTarget && livingTarget != null)
						RemoveFromAggroList(livingTarget);
				}

				if (Body.attackComponent.AttackState && _keepGuardBody.CanUseRanged)
					Body.SwitchToRanged(target);
			}

			return base.CheckProximityAggro();
		}

		protected override void CheckPlayerAggro()
		{
			foreach (GamePlayer player in Body.GetPlayersInRadius((ushort)AggroRange))
			{
				if (!CanAggroTarget(player))
					continue;

				if (Body is not GuardStealther && player.IsStealthed)
					continue;

				if (player.effectListComponent.ContainsEffectForEffectType(eEffect.Shade))
					continue;

				WarMapMgr.AddGroup((byte) player.CurrentZone.ID, player.X, player.Y, player.Name, (byte) player.Realm);
				player.Out.SendCheckLos(Body, player, new CheckLosResponse(LosCheckForAggroCallback));
				// We don't know if the LoS check will be positive, so we have to ask other players
			}
		}

		/// <summary>
		/// Check area for NPCs to attack
		/// </summary>
		protected override void CheckNPCAggro()
		{
			foreach (GameNPC npc in Body.GetNPCsInRadius((ushort)AggroRange))
			{
				// Non-pet NPCs are ignored
				if (npc is GameKeepGuard || npc.Brain == null || npc.Brain is not IControlledBrain)
					continue;

				GamePlayer player = (npc.Brain as IControlledBrain).GetPlayerOwner();
				
				if (player == null)
					continue;
				if (!CanAggroTarget(npc))
					continue;

				WarMapMgr.AddGroup((byte)player.CurrentZone.ID, player.X, player.Y, player.Name, (byte)player.Realm);
				player.Out.SendCheckLos(Body, npc, new CheckLosResponse(LosCheckForAggroCallback));
				// We don't know if the LoS check will be positive, so we have to ask other players
			}
		}

		public override bool CanAggroTarget(GameLiving target)
		{
			if (AggroLevel <= 0 || !GameServer.ServerRules.IsAllowedToAttack(Body, target, true))
				return false;

			GamePlayer checkPlayer = null;

			if (target is GameNPC && (target as GameNPC).Brain is IControlledBrain)
				checkPlayer = ((target as GameNPC).Brain as IControlledBrain).GetPlayerOwner();
			else if (target is GamePlayer)
				checkPlayer = target as GamePlayer;

			if (checkPlayer == null || !GameServer.KeepManager.IsEnemy(_keepGuardBody, checkPlayer, true))
				return false;

			return true;
		}

		private void LosCheckInCombatCallback(GamePlayer player, eLosCheckResponse response, ushort sourceOID, ushort targetOID)
		{
			if (response is not eLosCheckResponse.TRUE)
			{
				GameObject gameObject = Body.CurrentRegion.GetObject(targetOID);

				if (gameObject is GameLiving gameLiving)
				{
					FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
					RemoveFromAggroList(gameLiving);
				}
			}
		}
	}
}
