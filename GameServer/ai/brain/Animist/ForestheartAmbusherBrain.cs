using System.Collections;
using System.Collections.Generic;
using DOL.GS;
using DOL.GS.Spells;

namespace DOL.AI.Brain
{
	public class ForestheartAmbusherBrain : TurretFNFBrain
	{
		private static log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public ForestheartAmbusherBrain(GameLiving owner) : base(owner)
		{
		}

		/// <summary>
		/// Get a random target from aggro table
		/// </summary>
		/// <returns></returns>
		protected override GameLiving CalculateNextAttackTarget()
		{
			List<GameLiving> newTargets = new List<GameLiving>();
			List<GameLiving> oldTargets = new List<GameLiving>();
			base.CalculateNextAttackTarget();
			lock((m_aggroTable as ICollection).SyncRoot)
			{
				foreach(GameLiving living in m_aggroTable.Keys)
				{
					if(!living.IsAlive || living.CurrentRegion != Body.CurrentRegion || living.ObjectState != GameObject.eObjectState.Active)
						continue;

					if (living.IsMezzed || living.IsStealthed)
						continue;

					if (!Body.IsWithinRadius(living, MAX_AGGRO_DISTANCE, true))
						continue;

					newTargets.Add(living);
				}
			}

			foreach (GamePlayer living in Body.GetPlayersInRadius((ushort)((TurretPet)Body).TurretSpell.Range, Body.CurrentRegion.IsDungeon ? false : true))
            {
                if (!GameServer.ServerRules.IsAllowedToAttack(Body, living, true))
                    continue;

                if (living.IsInvulnerableToAttack)
                    continue;

                if (!living.IsAlive || living.CurrentRegion != Body.CurrentRegion || living.ObjectState != GameObject.eObjectState.Active)
                    continue;

                if (living.IsMezzed || living.IsStealthed)
                    continue;
                
	            newTargets.Add(living);
				
            }

			foreach (GameNPC living in Body.GetNPCsInRadius((ushort)((TurretPet)Body).TurretSpell.Range, Body.CurrentRegion.IsDungeon ? false : true))
            {
                if (!GameServer.ServerRules.IsAllowedToAttack(Body, living, true))
                    continue;

                if (!living.IsAlive || living.CurrentRegion != Body.CurrentRegion || living.ObjectState != GameObject.eObjectState.Active)
                    continue;

                if (living.IsMezzed || living.IsStealthed)
                    continue;

                if (Body.GetConLevel(living) <= -3)
	                continue;

				newTargets.Add(living);
				
			}

			// always favor previous targets and new targets that have not been attacked first, then re-attack old targets

            if (newTargets.Count > 0)
			{
				return newTargets[Util.Random(newTargets.Count - 1)];
			}
			else if (oldTargets.Count > 0)
			{
				return oldTargets[Util.Random(oldTargets.Count - 1)];
			}

			m_aggroTable.Clear();
			return null;
		}

		public override void OnAttackedByEnemy(AttackData ad)
		{
			AddToAggroList(ad.Attacker, (ad.Attacker.Level + 1) << 1);
		}

		/// <summary>
    /// Updates the pet window
    /// </summary>
    public override void UpdatePetWindow()
    {
    }
  }
}