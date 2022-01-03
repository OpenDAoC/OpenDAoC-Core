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
/*
 * [Ganrod] Nidel 2008-07-08
 * - Class for turret, like 1.90 EU official servers: Turret isn't interrupted
 */

using DOL.AI.Brain;
using DOL.GS.ServerProperties;

namespace DOL.GS
{
	public class TurretPet : GamePet
	{
		public TurretPet(INpcTemplate template)
			: base(template)
		{
		}

		private Spell turretSpell;

		/// <summary>
		/// Get first spell only
		/// </summary>
		public Spell TurretSpell
		{
			get { return turretSpell; }
			set { turretSpell = value; }
		}

		public override int Health { get => base.Health; set => base.Health = value; }

		/// <summary>
		/// Not all summoned turrets 'll throw ambient texts
		/// let's say 20%
		/// </summary>
		protected override void BuildAmbientTexts()
		{
			base.BuildAmbientTexts();
			if (ambientTexts.Count>0)
				foreach (var at in ambientTexts)
					at.Chance /= 5;
		}

        // Temporarily modified
        public override void StartAttack(GameObject attackTarget)
        {
            if (attackTarget == null)
                return;

            if (attackTarget is GameLiving && GameServer.ServerRules.IsAllowedToAttack(this, (GameLiving)attackTarget, true) == false)
                return;

            if (Brain is IControlledBrain)
            {
                if ((Brain as IControlledBrain).AggressionState == eAggressionState.Passive)
                    return;
                GamePlayer playerowner;
                if ((playerowner = ((IControlledBrain)Brain).GetPlayerOwner()) != null)
                    playerowner.Stealth(false);
            }

            TargetObject = attackTarget;
            if (TargetObject.Realm == 0 || Realm == 0)
                m_lastAttackTickPvE = GameLoop.GameLoopTime;
            else
                m_lastAttackTickPvP = GameLoop.GameLoopTime;

            if (attackComponent.Attackers.Count == 0)
            {
                if (SpellTimer == null)
                    SpellTimer = new SpellAction(this);
                if (!SpellTimer.IsAlive)
                    SpellTimer.Start(1);
            }

            if (Brain is TurretMainPetTankBrain)
            {
                attackComponent.StartAttack(TargetObject);
            }
        }

        /// <summary>
        /// [Ganrod] Nidel: Don't interrupt turret cast.
        /// </summary>
        /// <param name="duration"></param>
        /// <param name="attackType"></param>
        /// <param name="attacker"></param>
        public override void StartInterruptTimer(AttackData attack, int duration)
		{
			return;
		}

		public override void AutoSetStats()
		{
			Strength = Properties.PET_AUTOSET_STR_BASE;
			if (Strength < 1)
				Strength = 1;

			Constitution = Properties.PET_AUTOSET_CON_BASE;
			if (Constitution < 1)
				Constitution = 1;

			Quickness = Properties.PET_AUTOSET_QUI_BASE;
			if (Quickness < 1)
				Quickness = 1;

			Dexterity = Properties.PET_AUTOSET_DEX_BASE;
			if (Dexterity < 1)
				Dexterity = 1;

			Intelligence = Properties.PET_AUTOSET_INT_BASE;
			if (Intelligence < 1)
				Intelligence = 1;

			Empathy = 30;
			Piety = 30;
			Charisma = 30;

			//base.AutoSetStats();
		}
	}
}
