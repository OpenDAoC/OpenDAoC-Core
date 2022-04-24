using System.Collections;
using System.Collections.Generic;
using DOL.GS;
using log4net;
using System.Reflection;


namespace DOL.AI.Brain
{
	public class ForestheartAmbusherBrain : TheurgistPetBrain, IControlledBrain
	{
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private GameLiving m_owner;
		private GameLiving m_target;
		private bool m_melee = true;
		private bool m_active = true;

		private ushort m_range = 2000;
		public bool Melee { get { return m_melee; } set { m_melee = value; } }
		public ForestheartAmbusherBrain(GameLiving owner) : base(owner)
		{
			if (owner != null)
			{
				m_owner = owner;
			}
			AggroLevel = 100;
			AggroRange = 2000;
			IsMainPet = false;
		}

		public virtual GameNPC GetNPCOwner()
		{
			if (!(Owner is GameNPC))
				return null;

			GameNPC owner = Owner as GameNPC;

			int i = 0;
			while (owner != null)
			{
				i++;
				if (i > 50)
				{
					log.Error("Error with GetNPCOwner !");
					break;
				}
				if (owner.Brain is ForestheartAmbusherBrain)
				{
					if ((owner.Brain as ForestheartAmbusherBrain).Owner is GamePlayer)
						return null;
					else
						owner = (owner.Brain as ForestheartAmbusherBrain).Owner as GameNPC;
				}
				else
					break;
			}
			return owner;
		}
		public virtual GameLiving GetLivingOwner()
		{
		    GamePlayer player = GetPlayerOwner();
		    if (player != null)
				return player;

		    return null;
		}

		public override int ThinkInterval { get { return 1500; } }

		public override void Think() { AttackMostWanted(); }
		
		public override void AttackMostWanted()
		{
			if (!IsActive || !m_active) return;
			if (Body.attackComponent.IsAttacking) return;
			if (Body.attackComponent == null) { Body.attackComponent = new AttackComponent(Body); }
			EntityManager.AddComponent(typeof(AttackComponent), Body);

			// if (m_target == null) m_target = (GameLiving)Body.TempProperties.getProperty<object>("target", null);
			
			// if (m_target == null || !m_target.IsAlive)
			
			m_target = CalculateNextAttackTarget();
			if (m_target != null)
			{
				Body.TempProperties.setProperty("target", m_target);
				if (Body.IsWithinRadius(m_target, Body.AttackRange) || m_melee)
				{
					Body.attackComponent.StartAttack(m_target);
				}
			}
		}
		
		public GameLiving CalculateNextAttackTarget()
		{
			List<GameLiving> newTargets = new List<GameLiving>();
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

					if (!Body.IsWithinRadius(living, m_range, true))
						continue;

					newTargets.Add(living);
				}
			}

			foreach (GamePlayer living in Body.GetPlayersInRadius(m_range, Body.CurrentRegion.IsDungeon ? false : true))
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

			foreach (GameNPC living in Body.GetNPCsInRadius(m_range, Body.CurrentRegion.IsDungeon ? false : true))
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

            m_aggroTable.Clear();
			return null;
		}

		public void SetAggressionState(eAggressionState state) { }
		
		public override bool CheckSpells(eCheckSpellType type)
		{
			return false;
		}

		#region IControlledBrain Members
		public eWalkState WalkState { get { return WalkState; } }
		public eAggressionState AggressionState { get { return eAggressionState.Aggressive; } set { } }
		public GameLiving Owner { get { return m_owner; } }
		public void Attack(GameObject target) { }
		public void Follow(GameObject target) { }
		public void FollowOwner() { }
		public void Stay() { }
		public void ComeHere() { }
		public void Goto(GameObject target) { }
		public void UpdatePetWindow() { }
		public GamePlayer GetPlayerOwner() { return m_owner as GamePlayer; }
		public bool IsMainPet { get { return false; } set { } }
		#endregion
	}
}
