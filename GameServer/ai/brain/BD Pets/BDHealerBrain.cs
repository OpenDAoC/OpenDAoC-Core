using System.Reflection;
using DOL.GS;
using log4net;

namespace DOL.AI.Brain
{
	/// <summary>
	/// A brain that can be controlled
	/// </summary>
	public class BDHealerBrain : BDPetBrain
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// Constructs new controlled npc brain
		/// </summary>
		/// <param name="owner"></param>
		public BDHealerBrain(GameLiving owner) : base(owner)
		{
			AggroLevel = 0;
			AggroRange = 0;
		}

		#region Control

		/// <summary>
		/// Gets or sets the aggression state of the brain
		/// </summary>
		public override eAggressionState AggressionState
		{
			get { return eAggressionState.Passive; }
			set { }
		}

		/// <summary>
		/// Attack the target on command
		/// </summary>
		/// <param name="target"></param>
		public override void Attack(GameObject target) { }

		#endregion

		#region AI

		/// <summary>
		/// Checks the Abilities
		/// </summary>
		public override void CheckAbilities() { }

		/// <summary>
		/// Do nothing since we don't have offensive spells
		/// </summary>
		/// <param name="spell"></param>
		/// <returns></returns>
		protected override bool CheckOffensiveSpells(Spell spell) { return false; }

		/// <summary>
		/// Checks the Positive Spells.  Handles buffs, heals, etc.
		/// </summary>
		protected override bool CheckDefensiveSpells(Spell spell)
		{
			if (!CanCastDefensiveSpell(spell))
				return false;

			bool casted = false;
			Body.TargetObject = null;
			GamePlayer player;
			GameLiving owner;

			switch (spell.SpellType)
			{
				#region Heals
				case eSpellType.Heal:
					player = GetPlayerOwner();
					if (player != null)
					{

						if (player.HealthPercent < 90)
						{
							Body.TargetObject = player;
							break;
						}
					}
					//Heal owner
					owner = (this as IControlledBrain).Owner;
					if (owner.HealthPercent < 90)
					{
						Body.TargetObject = owner;
						break;
					}
					//Heal self
					if (Body.HealthPercent < 90)
					{
						Body.TargetObject = Body;
						break;
					}

					//Heal other minions
					foreach (IControlledBrain icb in ((GameNPC)owner).ControlledNpcList)
					{
						if (icb == null)
							continue;
						if (icb.Body.HealthPercent < 90)
						{
							Body.TargetObject = icb.Body;
							break;
						}
					}
					break;
				#endregion
				#region Buffs
				case eSpellType.HealthRegenBuff:
					{
						//Buff self
						if (!LivingHasEffect(Body, spell))
						{
							Body.TargetObject = Body;
							break;
						}

						owner = (this as IControlledBrain).Owner;

						//Buff owner
						if (owner != null)
						{
							player = GetPlayerOwner();

							//Buff player
							if (player != null)
							{
								if (!LivingHasEffect(player, spell))
								{
									Body.TargetObject = player;
									break;
								}
							}

							if (!LivingHasEffect(owner, spell))
							{
								Body.TargetObject = owner;
								break;
							}

							//Buff other minions
							foreach (IControlledBrain icb in ((GameNPC)owner).ControlledNpcList)
							{
								if (icb == null)
									continue;
								if (!LivingHasEffect(icb.Body, spell))
								{
									Body.TargetObject = icb.Body;
									break;
								}
							}


						}
						break;
					}
				#endregion
			}

			if (Body.TargetObject != null)
				casted = Body.CastSpell(spell, m_mobSpellLine, true);

			return casted;
		}

		/// <summary>
		/// Don't
		/// </summary>
		/// <param name="spell"></param>
		/// <returns></returns>
		protected override bool CheckInstantSpells(Spell spell) { return false; }

		/// <summary>
		/// Add living to the aggrolist
		/// aggroAmount can be negative to lower amount of aggro
		/// </summary>
		public override void AddToAggroList(GameLiving living, long aggroAmount) { }

		public override void  RemoveFromAggroList(GameLiving living) { }

		/// <summary>
		/// Returns the best target to attack
		/// </summary>
		/// <returns>the best target</returns>
		protected override GameLiving CalculateNextAttackTarget()
		{
			return null;
		}

		/// <summary>
		/// Selects and attacks the next target or does nothing
		/// </summary>
		public override void AttackMostWanted() { }

		/// <summary>
		/// Owner attacked event
		/// </summary>
		/// <param name="e"></param>
		/// <param name="sender"></param>
		/// <param name="arguments"></param>
		public override void OnOwnerAttacked(AttackData ad) { }

		#endregion
	}
}
