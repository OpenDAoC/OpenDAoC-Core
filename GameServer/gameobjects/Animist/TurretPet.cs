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

using DOL.AI.Brain;
using DOL.Database;

namespace DOL.GS
{
    public class TurretPet : GameSummonedPet
    {
        public TurretPet(INpcTemplate template) : base(template) { }

        public Spell TurretSpell;

        public override int Health { get => base.Health; set => base.Health = value; }

        protected override void BuildAmbientTexts()
        {
            base.BuildAmbientTexts();

            if (ambientTexts.Count>0)
            {
                foreach (DbMobXAmbientBehavior ambientText in ambientTexts)
                    ambientText.Chance /= 5;
            }
        }

        public override void StartAttack(GameObject attackTarget)
        {
            if (attackTarget == null)
                return;

            if (attackTarget is GameLiving livingTarget && GameServer.ServerRules.IsAllowedToAttack(this, livingTarget, true) == false)
                return;

            if (Brain is IControlledBrain brain)
            {
                if (brain.AggressionState == eAggressionState.Passive)
                    return;
            }

            TargetObject = attackTarget;

            if (TargetObject.Realm == 0 || Realm == 0)
                m_lastAttackTickPvE = GameLoop.GameLoopTime;
            else
                m_lastAttackTickPvP = GameLoop.GameLoopTime;

            if (Brain is TurretMainPetTankBrain)
                attackComponent.RequestStartAttack(TargetObject);
        }

        public override void StartInterruptTimer(int duration, AttackData.eAttackType attackType, GameLiving attacker)
        {
            // Don't interrupt turrets (1.90 EU).
            return;
        }
    }
}
