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

namespace DOL.AI
{
    /// <summary>
    /// <p>This class is the base brain of all npc's that only stay active when players are close</p>
    /// <p>This class defines the base for a brain that activates itself when players get close to
    /// it's body and becomes dormat again after a certain amount of time when no players are close
    /// to it's body anymore.</p>
    /// <p>Useful to save CPU for MANY mobs that have no players in range, they will stay dormant.</p>
    /// </summary>
    public abstract class APlayerVicinityBrain : ABrain
    {
        public APlayerVicinityBrain() : base() { }

        public override bool Start()
        {
            if (!Body.IsVisibleToPlayers)
                return false;

            return base.Start();
        }
    }
}