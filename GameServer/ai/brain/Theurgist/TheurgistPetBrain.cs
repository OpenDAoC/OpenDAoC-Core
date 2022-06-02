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
using System;
using System.Collections.Generic;
using System.Text;
using DOL.Events;
using DOL.GS;
using log4net;
using System.Reflection;
using System.Collections;
using DOL.GS.Spells;
using DOL.GS.PacketHandler;
using DOL.GS.Effects;

namespace DOL.AI.Brain
{
	public class TheurgistPetBrain : StandardMobBrain, IControlledBrain
	{
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private GameLiving m_owner;
		private GameLiving m_target;
		private bool m_melee = false;
		private bool m_active = true;
		public static readonly short MIN_ENEMY_FOLLOW_DIST = 90;
		public static readonly short MAX_ENEMY_FOLLOW_DIST = 5000;
		public bool Melee { get { return m_melee; } set { m_melee = value; } }
		public TheurgistPetBrain(GameLiving owner)
		{
			if (owner != null)
			{
				m_owner = owner;
			}
			AggroLevel = 100;
			IsMainPet = false;
		}

		public virtual GameNPC GetNPCOwner()
		{
		    return null;
		}
		public virtual GameLiving GetLivingOwner()
		{
		    GamePlayer player = GetPlayerOwner();
		    if (player != null)
			return player;

		    GameNPC npc = GetNPCOwner();
		    if (npc != null)
			return npc;

		    return null;
		}

		public override int ThinkInterval { get { return 1500; } }

		public override void Think() { AttackMostWanted(); }

		public void SetAggressionState(eAggressionState state) { }

		public override void Notify(DOLEvent e, object sender, EventArgs args)
		{
			if (!IsActive || m_melee || !m_active) return;

			if (args as AttackFinishedEventArgs != null)
			{
				m_melee = true;
				GameLiving target = m_target;
				if (target != null) Body.StartAttack(target);
				return;
			}
			if (e == GameLivingEvent.CastFailed)
			{

				GameLiving target = m_target;
				if (target != null) Body.StartAttack(target);
				return;

			}
		}

		public override void AttackMostWanted()
		{
			if (!IsActive || !m_active) return;
			if (Body.attackComponent == null) { Body.attackComponent = new DOL.GS.AttackComponent(Body); }
			EntityManager.AddComponent(typeof(AttackComponent), Body);
			if (Body.castingComponent == null) { Body.castingComponent = new DOL.GS.CastingComponent(Body); }
			EntityManager.AddComponent(typeof(CastingComponent), Body);

			if (m_target == null) m_target = (GameLiving)Body.TempProperties.getProperty<object>("target", null);
			
			if (m_target == null || !m_target.IsAlive)
			{
				Body.Die(Body);
			}
			else
			{
				GameLiving target = m_target;
				Body.TargetObject = target;
				if (Body.IsWithinRadius(target, Body.AttackRange) || m_melee)
				{
					Body.StartAttack(target);
					if (Body.Name.Contains("air")) CheckSpells(eCheckSpellType.Offensive);
				}
				else if (!CheckSpells(eCheckSpellType.Offensive))
				{
					if(Body.IsWithinRadius(target,Body.attackComponent.AttackRange))
						Body.StartAttack(target);
					//Get closer to the target
					else
					{
						if(Body.CurrentFollowTarget!=target)
						{
							Body.StopFollowing();
							Body.Follow(target, MIN_ENEMY_FOLLOW_DIST, MAX_ENEMY_FOLLOW_DIST);
						}
					}
				}
			}
		}

		public override bool CheckSpells(eCheckSpellType type)
		{
			if (Body == null || Body.Spells == null || Body.Spells.Count < 1 || m_melee)
				return false;

			if (Body.IsCasting)
				return true;

			bool casted = false;
			if (type == eCheckSpellType.Defensive)
			{
				foreach (Spell spell in Body.Spells)
				{
					if (!Body.IsBeingInterrupted && Body.GetSkillDisabledDuration(spell) == 0 && CheckDefensiveSpells(spell))
					{
						casted = true;
						break;
					}
				}
			}
			else
			{
				//Check Offensive Instant Casts
				if (Body.CanCastInstantHarmfulSpells)
				{
					foreach (Spell spell in Body.InstantHarmfulSpells)
					{
						if (Body.GetSkillDisabledDuration(spell) == 0)
						{
							if (Body.Name.Contains("air"))
							{
								if (Util.Chance(25))
								{
									if(CheckInstantSpells(spell))
										break;
								}
							}
							else
							{
								if(CheckInstantSpells(spell))
									break;
							}
						}
					}
				}
				//Check Offensive Casts
				if (Body.CanCastHarmfulSpells)
				{
					foreach (Spell spell in Body.HarmfulSpells)
					{
						if (!Body.IsBeingInterrupted && CheckOffensiveSpells(spell))
						{
							casted = true;
							break;
						}
					}
				}
			}
			//if (this is IControlledBrain && !Body.attackComponent.AttackState)
			//	((IControlledBrain)this).Follow(((IControlledBrain)this).Owner);
			bool checkingSpellLOS = Body.TempProperties.getProperty<Spell>("LOSCURRENTSPELL", null) != null; //Check if pet is checking for spell LoS
			return casted || Body.IsCasting || checkingSpellLOS;
		}

		#region IControlledBrain Members
		public eWalkState WalkState { get { return eWalkState.Stay; } }
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
