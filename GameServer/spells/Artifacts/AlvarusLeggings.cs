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
using DOL.Events;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;

namespace DOL.GS.Spells
{
    /// <summary>
    /// Alvarus spell handler
    /// Water breathing is a subspell
    /// </summary>
    [SpellHandler("AlvarusMorph")]
    public class AlvarusMorph : Morph
    {
        public override void ApplyEffectOnTarget(GameLiving target)
        {
            GamePlayer targetPlayer = target as GamePlayer;

            if (targetPlayer == null)
                return;
            
            
            if (!targetPlayer.IsSwimming && !target.IsUnderwater)
            {
                MessageToCaster("You must be under water to use this ability.", eChatType.CT_SpellResisted);
                return;
            }
            
            if (targetPlayer.effectListComponent.ContainsEffectForEffectType(eEffect.Morph))
            {
                targetPlayer.Out.SendMessage("You already have an active morph!", DOL.GS.PacketHandler.eChatType.CT_SpellResisted, DOL.GS.PacketHandler.eChatLoc.CL_ChatWindow);
                return;
            }
            
            new AlvarusMorphECSEffect(new ECSGameEffectInitParams(target, Spell.Duration, 1, this));
        }

        public AlvarusMorph(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
}
