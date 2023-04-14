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

namespace DOL.GS.Spells
{
    /// <summary>
    /// Spell Handler for firing bolts
    /// </summary>
    [SpellHandlerAttribute("ChainBolt")]
    public class ChainBoltSpellHandler : BoltSpellHandler
    {
        public ChainBoltSpellHandler(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine) { }

        protected GameLiving _currentSource;
        protected int _maxTick;
        protected int _currentTick;
        protected double _effetiveness = 1.0;

        /// <summary>
        /// called when spell effect has to be started and applied to targets
        /// </summary>
        public override bool StartSpell(GameLiving target)
        {
            if (target == null)
                return false;

            if (_maxTick >= 0)
            {
                _maxTick = (Spell.Pulse > 1) ? Spell.Pulse : 1;
                _currentTick = 1;
                _currentSource = target;
            }

            int ticksToTarget = _currentSource.GetDistanceTo(target) * 1000 / 850; // 850 units per second.
            int delay = 1 + ticksToTarget / 100;

            foreach (GamePlayer player in target.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                player.Out.SendSpellEffectAnimation(_currentSource, target, m_spell.ClientEffect, (ushort) delay, false, 1);

            new BoltOnTargetTimer(target, this, ticksToTarget);
            _currentSource = target;
            _currentTick++;

            return true;
        }

        public override void DamageTarget(AttackData ad, bool showEffectAnimation)
        {
            ad.Damage = (int) (ad.Damage * _effetiveness);
            base.DamageTarget(ad, showEffectAnimation);

            if (_currentTick < _maxTick)
            {
                _effetiveness -= 0.1;

                foreach (GamePlayer pl in _currentSource.GetPlayersInRadius(500))
                {
                    if (GameServer.ServerRules.IsAllowedToAttack(Caster, pl, true))
                    {
                        StartSpell(pl);
                        break;
                    }
                }
            }
        }
    }
}
