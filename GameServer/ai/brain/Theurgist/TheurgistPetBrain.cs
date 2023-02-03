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
using DOL.GS;

namespace DOL.AI.Brain
{
	public class TheurgistPetBrain : ControlledNpcBrain
	{
		private GameObject m_target;

		public TheurgistPetBrain(GameLiving owner) : base(owner)
		{
			IsMainPet = false;
		}

		public override void Think()
		{
			m_target = Body.TargetObject;

			if (m_target == null || m_target.Health <= 0)
			{
				Body.Die(null);
				return;
			}

			if (Body.CurrentFollowTarget != m_target)
			{
				Body.StopFollowing();
				Body.Follow(m_target, MIN_ENEMY_FOLLOW_DIST, MAX_ENEMY_FOLLOW_DIST);
			}

			if (!CheckSpells(eCheckSpellType.Offensive))
				Body.StartAttack(m_target);
		}

		public override eWalkState WalkState { get => eWalkState.Stay; set { } }
		public override eAggressionState AggressionState { get => eAggressionState.Aggressive; set { } }
		public override void Attack(GameObject target) { }
		public override void Disengage() { }
		public override void Follow(GameObject target) { }
		public override void FollowOwner() { }
		public override void Stay() { }
		public override void ComeHere() { }
		public override void Goto(GameObject target) { }
		public override void UpdatePetWindow() { }
	}
}
