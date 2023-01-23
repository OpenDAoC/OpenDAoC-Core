/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 */
using System.Reflection;
using DOL.GS;
using log4net;

namespace DOL.AI.Brain
{
	/// <summary>
	/// A brain that can be controlled
	/// </summary>
	public class BDBufferBrain : BDPetBrain
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// Constructs new controlled npc brain
		/// </summary>
		/// <param name="owner"></param>
		public BDBufferBrain(GameLiving owner) : base(owner) { }

		/// <summary>
		/// Attack the target on command
		/// </summary>
		/// <param name="target"></param>
		public override void Attack(GameObject target)
		{
			// Don't stop casting. Buffers should prioritize buffing.
			// 'AttackMostWanted()' will be called automatically once the pet is done buffing.
			m_orderAttackTarget = target as GameLiving;
			FSM.SetCurrentState(eFSMStateType.AGGRO);
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
				case (byte)eSpellType.CombatSpeedBuff:
				case (byte)eSpellType.DamageShield:
				case (byte)eSpellType.Bladeturn:
					{
						if (!Body.IsAttacking)
						{
							//Buff self
							if (!LivingHasEffect(Body, spell))
							{
								Body.TargetObject = Body;
								break;
							}

							if (spell.Target != "Self")
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
