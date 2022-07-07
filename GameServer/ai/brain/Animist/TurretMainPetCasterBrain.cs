using System.Collections;
using System.Collections.Generic;
using DOL.GS;

namespace DOL.AI.Brain
{
	public class TurretMainPetCasterBrain : TurretBrain
	{
	  public TurretMainPetCasterBrain(GameLiving owner) : base(owner) { }

		public override void Attack(GameObject target)
		{
			GameLiving defender = target as GameLiving;
			if(defender == null)
			{
				return;
			}

			if(!GameServer.ServerRules.IsAllowedToAttack(Body, defender, true))
			{
				return;
			}

			if(AggressionState == eAggressionState.Passive)
			{
				AggressionState = eAggressionState.Defensive;
				UpdatePetWindow();
			}
			m_orderAttackTarget = defender;
			AttackMostWanted();
			Body.StartAttack(m_orderAttackTarget);
			return;
		}

        protected override GameLiving CalculateNextAttackTarget()
        {
			List<GameLiving> newTargets = new List<GameLiving>();
			List<GameLiving> oldTargets = new List<GameLiving>();

			GameLiving normal = base.CalculateNextAttackTarget();

            if (AggressionState != eAggressionState.Aggressive || normal != null)
                return normal;

            List<GameLiving> livingList = new List<GameLiving>();
            
            lock ((m_aggroTable as ICollection).SyncRoot)
            {
                foreach (GameLiving living in m_aggroTable.Keys)
                {
                    if (!living.IsAlive || living.CurrentRegion != Body.CurrentRegion || living.ObjectState != GameObject.eObjectState.Active)
                        continue;

                    if (!Body.IsWithinRadius(living, MAX_AGGRO_DISTANCE, true))
                        continue;

                    if (!Body.IsWithinRadius(living, ((TurretPet)Body).TurretSpell.Range, true))
                        continue;

                    if (living.IsMezzed || living.IsStealthed)
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

				if (LivingHasEffect(living, ((TurretPet)Body).TurretSpell))
				{
					oldTargets.Add(living);
				}
				else
				{
					newTargets.Add(living as GameLiving);
				}
			}

			foreach (GameNPC living in Body.GetNPCsInRadius((ushort)((TurretPet)Body).TurretSpell.Range, Body.CurrentRegion.IsDungeon ? false : true))
			{
				if (!GameServer.ServerRules.IsAllowedToAttack(Body, living, true))
					continue;

				if (!living.IsAlive || living.CurrentRegion != Body.CurrentRegion || living.ObjectState != GameObject.eObjectState.Active)
					continue;

				if (living.IsMezzed || living.IsStealthed)
					continue;

				if (LivingHasEffect(living, ((TurretPet)Body).TurretSpell))
				{
					oldTargets.Add(living);
				}
				else
				{
					newTargets.Add(living as GameLiving);
				}
			}

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

		public override void CheckNPCAggro()
		{
		  if(AggressionState == eAggressionState.Aggressive)
		  {
		  	base.CheckNPCAggro();
		  }
		}

		public override void CheckPlayerAggro()
		{
		  if (AggressionState == eAggressionState.Aggressive)
		  {
			base.CheckPlayerAggro();
		  }
		}

		public override void OnAttackedByEnemy(AttackData ad)
		{
			if(AggressionState != eAggressionState.Passive)
			{
				AddToAggroList(ad.Attacker, (ad.Attacker.Level + 1) << 1);
			}
		}
	}
}