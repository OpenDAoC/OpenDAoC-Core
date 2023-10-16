using System.Reflection;
using DOL.GS;
using log4net;

namespace DOL.AI.Brain
{
	/// <summary>
	/// A brain that can be controlled
	/// </summary>
	public class BufferSubPetBrain : SubPetBrain
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// Constructs new controlled npc brain
		/// </summary>
		/// <param name="owner"></param>
		public BufferSubPetBrain(GameLiving owner) : base(owner) { }

		/// <summary>
		/// Attack the target on command
		/// </summary>
		/// <param name="target"></param>
		public override void Attack(GameObject target)
		{
			// Don't stop casting. Buffers should prioritize buffing.
			// 'AttackMostWanted()' will be called automatically once the pet is done buffing.
			if (m_orderAttackTarget == target)
				return;

			m_orderAttackTarget = target as GameLiving;
			FiniteStateMachine.SetCurrentState(EFSMStateType.AGGRO);
		}

		#region AI

		/// <summary>
		/// Checks the Abilities
		/// </summary>
		public override void CheckAbilities() { }

		/// <summary>
		/// Checks the Positive Spells.  Handles buffs, heals, etc.
		/// </summary>
		protected override bool CheckDefensiveSpells(Spell spell)
		{
			if (!CanCastDefensiveSpell(spell))
				return false;

			Body.TargetObject = null;
			GamePlayer player;
			GameLiving owner;

			switch (spell.SpellType)
			{
				#region Buffs
				case ESpellType.CombatSpeedBuff:
				case ESpellType.DamageShield:
				case ESpellType.Bladeturn:
					{
						if (!Body.IsAttacking)
						{
							//Buff self
							if (!LivingHasEffect(Body, spell))
							{
								Body.TargetObject = Body;
								break;
							}

							if (spell.Target != ESpellTarget.SELF)
							{
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
									foreach (IControlledBrain icb in ((GameNpc)owner).ControlledNpcList)
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
							}
						}
						break;
					}
				#endregion
			}

			bool casted = false;

			if (Body.TargetObject != null)
				casted = Body.CastSpell(spell, m_mobSpellLine, true);

			return casted;
		}

		/// <summary>
		/// Checks Instant Spells.  Handles Taunts, shouts, stuns, etc.
		/// </summary>
		protected override bool CheckInstantSpells(Spell spell) { return false; }

		#endregion
	}
}
